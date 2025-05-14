namespace VoterSystem.DataAccess.Functional;

public class Error(string message)
{
    public string Message { get; } = message;
    
    public override string ToString() => $"Error({Message})";
}