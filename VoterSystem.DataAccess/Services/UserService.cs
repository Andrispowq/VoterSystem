using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class UserService( 
    IHttpContextAccessor httpContextAccessor,
    ILogger<UserService> logger,
    UserManager<User> userManager, 
    SignInManager<User> signInManager,
    ITokenIssuer tokenIssuer) 
    : BaseService<User, UserService>(httpContextAccessor, logger), IUserService
{
    protected override bool CanAccessAll(bool admin) => admin;

    public async Task<bool> AnyAdmins()
    {
        var list = await userManager.GetUsersInRoleAsync("Admin");
        return list.Count > 0;
    }

    public async Task<Result<List<User>, ServiceError>> GetAllUsersAsync()
    {
        if (!IsAdmin)
        {
            return new UnauthorizedError("Access denied");
        }
        
        return await userManager.Users.ToListAsync();
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

    public async Task<Result<TokensDto, ServiceError>> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return new NotFoundError("User not found");
        
        var result = await signInManager.PasswordSignInAsync(user.UserName!, password, 
            isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut) return new UnauthorizedError("Too many failed attempts");
        if (!result.Succeeded) return new UnauthorizedError("Unsuccessful login attempt");

        var accessToken = tokenIssuer.GenerateJwtToken(user);

        //Regenerate refresh token on login
        user.RefreshToken = Guid.NewGuid();
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return new BadRequestError("Login failed");
        
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

        var accessToken = tokenIssuer.GenerateJwtToken(user);

        //Regenerate refresh token on redeeming
        user.RefreshToken = Guid.NewGuid();
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return new BadRequestError("Login failed");
        
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

        return user;
    }

    public async Task<Result<User, ServiceError>> GetUserByIdAsync(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return new NotFoundError("User not found");
        
        if (!IsAdmin && user.Id != UserId)
            return new UnauthorizedError("You may now access this user");

        return user;
    }

    public async Task<Result<User, ServiceError>> GetUserByEmailAsync(string email)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return new NotFoundError("User not found");

        return user;
    }

    public async Task<Result<Role, ServiceError>> GetUserRoleByIdAsync(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return new NotFoundError("User not found");
        
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

    public async Task<Result<User, ServiceError>> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(UserId.ToString());
        if (user is null) return new NotFoundError("User not found");
        return user;
    }
}