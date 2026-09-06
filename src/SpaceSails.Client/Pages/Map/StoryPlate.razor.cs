using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// StoryPlate — the code-behind for StoryPlate.razor.
//
// #1107 · A COMPONENT'S MEMBERS LIVE IN A .cs FILE, because the razor generator's output is NOT
// ANALYSED. Every line that lives inside an `@code { … }` block is invisible to the .NET analysers this
// repo runs with `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The
// moment the same characters sit in a `.cs` file beside the component, the analysers see them.
public partial class StoryPlate
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public Action CloseStoryPlate { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<StoryBeats.Beat, string?, (string Title, string Art, string Caption)> StoryBeatCopy { get; set; } = default!;

    /// <summary>bound by the `@if (_storyPlate is { } flash)` that stays in the page — the tuple the
    /// page's own field carries, under the name the `@if` gave it. Its third member (UntilSimTime) is the
    /// page's business: OnTick takes the plate down, not this card.</summary>
    [Parameter] public (StoryBeats.Beat Beat, string? Subject, double UntilSimTime) flash { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
