using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for interacting with users, login, refresh, logout, and things of this nature
/// </summary>
public interface IUserService
{
    Task<bool> AnyAdmins();
    
    Task<Option<ServiceError>> AddUserAsync(User user, string password, Role? role = null);
    Task<Result<Tokens, ServiceError>> LoginAsync(string email, string password);
    Task<Result<Tokens, ServiceError>> RedeemRefreshTokenAsync(Guid refreshToken);
    Task<Option<ServiceError>> LogoutAsync();
    Task<Result<User, ServiceError>> GetCurrentUserAsync();
    Result<Guid, ServiceError> GetCurrentUserId();
    Task<Result<User, ServiceError>> GetUserByIdAsync(Guid id);
    Result<List<Role>, ServiceError> GetCurrentUserRoles();
    Task<Option<ServiceError>> SetUserRoleAsync(Guid userId, Role role);
    bool IsCurrentUserAdmin();
}