using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public interface IUserService
{
    Task<Option<ServiceError>> AddUserAsync(User user, string password, Role? role = null);
    Task<Result<Tokens, ServiceError>> LoginAsync(string email, string password);
    Task<Result<Tokens, ServiceError>> RedeemRefreshTokenAsync(Guid refreshToken);
    Task<Option<ServiceError>> LogoutAsync();
    Task<Result<User, ServiceError>> GetCurrentUserAsync();
    Result<Guid, ServiceError> GetCurrentUserId();
    Task<Result<User, ServiceError>> GetUserByIdAsync(Guid id);
    Result<List<Role>, ServiceError> GetCurrentUserRoles();
    bool IsCurrentUserAdmin();
}