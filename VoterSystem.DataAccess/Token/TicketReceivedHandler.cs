using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoterSystem.Shared.Dto;

namespace VoterSystem.DataAccess.Token;

public sealed class TicketReceivedHandler(ILogger<ExternalLoginProvider> logger)
{
    public static readonly CookieAuthenticationEvents Events = new()
    {
        OnRedirectToLogin = OnRedirect(StatusCodes.Status401Unauthorized),
        OnRedirectToAccessDenied = OnRedirect(StatusCodes.Status403Forbidden)
    };

    public async Task HandleAsync(TicketReceivedContext context, ExternalLoginProvider provider, CancellationToken ct = default)
    {
        if (context.Principal is null)
        {
            RedirectToFrontend(context, 404, "Principal is null");
            return;
        }

        var userService = context.HttpContext
            .RequestServices
            .GetRequiredService<IExternalUserService>();
        var tokenRequestService = context.HttpContext
            .RequestServices
            .GetRequiredService<ITokenRequestService>();

        var claims = context.Principal.Claims.ToList();
        var id = GetClaim(claims, ClaimTypes.NameIdentifier, provider);
        var email = GetClaim(claims, ClaimTypes.Email, provider);
        var name = GetClaim(claims, ClaimTypes.Name, provider);
        
        var result = await userService.HandleExternalAuthAsync(
            new ThirdPartyAuthRequest
            {
                Provider = provider,
                Name = name,
                Email = email,
                ProviderKey = id
            }, ct);

        if (result.IsError)
        {
            RedirectToFrontend(context, 400, result.Error.ToString());
            return;
        }

        var tokens = new TokensDto
        {
            AuthToken = result.Value.AuthToken,
            RefreshToken = result.Value.RefreshToken,
            UserId = result.Value.UserId,
        };

        var tokenResult = await tokenRequestService.
            CreateRequestableTokensAsync(tokens, ct);
        if (tokenResult.IsError)
        {
            RedirectToFrontend(context, 400, tokenResult.Error.ToString());
            return;
        }
        
        RedirectToFrontend(context, 200, "Logged in successfully", tokenResult.Value);
    }

    private string GetClaim(List<Claim> claims, string type, ExternalLoginProvider provider)
    {
        var claim = claims.FirstOrDefault(c => c.Type == type);
        if (claim is not null) return claim.Value;

        if (type == ClaimTypes.Name)
        {
            var compositeName = BuildNameFromClaims(claims);
            if (!string.IsNullOrWhiteSpace(compositeName))
            {
                return compositeName;
            }
        }

        if (type == ClaimTypes.Email)
        {
            var upn = claims.FirstOrDefault(c => c.Type == ClaimTypes.Upn)?.Value;
            if (!string.IsNullOrWhiteSpace(upn))
            {
                return upn;
            }

            var nameId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(nameId))
            {
                return nameId;
            }
        }
        
        logger.LogWarning("TicketReceivedHandler: {Type} can not be claimed for provider {Provider}", type, provider);
        throw new MissingFieldException("Name missing");
    }

    private static string? BuildNameFromClaims(List<Claim> claims)
    {
        var givenName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        var surname = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;

        var parts = new[] { givenName, surname }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return parts.Length == 0 ? null : string.Join(' ', parts);
    }

    private static Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirect(int code)
    {
        return ctx =>
        {
            if (IsApiRequest(ctx.Request))
            {
                ctx.Response.StatusCode = code;
                return Task.CompletedTask;
            }

            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    }
    
    private static void RedirectToFrontend(TicketReceivedContext context, int code, string message, Guid? key = null)
    {
        //TODO: decide this
        var frontend = "https://localhost:6901";
        if (context.Properties is not null &&
            context.Properties.Items.TryGetValue("frontend", out var frontendType) && 
            frontendType is not null)
        {
            frontend = frontendType switch
            {
                "admin" => Environment.GetEnvironmentVariable("ADMIN_HTTPS") ?? "https://localhost:6912",
                "user" => Environment.GetEnvironmentVariable("WEB_HTTPS") ?? "https://localhost:6911",
                "mobile" => Environment.GetEnvironmentVariable("MOBILE_LINK") ?? "com.akmeczo.votersystem://",
                _ => frontend
            };
        }
        
        var url = $"{frontend}/signin-callback?code={code}&message={Uri.EscapeDataString(message)}";
        if (key.HasValue) url += "&key=" + key.Value;
        
        context.Response.Redirect(url);
        context.HandleResponse();
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api");
    }
}
