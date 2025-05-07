using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public class UserChangePasswordRequestDto
{
    [PasswordPropertyText]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public required string OldPassword { get; set; }
    
    [PasswordPropertyText]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public required string NewPassword { get; set; }
}