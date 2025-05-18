using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Shared.Dto;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public class CreateVotingComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();
    private readonly Mock<IVotingsService> _votingsService = new();
    private readonly FakeNavigationManager _fakeNavigationManager;

    public CreateVotingComponentTests()
    {
        // Setup mocks and register services
        _authenticationServiceMock
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("admin");
        
        _context.Services.AddSingleton(_authenticationServiceMock.Object);

        _votingsService.Setup(x => x.CreateVotingAsync(It.IsAny<VotingCreateRequestDto>())).ReturnsAsync(
            (VotingCreateRequestDto requestDto) => new VotingDto
            {
                VotingId = 1,
                Name = requestDto.Name,
                CreatedAt = DateTime.UtcNow,
                StartsAt = requestDto.StartsAt,
                EndsAt = requestDto.EndsAt,
                HasStarted = false,
                HasEnded = false,
                IsOngoing = false,
                HasVoted = null,
                VoteChoices = []
            });
        _context.Services.AddSingleton(_votingsService.Object);
        
        _fakeNavigationManager = _context.Services.GetRequiredService<FakeNavigationManager>();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void CreateVoting_RendersFormElementsCorrectly()
    {
        // Act
        var cut = _context.RenderComponent<CreateVoting>();

        // Assert
        var inputElements = cut.FindAll("input");
        Assert.NotEmpty(inputElements);

        var nameInput = cut.Find("#name");
        Assert.NotNull(nameInput);

        var submitButton = cut.Find("button[type='submit']");
        Assert.NotNull(submitButton);
    }

    [Fact]
    public void CreateVoting_WhenRendered_ShouldContainTitle()
    {
        var cut = _context.RenderComponent<CreateVoting>();
        Assert.Contains("Create Voting", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}