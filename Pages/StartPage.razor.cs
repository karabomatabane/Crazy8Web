using Crazy8.Models;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
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
    private bool _isPlayerSetup;
    private const string OwnerKey = "owner";

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .Build();
        _inputName = string.Empty;
        _isPlayerSetup = true;

        await _hubConnection.StartAsync();
        await LoadOwnerFromSessionAsync();
    }

    private async Task LoadOwnerFromSessionAsync()
    {
        try
        {
            ProtectedBrowserStorageResult<Player> result = await SessionStore.GetAsync<Player>(OwnerKey);
            Owner = result.Value;
            if (Owner != null)
            {
                _isPlayerSetup = false;
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
        NavigationManager.NavigateTo($"lobby/{GameService.GetGameId()}");
    }

    private async Task CreatePlayer()
    {
        if (string.IsNullOrEmpty(_inputName)) return;
        if (Owner == null)
        {
            Owner = new Player(_inputName);
        }
        else
        {
            Owner.Name = _inputName;
            _isPlayerSetup = false;
            StateHasChanged();
        }
        await SessionStore.SetAsync(OwnerKey, Owner);
    }
    
    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrEmpty(_inputName))
        {
            await CreatePlayer();
        }
    }

    private void EditName()
    {
        if (Owner != null) _inputName = Owner.Name;
        _isPlayerSetup = true;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}