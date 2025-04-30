using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public class VoteService(VoterSystemDbContext dbContext) : IVoteService
{
    public async Task<List<Vote>> GetAllVotes(Voting? voting = null, User? user = null)
    {
        var query = dbContext.Votes.AsQueryable();
        if (voting is not null) query = query.Where(v => v.VotingId == voting.VotingId);
        if (user is not null) query = query.Where(v => v.UserId == user.Id);

        return await query.ToListAsync();
    }

    public async Task<Option<ServiceError>> CastVote(Vote vote)
    {
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