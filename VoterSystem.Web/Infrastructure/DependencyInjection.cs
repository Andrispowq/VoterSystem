using VoterSystem.Web.Config;

namespace VoterSystem.Web.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBlazorServices(this IServiceCollection services, IConfiguration config)
    {
        AdminRedirect.BaseUrl = config["AdminUrl"] ?? "localhost";
        return services;
    }
}