using Crazy8.Models;
using Crazy8Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR.Client;

namespace Crazy8Web.Pages;

public partial class Game : ComponentBase
{
    [Inject] private GameService GameService { get; set; }
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    private HubConnection _hubConnection;

    protected override async void OnInitialized()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .Build();
        _hubConnection.On<Card>("FaceUp", (card) =>
        {
            // Update UI with face-up card
            JSRuntime.InvokeVoidAsync("alert", $"Face up card: {card.Rank} of {card.Suit}");
        });

        _hubConnection.On<string>("PlayerTurn", (playerId) =>
        {
            // Update UI with current player's turn
            JSRuntime.InvokeVoidAsync("alert", $"It's {playerId}'s turn");
        });

        await _hubConnection.StartAsync();
    }
    private void StartGame()
    {
        GameService.StartGame();
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}