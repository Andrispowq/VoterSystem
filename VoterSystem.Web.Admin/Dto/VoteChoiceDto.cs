namespace VoterSystem.Web.Admin.Dto;

public class VoteChoiceDto
{
    public required long ChoiceId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required DateTime CreatedAt { get; init; }
}