using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public class EmailConfirmPageComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();

    public EmailConfirmPageComponentTests()
    {
        // Setup mocks and register services
        _authenticationServiceMock
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("admin");
        
        _context.Services.AddSingleton(_authenticationServiceMock.Object);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void EmailConfirmPage_WhenRendered_ShouldContainConfirmationMessage()
    {
        var cut = _context.RenderComponent<EmailConfirmPage>();

        Assert.Contains("Email Confirmation", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}