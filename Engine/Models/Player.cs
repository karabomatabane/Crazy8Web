using System.Diagnostics;
using Crazy8.Utils;

namespace Crazy8.Models;

public class Player
{
    public Player(string name)
    {
        PlayerId = Guid.NewGuid().ToString();
        Name = name;
    }

    public string PlayerId { get; set; }
    private string Name { get; set; }
    public Card[]? Hand { get; set; }

    public Card? PlayCard()
    {
        if (Hand == null) return null;
        Console.WriteLine($"{Name}'s turn\n====================\nPick card from:\n" +
                          Utility.ListCards(Hand) + "\n====================");
        Console.Write("Your choice: ");
        return int.TryParse(Console.ReadLine(), out int choice) ? Hand[choice] : null;
    }

    public void PickCards(Deck deck, int numCards)
    {
        Card[]? playerHand = Hand;
        playerHand ??= Array.Empty<Card>(); //if hand is null, initialise as empty array.
        int n = playerHand.Length;
        Array.Resize(ref playerHand, n + numCards);
        Hand = playerHand;
        for (int i = 0; i < numCards; i++)
        {
            Hand[n + i] = deck.DealCards(1)[0];
        }
    }

    public bool HasCards()
    {
        Debug.Assert(Hand != null, nameof(Hand) + " != null");
        return Hand.Length > 0;
    }
}