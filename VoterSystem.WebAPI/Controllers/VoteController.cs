using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Services;
using VoterSystem.WebAPI.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/votes/")]
public class VoteController(IUserService userService, IVoteChoiceService voteChoiceService, IVoteService voteService) : ControllerBase
{
    [Authorize("UserOnly")]
    [HttpPost("cast-vote")]
    public async Task<IActionResult> CastVoteAsync([FromQuery] long choiceId)
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
    public async Task<IActionResult> GetMyVotes()
    {
        var votes = await voteService.GetMyVotes();
        return votes.ToOkResult(list => list.Select(DtoExtensions.ToVoteDto));
    }
}