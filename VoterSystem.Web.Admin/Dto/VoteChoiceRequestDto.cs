namespace VoterSystem.Web.Admin.Dto;

public class VoteChoiceRequestDto
{
    public required string Name { get; init; }
    public required string? Description { get; init; }
}