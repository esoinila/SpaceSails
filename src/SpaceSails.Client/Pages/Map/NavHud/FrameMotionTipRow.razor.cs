using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// FrameMotionTipRow — the code-behind for FrameMotionTipRow.razor (#242).
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Two parameters and neither is a state: the page is asked whether the line is warranted this instant, and
// handed the one verb that answers it. Nothing is remembered, which is why this row moved no field of the
// frame fingerprint's sweep.
public partial class FrameMotionTipRow
{
    /// <summary>The line and its working, or null when the frame is not hiding anything the captain is
    /// trying to do — the whole judgement is <see cref="FrameMotionTip.ShouldShow"/>'s, in Core.</summary>
    [Parameter] public Func<(string Line, string Readout)?> FrameMotionTipLine { get; set; } = default!;

    /// <summary>The page's own frame verb — the SAME one the frame chips and the picker call, so the chip
    /// cannot switch the plan into a frame the rest of the HUD is not reading (#144's one-frame law).</summary>
    [Parameter] public Action<string?> SetPlotFrame { get; set; } = default!;

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
