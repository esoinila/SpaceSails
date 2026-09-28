using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// BootDoors — the code-behind for BootDoors.razor.
//
// #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME, typed as the
// card it is handed to already declares it — which is what let the markup above move out of Map.razor
// without a single character of it changing.
public partial class BootDoors
{
    [Parameter] public bool _worldReady { get; set; }
    [Parameter] public bool _shuttleDescending { get; set; }
    [Parameter] public string? _bootPhase { get; set; }
    [Parameter] public string? _descentPhase { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
