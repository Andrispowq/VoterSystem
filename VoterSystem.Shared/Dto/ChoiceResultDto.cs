using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public class ChoiceResultDto
{
    public required long ChoiceId { get; init; }
    [Range(0, long.MaxValue)] public required long VoteCount { get; init; }
}