using VoterSystem.Shared.Dto;

namespace VoterSystem.Shared.Blazor.ViewModels;

public class VotingsViewModel
{
    public required List<VotingDto> Votings { get; set; }
}