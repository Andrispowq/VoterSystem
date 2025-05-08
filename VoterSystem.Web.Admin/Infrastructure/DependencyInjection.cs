using VoterSystem.Web.Admin.Config;

namespace VoterSystem.Web.Admin.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBlazorServices(this IServiceCollection services, IConfiguration config)
    {
        WebRedirect.BaseUrl = config["WebUrl"] ?? "localhost";
        return services;
    }
}