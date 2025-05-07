namespace VoterSystem.Web.Admin.Dto;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public required Role Role { get; init; }
    
    public required ICollection<VoteDto> Votes { get; init; }
}