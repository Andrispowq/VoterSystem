using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class TokenRequestService(
    ICacheService cacheService) 
    : ITokenRequestService
{
    private static string GetKeyFor(Guid id)
    {
        return $"social-login:tokens:{id}";
    }

    public async Task<Result<Guid, ServiceError>> CreateRequestableTokensAsync(TokensDto tokens, CancellationToken ct = default)
    {
        var dtoId = Guid.NewGuid();
        var cacheKey = GetKeyFor(dtoId);

        var result = await cacheService.SetAsync(
            cacheKey,
            tokens,
            TimeSpan.FromMinutes(5),
            ct);
        if (result.IsSome) return result.AsSome.Value;
        return dtoId;
    }

    public async Task<Result<TokensDto, ServiceError>> RequestTokensAsync(Guid id, CancellationToken ct = default)
    {
        var key = GetKeyFor(id);
        return await cacheService.GetAndDeleteAsync<TokensDto>(key, ct);
    }
}
