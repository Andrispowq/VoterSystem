using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public class VoteChoiceService(VoterSystemDbContext dbContext) : IVoteChoiceService
{
    public async Task<List<VoteChoice>> GetVoteChoices(Voting voting)
    {
        return await dbContext.VoteChoices
            .Where(c => c.VotingId == voting.VotingId)
            .ToListAsync();
    }

    public async Task<Option<ServiceError>> AddVotingChoice(Voting voting, VoteChoice choice, bool commit = true)
    {
        try
        {
            choice.VotingId = voting.VotingId;
            await dbContext.VoteChoices.AddAsync(choice);
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception e)
        {
            return new ConflictError(e.Message);
        }
    }

    public async Task<Option<ServiceError>> UpdateVotingChoice(VoteChoice choice, bool commit = true)
    {
        try
        { 
            var item = await dbContext.VoteChoices.FindAsync(choice.ChoiceId);
            if (item is null) return new NotFoundError("VoteChoice not found");
            
            dbContext.VoteChoices.Update(choice);
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception e)
        {
            return new ConflictError(e.Message);
        }
    }

    public async Task<Option<ServiceError>> DeleteVotingChoice(VoteChoice choice, bool commit = true)
    {
        try
        { 
            var item = await dbContext.VoteChoices.FindAsync(choice.ChoiceId);
            if (item is null) return new NotFoundError("VoteChoice not found");
            
            dbContext.VoteChoices.Remove(choice);
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception e)
        {
            return new ConflictError(e.Message);
        }
    }
}