using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmailApp.Data;
using EmailApp.Models;
using EmailApp.Services;

namespace EmailApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public AlarmController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAlarm([FromBody] AlarmDetail request)
        {
            var master = await _context.AlarmMasters
                .FirstOrDefaultAsync(x => x.AlarmId == request.AlarmId);

            if (master == null)
                return BadRequest(new { message = "AlarmMaster not found" });

            if (request.EventStamp == default)
                request.EventStamp = DateTime.UtcNow;

            _context.AlarmDetails.Add(request);
            await _context.SaveChangesAsync();

            var emails = await _context.Emails
                .Select(x => x.Address)
                .ToListAsync();

            if (emails.Any())
            {
                var subject = $"Alarm Triggered: {master.TagName}";
                var body = $"Alarm {master.TagName} ACTIVE at {request.EventStamp:yyyy-MM-dd HH:mm:ss}";
                
                await _emailService.SendBulkEmailAsync(emails, subject, body);
            }

            return Ok(new 
            { 
                message = "Alarm created successfully",
                data = request 
            });
        }
    }
}
