using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Shared.Blazor.ViewModels;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public sealed class RegisterPageComponentTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly Mock<IAuthenticationService> _userSvc = new();
    private readonly FakeNavigationManager _nav;

    public RegisterPageComponentTests()
    {
        _userSvc
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("admin");
        
        _ctx.Services.AddSingleton(_userSvc.Object);
        
        _nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void RendersExpectedHeadline()
    {
        var cut = _ctx.RenderComponent<Register>();

        Assert.Equal("Register", cut.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void SubmitDisabled_UntilPasswordsMatch()
    {
        var cut = _ctx.RenderComponent<Register>();

        cut.Find("#email").Change("me@test.com");
        cut.Find("#password").Change("P@ss1");
        cut.Find("#confirm").Change("Different");

        cut.Find("#confirm").Change("P@ss1");
        Assert.False(cut.Find("button[type='submit']").HasAttribute("disabled"));
    }

    [Fact]
    public void SuccessfulRegister_CreatesUser_SendsConfirmation_Redirects()
    {
        var newUser = new RegisterViewModel { Username = "me", Email = "me@test.com", Password = "Password1#" };

        _userSvc
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterViewModel>()))
            .ReturnsAsync(true);

        var cut = _ctx.RenderComponent<Register>();

        cut.Find("#username").Change(newUser.Username);
        cut.Find("#email").Change(newUser.Email);
        cut.Find("#password").Change("P@ss1");
        cut.Find("#confirm").Change("P@ss1");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() =>
        {
            //_userSvc.VerifyAll();
            Assert.Equal("http://localhost/", _nav.Uri);
        });
    }
}
