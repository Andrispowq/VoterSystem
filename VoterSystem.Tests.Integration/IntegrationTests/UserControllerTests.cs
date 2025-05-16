using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Tests.Integration.Abstractions;

namespace VoterSystem.Tests.Integration.IntegrationTests;

[Collection("IntegrationTests")]
public class UserControllerTests(TestWebAppFactory factory) : TestObjectFactory(factory)
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

    #region Register

    [Fact]
    public async Task Register_ReturnsOk_WhenCorrect()
    {
        // Act
        var newUser = new UserRegisterRequestDto
        {
            Email = "new@email.com",
            Name = "New User",
            Password = "NewPassword1#"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/register", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsError_WhenUserExists()
    {
        // Act
        var newUser = new UserRegisterRequestDto
        {
            Email = AdminLogin.Email,
            Name = "Admin User",
            Password = "NewPassword1#"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/register", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsError_WhenPasswordBad()
    {
        // Act
        var newUser = new UserRegisterRequestDto
        {
            Email = "test@email.com",
            Name = "Test User",
            Password = "New"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/register", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    #endregion
    
    #region Helpers
    
    protected override void SeedRoles(RoleManager<UserRole> roleManager)
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

    protected override void SeedUsers(UserManager<User> userManager)
    {
        // Example to seed an Admin user
        var adminUser = userManager.FindByEmailAsync(AdminLogin.Email).Result;
        if (adminUser == null)
        {
            adminUser = new User { UserName = AdminLogin.Email, Email = AdminLogin.Email, Name = "Test Admin" };
            userManager.CreateAsync(adminUser, AdminLogin.Password).Wait();
            userManager.AddToRoleAsync(adminUser, "Admin").Wait();
        }
        
        // Example to seed normal user
        var user = userManager.FindByEmailAsync(UserLogin.Email).Result;
        if (user == null)
        {
            user = new User { UserName = UserLogin.Email, Email = UserLogin.Email, Name = "Test User" };
            userManager.CreateAsync(user, UserLogin.Password).Wait();
        }
    }

    #endregion
}