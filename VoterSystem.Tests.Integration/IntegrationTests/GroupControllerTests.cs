using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Tests.Integration.Abstractions;

namespace VoterSystem.Tests.Integration.IntegrationTests;

[Collection("IntegrationTests")]
public class GroupControllerTests(TestWebAppFactory factory) : TestObjectFactory(factory)
{
    private static readonly UserLoginRequestDto AdminLogin = new()
    {
        Email = "admin@example.com",
        Password = "Admin@123"
    };

    private static readonly UserLoginRequestDto UserLogin = new()
    {
        Email = "user@example.com",
        Password = "User@123"
    };

    private static readonly UserLoginRequestDto AnotherUserLogin = new()
    {
        Email = "another.user@example.com",
        Password = "User@456"
    };

    [Fact]
    public async Task GetGroups_ReturnsOnlyGroupsUserBelongsTo()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AddMemberAsync(group.GroupId, UserLogin.Email, AdminLogin.Email);
        await CreateGroupOwnedByAsync(AdminLogin.Email); // group without user membership

        await AuthenticateAsAsync(UserLogin);
        var response = await HttpClient.GetAsync("/api/v1/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var groups = await response.Content.ReadFromJsonAsync<List<GroupDto>>() ?? [];
        Assert.Single(groups);
        Assert.Equal(group.GroupId, groups[0].GroupId);
    }

    [Fact]
    public async Task GetGroups_ReturnsUnauthorized_WhenMissingToken()
    {
        ClearAuthentication();
        var response = await HttpClient.GetAsync("/api/v1/groups");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupById_ReturnsOk_ForMember()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AddMemberAsync(group.GroupId, UserLogin.Email, AdminLogin.Email);

        await AuthenticateAsAsync(UserLogin);
        var response = await HttpClient.GetAsync($"/api/v1/groups/{group.GroupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(dto);
        Assert.Equal(group.GroupId, dto.GroupId);
    }

    [Fact]
    public async Task GetGroupById_ReturnsUnauthorized_ForNonMemberUser()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);

