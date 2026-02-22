using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit;

public sealed class GroupServiceTests : UnitTestBase
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ILogger<GroupService>> _logger = new();
    private readonly GroupService _service;

    private User _admin = null!;
    private User _member = null!;

    public GroupServiceTests()
    {
        _service = new GroupService(Context, _httpContextAccessor.Object, _logger.Object);
        SeedUsers();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsGroupsCurrentUserBelongsTo()
    {
        var groupWithMembership = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(groupWithMembership.GroupId, _member.Id, _admin.Id);

        var otherGroup = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(otherGroup.GroupId, _admin.Id, _admin.Id);

        SetCurrentUser(_member.Id, Role.User);

        var groups = await _service.GetAllAsync();

        Assert.Single(groups);
        Assert.Equal(groupWithMembership.GroupId, groups.Single().GroupId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsGroup_WhenUserIsMember()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _member.Id, _admin.Id);
        SetCurrentUser(_member.Id, Role.User);

        var result = await _service.GetByIdAsync(group.GroupId);

        Assert.True(result.HasValue);
        Assert.Equal(group.GroupId, result.Value.GroupId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUnauthorized_WhenUserNotInGroup()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _admin.Id, _admin.Id);
        SetCurrentUser(_member.Id, Role.User);

        var result = await _service.GetByIdAsync(group.GroupId);

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task CreateGroupAsync_AsAdmin_AddsGroupAndCreatorMembership()
    {
        SetCurrentUser(_admin.Id, Role.Admin);

        var request = new CreateGroupRequest
        {
            Name = "Engineering",
            Description = "Eng team"
        };

        var result = await _service.CreateGroupAsync(request);

        Assert.True(result.HasValue);
        var createdGroup = await Context.Groups.FindAsync(result.Value.GroupId);
        Assert.NotNull(createdGroup);

        var membership = await Context.GroupMembers.FindAsync(createdGroup.GroupId, _admin.Id);
        Assert.NotNull(membership);
    }

    [Fact]
    public async Task CreateGroupAsync_AsUser_ReturnsUnauthorized()
    {
        SetCurrentUser(_member.Id, Role.User);

        var request = new CreateGroupRequest
        {
            Name = "Finance",
            Description = "Finance team"
        };

        var result = await _service.CreateGroupAsync(request);

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task DeleteGroupAsync_RemovesGroupAndMemberships()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _member.Id, _admin.Id);
        SetCurrentUser(_admin.Id, Role.Admin);

        var result = await _service.DeleteGroupAsync(group.GroupId);

        Assert.True(result.IsNone);
        Assert.False(Context.Groups.Any(g => g.GroupId == group.GroupId));
        Assert.False(Context.GroupMembers.Any(m => m.GroupId == group.GroupId));
    }

    [Fact]
    public async Task DeleteGroupAsync_AsUser_ReturnsUnauthorized()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _admin.Id, _admin.Id);
        SetCurrentUser(_member.Id, Role.User);

        var result = await _service.DeleteGroupAsync(group.GroupId);

        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    [Fact]
    public async Task AddToGroupAsync_AddsMembershipAndGrantsAccess()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _admin.Id, _admin.Id);
        SetCurrentUser(_admin.Id, Role.Admin);

        var result = await _service.AddToGroupAsync(group.GroupId, _member.Id);

        Assert.True(result.IsNone);

        var membership = await Context.GroupMembers.FindAsync(group.GroupId, _member.Id);
        Assert.NotNull(membership);

        SetCurrentUser(_member.Id, Role.User);
        var access = await _service.GetByIdAsync(group.GroupId);
        Assert.True(access.HasValue);
    }

    [Fact]
    public async Task AddToGroupAsync_DoesNotCreateDuplicateMemberships()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _admin.Id, _admin.Id);
        SetCurrentUser(_admin.Id, Role.Admin);

        await _service.AddToGroupAsync(group.GroupId, _member.Id);
        var duplicateAttempt = await _service.AddToGroupAsync(group.GroupId, _member.Id);

        Assert.True(duplicateAttempt.IsSome);
        Assert.IsType<BadRequestError>(duplicateAttempt.AsSome.Value);
        var memberships = Context.GroupMembers.Where(m => m.GroupId == group.GroupId && m.UserId == _member.Id).ToList();
        Assert.Single(memberships);
    }

    [Fact]
    public async Task RemoveFromGroupAsync_RemovesMembershipAndRevokesAccess()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _member.Id, _admin.Id);
        SetCurrentUser(_admin.Id, Role.Admin);

        var result = await _service.RemoveFromGroupAsync(group.GroupId, _member.Id);

        Assert.True(result.IsNone);
        Assert.False(Context.GroupMembers.Any(m => m.GroupId == group.GroupId && m.UserId == _member.Id));

        SetCurrentUser(_member.Id, Role.User);
        var access = await _service.GetByIdAsync(group.GroupId);
        Assert.True(access.IsError);
        Assert.IsType<UnauthorizedError>(access.Error);
    }

    [Fact]
    public async Task RemoveFromGroupAsync_AsUser_ReturnsUnauthorized()
    {
        var group = await CreateGroupAsync(_admin.Id);
        await AddMemberAsync(group.GroupId, _member.Id, _admin.Id);
        SetCurrentUser(_member.Id, Role.User);

        var result = await _service.RemoveFromGroupAsync(group.GroupId, _member.Id);

        Assert.True(result.IsSome);
        Assert.IsType<UnauthorizedError>(result.AsSome.Value);
    }

    private void SeedUsers()
    {
        _admin = NextValidUser;
        _admin.Role = Role.Admin;
        _member = NextValidUser;

        Context.Users.AddRange(_admin, _member);
        Context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private async Task<Group> CreateGroupAsync(Guid creatorId)
    {
        var group = new Group
        {
            CreatorUserId = creatorId,
            Name = $"Group-{Guid.NewGuid():N}",
            Description = "Description"
        };

        await Context.Groups.AddAsync(group);
        var result = await Context.SaveChangesAsync();
        Assert.True(result.IsNone, result.ToString());
        return group;
    }

    private async Task AddMemberAsync(Guid groupId, Guid userId, Guid addedBy)
    {
        var member = new GroupMembers
        {
            GroupId = groupId,
            UserId = userId,
            AddedByUserId = addedBy
        };

        await Context.GroupMembers.AddAsync(member);
        var result = await Context.SaveChangesAsync();
        Assert.True(result.IsNone, result.ToString());
    }

    private void SetCurrentUser(Guid userId, params Role[] roles)
    {
        var httpContext = BuildHttpContext(userId, roles);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
    }
}
