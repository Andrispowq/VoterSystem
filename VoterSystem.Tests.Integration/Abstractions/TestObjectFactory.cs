using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
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
        DbContext = Scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();

        Init();
    }

    private void Init()
    {
        var roleManager = Scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();
        SeedRoles(roleManager);

        var userManager = Scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        SeedUsers(userManager);
    }

    protected async Task Login(UserLoginRequestDto loginRequest)
    {
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/login", loginRequest);
        var loginResponse = await response.Content.ReadFromJsonAsync<TokensDto>();

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse?.AuthToken);
    }

    protected abstract void SeedRoles(RoleManager<UserRole> roleManager);
    protected abstract void SeedUsers(UserManager<User> userManager);
}