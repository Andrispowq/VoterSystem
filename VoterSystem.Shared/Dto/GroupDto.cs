using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public sealed class GroupDto
{
    public required Guid GroupId { get; init; }
    public required Guid CreatorUserId { get; init; }
    [MaxLength(32)]
    public required string Name { get; set; }
    [MaxLength(255)]
    public required string Description { get; set; }
    public required DateTime? DeletedAt { get; set; }
    public required DateTime CreatedAt { get; init; }
}