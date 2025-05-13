using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;

namespace VoterSystem.Tests.UnitTests;

public class UserServiceTests : UnitTestBase, IDisposable
{
    private readonly UserService _userService;
    private readonly Mock<UserManager<User>> _mockUserManager = new();
    private readonly Mock<SignInManager<User>> _mockSignInManager = new();
    private readonly Mock<ITokenIssuer> _mockTokenIssuer = new();

    private User _user = null!;
    private User _adminUser = null!;

    public UserServiceTests()
    {
        _userService = new UserService(
            new Mock<IHttpContextAccessor>().Object,
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockTokenIssuer.Object
        );

        SeedDatabase();
    }

    #region User Creation

    [Fact]
    public async Task CreateUser_WhenRoleIsGiven_ReturnsNoError()
    {
        // Arrange
        var user = new User { UserName = "user@test.com", Email = "user@test.com" };
        var password = "Password123";
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), password)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), Role.User.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.CreateUser(user, password, Role.User);

        // Assert
        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task CreateUser_WhenCreationFails_ReturnsError()
    {
        // Arrange
        var user = new User { UserName = "user@test.com", Email = "user@test.com" };
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
        string email = "user@test.com";
        string password = "password123";
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(_user);
        _mockSignInManager
            .Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<bool>())).ReturnsAsync(SignInResult.Success);
        _mockTokenIssuer.Setup(x => x.GenerateJwtTokenAsync(It.IsAny<User>(), It.IsAny<UserManager<User>>()))
            .ReturnsAsync("accessToken");

        // Act
        var result = await _userService.LoginAsync(email, password);

        // Assert
        Assert.True(result.HasValue);
        Assert.Equal("accessToken", result.Value.AuthToken);
    }

    #endregion

    #region Helper Methods

    private void SeedDatabase()
    {
        _user = new User { UserName = "user@test.com", Email = "user@test.com", Id = Guid.NewGuid() };
        _adminUser = new User { UserName = "admin@test.com", Email = "admin@test.com", Id = Guid.NewGuid() };

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