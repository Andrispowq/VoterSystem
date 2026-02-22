namespace VoterSystem.Shared.Dto;

public sealed class GroupMemberDto
{
    public required Guid UserId { get; init; }
    public required Guid AddedByUserId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
}
