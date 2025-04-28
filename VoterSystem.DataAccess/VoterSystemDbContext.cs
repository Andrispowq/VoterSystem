using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess;

public class VoterSystemDbContext(DbContextOptions<VoterSystemDbContext> options) 
    : IdentityDbContext<User, UserRole, Guid>(options)
{
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<Voting> Votings { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        //We don't include the choiceId because we can only vote once
        modelBuilder.Entity<Vote>()
            .HasKey(p => new { p.UserId, p.VotingId });
        
        modelBuilder.Entity<Voting>()
            .HasIndex(v => v.Name)
            .IsUnique();
        
        //Cannot have the same choice in the same voting
        modelBuilder.Entity<VoteChoice>()
            .HasIndex(v => new { v.VotingId, v.Name })
            .IsUnique();
    }
}