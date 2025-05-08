using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Blazor.ViewModels;

public class UserPasswordResetViewModel
{
    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords must match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}