using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Services;

public interface IAuthenticationService
{
    public Task<bool> LoginAsync(LoginViewModel loginBindingViewModel);
    public Task LogoutAsync();
    public Task<bool> TryAutoLoginAsync();
    public Task<string?> GetCurrentlyLoggedInUserAsync();
}