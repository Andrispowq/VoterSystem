using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Token;

public class TokenIssuer(IOptions<JwtSettings> jwtSettingOptions) : ITokenIssuer
{
    private JwtSettings JwtSettings => jwtSettingOptions.Value;
    
    public static string AuthTokenKey => "VotingSystemAuthToken";
    
    public string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TokenIssuerKeys.UserIdKey, user.Id.ToString()),
            new(TokenIssuerKeys.UsernameKey, user.UserName!),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: JwtSettings.Issuer,
            audience: JwtSettings.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(JwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}