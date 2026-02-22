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
            .AsNoTracking()
            .Include(x => x.Members)
                .ThenInclude(m => m.User)
            .Where(x => x.Members.Any(y => y.UserId == UserId))
            .ToListAsync(ct);
    }

    public async Task<Result<Group, ServiceError>> GetByIdAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = await context.Groups
            .AsNoTracking()
            .Include(x => x.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(x => x.GroupId == groupId, ct);
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

        var id = Guid.NewGuid();
        var group = new Group
        {
            GroupId = id,
            Name = request.Name,
            Description = request.Description,
            CreatorUserId = UserId
        };
        
        var access = CheckAccessOn(group, RoleControlAction.Create);
        if (access.IsSome) return access.AsSome.Value;

        try
        {
            await context.Groups.AddAsync(group, ct);

            var member = new GroupMembers
            {
                GroupId = id,
                UserId = UserId,
                AddedByUserId = UserId
            };

            await context.GroupMembers.AddAsync(member, ct);

            var result = await context.SaveChangesAsync(ct);
            if (result.IsSome) return result.AsSome.Value;

            await context.Entry(group)
                .Collection(g => g.Members)
                .Query()
                .Include(m => m.User)
                .LoadAsync(ct);

            return group;
        }
        catch (Exception e)
        {
            return new BadRequestError("Failed to create group", e);
        }
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

        try
        {
            context.Groups.Remove(group);
            return await context.SaveChangesAsync(ct);
        }
        catch (Exception e)
        {
            return new BadRequestError("Failed to create group", e);
        }
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
        
        var existing = await context.GroupMembers.FindAsync([groupId, userId], ct);
        if (existing is not null) return new Option<ServiceError>.None();
        
        var member = new GroupMembers
        {
            GroupId = group.GroupId,
            UserId = userId,
            AddedByUserId = UserId
        };

        try
        {
            await context.GroupMembers.AddAsync(member, ct);
            return await context.SaveChangesAsync(ct);
        }
        catch (Exception e)
        {
            return new BadRequestError("Error while adding a group member", e);
        }
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

        try
        {
            context.GroupMembers.Remove(membership);
            return await context.SaveChangesAsync(ct);
        }
        catch (Exception e)
        {
            return new BadRequestError("Failed to create group", e);
        }
    }
}
