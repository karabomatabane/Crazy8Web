using Crazy8.Contracts;

namespace Crazy8.Models;

public class Card
{
    public string Suit { get; init; }
    public string Rank { get; init; }
    public string? Image { get; init; }
    private bool Revealed { get; set; } = true;
    public IEffect? Effect { get; set; }

    public void Flip()
    {
        Revealed = !Revealed;
    }

    public void ApplyEffect(Game game)
    {
        Effect?.Execute(game);
    }
}