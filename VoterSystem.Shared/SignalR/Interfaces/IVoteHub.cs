using VoterSystem.Shared.Dto;
using VoterSystem.Shared.SignalR.Models;

namespace VoterSystem.Shared.SignalR.Interfaces;

public interface IVoteHub
{
    Task NotifyVotingResultChanged(VotingUpdatedDto voting);
}