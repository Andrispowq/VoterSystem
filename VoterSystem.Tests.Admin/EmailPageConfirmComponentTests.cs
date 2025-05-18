using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public class EmailConfirmPageComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();
    // https://bunit.dev/docs/test-doubles/fake-navigation-manager.html
    private readonly FakeNavigationManager _fakeNavigationManager;

    public EmailConfirmPageComponentTests()
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
    public void EmailConfirmPage_WhenRendered_ShouldContainConfirmationMessage()
    {
        var cut = _context.RenderComponent<EmailConfirmPage>();

        Assert.Contains("Email Confirmation", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}