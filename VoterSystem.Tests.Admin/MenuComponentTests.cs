using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Web.Admin.Layout;

namespace VoterSystem.Tests.Admin;

public class MenuComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();
    // https://bunit.dev/docs/test-doubles/fake-navigation-manager.html
    private readonly FakeNavigationManager _fakeNavigationManager;

    public MenuComponentTests()
    {
        // Setup mocks and register services
        _authenticationServiceMock
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("admin");
        
        _context.Services.AddSingleton(_authenticationServiceMock.Object);
        _fakeNavigationManager = _context.Services.GetRequiredService<FakeNavigationManager>();
    }

    public void Dispose() => _context.Dispose();
    
    [Fact]
    public void Navbar_WhenUserIsAuthenticated_ShouldShowFullMenu()
    {
        var cut = _context.RenderComponent<MenuComponent>();
        
        var brand = cut.Find(".navbar-brand");
        Assert.Equal("Voter Admin", brand.TextContent);

        var collapseMenu = cut.Find(".navbar .container .navbar-collapse");
        Assert.NotEmpty(collapseMenu.Children);
        
        var welcomeText = cut.Find(".navbar .container .navbar-text");
        Assert.Equal("Welcome, admin!", welcomeText.TextContent);
        
        var logoutButton = cut.Find("[data-testid='logout']");
        Assert.Equal("Logout", logoutButton.TextContent.Trim());
    }
    
    [Fact]
    public void LogoutButton_WhenClicked_ShouldCallLogoutAndRedirectToLoginPage()
    {
        // Arrange
        var cut = _context.RenderComponent<MenuComponent>();
        
        // Act
        var logoutButton = cut.Find("[data-testid='logout']");
        
        // Assert
        logoutButton.Click();
        // https://bunit.dev/docs/verification/async-assertion.html
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("http://localhost/login", _fakeNavigationManager.Uri);
            _authenticationServiceMock.Verify(x => x.LogoutAsync(), Times.Once);
        });
    }
}