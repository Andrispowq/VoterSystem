using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.SignalR.Services;

public class VotingSubscriptionAuthorizationService(VoterSystemDbContext dbContext)
    : IVotingSubscriptionAuthorizationService
{
    public async Task<Option<ServiceError>> ValidateSubscriptionAsync(
        ClaimsPrincipal principal,
        long votingId,
        CancellationToken ct = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedError("Access denied");
        }

        var userIdClaim = principal.FindFirst(TokenIssuerKeys.UserIdKey)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedError("Access denied");
        }

        var isAdmin = principal.Claims.Any(claim =>
            claim is { Type: ClaimTypes.Role, Value: nameof(Role.Admin) });

        var voting = await dbContext.Votings
            .Include(item => item.Group!)
                .ThenInclude(group => group.Members)
            .FirstOrDefaultAsync(item => item.VotingId == votingId, ct);

        if (voting is null)
        {
            return new NotFoundError("Voting not found");
        }

        if (!HasGroupAccess(voting, isAdmin, userId))
        {
            return new UnauthorizedError("Access denied");
        }

        if (isAdmin || voting.CreatedByUserId == userId)
        {
            return new Option<ServiceError>.None();
        }

        var hasVoted = await dbContext.VotingParticipations
            .AnyAsync(participation => participation.VotingId == votingId && participation.UserId == userId, ct);

        return hasVoted
            ? new Option<ServiceError>.None()
            : new UnauthorizedError("Access denied");
    }

    private static bool HasGroupAccess(Voting voting, bool isAdmin, Guid userId)
    {
        if (isAdmin || voting.GroupId is null)
        {
            return true;
        }

        return voting.Group?.Members.Any(member => member.UserId == userId) == true;
    }
}
