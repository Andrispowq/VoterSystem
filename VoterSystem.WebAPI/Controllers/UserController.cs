using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Services;
using VoterSystem.WebAPI.Dto;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/users")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userService.GetUsersAsync();
        var dtos = users.Select(u => new UserDto(u));
        return Ok(dtos);
    }
}