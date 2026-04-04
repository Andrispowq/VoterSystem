namespace VoterSystem.Shared.Dto;

public record LoginAttemptResultDto
{
    public required bool Succeeded { get; init; }
    public required bool RequiresTwoFactor { get; init; }
    public Guid? UserId { get; init; }

    public static LoginAttemptResultDto Success()
    {
        return new LoginAttemptResultDto
        {
            Succeeded = true,
            RequiresTwoFactor = false
        };
    }

    public static LoginAttemptResultDto TwoFactorRequired(Guid userId)
    {
        return new LoginAttemptResultDto
        {
            Succeeded = false,
            RequiresTwoFactor = true,
            UserId = userId
        };
    }

    public static LoginAttemptResultDto Failure()
    {
        return new LoginAttemptResultDto
        {
            Succeeded = false,
            RequiresTwoFactor = false
        };
    }
}
