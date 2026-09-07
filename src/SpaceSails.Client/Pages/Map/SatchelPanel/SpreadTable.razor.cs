using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// SpreadTable — the code-behind for SpreadTable.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of SatchelPanel under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable the panel's `@if` binds
// arrives under the name that binding gave it. That is the whole trick: it is what let the markup move
// out of SatchelPanel.razor without a single character of it changing.
public partial class SpreadTable
{
    [Parameter] public List<SpreadReconcile.Paper> _laid { get; set; } = default!;
    [Parameter] public SpreadReconcile.Result? _reconciled { get; set; }
    [Parameter] public string? _satchelOutcome { get; set; }
    [Parameter] public bool CaseHasBegun { get; set; }
    [Parameter] public EventCallback ClearTheSpread { get; set; }
    [Parameter] public Func<string, bool> IsLaid { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpreadReconcile.Paper>> LayablePapers { get; set; } = default!;
    [Parameter] public Action<SpreadReconcile.Paper> LayItOnTheSpread { get; set; } = default!;
    [Parameter] public Func<string?> ProcessingUnderway { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> RipHint { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, bool> RipIsOffered { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> RipItUp { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SatchelLabel { get; set; } = default!;
    [Parameter] public Func<List<Core.Satchel.Item>> SpreadableFinds { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, Task> SpreadDigClicked { get; set; } = default!;
    [Parameter] public string? SpreadRefusal { get; set; }
    [Parameter] public Func<Core.Satchel.Item, string> SpreadRowVerb { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> WriteUpHint { get; set; } = default!;

    // SatchelPanel's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
