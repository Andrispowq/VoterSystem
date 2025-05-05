using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Services;

public interface IVotingsService
{
    Task<VotingsViewModel?> GetVotingsAsync();
}