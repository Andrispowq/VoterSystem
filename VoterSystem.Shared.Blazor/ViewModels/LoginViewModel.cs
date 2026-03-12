using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Blazor.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string? Password { get; set; }

    [RegularExpression(@"^\d{6}$", ErrorMessage = "Two-factor code must be 6 digits")]
    public string? TwoFactorCode { get; set; }
}
