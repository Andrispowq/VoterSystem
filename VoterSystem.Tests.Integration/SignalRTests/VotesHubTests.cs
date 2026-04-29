using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.Shared.Dto;
using VoterSystem.Shared.SignalR.Interfaces;
using VoterSystem.Shared.SignalR.Models;
using VoterSystem.Tests.Integration.Abstractions;

namespace VoterSystem.Tests.Integration.SignalRTests;

[Collection("IntegrationTests")]
public class VotesHubTests(TestWebAppFactory factory) : TestSignalRObjectFactory(factory, "Hubs/VotesHub")
{
    [Fact]
    public async Task AuthorizedUser_CanConnectToHub()
    {
        var token = await Login(AdminLogin);
        await using var hubConnection = CreateHubConnection(token);

        await hubConnection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, hubConnection.State);
    }

    [Fact]
    public async Task NotAuthorizedUser_CannotConnectToHub()
    {
        await using var hubConnection = CreateHubConnection();

        await Assert.ThrowsAsync<HttpRequestException>(() => hubConnection.StartAsync());
    }

    [Fact]
    public async Task SubscribeToVotingAsync_SendsNotification_OnlyToSubscribedConnections()
    {
        var voting = await CreateVotingAsync(UserLogin.Email);
        var notification = CreateNotification(voting.VotingId);
        var subscribedNotifications = new List<VotingUpdatedDto>();
        var unsubscribedNotifications = new List<VotingUpdatedDto>();

        var token = await Login(UserLogin);
        await using var unsubscribedConnection = CreateHubConnection(token);
        await using var subscribedConnection = CreateHubConnection(token);

        await unsubscribedConnection.StartAsync();
        await subscribedConnection.StartAsync();

        unsubscribedConnection.On<VotingUpdatedDto>(nameof(IVoteNotificationService.NotifyVotingResultChanged),
            received => unsubscribedNotifications.Add(received));
        subscribedConnection.On<VotingUpdatedDto>(nameof(IVoteNotificationService.NotifyVotingResultChanged),
            received => subscribedNotifications.Add(received));

        await subscribedConnection.InvokeAsync("SubscribeToVotingAsync", voting.VotingId);
        await NotifyVotingResultChangedAsync(notification);
        await Task.Delay(500);

        Assert.Empty(unsubscribedNotifications);
        Assert.Single(subscribedNotifications);
        Assert.Equal(notification.VotingId, subscribedNotifications[0].VotingId);
        CompareResult(notification.VotingResults, subscribedNotifications[0].VotingResults);
    }

    [Fact]
    public async Task SubscribeToVotingAsync_Throws_WhenUserHasNotVotedOnVoting()
    {
        var voting = await CreateVotingAsync(UserLogin.Email);
        var token = await Login(OtherUserLogin);
        await using var hubConnection = CreateHubConnection(token);

        await hubConnection.StartAsync();

        await Assert.ThrowsAsync<HubException>(() => hubConnection.InvokeAsync("SubscribeToVotingAsync", voting.VotingId));
    }

    [Fact]
    public async Task SubscribeToVotingAsync_Throws_WhenUserHasNoGroupAccess()
    {
        var group = await CreateGroupAsync(UserLogin.Email, UserLogin.Email);
        var voting = await CreateVotingAsync(UserLogin.Email, group.GroupId);
        var token = await Login(OtherUserLogin);
        await using var hubConnection = CreateHubConnection(token);

        await hubConnection.StartAsync();

        await Assert.ThrowsAsync<HubException>(() => hubConnection.InvokeAsync("SubscribeToVotingAsync", voting.VotingId));
    }

    [Fact]
    public async Task SubscribeToVotingAsync_AllowsGroupMemberWhoAlreadyVoted()
    {
        var group = await CreateGroupAsync(UserLogin.Email, UserLogin.Email, OtherUserLogin.Email);
        var voting = await CreateVotingAsync(UserLogin.Email, group.GroupId);
        await AddParticipationAsync(voting.VotingId, OtherUserLogin.Email);

        var notification = CreateNotification(voting.VotingId);
        var receivedNotifications = new List<VotingUpdatedDto>();

        var token = await Login(OtherUserLogin);
        await using var hubConnection = CreateHubConnection(token);

        await hubConnection.StartAsync();
        hubConnection.On<VotingUpdatedDto>(nameof(IVoteNotificationService.NotifyVotingResultChanged),
            received => receivedNotifications.Add(received));

        await hubConnection.InvokeAsync("SubscribeToVotingAsync", voting.VotingId);
        await NotifyVotingResultChangedAsync(notification);
        await Task.Delay(500);

        Assert.Single(receivedNotifications);
        Assert.Equal(notification.VotingId, receivedNotifications[0].VotingId);
        CompareResult(notification.VotingResults, receivedNotifications[0].VotingResults);
    }

    private async Task<Voting> CreateVotingAsync(string ownerEmail, Guid? groupId = null)
    {
        var dbContext = Scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
        var ownerId = await GetUserIdAsync(ownerEmail);

        var voting = new Voting
        {
            Name = $"Voting-{Guid.NewGuid():N}",
            StartsAt = DateTime.UtcNow.AddHours(-1),
            EndsAt = DateTime.UtcNow.AddHours(1),
            CreatedByUserId = ownerId,
            GroupId = groupId
        };

        await dbContext.Votings.AddAsync(voting);
        await dbContext.SaveChangesAsync();

        return voting;
    }

    private async Task<Group> CreateGroupAsync(string ownerEmail, params string[] memberEmails)
    {
        var dbContext = Scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
        var ownerId = await GetUserIdAsync(ownerEmail);
        var memberIds = await dbContext.Users
            .Where(user => memberEmails.Contains(user.Email!))
            .Select(user => user.Id)
            .ToListAsync();

        var group = new Group
        {
            GroupId = Guid.NewGuid(),
            CreatorUserId = ownerId,
            Name = $"Group-{Guid.NewGuid().ToString()[..8]}",
            Description = "SignalR test group"
        };

        await dbContext.Groups.AddAsync(group);

        foreach (var memberId in memberIds.Distinct())
        {
            await dbContext.GroupMembers.AddAsync(new GroupMembers
            {
                GroupId = group.GroupId,
                UserId = memberId,
                AddedByUserId = ownerId
            });
        }

        await dbContext.SaveChangesAsync();

        return group;
    }

    private async Task AddParticipationAsync(long votingId, string userEmail)
    {
        var dbContext = Scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();
        var userId = await GetUserIdAsync(userEmail);

        await dbContext.VotingParticipations.AddAsync(new VotingParticipation
        {
            VotingId = votingId,
            UserId = userId,
            HasVoted = true
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        var dbContext = Scope.ServiceProvider.GetRequiredService<VoterSystemDbContext>();

        return await dbContext.Users
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private async Task NotifyVotingResultChangedAsync(VotingUpdatedDto notification)
    {
        var voteNotificationService = Scope.ServiceProvider.GetRequiredService<IVoteNotificationService>();
        await voteNotificationService.NotifyVotingResultChanged(notification);
    }

    private static VotingUpdatedDto CreateNotification(long votingId)
    {
        return new VotingUpdatedDto
        {
            VotingId = votingId,
            VotingResults = new VotingResultsDto
            {
                ChoiceResults =
                [
                    new() { ChoiceId = 1, VoteCount = 4 },
                    new() { ChoiceId = 2, VoteCount = 3 },
                    new() { ChoiceId = 3, VoteCount = 3 }
                ]
            }
        };
    }

    private static void CompareResult(VotingResultsDto expected, VotingResultsDto actual)
    {
        Assert.Equal(expected.ChoiceResults.Count, actual.ChoiceResults.Count);

        for (var i = 0; i < expected.ChoiceResults.Count; i++)
        {
            Assert.Equal(expected.ChoiceResults[i].ChoiceId, actual.ChoiceResults[i].ChoiceId);
            Assert.Equal(expected.ChoiceResults[i].VoteCount, actual.ChoiceResults[i].VoteCount);
        }
    }
}
