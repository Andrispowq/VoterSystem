namespace VoterSystem.DataAccess.Model;

/// <summary>
/// Specifies access control for resources based on whether a certain user may or may not access it for
/// GET, CREATE, PUT/PATCH and DELETE requests on the resource
/// </summary>
public interface IRoleControlled
{
    bool CanAccessById(bool isAdmin, Guid userId);
    bool CanCreate(bool isAdmin, Guid userId);
    bool CanUpdate(bool isAdmin, Guid userId);
    bool CanDelete(bool isAdmin, Guid userId);
}