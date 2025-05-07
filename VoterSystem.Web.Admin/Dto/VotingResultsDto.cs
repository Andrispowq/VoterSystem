namespace VoterSystem.Web.Admin.Dto;

public class VotingResultsDto
{
    public required List<ChoiceResultDto> ChoiceResults { get; init; }
}