using System.ComponentModel.DataAnnotations;

namespace VoterSystem.DataAccess.Model;

public class User : ITimestamped
{
    public long UserId { get; set; }
    [MaxLength(50)] public required string Name { get; set; }
    [MaxLength(50)] public required string Email { get; set; }
    [MaxLength(100)] public required string PasswordHash { get; set; }
    public required UserLevel UserLevel { get; init; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; internal set; }
    
    public virtual ICollection<Vote> Votes { get; set; } = [];
}