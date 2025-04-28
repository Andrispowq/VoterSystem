using VoterSystem.DataAccess.Model;

namespace VoterSystem.Shared.Dto;

public class VoteChoiceDto(VoteChoice choice)
{
    public long ChoiceId => choice.ChoiceId;
    public string Name => choice.Name;
    public string? Description => choice.Description;
    public DateTime CreatedAt => choice.CreatedAt;
}