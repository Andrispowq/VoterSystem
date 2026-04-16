using System.Security.Claims;
using VoterSystem.Shared.Functional;

namespace VoterSystem.SignalR.Services;

public interface IVotingSubscriptionAuthorizationService
{
    Task<Option<ServiceError>> ValidateSubscriptionAsync(
        ClaimsPrincipal principal,
        long votingId,
        CancellationToken ct = default);
}
