using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Blazor.ViewModels;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}