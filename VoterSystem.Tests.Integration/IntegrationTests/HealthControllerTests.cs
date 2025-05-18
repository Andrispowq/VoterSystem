using Microsoft.AspNetCore.Identity;
using VoterSystem.DataAccess.Model;
using VoterSystem.Tests.Integration.Abstractions;

namespace VoterSystem.Tests.Integration.IntegrationTests;

[Collection("IntegrationTests")]
public class HealthControllerTests(TestWebAppFactory factory) : TestObjectFactory(factory)
{
    [Fact]
    public async Task TestHealthEndpoint_ReturnsOk()
    {
        var response = await HttpClient.PostAsync("/api/v1/health", null);
        response.EnsureSuccessStatusCode();
    }
    
    protected override void SeedRoles(RoleManager<UserRole> roleManager) {}
    protected override void SeedUsers(UserManager<User> userManager) {}
}