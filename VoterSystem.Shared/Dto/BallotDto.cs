namespace VoterSystem.Shared.Dto;

public class BallotDto
{
    public required VoteChoiceDto VoteChoice { get; init; }
    public required VotingDto Voting { get; init; }
    public required DateTime CreatedAt { get; init; }
}