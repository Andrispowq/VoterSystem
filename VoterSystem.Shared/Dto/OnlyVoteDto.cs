using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class OnlyVoteDto(Vote vote)
{
    public VoteChoiceDto VoteChoice => new(vote.VoteChoice);
    public DateTime CreatedAt => vote.CreatedAt;
}