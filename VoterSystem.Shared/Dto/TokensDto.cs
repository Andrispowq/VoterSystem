using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class TokensDto(Tokens tokens)
{
    public string AuthToken => tokens.AuthToken;
    public Guid RefreshToken => tokens.RefreshToken;
    public Guid UserId => tokens.UserId;
}