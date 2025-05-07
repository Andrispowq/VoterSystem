using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public class VoteService(VoterSystemDbContext dbContext, IUserService userService) 
    : BaseService<Vote>(userService), IVoteService
{
    private readonly IUserService _userService = userService;
    protected override bool CanAccessAll(bool admin) => admin;

    public async Task<Result<List<Vote>, ServiceError>> GetVotesForVoting(Voting voting)
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
        var hasVoted = await dbContext.Votes.AnyAsync(v => v.VotingId == voting.VotingId
                                                           && v.UserId == userId.Value);
        if (!hasVoted)
        {
            return new UnauthorizedError("Access denied");
        }

        return await GetVotes(voting);
    }

    private async Task<List<Vote>> GetVotes(Voting voting)
    {
        return await dbContext.Votes
            .Where(v => v.VotingId == voting.VotingId)
            .ToListAsync();
    }
    
    public async Task<Result<List<Vote>, ServiceError>> GetMyVotes()
    {
        var isAdmin = _userService.IsCurrentUserAdmin();
        if (isAdmin)
        {
            return new UnauthorizedError("Admins cannot vote");
        }
        
        var userId = _userService.GetCurrentUserId();
        if (userId.IsError) return userId.Error;
        
        return await dbContext.Votes
            .Where(v => v.UserId == userId.Value)
            .ToListAsync();
    }

    private async Task<Option<ServiceError>> CastVote(Vote vote)
    {
        var user = await _userService.GetUserRoleByIdAsync(vote.UserId);
        if (user.IsError) return user.Error;
        if (user.Value == Role.Admin)
        {
            return new UnauthorizedError("Admins cannot vote");
        }
        
        var check = await CheckAccessOn(vote, RoleControlAction.Create);
        if (check.IsSome) return check.AsSome.Value;
        
        try
        {
            await dbContext.Votes.AddAsync(vote);
            await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception e)
        {
            return new ConflictError(e.Message);
        }
    }

    public async Task<Option<ServiceError>> CastVote(User user, VoteChoice voteChoice)
    {
        var vote = new Vote
        {
            UserId = user.Id,
            VotingId = voteChoice.VotingId,
            ChoiceId = voteChoice.ChoiceId,
        };

        return await CastVote(vote);
    }
}