namespace VoterSystem.Shared.Dto;

public record LoginAttemptResultDto
{
    public required bool Succeeded { get; init; }
    public required bool RequiresTwoFactor { get; init; }
    public required Guid? ChallengeId { get; init; }

    public static LoginAttemptResultDto Success()
    {
        return new LoginAttemptResultDto
        {
            Succeeded = true,
            RequiresTwoFactor = false,
            ChallengeId = null
        };
    }

    public static LoginAttemptResultDto TwoFactorRequired(Guid challengeId)
    {
        return new LoginAttemptResultDto
        {
            Succeeded = false,
            RequiresTwoFactor = true,
            ChallengeId = challengeId
        };
    }

    public static LoginAttemptResultDto Failure()
    {
        return new LoginAttemptResultDto
        {
            Succeeded = false,
            RequiresTwoFactor = false,
            ChallengeId = null
        };
    }
}
