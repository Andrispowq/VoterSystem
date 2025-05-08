using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public class VotingService(VoterSystemDbContext dbContext, IUserService userService) 
    : BaseService<Voting>(userService), IVotingService
{
    private readonly IUserService _userService = userService;
    protected override bool CanAccessAll(bool admin) => true;
    
    public async Task<Result<List<Voting>, ServiceError>> GetAllVotings()
    {
        if (!CheckAccessOnAll())
        {
            return new UnauthorizedError("Access denied");
        }
        
        return await dbContext.Votings.ToListAsync();
    }

    public async Task<Result<Voting, ServiceError>> GetVotingById(long id)
    {
        var item = await dbContext.Votings.FindAsync(id);
        if (item is null)
        {
            return new NotFoundError("Voting not found");
        }

        var check = await CheckAccessOn(item, RoleControlAction.Access);
        if (check.IsSome) return check.AsSome.Value;

        return item;
    }

    public async Task<Option<ServiceError>> CreateVoting(Voting voting, bool commit = true)
    {
        var check = await CheckAccessOn(voting, RoleControlAction.Create);
        if (check.IsSome) return check.AsSome.Value;

        if (voting.StartsAt <= DateTime.UtcNow)
        {
            return new BadRequestError("Bad start time");
        }

        if (voting.EndsAt <= DateTime.UtcNow.AddDays(1)|| voting.StartsAt.AddDays(1) > voting.EndsAt)
        {
            return new BadRequestError("Bad end time");
        }
        
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
            var item = await GetVotingById(voting.VotingId);
            if (item.IsError) return item.Error;
            
            var check = await CheckAccessOn(item.Value, RoleControlAction.Update);
            if (check.IsSome) return check.AsSome.Value;

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
            var item = await GetVotingById(id);
            if (item.IsError) return item.Error;
            
            var check = await CheckAccessOn(item.Value, RoleControlAction.Delete);
            if (check.IsSome) return check.AsSome.Value;
            
            dbContext.Votings.Remove(item.Value);
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new ConflictError(ex.Message);
        }
    }

    public async Task<bool> HasVotedOnVoting(Voting voting)
    {
        var userId = _userService.GetCurrentUserId();
        if (userId.IsError) return false;
        
        return await dbContext.Votes
            .AnyAsync(v => v.VotingId == voting.VotingId && v.UserId == userId.Value);
    }
}