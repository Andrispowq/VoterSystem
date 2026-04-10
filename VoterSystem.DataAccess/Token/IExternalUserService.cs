using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Token;

public interface IExternalUserService
{
    /// <summary>
    /// Handles the authentication request coming in from a third-party provider
    /// </summary>
    /// <param name="request">Info about the request</param>
    /// <returns>The generated tokens, or an error</returns>
    Task<Result<TokensDto, ServiceError>> HandleExternalAuthAsync(
        ThirdPartyAuthRequest request);
}