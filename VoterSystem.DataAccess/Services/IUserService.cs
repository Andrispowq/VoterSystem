using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for interacting with users, login, refresh, logout, and things of this nature
/// </summary>
public interface IUserService
{
    //CRUD
    Task<Result<List<User>, ServiceError>> GetAllUsersAsync();
    Task<Option<ServiceError>> CreateUser(User user, string password, Role? role = null);
    //Helper
    Task<bool> AnyAdmins();
    //Auth userflow
    Task<Result<Tokens, ServiceError>> LoginAsync(string email, string password);
    Task<Result<Tokens, ServiceError>> RedeemRefreshTokenAsync(Guid refreshToken);
    Task<Option<ServiceError>> LogoutAsync();
    Task<Option<ServiceError>> ChangePasswordAsync(string oldPassword, string newPassword);
    Task<Result<string, ServiceError>> GenerateEmailConfirmTokenAsync();

    Task<Option<ServiceError>> ConfirmEmailAsync(string token);
    Task<Result<string, ServiceError>> GeneratePasswordResetTokenAsync();
    Task<Option<ServiceError>> ResetPasswordAsync(string token, string newPassword);
    //Helper methods
    Task<Result<User, ServiceError>> GetCurrentUserAsync();
    Result<Guid, ServiceError> GetCurrentUserId();
    Task<Result<User, ServiceError>> GetUserByIdAsync(Guid id);
    Task<Result<Role, ServiceError>> GetUserRoleByIdAsync(Guid id);
    Result<Role, ServiceError> GetCurrentUserRole();
    Task<Option<ServiceError>> SetUserRoleAsync(Guid userId, Role role);
    bool IsCurrentUserAdmin();
}