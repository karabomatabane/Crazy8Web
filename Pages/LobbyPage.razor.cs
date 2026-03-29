using Crazy8.Models;
using Crazy8Web.Constants;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TextCopy;

namespace Crazy8Web.Pages;

public partial class LobbyPage : ComponentBase
{
    [Parameter]
    public string? GameId { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ProtectedSessionStorage SessionStore { get; set; } = null!;
    [Inject] private GameService GameService { get; set; } = null!;
    [Inject] protected IMatToaster Toaster { get; set; } = null!;

    private HubConnection? _hubConnection;
    private Player? Owner { get; set; }
    private List<string>? _readyPlayers;
    
    private List<Player>? _players;
    private bool _isMine = false;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Player>(Const.JoinedKey, async (player) =>
        {
            await InvokeAsync(() =>
            {
                if (Owner is null)
                {
                    throw new Exception("We can't retrieve user information. Please start again.");
                }

                if (GameId is not null)
                {
                    _players = GameService.GetOtherPlayers(GameId, Owner);
                    _isMine = GameService.IsMine(GameId, Owner.PlayerId);
                }
                StateHasChanged();
            });
        });

        _hubConnection.On<string>(Const.PlayerReady, async (playerId) =>
        {
            await InvokeAsync(() =>
            {
                if (GameId is not null)
                {
                    _readyPlayers = GameService.GetReadyPlayers(GameId);
                    StateHasChanged();
                }
            });
        });

        _hubConnection.On(Const.StartSession, () =>
        {
            NavigationManager.NavigateTo($"/board/{GameId}");
        });

        await _hubConnection.StartAsync();
        if(!string.IsNullOrEmpty(GameId))
{
            await _hubConnection.InvokeAsync(Const.JoinGameGroup, GameId);
        }
        await LoadOwnerFromSessionAsync();

        if (Owner is null)
        {
            throw new Exception("We can't retrieve user information. Please start again.");
        }
        if (GameId is not null)
        {
            _players = GameService.GetOtherPlayers(GameId, Owner);
            _isMine = GameService.IsMine(GameId, Owner.PlayerId);
            _readyPlayers = GameService.GetReadyPlayers(GameId);
        }
    }

    private async Task LoadOwnerFromSessionAsync()
    {
        try
        {
            ProtectedBrowserStorageResult<Player> result = await SessionStore.GetAsync<Player>(Const.OwnerKey);
            Owner = result.Value;
            if (Owner is not null)
            {
                StateHasChanged(); // Force re-render to update UI
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void Start()
    {
        if (Owner is null || GameId is null)
            return;
        if (_isMine)
        {
            GameService.StartSession(GameId);
        }
        else
        {
            GameService.PlayerReady(GameId, Owner.PlayerId);
        }
    }

    private async Task CopyToClip()
    {
        if (string.IsNullOrEmpty(GameId)) return;
        await ClipboardService.SetTextAsync(GameId);
        Toaster.Add("Code copied to clipboard!", MatToastType.Success);
    }

    private bool IsOwner(string playerId) => GameId is not null && GameService.GetOwnerId(GameId) == playerId;

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync(); 
        }
    }
}