using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class VotingDto(Voting voting)
{
    public long VotingId => voting.VotingId;
    public string Name => voting.Name;
    public DateTime CreatedAt => voting.CreatedAt;
    public DateTime StartsAt => voting.StartsAt;
    public DateTime EndsAt => voting.EndsAt;
    public bool IsOngoing => voting.IsOngoing;

    /// <summary>
    /// For Admin users, this will always be null.
    /// For User users, this will indicate whether they have already cast a vote on this Voting.
    /// </summary>
    public bool? HasVoted { get; set; } = null;
    
    public ICollection<VoteChoiceDto> VoteChoices => voting.VoteChoices
        .Select(v => new VoteChoiceDto(v)).ToList();
}