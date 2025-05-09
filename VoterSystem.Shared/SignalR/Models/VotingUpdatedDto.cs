using VoterSystem.Shared.Dto;

namespace VoterSystem.Shared.SignalR.Models;

public class VotingUpdatedDto
{
    public required long VotingId { get; init; }
    public required VotingResultsDto VotingResults { get; init; }
}