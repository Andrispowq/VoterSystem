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
    [ProducesResponseType(typeof(List<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct = default)
    {
        var groups = await groupService.GetAllAsync(ct);
        return Ok(groups.Select(g => g.ToGroupDto()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var group = await groupService.GetByIdAsync(id, ct);
        return group.ToOkResult(x => x.ToGroupDto());
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroupAsync([FromBody] CreateGroupRequest request, CancellationToken ct = default)
    {
        var result = await groupService.CreateGroupAsync(request, ct);
        if (result.IsError) return result.ToHttpResult();

        var value = result.Value;
        return CreatedAtAction(nameof(GetById), new { id = value.GroupId }, value.ToGroupDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroupAsync(Guid id, CancellationToken ct = default)
    {
        var result = await groupService.DeleteGroupAsync(id, ct);
        return result.ToHttpResult();
    }

    [HttpPost("{id:guid}/members/{userId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddUserToGroup(Guid id, Guid userId, CancellationToken ct = default)
    {
        var result = await groupService.AddToGroupAsync(id, userId, ct);
        return result.ToHttpResult();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserFromGroup(Guid id, Guid userId, CancellationToken ct = default)
    {
        var result = await groupService.RemoveFromGroupAsync(id, userId, ct);
        return result.ToHttpResult();
    }
}
