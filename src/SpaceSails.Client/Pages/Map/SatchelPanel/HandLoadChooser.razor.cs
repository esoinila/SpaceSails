using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// HandLoadChooser — the code-behind for HandLoadChooser.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of SatchelPanel under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable the panel's `@if` binds
// arrives under the name that binding gave it. That is the whole trick: it is what let the markup move
// out of SatchelPanel.razor without a single character of it changing.
public partial class HandLoadChooser
{
    /// <summary>the panel's `@if (TheLoadChooser() is { } loading)` — the chooser the captain opened,
    /// under the name that pattern gave it.</summary>
    [Parameter] public (Core.Satchel.Item Rounds, int Pocket, string KindName, List<SentryHandLoad.Pick> Picks) loading { get; set; }
    [Parameter] public EventCallback CloseTheLoadChooser { get; set; }
    [Parameter] public Action<SentryHandLoad.Pick> LoadIntoIt { get; set; } = default!;
    [Parameter] public Action<SentryHandLoad.Pick, int> NudgeShare { get; set; } = default!;

    // SatchelPanel's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
