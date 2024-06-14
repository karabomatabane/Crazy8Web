using Crazy8.Contracts;

namespace Crazy8.Models;

public class Game
{
    public event Action<Card>? FaceUpCardChanged;
    public event Action<string>? PlayerTurnChanged;
    public string GameId { get; private set; }
    private Player[] Players { get; set; }
    public string Owner { get; set; }
    private Deck Deck { get; set; }
    private int Round { get; set; }
    private int TotalRounds { get; }
    private int Turn { get; set; }
    public int Step { get; set; } = 1; // default step is 1 
    public bool Clockwise { get; set; } = true; // default direction is clockwise
    private List<IEffect?> ActiveEffects { get; set; } // list of active effects
    private List<Player> Bench { get; set; }
    private List<Player> Out { get; set; }
    private Dictionary<string, IEffect?> SpecialCards { get; set; }
    public string? RequiredSuit { get; set; }

    public Game(Player owner, Dictionary<string, IEffect?> specialCards)
    {
        GameId = Guid.NewGuid().ToString();
        Players = new[] { owner };
        SpecialCards = specialCards;
        Deck = new Deck();
        Round = 0;
        TotalRounds = Players.Length - 1;
        ActiveEffects = new List<IEffect?>();
        Bench = new List<Player>();
        Out = new List<Player>();
    }

    public void StartGame(int round = 1)
    {
        if (Players.Length < 2) return;
        if (round > 1)
        {
            Players = Bench.ToArray();
            Bench = new List<Player>();
        }
        Deck.Shuffle();
        DealCards(5);
        while (true)
        {
            Deck.TurnCard();
            Card? card = GetFaceUp();
            if (card == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                throw new Exception("You shouldn't be here without a card!\nPanic!!!");
            }
            if (SpecialCards.TryGetValue(card.Rank, out IEffect? effect) && effect != null)
            {
                Deck.Reset();
                Deck.Shuffle();
            }
            else
            {
                FaceUpCardChanged?.Invoke(card);
                break;
            }
        }
        Round = round;
        NotifyPlayerTurn();
    }

    public void ProgressGame(Card? playerChoice)
    {
        /*    Manages the turn-based logic of the game, allowing each player to take their turn.
              Applies any special card effects if a special card is played.
             Checks game conditions (e.g., if a player has exhausted all their cards).
             Determines if the round has ended and prepares for the next round if needed.*/
        Player currentPlayer = Players[Turn];
        if (playerChoice == null)
        {
            currentPlayer.PickCards(Deck, 1);
            SetNext();
            Console.Clear();
            return;
        }

        Card? faceUp = GetFaceUp();
        if (faceUp == null) return;
        bool isValidMove = (SpecialCards.TryGetValue(playerChoice.Rank, out IEffect? effect) && effect is
                           {
                               Immune: true
                           }) || // immune special
                           RequiredSuit == playerChoice.Suit || // matches required
                           string.IsNullOrEmpty(RequiredSuit) && (playerChoice.Suit == faceUp.Suit || playerChoice.Rank //matches face up
            == faceUp.Rank);
        if (!isValidMove)
        {
            currentPlayer.PickCards(Deck, 2);
        }
        else if (!string.IsNullOrEmpty(RequiredSuit)) RequiredSuit = string.Empty;
        Deck.AddCard(playerChoice);
        ApplySpecialCard(playerChoice.Rank);
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
        Console.Clear();
    }

    public Card? GetFaceUp()
    {
        return Deck.FaceUp.Count > 0 ? Deck.FaceUp[^1] : null;
    }

    private void EndGame()
    {
        Console.WriteLine($"THE GAME HAS ENDED!\nWINNER: {Players[0]}");
        for (int i = 0; i < Out.Count; i++)
        {
            Console.WriteLine($"Rank: {i + 2} => {Out[i]}");
        }

        Console.WriteLine("THANK YOU FOR PLAYING!!");
    }

    private void DealCards(int count)
    {
        foreach (Player player in Players)
        {
            player.Hand = Deck.DealCards(count);
        }
    }

    private void ApplySpecialCard(string cardRank)
    {
        if (!SpecialCards.TryGetValue(cardRank, out IEffect? effect) || effect == null) return;
        effect.Execute(this);
            
        // Add effect to active effects if not single-turn
        if (effect.Frequency != EffectFrequency.SingleTurn)
        {
            ActiveEffects.Add(effect);
        }
    }

    private void SetNext()
    {
        int n = Players.Length - 1;
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
        NotifyPlayerTurn();
    }
    
    private void NotifyPlayerTurn()
    {
        string currentPlayerId = Players[Turn].PlayerId;
        PlayerTurnChanged?.Invoke(currentPlayerId);
    }
}