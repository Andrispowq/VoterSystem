using Microsoft.JSInterop;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public class NetworkService
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference? _module;
        public event Action<bool>? OnConnectivityChanged;

        public NetworkService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/network.js");
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
}
