using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VoterSystem.DataAccess.Model;

public class User : IdentityUser<Guid>
{
    [MaxLength(50)] public string Name { get; set; } = null!;
    public Guid? RefreshToken { get; set; }
    
    public virtual ICollection<Voting> Votings { get; set; } = [];
    public virtual ICollection<Vote> Votes { get; set; } = [];
}