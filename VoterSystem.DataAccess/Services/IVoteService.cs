using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for voting, and querying vote statistics
/// </summary>
public interface IVoteService
{
    Task<Result<List<AnonymousBallot>, ServiceError>> GetVotesForVoting(Voting voting);
    Task<Result<List<VotingParticipation>, ServiceError>> GetMyVotes();
    Task<Result<VoteResultDto, ServiceError>> CastVote(User user, VoteChoice voteChoice);
}