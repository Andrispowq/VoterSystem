using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public sealed class SettingsPageComponentTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly Mock<IAuthenticationService> _auth = new();

    public SettingsPageComponentTests()
    {
        _auth.SetupSequence(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new UserDto
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Email = "admin@example.com",
                EmailConfirmed = true,
                TwoFactorEnabled = false,
                Role = Role.Admin,
                Participations = []
            })
            .ReturnsAsync(new UserDto
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Email = "admin@example.com",
                EmailConfirmed = true,
                TwoFactorEnabled = true,
                Role = Role.Admin,
                Participations = []
            });
        _auth.Setup(x => x.EnableTwoFactorAsync()).ReturnsAsync(true);

        _ctx.Services.AddSingleton(_auth.Object);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void EnableTwoFactor_Click_CallsService_AndRefreshesBadge()
    {
        var cut = _ctx.RenderComponent<Settings>();

        cut.Find("button.btn-outline-secondary").Click();

        cut.WaitForAssertion(() =>
        {
            _auth.Verify(x => x.EnableTwoFactorAsync(), Times.Once);
            Assert.Contains("Two-factor authentication set up", cut.Markup);
        });
    }
}
