using VoterSystem.Web.Admin.Dto;

namespace VoterSystem.Web.Admin.ViewModels;

public class VotingsViewModel
{
    public required List<VotingDto> Votings { get; set; }
}