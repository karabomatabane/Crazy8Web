using Crazy8.Models;

namespace Crazy8.Utils;

public static class Utility
{
    public static string ListCards(Card[] cards)
    {
        string output = "";
        for (int i = 0; i < cards.Length; i++)
        {
            output += $"{i}. {cards[i].Rank} of {cards[i].Suit}\n";
        }

        return output;
    }
}