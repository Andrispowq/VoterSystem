using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public class VoteChoiceService(VoterSystemDbContext dbContext, IUserService userService) 
    :  BaseService<VoteChoice>(userService), IVoteChoiceService
{
    private readonly IUserService _userService = userService;
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
        
        var userId = _userService.GetCurrentUserId();
        if (userId.IsError) return userId.Error;

        if (voting.CreatedByUserId != userId.Value)
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