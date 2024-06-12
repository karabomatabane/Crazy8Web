using Crazy8.Models;
using Microsoft.AspNetCore.SignalR;

namespace Crazy8Web.Hubs;

public class GameHub : Hub
{
    public async Task ShowFaceUp(Card card)
    {
        await Clients.All.SendAsync("FaceUp", card);
    }

    public async Task ShowTurn(string playerId)
    {
        await Clients.All.SendAsync("PlayerTurn", playerId);
    }
}