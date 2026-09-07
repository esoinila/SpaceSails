using Microsoft.AspNetCore.Components;

namespace SpaceSails.Client.Pages;

// LoudPlanAlarmCard — the code-behind for LoudPlanAlarmCard.razor.
//
// #1107 · A COMPONENT'S MEMBERS LIVE IN A .cs FILE, because the razor generator's output is NOT
// ANALYSED. Every line that lives inside an `@code { … }` block is invisible to the .NET analysers this
// repo runs with `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The
// moment the same characters sit in a `.cs` file beside the component, the analysers see them.
public partial class LoudPlanAlarmCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    /// <summary>wired STRAIGHT to `@onclick` on the backdrop and to the shell's OnClose, so it crosses as
    /// an EventCallback and the PAGE is the receiver of the press (#1135 clause 2).</summary>
    [Parameter] public EventCallback DismissLoudPlanAlarm { get; set; }
    [Parameter] public string LoudPlanAlarmHail { get; set; } = default!;
    [Parameter] public string LoudPlanAlarmQuote { get; set; } = default!;

    /// <summary>bound by the `@if (LoudPlanAlarm is { } _planBreak)` that stays in the page — the
    /// sentence saying what broke, under the name the `@if` gave it, underscore and all.</summary>
    [Parameter] public string _planBreak { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
