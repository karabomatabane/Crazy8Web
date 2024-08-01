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
}