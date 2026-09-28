using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ParrotAndDiceRack — the code-behind for ParrotAndDiceRack.razor.
//
// #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME, typed as the
// card it is handed to already declares it — which is what let the markup above move out of Map.razor
// without a single character of it changing.
public partial class ParrotAndDiceRack
{
    [Parameter] public string? _parrotSquawk { get; set; }
    [Parameter] public DiceEvent? _diceTrayEvent { get; set; }
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Action DismissDiceTray { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
