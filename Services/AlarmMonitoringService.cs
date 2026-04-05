using EmailApp.Data;
using EmailApp.Models;
using EmailApp.Services;
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
        private readonly IOptions<MonitoringSettings> _settings;
        private DateTime _lastCheckTime;

        public AlarmMonitoringService(
            IServiceScopeFactory scopeFactory,
            ILogger<AlarmMonitoringService> logger,
            IOptions<MonitoringSettings> settings)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = settings;
            _lastCheckTime = DateTime.Now.AddMinutes(-_settings.Value.LookbackMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Alarm Monitoring Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckNewAlarms(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(_settings.Value.CheckIntervalSeconds), stoppingToken);
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

            _logger.LogInformation($"Checking alarms after {_lastCheckTime}");

            var since = DateTime.Now.AddMinutes(-_settings.Value.LookbackMinutes);

            var newAlarms = await alarmDb.AlarmDetails
                .Include(ad => ad.AlarmMaster)
                .Where(ad => ad.AlarmState.Trim() == "UNACK_ALM" && ad.EventStamp >= since)
                .OrderBy(ad => ad.EventStamp)
                .Take(20)
                .ToListAsync(stoppingToken);

            if (!newAlarms.Any())
            {
                _logger.LogInformation("No new alarms");
                return;
            }

            _logger.LogInformation($"Found {newAlarms.Count} alarms");

            var cooldownTime = DateTime.Now.AddMinutes(-_settings.Value.CooldownMinutes);
            var recentlySentAlarmIds = await appDb.AlarmEmailTracking
                .Where(t => t.EmailSent && t.EmailSentAt > cooldownTime)
                .Select(t => t.AlarmId)
                .ToListAsync(stoppingToken);

            var unsentAlarms = newAlarms
                .Where(a => !recentlySentAlarmIds.Contains(a.AlarmId))
                .ToList();

            if (!unsentAlarms.Any())
            {
                _logger.LogInformation("All alarms within cooldown period, skipping");
                return;
            }

            var recipients = await appDb.Emails
                .Select(e => e.Address)
                .ToListAsync(stoppingToken);

            if (!recipients.Any())
            {
                _logger.LogWarning("No email recipients found");
                return;
            }

            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            foreach (var alarm in unsentAlarms)
            {
                await ProcessAlarm(alarm, recipients, emailService, appDb);
            }

            _lastCheckTime = unsentAlarms.Max(a => a.EventStamp);
        }

        private async Task ProcessAlarm(AlarmDetail alarm, List<string> recipients, IEmailService emailService, AppDbContext appDb)
        {
            var tracking = new AlarmEmailTracking
            {
                AlarmDetailId = alarm.AlarmDetailId,
                AlarmId = alarm.AlarmId,
                EmailSent = false,
                CreatedAt = DateTime.Now
            };

            try
            {
                var subject = $"[ALARM] {alarm.AlarmMaster.TagName} - Priority {alarm.AlarmMaster.Priority}";
                var body = $@"
ALARM NOTIFICATION
==================
Tag      : {alarm.AlarmMaster.TagName}
Group    : {alarm.AlarmMaster.GroupName}
Priority : {alarm.AlarmMaster.Priority}
Value    : {alarm.AlarmValue}
Type     : {alarm.AlarmType?.Trim()}
Limit    : {alarm.Limit} ({alarm.LimitString})
Time     : {alarm.TransitionTime:yyyy-MM-dd HH:mm:ss}
Time UTC : {alarm.EventStamp:yyyy-MM-dd HH:mm:ss}
State    : {alarm.AlarmState.Trim()}

Please check immediately.
";

                await emailService.SendBulkEmailAsync(recipients, subject, body);

                tracking.EmailSent = true;
                tracking.EmailSentAt = DateTime.Now;
                tracking.EmailRecipients = string.Join(",", recipients);

                _logger.LogInformation($"Email sent for alarm {alarm.AlarmDetailId}: {alarm.AlarmMaster.TagName}");
            }
            catch (Exception ex)
            {
                tracking.ErrorMessage = ex.Message;
                _logger.LogError(ex, $"Failed to send email for alarm {alarm.AlarmDetailId}");
            }
            finally
            {
                await appDb.AlarmEmailTracking.AddAsync(tracking);
                await appDb.SaveChangesAsync();
            }
        }
    }

    public class MonitoringSettings
    {
        public int CheckIntervalSeconds { get; set; } = 10;
        public int LookbackMinutes { get; set; } = 10;
        public int CooldownMinutes { get; set; } = 3;
    }
}