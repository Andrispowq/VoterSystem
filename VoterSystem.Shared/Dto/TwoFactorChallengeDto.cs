namespace VoterSystem.Shared.Dto;

public record TwoFactorChallengeDto
{
    public required Guid UserId { get; init; }
    public required string Message { get; init; }
}
