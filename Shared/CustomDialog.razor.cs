using Microsoft.AspNetCore.Components;

namespace Crazy8Web.Shared;

public partial class CustomDialog : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public string Title { get; set; } = "Dialog";
    [Parameter] public string ConfirmText { get; set; } = "OK";
    [Parameter] public string CancelText { get; set; } = "Cancel";
    [Parameter] public bool ShowCancel { get; set; } = true;
    [Parameter] public bool ShowConfirm { get; set; } = true;
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }

    private async Task CloseDialog()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(IsOpen);
        await OnCancel.InvokeAsync();
    }

    private async Task ConfirmDialog()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(IsOpen);
        await OnConfirm.InvokeAsync();
    }

    private async Task HandleBackdropClick()
    {
        if (ShowCancel)
        {
            await CloseDialog();
        }
    }
}