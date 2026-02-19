using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public sealed class GroupService(
    VoterSystemDbContext context,
    IHttpContextAccessor http,
    ILogger<GroupService> logger) 
    : BaseService<Group, GroupService>(http, logger), IGroupService
{
    protected override bool CanAccessAll(bool admin) => true;
    
    public async Task<Result<Group, ServiceError>> GetByIdAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = await context.Groups.FindAsync([groupId], ct);
        if (group is null) return new NotFoundError("Group not found");

        var access = CheckAccessOn(group, RoleControlAction.Access);
        if (access.IsSome) return access.AsSome.Value;
        
        return group;
    }

    public async Task<Result<Group, ServiceError>> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin)
        {
            return new UnauthorizedError("You are not authorized to create a group");
        }

        var group = new Group
        {
            Name = request.Name,
            Description = request.Description,
            CreatorUserId = UserId
        };

        throw new NotImplementedException();
    }

    public async Task<Result<Group, ServiceError>> DeleteGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Option<ServiceError>> AddToGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Option<ServiceError>> RemoveFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}