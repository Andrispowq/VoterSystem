using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;

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
                Role = Role.User
            };
        }
    }

    protected static Voting GetNextValidVoting(Guid creatorId)
    {
        return new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            EndsAt = DateTime.UtcNow.AddDays(2),
            CreatedByUserId = creatorId,
        };
    }

    protected static Voting GetNextInvalidVoting(Guid creatorId)
    {
        return new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = creatorId
        };
    }

    protected static IHttpContextAccessor CreateHttpContextAccessor(
        Guid? userId = null,
        Role role = Role.User,
        bool isAuthenticated = true)
    {
        var context = new DefaultHttpContext();
        if (isAuthenticated)
        {
            var claims = new List<Claim>
            {
                new(TokenIssuerKeys.UserIdKey, (userId ?? Guid.NewGuid()).ToString()),
                new(TokenIssuerKeys.UsernameKey, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, role.ToString())
            };

            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, authenticationType: "TestAuth"));
        }

        return new HttpContextAccessor
        {
            HttpContext = context
        };
    }

    protected static NullLogger<T> CreateLogger<T>() => NullLogger<T>.Instance;

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}