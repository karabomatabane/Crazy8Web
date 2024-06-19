using Crazy8.Models;
using Crazy8Web.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Crazy8Web.Pages;

public partial class LobbyPage : ComponentBase
{
    [Parameter]
    public string? OwnerId { get; set; }
    private HubConnection _hubConnection;
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .Build();
        _hubConnection.On<Player>(Const.JoinedKey, (player) =>
        {
            // Update UI with face-up card
            JSRuntime.InvokeVoidAsync("alert", $"Player: {player.Name} just joined the game.");
        });
        
        await _hubConnection.StartAsync();
    }

}