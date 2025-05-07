using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class UserDto(User user)
{
    public Guid Id => user.Id;
    public string Name => user.Name;
    public string Email => user.Email!;
    public bool EmailConfirmed => user.EmailConfirmed;
    public bool TwoFactorEnabled => user.TwoFactorEnabled;
    public required Role Role { get; set; }
    
    public ICollection<VoteDto> Votes => GetVotes();

    private List<VoteDto> GetVotes()
    {
        return user.Votes.Select(vote => new VoteDto(vote)).ToList();
    }
}