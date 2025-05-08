using Microsoft.AspNetCore.Identity;

namespace VoterSystem.DataAccess.Model;

public class UserRole : IdentityRole<Guid>
{
    public UserRole() {}
    public UserRole(string role) : base(role) {}
}