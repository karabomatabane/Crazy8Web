﻿using Crazy8Web.Constants;
using Crazy8.Models;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR.Client;
using TextCopy;

namespace Crazy8Web.Pages;

public partial class Game : ComponentBase
{
    [Inject] private GameService GameService { get; set; }
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private ProtectedSessionStorage SessionStore { get; set; }
    [Inject] protected IMatToaster Toaster { get; set; }
    private Player? Owner { get; set; }
    
    private HubConnection _hubConnection;
    private Player? _turn;
    private Card? _faceUp;
    private List<Player> _players = new();
    private Card[] _myCards = Array.Empty<Card>();
    private int _choice = 0;
    private bool _dialogIsOpen = false;
    private string? _suit = null;
    private string? _dialogSuit = null;
    private bool _requireSuit = false;
    private int? _tempChoice = null;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .Build();
        _hubConnection.On<Card>(Const.FaceUp, async (card) =>
        {
            // Update UI with face-up card
            await InvokeAsync((() =>
            {
                _players = GameService.GetPlayers().ToList();
                _faceUp = card;
                _requireSuit = !string.IsNullOrEmpty(GameService.GetRequiredSuit());
                StateHasChanged();
            }));
        });

        _hubConnection.On<string>(Const.PlayerTurn, async (playerId) =>
        {
            // Update UI with current player's turn
            await InvokeAsync((() =>
            {
                _players = GameService.GetPlayers().ToList();
                _turn = _players?.FirstOrDefault(p => p.PlayerId == playerId);
                if (Owner != null)
                    _myCards = GameService.GetPlayerCards(Owner.PlayerId);
                _choice = 0;
                _tempChoice = null;
                StateHasChanged();
            }));
        });
        
        _hubConnection.On<string>(Const.PromptSuit, PromptPlayerForSuit);

        await _hubConnection.StartAsync();
        await LoadOwnerFromSessionAsync();
        if (!GameService.IsGameRunning())
        {
            GameService.StartGame();
        }
        _players = GameService.GetPlayers().ToList();
        if (Owner == null)
        {
            _myCards = [];
            return;
        }
        _myCards = GameService.GetPlayerCards(Owner.PlayerId);
        _faceUp = GameService.GetFaceUp();
        _requireSuit = !string.IsNullOrEmpty(GameService.GetRequiredSuit());
        _turn = _players[GameService.GetTurn()];
    }

    private async Task LoadOwnerFromSessionAsync()
    {
        try
        {
            ProtectedBrowserStorageResult<Player> result = await SessionStore.GetAsync<Player>(Const.OwnerKey);
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
    
    private void PromptPlayerForSuit(string defaultSuit)
    {
        _dialogSuit = defaultSuit;
        _dialogIsOpen = true && IsMyTurn();
    }
    
    private void OkClick()
    {
        _suit = _dialogSuit;
        _dialogIsOpen = false;
        if (_suit == null) return;
        GameService.ReceiveSuitSelection(_suit);
        _requireSuit = !string.IsNullOrEmpty(GameService.GetRequiredSuit());
    }

    private string GetPlayerName(Player player)
    {
        if (Owner != null)
        {
            return player.PlayerId == Owner.PlayerId ? "You" : player.Name;
        }

        return "";
    }

    private bool IsMyTurn()
    {
        if (Owner != null && _turn != null)
            return _turn.PlayerId == Owner.PlayerId;
        return false;
    }

    private void SelectCard(int choice)
    {
        if (!IsMyTurn()) return;
        if (_tempChoice == choice)
        {
            _choice = choice;
            PlayChoice();
        }
        else
            _tempChoice = choice;
    }

    private async void PlayChoice()
    {
        if (_myCards == null)
            return;
        await GameService.ProgressGame(_myCards[_choice]);
    }

    private async void Skip()
    {
        await GameService.ProgressGame(null);
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}