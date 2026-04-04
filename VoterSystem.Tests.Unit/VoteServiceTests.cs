using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit;

public class VoteServiceTests : UnitTestBase, IDisposable
{
    private readonly IOptions<VotingSettings> _votingSettings;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

    private User _user = null!;
    private Voting _voting = null!;
    private VoteChoice _voteChoice = null!;
    private VoteChoice _voteChoice2 = null!;

    public VoteServiceTests()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _votingSettings = Options.Create(new VotingSettings
        {
            MasterKey = "test-master-key"
        });

        SeedDatabase();
    }

    [Fact]
    public async Task CastVote_WhenUserIsAdmin_ReturnsUnauthorizedError()
    {
        var admin = new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin@example.com",
            Email = "admin@example.com",
            Name = "admin",
            Role = Role.Admin,
            LoginMode = UserLoginMode.Password
        };

        var service = CreateService(admin.Id, Role.Admin);

        var result = await service.CastVote(admin, _voteChoice);

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task CastVote_WhenUserVotesForOwnVoting_ReturnsUnauthorizedError()
    {
        var owner = new User
        {
            Id = _voting.CreatedByUserId,
            UserName = "owner@example.com",
            Email = "owner@example.com",
            Name = "owner",
            Role = Role.User,
            LoginMode = UserLoginMode.Password
        };
        var service = CreateService(owner.Id);

        var result = await service.CastVote(owner, _voteChoice);

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task CastVote_WhenValidVote_ReturnsSuccess()
    {
        var anotherUser = NextValidUser;
        anotherUser.Id = Guid.NewGuid();
        Context.Users.Add(anotherUser);
        await Context.SaveChangesAsync();

        var service = CreateService(anotherUser.Id);

        var result = await service.CastVote(anotherUser, _voteChoice);

        Assert.True(result.HasValue);
    }

    [Fact]
    public async Task GetVotesForVoting_WhenAdmin_ReturnsAllVotes()
    {
        var votes = new List<AnonymousBallot>
        {
            new()
            {
                VotingId = _voting.VotingId,
                ChoiceId = _voteChoice.ChoiceId,
                VoteTagBase64 = ""
            }
        };

        await Context.AnonymousBallots.AddRangeAsync(votes);
        await Context.SaveChangesAsync();

        var service = CreateService(_user.Id, Role.Admin);

        var result = await service.GetVotesForVoting(_voting);

        Assert.True(result.HasValue);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetVotesForVoting_WhenUserHasVoted_ReturnsUserVotes()
    {
        var ballots = new List<AnonymousBallot>
        {
            new()
            {
                VotingId = _voting.VotingId,
                ChoiceId = _voteChoice.ChoiceId,
                VoteTagBase64 = ""
            }
        };
        var participations = new List<VotingParticipation>
        {
            new()
            {
                UserId = _user.Id,
                VotingId = _voting.VotingId,
                HasVoted = true
            }
        };

        await Context.AnonymousBallots.AddRangeAsync(ballots);
        await Context.VotingParticipations.AddRangeAsync(participations);
        await Context.SaveChangesAsync();

        var service = CreateService(_user.Id);

        var result = await service.GetVotesForVoting(_voting);

        Assert.True(result.HasValue);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetVotesForVoting_WhenUserHasNotVoted_ReturnsUnauthorizedError()
    {
        var service = CreateService(Guid.NewGuid());

        var result = await service.GetVotesForVoting(_voting);

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task GetMyVotes_WhenUserIsAdmin_ReturnsUnauthorizedError()
    {
        var service = CreateService(_user.Id, Role.Admin);

        var result = await service.GetMyVotes();

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task GetMyVotes_WhenUserHasVotes_ReturnsUserVotes()
    {
        var ballots = new List<AnonymousBallot>
        {
            new()
            {
                VotingId = _voting.VotingId,
                ChoiceId = _voteChoice.ChoiceId,
                VoteTagBase64 = ""
            }
        };
        var participations = new List<VotingParticipation>
        {
            new()
            {
                UserId = _user.Id,
                VotingId = _voting.VotingId,
                HasVoted = true
            }
        };

        await Context.AnonymousBallots.AddRangeAsync(ballots);
        await Context.VotingParticipations.AddRangeAsync(participations);
        await Context.SaveChangesAsync();

        var service = CreateService(_user.Id);

        var result = await service.GetMyVotes();

        Assert.True(result.HasValue);
        Assert.Single(result.Value);
    }

    private VoteService CreateService(Guid userId, Role role = Role.User)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(ClaimTypes.Role, role.ToString()),
                new("id", userId.ToString())
            }, "TestAuth"))
        };

        _httpContextAccessor.Setup(h => h.HttpContext).Returns(context);

        return new VoteService(
            Context,
            _httpContextAccessor.Object,
            new NullLogger<VoteService>(),
            _votingSettings);
    }

    private void SeedDatabase()
    {
        _user = NextValidUser;
        Context.Users.Add(_user);

        _voting = GetStartedValidVoting(_user.Id);
        Context.Votings.Add(_voting);

        _voteChoice = new VoteChoice
        {
            ChoiceId = 1,
            VotingId = _voting.VotingId,
            Name = "choice1",
            Voting = _voting,
            VoteCount = 0
        };
        _voteChoice2 = new VoteChoice
        {
            ChoiceId = 2,
            VotingId = _voting.VotingId,
            Name = "choice2",
            Voting = _voting,
            VoteCount = 0
        };
        Context.VoteChoices.AddRange(_voteChoice, _voteChoice2);

        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}