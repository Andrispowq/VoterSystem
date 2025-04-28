using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class Voting : ITimestamped
{
    public long VotingId { get; set; }
    [MaxLength(255)]
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set;  } = DateTime.UtcNow;
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }
    
    [NotMapped]
    public bool IsOngoing => DateTime.UtcNow >= CreatedAt && DateTime.UtcNow <= EndsAt;
    
    public virtual ICollection<Vote> Votes { get; set; } = [];
    public virtual ICollection<VoteChoice> VoteChoices { get; set; } = [];
}