using Crazy8.Contracts;
using Crazy8.Models;
using Moq;

namespace Tests;

public class SpecialCardTest
{
    [Fact]
    public async Task TestProgressGame_WithValidCard()
    {
        // Arrange
        Player player1 = new Player("Tshiamo");
        Player player2 = new Player("Karabo");
        Player[] players = new[] { player1, player2 };
        Dictionary<string, IEffect?> specialCards = new Dictionary<string, IEffect?>
        {
            { "7", new JumpEffect() },
            { "8", new CallEffect() },
            { "Jack", new ReverseEffect() },
            { "2", new AttackEffect { Magnitude = 1 } },
            { "Joker", new AttackEffect { Magnitude = 2 } }
        };

        Mock<Deck> mockDeck = new Mock<Deck>();
        mockDeck.Setup(d => d.DealCards(2)).Returns(new Card[2]);
        mockDeck.Setup(d => d.TurnCard());

        Game game = new Game(players, specialCards)
        {
            Deck = mockDeck.Object
        };

        game.StartGame();

        Card card = new Card { Rank = "Ace", Suit = "Spades", Image = "" };
        player1.Hand = new[] { card };

        // Act
        await game.ProgressGame(card);

        // Assert
        Assert.DoesNotContain(card, player1.Hand); // Card should be removed from the player's hand
        mockDeck.Verify(d => d.AddCard(card), Times.Once); // Card should be added to the deck
        Assert.Equal(player2, game.Players[game.Turn]); // Turn should switch to the next player
    }
}