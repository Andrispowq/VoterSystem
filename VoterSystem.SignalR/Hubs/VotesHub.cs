using Microsoft.AspNetCore.SignalR;
using VoterSystem.Shared.SignalR.Models;
using VoterSystem.SignalR.Services;

namespace VoterSystem.SignalR.Hubs;

public class VotesHub : Hub<IVoteNotificationService>
{
    public async Task NotifyVotingResultChanged(VotingUpdatedDto voting)
    {
        await Clients.All.NotifyVotingResultChanged(voting);
    }
}