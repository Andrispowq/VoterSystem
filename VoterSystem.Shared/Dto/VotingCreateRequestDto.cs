using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public class VotingCreateRequestDto
{
    [MaxLength(255), MinLength(3)]
    public required string Name { get; set; }
    public required DateTime StartsAt { get; set; }
    public required DateTime EndsAt { get; set; }
}