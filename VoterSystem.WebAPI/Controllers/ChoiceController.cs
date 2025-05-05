using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/votings/{votingId:long}/choices")]
public class ChoiceController(IVotingService votingService, IVoteChoiceService voteChoiceService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VoteChoiceDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChoices(long votingId)
    {
        var voting  = await votingService.GetVoting(votingId);
        if (voting.IsError) return voting.GetErrorOrThrow().ToHttpResult();
        var value = voting.GetValueOrThrow();
        
        return Ok(value.VoteChoices.Select(c => new VoteChoiceDto(c)).ToList());
    }

    [Authorize]
    [HttpGet("{choiceId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VoteChoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChoiceById(long votingId, long choiceId)
    {
        var voting  = await votingService.GetVoting(votingId);
        if (voting.IsError) return voting.GetErrorOrThrow().ToHttpResult();
        var value = voting.GetValueOrThrow();

        var result = value.VoteChoices
            .Where(c => c.ChoiceId == choiceId)
            .Select(c => new VoteChoiceDto(c))
            .FirstOrDefault();

        return result is null ? Ok(result) : NotFound("Choice not found");
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(List<VoteChoiceDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateChoice(long votingId, [FromBody] VoteChoiceRequestDto voteChoice)
    {
        var voting  = await votingService.GetVoting(votingId);
        if (voting.IsError) return voting.GetErrorOrThrow().ToHttpResult();
        var value = voting.GetValueOrThrow();

        var choice = new VoteChoice
        {
            Name = voteChoice.Name,
            Description = voteChoice.Description,
            VotingId = value.VotingId,
        };

        var result = await voteChoiceService.AddVotingChoice(value, choice);
        if (result.IsSome) return result.ToHttpResult();

        return CreatedAtAction(nameof(GetChoiceById), new { votingId, choiceId = choice.ChoiceId },
            new VoteChoiceDto(choice));
    }

    [Authorize]
    [HttpDelete("{choiceId:long}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateChoice(long votingId, long choiceId)
    {
        var voting  = await votingService.GetVoting(votingId);
        if (voting.IsError) return voting.GetErrorOrThrow().ToHttpResult();
        var value = voting.GetValueOrThrow();
        
        var choice = value.VoteChoices
            .FirstOrDefault(c => c.ChoiceId == choiceId);
        if (choice is null) return NotFound("Choice not found");

        var result = await voteChoiceService.DeleteVotingChoice(choice);
        return result.IsSome ? result.ToHttpResult() : Ok();
    }
}