using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class VotingResultDto(Voting voting)
{
    public long VotingId => voting.VotingId;
    public string Name => voting.Name;
    public DateTime CreatedAt => voting.CreatedAt;
    public DateTime StartsAt => voting.StartsAt;
    public DateTime EndsAt => voting.EndsAt;
    public bool HasStarted => voting.HasStarted;
    public bool HasEnded => voting.HasEnded;
    public bool IsOngoing => voting.IsOngoing;

    /// <summary>
    /// For Admin users, this will always be null.
    /// For User users, this will indicate whether they have already cast a vote on this Voting.
    /// </summary>
    public bool? HasVoted { get; set; } = null;
    
    public ICollection<VoteChoiceDto> VoteChoices => voting.VoteChoices
        .Select(v => new VoteChoiceDto(v))
        .OrderBy(v => v.CreatedAt)
        .ToList();
    
    public ICollection<OnlyVoteDto> Results => voting.Votes
        .Select(v => new OnlyVoteDto(v))
        .OrderBy(v => v.CreatedAt)
        .ToList();
}