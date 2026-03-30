using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

/// <summary>
/// Used for interacting with users, login, refresh, logout, and things of this nature
/// </summary>
public interface IUserService : IExternalUserService
{
    //CRUD
    Task<Result<List<User>, ServiceError>> GetAllUsersAsync();
    Task<Option<ServiceError>> CreateUser(User user, string password);
    //Helper
    Task<bool> AnyAdmins();
    //Auth userflow
    Task<Result<LoginResultDto, ServiceError>> LoginAsync(string email, string password);
    Task<Result<TokensDto, ServiceError>> CompleteTwoFactorLoginAsync(Guid challengeId, string code);
    Task<Result<TokensDto, ServiceError>> RedeemRefreshTokenAsync(Guid refreshToken);
    Task<Option<ServiceError>> LogoutAsync();
    Task<Option<ServiceError>> ChangePasswordAsync(string oldPassword, string newPassword);
    Task<Option<ServiceError>> EnableTwoFactorAsync();
    Task<Result<string, ServiceError>> GenerateEmailConfirmTokenAsync();

    Task<Option<ServiceError>> ConfirmEmailAsync(string email, string token);
    Task<Result<string, ServiceError>> GeneratePasswordResetTokenAsync(string email);
    Task<Option<ServiceError>> ResetPasswordAsync(string email, string token, string newPassword);
    //Helper methods
    Task<Result<User, ServiceError>> GetUserByIdAsync(Guid id);
    Task<Result<User, ServiceError>> GetUserByEmailAsync(string email);
    Task<Result<Role, ServiceError>> GetUserRoleByIdAsync(Guid id);
    Task<Option<ServiceError>> SetUserRoleAsync(Guid userId, Role role);
    Task<Result<User, ServiceError>> GetCurrentUserAsync(CancellationToken ct = default);
}