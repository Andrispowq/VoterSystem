using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Tests.Integration.Abstractions;

namespace VoterSystem.Tests.Integration.IntegrationTests;

[Collection("IntegrationTests")]
public class VoteControllerTests(TestWebAppFactory factory) : TestObjectFactory(factory)
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

    #region Cast-vote POST /api/v1/votes/cast-vote

    [Fact]
    public async Task CastVote_ReturnsOk_WhenChoiceAndUserAreValid()
    {
        // Arrange – seed a voting with a choice that the regular user can vote on
        long choiceId;
        using (var scope = Factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
            var admin = ctx.Users.First(u => u.Email == AdminLogin.Email);
            var voting = new Voting
            {
                Name = "Integration voting",
                StartsAt = DateTime.UtcNow.AddHours(-1),
                EndsAt = DateTime.UtcNow.AddDays(2),
                CreatedByUserId = admin.Id
            };
            ctx.Votings.Add(voting);
            await ctx.SaveChangesAsync();

            var choice = new VoteChoice { Name = "Option-A", VotingId = voting.VotingId };
            var choice2 = new VoteChoice { Name = "Option-B", VotingId = voting.VotingId };
            ctx.VoteChoices.AddRange(choice, choice2);
            await ctx.SaveChangesAsync();
            choiceId = choice.ChoiceId;
        }

        await AuthenticateAsAsync(UserLogin);

        // Act
        var response = await HttpClient.PostAsync($"/api/v1/votes/cast-vote?choiceId={choiceId}",
                                                  new StringContent(string.Empty));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CastVote_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        var response = await HttpClient.PostAsync("/api/v1/votes/cast-vote?choiceId=1",
                                                  new StringContent(string.Empty));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CastVote_ReturnsNotFound_WhenChoiceDoesNotExist()
    {
        await AuthenticateAsAsync(UserLogin);

        var response = await HttpClient.PostAsync("/api/v1/votes/cast-vote?choiceId=999999",
                                                  new StringContent(string.Empty));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Get-my-votes GET /api/v1/votes

    [Fact]
    public async Task GetMyVotes_ReturnsOk_ForAuthenticatedUser()
    {
        await AuthenticateAsAsync(UserLogin);

        var response = await HttpClient.GetAsync("/api/v1/votes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyVotes_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        var response = await HttpClient.GetAsync("/api/v1/votes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyVotes_ReturnsUnauthorized_ForAdminUser()
    {
        await AuthenticateAsAsync(AdminLogin);

        var response = await HttpClient.GetAsync("/api/v1/votes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Helpers

    protected override void SeedRoles(RoleManager<UserRole> roleManager)
    {
        foreach (var roleName in new[] { "User", "Admin" })
        {
            if (!roleManager.RoleExistsAsync(roleName).Result)
            {
                roleManager.CreateAsync(new UserRole(roleName)).Wait();
            }
        }
    }

    protected override void SeedUsers(UserManager<User> userManager)
    {
        // Admin
        var admin = userManager.FindByEmailAsync(AdminLogin.Email).Result;
        if (admin is null)
        {
            admin = new User
            {
                UserName = AdminLogin.Email,
                Email = AdminLogin.Email,
                Name = "Seed-Admin",
                Role = Role.Admin
            };
            userManager.CreateAsync(admin, AdminLogin.Password).Wait();
            userManager.AddToRoleAsync(admin, "Admin").Wait();
        }

        // Regular user
        var user = userManager.FindByEmailAsync(UserLogin.Email).Result;
        if (user is null)
        {
            user = new User
            {
                UserName = UserLogin.Email,
                Email = UserLogin.Email,
                Name = "Seed-User",
                Role = Role.User
            };
            userManager.CreateAsync(user, UserLogin.Password).Wait();
            userManager.AddToRoleAsync(user, "User").Wait();
        }
    }

    #endregion
}