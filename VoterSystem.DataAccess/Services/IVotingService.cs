using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for managing votings
/// </summary>
public interface IVotingService
{
    Task<Result<List<Voting>, ServiceError>> GetAllVotings();
    Task<Result<List<Voting>, ServiceError>> GetVotableVotings();
    Task<Result<List<Voting>, ServiceError>> GetVotedVotings();
    
    Task<Result<Voting, ServiceError>> GetVotingById(long id);
    Task<Result<Voting, ServiceError>> CreateVoting(VotingCreateRequestDto request, bool commit = true);
    Task<Option<ServiceError>> UpdateVoting(Voting voting, bool commit = true);
    Task<Option<ServiceError>> DeleteVoting(long id, bool commit = true);
    
    Task<bool> HasVotedOnVoting(Voting voting);
}