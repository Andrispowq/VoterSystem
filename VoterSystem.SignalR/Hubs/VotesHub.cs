using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VoterSystem.Shared.SignalR.Interfaces;
using VoterSystem.Shared.SignalR.Models;

namespace VoterSystem.SignalR.Hubs;

[Authorize]
public class VotesHub(IVoteNotificationService service) : Hub<IVoteNotificationService>
{
    public async Task NotifyVotingResultChanged(VotingUpdatedDto voting)
    {
        await service.NotifyVotingResultChanged(voting);
    }
}