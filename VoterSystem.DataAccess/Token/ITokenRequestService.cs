using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Token;

public interface ITokenRequestService
{
    /// <summary>
    /// Stores the tokens provided in Redis under a freshly generated key,
    /// and returns the GUID identifying it.
    /// </summary>
    /// <param name="tokens">The tokens to store</param>
    /// <param name="ct"></param>
    /// <returns>The GUID from which the tokens are queryable until the TTL</returns>
    Task<Result<Guid, ServiceError>> CreateRequestableTokensAsync(
        TokensDto tokens, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves the stored tokens
    /// </summary>
    /// <param name="id">The Id of the stored tokens</param>
    /// <param name="ct"></param>
    /// <returns>The stored tokens, or an error</returns>
    Task<Result<TokensDto, ServiceError>> RequestTokensAsync(
        Guid id, CancellationToken ct = default);
}