using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class VotingParticipation : ITimestamped, IRoleControlled
{
    [Key]
    public long VotingParticipationId { get; init; }
    public required Guid UserId { get; init; }
    public required long VotingId { get; init; }
    public required bool HasVoted { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    public virtual Voting Voting { get; set; } = null!;
    
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