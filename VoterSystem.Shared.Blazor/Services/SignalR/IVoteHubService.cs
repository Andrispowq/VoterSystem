using VoterSystem.Shared.SignalR.Models;

namespace VoterSystem.Shared.Blazor.Services.SignalR;

public interface IVoteHubService : IBaseHubService
{
    event Action<VotingUpdatedDto>? OnVotingResultUpdated;
    Task StartHubAsync();
    Task<bool> SubscribeToVotingAsync(long votingId);
    Task<bool> UnsubscribeFromVotingAsync(long votingId);
}
