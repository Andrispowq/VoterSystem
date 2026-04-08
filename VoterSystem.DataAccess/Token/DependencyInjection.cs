using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Metadata;
using VoterSystem.DataAccess.Config;
using VoterSystem.Shared.Dto;

namespace VoterSystem.DataAccess.Token;

public static class DependencyInjection
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = services.BindWithEnvSubstitution<JwtSettings>(configuration, "JwtSettings");
        var samlSettings = services.BindWithEnvSubstitution<SamlSettings>(configuration, "Authorisation:Saml");

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

            if (samlSettings.Enabled)
            {
                ConfigureSaml(authBuilder, samlSettings);
            }
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

    private static void ConfigureSaml(AuthenticationBuilder authBuilder, SamlSettings samlSettings)
    {
        authBuilder.AddSaml2(nameof(ExternalLoginProvider.Saml), options =>
        {
            options.SignInScheme = IdentityConstants.ExternalScheme;
            //options.CallbackPath = samlSettings.CallbackPath;

            if (string.IsNullOrWhiteSpace(samlSettings.ServiceProviderEntityId))
            {
                throw new InvalidOperationException("Authorisation:Saml:ServiceProviderEntityId must be configured when SAML authentication is enabled.");
            }

            if (string.IsNullOrWhiteSpace(samlSettings.IdentityProviderEntityId))
            {
                throw new InvalidOperationException("Authorisation:Saml:IdentityProviderEntityId must be configured when SAML authentication is enabled.");
            }

            options.SPOptions.EntityId = new EntityId(samlSettings.ServiceProviderEntityId);
            //options.SPOptions.DefaultSubjectNameIdType = Saml2NameIdentifierFormat.EmailAddressNameIdentifier;
            options.SPOptions.AuthenticateRequestSigningBehavior = SigningBehavior.Never;
            options.SPOptions.WantAssertionsSigned = true;
            //options.SPOptions.MinIncomingSigningAlgorithm = Saml2SecurityAlgorithms.RsaSha256Signature;

            if (!string.IsNullOrWhiteSpace(samlSettings.PublicOrigin))
            {
                options.SPOptions.PublicOrigin = new Uri(samlSettings.PublicOrigin, UriKind.Absolute);
            }

            if (!string.IsNullOrWhiteSpace(samlSettings.ReturnUrl))
            {
                options.SPOptions.ReturnUrl = new Uri(samlSettings.ReturnUrl, UriKind.Absolute);
            }
            else if (!string.IsNullOrWhiteSpace(samlSettings.PublicOrigin))
            {
                options.SPOptions.ReturnUrl = new Uri($"{samlSettings.PublicOrigin.TrimEnd('/')}{samlSettings.CallbackPath}", UriKind.Absolute);
            }
            else
            {
                throw new InvalidOperationException("Authorisation:Saml:PublicOrigin or ReturnUrl must be configured when SAML authentication is enabled.");
            }

            if (!string.IsNullOrWhiteSpace(samlSettings.SigningCertificateBase64))
            {
                var certData = Convert.FromBase64String(samlSettings.SigningCertificateBase64);
                var certificate = string.IsNullOrWhiteSpace(samlSettings.SigningCertificatePassword)
                    ? new X509Certificate2(certData)
                    : new X509Certificate2(certData, samlSettings.SigningCertificatePassword);

                options.SPOptions.ServiceCertificates.Add(certificate);

                if (samlSettings.SignAuthnRequests)
                {
                    options.SPOptions.AuthenticateRequestSigningBehavior = SigningBehavior.Always;
                }
            }

            var identityProvider = new IdentityProvider(new EntityId(samlSettings.IdentityProviderEntityId), options.SPOptions)
            {
                AllowUnsolicitedAuthnResponse = samlSettings.AllowUnsolicitedAuthnResponse,
                LoadMetadata = !string.IsNullOrWhiteSpace(samlSettings.MetadataUrl),
                MetadataLocation = samlSettings.MetadataUrl
            };

            if (identityProvider.LoadMetadata)
            {
                if (!string.IsNullOrWhiteSpace(samlSettings.SingleLogoutUrl))
                {
                    identityProvider.SingleLogoutServiceUrl = new Uri(samlSettings.SingleLogoutUrl, UriKind.Absolute);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(samlSettings.SingleSignOnUrl))
                {
                    throw new InvalidOperationException("Authorisation:Saml:SingleSignOnUrl must be configured when metadata loading is disabled.");
                }

                identityProvider.SingleSignOnServiceUrl = new Uri(samlSettings.SingleSignOnUrl, UriKind.Absolute);

                if (!string.IsNullOrWhiteSpace(samlSettings.SingleLogoutUrl))
                {
                    identityProvider.SingleLogoutServiceUrl = new Uri(samlSettings.SingleLogoutUrl, UriKind.Absolute);
                }
            }

            options.IdentityProviders.Add(identityProvider);

            options.Events = (Func<TicketReceivedContext, Task>)Handler;
            options.EventsType = typeof(Func<TicketReceivedContext, Task>);
        });
    }
    
    private static async Task Handler(TicketReceivedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SamlTicket");

        logger.LogWarning("SAML TicketReceived fired. Name={Name}, Scheme={Scheme}",
            context.Principal?.Identity?.Name,
            context.Scheme.Name);

        var handler = context.HttpContext.RequestServices.GetRequiredService<TicketReceivedHandler>();
        await handler.HandleAsync(context, ExternalLoginProvider.Saml);
    }
}
