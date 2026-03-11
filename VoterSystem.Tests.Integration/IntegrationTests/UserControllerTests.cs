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
            Email = "new_user@email.com",
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

    #region Login

    [Fact]
    public async Task Login_ReturnsOk_AndSetsCookie_WhenCredentialsCorrect()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/login", AdminLogin);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Set-Cookie", response.Headers.ToString());
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordWrong()
    {
        // Arrange
        var badLogin = new UserLoginRequestDto
        {
            Email = AdminLogin.Email,
            Password = "WrongPass123!"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/login", badLogin);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsNotFound_WhenEmailDoesNotExist()
    {
        // Arrange
        var badLogin = new UserLoginRequestDto
        {
            Email = "nobody@example.com",
            Password = "SomePass1!"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/login", badLogin);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GetAllUsers

    [Fact]
    public async Task GetAllUsers_ReturnsOk_ForAdmin()
    {
        // Arrange
        await AuthenticateAsync(AdminLogin);

        // Act
        var response = await HttpClient.GetAsync("/api/v1/users/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsUnauthorized_ForNormalUser()
    {
        // Arrange
        await AuthenticateAsync(UserLogin);

        // Act
        var response = await HttpClient.GetAsync("/api/v1/users/all");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GetCurrentUser

    [Fact]
    public async Task GetCurrentUser_ReturnsOk_ForLoggedInUser()
    {
        // Arrange
        await AuthenticateAsync(UserLogin);

        // Act
        var response = await HttpClient.GetAsync("/api/v1/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GetUserById

    [Fact]
    public async Task GetUserById_ReturnsOk_ForAdminAccessingAnotherUser()
    {
        await AuthenticateAsync(AdminLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var response = await HttpClient.GetAsync($"/api/v1/users/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_ReturnsUnauthorized_ForUserAccessingAdmin()
    {
        var adminTokens = await LoginAndGetTokensAsync(AdminLogin); // no auth cookie set
        await AuthenticateAsync(UserLogin); // now sign-in as normal user

        var response = await HttpClient.GetAsync($"/api/v1/users/{adminTokens.UserId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_ReturnsOk_WhenValid()
    {
        await AuthenticateAsync(UserLogin);

        var dto = new UserChangePasswordRequestDto
        {
            OldPassword = "User@123",
            NewPassword = "User@456!"
        };

        var response = await HttpClient.PutAsJsonAsync("/api/v1/users/change-password", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //change back
        dto = new UserChangePasswordRequestDto
        {
            OldPassword = "User@456!",
            NewPassword = "User@123"
        };

        response = await HttpClient.PutAsJsonAsync("/api/v1/users/change-password", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReturnsBadRequest_WhenSamePassword()
    {
        await AuthenticateAsync(UserLogin);

        var dto = new UserChangePasswordRequestDto
        {
            OldPassword = "User@123",
            NewPassword = "User@123"
        };

        var response = await HttpClient.PutAsJsonAsync("/api/v1/users/change-password", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PromoteAndDemote

    [Fact]
    public async Task PromoteToAdmin_ReturnsOk_ForAdminPromotingUser()
    {
        await AuthenticateAsync(AdminLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var promote = await HttpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/promote?userId={userId}"));

        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);

        //demote back
        var demote = await HttpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/demote?userId={userId}"));

        Assert.Equal(HttpStatusCode.OK, demote.StatusCode);
    }

    [Fact]
    public async Task PromoteToAdmin_ReturnsBadRequest_WhenSelfPromote()
    {
        await AuthenticateAsync(AdminLogin);
        var adminId = await GetUserIdAsync(AdminLogin.Email);

        var promote = await HttpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/promote?userId={adminId}"));

        Assert.Equal(HttpStatusCode.BadRequest, promote.StatusCode);
    }

    [Fact]
    public async Task DemoteToUser_ReturnsOk_ForAdminDemotingUser()
    {
        await AuthenticateAsync(AdminLogin);
        var userId = await GetUserIdAsync(UserLogin.Email);

        var demote = await HttpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/demote?userId={userId}"));

        Assert.Equal(HttpStatusCode.OK, demote.StatusCode);
    }

    #endregion

    #region RefreshToken

    [Fact]
    public async Task RefreshToken_ReturnsOk_WhenValid()
    {
        var tokens = await LoginAndGetTokensAsync(UserLogin);

        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/refresh-token",
            tokens.RefreshToken.ToString());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_ReturnsOk_WhenAuthenticated()
    {
        await AuthenticateAsync(UserLogin);

        var response = await HttpClient.DeleteAsync("/api/v1/users/logout");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
            adminUser = new User
            {
                UserName = AdminLogin.Email,
                Email = AdminLogin.Email,
                Name = "Test Admin",
                Role = Role.Admin
            };
            userManager.CreateAsync(adminUser, AdminLogin.Password).Wait();
            userManager.AddToRoleAsync(adminUser, "Admin").Wait();
        }

        // Example to seed normal user
        var user = userManager.FindByEmailAsync(UserLogin.Email).Result;
        if (user == null)
        {
            user = new User
            {
                UserName = UserLogin.Email,
                Email = UserLogin.Email,
                Name = "Test User",
                Role = Role.User
            };
            userManager.CreateAsync(user, UserLogin.Password).Wait();
        }
    }
    
    private async Task<Guid> GetUserIdAsync(string email)
    {
        var response = await HttpClient.GetAsync("/api/v1/users/all");
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            Assert.Fail($"Failed with the following response: {await response.Content.ReadAsStringAsync()}");
        }

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? [];
        return users.Single(u => u.Email == email).Id;
    }

    private async Task<TokensDto> LoginAndGetTokensAsync(UserLoginRequestDto credentials)
    {
        // fresh client to avoid polluting the main CookieContainer
        using var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users/login", credentials);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            Assert.Fail($"Failed with the following response: {await response.Content.ReadAsStringAsync()}");
        }
        
        return (await response.Content.ReadFromJsonAsync<TokensDto>())!;
    }

    private async Task AuthenticateAsync(UserLoginRequestDto credentials)
    {
        var response = await HttpClient.PostAsJsonAsync("/api/v1/users/login", credentials);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            Assert.Fail($"Failed with the following response: {await response.Content.ReadAsStringAsync()}");
        }
        //Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        var cookie = setCookie.Split(';')[0];

        if (HttpClient.DefaultRequestHeaders.Contains("Cookie"))
        {
            HttpClient.DefaultRequestHeaders.Remove("Cookie");
        }

        HttpClient.DefaultRequestHeaders.Add("Cookie", cookie);
    }

    #endregion
}