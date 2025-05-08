using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Blazor.ViewModels;

namespace VoterSystem.Shared.Blazor.Services;

public interface IVotingsService
{
    Task<VotingsViewModel?> GetVotedVotingsAsync();
    Task<VotingsViewModel?> GetVotableVotingsAsync();
    Task<VotingsViewModel?> GetVotingsAsync();
    Task<VotingDto?> GetVotingByIdAsync(long votingId);
    Task<VotingResultsDto?> GetVotingResultAsync(long votingId);
    Task UpdateEndTime(long votingId, DateTime newEndAt);
    Task UpdateStartTime(long votingId, DateTime newStartAt);
    Task AddVoteChoice(long votingId, string newChoiceName, string newChoiceDescription);
    Task DeleteChoiceAsync(long votingId, long choiceId);
    Task StartVotingAsync(long votingId);
    
    Task<VotingDto?> CreateVotingAsync(VotingCreateRequestDto dto);
    Task DeleteVotingAsync(long votingId);

    Task<List<VoteDto>?> GetMyVotesAsync();
    Task<bool> VoteAsync(long choiceId);
}