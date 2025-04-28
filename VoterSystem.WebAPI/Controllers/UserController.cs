using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> RegisterAsync([FromBody] UserRegisterRequestDto request)
    {
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            UserName = request.Email
        };
        
        var result = await userService.AddUserAsync(user, request.Password);
        if (result.IsSome) return result.ToHttpResult();
        
        return Created();
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginRequestDto request)
    {
        var result = await userService.LoginAsync(request.Email, request.Password);
        
        var tokens = result.GetValueOrThrow();
        Response.Cookies.Append(TokenIssuer.CookieTokenName, tokens.AuthToken);
        
        return result.ToHttpResult();
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        return user.ToHttpResult();
    }

    [Authorize]
    [HttpDelete("logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        Response.Cookies.Delete(TokenIssuer.CookieTokenName);
        return (await userService.LogoutAsync()).ToHttpResult();
    }
}