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
        public DateTime EventStamp { get; set; }
        public int OperatorId { get; set; }
        public int? CommentId { get; set; }

        public AlarmMaster AlarmMaster { get; set; } = null!;
    }
}