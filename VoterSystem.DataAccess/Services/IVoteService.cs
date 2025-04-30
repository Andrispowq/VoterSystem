using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for voting, and querying vote statistics
/// </summary>
public interface IVoteService
{
    Task<List<Vote>> GetAllVotes(Voting? voting = null, User? user = null);
    Task<Option<ServiceError>> CastVote(Vote vote);
    Task<Option<ServiceError>> CastVote(User user, VoteChoice voteChoice);
}