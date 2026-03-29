using Crazy8.Contracts;
using Crazy8.Models;
using Crazy8Web.Constants;
using Crazy8Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using static System.Collections.Specialized.BitVector32;
using Game = Crazy8.Models.Game;

namespace Crazy8Web.Services;

public class GameService(IHubContext<GameHub> hubContext)
{
    private static int _suitPromptSubscribed;
    private readonly IHubContext<GameHub> _hubContext = hubContext;
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    private async void OnFaceUpCardChanged(GameEvent<Card> gameEvent)
    {
        // Notify clients about the face-up card
        await _hubContext.Clients.Group(gameEvent.GameId).SendAsync(Const.FaceUp, gameEvent.Value);
    }

    private async void OnPlayerTurnChanged(GameEvent<string> gameEvent)
    {
        // Notify clients about the player's turn
        await _hubContext.Clients.Group(gameEvent.GameId).SendAsync(Const.PlayerTurn, gameEvent.Value);
    }

    /// <summary>
    /// Creates a new game session with the specified owner and optional game identifier.
    /// </summary>
    /// <param name="owner">The player who will be set as the owner of the new game session. Cannot be null.</param>
    /// <param name="gameId">The unique identifier for the game session, or null to generate a new identifier.</param>
    /// <returns>A new instance of GameSession initialized with the specified owner and game identifier.</returns>
    private static GameSession CreateSession(Player owner, string? gameId = null, IEnumerable<Player>? additionalPlayers = null)
    {
        Game game = new(owner, BuildSpecialCards(), gameId);
        if (additionalPlayers != null)
        {
            foreach (Player player in additionalPlayers)
            {
                game.AddPlayer(player);
            }
        }
        return new GameSession { Game = game };
    }

    private GameSession GetSession(string gameId) =>
    _sessions.TryGetValue(gameId, out GameSession? session)
        ? session
        : throw new InvalidOperationException($"Game '{gameId}' not found.");


    public string CreateGame(Player owner)
    {
        GameSession session = CreateSession(owner);
        WireEvents(session);
        _sessions[session.Game.GameId] = session;

        if (Interlocked.Exchange(ref _suitPromptSubscribed, 1) == 0)
        {
            CallEffect.SuitPrompted += OnSuitPrompted;
        }

        return session.Game.GameId;
    }

    private static Dictionary<string, IEffect?> BuildSpecialCards()
    {
        return new()
        {
            { "7", new JumpEffect() }, { "8", new CallEffect() }, { "Jack", new ReverseEffect() },
            { "2", new AttackEffect() { Magnitude = 1, Immune = false } },
            { "Joker", new AttackEffect() { Magnitude = 2, Immune = true } }
        };
    }

    private void WireEvents(GameSession session)
    {
        string gameId = session.Game.GameId;

        session.Game.FaceUpCardChanged += OnFaceUpCardChanged;

        session.Game.PlayerTurnChanged += OnPlayerTurnChanged;

        session.Game.GameHasEnded += GameOnGameHasEnded;
    }

    private void GameOnGameHasEnded(string gameId, List<Player> results)
    {
        _hubContext.Clients.Group(gameId).SendAsync(Const.EndGame, results);
    }

    private void DeckOnVibeCheckEvent(object? sender, Deck.VibeCheckEventArgs e)
    {
        Console.WriteLine("Something went wrong!");
    }

    private Task<string> OnSuitPrompted(GameEvent<string> gameEvent)
    {
        GameSession session = GetSession(gameEvent.GameId);
        session.SuitSelectionTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hubContext.Clients.Group(gameEvent.GameId).SendAsync(Const.PromptSuit, gameEvent.Value);
        // wait for user to select suit
        return session.SuitSelectionTcs.Task;
    }

    public void ReceiveSuitSelection(string gameId, string selectedSuit)
    {
        GameSession session = GetSession(gameId);
        if (session.SuitSelectionTcs is null) return;
        session.SuitSelectionTcs.TrySetResult(selectedSuit);
        session.SuitSelectionTcs = null;
    }

    public bool IsGameRunning(string gameId) => _sessions[gameId].Game.IsRunning;

    //public string GetGameId() => _game.GameId;

    public void StartGame(string gameId)
    {
        _sessions[gameId].Game.StartGame();
    }

