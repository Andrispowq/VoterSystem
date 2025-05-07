namespace VoterSystem.Shared.Dto;

public class VotingResultsDto
{
    public required List<ChoiceResultDto> ChoiceResults { get; init; }
}