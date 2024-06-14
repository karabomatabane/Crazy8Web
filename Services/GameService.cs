﻿using Crazy8.Contracts;
using Crazy8.Models;
using Crazy8Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Crazy8Web.Services;

public class GameService
{
    private readonly IHubContext<GameHub> _hubContext;
    private Game _game;

    public GameService(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
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

    public void CreateGame(Player owner)
    {
        Dictionary<string, IEffect?> specialCards = new()
        {
            { "7", new JumpEffect() }, { "8", new CallEffect() }, { "Jack", new ReverseEffect() }
        };
        
        _game = new Game(owner, specialCards);
        
        // Subscribe to game events
        _game.FaceUpCardChanged += OnFaceUpCardChanged;
        _game.PlayerTurnChanged += OnPlayerTurnChanged;
    }
    
    public void StartGame()
    {
        _game.StartGame();
    }

    public void JoinGame(Player player, string gameId)
    {
        // TODO: add player to player's list
    }

    public void ProgressGame(Card? playerChoice)
    {
        _game.ProgressGame(playerChoice);
    }
}