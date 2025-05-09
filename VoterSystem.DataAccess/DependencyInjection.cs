using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared;

namespace VoterSystem.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration config)
    {
        // Options
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
        services.Configure<UserSettings>(config.GetSection("UserSettings"));
        
        // Database
        var connectionString = config.GetConnectionString("VoterSystemConnection");
        connectionString = Utils.ReplaceFromEnv(connectionString ?? "");
        services.AddDbContext<VoterSystemDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseLazyLoadingProxies()
        );

        //Identity
        services.AddIdentity<User, UserRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
            
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<VoterSystemDbContext>()
             .AddDefaultTokenProviders();
        
        // Services
        services.AddScoped<ITokenIssuer, TokenIssuer>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IVoteService, VoteService>();
        services.AddScoped<IVotingService, VotingService>();
        services.AddScoped<IVoteChoiceService, VoteChoiceService>();

        services.AddSingleton<IEmailService, EmailService>();

        return services;
    }
    
    public static T BindWithEnvSubstitution<T>(this IServiceCollection services, IConfiguration config,
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