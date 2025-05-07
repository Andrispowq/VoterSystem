using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class VotingResultsDto(List<Vote> votes)
{
    public List<ChoiceResultDto> ChoiceResults => CalculateResults(votes);

    private List<ChoiceResultDto> CalculateResults(List<Vote> list)
    {
        return list
            .GroupBy(v => v.ChoiceId)
            .Select(group => new ChoiceResultDto
            {
                ChoiceId = group.Key,
                VoteCount = group.Count()
            }).ToList();
    }
}