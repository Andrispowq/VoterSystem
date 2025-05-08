using System.ComponentModel.DataAnnotations;

namespace VoterSystem.DataAccess.Model;

public class VoteChoice : ITimestamped, IRoleControlled
{
    [Key] public long ChoiceId { get; set; }
    [MaxLength(50)] public required string Name { get; set; }
    [MaxLength(255)] public string? Description { get; set; }
    public required long VotingId { get; set; }
    
    public DateTime CreatedAt { get; set;  } = DateTime.UtcNow;

    public virtual Voting Voting { get; set; } = null!;
    public virtual ICollection<Vote> Votes { get; set; } = [];
    
    //Choices can be seen by anyone
    public bool CanAccessById(bool isAdmin, Guid userId)
    {
        return true;
    }

    //They can be created by the person creating the votings
    public bool CanCreate(bool isAdmin, Guid userId)
    {
        return Voting.CreatedByUserId == userId;
    }

    //They can be updated by the person creating the votings
    public bool CanUpdate(bool isAdmin, Guid userId)
    {
        return Voting.CreatedByUserId == userId;
    }

    //They can be deleted by the person creating the votings or admins
    public bool CanDelete(bool isAdmin, Guid userId)
    {
        return isAdmin || Voting.CreatedByUserId == userId;
    }
}