using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/votings")]
public class VotingController(IVotingService votingService, IUserService userService,
    IVoteService voteService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetVotings()
    {
        var votings = votingService.GetVotings();
        var dtos = votings.Select(v => new VotingDto(v));
        if (userService.IsCurrentUserAdmin()) return Ok(dtos);
        
        var userIdResult = userService.GetCurrentUserId();
        if (userIdResult.IsError) return userIdResult.GetErrorOrThrow().ToHttpResult();
        var userId = userIdResult.GetValueOrThrow();
            
        var votes = await voteService.GetAllVotes();
        votes = votes.Where(v => v.UserId == userId).ToList();

        foreach (var votingDto in dtos)
        {
            votingDto.HasVoted = votes.Any(v => v.VotingId == votingDto.VotingId);
        }
        
        return Ok(dtos);
    }

    [Authorize("AdminOnly")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(VotingDto))]
    public async Task<IActionResult> AddVoting([FromBody] Voting voting)
    {
        var result = await votingService.CreateVoting(voting);
        return result.IsSome
            ? result.ToHttpResult()
            : Created();
    }
}