using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Tests.Integration.Abstractions;

namespace VoterSystem.Tests.Integration.IntegrationTests;

[Collection("IntegrationTests")]
public class VotingControllerTests(TestWebAppFactory factory) : TestObjectFactory(factory)
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

    #region GET /votings/votable

    [Fact]
    public async Task GetVotableVotings_ReturnsOk_ForUser()
    {
        await AuthenticateAsAsync(UserLogin);

        var response = await HttpClient.GetAsync("/api/v1/votings/votable");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVotableVotings_ReturnsUnauthorized_WithoutToken()
    {
        var response = await HttpClient.GetAsync("/api/v1/votings/votable");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVotableVotings_ReturnsUnauthorized_ForAdmin()
    {
        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.GetAsync("/api/v1/votings/votable");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region GET /votings/{id}

    [Fact]
    public async Task GetVotingById_ReturnsOk_WhenExists()
    {
        var votingId = await SeedVotingAsync(AdminLogin.Email);

        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.GetAsync($"/api/v1/votings/{votingId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVotingById_ReturnsUnauthorized_WithoutToken()
    {
        var response = await HttpClient.GetAsync("/api/v1/votings/999999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVotingById_ReturnsNotFound_ForUnknownId()
    {
        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.GetAsync("/api/v1/votings/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /votings

    [Fact]
    public async Task AddVoting_ReturnsCreated_WhenValid()
    {
        await AuthenticateAsAsync(UserLogin);

        var dto = new VotingCreateRequestDto
        {
            Name = "Integration voting",
            StartsAt = DateTime.UtcNow.AddHours(3),
            EndsAt = DateTime.UtcNow.AddDays(3)
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddVoting_ReturnsCreated_WhenGroupProvidedAndUserMember()
    {
        var groupId = await CreateGroupWithMemberAsync(UserLogin.Email);
        await AuthenticateAsAsync(UserLogin);

        var dto = new VotingCreateRequestDto
        {
            Name = "Grouped voting",
            StartsAt = DateTime.UtcNow.AddHours(3),
            EndsAt = DateTime.UtcNow.AddDays(3),
            GroupId = groupId
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddVoting_ReturnsUnauthorized_WhenGroupProvidedAndUserNotMember()
    {
        var groupId = await CreateGroupWithMemberAsync(AdminLogin.Email);
        await AuthenticateAsAsync(UserLogin);

        var dto = new VotingCreateRequestDto
        {
            Name = "Unauthorized group voting",
            StartsAt = DateTime.UtcNow.AddHours(3),
            EndsAt = DateTime.UtcNow.AddDays(3),
            GroupId = groupId
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddVoting_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        await AuthenticateAsAsync(UserLogin);

        var dto = new VotingCreateRequestDto
        {
            Name = "Missing group voting",
            StartsAt = DateTime.UtcNow.AddHours(3),
            EndsAt = DateTime.UtcNow.AddDays(3),
            GroupId = Guid.NewGuid()
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddVoting_ReturnsUnauthorized_WithoutToken()
    {
        var dto = new VotingCreateRequestDto
        {
            Name = "Bad voting",
            StartsAt = DateTime.UtcNow.AddHours(3),
            EndsAt = DateTime.UtcNow.AddDays(3)
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddVoting_ReturnsBadRequest_WhenDatesInvalid()
    {
        await AuthenticateAsAsync(UserLogin);

        var dto = new VotingCreateRequestDto
        {
            Name = "Broken voting",
            StartsAt = DateTime.UtcNow.AddMinutes(5), // too soon
            EndsAt = DateTime.UtcNow.AddMinutes(10)
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST /votings/{id}/start

    [Fact]
    public async Task StartVoting_ReturnsOk_WithTwoChoices()
    {
        var votingId = await SeedVotingWithChoicesAsync(UserLogin.Email, 2);

        await AuthenticateAsAsync(UserLogin);

        var response = await HttpClient.PostAsync($"/api/v1/votings/{votingId}/start", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StartVoting_ReturnsUnauthorized_WithoutToken()
    {
        var response = await HttpClient.PostAsync("/api/v1/votings/1/start", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartVoting_ReturnsBadRequest_WhenChoiceCountLessThanTwo()
    {
        var votingId = await SeedVotingWithChoicesAsync(UserLogin.Email, 1);

        await AuthenticateAsAsync(UserLogin);

        var response = await HttpClient.PostAsync($"/api/v1/votings/{votingId}/start", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DELETE /votings/{id}

    [Fact]
    public async Task DeleteVoting_ReturnsOk_ForAdmin()
    {
        var votingId = await SeedVotingAsync(AdminLogin.Email);

        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.DeleteAsync($"/api/v1/votings/{votingId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVoting_ReturnsUnauthorized_WithoutToken()
    {
        var response = await HttpClient.DeleteAsync("/api/v1/votings/123456");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVoting_ReturnsNotFound_ForUnknownId()
    {
        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.DeleteAsync("/api/v1/votings/123456");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task<long> SeedVotingAsync(string ownerEmail)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
        var owner = await ctx.Users.FirstAsync(u => u.Email == ownerEmail);

        var voting = new Voting
        {
            Name = $"Voting-{Guid.NewGuid()}",
            StartsAt = DateTime.UtcNow.AddHours(5),
            EndsAt = DateTime.UtcNow.AddDays(3),
            CreatedByUserId = owner.Id
        };

        ctx.Votings.Add(voting);
        await ctx.SaveChangesAsync();
        return voting.VotingId;
    }

    private async Task<long> SeedVotingWithChoicesAsync(string ownerEmail, int choiceCount)
    {
        var id = await SeedVotingAsync(ownerEmail);

        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();

        for (var i = 0; i < choiceCount; i++)
        {
            ctx.VoteChoices.Add(new VoteChoice { VotingId = id, Name = $"Alt-{i}" });
        }

        await ctx.SaveChangesAsync();
        return id;
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
        //Delete votings from previous tests if there were any
        DbContext.Votings.ExecuteDelete();
        DbContext.GroupMembers.ExecuteDelete();
        DbContext.Groups.ExecuteDelete();

        CreateUserIfMissing(userManager, AdminLogin, "Admin");
        CreateUserIfMissing(userManager, UserLogin, "User");
    }

    private static void CreateUserIfMissing(UserManager<User> um, UserLoginRequestDto creds, string role)
    {
        var user = um.FindByEmailAsync(creds.Email).Result;
        if (user != null) return;

        user = new User
        {
            Email = creds.Email,
            UserName = creds.Email,
            Name = creds.Email.Split('@')[0],
            Role = Enum.Parse<Role>(role)
        };
        um.CreateAsync(user, creds.Password).Wait();
        um.AddToRoleAsync(user, role).Wait();
    }

    private async Task<Guid> CreateGroupWithMemberAsync(string memberEmail)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
        var member = await ctx.Users.FirstAsync(u => u.Email == memberEmail);

        var group = new Group
        {
            GroupId = Guid.NewGuid(),
            CreatorUserId = member.Id,
            Name = $"Group-{Guid.NewGuid():N}"[..30],
            Description = "Test group"
        };

        await ctx.Groups.AddAsync(group);
        await ctx.GroupMembers.AddAsync(new GroupMembers
        {
            GroupId = group.GroupId,
            UserId = member.Id,
            AddedByUserId = member.Id
        });
        await ctx.SaveChangesAsync();

        return group.GroupId;
    }

    #endregion
}
