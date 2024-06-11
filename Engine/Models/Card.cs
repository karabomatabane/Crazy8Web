using Crazy8.Contracts;

namespace Crazy8.Models;

public class Card
{
    public string Suit { get; init; }
    public string Rank { get; init; }
    public IEffect? Effect { get; set; }

    public void Flip()
    {
        //TODO: flips the card face up
    }

    public void ApplyEffect(Game game)
    {
        Effect?.Execute(game);
    }
}