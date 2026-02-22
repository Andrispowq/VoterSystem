using VoterSystem.Shared.Dto;

namespace VoterSystem.Shared.Blazor.Services;

public interface IGroupsService
{
    Task<List<GroupDto>> GetGroupsAsync();
    Task<GroupDto?> GetGroupAsync(Guid id);
    Task<GroupDto?> CreateGroupAsync(CreateGroupRequest request);
    Task DeleteGroupAsync(Guid id);
    Task AddUserAsync(Guid groupId, Guid userId);
    Task RemoveUserAsync(Guid groupId, Guid userId);
}
