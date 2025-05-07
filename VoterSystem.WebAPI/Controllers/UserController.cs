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
        
        var result = await userService.CreateUser(user, request.Password, newRole);
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
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsersAsync()
    {
        var users = await userService.GetAllUsersAsync();
        if (users.IsError) return users.Error.ToHttpResult();
        
        List<UserDto> userDtos = new List<UserDto>();
        foreach (var user in users.Value)
        {
            var result = await userService.GetUserRoleByIdAsync(user.Id);
            var role = result.IsError ? Role.User : result.Value;
            
            userDtos.Add(new UserDto(user)
            {
                Role = role
            });
        }
        
        return Ok(userDtos);
    }
    
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user.IsError) return user.Error.ToHttpResult();
        var userR = user.Value;
        
        var userLevels = userService.GetCurrentUserRole();
        if (userLevels.IsError) return user.Error.ToHttpResult();
        var userLevelsR = userLevels.Value;

        var ret = new UserDto(userR) { Role = userLevelsR };
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
        
        var userLevels = userService.GetCurrentUserRole();
        if (userLevels.IsError) return user.Error.ToHttpResult();
        var userLevelsR = userLevels.Value;

        var ret = new UserDto(userR) { Role = userLevelsR };
        return Ok(ret);
    }

    [Authorize]
    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] UserChangePasswordRequestDto dto)
    {
        var changePassword = await userService.ChangePasswordAsync(dto.OldPassword, dto.NewPassword);
        return changePassword.IsSome
            ? changePassword.ToHttpResult()
            : Ok();
    }

    [Authorize]
    [HttpPost("confirm-email")]
    public Task<IActionResult> RequestEmailConfirm()
    {
        throw new NotImplementedException();
    }

    [Authorize]
    [HttpPost("confirm-email/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail(string token)
    {
        var confirm = await userService.ConfirmEmailAsync(token);
        return confirm.IsSome
            ? confirm.ToHttpResult()
            : Ok();
    }

    [Authorize]
    [HttpPost("reset-password")]
    public Task<IActionResult> RequestPasswordReset()
    {
        throw new NotImplementedException();
    }

    [Authorize]
    [HttpPost("reset-password/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(string token, [FromBody] string newPassword)
    {
        var confirm = await userService.ResetPasswordAsync(token, newPassword);
        return confirm.IsSome
            ? confirm.ToHttpResult()
            : Ok();
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
        return userId == current.Value 
            ? BadRequest("Can not promote yourself") 
            : (await userService.SetUserRoleAsync(userId, Role.Admin)).ToHttpResult();
    }

    [Authorize("AdminOnly")]
    [HttpPatch("demote")]
    public async Task<IActionResult> DemoteToUserAsync([FromQuery] Guid userId)
    {
        var current = userService.GetCurrentUserId();
        if (current.IsError) return current.Error.ToHttpResult();
        return userId == current.Value 
            ? BadRequest("Can not demote yourself") 
            : (await userService.SetUserRoleAsync(userId, Role.User)).ToHttpResult();
    }
    
    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Tokens))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] Guid refreshToken)
    {
        return (await userService.RedeemRefreshTokenAsync(refreshToken)).ToHttpResult();
    }
}