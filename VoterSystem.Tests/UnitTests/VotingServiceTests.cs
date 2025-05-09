using Microsoft.EntityFrameworkCore;
using Moq;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;

namespace VoterSystem.Tests.UnitTests;

public class VotingsServiceTests : IDisposable
{
    private readonly VoterSystemDbContext _context;
    private readonly VotingService _votingService;
    private readonly Mock<IUserService> _mockUserService;
    
    private User? _user;
    private User? _user2;

    public VotingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<VoterSystemDbContext>()
            .UseInMemoryDatabase("TestVotingDatabase")
            .Options;

        _context = new VoterSystemDbContext(options);

        _mockUserService = new Mock<IUserService>();

        _votingService = new VotingService(
            _context,
            _mockUserService.Object);

        SeedDatabase();
    }

    #region Add

    [Fact]
    public async Task CreateVoting_WhenUserNotExists()
    {
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = Guid.NewGuid(),
        };

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user2!);

        var result = await _votingService.CreateVoting(voting);
        Assert.True(result.IsSome);
        Assert.IsType<BadRequestError>(result.AsSome.Value);
    }

    [Fact]
    public async Task CreateVoting_WhenInvalidTimes()
    {
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(0),
            EndsAt = DateTime.UtcNow.AddHours(0),
            CreatedByUserId = Guid.NewGuid(),
        };

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user2!);

        var result = await _votingService.CreateVoting(voting);
        Assert.True(result.IsSome);
        Assert.IsType<BadRequestError>(result.AsSome.Value);
    }

    [Fact]
    public async Task CreateVoting_AddsVoting()
    {
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddDays(2),
            CreatedByUserId = _user?.Id ?? throw new InvalidOperationException()
        };

        _mockUserService.Setup(x => x.IsCurrentUserAdmin()).Returns(false);
        _mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(_user!);

        var result = await _votingService.CreateVoting(voting);
        Assert.True(result.IsNone);

        // Assert
        var votings = await _context.Votings.ToListAsync();
        Assert.Single(votings);
    }

    #endregion

    #region Get

    [Fact]
    public async Task GetAllVotingsAsync_ReturnsAllVotingsForAdmin()
    {
        // Arrange
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = _user?.Id ?? throw new InvalidOperationException(),
        };

        var voting2 = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = _user?.Id ?? throw new InvalidOperationException(),
        };

        _context.Votings.AddRange(voting, voting2);
        
        await _context.SaveChangesAsync();

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
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = _user?.Id ?? throw new InvalidOperationException(),
        };

        var voting2 = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = _user2?.Id ?? throw new InvalidOperationException(),
        };

        _context.Votings.AddRange(voting, voting2);
        
        await _context.SaveChangesAsync();

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
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = _user?.Id ?? throw new InvalidOperationException(),
        };

        _context.Votings.Add(voting);
        await _context.SaveChangesAsync();

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
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = _user?.Id ?? throw new InvalidOperationException(),
        };

        _context.Votings.Add(voting);

        await _context.SaveChangesAsync();

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
        var voting = new Voting
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = _user?.Id ?? throw new InvalidOperationException(),
        };

        _context.Votings.Add(voting);

        await _context.SaveChangesAsync();

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
        _user = new User
        {
            Email = "test@test.com",
            UserName = "test",
            Name = "test"
        };

        _user2 = new User
        {
            Email = "test2@test.com",
            UserName = "test2",
            Name = "test2"
        };
        
        _context.Users.AddRange(_user, _user2);
        _context.SaveChanges();
        
        /*var voting = new Voting
        {
            CreatedAt = DateTime.UtcNow,
            StartsAt = DateTime.Now.AddDays(1),
            Name = "test",
            EndsAt = DateTime.Now.AddDays(2),
            CreatedByUserId = _user.Id,
        };

        _context.Votings.Add(voting);
        _context.SaveChanges();*/
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}