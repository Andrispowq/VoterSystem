using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public abstract class BaseService<T>(IUserService userService) where T : IRoleControlled
{
    protected abstract bool CanAccessAll(bool admin);
    
    protected bool CheckAccessOnAll()
    {
        var isAdmin = userService.IsCurrentUserAdmin();
        return CanAccessAll(isAdmin);
    }

    protected async Task<Option<ServiceError>> CheckAccessOn(T resource, RoleControlAction action)
    {
        var isAdmin = userService.IsCurrentUserAdmin();
        var userResult = await userService.GetCurrentUserAsync();
        if (userResult.IsError) return userResult.Error;
        var userId = userResult.Value.Id;
       
        var access = action switch
        {
            RoleControlAction.AccessAll => CanAccessAll(isAdmin),
            RoleControlAction.Access => resource.CanAccessById(isAdmin, userId),
            RoleControlAction.Create => resource.CanCreate(isAdmin, userId),
            RoleControlAction.Update => resource.CanUpdate(isAdmin, userId),
            RoleControlAction.Delete => resource.CanDelete(isAdmin, userId),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        return access
            ? new Option<ServiceError>.None()
            : new UnauthorizedError("Access denied");
    }
}