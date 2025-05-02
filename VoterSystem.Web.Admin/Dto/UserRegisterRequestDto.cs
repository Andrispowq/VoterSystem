using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Web.Admin.Dto;

public class UserRegisterRequestDto
{
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [Required(ErrorMessage = "Email is required")]
    public required string Email { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(50, ErrorMessage = "Name cannot be more than 50 characters")]
    public required string Name { get; set; }
    
    [PasswordPropertyText]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public required string Password { get; set; }
}