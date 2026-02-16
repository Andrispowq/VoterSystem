using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess;

public class VoterSystemDbContext(DbContextOptions<VoterSystemDbContext> options) 
    : IdentityDbContext<User, UserRole, Guid>(options)
{
    public DbSet<VotingParticipation> VotingParticipations { get; set; } = null!;
    public DbSet<AnonymousBallot> AnonymousBallots { get; set; } = null!;
    public DbSet<Voting> Votings { get; set; } = null!;
    public DbSet<VoteChoice> VoteChoices { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<AnonymousBallot>()
            .HasKey(p => p.AnonymousBallotId);
        
        modelBuilder.Entity<AnonymousBallot>()
            .HasOne(v => v.Voting)
            .WithMany(c => c.AnonymousBallots)
            .HasForeignKey(v => v.VotingId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<AnonymousBallot>()
            .HasOne(v => v.VoteChoice)
            .WithMany(c => c.AnonymousBallots)
            .HasForeignKey(v => v.ChoiceId)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<VotingParticipation>()
            .HasKey(p => new { p.UserId, p.VotingId });
        
        modelBuilder.Entity<VotingParticipation>()
            .HasOne(v => v.Voting)
            .WithMany(c => c.VotingParticipations)
            .HasForeignKey(v => v.VotingId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<VotingParticipation>()
            .HasOne(v => v.User)
            .WithMany(c => c.VotingParticipations)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<VoteChoice>()
            .HasOne(v => v.Voting)
            .WithMany(c => c.VoteChoices)
            .HasForeignKey(v => v.VotingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Voting>()
            .HasIndex(v => v.Name)
            .IsUnique();
        
        //Cannot have the same choice in the same voting
        modelBuilder.Entity<VoteChoice>()
            .HasIndex(v => new { v.VotingId, v.Name })
            .IsUnique();
        
        modelBuilder.Entity<AnonymousBallot>()
            .HasIndex(v => new { v.VotingId, v.VoteTag })
            .IsUnique();

        modelBuilder.Entity<AnonymousBallot>()
            .HasIndex(v => new { v.VotingId, v.ChoiceId });
        
        modelBuilder.Entity<Voting>()
            .HasOne(v => v.CreatedByUser)
            .WithMany(user => user.Votings)
            .HasForeignKey(v => v.CreatedByUserId)
            .IsRequired();
    }
}