using Crazy8.Models;

namespace Crazy8.Contracts;

public enum EffectFrequency
{
    SingleTurn,
    WholeRound,
    Persistent
}
public interface IEffect
{
    EffectFrequency Frequency { get; }
    bool Immune { get; }
    void Execute(Game game);
}