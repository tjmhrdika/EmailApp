using System.ComponentModel.DataAnnotations;
using EmailApp.Models;

public class EmailGroup
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public List<Email> Emails { get; set; } = new();
}