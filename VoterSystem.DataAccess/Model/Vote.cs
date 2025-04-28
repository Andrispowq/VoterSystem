using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class Vote : ITimestamped
{
    public required Guid UserId { get; init; }
    public required long VotingId { get; init; }
    public required long ChoiceId { get; init; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    public virtual Voting Voting { get; set; } = null!;
    public virtual VoteChoice VoteChoice { get; set; } = null!;
}