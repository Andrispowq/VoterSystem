using Microsoft.AspNetCore.SignalR;
using VoterSystem.Shared.SignalR.Interfaces;

namespace VoterSystem.SignalR.Hubs;

public class VotesHub : Hub<IVoteHub>;