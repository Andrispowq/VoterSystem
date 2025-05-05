using System.Net.Http.Json;
using Blazored.LocalStorage;
using ELTE.Cinema.Blazor.WebAssembly.Services;
using VoterSystem.Web.Admin.Dto;
using VoterSystem.Web.Admin.Exception;
using VoterSystem.Web.Admin.Infrastructure;
using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Services;

public class AuthenticationService(
    HttpClient httpClient,
    ILocalStorageService localStorageService,
    IToastService toastService,
    IHttpRequestUtility httpRequestUtility)
    : BaseService(toastService), IAuthenticationService
{
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
            await SetCurrentUserNameAsync(responseBody.UserId);

            return true;
        }

        await HandleHttpError(response);

        return false;
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
            var keys = new List<string>() { "AuthToken", "RefreshToken", "UserName" };
            await localStorageService.RemoveItemsAsync(keys);
            return false;
        }
        return true;
    }

    public async Task<string?> GetCurrentlyLoggedInUserAsync()
    {
        return await localStorageService.GetItemAsStringAsync("UserName");
    }

    private async Task SetCurrentUserNameAsync(Guid currentUserId)
    {
        var response = await httpRequestUtility.ExecuteGetHttpRequestAsync<UserDto>($"users/{currentUserId}");
        await localStorageService.SetItemAsStringAsync("UserName", response.Response.Name);
    }
}