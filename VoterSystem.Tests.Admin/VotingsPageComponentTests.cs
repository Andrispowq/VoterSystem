using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Blazor.Services;
using VoterSystem.Shared.Blazor.Services.SignalR;
using VoterSystem.Shared.Blazor.ViewModels;
using VoterSystem.Shared.Dto;
using VoterSystem.Web.Admin.Pages;

namespace VoterSystem.Tests.Admin;

public sealed class VotingsPageTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly Mock<IVotingsService> _votingSvc = new();
    private readonly Mock<IAuthenticationService> _userSvc = new();
    private readonly Mock<IVoteHubService> _voteHubService = new();
    private readonly FakeNavigationManager _nav;

    public VotingsPageTests()
    {
        _userSvc
            .Setup(x => x.GetCurrentlyLoggedInUserAsync())
            .ReturnsAsync("user");
        _userSvc
            .Setup(x => x.GetCurrentRoleAsync())
            .ReturnsAsync(Role.User);
        
        _ctx.Services.AddSingleton(_userSvc.Object);
        _ctx.Services.AddSingleton(_voteHubService.Object);

        _ctx.Services.AddSingleton(_votingSvc.Object);
        _nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void ShowsEmptyState_WhenNoVotings()
    {
        _votingSvc.Setup(s => s.GetVotingsAsync())
            .ReturnsAsync(new VotingsViewModel
            {
                Votings = new List<VotingDto>()
            })
            .Verifiable();

        var cut = _ctx.RenderComponent<Votings>();

        _votingSvc.Verify();
        Assert.Contains("No votings found", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListsAllVotings_ReturnedByService()
    {
        var list = new List<VotingDto>
        {
            new()
            {
                VotingId = 1,
                Name = "First",
                StartsAt = DateTime.UtcNow.AddHours(1),
                EndsAt = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow,
                HasStarted = false,
                HasEnded = false,
                IsOngoing = false,
                VoteChoices = []
            },
            new() 
            { 
                VotingId = 2, 
                Name = "Second", 
                StartsAt = DateTime.UtcNow.AddHours(1), 
                EndsAt = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow,
                HasStarted = false,
                HasEnded = false,
                IsOngoing = false,
                VoteChoices = [] 
            },
        };

        _votingSvc.Setup(s => s.GetVotingsAsync()).ReturnsAsync(new VotingsViewModel
        {
            Votings = list
        });

        // Act
        var cut = _ctx.RenderComponent<Votings>();

        // Assert – wait until cards show up
        cut.WaitForAssertion(() =>
        {
            var cards = cut.FindAll("div.card");
            Assert.Equal(list.Count, cards.Count);
        });
    }

    [Fact]
    public void ClickingRow_NavigatesToDetails()
    {
        var voting = new VotingDto
        {
            VotingId = 1,
            Name = "First",
            StartsAt = DateTime.UtcNow.AddHours(1),
            EndsAt = DateTime.UtcNow.AddDays(2),
            CreatedAt = DateTime.UtcNow,
            HasStarted = false,
            HasEnded = false,
            IsOngoing = false,
            VoteChoices = []
        };
        
        _votingSvc.Setup(s => s.GetVotingsAsync()).ReturnsAsync(new VotingsViewModel
        {
            Votings = [ voting ]
        });

        var cut = _ctx.RenderComponent<Votings>();

        // Wait for card then click
        cut.WaitForAssertion(() =>
        {
            var cards = cut.FindAll("div.card");
            cards.First().Click();
        });

        // Verify navigation
        cut.WaitForAssertion(() =>
            Assert.Equal($"http://localhost/votings/{voting.VotingId}", _nav.Uri));
    }
}
