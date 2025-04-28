using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for adding, modifying and deleting choices for a voting
/// </summary>
public interface IVoteChoiceService
{
    Task<List<VoteChoice>> GetVoteChoices(Voting voting);
    Task<Option<ServiceError>> AddVotingChoice(Voting voting, VoteChoice choice, bool commit = true);
    Task<Option<ServiceError>> UpdateVotingChoice(VoteChoice choice, bool commit = true);
    Task<Option<ServiceError>> DeleteVotingChoice(VoteChoice choice, bool commit = true);
}