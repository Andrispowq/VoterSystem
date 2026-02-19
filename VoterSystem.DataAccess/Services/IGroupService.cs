using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public interface IGroupService
{
    Task<Result<Group, ServiceError>> GetByIdAsync(Guid groupId, CancellationToken ct = default);
    
    Task<Result<Group, ServiceError>> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<Result<Group, ServiceError>> DeleteGroupAsync(Guid groupId, CancellationToken ct = default);
    
    Task<Option<ServiceError>> AddToGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<Option<ServiceError>> RemoveFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default);
}