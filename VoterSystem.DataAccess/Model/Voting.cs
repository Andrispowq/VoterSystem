using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class Voting : ITimestamped
{
    public long VotingId { get; set; }
    [MaxLength(255)]
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set;  } = DateTime.UtcNow;
    public required DateTime StartsAt { get; set; }
    public required DateTime EndsAt { get; set; }
    public required Guid CreatedByUserId { get; init; }
    
    [NotMapped]
    public bool HasStarted => DateTime.UtcNow >= StartsAt;
    [NotMapped]
    public bool HasEnded => DateTime.UtcNow > EndsAt;
    [NotMapped]
    public bool IsOngoing => HasStarted && !HasEnded;
    
    [ForeignKey("CreatedByUserId")]
    public virtual User CreatedByUser { get; set; } = null!;
    
    public virtual ICollection<Vote> Votes { get; set; } = [];
    public virtual ICollection<VoteChoice> VoteChoices { get; set; } = [];
}