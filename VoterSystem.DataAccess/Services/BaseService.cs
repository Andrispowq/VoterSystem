using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public abstract class BaseService<TType, TService>
    (IHttpContextAccessor httpContextAccessor, ILogger<TService> logger)
    where TType : IRoleControlled
    where TService : BaseService<TType, TService>
{
    protected abstract bool CanAccessAll(bool admin);
    
    protected bool CheckAccessOnAll()
    {
        return CanAccessAll(IsAdmin);
    }

    protected Option<ServiceError> CheckAccessOn(TType resource, RoleControlAction action)
    {
        var isAdmin = IsAdmin;
        var userId = UserId;
       
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

    private ILogger<TService> Logger { get; } = logger;

    protected Guid UserId => GetGuidClaim(TokenIssuerKeys.UserIdKey);
    protected Guid? MaybeUserId => MaybeGetGuidClaim(TokenIssuerKeys.UserIdKey);
    protected Guid Username => GetGuidClaim(TokenIssuerKeys.UsernameKey);
    protected List<string> Roles => GetClaims(ClaimTypes.Role);
    protected bool IsAdmin => IsInRole(Role.Admin);
    protected bool IsUser => IsInRole(Role.User);
    protected bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    protected HostString Host => httpContextAccessor.HttpContext?.Request.Host ?? default;
    
    protected string BrowserFingerprint
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers["X-Browser-Fingerprint"];
            return value?.ToString() ?? "";
        }
    }
    
    private bool IsInRole(Role type) => GetClaims(ClaimTypes.Role)
        .Any(c => c == type.ToString());
    
    private Guid GetGuidClaim(string claimName)
    {
        var claim = GetClaim(claimName);
        return Guid.Parse(claim);
    }
    
    private Guid? MaybeGetGuidClaim(string claimName)
    {
        var claim = MaybeGetClaim(claimName);
        if (claim is null) return null;
        return Guid.Parse(claim);
    }
    
    private string GetClaim(string claimName)
    {
        var claims = GetClaims(claimName);
        if (claims.Count == 0)
        {
            Logger.LogWarning("No claims under {Name} found", claimName);
            return "";
        }
        
        return claims[0];
    }
    
    private string? MaybeGetClaim(string claimName)
    {
        var claims = GetClaims(claimName);
        if (claims.Count == 0)
        {
            Logger.LogWarning("No claims under {Name} found", claimName);
            return null;
        }
        
        return claims[0];
    }

    private List<string> GetClaims(string claimName)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return [];
        }

        var user = context.User;
        var identity = user.Identity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return [];
        }

        var claims = user.Claims;
        return claims.Where(c => c.Type == claimName)
            .Select(c => c.Value).ToList();
    }
}