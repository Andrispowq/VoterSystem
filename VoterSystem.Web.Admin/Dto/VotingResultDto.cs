namespace VoterSystem.Web.Admin.Dto;

public class VotingResultDto
{
    public required long VotingId { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }
    public required bool HasStarted { get; init; }
    public required bool HasEnded { get; init; }
    public required bool IsOngoing { get; init; }
    public required  bool? HasVoted { get; init; }
    
    public required ICollection<VoteChoiceDto> VoteChoices { get; init; }
    public required ICollection<OnlyVoteDto> Results { get; init; }
}