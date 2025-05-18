using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public sealed class PasswordResetPageTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly Mock<IAuthenticationService> _auth = new();
    private readonly FakeNavigationManager _nav;

    public PasswordResetPageTests()
    {
        _ctx.Services.AddSingleton(_auth.Object);
        _nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void RendersExpectedHeadline()
    {
        var uri = _nav.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["email"] = "test@test.com",
            ["code"] = "test",
        });
        _nav.NavigateTo(uri);
        
        var cut = _ctx.RenderComponent<PasswordResetPage>();
        Assert.Equal("Reset Password", cut.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void SubmitButtonDisabledUntilAllFieldsValid()
    { 
        var uri = _nav.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["email"] = "test@test.com",
            ["code"] = "test",
        });
        _nav.NavigateTo(uri);

        var cut = _ctx.RenderComponent<PasswordResetPage>();
        _ = cut.Find("button[type='submit']");
    }

    [Fact]
    public void SuccessfulResetCallsServiceAndRedirectsToLogin()
    {
        _auth.Setup(s => s.ResetPasswordAsync(It.IsAny<UserPasswordResetRequestDto>()))
             .ReturnsAsync(true);
        
        var uri = _nav.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["email"] = "test@test.com",
            ["code"] = "test",
        });
        _nav.NavigateTo(uri);

        var cut = _ctx.RenderComponent<PasswordResetPage>();

        cut.Find("#password").Change("OkP@ss111");
        cut.Find("#password-confirm").Change("OkP@ss111");
        Assert.False(cut.Find("button[type='submit']").HasAttribute("disabled"));
        
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() =>
        {
            _auth.Verify(s =>
                s.ResetPasswordAsync(
                    It.Is<UserPasswordResetRequestDto>(d =>
                        d.Email == "test@test.com" && d.Token == "test" && d.NewPassword == "OkP@ss111")),
                Times.Once);
        });
    }
}