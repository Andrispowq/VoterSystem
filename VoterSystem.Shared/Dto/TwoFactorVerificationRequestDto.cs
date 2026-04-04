namespace VoterSystem.Shared.Dto;

public record TwoFactorVerificationRequestDto
{
    public required Guid UserId { get; init; }
    public required string Code { get; init; }
}
