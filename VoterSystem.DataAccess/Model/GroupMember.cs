namespace VoterSystem.DataAccess.Model;

public class GroupMembers : ITimestamped
{
    public required Guid GroupId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid AddedByUserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual User AddedByUser { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}