using VoterSystem.Web.Admin.Dto;
using VoterSystem.Web.Admin.Infrastructure;
using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Services;

public class VotingsService(IHttpRequestUtility httpRequestUtility) : IVotingsService
{
    public async Task<VotingsViewModel?> GetVotingsAsync()
    {
        var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<List<VotingDto>>("votings");
        return new VotingsViewModel
        {
            Votings = result.Response
        };
    }
}