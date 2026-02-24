using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit;

public class VoteServiceTests : UnitTestBase, IDisposable
{
    private readonly VoteService _voteService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<VoteService>> _loggerMock = new();
    private readonly IOptions<VotingSettings> _votingSettings;
    
    private User _user = null!;
    private Voting _voting = null!;
    private VoteChoice _voteChoice = null!;
    
    public VoteServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _votingSettings = new OptionsWrapper<VotingSettings>(new VotingSettings());
        _voteService = new VoteService(
            Context,
            _httpContextAccessorMock.Object,
            _loggerMock.Object,
            _votingSettings);

        SeedDatabase();
    }

    #region Cast Vote

    [Fact]
    public async Task CastVote_WhenUserIsAdmin_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin@example.com",
            Name = "Admin",
            Role = Role.Admin
        };
        SetCurrentUser(user.Id, Role.Admin);
        
        // Act
        var result = await _voteService.CastVote(user, _voteChoice);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task CastVote_WhenUserVotesForOwnVoting_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = new User
        {
            Id = _voting.CreatedByUserId,
            UserName = "owner@example.com",
            Name = "owner",
            Role = Role.User
        };
        SetCurrentUser(user.Id, Role.User);

        // Act
        var result = await _voteService.CastVote(user, _voteChoice);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task CastVote_WhenValidVote_ReturnsSuccess()
    {
        // Arrange
        var anotherUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "anotherUser@example.com",
            Name = "anotherUser",
            Role = Role.User
        };
        SetCurrentUser(anotherUser.Id, Role.User);

        // Act
        var result = await _voteService.CastVote(anotherUser, _voteChoice);

        // Assert
        Assert.True(result.HasValue); // Vote is cast successfully, no error
    }

    #endregion

    #region Get Votes

    [Fact]
    public async Task GetVotesForVoting_WhenAdmin_ReturnsAllVotes()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid(), Role.Admin);
        var votes = new List<AnonymousBallot>
        {
            new()
            {
                VotingId = _voting.VotingId,
                ChoiceId = 1,
                VoteTagBase64 = ""
            }
        };
        
        await Context.AnonymousBallots.AddRangeAsync(votes);
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
                UserId = userId,
                VotingId = _voting.VotingId,
                HasVoted = true
            }
        };
        
        await Context.AnonymousBallots.AddRangeAsync(ballots);
        await Context.VotingParticipations.AddRangeAsync(participations);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId, Role.User);

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
        SetCurrentUser(userId, Role.User);

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
        SetCurrentUser(Guid.NewGuid(), Role.Admin);

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
                UserId = userId,
                VotingId = _voting.VotingId,
                HasVoted = true
            }
        };
        
        await Context.AnonymousBallots.AddRangeAsync(ballots);
        await Context.VotingParticipations.AddRangeAsync(participations);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId, Role.User);

        // Act
        var result = await _voteService.GetMyVotes();

        // Assert
        Assert.True(result.HasValue);
        Assert.Single(result.Value);
    }

    #endregion

    #region Helper Methods

    private void SetCurrentUser(Guid userId, params Role[] roles)
    {
        var context = BuildHttpContext(userId, roles);
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
    }

    private void SeedDatabase()
    {
        _user = NextValidUser;
        Context.Users.Add(_user);
        
        _voting = GetNextValidVoting(_user.Id);
        Context.Votings.Add(_voting);
        
        _voteChoice = new VoteChoice
        {
            ChoiceId = 1,
            VotingId = _voting.VotingId,
            Name = "choice1",
            Voting = _voting
        };
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
