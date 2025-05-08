using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public class UserPasswordResetRequestDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    [MinLength(6)]
    public required string NewPassword { get; set; }
}