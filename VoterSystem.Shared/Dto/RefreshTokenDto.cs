namespace VoterSystem.Shared.Dto;

public sealed class RefreshTokenDto
{
    public required Guid RefreshToken { get; init; }
}