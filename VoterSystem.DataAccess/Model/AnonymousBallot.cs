using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoterSystem.DataAccess.Model;

public class AnonymousBallot : IRoleControlled
{
    [Key]
    public Guid BallotId { get; set; }
    public required long VotingId { get; init; }
    public required long ChoiceId { get; init; }
    [MaxLength(63)]
    public required string VoteTagBase64 { get; init; }
    
    [ForeignKey("ChoiceId")]
    public virtual VoteChoice VoteChoice { get; set; } = null!;
    public virtual Voting Voting { get; set; } = null!;
    
    //Admins can access all votes, and users can see their own ones
    public bool CanAccessById(bool isAdmin, Guid userId)
    {
        return true;
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