namespace VoterSystem.Shared.Dto;

public record TwoFactorChallengeDto
{
    public required Guid ChallengeId { get; init; }
    public required string Message { get; init; }
}
