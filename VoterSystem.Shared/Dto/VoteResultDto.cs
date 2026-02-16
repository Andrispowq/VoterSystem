namespace VoterSystem.Shared.Dto;

public sealed class VoteResultDto
{
    public required long VotingId { get; init; }
    public required string Receipt { get; init; }
}