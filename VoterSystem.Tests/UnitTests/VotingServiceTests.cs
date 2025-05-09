using Microsoft.EntityFrameworkCore;
using Moq;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;

namespace VoterSystem.Tests.UnitTests;

public class VotingsServiceTests : UnitTestBase, IDisposable
{
    private readonly VotingService _votingService;
    private readonly Mock<IUserService> _mockUserService;
    
    private User? _user;
    private User? _user2;

    public VotingsServiceTests()
    {
        _mockUserService = new Mock<IUserService>();

        _votingService = new VotingService(
            Context,
            _mockUserService.Object);

        SeedDatabase();
    }

    #region Add

    [Fact(Skip = "No support for FK validation in InMemory")]
    public async Task CreateVoting_WhenUserNotExists()
    {
        var voting = GetNextValidVoting(Guid.NewGuid());
        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user2!);

        var result = await _votingService.CreateVoting(voting);
        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task CreateVoting_WhenInvalidTimes()
    {
        var voting = GetNextInvalidVoting(_user?.Id ?? throw new InvalidOperationException());
        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user2!);

        var result = await _votingService.CreateVoting(voting);
        Assert.True(result.IsSome);
        Assert.IsType<BadRequestError>(result.AsSome.Value);
    }

    [Fact]
    public async Task CreateVoting_AddsVoting()
    {
        var voting = GetNextValidVoting(_user?.Id ?? throw new InvalidOperationException());
        
        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user!);

        var result = await _votingService.CreateVoting(voting);
        Assert.True(result.IsNone);

        // Assert
        var votings = await Context.Votings.ToListAsync();
        Assert.Single(votings);
    }

    #endregion

    #region Get

    [Fact]
    public async Task GetAllVotingsAsync_ReturnsAllVotingsForAdmin()
    {
        // Arrange
        var voting = GetNextValidVoting(_user?.Id ?? throw new InvalidOperationException());
        var voting2 = GetNextValidVoting(_user?.Id ?? throw new InvalidOperationException());
        Context.Votings.AddRange(voting, voting2);
        
        await Context.SaveChangesAsync();

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(true);

        // Act
        var votings = await _votingService.GetAllVotings();
        Assert.True(votings.HasValue);

        // Assert
        Assert.NotEmpty(votings.Value);
        Assert.Equal(2, votings.Value.Count);
    }

    [Fact]
    public async Task GetAllVotingsAsync_ReturnsOnlyOwnVotings()
    {
        // Arrange
        var voting = GetNextValidVoting(_user?.Id ?? throw new InvalidOperationException());
        var voting2 = GetNextValidVoting(_user2?.Id ?? throw new InvalidOperationException());
        Context.Votings.AddRange(voting, voting2);
        
        await Context.SaveChangesAsync();

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserId()).Returns(_user2.Id);

        // Act
        var votings = await _votingService.GetAllVotings();
        Assert.True(votings.HasValue);

        // Assert
        Assert.NotEmpty(votings.Value);
        Assert.Equal(2, votings.Value.Count);
        Assert.Equal(_user2.Id, votings.Value.First().CreatedByUserId);
    }

    [Fact]
    public async Task GetVotingByIdAsync_ReturnsAnyForAdmin()
    {
        // Arrange
        var voting = GetNextValidVoting(_user?.Id ?? throw new InvalidOperationException());
        Context.Votings.Add(voting);
        await Context.SaveChangesAsync();

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(true);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user);

        // Act
        var savedVoting = await _votingService.GetVotingById(voting.VotingId);
        Assert.True(savedVoting.HasValue);
    }

    [Fact]
    public async Task GetAllVotingByIdAsync_WhenNotAdmin()
    {
        // Arrange
        var voting = GetNextValidVoting(_user?.Id ?? throw new InvalidOperationException());
        Context.Votings.Add(voting);

        await Context.SaveChangesAsync();

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user2!);

        // Act & Assert
        var savedVoting = await _votingService.GetVotingById(voting.VotingId);
        Assert.True(savedVoting.HasValue);
    }

    [Fact]
    public async Task GetAllVotingByIdAsync_ReturnsWhenSameUser()
    {
        // Arrange
        var voting = GetNextValidVoting(_user?.Id ?? throw new InvalidOperationException());
        Context.Votings.Add(voting);

        await Context.SaveChangesAsync();

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user);

        // Act & Assert
        var savedVoting = await _votingService.GetVotingById(voting.VotingId);
        Assert.True(savedVoting.HasValue);
    }

    #endregion

    #region Helper

    private void SeedDatabase()
    {
        _user = NextValidUser;
        _user2 = NextValidUser;
        
        Context.Users.AddRange(_user, _user2);
        Context.SaveChanges();
    }

    #endregion

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}