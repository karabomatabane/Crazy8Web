using Crazy8.Contracts;
using Crazy8.Models;
using Crazy8Web.Constants;
using Crazy8Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Crazy8Web.Services;

public class GameService
{
    private readonly IHubContext<GameHub> _hubContext;
    private Game _game;
    private List<string> readyPlayers;
    private TaskCompletionSource<string>? _suitSelectionCompletionSource;

    public GameService(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
        readyPlayers = new List<string>();
    }

    private async void OnFaceUpCardChanged(Card card)
    {
        // Notify clients about the face-up card
        await _hubContext.Clients.All.SendAsync(Const.FaceUp, card);
    }

    private async void OnPlayerTurnChanged(string playerId)
    {
        // Notify clients about the player's turn
        await _hubContext.Clients.All.SendAsync(Const.PlayerTurn, playerId);
    }

    public void CreateGame(Player owner)
    {
        Dictionary<string, IEffect?> specialCards = new()
        {
            { "7", new JumpEffect() }, { "8", new CallEffect() }, { "Jack", new ReverseEffect() },
            { "2", new AttackEffect() { Magnitude = 1, Immune = false } },
            { "Joker", new AttackEffect() { Magnitude = 2, Immune = true } }
        };

        _game = new Game(owner, specialCards);

        // Subscribe to game events
        _game.FaceUpCardChanged += OnFaceUpCardChanged;
        _game.PlayerTurnChanged += OnPlayerTurnChanged;
        CallEffect.SuitPrompted += OnSuitPrompted;
    }

    private Task<string> OnSuitPrompted(string defaultSuit)
    {
        _suitSelectionCompletionSource = new TaskCompletionSource<string>();
        _hubContext.Clients.All.SendAsync(Const.PromptSuit, defaultSuit);
        // wait for user to select suit
        return _suitSelectionCompletionSource.Task;
    }

    public async void ReceiveSuitSelection(string selectedSuit)
    {
        if (_suitSelectionCompletionSource == null) return;
        _suitSelectionCompletionSource.SetResult(selectedSuit);
        _suitSelectionCompletionSource = null;
    }

    public bool IsGameRunning() => _game.IsRunning;

    public string GetGameId() => _game.GameId;

    public void StartGame()
    {
        _game.StartGame();
    }

    public void JoinGame(Player player, string gameId)
    {
        if (_game == null || gameId != _game.GameId)
        {
            throw new InvalidOperationException("Game does not exist or has not been created yet.");
        }

        _game.AddPlayer(player);
        _hubContext.Clients.All.SendAsync(Const.JoinedKey, player);
    }

    public Player[] GetPlayers()
    {
        return _game.GetPlayers();
    }

    public List<Player> GetOtherPlayers(Player player)
    {
        List<Player> players = _game.GetPlayers().Where(p => p.PlayerId != player.PlayerId).ToList();
        return players;
    }

    public List<string> GetReadyPlayers() => readyPlayers;

    public void PlayerReady(string playerId)
    {
        readyPlayers.Add(playerId);
        _hubContext.Clients.All.SendAsync(Const.PlayerReady, playerId);
    }

    public void StartSession()
    {
        _hubContext.Clients.All.SendAsync(Const.StartSession);
    }

    public Card[] GetPlayerCards(string playerId)
    {
        Player[] players = _game.GetPlayers();
        Player? player = players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player != null)
        {
            return player.Hand ?? [];
        }

        return [];
    }

    public string GetOwnerId() => _game.Owner;

    public bool IsMine(string playerId) => _game.Owner == playerId;

    public void ProgressGame(Card? playerChoice)
    {
        _game.ProgressGame(playerChoice);
    }

    public Card? GetFaceUp() => _game.GetFaceUp();
    public string? GetRequiredSuit() => _game.RequiredSuit;
    public int GetTurn() => _game.Turn;
}