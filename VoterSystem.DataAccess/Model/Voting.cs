using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class Voting : ITimestamped, IRoleControlled
{
    public long VotingId { get; set; }
    [MaxLength(255)]
    public required string Name { get; init; }
    public DateTime CreatedAt { get; set;  } = DateTime.UtcNow;
    public required DateTime StartsAt { get; set; }
    public required DateTime EndsAt { get; set; }
    public required Guid CreatedByUserId { get; init; }
    
    [NotMapped]
    public bool HasStarted => DateTime.UtcNow >= StartsAt && VoteChoices.Count >= 2;
    [NotMapped]
    public bool HasEnded => DateTime.UtcNow > EndsAt && VoteChoices.Count >= 2;
    [NotMapped]
    public bool IsOngoing => HasStarted && !HasEnded;
    
    [ForeignKey("CreatedByUserId")]
    public virtual User CreatedByUser { get; set; } = null!;
    
    public virtual ICollection<Vote> Votes { get; set; } = [];
    public virtual ICollection<VoteChoice> VoteChoices { get; set; } = [];
    
    //Votings can be accessed by anyone
    public bool CanAccessById(bool isAdmin, Guid userId)
    {
        return true;
    }

    //Creating is a user thing
    public bool CanCreate(bool isAdmin, Guid userId)
    {
        return !isAdmin;
    }

    //Updating is a user thing
    public bool CanUpdate(bool isAdmin, Guid userId)
    {
        return !isAdmin && CreatedByUserId == userId;
    }

    //Deleting is done by admins or the user creating it
    public bool CanDelete(bool isAdmin, Guid userId)
    {
        return isAdmin || CreatedByUserId == userId;
    }
}