        await AuthenticateAsAsync(UserLogin);
        var response = await HttpClient.GetAsync($"/api/v1/groups/{group.GroupId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupById_ReturnsNotFound_WhenGroupMissing()
    {
        ClearAuthentication();
        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.GetAsync($"/api/v1/groups/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_ReturnsCreated_ForAdmin()
    {
        ClearAuthentication();
        await AuthenticateAsAsync(AdminLogin);
        var request = new CreateGroupRequest
        {
            Name = "Engineering",
            Description = "Handles all builds"
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/groups", request);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(dto);
        Assert.Equal(request.Name, dto.Name);
    }

    [Fact]
    public async Task CreateGroup_ReturnsForbidden_ForUser()
    {
        ClearAuthentication();
        await AuthenticateAsAsync(UserLogin);
        var request = new CreateGroupRequest
        {
            Name = "Finance",
            Description = "Controls budgets"
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/groups", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_ReturnsOk_ForCreatorAdmin()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.DeleteAsync($"/api/v1/groups/{group.GroupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await DbContext.Groups.AnyAsync(g => g.GroupId == group.GroupId));
    }

    [Fact]
    public async Task DeleteGroup_ReturnsForbidden_ForRegularUser()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AuthenticateAsAsync(UserLogin);

        var response = await HttpClient.DeleteAsync($"/api/v1/groups/{group.GroupId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddUserToGroup_ReturnsOk_ForAdmin()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AuthenticateAsAsync(AdminLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var response = await HttpClient.PostAsync($"/api/v1/groups/{group.GroupId}/members/{userId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AuthenticateAsAsync(UserLogin);
        var groupResponse = await HttpClient.GetAsync($"/api/v1/groups/{group.GroupId}");
        Assert.Equal(HttpStatusCode.OK, groupResponse.StatusCode);
    }

    [Fact]
    public async Task AddUserToGroup_ReturnsForbidden_ForRegularUser()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AuthenticateAsAsync(UserLogin);
        var userId = await GetUserIdAsync(AnotherUserLogin.Email);

        var response = await HttpClient.PostAsync($"/api/v1/groups/{group.GroupId}/members/{userId}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddUserToGroup_ReturnsNotFound_ForMissingGroup()
    {
        ClearAuthentication();
        await AuthenticateAsAsync(AdminLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var response = await HttpClient.PostAsync($"/api/v1/groups/{Guid.NewGuid()}/members/{userId}", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUserFromGroup_ReturnsOk_ForAdmin()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AddMemberAsync(group.GroupId, UserLogin.Email, AdminLogin.Email);
        await AuthenticateAsAsync(AdminLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var response = await HttpClient.DeleteAsync($"/api/v1/groups/{group.GroupId}/members/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AuthenticateAsAsync(UserLogin);
        var groupResponse = await HttpClient.GetAsync($"/api/v1/groups/{group.GroupId}");
        Assert.Equal(HttpStatusCode.Unauthorized, groupResponse.StatusCode);
    }

    [Fact]
    public async Task RemoveUserFromGroup_ReturnsForbidden_ForRegularUser()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AddMemberAsync(group.GroupId, UserLogin.Email, AdminLogin.Email);
        await AuthenticateAsAsync(UserLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var response = await HttpClient.DeleteAsync($"/api/v1/groups/{group.GroupId}/members/{userId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUserFromGroup_ReturnsNotFound_WhenMembershipMissing()
    {
        ClearAuthentication();
        var group = await CreateGroupOwnedByAsync(AdminLogin.Email);
        await AuthenticateAsAsync(AdminLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var response = await HttpClient.DeleteAsync($"/api/v1/groups/{group.GroupId}/members/{userId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    protected override void SeedRoles(RoleManager<UserRole> roleManager)
    {
        foreach (var role in new[] { "User", "Admin" })
        {
            if (!roleManager.RoleExistsAsync(role).Result)
            {
                roleManager.CreateAsync(new UserRole(role)).Wait();
            }
        }
    }

    protected override void SeedUsers(UserManager<User> userManager)
    {
        CreateIfMissing(userManager, AdminLogin, Role.Admin);
        CreateIfMissing(userManager, UserLogin, Role.User);
        CreateIfMissing(userManager, AnotherUserLogin, Role.User);
    }

    private static void CreateIfMissing(UserManager<User> userManager, UserLoginRequestDto creds, Role role)
    {
        var user = userManager.FindByEmailAsync(creds.Email).Result;
        if (user != null) return;

        user = new User
        {
            Email = creds.Email,
            UserName = creds.Email,
            Name = creds.Email.Split('@')[0],
            Role = role,
            LoginMode = UserLoginMode.Password
        };

        userManager.CreateAsync(user, creds.Password).Wait();
        userManager.AddToRoleAsync(user, role.ToString()).Wait();
    }

    private void ClearAuthentication()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<Group> CreateGroupOwnedByAsync(string ownerEmail)
    {
        var owner = await DbContext.Users.SingleAsync(u => u.Email == ownerEmail);
        var group = new Group
        {
            Name = $"Group-{Guid.NewGuid():N}"[..30],
            Description = "Integration group",
            CreatorUserId = owner.Id
        };

        await DbContext.Groups.AddAsync(group);
        await DbContext.SaveChangesAsync();

        var membership = new GroupMembers
        {
            GroupId = group.GroupId,
            UserId = owner.Id,
            AddedByUserId = owner.Id
        };
        await DbContext.GroupMembers.AddAsync(membership);
        await DbContext.SaveChangesAsync();

        return group;
    }

    private async Task AddMemberAsync(Guid groupId, string userEmail, string addedByEmail)
    {
        var user = await DbContext.Users.SingleAsync(u => u.Email == userEmail);
        var addedBy = await DbContext.Users.SingleAsync(u => u.Email == addedByEmail);

        var existing = await DbContext.GroupMembers.FindAsync(groupId, user.Id);
        if (existing != null) return;

        await DbContext.GroupMembers.AddAsync(new GroupMembers
        {
            GroupId = groupId,
            UserId = user.Id,
            AddedByUserId = addedBy.Id
        });
        await DbContext.SaveChangesAsync();
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        var user = await DbContext.Users.SingleAsync(u => u.Email == email);
        return user.Id;
    }
}
