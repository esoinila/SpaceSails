using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// BinSleeve — the code-behind for BinSleeve.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of SatchelPanel under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable the panel's `@if` binds
// arrives under the name that binding gave it. That is the whole trick: it is what let the markup move
// out of SatchelPanel.razor without a single character of it changing.
public partial class BinSleeve
{
    [Parameter] public string? _satchelOutcome { get; set; }
    [Parameter] public Func<List<Core.Satchel.Item>> BinnableFinds { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> BinRowFlag { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> BinRowHint { get; set; } = default!;
    [Parameter] public Func<RipAndBin.Bin?> BinWithinReach { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> RipItUp { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SatchelLabel { get; set; } = default!;

    // SatchelPanel's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
