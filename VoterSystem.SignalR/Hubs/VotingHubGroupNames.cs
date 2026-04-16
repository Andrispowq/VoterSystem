namespace VoterSystem.SignalR.Hubs;

public static class VotingHubGroupNames
{
    public static string ForVoting(long votingId) => $"voting:{votingId}";
}
