using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.Shared.Blazor.Config;

namespace VoterSystem.Shared.Blazor.Services.SignalR
{
    public abstract class BaseHubService(AppConfig appConfig, JsonSerializerOptions jsonOptions) : IBaseHubService
    {
        protected HubConnection? HubConnection;

        protected void InitHub(string hubName)
        {
            var fullUri = new Uri(new Uri(appConfig.HubBaseUrl), hubName);

            HubConnection = new HubConnectionBuilder()
                .WithUrl(fullUri)
                .AddJsonProtocol(config =>
                {
                    config.PayloadSerializerOptions = jsonOptions;
                })
                .WithAutomaticReconnect()
                .Build();
        }

        protected async Task ConnectHubAsync()
        {
            if (HubConnection!.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
            }
        }

        public async Task DisconnectHubAsync()
        {
            if (HubConnection!.State != HubConnectionState.Disconnected)
            {
                await HubConnection.StopAsync();
            }
        }
    }
}
