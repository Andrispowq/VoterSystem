using VoterSystem.Shared.Blazor.Exception;
using VoterSystem.Shared.Blazor.Infrastructure;
using VoterSystem.Shared.Dto;

namespace VoterSystem.Shared.Blazor.Services;

public class GroupsService(IHttpRequestUtility httpRequestUtility, IToastService toastService)
    : BaseService(toastService), IGroupsService
{
    public async Task<List<GroupDto>> GetGroupsAsync()
    {
        try
        {
            var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<List<GroupDto>>("groups");
            return result.Response;
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            return [];
        }
    }

    public async Task<GroupDto?> GetGroupAsync(Guid id)
    {
        try
        {
            var result = await httpRequestUtility.ExecuteGetHttpRequestAsync<GroupDto>($"groups/{id}");
            return result.Response;
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task<GroupDto?> CreateGroupAsync(CreateGroupRequest request)
    {
        try
        {
            return await httpRequestUtility.ExecutePostHttpRequestAsync<CreateGroupRequest, GroupDto>("groups", request);
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            ShowErrorMessage("Failed to create group");
            return null;
        }
    }

    public async Task DeleteGroupAsync(Guid id)
    {
        try
        {
            await httpRequestUtility.ExecuteDeleteHttpRequestAsync($"groups/{id}");
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            ShowErrorMessage("Failed to delete group");
        }
    }

    public async Task AddUserAsync(Guid groupId, Guid userId)
    {
        try
        {
            await httpRequestUtility.ExecutePostHttpRequestAsync($"groups/{groupId}/members/{userId}");
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            ShowErrorMessage("Failed to add user to group");
        }
    }

    public async Task RemoveUserAsync(Guid groupId, Guid userId)
    {
        try
        {
            await httpRequestUtility.ExecuteDeleteHttpRequestAsync($"groups/{groupId}/members/{userId}");
        }
        catch (HttpRequestErrorException ex)
        {
            Console.WriteLine(ex.Message);
            ShowErrorMessage("Failed to remove user from group");
        }
    }
}
