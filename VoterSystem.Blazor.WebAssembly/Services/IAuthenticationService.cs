using ELTE.Cinema.Blazor.WebAssembly.ViewModels;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public interface IAuthenticationService
    {
        public Task<bool> LoginAsync(LoginViewModel loginBindingViewModel);
        public Task LogoutAsync();
        public Task<bool> TryAutoLoginAsync();
        public Task<string?> GetCurrentlyLoggedInUserAsync();
    }
}
