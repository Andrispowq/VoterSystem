using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/users")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAsync([FromBody] UserRegisterRequestDto request)
    {
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            UserName = request.Email
        };

        //If there are no admins, add one (this will be the first user)
        //After that, a user has to be promoted by an admin
        var hasAdmin = await userService.AnyAdmins();
        var newRole = hasAdmin ? Role.User : Role.Admin;
        
        var result = await userService.AddUserAsync(user, request.Password, newRole);
        if (result.IsSome) return result.ToHttpResult();
        
        return Created();
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Tokens))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginRequestDto request)
    {
        var result = await userService.LoginAsync(request.Email, request.Password);
        if (result.IsError) return result.ToHttpResult();
        
        var tokens = result.Value;
        Response.Cookies.Append(TokenIssuer.AuthTokenKey, tokens.AuthToken);
        Response.Cookies.Append(TokenIssuer.RefreshTokenName, tokens.RefreshToken.ToString());
        
        return result.ToHttpResult();
    }
    
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user.IsError) return user.Error.ToHttpResult();
        var userR = user.Value;
        
        var userLevels = userService.GetCurrentUserRoles();
        if (userLevels.IsError) return user.Error.ToHttpResult();
        var userLevelsR = userLevels.Value;

        var ret = new UserDto(userR) { Role = userLevelsR.FirstOrDefault() };
        return Ok(ret);
    }
    
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserByIdAsync(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user.IsError) return user.Error.ToHttpResult();
        var userR = user.Value;
        
        var userLevels = userService.GetCurrentUserRoles();
        if (userLevels.IsError) return user.Error.ToHttpResult();
        var userLevelsR = userLevels.Value;

        var ret = new UserDto(userR) { Role = userLevelsR.FirstOrDefault() };
        return Ok(ret);
    }

    [Authorize]
    [HttpDelete("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(NotFoundError))]
    public async Task<IActionResult> LogoutAsync()
    {
        Response.Cookies.Delete(TokenIssuer.AuthTokenKey);
        Response.Cookies.Delete(TokenIssuer.RefreshTokenName);
        return (await userService.LogoutAsync()).ToHttpResult();
    }

    [Authorize("AdminOnly")]
    [HttpPatch("promote")]
    public async Task<IActionResult> PromoteToAdminAsync([FromQuery] Guid userId)
    {
        var current = userService.GetCurrentUserId();
        if (current.IsError) return current.Error.ToHttpResult();
        if (userId == current.Value)
        {
            return BadRequest("Can not promote yourself");
        }
        
        return (await userService.SetUserRoleAsync(userId, Role.Admin)).ToHttpResult();
    }

    [Authorize("AdminOnly")]
    [HttpPatch("demote")]
    public async Task<IActionResult> DemoteToUserAsync([FromQuery] Guid userId)
    {
        var current = userService.GetCurrentUserId();
        if (current.IsError) return current.Error.ToHttpResult();
        if (userId == current.Value)
        {
            return BadRequest("Can not demote yourself");
        }

        return (await userService.SetUserRoleAsync(userId, Role.User)).ToHttpResult();
    }
    
    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Tokens))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshTokenAsync()
    {
        var token = Request.Cookies.FirstOrDefault(t => t.Key == TokenIssuer.RefreshTokenName).Value;
        if (token is null) return Unauthorized();

        if (!Guid.TryParse(token, out var refreshToken))
        {
            return BadRequest("Refresh token is not a Guid");
        }

        return (await userService.RedeemRefreshTokenAsync(refreshToken)).ToHttpResult();
    }
}