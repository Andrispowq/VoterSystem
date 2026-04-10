using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public class ForgotPasswordComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();

    public ForgotPasswordComponentTests()
    {
        // Setup mocks and register services
        _authenticationServiceMock
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("admin");
        
        _context.Services.AddSingleton(_authenticationServiceMock.Object);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void ForgotPassword_RendersFormWithEmailInput()
    {
        var cut = _context.RenderComponent<ForgotPassword>();

        var emailInput = cut.Find("#email");
        Assert.NotNull(emailInput);

        var submitButton = cut.Find("button[type='submit']");
        Assert.NotNull(submitButton);
    }

    [Fact]
    public void ForgotPassword_WhenRendered_ShouldContainTitle()
    {
        var cut = _context.RenderComponent<ForgotPassword>();
        Assert.Contains("Forgot Password", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}