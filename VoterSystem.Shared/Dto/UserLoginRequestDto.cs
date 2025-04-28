using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public class UserLoginRequestDto
{
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [Required(ErrorMessage = "Email is required")]
    public required string Email { get; set; }
    [PasswordPropertyText]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public required string Password { get; set; }
}