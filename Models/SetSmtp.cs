using System.ComponentModel.DataAnnotations;

namespace EmailApp.Models
{
    public class SetSmtp
    {
        public Guid Id { get; set; }

        [Required]
        public string Host { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; }

        [Required]
        [EmailAddress]
        public string User { get; set; } = string.Empty;

        [Required]
        public string Pass { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string FromEmail { get; set; } = string.Empty;

        public SetSmtp Clone()
        {
            return new SetSmtp
            {
                Id = Id,
                Host = Host,
                Port = Port,
                User = User,
                Pass = Pass,
                FromEmail = FromEmail
            };
        }
    }
}
