namespace VoterSystem.Shared.Dto;

public class UserEmailConfirmRequestDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
}