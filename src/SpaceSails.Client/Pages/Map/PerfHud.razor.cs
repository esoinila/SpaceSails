using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// PerfHud — the code-behind for PerfHud.razor.
//
// #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME, typed as the
// card it is handed to already declares it — which is what let the markup above move out of Map.razor
// without a single character of it changing.
public partial class PerfHud
{
    [Parameter] public DeckView? _deckView { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
