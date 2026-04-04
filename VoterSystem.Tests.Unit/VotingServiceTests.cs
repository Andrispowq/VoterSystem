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

public class VotingsServiceTests : UnitTestBase, IDisposable
{
    private readonly User _user;
    private readonly User _user2;
    
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

    public VotingsServiceTests()
    {
        _user = NextValidUser;
        _user2 = NextValidUser;
        _httpContextAccessor = new Mock<IHttpContextAccessor>();

        Context.Users.AddRange(_user, _user2);
        Context.SaveChanges();
    }

    [Fact]
    public async Task CreateVoting_WhenInvalidTimes_ReturnsBadRequest()
    {
        var service = CreateService(_user.Id);
        var request = ToCreateRequest(GetStartedValidVoting(_user.Id));

        var result = await service.CreateVoting(request);

        Assert.True(result.IsError);
        Assert.IsType<BadRequestError>(result.Error);
    }

    [Fact]
    public async Task CreateVoting_AddsVoting()
    {
        var service = CreateService(_user.Id);
        var request = ToCreateRequest(GetUnstartedValidVoting(_user.Id));

        var result = await service.CreateVoting(request);

        Assert.True(result.HasValue);

        var votings = await Context.Votings.ToListAsync();
        Assert.Single(votings);
        Assert.Equal(_user.Id, votings[0].CreatedByUserId);
    }

    [Fact]
    public async Task GetAllVotingsAsync_ReturnsAllVotingsForAdmin()
    {
        var voting = GetUnstartedValidVoting(_user.Id);
        var voting2 = GetUnstartedValidVoting(_user2.Id);
        Context.Votings.AddRange(voting, voting2);
        await Context.SaveChangesAsync();

        var service = CreateService(_user.Id, Role.Admin);

        var votings = await service.GetAllVotings();

        Assert.True(votings.HasValue);

        // Assert
        Assert.NotEmpty(votings.Value);
        Assert.Equal(2, votings.Value.Count);
    }

    [Fact]
    public async Task GetAllVotingsAsync_ReturnsAllVotingsForUser()
    {
        var voting = GetUnstartedValidVoting(_user.Id);
        var voting2 = GetUnstartedValidVoting(_user2.Id);
        Context.Votings.AddRange(voting, voting2);
        await Context.SaveChangesAsync();

        var service = CreateService(_user2.Id);

        var votings = await service.GetAllVotings();

        Assert.True(votings.HasValue);
        Assert.Single(votings.Value);
        Assert.Equal(voting2.VotingId, votings.Value[0].VotingId);
    }

    [Fact]
    public async Task GetVotingByIdAsync_ReturnsAnyForAdmin()
    {
        var voting = GetUnstartedValidVoting(_user.Id);
        Context.Votings.Add(voting);
        await Context.SaveChangesAsync();

        var service = CreateService(_user2.Id, Role.Admin);

        var savedVoting = await service.GetVotingById(voting.VotingId);

        Assert.True(savedVoting.HasValue);
    }

    [Fact]
    public async Task GetVotingByIdAsync_ReturnsVotingForOtherUser()
    {
        var voting = GetUnstartedValidVoting(_user.Id);
        Context.Votings.Add(voting);
        await Context.SaveChangesAsync();

        var service = CreateService(_user2.Id);

        var savedVoting = await service.GetVotingById(voting.VotingId);

        Assert.True(savedVoting.HasValue);
    }

    [Fact]
    public async Task GetVotingByIdAsync_ReturnsVotingForOwner()
    {
        var voting = GetUnstartedValidVoting(_user.Id);
        Context.Votings.Add(voting);
        await Context.SaveChangesAsync();

        var service = CreateService(_user.Id);

        var savedVoting = await service.GetVotingById(voting.VotingId);

        Assert.True(savedVoting.HasValue);
    }

    private VotingService CreateService(Guid userId, Role role = Role.User)
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
        
        return new VotingService(
            Context,
            _httpContextAccessor.Object,
            new NullLogger<VotingService>());
    }

    private static VotingCreateRequestDto ToCreateRequest(Voting voting)
    {
        return new VotingCreateRequestDto
        {
            Name = voting.Name,
            StartsAt = voting.StartsAt,
            EndsAt = voting.EndsAt
        };
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}