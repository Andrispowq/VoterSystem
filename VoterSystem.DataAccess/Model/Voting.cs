using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class Voting : ITimestamped
{
    public long VotingId { get; set; }
    public DateTime CreatedAt { get; set;  } = DateTime.UtcNow;
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }
    
    [NotMapped]
    public bool IsOngoing => DateTime.UtcNow >= CreatedAt && DateTime.UtcNow <= EndsAt;
    
    public virtual ICollection<Vote> Votes { get; set; } = [];
}