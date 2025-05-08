using Microsoft.JSInterop;

namespace VoterSystem.Shared.Blazor.Services;

public class NetworkService(IJSRuntime jsRuntime)
{
    private IJSObjectReference? _module;
    public event Action<bool>? OnConnectivityChanged;

    public async Task InitializeAsync()
    {
        _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/network.js");
        await _module.InvokeVoidAsync("registerConnectivityListener", DotNetObjectReference.Create(this));
    }

    [JSInvokable]
    public void UpdateConnectivity(bool isOnline)
    {
        OnConnectivityChanged?.Invoke(isOnline);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}