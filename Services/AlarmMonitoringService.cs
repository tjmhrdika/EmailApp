using EmailApp.Configuration;
using EmailApp.Data;
using EmailApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailApp.Services
{
    public class AlarmMonitoringService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlarmMonitoringService> _logger;
        private readonly MonitoringOptions _options;
        private DateTime _lastCheckTime;

        public AlarmMonitoringService(
            IServiceScopeFactory scopeFactory,
            ILogger<AlarmMonitoringService> logger,
            IOptions<MonitoringOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
            _lastCheckTime = DateTime.Now.AddMinutes(-Math.Max(_options.LookbackMinutes, 60));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Alarm Monitoring Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckNewAlarms(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    TryLogError(ex, "Error checking alarms");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(GetCheckIntervalSeconds()), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private int GetCheckIntervalSeconds()
        {
            return _options.CheckIntervalSeconds > 0
                ? _options.CheckIntervalSeconds
                : 10;
        }

        private void TryLogError(Exception ex, string message)
        {
            try
            {
                _logger.LogError(ex, message);
            }
            catch
            {
                // Logging must not terminate the web app when Windows EventLog is unavailable.
            }
        }

        private async Task CheckNewAlarms(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var alarmDb = scope.ServiceProvider.GetRequiredService<AlarmDbContext>();
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scanTime = DateTime.Now;

            var alarms = await LoadUnsentAlarms(alarmDb, appDb, scanTime, stoppingToken);

            if (!alarms.Any())
            {
                UpdateLastCheckTime(alarms, scanTime);
                return;
            }

            var recipients = await LoadRecipients(appDb, stoppingToken);

            if (!recipients.Any())
            {
                await TrackAlarmsWithoutRecipients(appDb, alarms, stoppingToken);
                UpdateLastCheckTime(alarms, scanTime);
                return;
            }

            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await ProcessAlarmBatch(alarms, recipients, emailService, appDb, stoppingToken);

            UpdateLastCheckTime(alarms, scanTime);
        }

        private async Task<List<AlarmDetail>> LoadUnsentAlarms(
            AlarmDbContext alarmDb,
            AppDbContext appDb,
            DateTime scanTime,
            CancellationToken stoppingToken)
        {
            var alarmStates = GetConfiguredAlarmStates();

            var alarmQuery = alarmDb.AlarmDetails
                .AsNoTracking()
                .Include(ad => ad.AlarmMaster)
                .Where(ad => ad.EventStamp > _lastCheckTime && ad.EventStamp <= scanTime);

            if (alarmStates.Length == 1)
            {
                var state = alarmStates[0];
                alarmQuery = alarmQuery.Where(ad => ad.AlarmState == state);
            }
            else if (alarmStates.Length == 2)
            {
                var firstState = alarmStates[0];
                var secondState = alarmStates[1];
                alarmQuery = alarmQuery.Where(ad => ad.AlarmState == firstState || ad.AlarmState == secondState);
            }
            else
            {
                alarmQuery = alarmQuery.Where(ad => alarmStates.Contains(ad.AlarmState));
            }

            var alarms = await alarmQuery
                .OrderBy(ad => ad.EventStamp)
                .Take(100)
                .ToListAsync(stoppingToken);

            if (!alarms.Any())
                return alarms;

            var alarmDetailIds = alarms
                .Select(alarm => alarm.AlarmDetailId)
                .ToList();

            var trackedAlarmIds = await appDb.AlarmEmailTracking
                .AsNoTracking()
                .Where(tracking => alarmDetailIds.Contains(tracking.AlarmDetailId))
                .Select(t => t.AlarmDetailId)
                .ToListAsync(stoppingToken);

            var trackedAlarmIdSet = trackedAlarmIds.ToHashSet();

            return alarms
                .Where(a => !trackedAlarmIdSet.Contains(a.AlarmDetailId))
                .ToList();
        }

        private string[] GetConfiguredAlarmStates()
        {
            var states = _options.AlarmStates
                .Where(state => !string.IsNullOrWhiteSpace(state))
                .Select(state => state.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return states.Length == 0
                ? ["UNACK_ALM", "UNACK_RTN"]
                : states;
        }

        private static async Task<List<string>> LoadRecipients(AppDbContext appDb, CancellationToken stoppingToken)
        {
            return await appDb.Emails
                .AsNoTracking()
                .Select(e => e.Address)
                .Where(address => !string.IsNullOrWhiteSpace(address))
                .Distinct()
                .ToListAsync(stoppingToken);
        }

        private async Task TrackAlarmsWithoutRecipients(
            AppDbContext appDb,
            IEnumerable<AlarmDetail> alarms,
            CancellationToken stoppingToken)
        {
            foreach (var alarm in alarms)
            {
                appDb.AlarmEmailTracking.Add(CreateTracking(alarm, false, null, "No email recipients configured"));
            }

            await appDb.SaveChangesAsync(stoppingToken);
            _logger.LogWarning("No email recipients found");
        }

        private async Task ProcessAlarmBatch(
            IReadOnlyCollection<AlarmDetail> alarms,
            List<string> recipients,
            IEmailService emailService,
            AppDbContext appDb,
            CancellationToken stoppingToken)
        {
            var recipientList = string.Join(",", recipients);
            var subject = alarms.Count == 1
                ? $"[ALARM] {alarms.First().AlarmMaster.TagName} - Priority {alarms.First().AlarmMaster.Priority}"
                : $"[ALARM DIGEST] {alarms.Count} alarm notifications";

            try
            {
                var body = CreateAlarmDigestEmailBody(alarms);

                await emailService.SendBulkEmailAsync(recipients, subject, body);

                foreach (var alarm in alarms)
                {
                    appDb.AlarmEmailTracking.Add(CreateTracking(alarm, true, recipientList));
                }

                _logger.LogInformation("Alarm digest sent for {AlarmCount} alarms to {RecipientCount} recipients", alarms.Count, recipients.Count);
            }
            catch (Exception ex)
            {
                foreach (var alarm in alarms)
                {
                    appDb.AlarmEmailTracking.Add(CreateTracking(alarm, false, null, ex.Message));
                }

                _logger.LogError(ex, "Failed to send alarm digest for {AlarmCount} alarms", alarms.Count);
            }

            await appDb.SaveChangesAsync(stoppingToken);
        }

        private static string CreateAlarmDigestEmailBody(IReadOnlyCollection<AlarmDetail> alarms)
        {
            var lines = new List<string>
            {
                "Alarm Notification Digest",
                $"Total Alarms: {alarms.Count}",
                $"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "Action: Please check immediately.",
                ""
            };

            foreach (var alarm in alarms.OrderBy(item => item.EventStamp))
            {
                lines.Add($"Alarm Detail ID: {alarm.AlarmDetailId}");
                lines.Add($"Event Time: {alarm.EventStamp:yyyy-MM-dd HH:mm:ss}");
                lines.Add($"State: {alarm.AlarmState}");
                lines.Add($"Tag: {alarm.AlarmMaster.TagName}");
                lines.Add($"Group: {alarm.AlarmMaster.GroupName}");
                lines.Add($"Priority: {alarm.AlarmMaster.Priority}");
                lines.Add("");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static AlarmEmailTracking CreateTracking(
            AlarmDetail alarm,
            bool emailSent,
            string? recipients = null,
            string? errorMessage = null)
        {
            return new AlarmEmailTracking
            {
                AlarmDetailId = alarm.AlarmDetailId,
                AlarmId = alarm.AlarmId,
                EmailSent = emailSent,
                EmailSentAt = emailSent ? DateTime.Now : null,
                EmailRecipients = recipients,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.Now
            };
        }

        private void UpdateLastCheckTime(IReadOnlyCollection<AlarmDetail> alarms, DateTime scanTime)
        {
            if (!alarms.Any())
            {
                _lastCheckTime = scanTime;
                return;
            }

            _lastCheckTime = alarms.Count >= 100
                ? alarms.Max(alarm => alarm.EventStamp).AddTicks(-1)
                : scanTime;
        }
    }
}
