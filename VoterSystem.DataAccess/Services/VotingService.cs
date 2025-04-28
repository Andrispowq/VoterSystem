using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public class VotingService(VoterSystemDbContext dbContext) : IVotingService
{
    public IReadOnlyList<Voting> GetVotings()
    {
        return dbContext.Votings.ToList();
    }

    public async Task<Result<Voting, ServiceError>> GetVoting(long id)
    {
        var item = await dbContext.Votings.FindAsync(id);
        return item is null
            ? new NotFoundError("Voting not found")
            : item;
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
            var item = await dbContext.Votings.FindAsync(voting.VotingId);
            if (item is null) return new NotFoundError("Voting not found");

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
            var item = await dbContext.Votings.FindAsync(id);
            if (item is null) return new NotFoundError("Voting not found");
            
            dbContext.Votings.Remove(item);
            if (commit) await dbContext.SaveChangesAsync();
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new ConflictError(ex.Message);
        }
    }
}