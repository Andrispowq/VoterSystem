using System.ComponentModel.DataAnnotations;

namespace VoterSystem.DataAccess.Model;

public class GroupMembers : ITimestamped
{
    public long GroupId { get; init; }
    public Guid UserId { get; init; }
    public Guid AddedByUserId { get; init; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual User AddedByUser { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}