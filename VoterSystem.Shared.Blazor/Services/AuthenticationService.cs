using System.Net.Http.Json;
using Blazored.LocalStorage;
using ELTE.Cinema.Blazor.WebAssembly.Services;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Blazor.Exception;
using VoterSystem.Shared.Blazor.Infrastructure;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Blazor.ViewModels;

namespace VoterSystem.Shared.Blazor.Services;

public class AuthenticationService(
    HttpClient httpClient,
    ILocalStorageService localStorageService,
    IToastService toastService,
    IHttpRequestUtility httpRequestUtility)
    : BaseService(toastService), IAuthenticationService
{
    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            var response = await httpRequestUtility.ExecuteGetHttpRequestAsync<List<UserDto>>("users/all");
            return response.Response;
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            return [];
        }
    }

    public async Task<bool> PromoteUserToAdminAsync(Guid userId)
    {
        try
        {
            await httpRequestUtility.ExecutePatchHttpRequestAsync($"users/promote?userId={userId}");
            return true;
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<bool> DemoteAdminToUserAsync(Guid userId)
    {
        try
        {
            await httpRequestUtility.ExecutePatchHttpRequestAsync($"users/demote?userId={userId}");
            return true;
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<bool> LoginAsync(LoginViewModel loginBindingViewModel)
    {
        UserLoginRequestDto loginDto = new UserLoginRequestDto
        {
            Email = loginBindingViewModel.Email ?? "",
            Password = loginBindingViewModel.Password ?? ""
        };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("/api/v1/users/login", loginDto);
        }
        catch (System.Exception)
        {
            ShowErrorMessage("Unknown error occured");
            return false;
        }

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadFromJsonAsync<TokensDto>()
                               ?? throw new System.Exception("Error with auth response.");

            await localStorageService.SetItemAsStringAsync("AuthToken", responseBody.AuthToken);
            await localStorageService.SetItemAsStringAsync("RefreshToken", responseBody.RefreshToken.ToString());
            await localStorageService.SetItemAsStringAsync("UserId", responseBody.UserId.ToString());
            await SetCurrentUserNameAsync(responseBody.UserId);

            return true;
        }

        await HandleHttpError(response);

        return false;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordViewModel changePasswordBindingViewModel)
    {
        try
        {
            var dto = new UserChangePasswordRequestDto
            {
                OldPassword = changePasswordBindingViewModel.OldPassword,
                NewPassword = changePasswordBindingViewModel.NewPassword,
            };
            
            await httpRequestUtility.ExecutePutHttpRequestAsync($"users/change-password", dto);
            await LogoutAsync();
            
            return true;
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<Role?> GetCurrentRoleAsync()
    {
        return (await GetCurrentUserAsync())?.Role;
    }

    public async Task LogoutAsync()
    {
        try
        {
            await httpRequestUtility.ExecuteDeleteHttpRequestAsync("users/logout");
        }
        catch (HttpRequestException) { }

        var keys = new List<string> { "AuthToken", "RefreshToken", "UserName" };
        await localStorageService.RemoveItemsAsync(keys);
    }

    public async Task<bool> TryAutoLoginAsync()
    {
        if (!await localStorageService.ContainKeyAsync("RefreshToken"))
            return false;
            
        try
        {
            await httpRequestUtility.RedeemTokenAsync();
        }
        catch (HttpRequestErrorException)
        {
            var keys = new List<string> { "AuthToken", "RefreshToken", "UserName" };
            await localStorageService.RemoveItemsAsync(keys);
            return false;
        }
        return true;
    }

    public async Task<string?> GetCurrentlyLoggedInUserAsync()
    {
        return await localStorageService.GetItemAsStringAsync("UserName");
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var userId = await localStorageService.GetItemAsStringAsync("UserId");
            var response = await httpRequestUtility.ExecuteGetHttpRequestAsync<UserDto>($"users/{userId}");
            return response.Response;
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task<bool> RegisterAsync(RegisterViewModel registerViewModel)
    {
        UserRegisterRequestDto register = new UserRegisterRequestDto
        {
            Email = registerViewModel.Email,
            Password = registerViewModel.Password,
            Name = registerViewModel.Username
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync("users/register", register);
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {
            ShowErrorMessage("Unknown error occured");
            return false;
        }
    }

    public async Task<bool> RequestEmailConfirmationAsync()
    {
        try
        {
            await httpRequestUtility.ExecutePostHttpRequestAsync("users/confirm-email-request");
            return true;
        }
        catch (System.Exception)
        {
            ShowErrorMessage("Unknown error occured");
            return false;
        }
    }

    public async Task<bool> ConfirmEmailAsync(UserEmailConfirmRequestDto dto)
    {
        try
        {
            await httpRequestUtility.ExecutePostHttpRequestAsync("users/confirm-email", dto);
            return true;
        }
        catch (System.Exception)
        {
            ShowErrorMessage("Unknown error occured");
            return false;
        }
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        try
        {
            await httpRequestUtility.ExecutePostHttpRequestAsync("users/reset-password-request", email);
            return true;
        }
        catch (System.Exception)
        {
            ShowErrorMessage("Unknown error occured");
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(UserPasswordResetRequestDto dto)
    {
        try
        {
            await httpRequestUtility.ExecutePostHttpRequestAsync("users/reset-password", dto);
            return true;
        }
        catch (System.Exception)
        {
            ShowErrorMessage("Unknown error occured");
            return false;
        }
    }

    private async Task SetCurrentUserNameAsync(Guid currentUserId)
    {
        try
        {
            var response = await httpRequestUtility.ExecuteGetHttpRequestAsync<UserDto>($"users/{currentUserId}");
            await localStorageService.SetItemAsStringAsync("UserName", response.Response.Name);
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}