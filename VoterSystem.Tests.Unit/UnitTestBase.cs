using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;

namespace VoterSystem.Tests.Unit;

public abstract class UnitTestBase : IAsyncDisposable
{
    protected readonly VoterSystemDbContext Context;

    protected UnitTestBase()
    {
        var options = new DbContextOptionsBuilder<VoterSystemDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new VoterSystemDbContext(options);
    }

    protected User NextValidUser
    {
        get
        {
            var id = Helpers.NextUniqueId;
            return new User
            {
                Email = $"{id}@email.com",
                Name = $"{id}",
                UserName = $"{id}@email.com",
                Role = Role.User
            };
        }
    }

    protected Voting GetNextValidVoting(Guid creatorId)
    {
        return new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddDays(2),
            CreatedByUserId = creatorId,
        };
    }

    protected Voting GetNextInvalidVoting(Guid creatorId)
    {
        return new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = creatorId,
        };
    }

    protected static DefaultHttpContext BuildHttpContext(Guid userId, params Role[] roles)
    {
        if (roles.Length == 0)
        {
            roles = new[] { Role.User };
        }

        var claims = new List<Claim>
        {
            new(TokenIssuerKeys.UserIdKey, userId.ToString()),
            new(TokenIssuerKeys.UsernameKey, $"user-{userId}"),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "UnitTestsAuthType");
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    protected static DefaultHttpContext BuildAnonymousHttpContext()
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}
