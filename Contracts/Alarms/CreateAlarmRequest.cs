using System.ComponentModel.DataAnnotations;

namespace EmailApp.Contracts.Alarms
{
    public class CreateAlarmRequest
    {
        [Required]
        public int AlarmId { get; set; }

        [Required]
        public string AlarmState { get; set; } = string.Empty;

        public DateTime? EventStamp { get; set; }

        public short Priority { get; set; }
        public int? CommentId { get; set; }
        public int? OperatorID { get; set; }
        public string? AlarmTransition { get; set; }
        public string? AlarmType { get; set; }
        public DateTime? TransitionTime { get; set; }
        public short? TransitionTimeFracSec { get; set; }
        public short? TransitionTimeZoneOffset { get; set; }
        public short? TransitionDaylightAdjustment { get; set; }
        public string? OperatorName { get; set; }
        public string? OperatorNode { get; set; }
    }
}
