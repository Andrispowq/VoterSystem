using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.Tests.UnitTests;

public class UnitTestBase
{
    protected readonly VoterSystemDbContext Context;

    protected UnitTestBase()
    {
        var options = new DbContextOptionsBuilder<VoterSystemDbContext>()
            .UseInMemoryDatabase("TestVotingDatabase")
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
                UserName = $"{id}@email.com"
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
}