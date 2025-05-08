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
        //Default values for now, will be overwritten in prod
        if (Environment.GetEnvironmentVariable("API_HTTPS") is null)
            Environment.SetEnvironmentVariable("API_HTTPS", "https://localhost:6910");
        if (Environment.GetEnvironmentVariable("WEB_HTTPS") is null)
            Environment.SetEnvironmentVariable("WEB_HTTPS", "https://localhost:6911");
        if (Environment.GetEnvironmentVariable("ADMIN_HTTPS") is null)
            Environment.SetEnvironmentVariable("ADMIN_HTTPS", "https://localhost:6912");
        
        var appConfig = services.BindWithEnvSubstitution<AppConfig>(config, "AppConfig");

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
        
        RedirectUrls.WebBaseUrl = Utils.ReplaceFromEnv(config["WebUrl"] ?? "localhost");
        RedirectUrls.AdminBaseUrl = Utils.ReplaceFromEnv(config["AdminBaseUrl"] ?? "localhost");

        services.AddScoped(_ => new HttpClient { BaseAddress = url });
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IHttpRequestUtility, HttpRequestUtility>();
        services.AddScoped<IVotingsService, VotingsService>();
        
        services.AddScoped<NetworkService>();

        return services;
    }
    
    private static T BindWithEnvSubstitution<T>(this IServiceCollection services, IConfiguration config,
        string sectionName)
        where T : class, new()
    {
        var section = config.GetSection(sectionName);
        var raw = new ConfigurationBuilder()
            .AddInMemoryCollection(section.AsEnumerable()
                .Where(pair => pair.Value is not null)
                .Select(pair => new KeyValuePair<string, string>(
                    pair.Key,
                    Utils.ReplaceFromEnv(pair.Value!)))!)
            .Build();

        var instance = new T();
        raw.Bind(sectionName, instance);

        services.Configure<T>(options =>
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.CanWrite)
                {
                    prop.SetValue(options, prop.GetValue(instance));
                }
            }
        });

        return instance;
    }
}