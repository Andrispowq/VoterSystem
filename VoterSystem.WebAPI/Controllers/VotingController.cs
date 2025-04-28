using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/voting")]
public class VotingController(IVotingService votingService) : ControllerBase
{
    [Authorize("AdminOnly")]
    [HttpGet]
    public IActionResult GetVotingsAdmin()
    {
        var votings = votingService.GetVotings();
        return Ok(votings.Select(v => new VotingDto(v)));
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