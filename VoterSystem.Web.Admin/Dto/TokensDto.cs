namespace VoterSystem.Web.Admin.Dto;

public class TokensDto
{
    public required string AuthToken { get; init; }
    public required Guid RefreshToken { get; init; }
    public required Guid UserId { get; init; }
}