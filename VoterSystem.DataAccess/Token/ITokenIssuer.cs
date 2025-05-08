using Microsoft.AspNetCore.Identity;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Token;

public interface ITokenIssuer
{
    Task<string> GenerateJwtTokenAsync(User user, UserManager<User> userManager);
}