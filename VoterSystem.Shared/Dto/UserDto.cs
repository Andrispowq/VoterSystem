using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required bool EmailConfirmed { get; init; }
    public required bool TwoFactorEnabled { get; init; }
    public Role Role { get; set; }
    
    public required ICollection<VoteDto> Votes { get; init; }
}