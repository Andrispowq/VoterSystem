using VoterSystem.Shared.SignalR.Models;

namespace VoterSystem.Shared.Blazor.Services.SignalR;

public interface IVoteHubService : IBaseHubService
{
    event Action<VotingUpdatedDto>? OnVotingResultUpdated;
    Task StartHubAsync();
}