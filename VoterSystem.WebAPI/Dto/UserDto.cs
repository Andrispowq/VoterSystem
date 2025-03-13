using VoterSystem.DataAccess.Model;

namespace VoterSystem.WebAPI.Dto;

public class UserDto(User user)
{
    public string Name => user.Name;
    public string Email => user.Email;
    public UserLevel UserLevel => user.UserLevel;
}