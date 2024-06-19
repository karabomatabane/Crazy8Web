using Crazy8.Models;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Crazy8Web.Pages;

public partial class StartPage : ComponentBase
{
    [Inject] private GameService GameService { get; set; }
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private ProtectedSessionStorage SessionStore { get; set; }
    [Inject] protected IMatToaster Toaster { get; set; }

    private HubConnection _hubConnection;
    private Player? Owner { get; set; }
    private static string? _inputName;
    private const string OwnerKey = "owner";
    
    protected override async Task OnInitializedAsync()
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

        await LoadOwnerFromSessionAsync();
    }
    
    private async Task LoadOwnerFromSessionAsync()
    {
        try
        {
            var result = await SessionStore.GetAsync<Player>(OwnerKey);
            Owner = result.Value;
            if (Owner != null)
            {
                StateHasChanged(); // Force re-render to update UI
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    private void PrepareToJoin()
    {
        // TODO: Use game id to add player to a game
        if (Owner == null) return;
        NavigationManager.NavigateTo($"/join/{Owner.PlayerId}");
    }
    

    private void CreateGame()
    {
        if (Owner == null) return;
        GameService.CreateGame(Owner);
        NavigationManager.NavigateTo($"lobby/{Owner.PlayerId}");
    }

    private async Task CreatePlayer()
    {
        Console.WriteLine(_inputName);
        if (string.IsNullOrEmpty(_inputName)) return;
        await SessionStore.SetAsync("test", "This is a test");
        Owner = new Player(_inputName);
        await SessionStore.SetAsync(OwnerKey, Owner);
    }

    private void ChangeName()
    {
        Owner = null;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
    


}