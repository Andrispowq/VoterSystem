using Microsoft.AspNetCore.SignalR.Client;
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
}