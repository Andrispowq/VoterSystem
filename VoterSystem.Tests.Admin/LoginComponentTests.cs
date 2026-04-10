using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public class LoginComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();

    public LoginComponentTests()
    {
        // Setup mocks and register services
        _authenticationServiceMock
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("admin");
        
        _context.Services.AddSingleton(_authenticationServiceMock.Object);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void Login_RendersFormInputs()
    {
        var cut = _context.RenderComponent<Login>();

        var emailInput = cut.Find("#email");
        Assert.NotNull(emailInput);

        var passwordInput = cut.Find("input[type='password']");
        Assert.NotNull(passwordInput);

        var loginButton = cut.Find("button[type='submit']");
        Assert.NotNull(loginButton);
    }

    [Fact]
    public void Login_WhenRendered_ShouldContainLoginText()
    {
        var cut = _context.RenderComponent<Login>();
        Assert.Contains("Login", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}