using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Config;
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
            .Setup(x => x.TryAutoLoginAsync())
            .ReturnsAsync(false);
        _authenticationServiceMock
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("admin");
        
        _context.Services.AddSingleton(_authenticationServiceMock.Object);
        _context.Services.AddSingleton(new AppConfig
        {
            ToastDurationInMillis = 3000,
            HubBaseUrl = "http://localhost",
            EnableTestUsers = false
        });
        _context.Services.AddSingleton(CreateJsonOptions());
        _context.Services.AddSingleton(new HttpClient(new SupportedExternalLoginModesHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        });
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void Login_RendersFormInputs()
    {
        var cut = _context.RenderComponent<Login>();

        var emailInput = cut.WaitForElement("#email");
        Assert.NotNull(emailInput);

        var passwordInput = cut.WaitForElement("input[type='password']");
        Assert.NotNull(passwordInput);

        var loginButton = cut.WaitForElement("button[type='submit']");
        Assert.NotNull(loginButton);
    }

    [Fact]
    public void Login_WhenRendered_ShouldContainLoginText()
    {
        var cut = _context.RenderComponent<Login>();
        cut.WaitForElement("#email");
        Assert.Contains("Login", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed class SupportedExternalLoginModesHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""["google","facebook","saml"]""")
            });
    }
}
