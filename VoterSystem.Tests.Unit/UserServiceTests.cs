using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Unit;

public class UserServiceTests : UnitTestBase, IDisposable
{
    private readonly UserService _userService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<ITokenIssuer> _mockTokenIssuer = new();

    private User _user = null!;
    private User _adminUser = null!;

    public UserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _mockUserManager = CreateUserManagerMock();
        _mockSignInManager = CreateSignInManagerMock(_mockUserManager);

        _userService = new UserService(
            _httpContextAccessorMock.Object,
            _loggerMock.Object,
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockTokenIssuer.Object
        );

        SeedDatabase();
    }

    private Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            null!, // IOptions<IdentityOptions> optionsAccessor
            null!, // IPasswordHasher<User> passwordHasher
            null!, // IEnumerable<IUserValidator<User>> userValidators
            null!, // IEnumerable<IPasswordValidator<User>> passwordValidators
            null!, // ILookupNormalizer keyNormalizer
            null!, // IdentityErrorDescriber errors
            null!, // IServiceProvider services
            null! // ILogger<UserManager<User>> logger
        );
    }

    private Mock<SignInManager<User>> CreateSignInManagerMock(Mock<UserManager<User>> userManagerMock)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        return new Mock<SignInManager<User>>(
            userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, // IOptions<IdentityOptions> optionsAccessor
            null!, // ILogger<SignInManager<User>> logger
            null!, // IAuthenticationSchemeProvider schemes
            null! // IUserConfirmation<User> confirmation
        );
    }

    #region User Creation

    [Fact]
    public async Task CreateUser_WhenRoleIsGiven_ReturnsNoError()
    {
        // Arrange
        var user = new User { UserName = "user@test.com", Email = "user@test.com", Name = "user", Role = Role.User };
        var password = "Password123";
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), password)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), Role.User.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.CreateUser(user, password);

        // Assert
        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task CreateUser_WhenCreationFails_ReturnsError()
    {
        // Arrange
        var user = new User { UserName = "user@test.com", Email = "user@test.com", Name = "user", Role = Role.User };
        var password = "Password123";
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User creation failed" }));

        // Act
        var result = await _userService.CreateUser(user, password);

        // Assert
        Assert.True(result.IsSome);
        Assert.IsType<BadRequestError>(result.AsSome.Value);
    }

    #endregion

    #region User Login

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        string email = "nonexistent@test.com";
        string password = "password123";
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(null as User);

        // Act
        var result = await _userService.LoginAsync(email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorizedError()
    {
        // Arrange
        string email = "user@test.com";
        string password = "wrongpassword";
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(_user);
        _mockSignInManager
            .Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<bool>())).ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _userService.LoginAsync(email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task Login_WhenSuccessful_ReturnsTokens()
    {
        // Arrange
        var email = "user@test.com";
        var password = "password123";
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(_user);
        _mockSignInManager
            .Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<bool>())).ReturnsAsync(SignInResult.Success);
        _mockTokenIssuer.Setup(x => x.GenerateJwtToken(It.IsAny<User>()))
            .Returns("accessToken");
        _mockUserManager
            .Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.LoginAsync(email, password);

        // Assert
        Assert.True(result.HasValue);
        Assert.Equal("accessToken", result.Value.AuthToken);
    }

    #endregion

    [Fact]
    public async Task AnyAdmins_ReturnsTrue_WhenAdminsExist()
    {
        var admins = new List<User> { NextValidUser };
        _mockUserManager.Setup(x => x.GetUsersInRoleAsync("Admin")).ReturnsAsync(admins);

        var result = await _userService.AnyAdmins();

        Assert.True(result);
    }

    [Fact]
    public async Task AnyAdmins_ReturnsFalse_WhenNoAdminsExist()
    {
        _mockUserManager.Setup(x => x.GetUsersInRoleAsync("Admin")).ReturnsAsync(new List<User>());

        var result = await _userService.AnyAdmins();

        Assert.False(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUnauthorized_WhenNotAdmin()
    {
        _mockUserManager.Setup(x => x.Users).Returns(new List<User>().AsQueryable());
        SetCurrentUserRole(false);

        var result = await _userService.GetAllUsersAsync();

        Assert.True(result.IsError);
        Assert.IsType<UnauthorizedError>(result.Error);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUsers_WhenAdmin()
    {
        var users = new List<User> { NextValidUser, NextValidUser };
        _mockUserManager.Setup(x => x.Users).Returns(users.AsQueryable().BuildMock());
        SetCurrentUserRole(true);

        var result = await _userService.GetAllUsersAsync();

        Assert.True(result.HasValue);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task LogoutAsync_ReturnsNone_WhenSuccessful()
    {
        _mockUserService_GetCurrentUserAsync_ReturnsValidUser();
        _mockSignInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await _userService.LogoutAsync();
        Assert.True(result.IsNone, result.ToString());
    }

    [Fact]
    public async Task LogoutAsync_ReturnsError_WhenNoCurrentUser()
    {
        _mockUserService_GetCurrentUserAsync_ReturnsError();

        var result = await _userService.LogoutAsync();

        Assert.True(result.IsSome);
        Assert.IsType<NotFoundError>(result.AsSome.Value);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsBadRequest_WhenOldNewSame()
    {
        var oldPassword = "Pass1";
        var newPassword = "Pass1";

        _mockUserService_GetCurrentUserAsync_ReturnsValidUser();

        var result = await _userService.ChangePasswordAsync(oldPassword, newPassword);

        Assert.True(result.IsSome);
        Assert.IsType<BadRequestError>(result.AsSome.Value);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsNone_WhenSuccessful()
    {
        var oldPassword = "Pass1";
        var newPassword = "Pass2";

        _mockUserService_GetCurrentUserAsync_ReturnsValidUser();
        _mockUserManager.Setup(x => x.ChangePasswordAsync(It.IsAny<User>(), oldPassword, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _userService.ChangePasswordAsync(oldPassword, newPassword);

        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsConflict_WhenFailed()
    {
        var oldPassword = "Pass1";
        var newPassword = "Pass2";

        _mockUserService_GetCurrentUserAsync_ReturnsValidUser();
        _mockUserManager.Setup(x => x.ChangePasswordAsync(It.IsAny<User>(), oldPassword, newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

        var result = await _userService.ChangePasswordAsync(oldPassword, newPassword);

        Assert.True(result.IsSome);
        Assert.IsType<ConflictError>(result.AsSome.Value);
    }

    [Fact]
    public async Task GenerateEmailConfirmTokenAsync_ReturnsToken_WhenUserExists()
    {
        _mockUserService_GetCurrentUserAsync_ReturnsValidUser();
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>())).ReturnsAsync("token");

        var result = await _userService.GenerateEmailConfirmTokenAsync();

        Assert.True(result.HasValue, result.ToString());
        Assert.Equal("token", result.Value);
    }

    [Fact]
    public async Task GenerateEmailConfirmTokenAsync_ReturnsError_WhenNoUser()
    {
        _mockUserService_GetCurrentUserAsync_ReturnsError();

        var result = await _userService.GenerateEmailConfirmTokenAsync();

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsNone_WhenSuccessful()
    {
        var email = "test@test.com";
        var token = "token";

        _mockUserManager.Setup(x => x.Users)
            .Returns(new List<User> { new User { Email = email, Role = Role.User } }.AsQueryable().BuildMock());
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(It.IsAny<User>(), token)).ReturnsAsync(IdentityResult.Success);

        var result = await _userService.ConfirmEmailAsync(email, token);

        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsConflict_WhenFailed()
    {
        var email = "test@test.com";
        var token = "token";

        _mockUserManager.Setup(x => x.Users)
            .Returns(new List<User> { new User { Email = email, Role = Role.User } }.AsQueryable().BuildMock());
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(It.IsAny<User>(), token))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

        var result = await _userService.ConfirmEmailAsync(email, token);

        Assert.True(result.IsSome);
        Assert.IsType<ConflictError>(result.AsSome.Value);
    }

    // Helper mocks for repeated setups
    private void SetCurrentUserRole(bool isAdmin)
    {
        var context = BuildHttpContext(Guid.NewGuid(), isAdmin ? Role.Admin : Role.User);
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
    }

    private void _mockUserService_GetCurrentUserAsync_ReturnsValidUser()
    {
        var id = Guid.NewGuid();
        
        var context = BuildHttpContext(id, Role.User);
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
        
        _mockUserManager.Setup(x => x.FindByIdAsync(id.ToString())).ReturnsAsync(new User
        {
            Name = "Valid User",
            Email = "validuser@test.com",
            UserName = "validuser@test.com",
            Id = id,
            Role = Role.User
        });
    }

    private void _mockUserService_GetCurrentUserAsync_ReturnsError()
    {
        var id = Guid.NewGuid();
        var context = BuildHttpContext(id, Role.User);
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
        _mockUserManager.Setup(x => x.FindByIdAsync(id.ToString())).ReturnsAsync((User?)null);
    }

    #region Helper Methods

    private void SeedDatabase()
    {
        _user = new User { UserName = "user@test.com", Email = "user@test.com", Name = "user", Id = Guid.NewGuid(), Role = Role.User };
        _adminUser = new User
        {
            UserName = "admin@test.com",
            Email = "admin@test.com",
            Name = "admin",
            Id = Guid.NewGuid(),
            Role = Role.Admin
        };

        Context.Users.AddRange(_user, _adminUser);
        Context.SaveChanges();
    }

    #endregion

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
