namespace VoterSystem.Shared.Blazor.Config;

public class AppConfig
{
    public required long ToastDurationInMillis { get; init; }
    public required string HubBaseUrl { get; init; }
}