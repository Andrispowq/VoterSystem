using Moq;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;

namespace VoterSystem.Tests.UnitTests;

public class VoteServiceTests : UnitTestBase, IDisposable
{
    private readonly VoteService _voteService;
    private readonly Mock<IUserService> _mockUserService;
    
    private User _user = null!;
    private Voting _voting = null!;
    private VoteChoice _voteChoice = null!;
    
    public VoteServiceTests()
    {
        _mockUserService = new Mock<IUserService>();
        _voteService = new VoteService(
            Context,
            _mockUserService.Object);

        SeedDatabase();
    }

    #region Cast Vote

    [Fact]
    public async Task CastVote_WhenUserIsAdmin_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), UserName = "admin@example.com" }; // Simulate an admin user
        _mockUserService.Setup(x => x.GetCurrentUserId()).Returns(user.Id);
        _mockUserService.Setup(x => x.GetUserRoleByIdAsync(user.Id)).ReturnsAsync(Role.Admin);
        
        // Act
        var result = await _voteService.CastVote(user, _voteChoice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task CastVote_WhenUserVotesForOwnVoting_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = new User
        {
            Id = _voting.CreatedByUserId, UserName = "owner@example.com"
        }; // Simulate the owner of the voting
        
        _mockUserService.Setup(x => x.GetCurrentUserId()).Returns(user.Id);
        _mockUserService.Setup(x => x.GetUserRoleByIdAsync(user.Id)).ReturnsAsync(Role.User);

        // Act
        var result = await _voteService.CastVote(user, _voteChoice);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task CastVote_WhenValidVote_ReturnsSuccess()
    {
        // Arrange
        var anotherUser = new User { Id = Guid.NewGuid(), UserName = "anotherUser@example.com", Name = "anotherUser" };
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(anotherUser);
        
        _mockUserService.Setup(x => x.GetCurrentUserId()).Returns(anotherUser.Id);
        _mockUserService.Setup(x => x.GetUserRoleByIdAsync(anotherUser.Id)).ReturnsAsync(Role.User);

        // Act
        var result = await _voteService.CastVote(anotherUser, _voteChoice);

        // Assert
        Assert.True(result.IsNone); // Vote is cast successfully, no error
    }

    #endregion

    #region Get Votes

    [Fact]
    public async Task GetVotesForVoting_WhenAdmin_ReturnsAllVotes()
    {
        // Arrange
        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(true);
        var votes = new List<Vote>
        {
            new()
            {
                VotingId = _voting.VotingId, 
                ChoiceId = 1,
                UserId = Guid.NewGuid()
            }
        };
        
        await Context.Votes.AddRangeAsync(votes);
        await Context.SaveChangesAsync();

        // Act
        var result = await _voteService.GetVotesForVoting(_voting);

        // Assert
        Assert.True(result.HasValue);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetVotesForVoting_WhenUserHasVoted_ReturnsUserVotes()
    {
        // Arrange
        var userId = _user.Id;
        var votes = new List<Vote>
        {
            new()
            {
                VotingId = _voting.VotingId, 
                UserId = userId, 
                ChoiceId = _voteChoice.ChoiceId,
            }
        };
        
        await Context.Votes.AddRangeAsync(votes);
        await Context.SaveChangesAsync();

        _mockUserService.Setup(x => x.GetCurrentUserId()).Returns(userId);

        // Act
        var result = await _voteService.GetVotesForVoting(_voting);

        // Assert
        Assert.True(result.HasValue);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetVotesForVoting_WhenUserHasNotVoted_ReturnsUnauthorizedError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserService.Setup(x => x.GetCurrentUserId()).Returns(userId);

        // Act
        var result = await _voteService.GetVotesForVoting(_voting);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    #endregion

    #region Get My Votes

    [Fact]
    public async Task GetMyVotes_WhenUserIsAdmin_ReturnsUnauthorizedError()
    {
        // Arrange
        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(true);

        // Act
        var result = await _voteService.GetMyVotes();

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task GetMyVotes_WhenUserHasVotes_ReturnsUserVotes()
    {
        // Arrange
        var userId = _user.Id;
        var votes = new List<Vote>
        {
            new()
            {
                UserId = userId, 
                VotingId = _voting.VotingId, 
                ChoiceId = _voteChoice.ChoiceId
            }
        };
        
        await Context.Votes.AddRangeAsync(votes);
        await Context.SaveChangesAsync();

        _mockUserService.Setup(x => x.GetCurrentUserId()).Returns(userId);

        // Act
        var result = await _voteService.GetMyVotes();

        // Assert
        Assert.True(result.HasValue);
        Assert.Single(result.Value);
    }

    #endregion

    #region Helper Methods

    private void SeedDatabase()
    {
        _user = NextValidUser;
        Context.Users.Add(_user);
        
        _voting = GetNextValidVoting(_user.Id);
        Context.Votings.Add(_voting);
        
        _voteChoice = new VoteChoice
            { ChoiceId = 1, VotingId = _voting.VotingId, Name = "choice1" };
        Context.VoteChoices.Add(_voteChoice);
        
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }

    #endregion
}