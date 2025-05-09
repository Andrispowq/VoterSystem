using Microsoft.Extensions.DependencyInjection;
using VoterSystem.SignalR.Services;

namespace VoterSystem.SignalR;

public static class DependencyInjection
{
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSingleton<IVoteNotificationService, VoteNotificationService>();

        return services;
    }
}