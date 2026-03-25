using System.ComponentModel.DataAnnotations;

public class Email
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(320, MinimumLength = 4)]
    public string Address { get; set; } = String.Empty;

    public Guid? EmailGroupId { get; set; }
    public EmailGroup? EmailGroup { get; set; }
}