using Crazy8.Contracts;

namespace Crazy8.Models;

public class Game
{
    /// <summary>
    /// Event triggered when the face-up card changes
    /// </summary>
    public event Action<Card>? FaceUpCardChanged;

    /// <summary>
    /// Event triggered when the current player's turn changes
    /// </summary>
    public event Action<string>? PlayerTurnChanged;

    /// <summary>
    /// Event triggered when the game ends
    /// </summary>
    public event Action<List<Player>>? GameHasEnded;

    /// <summary>
    /// Unique identifier for the game session
    /// </summary>
    public string GameId { get; private set; }

    /// <summary>
    /// Array of currently active players in the game
    /// </summary>
    public Player[] Players { get; private set; }

    /// <summary>
    /// Player ID of the game owner/creator
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// The deck of cards used in the game
    /// </summary>
    public Deck Deck { get; set; }

    /// <summary>
    /// Current round number
    /// </summary>
    private int Round { get; set; }

    /// <summary>
    /// Total number of rounds to be played
    /// </summary>
    private int TotalRounds { get; }

    /// <summary>
    /// Index of the player whose turn it currently is
    /// </summary>
    public int Turn { get; private set; }

    /// <summary>
    /// Number of positions to advance on each turn
    /// </summary>
    public int Step { get; set; } = 1; // default step is 1

    /// <summary>
    /// Direction of play (true for clockwise, false for counter-clockwise)
    /// </summary>
    public bool Clockwise { get; set; } = true; // default direction is clockwise

    /// <summary>
    /// Players who have finished their cards for the current round
    /// </summary>
    private List<Player> Bench { get; set; }

    /// <summary>
    /// Players who are out of the game completely
    /// </summary>
    private List<Player> Out { get; set; }

    /// <summary>
    /// Number of accumulated attack points that next player must respond to or draw cards
    /// </summary>
    public int Attacks { get; set; }

    /// <summary>
    /// Dictionary mapping card ranks to their special effects
    /// </summary>
    private Dictionary<string, IEffect?> SpecialCards { get; set; }

    /// <summary>
    /// Suit that must be played in the next turn (e.g., after playing an 8)
    /// </summary>
    public string? RequiredSuit { get; set; }

    /// <summary>
    /// Indicates whether the game is currently in progress
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Flag to handle special case in direction change for 2-player games
    /// </summary>
    private bool _pivot;

    private const int DeckSize = 54;

    public Game(Player owner, Dictionary<string, IEffect?> specialCards)
    {
        GameId = Guid.NewGuid().ToString();
        Players = [owner];
        Owner = owner.PlayerId;
        SpecialCards = specialCards;
        Round = 0;
        TotalRounds = Players.Length - 1;
        Bench = [];
        Out = [];
        Deck = new Deck(DeckSize);
    }

    public Game(Player[] players, Dictionary<string, IEffect?> specialCards)
    {
        GameId = Guid.NewGuid().ToString();
        Players = players;
        Owner = players[0].PlayerId;
        SpecialCards = specialCards;
        Round = 0;
        TotalRounds = Players.Length - 1;
        Bench = [];
        Out = [];
        Deck = new Deck(DeckSize);
    }

    public void StartGame(int round = 1)
    {
        IsRunning = true;
        if (Players.Length < 2) return;
        if (round > 1)
        {
            Players = Bench.ToArray();
            Bench = [];
        }

        if (Deck.GetCount() != DeckSize)
        {
            Deck = new Deck(DeckSize);
        }

        Deck.Shuffle();
        DealCards(5);
        while (true)
        {
            Deck.TurnCard();
            Card? card = GetFaceUp();

            if (card != null && SpecialCards.TryGetValue(card.Rank, out IEffect? effect) && effect != null)
            {
                Deck.Reset();
                Deck.Shuffle();
            }
            else
            {
                NotifyFaceUp(card);
                break;
            }
        }

        Round = round;
        NotifyPlayerTurn();
    }

    public async Task ProgressGame(Card? playerChoice)
    {
        /*    Manages the turn-based logic of the game, allowing each player to take their turn.
              Applies any special card effects if a special card is played.
             Checks game conditions (e.g., if a player has exhausted all their cards).
             Determines if the round has ended and prepares for the next round if needed.*/
        Player currentPlayer = Players[Turn];
        if (await ValidateChoice(playerChoice) == false) return;
        SetNext();
        if (!currentPlayer.HasCards())
        {
            if (playerChoice != null && SpecialCards.TryGetValue(playerChoice.Rank, out IEffect? cardEffect) &&
                cardEffect != null) return;
            Bench.Add(currentPlayer);
            List<Player> temp = [..Players];
            temp.Remove(currentPlayer);
            Players = temp.ToArray();
            if (Players.Length == 1)
            {
                Out.Add(Players[0]);
                Round++;
                if (Round >= TotalRounds)
                {
                    Out.Reverse();
                    List<Player> results = [..Bench, ..Out];
                    EndGame(results);
                }
                else
                {
                    StartGame(Round);
                }
            }
        }
    }

