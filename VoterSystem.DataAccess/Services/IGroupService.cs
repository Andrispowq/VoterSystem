using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public interface IGroupService
{
    /// <summary>
    /// Returns all the groups the user is associated with
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<Group>> GetAllAsync(CancellationToken ct = default);
    
    Task<Result<Group, ServiceError>> GetByIdAsync(long groupId, CancellationToken ct = default);
    
    Task<Result<Group, ServiceError>> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<Option<ServiceError>> DeleteGroupAsync(long groupId, CancellationToken ct = default);
    
    Task<Option<ServiceError>> AddToGroupAsync(long groupId, Guid userId, CancellationToken ct = default);
    Task<Option<ServiceError>> RemoveFromGroupAsync(long groupId, Guid userId, CancellationToken ct = default);
}