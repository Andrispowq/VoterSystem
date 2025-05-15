using VoterSystem.Shared.SignalR.Models;

namespace VoterSystem.SignalR.Services;

public interface IVoteNotificationService
{
    Task NotifyVotingResultChanged(VotingUpdatedDto voting);
}