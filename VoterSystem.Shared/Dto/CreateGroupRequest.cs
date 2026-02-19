using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public sealed class CreateGroupRequest
{
    [MaxLength(32)]
    public required string Name { get; init; }
    [MaxLength(255)]
    public required string Description { get; init; }
}