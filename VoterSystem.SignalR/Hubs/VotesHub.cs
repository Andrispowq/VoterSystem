using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VoterSystem.Shared.SignalR.Interfaces;
using VoterSystem.SignalR.Services;

namespace VoterSystem.SignalR.Hubs;

[Authorize]
public class VotesHub(IVotingSubscriptionAuthorizationService authorizationService) : Hub<IVoteNotificationService>
{
    public async Task SubscribeToVotingAsync(long votingId)
    {
        var principal = Context.User ?? new ClaimsPrincipal();
        var error = await authorizationService.ValidateSubscriptionAsync(principal, votingId, Context.ConnectionAborted);
        if (error.IsSome)
        {
            throw new HubException(error.AsSome.Value.Message);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, VotingHubGroupNames.ForVoting(votingId), Context.ConnectionAborted);
    }

    public async Task UnsubscribeFromVotingAsync(long votingId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            VotingHubGroupNames.ForVoting(votingId),
            Context.ConnectionAborted);
    }
}
