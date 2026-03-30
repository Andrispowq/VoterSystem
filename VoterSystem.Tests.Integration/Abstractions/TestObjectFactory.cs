using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;

namespace VoterSystem.Tests.Integration.Abstractions;

public abstract class TestObjectFactory : BaseTest
{
    protected readonly VoterSystemDbContext DbContext;

    protected TestObjectFactory(TestWebAppFactory factory) : base(factory)
    {
        factory.EmailService.Clear();
        DbContext = Scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();

        DbContext.Votings.ExecuteDelete();
        DbContext.VoteChoices.ExecuteDelete();
        DbContext.VotingParticipations.ExecuteDelete();
        DbContext.AnonymousBallots.ExecuteDelete();
        DbContext.Users.ExecuteDelete();
        DbContext.Groups.ExecuteDelete();
        DbContext.GroupMembers.ExecuteDelete();
        
        //DbContext.Database.EnsureDeleted();
        //DbContext.Database.EnsureCreated();

        Init();
    }

    private void Init()
    {
        var roleManager = Scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();
        SeedRoles(roleManager);

        var userManager = Scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        SeedUsers(userManager);
    }

    protected async Task AuthenticateAsAsync(UserLoginRequestDto credentials)
    {
        var login = await HttpClient.PostAsJsonAsync("/api/v1/users/login", credentials);
        login.EnsureSuccessStatusCode();

        var tokens = await login.Content.ReadFromJsonAsync<TokensDto>()
                     ?? throw new InvalidOperationException("No token returned");

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AuthToken);
    }

    protected abstract void SeedRoles(RoleManager<UserRole> roleManager);
    protected abstract void SeedUsers(UserManager<User> userManager);
}