using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/votes/")]
public class VoteController(IUserService userService, IVoteChoiceService voteChoiceService, IVoteService voteService) : ControllerBase
{
    [Authorize("UserOnly")]
    [HttpPost("cast-vote")]
    public async Task<IActionResult> CastVoteAsync([FromQuery] Guid choiceId)
    {
        var choiceResult = await voteChoiceService.GetChoiceById(choiceId);
        if (choiceResult.IsError) return choiceResult.Error.ToHttpResult();
        var choice = choiceResult.Value;
        
        var userResult = await userService.GetCurrentUserAsync();
        if (userResult.IsError) return userResult.Error.ToHttpResult();
        var user = userResult.Value;

        var result = await voteService.CastVote(user, choice);
        return result.IsSome ? result.ToHttpResult() : Ok();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllVotes()
    {
        User? filterUser = null;
        
        if (!userService.IsCurrentUserAdmin())
        {
            var userResult = await userService.GetCurrentUserAsync();
            if (userResult.IsError) return userResult.Error.ToHttpResult();
            filterUser = userResult.Value;
        }
        
        var votes = await voteService.GetAllVotes(user: filterUser);
        return Ok(votes.Select(v => new VoteDto(v)));
    }
}