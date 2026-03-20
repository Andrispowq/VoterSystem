using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VoterSystem.DataAccess.Config;
using VoterSystem.Shared.Dto;

namespace VoterSystem.DataAccess.Token;

public static class DependencyInjection
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = services.BindWithEnvSubstitution<JwtSettings>(configuration, "JwtSettings");

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ExternalScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = jwtSettings.Audience,
                ValidIssuer = jwtSettings.Issuer,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.ContainsKey(TokenIssuerKeys.AuthTokenKey))
                    {
                        context.Token = context.Request.Cookies[TokenIssuerKeys.AuthTokenKey];
                    }
                    else
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/Hubs"))
                        {
                            context.Token = accessToken;
                        }
                    }

                    return Task.CompletedTask;
                }
            };
        });

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "IntegrationTest")
        {
            authBuilder.AddGoogle(nameof(ExternalLoginProvider.Google), opts =>
            {
                var clientId = Environment.GetEnvironmentVariable("OAUTH_GOOGLE_CLIENT_ID") ??
                               configuration["Authorisation:Google:ClientId"] ?? "dummyId";

                var clientSecret = Environment.GetEnvironmentVariable("OAUTH_GOOGLE_CLIENT_SECRET") ??
                                   configuration["Authorisation:Google:ClientSecret"] ?? "dummySecret";

                opts.ClientId = clientId;
                opts.ClientSecret = clientSecret;
                opts.SignInScheme = IdentityConstants.ExternalScheme;
                opts.CallbackPath = "/api/v1/users/external-callback-google";

                opts.CorrelationCookie.SameSite = SameSiteMode.None;
                opts.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opts.CorrelationCookie.Path = "/";
                opts.CorrelationCookie.HttpOnly = true;

                opts.Events.OnTicketReceived = context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<TicketReceivedHandler>();
                    return handler.HandleAsync(context, ExternalLoginProvider.Google);
                };
            }).AddFacebook(nameof(ExternalLoginProvider.Facebook), opts =>
            {
                var clientId = Environment.GetEnvironmentVariable("OAUTH_FACEBOOK_CLIENT_ID")
                               ?? configuration["Authorisation:Facebook:ClientId"] ?? "dummyId";

                var clientSecret = Environment.GetEnvironmentVariable("OAUTH_FACEBOOK_CLIENT_SECRET")
                                   ?? configuration["Authorisation:Facebook:ClientSecret"] ?? "dummySecret";

                opts.ClientId = clientId;
                opts.ClientSecret = clientSecret;
                opts.SignInScheme = IdentityConstants.ExternalScheme;
                opts.CallbackPath = "/api/v1/users/external-callback-facebook";

                opts.Fields.Add("name");
                opts.Fields.Add("email");

                opts.CorrelationCookie.SameSite = SameSiteMode.None;
                opts.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opts.CorrelationCookie.Path = "/";
                opts.CorrelationCookie.HttpOnly = true;

                opts.Events.OnTicketReceived = context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<TicketReceivedHandler>();
                    return handler.HandleAsync(context, ExternalLoginProvider.Facebook);
                };
            });
        }

        services.ConfigureExternalCookie(opts =>
        {
            opts.Cookie.SameSite = SameSiteMode.None;
            opts.Cookie.SecurePolicy = CookieSecurePolicy.None;
            opts.Events = TicketReceivedHandler.Events;
        });

        return services.ConfigureApplicationCookie(options =>
        {
            options.Events = TicketReceivedHandler.Events;
        });
    }
}