    public void Rematch(string gameId)
    {
        _hubContext.Clients.Group(gameId).SendAsync(Const.RematchClicked);
    }

    public void RestartGame(string gameId, List<Player> players)
    {
        if (players is null || players.Count == 0)
            throw new InvalidOperationException("No players for rematch.");

        Player owner = players[0];
        IEnumerable<Player> additionalPlayers = players.Skip(1);

        GameSession replacement = CreateSession(owner, gameId, additionalPlayers);
        WireEvents(replacement);

        // preserve ready list, replace only game
        GameSession current = GetSession(gameId);
        current.Game = replacement.Game;
        current.ReadyPlayers.Clear();

        _ = _hubContext.Clients.Group(gameId).SendAsync(Const.RematchStarted);
    }

    public void JoinGame(Player player, string gameId)
    {
        if (!_sessions.TryGetValue(gameId, out GameSession? session))
        {
            // TODO: use a toast to notify user
            Console.WriteLine("Game does not exist. Please confirm you have a valid game ID and try again ;D");
            throw new InvalidOperationException("Game does not exist or has not been created yet.");
        }

        session.Game.AddPlayer(player);
        _hubContext.Clients.Group(gameId).SendAsync(Const.JoinedKey, player);
    }

    public Player[] GetPlayers(string gameId)
    {
        if(_sessions.TryGetValue(gameId, out GameSession? session))
        {
            return session.Game.GetPlayers();
        }
        throw new InvalidOperationException("The game ID you entered has no associated game.");
    }

    public List<Player> GetOtherPlayers(string gameId, Player player)
    {
        if (_sessions.TryGetValue(gameId, out GameSession? session))
        {
            List<Player> players = session.Game.GetPlayers().Where(p => p.PlayerId != player.PlayerId).ToList();
            return players;
        }
        throw new InvalidOperationException("The game ID you entered has no associated game.");
    }

    public List<string> GetReadyPlayers(string gameId) => _sessions[gameId].ReadyPlayers;

    public void PlayerReady(string gameId, string playerId)
    {
        GameSession session = GetSession(gameId);
        if (!session.ReadyPlayers.Contains(playerId))
        {
            session.ReadyPlayers.Add(playerId);
        }
        _hubContext.Clients.Group(gameId).SendAsync(Const.PlayerReady, playerId);
    }

    public void StartSession(string gameId)
    {
        GameSession session = GetSession(gameId);
        _hubContext.Clients.Group(gameId).SendAsync(Const.StartSession);
        session.Game.Deck.VibeCheckEvent += DeckOnVibeCheckEvent;
    }

    public Card[] GetPlayerCards(string gameId, string playerId)
    {
        GameSession session = GetSession(gameId);
        Player[] players = session.Game.GetPlayers();
        Player? player = players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player != null)
        {
            return player.Hand ?? [];
        }

        return [];
    }

    public string GetOwnerId(string gameId) => _sessions[gameId].Game.Owner;

    public bool IsMine(string gameId, string playerId) => _sessions[gameId].Game.Owner == playerId;

    public async Task ProgressGame(string gameId, Card? playerChoice)
    {
        GameSession session = GetSession(gameId);
        var game = session.Game;
        await game.ProgressGame(playerChoice);
        bool isFine = game.Deck.VibeCheck(game.Players, "progress game");
        if (!isFine)
        {
            Console.WriteLine("Something went wrong playing card");
        }
    }

    public void PenalisePlayer(string gameId, string playerId)
    {
        GameSession session = GetSession(gameId);
        session.Game.PenalisePlayer(playerId);
    }

    public void CallOut(string gameId, string playerName, int count)
    {
        _hubContext.Clients.Group(gameId).SendAsync(Const.CallOut, playerName, count);
    }

    public Card? GetFaceUp(string gameId) => _sessions[gameId].Game.GetFaceUp();
    public int GetAttacks(string gameId) => _sessions[gameId].Game.Attacks;
    public string? GetRequiredSuit(string gameId) => _sessions[gameId].Game.RequiredSuit;
    public int GetTurn(string gameId) => _sessions[gameId].Game.Turn;
}

internal class GameSession
{
    public Game Game { get; set; } = null!;
    public List<string> ReadyPlayers { get; set; } = [];
    public TaskCompletionSource<string>? SuitSelectionTcs { get; set; } = null!;
}