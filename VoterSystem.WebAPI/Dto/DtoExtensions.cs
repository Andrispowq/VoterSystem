using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;

namespace VoterSystem.WebAPI.Dto;

public static class DtoExtensions
{
    public static TokensDto ToTokensDto(this Tokens tokens)
    {
        return new()
        {
            AuthToken = tokens.AuthToken,
            RefreshToken = tokens.RefreshToken,
            UserId = tokens.UserId,
        };
    }

    public static UserDto ToUserDto(this User user)
    {
        return new()
        {
            Id = user.Id,
            Email = user.Email!,
            EmailConfirmed = user.EmailConfirmed,
            Name = user.Name,
            Votes = GetVotes(user),
            TwoFactorEnabled = user.TwoFactorEnabled,
        };
    }

    public static VoteChoiceDto ToVoteChoiceDto(this VoteChoice voteChoice)
    {
        return new()
        {
            ChoiceId = voteChoice.ChoiceId,
            Name = voteChoice.Name,
            Description = voteChoice.Description,
            CreatedAt = voteChoice.CreatedAt,
        };
    }

    public static VoteDto ToVoteDto(this Vote vote)
    {
        return new()
        {
            CreatedAt = vote.CreatedAt,
            VoteChoice = vote.VoteChoice.ToVoteChoiceDto(),
            Voting = vote.Voting.ToVotingDto()
        };
    }

    public static VotingDto ToVotingDto(this Voting voting)
    {
        return new()
        {
            VotingId = voting.VotingId,
            Name = voting.Name,
            CreatedAt = voting.CreatedAt,
            StartsAt = voting.StartsAt,
            EndsAt = voting.EndsAt,
            HasStarted = voting.HasStarted,
            HasEnded = voting.HasEnded,
            IsOngoing = voting.IsOngoing,
            VoteChoices = voting.VoteChoices
                .Select(v => v.ToVoteChoiceDto())
                .OrderBy(v => v.CreatedAt)
                .ToList()
        };
    }

    public static VotingResultsDto ToVotingResultsDto(this List<Vote> votes)
    {
        return new()
        {
            ChoiceResults = CalculateResults(votes)
        };
    }

    private static List<VoteDto> GetVotes(User user)
    {
        return user.Votes.Select(vote => vote.ToVoteDto()).ToList();
    }

    private static List<ChoiceResultDto> CalculateResults(List<Vote> list)
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