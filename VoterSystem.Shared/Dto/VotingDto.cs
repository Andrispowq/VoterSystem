namespace VoterSystem.Shared.Dto;

public class VotingDto
{
    public required long VotingId { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }
    public required bool HasStarted { get; init; }
    public required bool HasEnded { get; init; }
    public required bool IsOngoing { get; init; }

    /// <summary>
    /// For Admin users, this will always be null.
    /// For User users, this will indicate whether they have already cast a vote on this Voting.
    /// </summary>
    public bool? HasVoted { get; set; } = null;
    
    public required ICollection<VoteChoiceDto> VoteChoices { get; init; }
}