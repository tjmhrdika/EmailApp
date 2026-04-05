using System;
using System.ComponentModel.DataAnnotations;

namespace EmailApp.Models
{
    public class AlarmDetail
    {
        [Key]
        public int AlarmDetailId { get; set; }
        public int AlarmId { get; set; }
        public string AlarmState { get; set; } = string.Empty;
        public string? AlarmType { get; set; }
        public double? AlarmValue { get; set; }
        public DateTime EventStamp { get; set; }
        public short Priority { get; set; }
        public short? OutstandingAcks { get; set; }
        public double? Limit { get; set; }
        public string? LimitString { get; set; }
        public string? ValueString { get; set; }
        public string? AlarmTransition { get; set; }
        public DateTime? TransitionTime { get; set; }
        public short? TransitionTimeFracSec { get; set; }
        public short? TransitionTimeZoneOffset { get; set; }
        public short? TransitionDaylightAdjustment { get; set; }
        public string? OperatorName { get; set; }
        public string? OperatorNode { get; set; }
        public int? CommentId { get; set; }
        public int? OperatorID { get; set; }
        public string? UnAckDuration { get; set; }
        public int? Cookie { get; set; }

        public AlarmMaster AlarmMaster { get; set; } = null!;
    }
}