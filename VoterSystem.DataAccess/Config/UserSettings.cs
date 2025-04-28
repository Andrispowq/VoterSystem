namespace VoterSystem.DataAccess.Config;

public record UserSettings
{
    public required int MinimumPasswordLength { get; init; }
}