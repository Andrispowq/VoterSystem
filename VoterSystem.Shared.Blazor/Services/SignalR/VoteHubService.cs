using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
using VoterSystem.Shared.Blazor.Config;
using VoterSystem.Shared.Blazor.Infrastructure;
using VoterSystem.Shared.SignalR.Models;

namespace VoterSystem.Shared.Blazor.Services.SignalR;

public class VoteHubService(AppConfig appConfig, JsonSerializerOptions jsonOptions,
    ILocalStorageService localStorageService, IHttpRequestUtility httpRequestUtility) 
    : BaseHubService(appConfig, jsonOptions, localStorageService, httpRequestUtility), IVoteHubService
{
    private readonly HashSet<long> _subscribedVotingIds = [];
    private bool _hubHandlersRegistered;

    public event Action<VotingUpdatedDto>? OnVotingResultUpdated;
    
    public async Task StartHubAsync()
    {
        InitHub("/Hubs/VotesHub");

        if (HubConnection is null)
        {
            return;
        }

        if (!_hubHandlersRegistered)
        {
            HubConnection.On<VotingUpdatedDto>("NotifyVotingResultChanged", dto =>
            {
                Console.WriteLine($"NotifyVotingResultChanged with {dto}");
                OnVotingResultUpdated?.Invoke(dto);
            });

            HubConnection.Reconnected += async _ =>
            {
                foreach (var votingId in _subscribedVotingIds)
                {
                    await HubConnection.InvokeAsync(nameof(SubscribeToVotingAsync), votingId);
                }
            };

            _hubHandlersRegistered = true;
        }

        await ConnectHubAsync();
    }

    public async Task<bool> SubscribeToVotingAsync(long votingId)
    {
        if (_subscribedVotingIds.Contains(votingId))
        {
            return true;
        }

        try
        {
            await StartHubAsync();
            if (HubConnection is null)
            {
                return false;
            }

            await HubConnection.InvokeAsync(nameof(SubscribeToVotingAsync), votingId);
            _subscribedVotingIds.Add(votingId);

            return true;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Failed to subscribe to voting {votingId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UnsubscribeFromVotingAsync(long votingId)
    {
        try
        {
            if (HubConnection is not null && HubConnection.State == HubConnectionState.Connected)
            {
                await HubConnection.InvokeAsync(nameof(UnsubscribeFromVotingAsync), votingId);
            }

            _subscribedVotingIds.Remove(votingId);
            return true;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Failed to unsubscribe from voting {votingId}: {ex.Message}");
            return false;
        }
    }

    public override async Task DisconnectHubAsync()
    {
        if (HubConnection is not null && HubConnection.State == HubConnectionState.Connected)
        {
            foreach (var votingId in _subscribedVotingIds.ToList())
            {
                await UnsubscribeFromVotingAsync(votingId);
            }
        }

        _subscribedVotingIds.Clear();
        _hubHandlersRegistered = false;
        await base.DisconnectHubAsync();
    }
}
