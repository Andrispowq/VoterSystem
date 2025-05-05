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
        var votings = await votingService.GetVotings();
        if (votings.IsError) return BadRequest(votings.GetErrorOrThrow());
        var dtos = votings.GetValueOrThrow()
            .Select(v => new VotingDto(v)).ToList();
        if (userService.IsCurrentUserAdmin()) return Ok(dtos);
        
        var userIdResult = userService.GetCurrentUserId();
        if (userIdResult.IsError) return userIdResult.GetErrorOrThrow().ToHttpResult();
        var userId = userIdResult.GetValueOrThrow();
            
        var votes = await voteService.GetAllVotes();
        votes = votes.Where(v => v.UserId == userId).ToList();

        foreach (var votingDto in dtos)
        {
            votingDto.HasVoted = votes.Any(v => v.VoteChoice.VotingId == votingDto.VotingId);
        }
        
        return Ok(dtos.OrderBy(v => v.CreatedAt));
    }
    
    [Authorize]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetVotingById(long id)
    {
        var votingRes = await votingService.GetVoting(id);
        if (votingRes.IsError) return votingRes.GetErrorOrThrow().ToHttpResult();
        var voting = votingRes.GetValueOrThrow();
        var dto = new VotingDto(voting);
        
        var userIdResult = userService.GetCurrentUserId();
        if (userIdResult.IsError) return userIdResult.GetErrorOrThrow().ToHttpResult();
        var userId = userIdResult.GetValueOrThrow();
            
        var votes = await voteService.GetAllVotes();
        votes = votes.Where(v => v.UserId == userId).ToList();
        dto.HasVoted = votes.Any(v => v.VoteChoice.VotingId == dto.VotingId);

        return Ok(dto);
    }

    [Authorize]
    [HttpGet("{id:long}/results")]
    public async Task<IActionResult> GetVotingResults(long id)
    {
        var votingRes = await votingService.GetVoting(id);
        if (votingRes.IsError) return votingRes.GetErrorOrThrow().ToHttpResult();
        var voting = votingRes.GetValueOrThrow();

        return Ok(new VotingResultDto(voting));
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(VotingDto))]
    public async Task<IActionResult> AddVoting([FromBody] Voting voting)
    {
        var result = await votingService.CreateVoting(voting);
        return result.IsSome
            ? result.ToHttpResult()
            : Created();
    }

    [Authorize]
    [HttpPatch("{id:long}/starts-at")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VotingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> StartsAtVoting(long id, [FromBody] DateTime startsAt)
    {
        var votingRes = await votingService.GetVoting(id);
        if (votingRes.IsError) return votingRes.GetErrorOrThrow().ToHttpResult();
        var voting = votingRes.GetValueOrThrow();

        if (voting.HasStarted)
        {
            return BadRequest("Vote has already started");
        }

        if (startsAt < DateTime.UtcNow)
        {
            return BadRequest("Vote cannot start before this time");
        }

        voting.StartsAt = startsAt;
        
        var error = await votingService.UpdateVoting(voting);
        if (error.IsSome) return error.ToHttpResult();
        
        return Ok(new VotingDto(voting));
    }

    [Authorize]
    [HttpPatch("{id:long}/ends-at")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VotingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> EndAtVoting(long id, [FromBody] DateTime endsAt)
    {
        var votingRes = await votingService.GetVoting(id);
        if (votingRes.IsError) return votingRes.GetErrorOrThrow().ToHttpResult();
        var voting = votingRes.GetValueOrThrow();

        if (voting.HasStarted)
        {
            return BadRequest("Vote has already started");
        }

        if (endsAt <= DateTime.UtcNow || endsAt <= voting.StartsAt)
        {
            return BadRequest("Vote cannot end before start or this time");
        }

        voting.EndsAt = endsAt;
        
        var error = await votingService.UpdateVoting(voting);
        if (error.IsSome) return error.ToHttpResult();
        
        return Ok(new VotingDto(voting));
    }

    [Authorize]
    [HttpPost("{id:long}/start")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VotingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> EndAtVoting(long id)
    {
        var votingRes = await votingService.GetVoting(id);
        if (votingRes.IsError) return votingRes.GetErrorOrThrow().ToHttpResult();
        var voting = votingRes.GetValueOrThrow();

        if (voting.HasStarted)
        {
            return BadRequest("Vote has already started");
        }

        voting.StartsAt = DateTime.UtcNow;
        
        var error = await votingService.UpdateVoting(voting);
        if (error.IsSome) return error.ToHttpResult();
        
        return Ok(new VotingDto(voting));
    }
}