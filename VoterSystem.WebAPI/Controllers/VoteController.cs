using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.SignalR.Interfaces;
using VoterSystem.Shared.SignalR.Models;
using VoterSystem.WebAPI.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/votes/")]
internal class VoteController(
    IUserService userService, 
    IVoteChoiceService voteChoiceService, 
    IVoteService voteService,
    IVoteNotificationService voteNotificationService) : ControllerBase
{
    
    [Authorize("UserOnly")]
    [HttpPost("cast-vote")]
    [ProducesResponseType<VoteResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CastVoteAsync([FromQuery] long choiceId)
    {
        var choiceResult = await voteChoiceService.GetChoiceById(choiceId);
        if (choiceResult.IsError) return choiceResult.Error.ToHttpResult();
        var choice = choiceResult.Value;
      
        var userResult = await userService.GetCurrentUserAsync();
        if (userResult.IsError) return userResult.Error.ToHttpResult();
        var user = userResult.Value;
        
        var result = await voteService.CastVote(user, choice);
        if (result.IsError) return result.ToHttpResult();
        
        var votes = await voteService.GetVotesForVoting(choice.Voting);
        if (votes.IsError) return votes.Error.ToHttpResult();

        var results = votes.Value.ToVotingResultsDto();
        var notification = new VotingUpdatedDto
        {
            VotingId = choice.VotingId,
            VotingResults = results
        };
        
        await voteNotificationService.NotifyVotingResultChanged(notification);
        return result.ToHttpResult();
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType<List<VotingParticipationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyVotes()
    {
        var votes = await voteService.GetMyVotes();
        return votes.ToOkResult(list => list.Select(DtoExtensions.ToVotingParticipationDto));
    }
}