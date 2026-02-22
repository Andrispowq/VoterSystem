using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/groups")]
[Authorize]
public class GroupController(IGroupService groupService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType<GroupDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        var result = await groupService.CreateGroupAsync(request, ct);
        if (result.IsError) return result.ToHttpResult();
        var value = result.Value;
        
        return CreatedAtAction(nameof(GetById), new { id = value.GroupId }, value.ToGroupDto());
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<GroupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
    {
        var group = await groupService.GetByIdAsync(id, ct);
        return group.ToOkResult(x => x.ToGroupDto());
    }
}