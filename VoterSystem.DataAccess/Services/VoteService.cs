using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class VoteService(
    VoterSystemDbContext dbContext, 
    IHttpContextAccessor http,
    ILogger<VoteService> logger,
    IOptions<VotingSettings> votingSettingsOptions) 
    : BaseService<AnonymousBallot, VoteService>(http, logger), IVoteService
{
    private readonly VotingSettings _votingSettings = votingSettingsOptions.Value;
    protected override bool CanAccessAll(bool admin) => admin;

    public async Task<Result<List<AnonymousBallot>, ServiceError>> GetVotesForVoting(Voting voting)
    {
        //Admin can access them all
        if (IsAdmin)
        {
            return await GetVotes(voting);
        }
        
        //If you created it, you can access them all
        if (voting.CreatedByUserId == UserId)
        {
            return await GetVotes(voting);
        }
        
        //If you have voted, you can also access them all
        var hasVoted = await dbContext.VotingParticipations
            .AnyAsync(v => 
                v.VotingId == voting.VotingId && v.UserId == UserId);
        if (!hasVoted)
        {
            return new UnauthorizedError("Access denied");
        }

        return await GetVotes(voting);
    }

    private async Task<List<AnonymousBallot>> GetVotes(Voting voting)
    {
        return await dbContext.AnonymousBallots
            .Where(v => v.VotingId == voting.VotingId)
            .ToListAsync();
    }
    
    public async Task<Result<List<VotingParticipation>, ServiceError>> GetMyVotes()
    {
        if (IsAdmin)
        {
            return new UnauthorizedError("Admins cannot vote");
        }
        
        return await dbContext.VotingParticipations
            .Where(v => v.UserId == UserId)
            .ToListAsync();
    }

    public async Task<Result<VoteResultDto, ServiceError>> CastVote(User user, VoteChoice voteChoice)
    {
        if (voteChoice.Voting.CreatedByUserId == user.Id)
        {
            return new UnauthorizedError("You can not vote on your own voting!");
        }

        if (IsAdmin)
        {
            return new UnauthorizedError("Admins cannot vote");
        }

        if (voteChoice.Voting.GroupId.HasValue)
        {
            var isMember = await dbContext.GroupMembers
                .AnyAsync(m => m.GroupId == voteChoice.Voting.GroupId && m.UserId == user.Id);
            if (!isMember)
            {
                return new UnauthorizedError("You are not part of this group");
            }
        }

        var alreadyVoted = await dbContext.VotingParticipations.AnyAsync(
            x => x.UserId == user.Id && x.VotingId == voteChoice.VotingId);
        if (alreadyVoted)
        {
            return new ConflictError("User already voted on this voting");
        }

        try
        {
            var participation = new VotingParticipation
            {
                UserId = user.Id,
                VotingId = voteChoice.VotingId,
                HasVoted = true
            };

            await dbContext.VotingParticipations.AddAsync(participation);

            var saltS = Convert.ToBase64String(voteChoice.Voting.KeySalt);
            var result = SaltGenerator.CreateVoteTag(saltS, _votingSettings.MasterKey);

            var ballot = new AnonymousBallot
            {
                VotingId = voteChoice.VotingId,
                ChoiceId = voteChoice.ChoiceId,
                VoteTagBase64 = result.HashCode
            };

            await dbContext.AnonymousBallots.AddAsync(ballot);

            voteChoice.VoteCount++;
            dbContext.VoteChoices.Update(voteChoice);

            //var check = await CheckAccessOn(ballot, RoleControlAction.Create);
            //if (check.IsSome) return check.AsSome.Value;

            await dbContext.SaveChangesAsync();
            return new VoteResultDto
            {
                VotingId = voteChoice.VotingId,
                Receipt = result.Receipt
            };
        }
        catch (DbUpdateException e)
        {
            return new ConflictError(e.Message);
        }
        catch (Exception e)
        {
            return new BadRequestError(e.Message);
        }
    }
}
