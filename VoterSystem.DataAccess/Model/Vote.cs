namespace VoterSystem.DataAccess.Model;

public class Vote : ITimestamped
{
    public required long UserId { get; init; }
    public required long VotingId { get; init; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual Voting Voting { get; set; } = null!;
}