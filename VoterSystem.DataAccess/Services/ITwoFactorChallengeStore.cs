using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public interface ITwoFactorChallengeStore
{
    Task<Guid> CreateChallengeAsync(Guid userId, string code, TimeSpan ttl, CancellationToken ct = default);
    Task<Result<Guid, ServiceError>> VerifyChallengeAsync(Guid challengeId, string code, CancellationToken ct = default);
}
