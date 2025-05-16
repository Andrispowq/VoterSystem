using VoterSystem.Shared.SignalR.Models;

namespace VoterSystem.Shared.SignalR.Interfaces;

public interface IVoteNotificationService
{
    Task NotifyVotingResultChanged(VotingUpdatedDto voting);
}