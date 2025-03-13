using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Dto;
using VoterSystem.DataAccess.Functional;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Util;

namespace VoterSystem.DataAccess.Services;

public class UserService(VoterSystemDbContext context) : IUserService
{
    public async Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        return await context.Users.ToListAsync();
    }

    public async Task<Result<User, ServiceError>> GetUserAsync(long id)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return new NotFoundError($"User (id: {id}) not found");
        return user;
    }

    public async Task<Result<User, ServiceError>> CreateUserAsync(CreateUserDto userDto)
    {
        var passwordGood = Utils.IsPasswordAdequate(userDto.Password);
        if (passwordGood != Utils.PasswordErrors.Good)
        {
            return new BadRequestError($"Password is not valid, error: {passwordGood.ToString()}");
        }
            
        string hash = Crypto.HashPassword(userDto.Password);

        var user = new User
        {
            Name = userDto.Name,
            Email = userDto.Email,
            PasswordHash = hash,
            UserLevel = userDto.UserLevel,
        };

        try
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
            return new ConflictError($"User ({userDto}) already exists");
        }
    }

    public bool TryAccess(User user, string password)
    {
        return Crypto.VerifyPassword(password, user.PasswordHash);
    }
}