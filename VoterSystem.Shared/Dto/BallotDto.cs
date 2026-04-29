namespace VoterSystem.Shared.Dto;

public class BallotDto
{
    public required Guid AnonymousBallotId { get; init; }
    public required VoteChoiceDto VoteChoice { get; init; }
    public required VotingDto Voting { get; init; }
}