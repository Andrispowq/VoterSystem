using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace VoterSystem.Tests.Integration.Abstractions;

public class BaseTest(TestWebAppFactory factory)
{
    protected HttpClient HttpClient { get; private set; } = factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });
    protected readonly IServiceScope Scope = factory.Services.CreateScope();
    protected readonly TestWebAppFactory Factory = factory;
}