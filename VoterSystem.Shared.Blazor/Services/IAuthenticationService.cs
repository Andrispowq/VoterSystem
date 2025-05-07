using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Blazor.ViewModels;

namespace VoterSystem.Shared.Blazor.Services;

public interface IAuthenticationService
{
    //User management
    Task<List<UserDto>> GetUsersAsync();
    Task<bool> PromoteUserToAdminAsync(Guid userId);
    Task<bool> DemoteAdminToUserAsync(Guid userId);
    
    //Our methods
    Task<bool> LoginAsync(LoginViewModel loginBindingViewModel);
    Task<bool> ChangePasswordAsync(ChangePasswordViewModel changePasswordBindingViewModel);
    Task<Role?> GetCurrentRoleAsync();
    Task LogoutAsync();
    Task<bool> TryAutoLoginAsync();
    Task<string?> GetCurrentlyLoggedInUserAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<bool> RegisterAsync(RegisterViewModel registerViewModel);
}