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
    [Parameter]
    public string? GameId { get; set; }
    [Inject] private GameService GameService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ProtectedSessionStorage SessionStore { get; set; } = null!;
    [Inject] protected IMatToaster Toaster { get; set; } = null!;
    /// <summary>
    /// Gets or sets the player instance representing the local user.
    /// </summary>
    private Player? LocalPlayer { get; set; }

    private HubConnection? _hubConnection;
    private Player? _turn;
    private Card? _faceUp;
    private List<Player> _players = [];
    private Card[] _myCards = [];
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
    private bool _clickedRematch = false;
    private int _rematchCount = 0;
    
    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/gameHub"))
            .WithAutomaticReconnect()
            .Build();
        _hubConnection.On<Card>(Const.FaceUp, async (card) =>
        {
            if (GameId is null) return;
            // Update UI with face-up card
            await InvokeAsync((() =>
            {
                _players = [.. GameService.GetPlayers(GameId)];
                _faceUp = card;
                _requireSuit = !string.IsNullOrEmpty(GameService.GetRequiredSuit(GameId));
                Console.WriteLine("New FaceUp!!");
                StateHasChanged();
            }));
        });

        _hubConnection.On<string>(Const.PlayerTurn, async (playerId) =>
        {
            if (GameId is null) return;
            // Update UI with current player's turn
            await InvokeAsync((() =>
            {
                _players = [.. GameService.GetPlayers(GameId)];
                _turn = _players?.FirstOrDefault(p => p.PlayerId == playerId);
                if (LocalPlayer != null)
                    _myCards = GameService.GetPlayerCards(GameId, LocalPlayer.PlayerId);
                _choice = 0;
                _tempChoice = null;
                _attacks = GameService.GetAttacks(GameId);
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
                _players = results;
                StateHasChanged();
            });
        });

        _hubConnection.On(Const.RematchClicked, async () =>
        {
            await InvokeAsync(() =>
            {
                _rematchCount++;
                if (_rematchCount == _players.Count && LocalPlayer != null && GameId != null && GameService.IsGameCreator(GameId, LocalPlayer.PlayerId))
                {
                    GameService.RestartGame(GameId, _players);
                }
            });
        });
        
       
        
        _hubConnection.On<string, string, int>(Const.CallOut, async (callerPlayerId, playerName, count) =>
        {
            await InvokeAsync(() =>
            {
                // Do not show the call-out toaster to the player who triggered it.
                if (LocalPlayer is not null && string.Equals(LocalPlayer.PlayerId, callerPlayerId, StringComparison.Ordinal))
                {
                    return;
                }

                Toaster.Add($"{playerName} has {count} cards!", MatToastType.Info, "Call Out");
                StateHasChanged();
            });
        });

        _hubConnection.On<string, string>(Const.PenaltyApplied, async (penalisedPlayerId, penalisedPlayerName) =>
        {
            await InvokeAsync(() =>
            {
                if (LocalPlayer is not null && string.Equals(LocalPlayer.PlayerId, penalisedPlayerId, StringComparison.Ordinal))
                {
                    Toaster.Add("You have been penalised.", MatToastType.Warning, "Penalty");
                }
                else
                {
                    Toaster.Add($"{penalisedPlayerName} has been penalised.", MatToastType.Warning, "Penalty");
                }

                StateHasChanged();
            });
        });

        _hubConnection.On(Const.RematchStarted, async () =>
        {
            await InvokeAsync(() => NavigationManager.NavigateTo($"/board/{GameId}", true));
        });

        await _hubConnection.StartAsync();
        if (!string.IsNullOrEmpty(GameId))
        {
            await _hubConnection.InvokeAsync(Const.JoinGameGroup, GameId);
        }
        await LoadOwnerFromSessionAsync();

        if (LocalPlayer is null || GameId is null)
        {
            _myCards = [];
            return;
        }

        _players = [.. GameService.GetPlayers(GameId)];
        

        if (!GameService.IsGameRunning(GameId) && GameService.IsGameCreator(GameId, LocalPlayer.PlayerId))
        {
            GameService.StartGame(GameId);
        }

        _myCards = GameService.GetPlayerCards(GameId, LocalPlayer.PlayerId);
        if (GameService.IsGameRunning(GameId))
        {
            _faceUp = GameService.GetFaceUp(GameId);
        }

        _requireSuit = !string.IsNullOrEmpty(GameService.GetRequiredSuit(GameId));
        _turn = _players[GameService.GetTurn(GameId)];
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
            ProtectedBrowserStorageResult<Player> result = await SessionStore.GetAsync<Player>(Const.LocalPlayerKey);
            LocalPlayer = result.Value;
            if (LocalPlayer != null)
            {
                StateHasChanged(); // Force re-render to update UI
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OkClick()
    {
        _suit = _dialogSuit;
        _dialogIsOpen = false;
        if (_suit is null || GameId is null) return;
        GameService.ReceiveSuitSelection(GameId, _suit);
        _requireSuit = !string.IsNullOrEmpty(GameService.GetRequiredSuit(GameId));
    }

    private string GetPlayerName(Player player)
    {
        if (LocalPlayer != null)
        {
            return player.PlayerId == LocalPlayer.PlayerId ? "You" : player.Name;
        }

        return "";
    }

    private bool IsMyTurn()
    {
        if (LocalPlayer != null && _turn != null)
            return _turn.PlayerId == LocalPlayer.PlayerId;
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
        if (GameId != null)
        {
            await GameService.ProgressGame(GameId, _myCards[_choice]); 
        }
    }

    private async void Pick()
    {
        if (GameId != null)
        {
            await GameService.ProgressGame(GameId, null); 
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync(); 
        }
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
        if (GameId is null) return;

        Player? player = _players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player is null || player.HasCalledOutThisTurn) return;

        GameService.PenalisePlayer(GameId, playerId, player.Name);
    }

    private void AnnounceCardCount()
    {
        UpdateHasCalledOut(true);
        if (LocalPlayer is null || GameId is null) return;
        GameService.CallOut(GameId, LocalPlayer.PlayerId, LocalPlayer.Name, _myCards?.Length ?? 0);
    }

    private void Rematch()
    {
        if (!_clickedRematch && GameId != null)
        {
            GameService.Rematch(GameId);
        }
        _clickedRematch = true;
    }

    private void LeaveGame()
    {
        if (LocalPlayer is null || GameId is null || _hubConnection is null) return;
        _hubConnection.InvokeAsync(Const.LeaveGameGroup, GameId);
        _players.RemoveAll(p => p.PlayerId == LocalPlayer.PlayerId);
        // Navigate to the home page
        NavigationManager.NavigateTo("/");
    }

    private void UpdateHasCalledOut(bool value)
    {
        if (LocalPlayer is null) return;
        int index = _players.FindIndex(p => p.PlayerId == LocalPlayer.PlayerId);
        if (index < 0)
        {
            throw new InvalidOperationException("Owner is not in the game!");
        }
        _players[index].HasCalledOutThisTurn = value;
        LocalPlayer.HasCalledOutThisTurn = value;
    }
}