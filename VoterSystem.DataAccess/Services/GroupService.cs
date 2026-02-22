using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

    public async Task<List<Group>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Groups
            .Include(x => x.Members)
            .Where(x => x.Members.Any(y => y.AddedByUserId == UserId))
            .ToListAsync(ct);
    }

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
        
        var access = CheckAccessOn(group, RoleControlAction.Create);
        if (access.IsSome) return access.AsSome.Value;

        await context.Groups.AddAsync(group, ct);

        var member = new GroupMembers
        {
            GroupId = group.GroupId,
            UserId = UserId,
            AddedByUserId = UserId
        };
        
        await context.GroupMembers.AddAsync(member, ct);
        
        var result = await context.SaveChangesAsync(ct);
        if (result.IsSome) return result.AsSome.Value;

        return group;
    }

    public async Task<Option<ServiceError>> DeleteGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        if (!IsAdmin)
        {
            return new UnauthorizedError("You are not authorized to delete a group");
        }

        var group = await context.Groups.FindAsync([groupId], ct);
        if (group is null) return new NotFoundError("Group not found");
        
        var access = CheckAccessOn(group, RoleControlAction.Delete);
        if (access.IsSome) return access.AsSome.Value;

        context.Groups.Remove(group);
        return await context.SaveChangesAsync(ct);
    }

    public async Task<Option<ServiceError>> AddToGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        if (!IsAdmin)
        {
            return new UnauthorizedError("You are not authorized to update a group");
        }

        var group = await context.Groups.FindAsync([groupId], ct);
        if (group is null) return new NotFoundError("Group not found");
        
        var access = CheckAccessOn(group, RoleControlAction.Update);
        if (access.IsSome) return access.AsSome.Value;
        
        var member = new GroupMembers
        {
            GroupId = group.GroupId,
            UserId = userId,
            AddedByUserId = UserId
        };
        
        await context.GroupMembers.AddAsync(member, ct);
        return await context.SaveChangesAsync(ct);
    }

    public async Task<Option<ServiceError>> RemoveFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        if (!IsAdmin)
        {
            return new UnauthorizedError("You are not authorized to update a group");
        }

        var group = await context.Groups.FindAsync([groupId], ct);
        if (group is null) return new NotFoundError("Group not found");
        
        var access = CheckAccessOn(group, RoleControlAction.Update);
        if (access.IsSome) return access.AsSome.Value;
        
        var membership = await context.GroupMembers.FindAsync([groupId, userId], ct);
        if (membership is null) return new NotFoundError("Group member not found");
        
        context.GroupMembers.Remove(membership);
        return await context.SaveChangesAsync(ct);
    }
}