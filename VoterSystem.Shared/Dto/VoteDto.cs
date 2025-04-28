using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class VoteDto(Vote vote)
{
    public VoteChoiceDto VoteChoice => new(vote.VoteChoice);
    public VotingDto Voting => new(vote.Voting);
    public DateTime CreatedAt => vote.CreatedAt;
}