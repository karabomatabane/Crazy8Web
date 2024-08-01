using Crazy8.Contracts;

namespace Crazy8.Models;

public class Game
{
    public event Action<Card>? FaceUpCardChanged;
    public event Action<string>? PlayerTurnChanged;
    public string GameId { get; private set; }
    public Player[] Players { get; private set; }
    public string Owner { get; set; }
    public Deck? Deck { get; set; }
    private int Round { get; set; }
    private int TotalRounds { get; }
    public int Turn { get; private set; }
    public int Step { get; set; } = 1; // default step is 1 
    public bool Clockwise { get; set; } = true; // default direction is clockwise
    private List<IEffect?> ActiveEffects { get; set; } // list of active effects
    private List<Player> Bench { get; set; }
    private List<Player> Out { get; set; }
    public int Attacks { get; set; }
    private Dictionary<string, IEffect?> SpecialCards { get; set; }
    public string? RequiredSuit { get; set; }
    public bool IsRunning { get; set; }
    private bool _pivot = false;

    public Game(Player owner, Dictionary<string, IEffect?> specialCards)
    {
        GameId = Guid.NewGuid().ToString();
        Players = new[] { owner };
        Owner = owner.PlayerId;
        SpecialCards = specialCards;
        Round = 0;
        TotalRounds = Players.Length - 1;
        ActiveEffects = new List<IEffect?>();
        Bench = new List<Player>();
        Out = new List<Player>();
    }

    public Game(Player[] players, Dictionary<string, IEffect?> specialCards)
    {
        GameId = Guid.NewGuid().ToString();
        Players = players;
        Owner = players[0].PlayerId;
        SpecialCards = specialCards;
        Round = 0;
        TotalRounds = Players.Length - 1;
        ActiveEffects = new List<IEffect?>();
        Bench = new List<Player>();
        Out = new List<Player>();
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

        Deck = new Deck(54);

        Deck.Shuffle();
        DealCards(["8", "2", "Ace", "7", "Joker"]);
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
            Bench.Add(currentPlayer);
            List<Player> temp = new(Players);
            temp.Remove(currentPlayer);
            Players = temp.ToArray();
            if (Players.Length == 1)
            {
                Out.Add(Players[0]);
                Round++;
                if (Round >= TotalRounds)
                    EndGame();
                StartGame(Round);
            }
        }
    }

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
                isValidMove = true;
            }
            else
            {
                currentPlayer.PickCards(Deck, 2);
                return true;
            }
        }
        else if (!string.IsNullOrEmpty(RequiredSuit)) RequiredSuit = string.Empty;

        if (isValidMove || cardEffect is AttackEffect)
        {
            Deck.AddCard(playerChoice);
            if (currentPlayer.Hand == null)
                return false;
            Card[] playerCards = currentPlayer.Hand.Where(card => card != playerChoice).ToArray();
            Players[Turn].Hand = playerCards;
            await ApplySpecialCard(playerChoice.Rank);
            NotifyFaceUp();
            return true;
        }

        return false;
    }

    public void AddPlayer(Player player)
    {
        if (Players.Contains(player)) return;
        Player[] players = Players;
        Array.Resize(ref players, players.Length + 1);
        players[^1] = player;
        Players = players;
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

    private void EndGame()
    {
        IsRunning = false;
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
        if (Deck == null)
        {
            Console.WriteLine("No deck. Panic!!");
            return;
        }

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

        // Add effect to active effects if not single-turn
        if (effect.Frequency != EffectFrequency.SingleTurn)
        {
            ActiveEffects.Add(effect);
        }
    }

    private void SetNext()
    {
        int n = Players.Length;
        if (n == 2 && _pivot)
        {
            _pivot = false;
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