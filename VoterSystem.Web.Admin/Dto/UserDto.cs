namespace VoterSystem.Web.Admin.Dto;

public class UserDto
{
    public required string Name { get; init; }
    public required string Email{ get; init; }
    //public required Role Role { get; init; }
    
    public required ICollection<VoteDto> Votes { get; init; }
}