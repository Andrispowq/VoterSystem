namespace VoterSystem.DataAccess.Model;

public interface ITimestamped
{
    public DateTime CreatedAt { get; }
}