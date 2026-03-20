using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Shared.Blazor.ViewModels;
using VoterSystem.Shared.Dto;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public sealed class LoginTwoFactorComponentTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly Mock<IAuthenticationService> _auth = new();
    private readonly FakeNavigationManager _nav;

    public LoginTwoFactorComponentTests()
    {
        _auth.Setup(x => x.TryAutoLoginAsync()).ReturnsAsync(false);
        _auth.Setup(x => x.LoginAsync(It.IsAny<LoginViewModel>()))
            .ReturnsAsync(LoginAttemptResultDto.TwoFactorRequired(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        _auth.Setup(x => x.CompleteTwoFactorLoginAsync(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "123456"))
            .ReturnsAsync(true);

        _ctx.Services.AddSingleton(_auth.Object);
        _nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Login_WithTwoFactorChallenge_RendersVerificationForm_AndNavigatesAfterVerify()
    {
        var cut = _ctx.RenderComponent<Login>();

        cut.Find("#email").Change("admin@example.com");
        cut.Find("#password").Change("Password1!");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("verification code", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.Find("#two-factor-code").Change("123456");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() =>
        {
            _auth.Verify(x => x.CompleteTwoFactorLoginAsync(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "123456"), Times.Once);
            Assert.Equal("http://localhost/votings", _nav.Uri);
        });
    }
}
