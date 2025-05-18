using Microsoft.AspNetCore.SignalR.Client;
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
        //Arrange
        var token = await Login(AdminLogin);
        var hubConnection = CreateHubConnection(token);
        
        //Act
        await hubConnection.StartAsync();

        //Verify
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);
    }
    
    [Fact]
    public async Task NotAuthorizedUser_CannotConnectToHub()
    {
        //Arrange
        var hubConnection = CreateHubConnection();
        
        //Act & Verify
        await Assert.ThrowsAsync<HttpRequestException>(() => hubConnection.StartAsync());
    }
    
    [Fact]
    public async Task VoteCast_SendsNotification_LoggedInUser()
    {
        // Arrange
        const int votingId = 1;
        var notification = new VotingUpdatedDto
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

        var receivedNotifications = new List<VotingUpdatedDto>();
        
        var token = await Login(UserLogin);
        var hubConnection1 = CreateHubConnection(token);
        await hubConnection1.StartAsync();
        
        var hubConnection2 = CreateHubConnection(token);
        await hubConnection2.StartAsync();
        
        // Listen for SeatStatusChanged messages
        hubConnection2.On<VotingUpdatedDto>(nameof(IVoteNotificationService.NotifyVotingResultChanged), received =>
        {
            Assert.Equal(notification.VotingId, received.VotingId);
            receivedNotifications.Add(received);
        });
        
        //Act
        await hubConnection1.InvokeAsync(nameof(IVoteNotificationService.NotifyVotingResultChanged), notification);
        
        // Allow time for message propagation
        await Task.Delay(500);

        // Assert
        Assert.Single(receivedNotifications);
        Assert.Equal(notification.VotingId, receivedNotifications[0].VotingId);
        CompareResult(notification.VotingResults, receivedNotifications[0].VotingResults);
    }
    
    [Fact]
    public async Task VoteCast_SendsNotification_LoggedInAdmin()
    {
        // Arrange
        const int votingId = 1;
        var notification = new VotingUpdatedDto
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

        var receivedNotifications = new List<VotingUpdatedDto>();
        
        var token = await Login(AdminLogin);
        var hubConnection1 = CreateHubConnection(token);
        await hubConnection1.StartAsync();
        
        var hubConnection2 = CreateHubConnection(token);
        await hubConnection2.StartAsync();
        
        // Listen for SeatStatusChanged messages
        hubConnection2.On<VotingUpdatedDto>(nameof(IVoteNotificationService.NotifyVotingResultChanged), received =>
        {
            Assert.Equal(notification.VotingId, received.VotingId);
            receivedNotifications.Add(received);
        });
        
        //Act
        await hubConnection1.InvokeAsync(nameof(IVoteNotificationService.NotifyVotingResultChanged), notification);
        
        // Allow time for message propagation
        await Task.Delay(500);

        // Assert
        Assert.Single(receivedNotifications);
        Assert.Equal(notification.VotingId, receivedNotifications[0].VotingId);
        CompareResult(notification.VotingResults, receivedNotifications[0].VotingResults);
    }
    
    [Fact]
    public async Task VoteCast_SendsNotification_NotLoggedIn()
    {
        // Arrange
        const int votingId = 1;
        var notification = new VotingUpdatedDto
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

        var receivedNotifications = new List<VotingUpdatedDto>();
        
        var token = await Login(UserLogin);
        var hubConnection1 = CreateHubConnection(token);
        await hubConnection1.StartAsync();
        
        var hubConnection2 = CreateHubConnection();

        try
        {
            await hubConnection2.StartAsync();

            // Listen for SeatStatusChanged messages
            hubConnection2.On<VotingUpdatedDto>(nameof(IVoteNotificationService.NotifyVotingResultChanged), received =>
            {
                Assert.Equal(notification.VotingId, received.VotingId);
                receivedNotifications.Add(received);
            });

            //Act
            await hubConnection1.InvokeAsync(nameof(IVoteNotificationService.NotifyVotingResultChanged), notification);
            
            Assert.Fail("Should not get here"); 
        }
        catch (HttpRequestException) {}

        // Allow time for message propagation
        await Task.Delay(500);

        // Assert
        Assert.Empty(receivedNotifications);
    }

    private void CompareResult(VotingResultsDto expected, VotingResultsDto actual)
    {
        Assert.Equal(expected.ChoiceResults.Count, actual.ChoiceResults.Count);

        for (var i = 0; i < expected.ChoiceResults.Count; i++)
        {
            Assert.Equal(expected.ChoiceResults[i].ChoiceId, actual.ChoiceResults[i].ChoiceId);
            Assert.Equal(expected.ChoiceResults[i].VoteCount, actual.ChoiceResults[i].VoteCount);
        }
    }
}