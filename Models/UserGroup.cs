using System;

namespace EmailApp.Models
{
    public class UserGroup
    {
        public Guid UserId { get; set; }
        public int GroupId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string? AssignedBy { get; set; }
        
        public User User { get; set; } = null!;
        public Group Group { get; set; } = null!;
    }
}