using VoterSystem.Shared.Blazor.Infrastructure;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Blazor.ViewModels;

namespace VoterSystem.Shared.Blazor.Services;

public class VotingsService(IHttpRequestUtility httpRequestUtility) : IVotingsService
{
    public async Task<VotingsViewModel?> GetVotedVotingsAsync()
    {
        try
        {
            var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<List<VotingDto>>("votings/voted");
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

    public async Task<VotingsViewModel?> GetVotableVotingsAsync()
    {
        try
        {
            var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<List<VotingDto>>("votings/votable");
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

    public async Task<VotingResultsDto?> GetVotingResultAsync(long votingId)
    {
        try
        {
            var result =
                await httpRequestUtility.ExecuteGetHttpRequestAsync<VotingResultsDto>($"votings/{votingId}/results");
            return result.Response;
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
            
            await httpRequestUtility.ExecutePostHttpRequestAsync<VoteChoiceRequestDto, VotingDto>($"votings/{votingId}/choices", 
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

    public async Task<VotingDto?> CreateVotingAsync(VotingCreateRequestDto dto)
    {
        try
        {
            return await httpRequestUtility.ExecutePostHttpRequestAsync<VotingCreateRequestDto, VotingDto>("votings", dto);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public async Task DeleteVotingAsync(long votingId)
    {
        try
        {
            await httpRequestUtility.ExecuteDeleteHttpRequestAsync($"votings/{votingId}");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task<List<VoteDto>?> GetMyVotesAsync()
    {
        try
        {
            var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<List<VoteDto>>($"votes");
            return result.Response;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public async Task<bool> VoteAsync(long choiceId)
    {
        try
        {
            await httpRequestUtility.ExecutePostHttpRequestAsync($"votes/cast-vote?choiceId={choiceId}");
            return true;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }
}