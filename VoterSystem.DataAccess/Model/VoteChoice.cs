using System.ComponentModel.DataAnnotations;

namespace VoterSystem.DataAccess.Model;

public class VoteChoice : ITimestamped
{
    [Key] public long ChoiceId { get; set; }
    [MaxLength(50)] public required string Name { get; set; }
    [MaxLength(255)] public string? Description { get; set; }
    public required long VotingId { get; set; }
    
    public DateTime CreatedAt { get; set;  } = DateTime.UtcNow;

    public virtual Voting Voting { get; set; } = null!;
}