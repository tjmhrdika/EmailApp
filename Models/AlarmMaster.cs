using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EmailApp.Models
{
    public class AlarmMaster
    {
        [Key]
        public int AlarmId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public short Priority { get; set; }
        public int? CauseId { get; set; }
        public DateTime OriginationTime { get; set; }
        
        public ICollection<AlarmDetail> AlarmDetails { get; set; } = new List<AlarmDetail>();
    }
}
