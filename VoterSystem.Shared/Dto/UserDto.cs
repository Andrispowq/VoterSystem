using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class UserDto(User user)
{
    public string Name => user.Name;
    public string Email => user.Email!;
    public required Role Role { get; set; }
    
    public ICollection<VoteDto> Votes => user.Votes.Select(vote => new VoteDto(vote)).ToList();
}