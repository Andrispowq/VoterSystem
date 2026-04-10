using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using VoterSystem.Shared.Dto;

namespace VoterSystem.DataAccess.Model;

public class User : IdentityUser<Guid>, ISoftDeletable, IRoleControlled
{
    [MaxLength(50)] public string Name { get; set; } = null!;
    public Guid? RefreshToken { get; set; }
    public DateTime? DeletedAt { get; set; }
    public required Role Role { get; set; }
    public required UserLoginMode LoginMode { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
    
    public virtual ICollection<Voting> Votings { get; set; } = [];
    public virtual ICollection<VotingParticipation> VotingParticipations { get; set; } = [];
    public virtual ICollection<GroupMembers> Groups { get; set; } = [];
    public virtual ICollection<GroupMembers> GroupAdditions { get; set; } = [];
    public virtual ICollection<Group> GroupsCreated { get; set; } = [];
    
    //We can access users as an admin or ourselves
    public bool CanAccessById(bool isAdmin, Guid userId)
    {
        return isAdmin || Id == userId;
    }

    //Creation is allowed
    public bool CanCreate(bool isAdmin, Guid userId)
    {
        return true;
    }

    //Update is allowed for admins and ourselves
    public bool CanUpdate(bool isAdmin, Guid userId)
    {
        return !IsDeleted && (isAdmin || Id == userId);
    }

    //Delete is allowed for admins and ourselves
    public bool CanDelete(bool isAdmin, Guid userId)
    {
        return !IsDeleted && (isAdmin || Id == userId);
    }
}
