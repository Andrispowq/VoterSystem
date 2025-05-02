namespace VoterSystem.Web.Admin.Dto;

public class VoteDto
{
    public required VoteChoiceDto VoteChoice { get; init; }
    public required VotingDto Voting { get; init; }
    public required DateTime CreatedAt { get; init; }
}