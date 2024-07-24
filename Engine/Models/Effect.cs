using Crazy8.Constants;
using Crazy8.Contracts;

namespace Crazy8.Models;


public class ReverseEffect : IEffect
{
    public EffectFrequency Frequency { get; } = EffectFrequency.Persistent;
    public bool Immune => false;
    public EffectType Type { get; } = EffectType.Transformation;
    public async Task Execute(Game game)
    {
        game.Clockwise = !game.Clockwise;
    }
}

public class JumpEffect : IEffect
{
    public EffectFrequency Frequency { get; } = EffectFrequency.SingleTurn;
    public bool Immune => false;
    public EffectType Type { get; } = EffectType.Transformation;
    public async Task Execute(Game game)
    {
        game.Step = 2;
    }
}

public class AttackEffect : IEffect
{
    public EffectFrequency Frequency { get; } = EffectFrequency.SingleTurn;
    public bool Immune { get; init; }
    public EffectType Type { get; } = EffectType.Transformation;
    public int Magnitude { get; init; } = 1;
    public async Task Execute(Game game)
    {
        game.Attacks += Magnitude;
    }
}

public class CallEffect : IEffect
{
    public EffectFrequency Frequency => EffectFrequency.SingleTurn;
    public EffectType Type { get; } = EffectType.Transformation;
    public bool Immune => true;
    // Define an event to prompt the player for a suit
    public static event Func<string, Task<string>>? SuitPrompted;

    public async Task Execute(Game game)
    {
        if (SuitPrompted != null)
        {
            game.RequiredSuit = await SuitPrompted.Invoke(game.GetFaceUp()!.Suit);
        }
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