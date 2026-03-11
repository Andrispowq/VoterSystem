using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;

namespace VoterSystem.WebAPI.Dto;

public static class DtoExtensions
{
    public static TokensDto ToTokensDto(this TokensDto tokensDto)
    {
        return new TokensDto
        {
            AuthToken = tokensDto.AuthToken,
            RefreshToken = tokensDto.RefreshToken,
            UserId = tokensDto.UserId,
        };
    }

    public static UserDto ToUserDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            EmailConfirmed = user.EmailConfirmed,
            Name = user.Name,
            Participations = GetVotes(user),
            TwoFactorEnabled = user.TwoFactorEnabled,
        };
    }

    public static VoteChoiceDto ToVoteChoiceDto(this VoteChoice voteChoice)
    {
        return new VoteChoiceDto
        {
            ChoiceId = voteChoice.ChoiceId,
            Name = voteChoice.Name,
            Description = voteChoice.Description,
            CreatedAt = voteChoice.CreatedAt,
        };
    }

    public static BallotDto ToBallotDto(this AnonymousBallot vote)
    {
        return new BallotDto
        {
            CreatedAt = vote.CreatedAt,
            VoteChoice = vote.VoteChoice.ToVoteChoiceDto(),
            Voting = vote.Voting.ToVotingDto()
        };
    }

    public static VotingParticipationDto ToVotingParticipationDto(this VotingParticipation vote)
    {
        return new VotingParticipationDto
        {
            Voting = vote.Voting.ToVotingDto()
        };
    }

    public static VotingDto ToVotingDto(this Voting voting)
    {
        return new VotingDto
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

    public static VotingResultsDto ToVotingResultsDto(this List<AnonymousBallot> votes)
    {
        return new()
        {
            ChoiceResults = CalculateResults(votes)
        };
    }

    private static List<VotingParticipationDto> GetVotes(User user)
    {
        return user.VotingParticipations.Select(
            vote => vote.ToVotingParticipationDto()).ToList();
    }

    private static List<ChoiceResultDto> CalculateResults(List<AnonymousBallot> list)
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