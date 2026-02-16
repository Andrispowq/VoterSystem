using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;

namespace VoterSystem.DataAccess;

public class DbInitializer
{
    private sealed class UserSeedDto
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
        public required Role Role { get; init; }
    }

    private static readonly List<UserSeedDto> Users =
    [
        new() { Email = "example1@gmail.com", Password = "test_Str0ng_password", Role = Role.User },
        new() { Email = "example2@gmail.com", Password = "test_Str0ng_password", Role = Role.User },
        new() { Email = "example3@gmail.com", Password = "test_Str0ng_password", Role = Role.User },
        new() { Email = "example4@gmail.com", Password = "test_Str0ng_password", Role = Role.User },
        new() { Email = "example5@gmail.com", Password = "test_Str0ng_password", Role = Role.User },
        new() { Email = "example6@gmail.com", Password = "test_Str0ng_password", Role = Role.Admin },
        new() { Email = "akmeczo@gmail.com", Password = "1Password#", Role = Role.Admin }
    ];

    public static async Task InitialiseAsync(
        VoterSystemDbContext context,
        IUserService userService,
        IVoteService voteService,
        RoleManager<UserRole> roleManager,
        ILogger<DbInitializer> logger,
        bool prune = true)
    {
        if (prune)
        {
            await context.Database.EnsureDeletedAsync();
        }

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        
        if (!context.Users.Any())
        {
            foreach (var user in Users)
            {
                var usr = new User
                {
                    Name = user.Email,
                    UserName = user.Email.Split("@")[0],
                    Email = user.Email
                };

                var result = await userService.CreateUser(usr, user.Password, user.Role);
                if (result.IsSome)
                {
                    throw new InvalidOperationException(result.AsSome.Value.Message);
                }
            }
        }
        
        var firstUser = await context.Users.FirstOrDefaultAsync();
        var secondUser = await context.Users.Skip(1).FirstOrDefaultAsync();
        
        List<Voting> votings =
        [
            new() { Name = "szavazas 1", StartsAt = DateTime.UtcNow.AddDays(-5), EndsAt = DateTime.UtcNow.AddDays(-2), 
                CreatedByUserId = firstUser!.Id },
            new() { Name = "szavazas 2", StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddDays(5), 
                CreatedByUserId = firstUser.Id },
            new() { Name = "szavazas 3", StartsAt = DateTime.UtcNow.AddDays(1), EndsAt = DateTime.UtcNow.AddDays(5), 
                CreatedByUserId = firstUser.Id },
            new()
            {
                Name = "szavazas 4", StartsAt = DateTime.UtcNow.AddYears(1),
                EndsAt = DateTime.UtcNow.AddYears(1).AddDays(5),
                CreatedByUserId = secondUser!.Id
            },
            new()
            {
                Name = "szavazas 5", StartsAt = DateTime.UtcNow.AddYears(1),
                EndsAt = DateTime.UtcNow.AddYears(1).AddDays(5),
                CreatedByUserId = secondUser.Id
            }
        ];

        if (!context.Votings.Any())
        {
            foreach (var voting in votings)
            {
                await context.Votings.AddAsync(voting);
                await context.SaveChangesAsync();
                
                var choices = new List<VoteChoice>
                {
                    new() { Name = "elso valasz", VotingId = voting.VotingId, Description = "nem kamu" },
                    new() { Name = "masodik valasz", VotingId = voting.VotingId, Description = "nem kamu" },
                    new() { Name = "harmadik valasz", VotingId = voting.VotingId, Description = "nem kamu" },
                    new() { Name = "negyedik valasz", VotingId = voting.VotingId, Description = "nem kamu" },
                };

                foreach (var choice in choices)
                {
                    await context.VoteChoices.AddAsync(choice);
                }
                
                await context.SaveChangesAsync();
            }
        }

        if (!context.AnonymousBallots.Any())
        {
            var users = await context.Users.ToListAsync();
            var voting = await context.Votings.OrderBy(x => x.CreatedAt).Skip(1).FirstAsync();
            var choices = await context.VoteChoices.Where(c => c.VotingId == voting.VotingId)
                .ToListAsync();

            await TryCastVote(voteService, logger, users[4], choices[0]);
            await TryCastVote(voteService, logger, users[1], choices[^1]);
            await TryCastVote(voteService, logger, users[2], choices[0]);
            await TryCastVote(voteService, logger, users[3], choices[1]);
        }
    }

    private static async Task TryCastVote(IVoteService voteService, 
        ILogger<DbInitializer> logger,
        User user, VoteChoice choice)
    {
        var result = await voteService.CastVote(user, choice);
        if (result.IsError)
        {
            logger.LogWarning("Failed to vote with user {User} for option {Choice}: {Error}",
                user.Name, choice.Name, result.ToString());
        }
    }
    
    private static async Task SeedRolesAsync(RoleManager<UserRole> roleManager)
    {
        string[] roleNames = [ nameof(Role.User), nameof(Role.Admin) ];

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                // Create the roles and seed them to the database
                await roleManager.CreateAsync(new UserRole(roleName));
            }
        }
    }
}