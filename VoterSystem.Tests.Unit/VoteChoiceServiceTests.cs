using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit;

public class VoteChoiceServiceTests : UnitTestBase, IDisposable
{
    private readonly VoteChoiceService _voteChoiceService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<VoteChoiceService>> _loggerMock = new();

    private User _user = null!;
    private User _otherUser = null!;
    private Voting _voting = null!;
    private Voting _startedVoting = null!;
    private VoteChoice _voteChoice = null!;
    private VoteChoice _startedChoice1 = null!;

    public VoteChoiceServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _voteChoiceService = new VoteChoiceService(
            Context,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        SeedDatabase();
        SetCurrentUser(_user.Id);
    }

    [Fact]
    public async Task GetVoteChoices_ReturnsChoicesForVoting()
    {
        // Act
        var choices = await _voteChoiceService.GetVoteChoices(_voting);

        // Assert
        Assert.NotNull(choices);
        Assert.Single(choices);
        Assert.Equal(_voteChoice.ChoiceId, choices.First().ChoiceId);
    }

    [Fact]
    public async Task GetChoiceById_ReturnsChoice_WhenExists()
    {
        // Act
        var result = await _voteChoiceService.GetChoiceById(_voteChoice.ChoiceId);

        // Assert
        Assert.True(result.HasValue);
        Assert.Equal(_voteChoice.ChoiceId, result.Value.ChoiceId);
    }

    [Fact]
    public async Task GetChoiceById_ReturnsNotFound_WhenNotExists()
    {
        // Act
        var result = await _voteChoiceService.GetChoiceById(-1);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task AddVotingChoice_ReturnsUnauthorized_WhenVotingStarted()
    {
        var newChoice = new VoteChoice
        { 
            Name = "New Choice",
            VotingId = _startedVoting.VotingId 
        };
        
        SetCurrentUser(_startedVoting.CreatedByUserId);

        // Act
        var result = await _voteChoiceService.AddVotingChoice(_startedVoting, newChoice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task AddVotingChoice_ReturnsUnauthorized_WhenUserNotCreator()
    {
        var newChoice = new VoteChoice
        {
            Name = "New Choice",
            VotingId = _voting.VotingId
        };

        SetCurrentUser(_otherUser.Id);

        // Act
        var result = await _voteChoiceService.AddVotingChoice(_voting, newChoice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task AddVotingChoice_AddsChoice_WhenAuthorized()
    {
        var newChoice = new VoteChoice
        {
            Name = "New Choice",
            VotingId = _voting.VotingId
        };

        SetCurrentUser(_user.Id);

        // Act
        var result = await _voteChoiceService.AddVotingChoice(_voting, newChoice);

        // Assert
        Assert.True(result.IsNone);

        var choices = await Context.VoteChoices.Where(c => c.VotingId == _voting.VotingId).ToListAsync();
        Assert.Contains(choices, c => c.Name == "New Choice");
    }

    [Fact]
    public async Task UpdateVotingChoice_ReturnsUnauthorized_WhenVotingStarted()
    {
        var choice = new VoteChoice
        {
            ChoiceId = _voteChoice.ChoiceId,
            VotingId = _startedVoting.VotingId,
            Voting = _startedVoting,
            Name = "Updated Choice"
        };

        // Act
        var result = await _voteChoiceService.UpdateVotingChoice(choice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task UpdateVotingChoice_ReturnsNotFound_WhenChoiceMissing()
    {
        var choice = new VoteChoice
        {
            ChoiceId = -1,
            VotingId = _voting.VotingId,
            Voting = _voting,
            Name = "Nonexistent Choice"
        };

        SetCurrentUser(_user.Id);

        // Act
        var result = await _voteChoiceService.UpdateVotingChoice(choice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<NotFoundError>(result.AsSome.Value);
    }

    [Fact]
    public async Task UpdateVotingChoice_UpdatesChoice_WhenValid()
    {
        SetCurrentUser(_user.Id);

        // Act
        _voteChoice.Name = "Updated Choice Name";
        var result = await _voteChoiceService.UpdateVotingChoice(_voteChoice);

        // Assert
        Assert.True(result.IsNone);

        var savedChoice = await Context.VoteChoices.FindAsync(_voteChoice.ChoiceId);
        Assert.Equal("Updated Choice Name", savedChoice!.Name);
    }

    [Fact]
    public async Task DeleteVotingChoice_ReturnsUnauthorized_WhenVotingStarted()
    {
        // Act
        SetCurrentUser(_user.Id);
        
        var result = await _voteChoiceService.DeleteVotingChoice(
            new VoteChoice
            {
                ChoiceId = _voteChoice.ChoiceId, 
                VotingId = _startedVoting.VotingId,
                Voting = _startedVoting,
                Name = "masodik"
            });

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task DeleteVotingChoice_ReturnsNotFound_WhenChoiceMissing()
    {
        var missingChoice = new VoteChoice
        {
            ChoiceId = -1,
            VotingId = _voting.VotingId,
            Voting = _voting,
            Name = "elso valasztas"
        };

        // Act
        var result = await _voteChoiceService.DeleteVotingChoice(missingChoice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<NotFoundError>(result.AsSome.Value);
    }

    [Fact]
    public async Task DeleteVotingChoice_DeletesChoice_WhenValid()
    {
        SetCurrentUser(_voting.CreatedByUserId);

        // Act
        var result = await _voteChoiceService.DeleteVotingChoice(_voteChoice);

        // Assert
        Assert.True(result.IsNone);

        var deletedChoice = await Context.VoteChoices.FindAsync(_voteChoice.ChoiceId);
        Assert.Null(deletedChoice);
    }

    private void SetCurrentUser(Guid userId, params Role[] roles)
    {
        var context = BuildHttpContext(userId, roles);
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
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
            Name = "choice1",
            Voting = _voting
        };

        _startedChoice1 = new VoteChoice
        {
            VotingId = _startedVoting.VotingId,
            Name = "choice1",
            Voting = _startedVoting
        };

        var startedChoice2 = new VoteChoice
        {
            VotingId = _startedVoting.VotingId,
            Name = "choice2",
            Voting = _startedVoting
        };
        
        Context.VoteChoices.AddRange(_voteChoice, _startedChoice1, startedChoice2);

        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
