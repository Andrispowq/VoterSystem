using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit;

public class VoteChoiceServiceTests : UnitTestBase, IDisposable
{
    private User _user = null!;
    private User _otherUser = null!;
    private Voting _voting = null!;
    private Voting _startedVoting = null!;
    private VoteChoice _voteChoice = null!;
    
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

    public VoteChoiceServiceTests()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        
        SeedDatabase();
    }

    [Fact]
    public async Task GetVoteChoices_ReturnsChoicesForVoting()
    {
        var service = CreateService(_user.Id);

        var choices = await service.GetVoteChoices(_voting);

        Assert.NotNull(choices);
        Assert.Single(choices);
        Assert.Equal(_voteChoice.ChoiceId, choices.First().ChoiceId);
    }

    [Fact]
    public async Task GetChoiceById_ReturnsChoice_WhenExists()
    {
        var service = CreateService(_user.Id);

        var result = await service.GetChoiceById(_voteChoice.ChoiceId);

        Assert.True(result.HasValue);
        Assert.Equal(_voteChoice.ChoiceId, result.Value.ChoiceId);
    }

    [Fact]
    public async Task GetChoiceById_ReturnsNotFound_WhenNotExists()
    {
        var service = CreateService(_user.Id);

        var result = await service.GetChoiceById(-1);

        Assert.True(result.IsError);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task AddVotingChoice_ReturnsUnauthorized_WhenVotingStarted()
    {
        var service = CreateService(_startedVoting.CreatedByUserId);
        var newChoice = new VoteChoice
        {
            Name = "New Choice",
            VotingId = _startedVoting.VotingId
        };

        var result = await service.AddVotingChoice(_startedVoting, newChoice);

        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task AddVotingChoice_ReturnsUnauthorized_WhenUserNotCreator()
    {
        var service = CreateService(_otherUser.Id);
        var newChoice = new VoteChoice
        {
            Name = "New Choice",
            VotingId = _voting.VotingId
        };

        var result = await service.AddVotingChoice(_voting, newChoice);

        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task AddVotingChoice_AddsChoice_WhenAuthorized()
    {
        var service = CreateService(_user.Id);
        var newChoice = new VoteChoice
        {
            Name = "New Choice",
            VotingId = _voting.VotingId
        };

        var result = await service.AddVotingChoice(_voting, newChoice);

        Assert.True(result.IsNone);

        var choices = await Context.VoteChoices.Where(c => c.VotingId == _voting.VotingId).ToListAsync();
        Assert.Contains(choices, c => c.Name == "New Choice");
    }

    [Fact]
    public async Task UpdateVotingChoice_ReturnsUnauthorized_WhenVotingStarted()
    {
        var service = CreateService(_user.Id);
        var choice = new VoteChoice
        {
            ChoiceId = _voteChoice.ChoiceId,
            VotingId = _startedVoting.VotingId,
            Voting = _startedVoting,
            Name = "Updated Choice"
        };

        var result = await service.UpdateVotingChoice(choice);

        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task UpdateVotingChoice_ReturnsNotFound_WhenChoiceMissing()
    {
        var service = CreateService(_user.Id);
        var choice = new VoteChoice
        {
            ChoiceId = -1,
            VotingId = _voting.VotingId,
            Voting = _voting,
            Name = "Nonexistent Choice"
        };

        var result = await service.UpdateVotingChoice(choice);

        Assert.True(result.IsSome);
        Assert.IsType<NotFoundError>(result.AsSome.Value);
    }

    [Fact]
    public async Task UpdateVotingChoice_UpdatesChoice_WhenValid()
    {
        var service = CreateService(_user.Id);

        _voteChoice.Name = "Updated Choice Name";
        _voteChoice.Voting = _voting;
        var result = await service.UpdateVotingChoice(_voteChoice);

        Assert.True(result.IsNone);

        var savedChoice = await Context.VoteChoices.FindAsync(_voteChoice.ChoiceId);
        Assert.Equal("Updated Choice Name", savedChoice!.Name);
    }

    [Fact]
    public async Task DeleteVotingChoice_ReturnsUnauthorized_WhenVotingStarted()
    {
        var service = CreateService(_user.Id);

        var result = await service.DeleteVotingChoice(
            new VoteChoice
            {
                ChoiceId = _voteChoice.ChoiceId,
                VotingId = _startedVoting.VotingId,
                Voting = _startedVoting,
                Name = "masodik"
            });

        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task DeleteVotingChoice_ReturnsNotFound_WhenChoiceMissing()
    {
        var service = CreateService(_user.Id);
        var missingChoice = new VoteChoice
        {
            ChoiceId = -1,
            VotingId = _voting.VotingId,
            Voting = _voting,
            Name = "elso valasztas"
        };

        var result = await service.DeleteVotingChoice(missingChoice);

        Assert.True(result.IsSome);
        Assert.IsType<NotFoundError>(result.AsSome.Value);
    }

    [Fact]
    public async Task DeleteVotingChoice_DeletesChoice_WhenValid()
    {
        var service = CreateService(_voting.CreatedByUserId);
        _voteChoice.Voting = _voting;

        var result = await service.DeleteVotingChoice(_voteChoice);

        Assert.True(result.IsNone);

        var deletedChoice = await Context.VoteChoices.FindAsync(_voteChoice.ChoiceId);
        Assert.Null(deletedChoice);
    }

    private VoteChoiceService CreateService(Guid userId, Role role = Role.User)
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

        return new VoteChoiceService(
            Context,
            _httpContextAccessor.Object,
            new NullLogger<VoteChoiceService>());
    }

    private void SeedDatabase()
    {
        _user = NextValidUser;
        _user.Id = Guid.NewGuid();

        _otherUser = NextValidUser;
        _otherUser.Id = Guid.NewGuid();

        Context.Users.AddRange(_user, _otherUser);

        _voting = GetNextValidVoting(_user.Id);
        _voting.StartsAt = DateTime.UtcNow.AddHours(1);
        _voting.EndsAt = DateTime.UtcNow.AddDays(1);

        _startedVoting = GetNextValidVoting(_user.Id);
        _startedVoting.StartsAt = DateTime.UtcNow.AddHours(-1);
        _startedVoting.EndsAt = DateTime.UtcNow.AddHours(1);

        Context.Votings.AddRange(_voting, _startedVoting);

        _voteChoice = new VoteChoice
        {
            VotingId = _voting.VotingId,
            Voting = _voting,
            Name = "choice1"
        };

        var startedChoice1 = new VoteChoice
        {
            VotingId = _startedVoting.VotingId,
            Voting = _startedVoting,
            Name = "choice1"
        };

        var startedChoice2 = new VoteChoice
        {
            VotingId = _startedVoting.VotingId,
            Voting = _startedVoting,
            Name = "choice2"
        };

        Context.VoteChoices.AddRange(_voteChoice, startedChoice1, startedChoice2);

        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
