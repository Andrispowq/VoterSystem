namespace VoterSystem.DataAccess.Config;

public record UserSettings
{
    public int MinimumPasswordLength { get; init; }
}