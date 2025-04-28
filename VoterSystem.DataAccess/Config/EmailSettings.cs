namespace VoterSystem.DataAccess.Config
{
    public class EmailSettings
    {
        public required string Host { get; init; }
        public int Port { get; init; }
        public bool EnableSsl { get; init; }
        public required string UserName { get; init; }
        public required string Password { get; init; }
        public required string FromEmail { get; init; }
    }
}
