using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class VoteService(
    VoterSystemDbContext dbContext, 
    IUserService userService, 
    IOptions<VotingSettings> votingSettingsOptions) 
    : BaseService<AnonymousBallot>(userService), IVoteService
{
    private readonly IUserService _userService = userService;
    private readonly VotingSettings _votingSettings = votingSettingsOptions.Value;
    protected override bool CanAccessAll(bool admin) => admin;

    public async Task<Result<List<AnonymousBallot>, ServiceError>> GetVotesForVoting(Voting voting)
    {
        //Admin can access them all
        var isAdmin = _userService.IsCurrentUserAdmin();
        if (isAdmin)
        {
            return await GetVotes(voting);
        }
        
        //If you created it, you can access them all
        var userId = _userService.GetCurrentUserId();
        if (userId.IsError) return userId.Error;
        if (voting.CreatedByUserId == userId.Value)
        {
            return await GetVotes(voting);
        }
        
        //If you have voted, you can also access them all
        var hasVoted = await dbContext.VotingParticipations
            .AnyAsync(v => 
                v.VotingId == voting.VotingId && v.UserId == userId.Value);
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
        var isAdmin = _userService.IsCurrentUserAdmin();
        if (isAdmin)
        {
            return new UnauthorizedError("Admins cannot vote");
        }
        
        var userId = _userService.GetCurrentUserId();
        if (userId.IsError) return userId.Error;
        
        return await dbContext.VotingParticipations
            .Where(v => v.UserId == userId.Value)
            .ToListAsync();
    }

    public async Task<Result<VoteResultDto, ServiceError>> CastVote(User user, VoteChoice voteChoice)
    {
        if (voteChoice.Voting.CreatedByUserId == user.Id)
        {
            return new UnauthorizedError("You can not vote on your own voting!");
        }
        
        var role = await _userService.GetUserRoleByIdAsync(user.Id);
        if (role.IsError) return role.Error;
        if (role.Value == Role.Admin)
        {
            return new UnauthorizedError("Admins cannot vote");
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