using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using ELTE.Cinema.Blazor.WebAssembly.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.Shared.Blazor.Config;
using VoterSystem.Shared.Blazor.Services;

namespace VoterSystem.Shared.Blazor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedBlazorServices(this IServiceCollection services, IConfiguration config)
    {
        var appConfig = config.GetSection("AppConfig").Get<AppConfig>();
        if (appConfig == null)
            throw new ArgumentNullException(nameof(appConfig), "Not exist or wrong appConfig");

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

        var url = new Uri(config["ApiBaseUrl"] ?? "-");

        services.AddScoped(_ => new HttpClient { BaseAddress = url });
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IHttpRequestUtility, HttpRequestUtility>();
        services.AddScoped<IVotingsService, VotingsService>();
        
        services.AddScoped<NetworkService>();

        return services;
    }
}