using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for managing votings
/// </summary>
public interface IVotingService
{
    Task<Result<List<Voting>, ServiceError>> GetAllVotings();
    Task<Result<Voting, ServiceError>> GetVotingById(long id);
    Task<Option<ServiceError>> CreateVoting(Voting voting, bool commit = true);
    Task<Option<ServiceError>> UpdateVoting(Voting voting, bool commit = true);
    Task<Option<ServiceError>> DeleteVoting(long id, bool commit = true);
    
    Task<bool> HasVotedOnVoting(Voting voting);
}