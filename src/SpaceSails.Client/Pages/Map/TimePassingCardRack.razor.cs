using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// TimePassingCardRack — the code-behind for TimePassingCardRack.razor.
//
// #251 · the rack owns the three cards that stand in front of a long stretch of clock: the jump, the coast
// skip and the arrival brake. Its members live in a .cs file beside the component because the razor
// generator's output is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever
// shown.
public partial class TimePassingCardRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public string _brakeDestName { get; set; } = default!;
    [Parameter] public ArrivalBrake.Gate _brakeGate { get; set; } = default!;
    [Parameter] public bool _coastSkipActive { get; set; }
    [Parameter] public int _coastSkipDays { get; set; }
    [Parameter] public string _coastSkipLabel { get; set; } = default!;
    [Parameter] public bool _deckMode { get; set; }
    [Parameter] public bool _jumpActive { get; set; }
    [Parameter] public string _jumpDestName { get; set; } = default!;
    [Parameter] public string _jumpFlavor { get; set; } = default!;
    [Parameter] public int _jumpTotalYears { get; set; }
    [Parameter] public int _jumpYear { get; set; }
    /// <summary>the page's `_voidCardTucked = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _voidCardTuckedSet { get; set; } = default!;
    /// <summary>the page's `bool _voidCardTucked` — and this markup WRITES it, so it crosses as a pair: the value in, the page's own setter out.</summary>
    [Parameter] public bool _voidCardTuckedValue { get; set; }
    [Parameter] public bool _worldReady { get; set; }
    [Parameter] public Func<string> ArrivalBrakeAskText { get; set; } = default!;
    [Parameter] public bool BrakeIsAerobrake { get; set; }
    [Parameter] public EventCallback DeclineArrivalBrake { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public EventCallback FireArrivalBrake { get; set; } = default!;

    /// <summary>#251 · the member's OWN NAME, so the moved markup still reads and assigns `_voidCardTucked`
    /// and the assignment still lands on the page (NoSurfaceSwallowsAWriteTests).</summary>
    private bool _voidCardTucked { get => _voidCardTuckedValue; set { _voidCardTuckedValue = value; _voidCardTuckedSet(value); } }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
