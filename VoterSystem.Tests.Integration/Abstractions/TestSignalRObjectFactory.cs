using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;

namespace VoterSystem.Tests.Integration.Abstractions;

public class TestSignalRObjectFactory : BaseTest
{
    private readonly string _hubName;

    private readonly UserRegisterRequestDto _adminUser = new()
    {
        Email = "admin@example.com",
        Name = "Test Admin",
        Password = "testAdmin123!"
    };

    private readonly UserRegisterRequestDto _user = new()
    {
        Email = "user@example.com",
        Name = "Test User",
        Password = "testUser123!"
    };

    protected UserLoginRequestDto AdminLogin => new() { Email = _adminUser.Email, Password = _adminUser.Password };
    protected UserLoginRequestDto UserLogin => new() { Email = _user.Email, Password = _user.Password };

    protected TestSignalRObjectFactory(TestWebAppFactory factory, string hubName) : base(factory)
    {
        _hubName = hubName;
        
        var dbContext = Scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
        SeedDatabase(dbContext);

        var roleManager = Scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();
        SeedRoles(roleManager);

        var userService = Scope.ServiceProvider.GetRequiredService<IUserService>();

        var user = new User { UserName = _user.Email, Name = _user.Name, Email = _user.Email };
        var admin = new User { UserName = _adminUser.Email, Name = _adminUser.Name, Email = _adminUser.Email };
        
        userService.CreateUser(user, _user.Password, Role.User).Wait();
        userService.CreateUser(admin, _adminUser.Password, Role.Admin).Wait();
    }
    
    protected async Task<string> Login(UserLoginRequestDto user)
    {
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/login", user);

        try
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<TokensDto>();

            if (loginResponse?.AuthToken == null)
                throw new Exception("Login failed");

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.AuthToken);
            
            return loginResponse.AuthToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            var message = await response.Content.ReadAsStringAsync();
            Console.WriteLine(message);
            throw;
        }
    }
    
    protected HubConnection CreateHubConnection(string? token = null)
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"http://localhost/{_hubName}", options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                    
                if (token != null)
                {
                    options.AccessTokenProvider = () => Task.FromResult(token)!;
                }
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .AddJsonProtocol()
            .Build();

        return hubConnection;
    }
    
    private static void SeedDatabase(VoterSystemDbContext context)
    {
        //TODO

        context.SaveChanges();
    }

    private static void SeedRoles(RoleManager<UserRole> roleManager)
    {
        string[] roleNames = ["User", "Admin"];

        foreach (var roleName in roleNames)
        {
            var roleExist = roleManager.RoleExistsAsync(roleName).Result;
            if (!roleExist)
            {
                // Create the roles and seed them to the database
                roleManager.CreateAsync(new UserRole(roleName)).Wait();
            }
        }
    }
}