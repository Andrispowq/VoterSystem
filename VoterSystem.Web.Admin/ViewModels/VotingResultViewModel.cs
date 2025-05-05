using VoterSystem.Web.Admin.Dto;

namespace VoterSystem.Web.Admin.ViewModels;

public class VotingResultViewModel(VotingResultDto dto)
{
    public long VotingId => dto.VotingId;
    public string Name => dto.Name;
    public DateTime CreatedAt => dto.CreatedAt;
    public DateTime StartsAt => dto.StartsAt;
    public DateTime EndsAt => dto.EndsAt;
    public bool HasStarted => dto.HasStarted;
    public bool HasEnded => dto.HasEnded;
    public bool IsOngoing => dto.IsOngoing;
    public bool? HasVoted => dto.HasVoted;
    
    public ICollection<VoteChoiceDto> VoteChoices => dto.VoteChoices;
    public ICollection<OnlyVoteDto> Results => dto.Results;
}