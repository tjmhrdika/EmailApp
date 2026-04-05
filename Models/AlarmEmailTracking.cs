using System;

namespace EmailApp.Models
{
    public class AlarmEmailTracking
    {
        public int Id { get; set; }
        public int AlarmDetailId { get; set; }
        public int AlarmId { get; set; }
        public bool EmailSent { get; set; }
        public DateTime? EmailSentAt { get; set; }
        public string? EmailRecipients { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}