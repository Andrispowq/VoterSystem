using VoterSystem.Web.Admin.Dto;
using VoterSystem.Web.Admin.Infrastructure;
using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Services;

public class VotingsService(IHttpRequestUtility httpRequestUtility) : IVotingsService
{
    public async Task<VotingsViewModel?> GetVotingsAsync()
    {
        try
        {
            var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<List<VotingDto>>("votings");
            return new VotingsViewModel
            {
                Votings = result.Response
            };
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public async Task<VotingDto?> GetVotingByIdAsync(long votingId)
    {
        try
        {
            var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<VotingDto>($"votings/{votingId}");
            return result.Response;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public async Task<VotingResultViewModel?> GetVotingResultAsync(long votingId)
    {
        try
        {
            var result =
                await httpRequestUtility.ExecuteGetHttpRequestAsync<VotingResultDto>($"votings/{votingId}/results");
            return new VotingResultViewModel(result.Response);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public async Task UpdateEndTime(long votingId, DateTime newEndAt)
    {
        try
        {
            await httpRequestUtility.ExecutePatchHttpRequestAsync<DateTime, VotingDto>($"votings/{votingId}/ends-at", 
                    newEndAt);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task UpdateStartTime(long votingId, DateTime newStartAt)
    {
        try
        {
            await httpRequestUtility.ExecutePatchHttpRequestAsync<DateTime, VotingDto>($"votings/{votingId}/starts-at", 
                newStartAt);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task AddVoteChoice(long votingId, string newChoiceName, string newChoiceDescription)
    {
        try
        {
            var request = new VoteChoiceRequestDto
            {
                Name = newChoiceName,
                Description = newChoiceDescription
            };
            
            await httpRequestUtility.ExecutePostHttpRequestAsync<VoteChoiceRequestDto, VotingResultDto>($"votings/{votingId}/choices", 
                request);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task DeleteChoiceAsync(long votingId, long choiceId)
    {
        try
        {
            await httpRequestUtility.ExecuteDeleteHttpRequestAsync($"votings/{votingId}/choices/{choiceId}");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task StartVotingAsync(long votingId)
    {
        try
        {
            await httpRequestUtility.ExecutePostHttpRequestAsync($"votings/{votingId}/start");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}