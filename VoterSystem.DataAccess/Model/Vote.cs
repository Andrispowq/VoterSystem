using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class Vote : ITimestamped, IRoleControlled
{
    public required Guid UserId { get; init; }
    public required long VotingId { get; init; }
    public required long ChoiceId { get; init; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    public virtual Voting Voting { get; set; } = null!;
    [ForeignKey("ChoiceId")]
    public virtual VoteChoice VoteChoice { get; set; } = null!;

    //Admins can access all votes, and users can see their own ones
    public bool CanAccessById(bool isAdmin, Guid userId)
    {
        return isAdmin || UserId == userId;
    }

    //Only non-admins can vote
    public bool CanCreate(bool isAdmin, Guid userId)
    {
        return !isAdmin;
    }

    //A vote cannot be changed
    public bool CanUpdate(bool isAdmin, Guid userId)
    {
        return false;
    }

    //Votes are only deleted if the voting they reference is also deleted
    public bool CanDelete(bool isAdmin, Guid userId)
    {
        return false;
    }
}