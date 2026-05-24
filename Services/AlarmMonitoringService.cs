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
            _lastCheckTime = DateTime.UtcNow.AddMinutes(-_options.LookbackMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Alarm Monitoring Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckNewAlarms(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(_options.CheckIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking alarms");
                }
            }
        }

        private async Task CheckNewAlarms(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var alarmDb = scope.ServiceProvider.GetRequiredService<AlarmDbContext>();
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scanTime = DateTime.UtcNow;

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

            foreach (var alarm in alarms)
            {
                await ProcessAlarm(alarm, recipients, emailService, appDb, stoppingToken);
            }

            UpdateLastCheckTime(alarms, scanTime);
        }

        private async Task<List<AlarmDetail>> LoadUnsentAlarms(
            AlarmDbContext alarmDb,
            AppDbContext appDb,
            DateTime scanTime,
            CancellationToken stoppingToken)
        {
            var alarms = await alarmDb.AlarmDetails
                .AsNoTracking()
                .Include(ad => ad.AlarmMaster)
                .Where(ad => ad.AlarmState == "UNACK_ALM")
                .Where(ad => ad.EventStamp > _lastCheckTime && ad.EventStamp <= scanTime)
                .OrderBy(ad => ad.EventStamp)
                .Take(100)
                .ToListAsync(stoppingToken);

            if (!alarms.Any())
                return alarms;

            var trackedAlarmIds = await appDb.AlarmEmailTracking
                .AsNoTracking()
                .Select(t => t.AlarmDetailId)
                .ToListAsync(stoppingToken);

            var trackedAlarmIdSet = trackedAlarmIds.ToHashSet();

            return alarms
                .Where(a => !trackedAlarmIdSet.Contains(a.AlarmDetailId))
                .ToList();
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

        private async Task ProcessAlarm(
            AlarmDetail alarm,
            List<string> recipients,
            IEmailService emailService,
            AppDbContext appDb,
            CancellationToken stoppingToken)
        {
            var tracking = CreateTracking(alarm, false);

            try
            {
                var subject = $"[ALARM] {alarm.AlarmMaster.TagName} - Priority {alarm.AlarmMaster.Priority}";
                var body = CreateAlarmEmailBody(alarm);

                await emailService.SendBulkEmailAsync(recipients, subject, body);

                tracking.EmailSent = true;
                tracking.EmailSentAt = DateTime.UtcNow;
                tracking.EmailRecipients = string.Join(",", recipients);

                _logger.LogInformation("Email sent for alarm {AlarmDetailId}: {TagName}", alarm.AlarmDetailId, alarm.AlarmMaster.TagName);
            }
            catch (Exception ex)
            {
                tracking.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Failed to send email for alarm {AlarmDetailId}", alarm.AlarmDetailId);
            }
            finally
            {
                await appDb.AlarmEmailTracking.AddAsync(tracking);
                await appDb.SaveChangesAsync(stoppingToken);
            }
        }

        private static string CreateAlarmEmailBody(AlarmDetail alarm)
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Alarm Notification",
                $"Tag: {alarm.AlarmMaster.TagName}",
                $"Group: {alarm.AlarmMaster.GroupName}",
                $"Priority: {alarm.AlarmMaster.Priority}",
                $"State: {alarm.AlarmState}",
                $"Event Time: {alarm.EventStamp:yyyy-MM-dd HH:mm:ss}",
                $"Alarm Detail ID: {alarm.AlarmDetailId}",
                "Action: Please check immediately."
            });
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
                EmailSentAt = emailSent ? DateTime.UtcNow : null,
                EmailRecipients = recipients,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
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
