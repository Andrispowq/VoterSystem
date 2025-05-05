namespace VoterSystem.Web.Admin.Dto;

public class OnlyVoteDto
{
    public required VoteChoiceDto VoteChoice { get; init; }
    public required DateTime CreatedAt { get; init; }
}