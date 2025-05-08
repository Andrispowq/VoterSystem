namespace VoterSystem.DataAccess.Model;

public record Tokens
{
    public required string AuthToken { get; init; }
    public required Guid RefreshToken { get; init; }
    public required Guid UserId { get; init; }
}