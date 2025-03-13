using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.DataAccess.Services;

namespace VoterSystem.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration config)
    {
        // Database
        var connectionString = config.GetConnectionString("VoterSystemConnection");
        services.AddDbContext<VoterSystemDbContext>(options => options
            .UseSqlite(connectionString)
            .UseLazyLoadingProxies()
        );
        
        // Services
        services.AddScoped<IUserService, UserService>();

        //services.AddSingleton<IEmailService, SmtpEmailService>();

        return services;
    }
}