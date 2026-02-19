using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess;

public class VoterSystemDbContext(DbContextOptions<VoterSystemDbContext> options) 
    : IdentityDbContext<User, UserRole, Guid>(options)
{
    public DbSet<VotingParticipation> VotingParticipations { get; set; } = null!;
    public DbSet<AnonymousBallot> AnonymousBallots { get; set; } = null!;
    public DbSet<Voting> Votings { get; set; } = null!;
    public DbSet<VoteChoice> VoteChoices { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupMembers> GroupMembers { get; set; } = null!;

    public new async Task<Option<ServiceError>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await base.SaveChangesAsync(cancellationToken);
            return new Option<ServiceError>.None();
        }
        catch (DbUpdateConcurrencyException e)
        {
            return new BadRequestError("Failed to save the database changes", e);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasOne(p => p.CreatorUser)
                .WithMany(p => p.GroupsCreated)
                .HasForeignKey(p => p.CreatorUserId)
                .IsRequired();
        });

        modelBuilder.Entity<GroupMembers>(entity =>
        {
            entity.HasKey(p => new { p.GroupId, p.UserId });
            
            entity.HasOne(p => p.User)
                .WithMany(p => p.Groups)
                .HasForeignKey(p => p.UserId)
                .IsRequired();
            
            entity.HasOne(p => p.Group)
                .WithMany(p => p.Members)
                .HasForeignKey(p => p.GroupId)
                .IsRequired();

            entity.HasOne(p => p.AddedByUser)
                .WithMany(p => p.GroupAdditions)
                .HasForeignKey(p => p.AddedByUserId)
                .IsRequired();
        });
        
        modelBuilder.Entity<AnonymousBallot>(entity =>
        {
            entity.HasIndex(v => new { v.VotingId, VoteTag = v.VoteTagBase64 })
                .IsUnique();
            
            entity.HasIndex(v => new { v.VotingId, v.ChoiceId });
            
            entity.HasOne(v => v.Voting)
                .WithMany(c => c.AnonymousBallots)
                .HasForeignKey(v => v.VotingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.VoteChoice)
                .WithMany(c => c.AnonymousBallots)
                .HasForeignKey(v => v.VoteTagBase64)
                .OnDelete(DeleteBehavior.NoAction);
        });
        
        modelBuilder.Entity<VotingParticipation>(entity =>
        {
            entity.HasKey(p => new { p.UserId, p.VotingId });
            
            entity.HasOne(v => v.Voting)
                .WithMany(c => c.VotingParticipations)
                .HasForeignKey(v => v.VotingId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(v => v.User)
                .WithMany(c => c.VotingParticipations)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<VoteChoice>(entity =>
        {
            //Cannot have the same choice in the same voting
            entity.HasIndex(v => new { v.VotingId, v.Name })
                .IsUnique();
            
            entity.HasOne(v => v.Voting)
                .WithMany(c => c.VoteChoices)
                .HasForeignKey(v => v.VotingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Voting>(entity => 
        {
            entity.HasIndex(v => v.Name)
                .IsUnique();
            
            entity.HasOne(v => v.CreatedByUser)
                .WithMany(user => user.Votings)
                .HasForeignKey(v => v.CreatedByUserId)
                .IsRequired();
            
            entity.HasOne(v => v.Group)
                .WithMany(user => user.Votings)
                .HasForeignKey(v => v.GroupId);
        });
    }
}