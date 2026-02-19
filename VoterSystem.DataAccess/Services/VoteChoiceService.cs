using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class VoteChoiceService(
    VoterSystemDbContext dbContext, IHttpContextAccessor http, ILogger<VoteChoiceService> logger) 
    :  BaseService<VoteChoice, VoteChoiceService>(http, logger), IVoteChoiceService
{
    protected override bool CanAccessAll(bool admin) => true;
    
    public async Task<List<VoteChoice>> GetVoteChoices(Voting voting)
    {
        return await dbContext.VoteChoices
            .Where(c => c.VotingId == voting.VotingId)
            .ToListAsync();
    }

    public async Task<Result<VoteChoice, ServiceError>> GetChoiceById(long choiceId)
    {
        var choice = await dbContext.VoteChoices.FindAsync(choiceId);
        if (choice is null) return new NotFoundError("Choice not found");
        
        return choice;
    }

    public async Task<Option<ServiceError>> AddVotingChoice(Voting voting, VoteChoice choice, bool commit = true)
    {
        if (voting.HasStarted)
        {
            return new UnauthorizedError("Voting has already started");
        }
        
        if (voting.CreatedByUserId != UserId)
        {
            return new UnauthorizedError("Access not authorized");
        }
        
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
        if (choice.Voting.HasStarted)
        {
            return new UnauthorizedError("Voting has already started");
        }

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
        if (choice.Voting.HasStarted)
        {
            return new UnauthorizedError("Voting has already started");
        }

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