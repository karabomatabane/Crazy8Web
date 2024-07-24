using Crazy8.Models;

namespace Crazy8.Contracts;

public enum EffectFrequency
{
    SingleTurn,
    WholeRound,
    Persistent
}
public enum EffectType
{
    Attack,
    Transformation
}
public interface IEffect
{
    EffectFrequency Frequency { get; }
    EffectType Type { get; }
    /// <summary>
    /// True if card can be played on top of any card
    /// </summary>
    bool Immune { get; }
    Task Execute(Game game);
}