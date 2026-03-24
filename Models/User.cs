using System.ComponentModel.DataAnnotations;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = String.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, and number")]
    public string Password { get; set; } = String.Empty;
    
    public bool IsAdmin { get; set; }
}