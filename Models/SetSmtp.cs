using System.ComponentModel.DataAnnotations;
public class SetSmtp
{
    public Guid Id { get; set; }
    [Required]
    public string Host { get; set; } = String.Empty;
    [Required]
    public int Port { get; set; }
    [Required, EmailAddress]
    public string User { get; set; } = String.Empty;
    [Required]
    public string Pass { get; set; } = String.Empty;
    [Required, EmailAddress]
    public string FromEmail { get; set; } = String.Empty;
    public SetSmtp Clone()
    {
        return new SetSmtp
        {
            Id = this.Id,
            Host = this.Host,
            Port = this.Port,
            User = this.User,
            Pass = this.Pass,
            FromEmail = this.FromEmail
        };
    }    
}