    /// <summary>
    /// Validates the player's choice of card to play.
    /// If the choice is null, the player draws cards from the deck.
    /// If the choice is a valid card, it is played and any special effects are applied.
    /// If the choice is invalid, the player draws cards as a penalty.
    /// </summary>
    /// <param name="playerChoice"></param>
    /// <returns></returns>
    private async Task<bool> ValidateChoice(Card? playerChoice)
    {
        Player currentPlayer = Players[Turn];
        if (playerChoice == null)
        {
            if (Attacks > 0)
            {
                currentPlayer.PickCards(Deck, 2 * Attacks);
                Attacks = 0;
                return true;
            }

            currentPlayer.PickCards(Deck, 1);
            return true;
        }

        Card? faceUp = GetFaceUp();
        if (faceUp == null) return false;
        // TODO: find out why players don't get penalised if they play invalid card on top of a joker
        bool isValidMove = (SpecialCards.TryGetValue(playerChoice.Rank, out IEffect? cardEffect) && cardEffect is
                           {
                               Immune: true
                           }) || // immune special
                           RequiredSuit == playerChoice.Suit || // matches required
                           string.IsNullOrEmpty(RequiredSuit) &&
                           (playerChoice.Suit == faceUp.Suit || // matches suit
                            playerChoice.Rank == faceUp.Rank); // matches rank
        Console.WriteLine("Is player choice a call effect? " + (cardEffect is CallEffect));
        Console.WriteLine("Player's choice: " + playerChoice.Rank);
        Console.WriteLine("Face up card: " + faceUp.Rank);
        Console.WriteLine("Required suit: " + RequiredSuit);
        if (Attacks > 0)
        {
            if (cardEffect is not AttackEffect)
            {
                currentPlayer.PickCards(Deck, 2 * Attacks);
                Attacks = 0;
                return false;
            }
            else
                isValidMove = true;
        }
        else if (!isValidMove)
        {
            if (Attacks == 0)
            {
                isValidMove =
                    true; // TODO: Investigate as to why this is needed (might need a `faceUp.Effect is AttackEffect`)
            }
            else
            {
                currentPlayer.PickCards(Deck, 2);
                return true;
            }
        }
        else if (!string.IsNullOrEmpty(RequiredSuit) && cardEffect is not CallEffect) RequiredSuit = string.Empty;

        if (!isValidMove && cardEffect is not AttackEffect) return false;
        Deck.AddCard(playerChoice);
        if (currentPlayer.Hand == null)
            return false;
        Card[] playerCards = currentPlayer.Hand.Where(card => card != playerChoice).ToArray();
        Players[Turn].Hand = playerCards;
        await ApplySpecialCard(playerChoice.Rank);
        NotifyFaceUp();
        return true;
    }

    /// <summary>
    /// Penalizes a player by forcing them to draw two additional cards from the deck
    /// </summary>
    /// <param name="playerId">The unique identifier of the player to be penalized</param>
    public void PenalisePlayer(string playerId)
    {
        Player? player = Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;
        player.PickCards(Deck, 2);
        player.HasCalledOutThisTurn = true;
        NotifyPlayerTurn();
    }

    public void AddPlayer(Player player)
    {
        Console.WriteLine($"Adding: {player.Name} to {Players.Length}");
        if (Players.Contains(player)) return;
        Player[] players = Players;
        Array.Resize(ref players, players.Length + 1);
        players[^1] = player;
        Players = players;
        Console.WriteLine($"Leaving us with: {Players.Length}");
    }

    public Player[] GetPlayers()
    {
        return Players;
    }

    public Card? GetFaceUp()
    {
        Card? card = null;
        if (Deck is { FaceUp.Count: > 0 })
        {
            card = Deck.FaceUp[^1];
        }

        return card;
    }

    /// <summary>
    /// Ends the game and notifies all players of the results.
    /// </summary>
    /// <param name="results">
    /// Ordered list of players who finished the game.
    /// </param>
    private void EndGame(List<Player> results)
    {
        IsRunning = false;
        GameHasEnded?.Invoke(results);
        // Console.WriteLine($"THE GAME HAS ENDED!\nWINNER: {Players[0]}");
        // for (int i = 0; i < Out.Count; i++)
        // {
        //     Console.WriteLine($"Rank: {i + 2} => {Out[i]}");
        // }
        //
        // Console.WriteLine("THANK YOU FOR PLAYING!!");
    }

    private void DealCards(int count)
    {
        foreach (Player player in Players)
        {
            player.Hand = Deck.DealCards(count);
            _ = Deck.VibeCheck(Players, "DEAL CARDS");
        }
    }

    private void DealCards(string[] ranks)
    {
        //TODO: find out why we dealing more cards than needed => resolved
        //Resolution: StartGame was called by each player, leading to card miscount
        foreach (Player player in Players)
        {
            player.Hand = Deck.DealCards(ranks);
        }
    }

    private async Task ApplySpecialCard(string cardRank)
    {
        bool directionBefore = Clockwise;
        if (!SpecialCards.TryGetValue(cardRank, out IEffect? effect) || effect == null) return;
        await effect.Execute(this);
        _pivot = directionBefore != Clockwise;
    }

    private void SetNext()
    {
        int n = Players.Length;
        if (n == 2 && _pivot)
        {
            _pivot = false;
            NotifyPlayerTurn();
            return;
        }

        if (Clockwise)
        {
            Turn = (Turn + Step + n) % n;
        }
        else
        {
            Turn = (Turn + n - Step) % n;
        }

        // reset to default
        Step = 1;
        _pivot = false;
        NotifyPlayerTurn();
    }

    private void NotifyPlayerTurn()
    {
        string currentPlayerId = Players[Turn].PlayerId;
        PlayerTurnChanged?.Invoke(currentPlayerId);
    }

    private void NotifyFaceUp(Card? card = null)
    {
        FaceUpCardChanged?.Invoke(card ?? GetFaceUp() ?? new Card() { Rank = "Unknown", Suit = "Unknown" });
    }
}