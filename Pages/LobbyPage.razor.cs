﻿using Crazy8.Models;
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
    private HubConnection _hubConnection;
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private ProtectedSessionStorage SessionStore { get; set; }
    [Inject] private GameService GameService { get; set; }
    private Player? Owner { get; set; }
    [Inject] protected IMatToaster Toaster { get; set; }
    private List<Player>? _players;
    private bool _isMine = false;
    
    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .Build();
        _hubConnection.On<Player>(Const.JoinedKey, async (player) =>
        {
            await InvokeAsync(() =>
            {
                if (Owner == null)
                {
                    throw new Exception("We can't retrieve user information. Please start again.");
                }
                _players?.Add(player);
                _isMine = GameService.IsMine(Owner.PlayerId);
                StateHasChanged();
            });
        });
        await _hubConnection.StartAsync();
        await LoadOwnerFromSessionAsync();
        if (Owner == null)
        {
            throw new Exception("We can't retrieve user information. Please start again.");
        }
        _players = new List<Player>();
        _isMine = GameService.IsMine(Owner.PlayerId);
    }
    
    private async Task LoadOwnerFromSessionAsync()
    {
        try
        {
            var result = await SessionStore.GetAsync<Player>(Const.OwnerKey);
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

    private void Start()
    {
        // TODO: implement game start session.
    }


    private async Task CopyToClip()
    {
        if (string.IsNullOrEmpty(GameId)) return;
        await ClipboardService.SetTextAsync(GameId);
        Toaster.Add("Code copied to clipboard!", MatToastType.Success);
    }
    
    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}