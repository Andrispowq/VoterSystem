using System.Net;
using System.Net.Http.Headers;
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
public class ChoiceControllerTests(TestWebAppFactory factory) : TestObjectFactory(factory)
{
    private static readonly UserLoginRequestDto UserLogin = new()
    {
        Email = "user@example.com",
        Password = "User@123"
    };

    private static readonly UserLoginRequestDto AdminLogin = new()
    {
        Email = "admin@example.com",
        Password = "Admin@123"
    };

    #region Get all

    [Fact]
    public async Task GetChoices_ReturnsOk_WhenVotingExists()
    {
        var (vId, _) = await SeedVotingWithChoiceAsync(UserLogin.Email);
        await AuthenticateAsync(UserLogin);

        var response = await HttpClient.GetAsync($"/api/v1/votings/{vId}/choices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChoices_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await HttpClient.GetAsync("/api/v1/votings/1/choices");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetChoices_ReturnsNotFound_WhenVotingMissing()
    {
        await AuthenticateAsync(UserLogin);
        var response = await HttpClient.GetAsync("/api/v1/votings/999999/choices");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    #endregion

    #region Get

    [Fact]
    public async Task GetChoiceById_ReturnsOk_WhenChoiceExists()
    {
        var (vId, cId) = await SeedVotingWithChoiceAsync(UserLogin.Email);
        await AuthenticateAsync(UserLogin);

        var response = await HttpClient.GetAsync($"/api/v1/votings/{vId}/choices/{cId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChoiceById_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await HttpClient.GetAsync("/api/v1/votings/1/choices/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetChoiceById_ReturnsNotFound_WhenChoiceMissing()
    {
        var (vId, _) = await SeedVotingWithChoiceAsync(UserLogin.Email);
        await AuthenticateAsync(UserLogin);

        var response = await HttpClient.GetAsync($"/api/v1/votings/{vId}/choices/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    #endregion
    
    #region Create

    [Fact]
    public async Task CreateChoice_ReturnsCreated_WhenValid()
    {
        var (vId, _) = await SeedVotingWithChoiceAsync(UserLogin.Email);
        await AuthenticateAsync(UserLogin);

        var dto = new VoteChoiceRequestDto { Name = "New-Option", Description = "desc" };

        var response = await HttpClient.PostAsJsonAsync($"/api/v1/votings/{vId}/choices", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateChoice_ReturnsUnauthorized_WhenNoToken()
    {
        var dto = new VoteChoiceRequestDto { Name = "Option", Description = "desc" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings/1/choices", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateChoice_ReturnsNotFound_WhenVotingMissing()
    {
        await AuthenticateAsync(UserLogin);
        var dto = new VoteChoiceRequestDto { Name = "Option", Description = "desc" };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/votings/999999/choices", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    #endregion
    
    #region Delete

    [Fact]
    public async Task DeleteChoice_ReturnsOk_WhenValid()
    {
        var (vId, cId) = await SeedVotingWithChoiceAsync(UserLogin.Email);
        await AuthenticateAsync(UserLogin);

        var response = await HttpClient.DeleteAsync($"/api/v1/votings/{vId}/choices/{cId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteChoice_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await HttpClient.DeleteAsync("/api/v1/votings/1/choices/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteChoice_ReturnsNotFound_WhenChoiceMissing()
    {
        var (vId, _) = await SeedVotingWithChoiceAsync(UserLogin.Email);
        await AuthenticateAsync(UserLogin);

        var response = await HttpClient.DeleteAsync($"/api/v1/votings/{vId}/choices/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    #endregion Delete

    #region Helpers

    private async Task<(long VotingId, long ChoiceId)> SeedVotingWithChoiceAsync(string ownerEmail)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx   = scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
        var owner = await ctx.Users.FirstAsync(u => u.Email == ownerEmail);

        var voting = new Voting
        {
            Name = $"Voting-{Guid.NewGuid()}",
            StartsAt = DateTime.UtcNow.AddHours(2),
            EndsAt = DateTime.UtcNow.AddDays(2),
            CreatedByUserId = owner.Id
        };
        ctx.Votings.Add(voting);
        await ctx.SaveChangesAsync();

        var choice = new VoteChoice
        {
            Name  = "Initial-Option",
            Description = "init",
            VotingId = voting.VotingId
        };
        ctx.VoteChoices.Add(choice);
        await ctx.SaveChangesAsync();

        return (voting.VotingId, choice.ChoiceId);
    }

    private async Task AuthenticateAsync(UserLoginRequestDto creds)
    {
        var resp = await HttpClient.PostAsJsonAsync("/api/v1/users/login", creds);
        resp.EnsureSuccessStatusCode();

        var tokens = await resp.Content.ReadFromJsonAsync<Tokens>()
                     ?? throw new InvalidOperationException("No tokens returned");

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AuthToken);
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
        DbContext.Votings.ExecuteDelete();
        
        CreateIfMissing(userManager, AdminLogin, "Admin");
        CreateIfMissing(userManager, UserLogin,  "User");
    }

    private static void CreateIfMissing(UserManager<User> um, UserLoginRequestDto creds, string role)
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
    
    #endregion
}