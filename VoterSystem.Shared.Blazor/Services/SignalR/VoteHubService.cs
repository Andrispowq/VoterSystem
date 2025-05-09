using System.Text.Json;
using VoterSystem.Shared.Blazor.Config;
using VoterSystem.Shared.SignalR.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace VoterSystem.Shared.Blazor.Services.SignalR;

public class VoteHubService(AppConfig appConfig, JsonSerializerOptions jsonOptions) : BaseHubService(appConfig, jsonOptions), IVoteHubService
{
    public event Action<VotingUpdatedDto>? OnVotingResultUpdated;
    
    public async Task StartHubAsync()
    {
        InitHub("VotesHub");

        HubConnection!.On<VotingUpdatedDto>("NotifyVotingResultChanged", dto =>
        {
            Console.WriteLine($"NotifyVotingResultChanged with {dto}");
            OnVotingResultUpdated?.Invoke(dto);
        });

        await ConnectHubAsync();
    }
}