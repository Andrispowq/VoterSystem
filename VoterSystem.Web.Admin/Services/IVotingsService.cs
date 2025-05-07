using VoterSystem.Web.Admin.Dto;
using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Services;

public interface IVotingsService
{
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
}