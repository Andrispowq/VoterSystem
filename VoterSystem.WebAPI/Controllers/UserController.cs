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
        
        var tokens = result.GetValueOrThrow();
        Response.Cookies.Append(TokenIssuer.CookieTokenName, tokens.AuthToken);
        
        return result.ToHttpResult();
    }
    
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user.IsError) return user.GetErrorOrThrow().ToHttpResult();
        var userR = user.GetValueOrThrow();
        
        var userLevels = userService.GetCurrentUserRoles();
        if (userLevels.IsError) return user.GetErrorOrThrow().ToHttpResult();
        var userLevelsR = userLevels.GetValueOrThrow();

        var ret = new UserDto(userR) { Role = userLevelsR.FirstOrDefault() };
        return Ok(ret);
    }

    [Authorize]
    [HttpDelete("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(NotFoundError))]
    public async Task<IActionResult> LogoutAsync()
    {
        Response.Cookies.Delete(TokenIssuer.CookieTokenName);
        return (await userService.LogoutAsync()).ToHttpResult();
    }

    [Authorize("AdminOnly")]
    [HttpPatch("promote")]
    public async Task<IActionResult> PromoteToAdminAsync([FromQuery] Guid userId)
    {
        var current = userService.GetCurrentUserId();
        if (current.IsError) return current.GetErrorOrThrow().ToHttpResult();
        if (userId == current.GetValueOrThrow())
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
        if (current.IsError) return current.GetErrorOrThrow().ToHttpResult();
        if (userId == current.GetValueOrThrow())
        {
            return BadRequest("Can not demote yourself");
        }

        return (await userService.SetUserRoleAsync(userId, Role.User)).ToHttpResult();
    }
}