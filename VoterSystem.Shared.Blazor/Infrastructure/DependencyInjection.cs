using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.Shared.Blazor.Config;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Shared.Blazor.Services.SignalR;

namespace VoterSystem.Shared.Blazor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedBlazorServices(this IServiceCollection services, IConfiguration config)
    {
        var appConfig = config.GetSection("AppConfig").Get<AppConfig>()
            ?? throw new NullReferenceException("AppConfig is null");

        services.AddSingleton(appConfig);
        services.AddSingleton<IToastService, ToastService>();

        services.AddScoped<JsonSerializerOptions>(_ =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            return options;
        });

        services.AddBlazoredLocalStorage();

        var urlS = config["ApiBaseUrl"] ?? "-";
        urlS = Utils.ReplaceFromEnv(urlS);
        var url = new Uri(urlS);
        
        RedirectUrls.WebBaseUrl = Utils.ReplaceFromEnv(config["WebBaseUrl"] ?? "localhost");
        RedirectUrls.AdminBaseUrl = Utils.ReplaceFromEnv(config["AdminBaseUrl"] ?? "localhost");

        services.AddScoped(_ => new HttpClient { BaseAddress = url });
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IHttpRequestUtility, HttpRequestUtility>();
        services.AddScoped<IVotingsService, VotingsService>();
        services.AddScoped<IVoteHubService, VoteHubService>();
        
        services.AddScoped<NetworkService>();

        return services;
    }
}