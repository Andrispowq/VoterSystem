using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Token;

public interface ITokenIssuer
{
    string GenerateJwtToken(User user);
}