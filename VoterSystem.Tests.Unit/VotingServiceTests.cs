using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit;

public class VotingsServiceTests : UnitTestBase, IDisposable
{
    private readonly VotingService _votingService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<VotingService>> _loggerMock = new();

    private User? _user;
    private User? _user2;

    public VotingsServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _votingService = new VotingService(
            Context,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        SeedDatabase();
        SetCurrentUser(_user!.Id);
    }
 
    #region Add

    [Fact]
    public async Task CreateVoting_ReturnsUnauthorized_ForAdmin()
    {
        SetCurrentUser(Guid.NewGuid(), Role.Admin);
        var request = BuildValidRequest();

        var result = await _votingService.CreateVoting(request);

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task CreateVoting_WhenInvalidTimes_ReturnsBadRequest()
    {
        SetCurrentUser(_user!.Id);
        var request = new VotingCreateRequestDto
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            EndsAt = DateTime.UtcNow.AddDays(1)
        };

        var result = await _votingService.CreateVoting(request);

        Assert.True(result.IsError);
        Assert.IsType<BadRequestError>(result.Error);
    }

    [Fact]
    public async Task CreateVoting_AddsVoting()
    {
        SetCurrentUser(_user!.Id);
        var request = BuildValidRequest();

        var result = await _votingService.CreateVoting(request);
        Assert.True(result.HasValue);

        var votings = await Context.Votings.ToListAsync();
        Assert.Single(votings);
        Assert.Equal(_user!.Id, votings[0].CreatedByUserId);
    }

    #endregion

    #region Get

    [Fact]
    public async Task GetAllVotingsAsync_ReturnsAllVotingsForAdmin()
    {
        var voting = GetNextValidVoting(_user!.Id);
        var voting2 = GetNextValidVoting(_user!.Id);
        Context.Votings.AddRange(voting, voting2);
        await Context.SaveChangesAsync();

        SetCurrentUser(Guid.NewGuid(), Role.Admin);

        var votings = await _votingService.GetAllVotings();
        Assert.True(votings.HasValue);
        Assert.Equal(2, votings.Value.Count);
    }

    [Fact]
    public async Task GetAllVotingsAsync_ReturnsAllVotingsForUser()
    {
        var voting = GetNextValidVoting(_user!.Id);
        var voting2 = GetNextValidVoting(_user2!.Id);
        Context.Votings.AddRange(voting, voting2);
        await Context.SaveChangesAsync();

        SetCurrentUser(_user2!.Id);

        var votings = await _votingService.GetAllVotings();
        Assert.True(votings.HasValue);
        Assert.Equal(2, votings.Value.Count);
        Assert.Contains(votings.Value, v => v.CreatedByUserId == _user2!.Id);
    }

    [Fact]
    public async Task GetVotingByIdAsync_ReturnsAnyForAdmin()
    {
        var voting = GetNextValidVoting(_user!.Id);
        Context.Votings.Add(voting);
        await Context.SaveChangesAsync();

        SetCurrentUser(Guid.NewGuid(), Role.Admin);

        var savedVoting = await _votingService.GetVotingById(voting.VotingId);
        Assert.True(savedVoting.HasValue);
    }

    [Fact]
    public async Task GetVotingByIdAsync_ReturnsForNonAdmin()
    {
        var voting = GetNextValidVoting(_user!.Id);
        Context.Votings.Add(voting);
        await Context.SaveChangesAsync();

        SetCurrentUser(_user2!.Id);

        var savedVoting = await _votingService.GetVotingById(voting.VotingId);
        Assert.True(savedVoting.HasValue);
    }

    [Fact]
    public async Task GetVotingByIdAsync_ReturnsForOwner()
    {
        var voting = GetNextValidVoting(_user!.Id);
        Context.Votings.Add(voting);
        await Context.SaveChangesAsync();

        SetCurrentUser(_user!.Id);

        var savedVoting = await _votingService.GetVotingById(voting.VotingId);
        Assert.True(savedVoting.HasValue);
    }

    #endregion

    #region Helper

    private VotingCreateRequestDto BuildValidRequest()
    {
        return new VotingCreateRequestDto
        {
            Name = Helpers.NextUniqueId,
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddDays(2)
        };
    }

    private void SetCurrentUser(Guid userId, params Role[] roles)
    {
        var context = BuildHttpContext(userId, roles);
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
    }

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
