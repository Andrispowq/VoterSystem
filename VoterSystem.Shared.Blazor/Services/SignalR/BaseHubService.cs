using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.Shared.Blazor.Config;
using VoterSystem.Shared.Blazor.Infrastructure;

namespace VoterSystem.Shared.Blazor.Services.SignalR
{
    public abstract class BaseHubService(AppConfig appConfig, JsonSerializerOptions jsonOptions,
        ILocalStorageService localStorageService, IHttpRequestUtility httpRequestUtility) : IBaseHubService
    {
        protected HubConnection? HubConnection;

        protected void InitHub(string hubName)
        {
            if (HubConnection is not null)
            {
                return;
            }

            var fullUri = new Uri(new Uri(appConfig.HubBaseUrl), hubName);

            HubConnection = new HubConnectionBuilder()
                .WithUrl(fullUri, options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var token = await localStorageService.GetItemAsStringAsync("AuthToken");

                        if (token is null || httpRequestUtility.IsAccessTokenExpired(token))
                        {
                            token = (await httpRequestUtility.RedeemTokenAsync()).AuthToken;
                        }

                        return token;
                    };
                })
                .AddJsonProtocol(config =>
                {
                    config.PayloadSerializerOptions = jsonOptions;
                })
                .WithAutomaticReconnect()
                .Build();
        }

        protected async Task ConnectHubAsync()
        {
            if (HubConnection is not null && HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
            }
        }

        public virtual async Task DisconnectHubAsync()
        {
            if (HubConnection is null)
            {
                return;
            }

            if (HubConnection.State != HubConnectionState.Disconnected)
            {
                await HubConnection.StopAsync();
            }

            await HubConnection.DisposeAsync();
            HubConnection = null;
        }
    }
}
