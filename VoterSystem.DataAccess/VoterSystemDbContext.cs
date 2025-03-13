using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess;

public class VoterSystemDbContext(DbContextOptions<VoterSystemDbContext> options) : DbContext(options)
{
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<Voting> Votings { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Vote>()
            .HasKey(p => new { p.UserId, p.VotingId });
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}