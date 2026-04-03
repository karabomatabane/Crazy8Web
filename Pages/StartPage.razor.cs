using Crazy8.Models;
using Crazy8Web.Constants;
using Crazy8Web.Data.Entities;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Crazy8Web.Pages;

public partial class StartPage : ComponentBase, IDisposable
{
    [Inject] private GameService GameService { get; set; } = null!;
    [Inject] private HttpClient Http { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [Inject] private ProtectedSessionStorage SessionStore { get; set; } = null!;
    [Inject] protected IMatToaster Toaster { get; set; } = null!;

    [SupplyParameterFromForm]
    public AuthModel Model { get; set; } = null!;

    [SupplyParameterFromQuery]
    public int? LoginError { get; set; }

    [SupplyParameterFromQuery]
    public int? RegisterError { get; set; }

    [SupplyParameterFromQuery(Name = "registerUsername")]
    public string? RegisterUsername { get; set; }

    [SupplyParameterFromQuery(Name = "registerEmail")]
    public string? RegisterEmail { get; set; }

    private EditContext? editContext;
    private ValidationMessageStore? messageStore;
    private Player? LocalPlayer { get; set; }
    private static string? _inputName;
    private bool _canSubmit;
    private bool _showRegisterValidation;

    private bool _isRegisterMode;


    protected override async Task OnInitializedAsync()
    {
        _inputName = string.Empty;
        Model ??= new();
        editContext = new(Model);
        messageStore = new(editContext);

        editContext.OnFieldChanged += HandleFieldChanged;

        await LoadOwnerFromSessionAsync();
    }

    protected override void OnParametersSet()
    {
        if (RegisterError.HasValue)
        {
            _isRegisterMode = true;
        }
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        _canSubmit = editContext?.Validate() == true;
        StateHasChanged();
    }

    private bool ShouldShow<T>(Expression<Func<T>> accessor)
    {
        var field = FieldIdentifier.Create(accessor);
        return _showRegisterValidation || (editContext?.IsModified(field) ?? false);
    }

    private async Task LoadOwnerFromSessionAsync()
    {
        try
        {
            ProtectedBrowserStorageResult<Player> result = await SessionStore
                .GetAsync<Player>(Const.LocalPlayerKey);
            LocalPlayer = result.Value;
            if (LocalPlayer is not null)
            {
                StateHasChanged(); // Force re-render to update UI
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void ToggleMode()
    {
        _isRegisterMode = !_isRegisterMode;
    }

    private void PrepareToJoin()
    {
        // TODO: Use game id to add player to a game
        if (LocalPlayer is null) return;
        NavigationManager.NavigateTo($"/join/{LocalPlayer.PlayerId}");
    }


    private void CreateGame()
    {
        if (LocalPlayer is null) return;
        NavigationManager.NavigateTo($"lobby/{GameService.CreateGame(LocalPlayer)}");
    }

    private async Task CreatePlayer()
    {
        if (string.IsNullOrWhiteSpace(_inputName)) return;

        string name = _inputName.Trim();

        if (LocalPlayer is null)
        {
            LocalPlayer = new Player(name);
        }
        else
        {
            LocalPlayer.Name = name;
        }

        await SessionStore.SetAsync(Const.LocalPlayerKey, LocalPlayer);
        StateHasChanged();
    }

    public void Dispose()
    {
        editContext?.OnFieldChanged -= HandleFieldChanged;
    }
}