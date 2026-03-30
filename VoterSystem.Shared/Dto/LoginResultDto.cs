namespace VoterSystem.Shared.Dto;

public record LoginResultDto
{
    public required TokensDto? Tokens { get; init; }
    public required TwoFactorChallengeDto? Challenge { get; init; }

    public bool RequiresTwoFactor => Challenge is not null;

    public static LoginResultDto FromTokens(TokensDto tokens)
    {
        return new LoginResultDto
        {
            Tokens = tokens,
            Challenge = null
        };
    }

    public static LoginResultDto FromChallenge(TwoFactorChallengeDto challenge)
    {
        return new LoginResultDto
        {
            Tokens = null,
            Challenge = challenge
        };
    }
}
