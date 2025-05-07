using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.WebAPI.Functional;

namespace VoterSystem.WebAPI.Controllers;

[ApiController]
[Route("/api/v1/votings")]
public class VotingController(IVotingService votingService, IVoteService voteService,
    IUserService userService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VotingDto>))]
    public async Task<IActionResult> GetVotings()
    {
        var votings = await votingService.GetAllVotings();
        if (votings.IsError) return votings.Error.ToHttpResult();
        
        return Ok(votings.Value
            .Select(v => new VotingDto(v)).ToList());
    }
    
    [Authorize]
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VotingDto))]
    public async Task<IActionResult> GetVotingById(long id)
    {
        var votingRes = await votingService.GetVotingById(id);
        if (votingRes.IsError) return votingRes.Error.ToHttpResult();
        var voting = votingRes.Value;
        var dto = new VotingDto(voting)
        {
            HasVoted = await votingService.HasVotedOnVoting(voting)
        };

        return Ok(dto);
    }

    [Authorize]
    [HttpGet("{id:long}/results")]
    public async Task<IActionResult> GetVotingResults(long id)
    {
        var votingRes = await votingService.GetVotingById(id);
        if (votingRes.IsError) return votingRes.Error.ToHttpResult();
        var voting = votingRes.Value;

        var votes = await voteService.GetVotesForVoting(voting);
        if (votes.IsError) return votes.Error.ToHttpResult();
        
        return Ok(new VotingResultsDto(votes.Value));
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(VotingDto))]
    public async Task<IActionResult> AddVoting([FromBody] VotingCreateRequestDto voting)
    {
        var userId = userService.GetCurrentUserId();
        if (userId.IsError) return userId.ToHttpResult();
        
        var obj = new Voting
        {
            StartsAt = voting.StartsAt,
            EndsAt = voting.EndsAt,
            Name = voting.Name,
            CreatedByUserId = userId.Value,
        };
        
        var result = await votingService.CreateVoting(obj);
        return result.IsSome
            ? result.ToHttpResult()
            : CreatedAtAction(nameof(GetVotingById), new { id = obj.VotingId }, new VotingDto(obj));
    }

    [Authorize]
    [HttpPatch("{id:long}/starts-at")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VotingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> StartsAtVoting(long id, [FromBody] DateTime startsAt)
    {
        var votingRes = await votingService.GetVotingById(id);
        if (votingRes.IsError) return votingRes.Error.ToHttpResult();
        var voting = votingRes.Value;

        if (voting.HasStarted)
        {
            return BadRequest("Vote has already started");
        }

        if (startsAt < DateTime.UtcNow.AddDays(1) || startsAt >= voting.EndsAt.AddDays(-1))
        {
            return BadRequest("Vote cannot start one day before this time or before it ends");
        }

        voting.StartsAt = startsAt;
        
        var error = await votingService.UpdateVoting(voting);
        return error.IsSome ? error.ToHttpResult() : Ok(new VotingDto(voting));
    }

    [Authorize]
    [HttpPatch("{id:long}/ends-at")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VotingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> EndAtVoting(long id, [FromBody] DateTime endsAt)
    {
        var votingRes = await votingService.GetVotingById(id);
        if (votingRes.IsError) return votingRes.Error.ToHttpResult();
        var voting = votingRes.Value;

        if (voting.HasStarted)
        {
            return BadRequest("Vote has already started");
        }

        if (endsAt <= DateTime.UtcNow.AddDays(1) || endsAt <= voting.StartsAt.AddDays(1))
        {
            return BadRequest("Vote cannot end one day before start or this time");
        }

        voting.EndsAt = endsAt;
        
        var error = await votingService.UpdateVoting(voting);
        return error.IsSome ? error.ToHttpResult() : Ok(new VotingDto(voting));
    }

    [Authorize]
    [HttpPost("{id:long}/start")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VotingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> StartVoting(long id)
    {
        var votingRes = await votingService.GetVotingById(id);
        if (votingRes.IsError) return votingRes.Error.ToHttpResult();
        var voting = votingRes.Value;

        if (voting.VoteChoices.Count < 2)
        {
            return BadRequest("Vote choices must be at least 2 choices");
        }
        
        if (voting.HasStarted)
        {
            return BadRequest("Vote has already started");
        }

        voting.StartsAt = DateTime.UtcNow;
        
        var error = await votingService.UpdateVoting(voting);
        return error.IsSome ? error.ToHttpResult() : Ok(new VotingDto(voting));
    }

    [Authorize]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteVoting(long id)
    {
        var votes = await votingService.DeleteVoting(id);
        return votes.ToHttpResult();
    }

    [Authorize]
    [HttpGet("{id:long}/votes")]
    public async Task<IActionResult> GetAllVotes(long id)
    {
        var votingRes = await votingService.GetVotingById(id);
        if (votingRes.IsError) return votingRes.Error.ToHttpResult();
        var voting = votingRes.Value;

        var votes = await voteService.GetVotesForVoting(voting);
        return votes.ToOkResult(list => list.Select(v => new VoteDto(v)));
    }
}