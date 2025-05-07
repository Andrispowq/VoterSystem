using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for voting, and querying vote statistics
/// </summary>
public interface IVoteService
{
    Task<Result<List<Vote>, ServiceError>> GetVotesForVoting(Voting voting);
    Task<Result<List<Vote>, ServiceError>> GetMyVotes();
    Task<Option<ServiceError>> CastVote(User user, VoteChoice voteChoice);
}