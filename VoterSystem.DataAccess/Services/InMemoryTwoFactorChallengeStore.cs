using System.Collections.Concurrent;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class InMemoryTwoFactorChallengeStore : ITwoFactorChallengeStore
{
    private readonly ConcurrentDictionary<Guid, TwoFactorChallengeEntry> _challenges = new();

    public Task<Guid> CreateChallengeAsync(Guid userId, string code, TimeSpan ttl, CancellationToken ct = default)
    {
        var challengeId = Guid.NewGuid();
        _challenges[challengeId] = new TwoFactorChallengeEntry
        {
            UserId = userId,
            Code = code,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl)
        };

        return Task.FromResult(challengeId);
    }

    public Task<Result<Guid, ServiceError>> VerifyChallengeAsync(Guid challengeId, string code, CancellationToken ct = default)
    {
        if (!_challenges.TryRemove(challengeId, out var challenge))
        {
            return Task.FromResult<Result<Guid, ServiceError>>(new NotFoundError("Two-factor challenge not found"));
        }

        if (challenge.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Task.FromResult<Result<Guid, ServiceError>>(new UnauthorizedError("Two-factor challenge expired"));
        }

        // TODO: Replace this in-memory verification with Redis-backed challenge storage and TTL enforcement.
        if (!string.Equals(challenge.Code, code, StringComparison.Ordinal))
        {
            return Task.FromResult<Result<Guid, ServiceError>>(new UnauthorizedError("Invalid two-factor code"));
        }

        return Task.FromResult<Result<Guid, ServiceError>>(challenge.UserId);
    }

    private sealed record TwoFactorChallengeEntry
    {
        public required Guid UserId { get; init; }
        public required string Code { get; init; }
        public required DateTime ExpiresAtUtc { get; init; }
    }
}
