using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class UserService( 
    IHttpContextAccessor httpContextAccessor,
    ILogger<UserService> logger,
    UserManager<User> userManager, 
    SignInManager<User> signInManager,
    ITokenIssuer tokenIssuer,
    IEmailService emailService) 
    : BaseService<User, UserService>(httpContextAccessor, logger), IUserService
{
    private readonly ILogger<UserService> _logger = logger;

    private static readonly string TokenProviderFor2Fa = TokenOptions.DefaultEmailProvider;

    protected override bool CanAccessAll(bool admin) => admin;

    public async Task<bool> AnyAdmins()
    {
        var list = await userManager.GetUsersInRoleAsync("Admin");
        return list.Any(u => u.DeletedAt is null);
    }

    public async Task<Result<List<User>, ServiceError>> GetAllUsersAsync(string? nameQuery = null)
    {
        if (!IsAdmin)
        {
            return new UnauthorizedError("Access denied");
        }

        var query = userManager.Users
            .Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(nameQuery))
        {
            var normalizedQuery = nameQuery.Trim().ToUpperInvariant();
            query = query.Where(u => u.NormalizedUserName != null &&
                                     /*u.NormalizedUserName.Contains(normalizedQuery)*/
                                     EF.Functions.Like(u.NormalizedUserName, $"%{normalizedQuery}%"));
        }

        return await query.ToListAsync();
    }

    public async Task<Option<ServiceError>> CreateUser(User user, string password)
    {
        user.RefreshToken = Guid.NewGuid();

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new BadRequestError($"User creation failed: {result.Errors.First().Description}");
        }

        result = await userManager.AddToRoleAsync(user, user.Role.ToString());
        if (!result.Succeeded)
        {
            return new BadRequestError($"Adding to role failed: {result.Errors.First().Description}");
        }

        return new Option<ServiceError>.None();
    }

    public async Task<Result<LoginResultDto, ServiceError>> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return new NotFoundError("User not found");
        if (user.IsDeleted) return new UnauthorizedError("User has been deleted");
        
        var result = await signInManager.PasswordSignInAsync(user.UserName!, password, 
            isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut) return new UnauthorizedError("Too many failed attempts");

        if (result.RequiresTwoFactor || user.TwoFactorEnabled)
        {
            var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenProviderFor2Fa);
            var emailResult = await emailService.SendEmailAsync(
                user.Email!,
                "Your two-factor authentication code",
                EmailText.GetTwoFactorCodeEmail(user.Email!, token));
            if (emailResult.IsSome)
            {
                _logger.LogWarning("Failed to send 2FA code in email, error: {Message}", emailResult);
            }

            return LoginResultDto.FromChallenge(new TwoFactorChallengeDto
            {
                Message = "Two-factor authentication required",
                UserId = user.Id
            });
        }
        
        if (!result.Succeeded) return new UnauthorizedError("Unsuccessful login attempt");

        var tokens = await IssueTokensAsync(user);
        if (tokens.IsError) return tokens.Error;
        return LoginResultDto.FromTokens(tokens.Value);
    }

    public async Task<Result<TokensDto, ServiceError>> CompleteTwoFactorLoginAsync(Guid userId, string code)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return new NotFoundError("User not found");
        if (user.IsDeleted) return new UnauthorizedError("User has been deleted");
        
        var success = await userManager.VerifyTwoFactorTokenAsync(user, TokenProviderFor2Fa, code);
        if (!success) return new BadRequestError("Failed to verify two factor authentication");

        return await IssueTokensAsync(user);
    }

    public async Task<Option<ServiceError>> EnableTwoFactorAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user.IsError) return user.Error;

        if (!user.Value.TwoFactorEnabled)
        {
            var setResult = await userManager.SetTwoFactorEnabledAsync(user.Value, true);
            if (!setResult.Succeeded)
            {
                return new BadRequestError($"Failed to enable two-factor authentication: {setResult.Errors.First().Description}");
            }
        }

        var emailResult = await emailService.SendEmailAsync(
            user.Value.Email!,
            "Two-factor authentication enabled",
            EmailText.GetTwoFactorEnabledEmail(user.Value.Email!));
        if (emailResult.IsSome) return emailResult.AsSome.Value;

        return new Option<ServiceError>.None();
    }

    private async Task<Result<TokensDto, ServiceError>> IssueTokensAsync(User user)
    {
        var accessToken = tokenIssuer.GenerateJwtToken(user);

        var ensureRefresh = await EnsureRefreshTokenAsync(user);
        if (ensureRefresh.IsSome) return ensureRefresh.AsSome.Value;
        
        return new TokensDto
        {
            AuthToken = accessToken,
            RefreshToken = user.RefreshToken!.Value,
            UserId = user.Id,
        };
    }

    public async Task<Result<TokensDto, ServiceError>> RedeemRefreshTokenAsync(Guid refreshToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user is null) return new NotFoundError("Invalid refresh token");
        if (user.IsDeleted) return new UnauthorizedError("User has been deleted");

        var accessToken = tokenIssuer.GenerateJwtToken(user);

        var ensureRefresh = await EnsureRefreshTokenAsync(user);
        if (ensureRefresh.IsSome) return ensureRefresh.AsSome.Value;
        
        return new TokensDto
        {
            AuthToken = accessToken,
            RefreshToken = user.RefreshToken!.Value,
            UserId = user.Id
        };
    }

    public async Task<Option<ServiceError>> LogoutAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user.IsError) return user.Error;
        await signInManager.SignOutAsync();
        return new Option<ServiceError>.None();
    }

    public async Task<Option<ServiceError>> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        var user = await GetCurrentUserAsync();
        if (user.IsError) return user.Error;

        if (oldPassword == newPassword)
        {
            return new BadRequestError("Passwords cannot be the same");
        }

        var result = await userManager.ChangePasswordAsync(user.Value, oldPassword, newPassword);
        if (result.Succeeded) return new Option<ServiceError>.None();

        return new ConflictError(result.Errors.First().Description);
    }

    public async Task<Result<string, ServiceError>> GenerateEmailConfirmTokenAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user.IsError) return user.Error;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user.Value);
        return token;
    }

    public async Task<Option<ServiceError>> ConfirmEmailAsync(string email, string token)
    {
        var user = await GetUserByEmailAsync(email);
        if (user.IsError) return user.Error;

        var result = await userManager.ConfirmEmailAsync(user.Value, token);
        if (result.Succeeded) return new Option<ServiceError>.None();

        return new ConflictError(result.Errors.First().Description);
    }

    public async Task<Result<string, ServiceError>> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        if (user.IsError) return user.Error;

        /*if (!user.Value.EmailConfirmed)
        {
            return new UnauthorizedError("Cannot reset password with unconfirmed email");
        }*/

        var token = await userManager.GeneratePasswordResetTokenAsync(user.Value);
        return token;
    }

    public async Task<Option<ServiceError>> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await GetUserByEmailAsync(email);
        if (user.IsError) return user.Error;

        var result = await userManager.ResetPasswordAsync(user.Value, token, newPassword);
        if (result.Succeeded) return new Option<ServiceError>.None();

        return new ConflictError(result.Errors.First().Description);
    }

    public async Task<Result<User, ServiceError>> GetCurrentUserAsync()
    {
        var userId = MaybeUserId;
        if (userId is null) return new NotFoundError("No user present");
        
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null) return new NotFoundError("User not found");
        if (user.IsDeleted) return new NotFoundError("User not found");

        return user;
    }

    public async Task<Result<User, ServiceError>> GetUserByIdAsync(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return new NotFoundError("User not found");
        if (user.IsDeleted) return new NotFoundError("User not found");
        
        if (!IsAdmin && user.Id != UserId)
            return new UnauthorizedError("You may now access this user");

        return user;
    }

    public async Task<Result<User, ServiceError>> GetUserByEmailAsync(string email)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return new NotFoundError("User not found");
        if (user.IsDeleted) return new NotFoundError("User not found");

        return user;
    }

    public async Task<Result<Role, ServiceError>> GetUserRoleByIdAsync(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return new NotFoundError("User not found");
        if (user.IsDeleted) return new NotFoundError("User not found");
        
        var result = await userManager.GetRolesAsync(user);
        return result.Select(Enum.Parse<Role>).FirstOrDefault();
    }
    
    public async Task<Option<ServiceError>> SetUserRoleAsync(Guid userId, Role role)
    {
        if (!IsAdmin || userId == UserId)
        {
            return new UnauthorizedError("You may not modify this user");
        }
        
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return new NotFoundError("User not found");
        if (user.IsDeleted) return new NotFoundError("User not found");
        
        user.Role = role;
        await userManager.UpdateAsync(user);
        
        var prevRoles = await userManager.GetRolesAsync(user);
        var result = await userManager.RemoveFromRolesAsync(user, prevRoles);
        if (!result.Succeeded)
        {
            return new BadRequestError($"Failed to remove previous roles: {result.Errors.First().Description}");
        }
        
        result = await userManager.AddToRoleAsync(user, role.ToString());
        if (!result.Succeeded)
        {
            return new BadRequestError($"Failed to add to role: {result.Errors.First().Description}");
        }
        
        return new Option<ServiceError>.None();
    }

    public async Task<Result<TokensDto, ServiceError>> HandleExternalAuthAsync(ThirdPartyAuthRequest request)
    {
        var provider = request.Provider.ToString();
        var providerKey = request.ProviderKey;
        var user = await userManager.FindByLoginAsync(provider, providerKey);
        if (user is not null && user.IsDeleted)
        {
            return new UnauthorizedError("User has been deleted");
        }
        if (user is null)
        {
            var register = await HandleExternalRegisterAsync(request);
            if (register.IsError)
            {
                _logger.LogWarning("Failed to register user with external provider {Provider}: {Error}", 
                    provider, register.Error);
                return register.Error;
            }
            user = register.Value;
        }

        if (user.IsDeleted)
        {
            return new UnauthorizedError("User has been deleted");
        }

        var result = await HandleExternalLoginAsync(user);
        if (result.IsError)
        {
            _logger.LogWarning("Failed to handle external login for user {UserId} with provider {Provider}: {Error}", 
                user.Id, provider, result.Error);
        }
        return result;
    }

    private async Task<Option<ServiceError>> EnsureRefreshTokenAsync(User user)
    {
        if (user.RefreshToken.HasValue)
        {
            return new Option<ServiceError>.None();
        }

        user.RefreshToken = Guid.NewGuid();
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _logger.LogWarning("Failed to persist refresh token for user {UserId}: {Error}",
                user.Id, updateResult.Errors.FirstOrDefault()?.Description);
            return new BadRequestError("Login failed");
        }

        return new Option<ServiceError>.None();
    }

    private async Task<Result<User, ServiceError>> HandleExternalRegisterAsync(ThirdPartyAuthRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && user.IsDeleted)
        {
            return new UnauthorizedError("User has been deleted");
        }
        if (user is null)
        {
            user = new User
            {
                Name = request.Name,
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                LoginMode = UserLoginMode.Social,
                Role = Role.User,
                RefreshToken = Guid.NewGuid()
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Failed to create new user for external registration: {Errors}", createResult.Errors.First().Description);
                return new BadRequestError("Could not create new user");
            }
            const string role = nameof(Role.User);
            await userManager.AddToRoleAsync(user, role);
        }

        // link login to existing account
        var provider = request.Provider.ToString();
        var providerKey = request.ProviderKey;
        var info = new UserLoginInfo(provider, providerKey, provider);
        var linkResult = await userManager.AddLoginAsync(user, info);
        if (!linkResult.Succeeded)
        {
            _logger.LogWarning("Failed to link external login for user {UserId} with provider {Provider}: {Errors}", 
                user.Id, provider, linkResult.Errors);
            return new BadRequestError("Could not link Google login");
        }

        return user;
    }

    private async Task<Result<TokensDto, ServiceError>> HandleExternalLoginAsync(User user)
    {
        return await IssueTokensAsync(user);
    }
}
