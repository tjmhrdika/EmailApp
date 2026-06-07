using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EmailApp.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
        
        public bool IsAdmin { get; set; }
        
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
}