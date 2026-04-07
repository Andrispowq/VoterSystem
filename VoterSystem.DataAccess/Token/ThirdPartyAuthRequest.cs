using VoterSystem.Shared.Dto;

namespace VoterSystem.DataAccess.Token;

public class ThirdPartyAuthRequest
{
    public required ExternalLoginProvider Provider { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string ProviderKey { get; init; }
}