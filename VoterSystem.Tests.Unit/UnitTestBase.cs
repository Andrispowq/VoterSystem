using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;

namespace VoterSystem.Tests.Unit;

public class UnitTestBase : IAsyncDisposable
{
    protected readonly VoterSystemDbContext Context;

    protected UnitTestBase()
    {
        var options = new DbContextOptionsBuilder<VoterSystemDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new VoterSystemDbContext(options);
    }

    protected static User NextValidUser
    {
        get
        {
            var id = Helpers.NextUniqueId;
            return new User
            {
                Email = $"{id}@email.com",
                Name = $"{id}",
                UserName = $"{id}@email.com",
                Role = Role.User,
                LoginMode = UserLoginMode.Password
            };
        }
    }

    protected static Voting GetUnstartedValidVoting(Guid creatorId)
    {
        return new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddDays(2),
            CreatedByUserId = creatorId,
        };
    }

    protected static Voting GetStartedValidVoting(Guid creatorId)
    {
        return new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = creatorId
        };
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}