using System.ComponentModel.DataAnnotations;

public class Email
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(320, MinimumLength = 4)]
    public string Address { get; set; } = String.Empty;
}