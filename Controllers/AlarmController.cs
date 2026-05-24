using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmailApp.Contracts.Alarms;
using EmailApp.Data;
using EmailApp.Models;
using EmailApp.Services;

namespace EmailApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmController : ControllerBase
    {
        private readonly AlarmDbContext _alarmDb;
        private readonly AppDbContext _appDb;
        private readonly IEmailService _emailService;

        public AlarmController(
            AlarmDbContext alarmDb,
            AppDbContext appDb,
            IEmailService emailService)
        {
            _alarmDb = alarmDb;
            _appDb = appDb;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAlarm([FromBody] CreateAlarmRequest request)
        {
            var master = await _alarmDb.AlarmMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AlarmId == request.AlarmId);

            if (master == null)
                return BadRequest(new { message = "AlarmMaster not found" });

            var alarm = CreateAlarmDetail(request);

            _alarmDb.AlarmDetails.Add(alarm);
            await _alarmDb.SaveChangesAsync();

            var emails = await _appDb.Emails
                .AsNoTracking()
                .Select(x => x.Address)
                .ToListAsync();

            if (emails.Any())
            {
                var subject = $"Alarm Triggered: {master.TagName}";
                var body = CreateAlarmEmailBody(alarm, master);
                
                await _emailService.SendBulkEmailAsync(emails, subject, body);
            }

            await TrackAlarmEmail(alarm, emails);

            return Ok(new 
            { 
                message = "Alarm created successfully",
                data = alarm 
            });
        }

        private static AlarmDetail CreateAlarmDetail(CreateAlarmRequest request)
        {
            return new AlarmDetail
            {
                AlarmId = request.AlarmId,
                AlarmState = request.AlarmState,
                EventStamp = request.EventStamp ?? DateTime.UtcNow,
                Priority = request.Priority,
                CommentId = request.CommentId,
                OperatorID = request.OperatorID,
                AlarmTransition = request.AlarmTransition,
                AlarmType = request.AlarmType,
                TransitionTime = request.TransitionTime,
                TransitionTimeFracSec = request.TransitionTimeFracSec,
                TransitionTimeZoneOffset = request.TransitionTimeZoneOffset,
                TransitionDaylightAdjustment = request.TransitionDaylightAdjustment,
                OperatorName = request.OperatorName,
                OperatorNode = request.OperatorNode
            };
        }

        private static string CreateAlarmEmailBody(AlarmDetail alarm, AlarmMaster master)
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Alarm Notification",
                $"Tag: {master.TagName}",
                $"Group: {master.GroupName}",
                $"Priority: {master.Priority}",
                $"State: {alarm.AlarmState}",
                $"Event Time: {alarm.EventStamp:yyyy-MM-dd HH:mm:ss}",
                $"Alarm Detail ID: {alarm.AlarmDetailId}",
                "Action: Please check immediately."
            });
        }

        private async Task TrackAlarmEmail(AlarmDetail alarm, IReadOnlyCollection<string> emails)
        {
            var tracking = new AlarmEmailTracking
            {
                AlarmDetailId = alarm.AlarmDetailId,
                AlarmId = alarm.AlarmId,
                EmailSent = emails.Any(),
                EmailSentAt = emails.Any() ? DateTime.UtcNow : null,
                EmailRecipients = emails.Any() ? string.Join(",", emails) : null,
                ErrorMessage = emails.Any() ? null : "No email recipients configured",
                CreatedAt = DateTime.UtcNow
            };

            _appDb.AlarmEmailTracking.Add(tracking);
            await _appDb.SaveChangesAsync();
        }
    }
}
