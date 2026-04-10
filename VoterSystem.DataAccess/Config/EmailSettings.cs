namespace VoterSystem.DataAccess.Config;

public class EmailSettings
{
    public string Host { get; init; } = null!;
    public int? Port { get; init; }
    public bool EnableSsl { get; init; }
    public string UserName { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FromEmail { get; init; } = null!;
}