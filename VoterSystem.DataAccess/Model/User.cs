using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VoterSystem.DataAccess.Model;

public class User : IdentityUser<Guid>, ISoftDeletable, IRoleControlled
{
    [MaxLength(50)] public string Name { get; set; } = null!;
    public Guid? RefreshToken { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public virtual ICollection<Voting> Votings { get; set; } = [];
    public virtual ICollection<Vote> Votes { get; set; } = [];
    
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
        return isAdmin || Id == userId;
    }

    //Delete is allowed for admins and ourselves
    public bool CanDelete(bool isAdmin, Guid userId)
    {
        return isAdmin || Id == userId;
    }
}