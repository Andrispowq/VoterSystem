namespace VoterSystem.DataAccess.Config;

public record JwtSettings
{
    public string SecretKey { get; init; }
    public string Audience { get; init; }
    public string Issuer { get; init; }
    public int AccessTokenExpirationMinutes { get; init; }
}