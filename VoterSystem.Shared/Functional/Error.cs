namespace VoterSystem.Shared.Functional;

public class Error(string message, Exception? exception)
{
    public string Message { get; } = message;
    public Exception? Exception { get; } = exception;
    
    public override string ToString() => $"Error(Message={Message}, Exception={Exception})";
}