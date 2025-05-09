using Microsoft.AspNetCore.SignalR;
using VoterSystem.Shared.SignalR.Models;
using VoterSystem.SignalR.Hubs;

namespace VoterSystem.SignalR.Services;

public class VoteNotificationService(IHubContext<VotesHub> hubContext) : IVoteNotificationService
{
    public async Task NotifyVotingResultChanged(VotingUpdatedDto voting)
    {
        await hubContext.Clients.All.SendAsync("NotifyVotingResultChanged", voting);
    }
}