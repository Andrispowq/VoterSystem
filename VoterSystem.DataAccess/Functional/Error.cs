namespace VoterSystem.DataAccess.Functional;

public class Error(string message)
{
    public string Message { get; } = message;
}