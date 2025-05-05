using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess;

public class VoterSystemDbContext(DbContextOptions<VoterSystemDbContext> options) 
    : IdentityDbContext<User, UserRole, Guid>(options)
{
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<Voting> Votings { get; set; } = null!;
    public DbSet<VoteChoice> VoteChoices { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Vote>()
            .HasKey(p => new { p.UserId, p.ChoiceId });
        
        modelBuilder.Entity<Vote>()
            .HasOne(v => v.VoteChoice)
            .WithMany(c => c.Votes)
            .HasForeignKey(v => v.ChoiceId)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Voting)
            .WithMany(c => c.Votes)
            .HasForeignKey(v => v.VotingId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voting>()
            .HasIndex(v => v.Name)
            .IsUnique();
        
        //Cannot have the same choice in the same voting
        modelBuilder.Entity<VoteChoice>()
            .HasIndex(v => new { v.VotingId, v.Name })
            .IsUnique();
        
        modelBuilder.Entity<Voting>()
            .HasOne(v => v.CreatedByUser)
            .WithMany(user => user.Votings)
            .HasForeignKey(v => v.CreatedByUserId)
            .IsRequired();
    }
}