using Microsoft.AspNetCore.Components;
using SatchelPage = SpaceSails.Client.Pages.Map.SatchelPage;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// SatchelTabs — the code-behind for SatchelTabs.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of SatchelPanel under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable the panel's `@if` binds
// arrives under the name that binding gave it. That is the whole trick: it is what let the markup move
// out of SatchelPanel.razor without a single character of it changing.
public partial class SatchelTabs
{
    [Parameter] public List<Core.Satchel.Item> _satchel { get; set; } = default!;
    /// <summary>the page's `SatchelPage _satchelPage` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_satchelPage` and the assignment still lands on the page.</summary>
    [Parameter] public SatchelPage _satchelPageValue { get; set; } = default!;
    /// <summary>The page's `_satchelPage = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<SatchelPage> _satchelPageSet { get; set; } = default!;
    private SatchelPage _satchelPage { get => _satchelPageValue; set { _satchelPageValue = value; _satchelPageSet(value); } }
    [Parameter] public Func<RipAndBin.Bin?> BinWithinReach { get; set; } = default!;
    [Parameter] public bool CaptainIsSeatedAnywhere { get; set; }
    [Parameter] public Func<IReadOnlyList<CompassLine>> CompassOnFoot { get; set; } = default!;
    [Parameter] public string SpreadDoorHint { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.CaseSubjects.SubjectThread>> TheBookThreads { get; set; } = default!;
    [Parameter] public Action<SatchelPage> TheSatchelTurnsTo { get; set; } = default!;

    // SatchelPanel's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
