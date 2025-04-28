namespace VoterSystem.DataAccess.Config;

public record JwtSettings
{
    public required string SecretKey { get; init; }
    public required string Audience { get; init; }
    public required string Issuer { get; init; }
    public required int AccessTokenExpirationMinutes { get; init; }
}