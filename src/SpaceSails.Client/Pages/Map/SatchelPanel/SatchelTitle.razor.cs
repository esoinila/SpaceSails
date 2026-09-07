using Microsoft.AspNetCore.Components;
using SatchelPage = SpaceSails.Client.Pages.Map.SatchelPage;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// SatchelTitle — the code-behind for SatchelTitle.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of SatchelPanel under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable the panel's `@if` binds
// arrives under the name that binding gave it. That is the whole trick: it is what let the markup move
// out of SatchelPanel.razor without a single character of it changing.
public partial class SatchelTitle
{
    [Parameter] public SatchelPage _satchelPage { get; set; }
    [Parameter] public Func<RipAndBin.Bin?> BinWithinReach { get; set; } = default!;

    // SatchelPanel's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
