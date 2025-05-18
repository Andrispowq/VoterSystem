using System.Text.Json;
using Blazored.LocalStorage;
using VoterSystem.Shared.Blazor.Config;
using VoterSystem.Shared.SignalR.Models;
using Microsoft.AspNetCore.SignalR.Client;
using VoterSystem.Shared.Blazor.Infrastructure;

namespace VoterSystem.Shared.Blazor.Services.SignalR;

public class VoteHubService(AppConfig appConfig, JsonSerializerOptions jsonOptions,
    ILocalStorageService localStorageService, IHttpRequestUtility httpRequestUtility) 
    : BaseHubService(appConfig, jsonOptions, localStorageService, httpRequestUtility), IVoteHubService
{
    public event Action<VotingUpdatedDto>? OnVotingResultUpdated;
    
    public async Task StartHubAsync()
    {
        InitHub("/Hubs/VotesHub");

        HubConnection!.On<VotingUpdatedDto>("NotifyVotingResultChanged", dto =>
        {
            Console.WriteLine($"NotifyVotingResultChanged with {dto}");
            OnVotingResultUpdated?.Invoke(dto);
        });

        await ConnectHubAsync();
    }
}