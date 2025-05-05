using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;

namespace VoterSystem.DataAccess.Services;

public class UserService( 
    IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager, 
    SignInManager<User> signInManager,
    ITokenIssuer tokenIssuer) : IUserService
{
    public async Task<bool> AnyAdmins()
    {
        var list = await userManager.GetUsersInRoleAsync("Admin");
        return list.Count > 0;
    }

    public async Task<Option<ServiceError>> AddUserAsync(User user, string password, Role? role = null)
    {
        user.RefreshToken = Guid.NewGuid();

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new BadRequestError($"User creation failed: {result.Errors.First().Description}");
        }

        if (role is not null)
        {
            result = await userManager.AddToRoleAsync(user, role.Value.ToString());
            if (!result.Succeeded)
            {
                return new BadRequestError($"Adding to role failed: {result.Errors.First().Description}");
            }
        }

        return new Option<ServiceError>.None();
    }

    public async Task<Result<Tokens, ServiceError>> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return new NotFoundError("User not found");
        
        var result = await signInManager.PasswordSignInAsync(user.UserName!, password, 
            isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut) return new UnauthorizedError("Too many failed attempts");
        if (!result.Succeeded) return new UnauthorizedError("Unsuccessful login attempt");

        var accessToken = await tokenIssuer.GenerateJwtTokenAsync(user, userManager);
        
        return new Tokens
        {
            AuthToken = accessToken,
            RefreshToken = user.RefreshToken!.Value,
            UserId = user.Id,
        };
    }

    public async Task<Result<Tokens, ServiceError>> RedeemRefreshTokenAsync(Guid refreshToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user is null) return new NotFoundError("Invalid refresh token");

        var accessToken = await tokenIssuer.GenerateJwtTokenAsync(user, userManager);
        
        return new Tokens
        {
            AuthToken = accessToken,
            RefreshToken = user.RefreshToken!.Value,
            UserId = user.Id,
        };
    }

    public async Task<Option<ServiceError>> LogoutAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user.IsError) return user.Error;
        
        await signInManager.SignOutAsync();
        return new Option<ServiceError>.None();
    }

    public async Task<Result<User, ServiceError>> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId.IsError) return userId.Error;

        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null) return new NotFoundError("User not found");

        return user;
    }

    public Result<Guid, ServiceError> GetCurrentUserId()
    {
        var id = httpContextAccessor.HttpContext?.User.FindFirstValue("id");
        if (id is null) return new NotFoundError("No ID found");

        if (Guid.TryParse(id, out Guid userId)) return userId;
        return new BadRequestError("Invalid GUID as ID");
    }

    public async Task<Result<User, ServiceError>> GetUserByIdAsync(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return new NotFoundError("User not found");
        
        var currentId = GetCurrentUserId();
        if (currentId.IsError) return currentId.Error;
        
        if (!IsCurrentUserAdmin() && user.Id != currentId.Value)
            return new UnauthorizedError("You may now access this user");

        return user;
    }

    public Result<List<Role>, ServiceError> GetCurrentUserRoles()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null) return new UnauthorizedError("No user found");
        
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        try
        {
            return roles.Select(Enum.Parse<Role>).ToList();
        }
        catch (Exception e)
        {
            return new BadRequestError(e.Message);
        }
    }

    public async Task<Option<ServiceError>> SetUserRoleAsync(Guid userId, Role role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return new NotFoundError("User not found");
        
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

    public bool IsCurrentUserAdmin()
    {
        var roles = GetCurrentUserRoles();
        if (roles.IsError) return false;

        return roles.Value.Contains(Role.Admin);
    }
}