using Crazy8.Constants;
using Crazy8.Contracts;

namespace Crazy8.Models;

public class ReverseEffect : IEffect
{
    public EffectFrequency Frequency { get; } = EffectFrequency.Persistent;
    public bool Immune => false;
    public void Execute(Game game)
    {
        game.Clockwise = !game.Clockwise;
    }
}

public class JumpEffect : IEffect
{
    public EffectFrequency Frequency { get; } = EffectFrequency.SingleTurn;
    public bool Immune => false;
    public void Execute(Game game)
    {
        game.Step = 2;
    }
}

public class CallEffect : IEffect
{
    public EffectFrequency Frequency => EffectFrequency.SingleTurn;
    public bool Immune => true;

    public void Execute(Game game)
    {
        game.RequiredSuit = PromptPlayerForSuit(game.GetFaceUp()!.Suit);
    }

    private string PromptPlayerForSuit(string defaultSuit)
    {
        Console.WriteLine($"Pick a suit:\n0. Hearts\n1. Diamonds\n2. Clubs\n3. Spades\nPress 'Enter' " +
                          $"to select default: {defaultSuit}");
        Console.Write("Choice (0-3): ");
        if (int.TryParse(Console.ReadLine(), out int choice))
        {
            return Const.Suits[choice];
        }

        return defaultSuit;
    }
}