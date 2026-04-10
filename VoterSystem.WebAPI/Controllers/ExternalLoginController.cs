using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class ExternalLoginController(
    IExternalUserService externalUserService,
    ITokenRequestService tokenRequestService,
    ILogger<ExternalLoginController> logger) : ControllerBase
{
    [HttpGet("external-login/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status307TemporaryRedirect)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ExternalLogin([FromRoute] ExternalLoginProvider provider,
        [FromQuery] string frontend)
    {
        if (frontend is not ("admin" or "user" or "mobile"))
        {
            return BadRequest("Error: frontend query param must be set to 'admin' or 'user' or 'mobile'");
        }

        var url = provider switch
        {
            ExternalLoginProvider.Google => nameof(ExternalLoginCallbackGoogle),
            ExternalLoginProvider.Facebook => nameof(ExternalLoginCallbackFacebook),
            ExternalLoginProvider.Saml => nameof(ExternalLoginCallbackSaml),
            _ => null
        };
        
        logger.LogWarning("OAuth return url for provider {Provider} is {Url}", provider, url);

        if (url is null)
        {
            return new BadRequestError("Invalid provider option").ToHttpResult();
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(url),
            Items = { new KeyValuePair<string, string?>("frontend", frontend) }
        };
        
        logger.LogWarning("OAuth properties are: redirect url: {RedirectUrl}, frontend: {Frontend}", 
            props.RedirectUri, props.Items["frontend"]);

        return Challenge(props, provider.ToString());
    }

    [HttpGet("external-callback-google")]
    [AllowAnonymous]
    [ProducesResponseType<TokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ExternalLoginCallbackGoogle()
    {
        return NoContent();
    }

    [HttpGet("external-callback-facebook")]
    [AllowAnonymous]
    [ProducesResponseType<TokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ExternalLoginCallbackFacebook()
    {
        return NoContent();
    }

    [HttpGet("external-callback-saml")]
    [HttpPost("external-callback-saml")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExternalLoginCallbackSaml(
        [FromServices] IAuthenticationSchemeProvider schemes,
        CancellationToken ct = default)
    {
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return RedirectToFrontend("user", 400, "External SAML sign-in cookie was not available.");
            //return Unauthorized("External SAML sign-in cookie was not available.");
        }

        var principal = authResult.Principal;
        var frontend = authResult.Properties?.Items.TryGetValue("frontend", out var f) == true
            ? f ?? "user"
            : "user";
        
        var claims = principal.Claims.ToList();
        var id = GetClaim(claims, ClaimTypes.NameIdentifier, ExternalLoginProvider.Saml);
        var email = GetClaim(claims, "email", ExternalLoginProvider.Saml);
        var name = email;//GetClaim(claims, ClaimTypes.Name, ExternalLoginProvider.Saml);
        
        var result = await externalUserService.HandleExternalAuthAsync(
            new ThirdPartyAuthRequest
            {
                Provider = ExternalLoginProvider.Saml,
                Name = name,
                Email = email,
                ProviderKey = id
            });

        if (result.IsError)
        {
            return RedirectToFrontend(frontend, 400, result.Error.ToString());
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
            return RedirectToFrontend(frontend, 400, tokenResult.Error.ToString());
        }
        
        return RedirectToFrontend(frontend, 200, "Logged in successfully", tokenResult.Value);
    }
    
    private IActionResult RedirectToFrontend(string frontendType, int code, string message, Guid? key = null)
    {
        //TODO: decide this
        var frontend = frontendType switch
        {
            "admin" => Environment.GetEnvironmentVariable("ADMIN_HTTPS") ?? "https://localhost:6912",
            "user" => Environment.GetEnvironmentVariable("WEB_HTTPS") ?? "https://localhost:6911",
            "mobile" => Environment.GetEnvironmentVariable("MOBILE_LINK") ?? "com.akmeczo.votersystem:/",
            _ => "https://localhost:6901"
        };
        
        var url = $"{frontend}/signin-callback?code={code}&message={Uri.EscapeDataString(message)}";
        if (key.HasValue) url += "&key=" + key.Value;

        return Redirect(url);
    }
    
    [AllowAnonymous]
    [HttpPost("request-signin-tokens")]
    [ProducesResponseType<TokensDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSigninTokensAsync(
        Guid id, CancellationToken ct = default)
    {
        var result = await tokenRequestService.RequestTokensAsync(id, ct);
        if (result.IsError) return result.ToHttpResult();
        
        Response.Cookies.Append(TokenIssuerKeys.AuthTokenKey, result.Value.AuthToken);
        Response.Cookies.Append(TokenIssuerKeys.RefreshTokenKey, result.Value.RefreshToken.ToString());
        Response.Cookies.Append(TokenIssuerKeys.UserIdKey, result.Value.UserId.ToString());

        return result.ToHttpResult();
    }

    private string GetClaim(List<Claim> claims, string type, ExternalLoginProvider provider)
    {
        var claim = claims.FirstOrDefault(c => c.Type == type);
        if (claim is not null) return claim.Value;

        switch (type)
        {
            case ClaimTypes.Name:
            {
                var compositeName = BuildNameFromClaims(claims);
                if (!string.IsNullOrWhiteSpace(compositeName))
                {
                    return compositeName;
                }

                break;
            }
            case ClaimTypes.Email:
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

                break;
            }
        }

        logger.LogWarning("TicketReceivedHandler: {Type} can not be claimed for provider {Provider}", type, provider);
        //throw new MissingFieldException("Claim missing");
        return string.Empty;
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
}
