using Crazy8Web.Constants;
using Crazy8.Models;
using Crazy8Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Crazy8Web.Pages;

public partial class JoinPage : ComponentBase
{
    [Parameter] public required string PlayerId { get; set; }
    [Inject] private ProtectedSessionStorage SessionStore { get; set; }
    [Inject] private GameService GameService { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }

    private static string? _joiningId;
    private Player? Owner { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _joiningId = string.Empty;
        await LoadOwnerFromSessionAsync();
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


    private void JoinGame()
    {
        if (Owner == null || string.IsNullOrEmpty(_joiningId)) return;
        GameService.JoinGame(Owner, _joiningId);
        NavigationManager.NavigateTo($"lobby/{GameService.GetGameId()}");
    }
}