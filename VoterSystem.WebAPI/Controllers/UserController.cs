using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Config;
using VoterSystem.WebAPI.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/users")]
public class UserController(IUserService userService, IEmailService emailService,
    IOptions<BlazorSettings> blazorSettings) : ControllerBase
{
    private readonly BlazorSettings _blazorSettings = blazorSettings.Value;
    
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

            var dto = user.ToUserDto();
            dto.Role = role;
            userDtos.Add(dto);
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

        var ret = userR.ToUserDto();
        ret.Role = userLevelsR;
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

        var ret = userR.ToUserDto();
        ret.Role = userLevelsR;
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
    [HttpPost("confirm-email-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestEmailConfirm()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user.IsError) return user.ToHttpResult();
        var userValue = user.Value;
        if (userValue.EmailConfirmed)
        {
            return Unauthorized("Email is already confirmed");
        }
        
        var code = await userService.GenerateEmailConfirmTokenAsync();
        if (code.IsError) return code.ToHttpResult();

        var codeResult = code.Value;
        var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(codeResult));
        
        var link = $"{_blazorSettings.AdminPageUrl}/confirm-email?email={userValue.Email}&code={base64}";
        var message = EmailText.GetEmail(userValue.Email!, "email confirmation", link);

        var result = await emailService.SendEmailAsync(user.Value.Email!, "Email confirmation code", message);
        return result.ToHttpResult();
    }

    [Authorize]
    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromBody] UserEmailConfirmRequestDto dto)
    {
        try
        {
            var convertedToken = Encoding.ASCII.GetString(Convert.FromBase64String(dto.Token));

            var confirm = await userService.ConfirmEmailAsync(dto.Email, convertedToken);
            return confirm.IsSome
                ? confirm.ToHttpResult()
                : Ok();
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost("reset-password-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] string email)
    {
        var user = await userService.GetUserByEmailAsync(email);
        if (user.IsError) return user.ToHttpResult();
        var userValue = user.Value;
        if (!userValue.EmailConfirmed)
        { 
            return Unauthorized("Email is not confirmed");
        }
        
        var code = await userService.GeneratePasswordResetTokenAsync(email);
        if (code.IsError) return code.ToHttpResult();

        var codeResult = code.Value;
        var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(codeResult));
        
        var link = $"{_blazorSettings.AdminPageUrl}/reset-password?email={email}&code={base64}";
        var message = EmailText.GetEmail(userValue.Email!, "password reset", link);

        var result = await emailService.SendEmailAsync(user.Value.Email!, "Email confirmation code", message);
        return result.ToHttpResult();
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword([FromBody] UserPasswordResetRequestDto dto)
    {
        try
        {
            var convertedToken = Encoding.ASCII.GetString(Convert.FromBase64String(dto.Token));

            var confirm = await userService.ResetPasswordAsync(dto.Email, convertedToken, dto.NewPassword);
            return confirm.IsSome
                ? confirm.ToHttpResult()
                : Ok();
        }
        catch
        {
            return BadRequest();
        }
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