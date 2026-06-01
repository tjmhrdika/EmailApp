using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EmailApp.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
}