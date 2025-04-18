using Crazy8Web.Constants;
using Crazy8.Models;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR.Client;

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
    private List<Player> _players = [];
    private Card[] _myCards = Array.Empty<Card>();
    private int _choice = 0;
    private bool _dialogIsOpen = false;
    private string? _suit = null;
    private string? _dialogSuit = null;
    private bool _requireSuit = false;
    private int? _tempChoice = null;
    private int _attacks = 0;
    private bool _gameHasEnded = false;
    private List<Player>? _results = null;
    private bool _isPeeking = false;
    
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
                Console.WriteLine("New FaceUp!!");
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
                _attacks = GameService.GetAttacks();
                _dialogIsOpen = false;
                StateHasChanged();
            }));
        });

        _hubConnection.On<string>(Const.PromptSuit, async (defaultSuit) =>
        {
            // Trigger the dialog for all players
            await InvokeAsync(() =>
            {
                _dialogSuit = defaultSuit;
                _dialogIsOpen = true;
                StateHasChanged();
            });
        });

        _hubConnection.On<List<Player>>(Const.EndGame, async (results) =>
        {
            await InvokeAsync(() =>
            {
                // TODO: Handle end game
                _gameHasEnded = true;
                _results = results;
                StateHasChanged();
            });
        });

        await _hubConnection.StartAsync();
        await LoadOwnerFromSessionAsync();

        _players = GameService.GetPlayers().ToList();
        if (Owner == null)
        {
            _myCards = [];
            return;
        }

        if (!GameService.IsGameRunning() && GameService.IsMine(Owner.PlayerId))
        {
            GameService.StartGame();
        }

        _myCards = GameService.GetPlayerCards(Owner.PlayerId);
        if (GameService.IsGameRunning())
        {
            _faceUp = GameService.GetFaceUp();
        }

        _requireSuit = !string.IsNullOrEmpty(GameService.GetRequiredSuit());
        _turn = _players[GameService.GetTurn()];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_myCards.Length > 0)
        {
            await JSRuntime.InvokeVoidAsync("applyCardFanEffect");
        }
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

    private string GetAttacks() => (_attacks * 2).ToString();

    private async void SelectCard(int choice)
    {
        if (!IsMyTurn()) return;
        if (_tempChoice == choice)
        {
            _choice = choice;
            StateHasChanged(); // Refresh the UI to apply the "selected" class
            //
            // Call the JavaScript function to animate the card
            // await JSRuntime.InvokeVoidAsync("animateCard", choice);
            // await Task.Delay(500);

            _tempChoice = null;
            PlayChoice();
        }
        else
            _tempChoice = choice;
    }

    private void SelectRequiredSuit(string suit)
    {
        if (!IsMyTurn()) return;
        _dialogSuit = suit;
        StateHasChanged();
    }

    private async void PlayChoice()
    {
        UpdateHasCalledOut(false);
        await GameService.ProgressGame(_myCards[_choice]);
    }

    private async void Pick()
    {
        await GameService.ProgressGame(null);
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }

    private void TogglePeek()
    {
        _isPeeking = !_isPeeking;

        // Auto-disable peeking after a short time
        if (_isPeeking)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                await InvokeAsync(() =>
                {
                    _isPeeking = false;
                    StateHasChanged();
                });
            });
        }

        StateHasChanged();
    }

    private void PenaliseForCardNumber(string playerId)
    {
        // TODO: Use _callOuts to decide if player must be penalised.
    }

    private void AnnounceCardCount()
    {
        UpdateHasCalledOut(true);
    }

    private void Rematch()
    {
        // TODO: Start a new game from scratch with the same players
    }

    private void LeaveGame()
    {
        // Navigate to the home page
        NavigationManager.NavigateTo("/");
    }

    private void UpdateHasCalledOut(bool value)
    {
        if (Owner == null) return;
        int index = _players.FindIndex(p => p.PlayerId == Owner.PlayerId);
        if (index < 0)
        {
            throw new InvalidOperationException("Owner is not in the game!");
        }
        _players[index].HasCalledOutThisTurn = value;
        Owner.HasCalledOutThisTurn = value;
    }
}