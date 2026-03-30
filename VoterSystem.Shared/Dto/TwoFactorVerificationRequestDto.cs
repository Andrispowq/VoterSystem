namespace VoterSystem.Shared.Dto;

public record TwoFactorVerificationRequestDto
{
    public required Guid ChallengeId { get; init; }
    public required string Code { get; init; }
}
