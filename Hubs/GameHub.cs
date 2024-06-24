using Crazy8.Models;
using Crazy8Web.Constants;
using Crazy8Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace Crazy8Web.Hubs;

public class GameHub : Hub
{
    
    private readonly GameService _gameService;

    public GameHub(GameService gameService)
    {
        _gameService = gameService;
    }

    public async Task ShowFaceUp(Card card)
    {
        await Clients.All.SendAsync(Const.FaceUp, card);
    }

    public async Task ShowTurn(string playerId)
    {
        await Clients.All.SendAsync(Const.PlayerTurn, playerId);
    }
    public async Task JoinGame(Player player, string gameId)
    {
        _gameService.JoinGame(player, gameId);
        await Clients.All.SendAsync(Const.JoinedKey, player);
    }

    public async Task PlayerReady(string playerId)
    {
        await Clients.All.SendAsync(Const.PlayerReady, playerId);
    }

    public async Task StartSession()
    {
        await Clients.All.SendAsync(Const.StartSession);
    }
}