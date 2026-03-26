using System.ComponentModel.DataAnnotations;
public class SetSmtp
{
    public Guid Id { get; set; }
    [Required]
    public string Smtp { get; set; } = String.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = String.Empty;
    [Required]
    public string Password { get; set; } = String.Empty;
}