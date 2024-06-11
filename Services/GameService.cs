using Crazy8.Contracts;
using Crazy8.Models;
using Crazy8Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Crazy8Web.Services;

public class GameService
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly Game _game;

    public GameService(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
        Player[] players = new[]
        {
            new Player("Tshiamo"), new Player("Karabo"), new Player("Oratile"), 
            new Player("Fentse"),
            new Player("Thabi")
        };

        Dictionary<string, IEffect?> specialCards = new()
        {
            { "7", new JumpEffect() }, { "8", new CallEffect() }, { "Jack", new ReverseEffect() }
        };
        
        _game = new Game(players, specialCards);

        // Subscribe to game events
        _game.FaceUpCardChanged += OnFaceUpCardChanged;
        _game.PlayerTurnChanged += OnPlayerTurnChanged;
    }
    
    private async void OnFaceUpCardChanged(Card card)
    {
        // Notify clients about the face-up card
        await _hubContext.Clients.All.SendAsync("FaceUp", card);
    }

    private async void OnPlayerTurnChanged(string playerId)
    {
        // Notify clients about the player's turn
        await _hubContext.Clients.All.SendAsync("PlayerTurn", playerId);
    }
    
    public void StartGame()
    {
        _game.StartGame();
    }

    public void ProgressGame(Card? playerChoice)
    {
        _game.ProgressGame(playerChoice);
    }
}