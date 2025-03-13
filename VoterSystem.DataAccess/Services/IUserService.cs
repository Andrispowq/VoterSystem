using VoterSystem.DataAccess.Dto;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Services;

public interface IUserService
{
    Task<IReadOnlyCollection<User>> GetUsersAsync();
    Task<Result<User, ServiceError>> GetUserAsync(long id);
    Task<Result<User, ServiceError>> CreateUserAsync(CreateUserDto userDto);
    
    bool TryAccess(User user, string password);
}