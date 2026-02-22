using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class VotingService(
    VoterSystemDbContext dbContext, IHttpContextAccessor http, ILogger<VotingService> logger) 
    : BaseService<Voting, VotingService>(http, logger), IVotingService
{
    protected override bool CanAccessAll(bool admin) => true;
    
    public async Task<Result<List<Voting>, ServiceError>> GetAllVotings()
    {
        if (!CheckAccessOnAll())
        {
            return new UnauthorizedError("Access denied");
        }

        var query = ApplyGroupFilter(dbContext.Votings);
        return await query.ToListAsync();
    }

    public async Task<Result<List<Voting>, ServiceError>> GetVotableVotings()
    {
        var query = ApplyGroupFilter(dbContext.Votings
            .Include(x => x.VotingParticipations));

        query = query.Where(x => x.VotingParticipations.All(v => v.UserId != UserId));

        return await query.ToListAsync();
    }

    public async Task<Result<List<Voting>, ServiceError>> GetVotedVotings()
    {
        var query = ApplyGroupFilter(dbContext.Votings
            .Include(x => x.VotingParticipations));

        query = query.Where(x => x.VotingParticipations.Any(v => v.UserId == UserId));

        return await query.ToListAsync();
    }

    public async Task<Result<Voting, ServiceError>> GetVotingById(long id)
    {
        var voting = await dbContext.Votings
            .Include(v => v.Group)
                .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(v => v.VotingId == id);

        if (voting is null)
        {
            return new NotFoundError("Voting not found");
        }

        if (!HasGroupAccess(voting))
        {
            return new UnauthorizedError("Access denied");
        }

        var check = CheckAccessOn(voting, RoleControlAction.Access);
        if (check.IsSome) return check.AsSome.Value;

        return voting;
    }

    public async Task<Result<Voting, ServiceError>> CreateVoting(VotingCreateRequestDto request, bool commit = true)
    {
        if (request.GroupId.HasValue)
        {
            var group = await dbContext.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == request.GroupId.Value);

            if (group is null)
            {
                return new NotFoundError("Group not found");
            }

            if (group.Members.All(m => m.UserId != UserId))
            {
                return new UnauthorizedError("You are not part of this group");
            }
        }

        var voting = new Voting
        {
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Name = request.Name,
            CreatedByUserId = UserId,
            GroupId = request.GroupId
        };
        
        var check = CheckAccessOn(voting, RoleControlAction.Create);
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
            return voting;
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
            
            var check = CheckAccessOn(item.Value, RoleControlAction.Update);
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
            
            var check = CheckAccessOn(item.Value, RoleControlAction.Delete);
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
        return await dbContext.VotingParticipations
            .AnyAsync(v => v.VotingId == voting.VotingId && v.UserId == UserId);
    }

    private IQueryable<Voting> ApplyGroupFilter(IQueryable<Voting> query)
    {
        query = query
            .Include(v => v.Group)
                .ThenInclude(g => g.Members);

        if (IsAdmin)
        {
            return query;
        }

        return query.Where(v => v.GroupId == null || v.Group!.Members.Any(m => m.UserId == UserId));
    }

    private bool HasGroupAccess(Voting voting)
    {
        if (IsAdmin || voting.GroupId is null)
        {
            return true;
        }

        return voting.Group?.Members.Any(m => m.UserId == UserId) == true;
    }
}
