using System.ComponentModel.DataAnnotations;

namespace VoterSystem.DataAccess.Model;

public class Group : IRoleControlled, ITimestamped, ISoftDeletable
{
    [Key]
    public Guid GroupId { get; init; }
    public required Guid CreatorUserId { get; init; }
    [MaxLength(32)]
    public required string Name { get; set; }
    [MaxLength(255)]
    public required string Description { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public virtual User CreatorUser { get; init; } = null!;
    public virtual ICollection<GroupMembers> Members { get; set; } = [];
    public virtual ICollection<Voting> Votings { get; set; } = [];
    
    public bool CanAccessById(bool isAdmin, Guid userId)
    {
        return isAdmin || Members.Any(m => m.UserId == userId);
    }

    public bool CanCreate(bool isAdmin, Guid userId)
    {
        return isAdmin;
    }

    public bool CanUpdate(bool isAdmin, Guid userId)
    {
        return isAdmin && CreatorUserId == userId;
    }

    public bool CanDelete(bool isAdmin, Guid userId)
    {
        return isAdmin && CreatorUserId == userId;
    }
}