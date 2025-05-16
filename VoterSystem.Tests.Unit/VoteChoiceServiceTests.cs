using Microsoft.EntityFrameworkCore;
using Moq;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;

namespace VoterSystem.Tests.Unit;

public class VoteChoiceServiceTests : UnitTestBase, IDisposable
{
    private readonly VoteChoiceService _voteChoiceService;
    private readonly Mock<IUserService> _mockUserService;

    private User _user = null!;
    private User _otherUser = null!;
    private Voting _voting = null!;
    private Voting _startedVoting = null!;
    private VoteChoice _voteChoice = null!;
    private VoteChoice _startedChoice1 = null!;

    public VoteChoiceServiceTests()
    {
        _mockUserService = new Mock<IUserService>();
        _voteChoiceService = new VoteChoiceService(Context, _mockUserService.Object);

        SeedDatabase();
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
        
        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(_startedVoting.CreatedByUserId);

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

        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(new UnauthorizedError("Access denied").AsError<Guid, ServiceError>());
        var unauthorizedUserId = Guid.NewGuid();
        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(unauthorizedUserId.AsResult<Guid, ServiceError>());
        // The voting creator is _user, but userId will be something else

        // To simulate different user
        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(Guid.NewGuid().AsResult<Guid, ServiceError>());

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

        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(_user.Id.AsResult<Guid, ServiceError>());

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

        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(_user.Id.AsResult<Guid, ServiceError>());

        // Act
        var result = await _voteChoiceService.UpdateVotingChoice(choice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<NotFoundError>(result.AsSome.Value);
    }

    [Fact]
    public async Task UpdateVotingChoice_UpdatesChoice_WhenValid()
    {
        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(_user.Id.AsResult<Guid, ServiceError>());

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
        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(_user.Id.AsResult<Guid, ServiceError>());
        
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
        _mockUserService.Setup(s => s.GetCurrentUserId()).Returns(_voting.CreatedByUserId);

        // Act
        var result = await _voteChoiceService.DeleteVotingChoice(_voteChoice);

        // Assert
        Assert.True(result.IsNone);

        var deletedChoice = await Context.VoteChoices.FindAsync(_voteChoice.ChoiceId);
        Assert.Null(deletedChoice);
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
            Name = "choice1"
        };

        _startedChoice1 = new VoteChoice
        {
            VotingId = _startedVoting.VotingId,
            Name = "choice1"
        };

        var startedChoice2 = new VoteChoice
        {
            VotingId = _startedVoting.VotingId,
            Name = "choice2"
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
