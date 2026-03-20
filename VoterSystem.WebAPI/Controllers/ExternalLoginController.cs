using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class ExternalLoginController(
    ITokenRequestService tokenRequestService) : ControllerBase
{
    [HttpGet("external-login/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status307TemporaryRedirect)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ExternalLoginAsync([FromRoute] ExternalLoginProvider provider,
        [FromQuery] string frontend)
    {
        if (frontend is not ("admin" or "user"))
        {
            return BadRequest("Error: frontend query param must be set to 'admin' or 'user'");
        }
        
        var url = provider switch
        {
            ExternalLoginProvider.Google => nameof(ExternalLoginCallbackGoogle),
            //ExternalLoginProvider.Facebook => nameof(ExternalLoginCallbackFacebook),
            _ => null
        };

        if (url is null)
        {
            return new BadRequestError("Invalid provider option").ToHttpResult();
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(url),
            Items = { new KeyValuePair<string, string?>("frontend", frontend) }
        };

        return Challenge(props, provider.ToString());
    }

    [HttpGet("external-callback-google")]
    [ProducesResponseType<TokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ExternalLoginCallbackGoogle()
    {
        return NoContent();
    }

    [HttpGet("external-callback-facebook")]
    [ProducesResponseType<TokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ExternalLoginCallbackFacebook()
    {
        return NoContent();
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
}