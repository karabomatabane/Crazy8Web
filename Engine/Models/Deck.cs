using Crazy8.Constants;

namespace Crazy8.Models;

public sealed class Deck
{
    private List<Card> FaceDown { get; set; }
    public event EventHandler<VibeCheckEventArgs> VibeCheckEvent = null!;
    public List<Card> FaceUp { get; set; }
    private static readonly Random Random = new Random();
    private readonly int _size;

    public Deck(int size = 52)
    {
        FaceDown = new List<Card>();
        FaceUp = new List<Card>();
        _size = size;
        foreach (string suit in Const.Suits)
        {
            foreach (string rank in Const.Ranks)
            {
                FaceDown.Add(new Card()
                {
                    Suit = suit, Rank = rank, Image = $"assets/cards/{rank.ToLower()}_of_{suit.ToLower()}.png"
                });
            }
        }

        FaceDown.Add(new Card()
        {
            Suit = "Red", Rank = "Joker", Image = $"assets/cards/red_joker.png"
        });
        FaceDown.Add(new Card()
        {
            Suit = "Black", Rank = "Joker", Image = $"assets/cards/black_joker.png"
        });
    }

    public int GetCount()
    {
        return FaceDown.Count;
    }

    public void Shuffle()
    {
        // Fisher–Yates shuffle
        int n = FaceDown.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Next(n + 1);
            // val at pos n becomes value at pos n and vice-versa
            (FaceDown[k], FaceDown[n]) = (FaceDown[n], FaceDown[k]);
        }
    }

    public void TurnCard()
    {
        if (FaceDown.Count > 0)
        {
            FaceUp.Add(FaceDown[0]);
            FaceDown.RemoveAt(0);
        }
        else
        {
            // take all the face up cards except the last one
            FaceDown = FaceUp.Take(FaceUp.Count - 1).ToList();
            // shuffle the deck
            Shuffle();
        }
    }

    public void AddCard(Card card)
    {
        if (FaceDown.Contains(card) || FaceUp.Contains(card))
        {
            // throw new Exception("You cannot have a card that already exists in the deck!\nPanic!!!");
            VibeCheckEventArgs args = new("You cannot have a card that already exists in the deck!") 
                { Card = card };
            OnVibeCheckEvent(args);
        }

        FaceUp.Add(card);
    }

    /// <summary>
    /// Deal cards from deck
    /// </summary>
    /// <param name="count">Number of cards to deal</param>
    /// <returns>Array of cards <see cref="Card"/></returns>
    public Card[] DealCards(int count)
    {
        Card[] cards = FaceDown.Take(count).ToArray();
        FaceDown.RemoveRange(0, count);
        return cards;
    }

    /// <summary>
    /// Deal specific cards
    /// </summary>
    /// <param name="ranks">Array of ranks required</param>
    /// <returns></returns>
    public Card[] DealCards(string[] ranks)
    {
        return ranks.Select(rank => GetCard(rank)!).ToArray();
    }

    private Card? GetCard(string rank)
    {
        Card? card = FaceDown.FirstOrDefault(card => card.Rank == rank);
        if (card != null) FaceDown.Remove(card);
        return card;
    }

    /// <summary>
    /// Checks the distribution of cards across the game.
    /// </summary>
    /// <returns>True if cards are consistent, and false if universe is fucked.</returns>
    public bool VibeCheck(Player[] players, string invoker)
    {
        // Count cards
        int playersHands = 0;
        foreach (Player player in players)
        {
            if (player.Hand == null) continue;
            playersHands += player.Hand.Length;
        }
        int totalCards = FaceDown.Count + FaceUp.Count + playersHands;
        if (totalCards == _size) return true;
        OnVibeCheckEvent(new 
            VibeCheckEventArgs($"The universe is fucked. Check {invoker} to find out what happened."));
        return false;
    }

    public void Reset()
    {
        foreach (Card card in FaceUp.ToList())
        {
            FaceDown.Add(card);
            FaceUp.Remove(card);
        }
    }
    
    public class VibeCheckEventArgs(string reason) : EventArgs
    {
        public string Reason { get; set; } = reason;
        public Card? Card { get; set; }
    }

    private void OnVibeCheckEvent(VibeCheckEventArgs e)
    {
        VibeCheckEvent.Invoke(this, e);
    }
}