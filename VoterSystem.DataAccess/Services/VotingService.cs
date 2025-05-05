using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public class VotingService(VoterSystemDbContext dbContext,
    IUserService userService) : IVotingService
{
    public async Task<Result<IReadOnlyList<Voting>, ServiceError>> GetVotings()
    {
        var userId = userService.GetCurrentUserId();
        if (userId.IsError) return userId.GetErrorOrThrow();
        var id = userId.GetValueOrThrow();

        var isAdmin = userService.IsCurrentUserAdmin();
        
        return await dbContext.Votings
            .Where(v => isAdmin || v.CreatedByUserId == id)
            .ToListAsync();
    }

    public async Task<Result<Voting, ServiceError>> GetVoting(long id)
    {
        var item = await dbContext.Votings.FindAsync(id);
        if (item is null)
        {
            return new NotFoundError("Voting not found");
        }
        
        var userId = userService.GetCurrentUserId();
        if (userId.IsError) return userId.GetErrorOrThrow();
        var guid = userId.GetValueOrThrow();

        var isAdmin = userService.IsCurrentUserAdmin();
        if (isAdmin || item.CreatedByUserId == guid)
        {
            return item;
        }

        return new UnauthorizedError("Access not authorized");
    }

    public async Task<Option<ServiceError>> CreateVoting(Voting voting, bool commit = true)
    {
        try
        {
            await dbContext.Votings.AddAsync(voting);
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new ConflictError(ex.Message);
        }
    }

    public async Task<Option<ServiceError>> UpdateVoting(Voting voting, bool commit = true)
    {
        try
        {
            var item = await GetVoting(voting.VotingId);
            if (item.IsError) return item.GetErrorOrThrow();

            dbContext.Votings.Update(voting);
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new ConflictError(ex.Message);
        }
    }

    public async Task<Option<ServiceError>> DeleteVoting(long id, bool commit = true)
    {
        try
        {
            var item = await GetVoting(id);
            if (item.IsError) return item.GetErrorOrThrow();
            
            dbContext.Votings.Remove(item.GetValueOrThrow());
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new ConflictError(ex.Message);
        }
    }
}