using Crazy8.Models;
using Crazy8Web.Constants;
using Crazy8Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace Crazy8Web.Hubs;

public class GameHub(GameService gameService) : Hub
{
    private readonly GameService _gameService = gameService;

    public Task JoinGameGroup(string gameId) => Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    public Task LeaveGameGroup(string gameId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
}