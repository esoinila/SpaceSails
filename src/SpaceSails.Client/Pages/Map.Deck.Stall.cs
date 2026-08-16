using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Deck (#870 split; the header note lives in Map.Deck.cs) — #825's one staleness clock: how far behind the wall the sim is, the banner that says so, and the receipt a control gets when the machine is not handing out frames.
public partial class Map
{
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  #825 · THE STALL SAYS SO. Owner, 2026-08-11, on B1 near the goods car with a build eating the CPU:
    //  "why does the click to walk no longer work?"
    //
    //  It did work. The click was taken, the route was planned, and then the frame that should have walked
    //  it bought FrameGap.SpentPerFrameSeconds of legs on sixteen seconds of wall clock — 0.9 deck units
    //  out of the hundred and forty-four the captain asked for — and said nothing. The clamp is right (see
    //  FrameGap: a sixteen-second step would put a body through a bulkhead while the air, the nerve, the
    //  tracker and the Old Ones all missed it). The SILENCE was the bug.
    //
    //  ONE CLOCK. The banner across the top of the deck and the acknowledgement a control gets are the same
    //  fact asked twice, so they ask the same field through the same threshold. This is deliberately NOT
    //  the comms fiction: CommsLink's "SIGNAL BREAKING UP" is the mothership's scripted downlink episode on
    //  the excursion's own on-site clock, it has never gated a walk, and it answers a question nobody with
    //  a dead-feeling control was asking.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>#825 · How long the last SERVICED frame actually spanned, in real seconds — unclamped, off
    /// the browser's own high-resolution stamp. This is the machine's honesty, not the sim's clock.</summary>
    private double _frameGapSeconds;

    /// <summary>#825 · Wall-clock milliseconds at that frame. The frame gap alone is not enough: in a
    /// starved tab the pointer event and the animation callback are two queued jobs and the browser decides
    /// their order, so a click that lands BEFORE the catching-up frame would otherwise read a stale, healthy
    /// gap and be waved through silently — the very bug, one event earlier.</summary>
    private long _frameServicedAtMs;

    /// <summary>#825 · Has the hold already been said for this stall? A stall is one event, not one per
    /// press, and a captain hammering a dead-feeling control does not need the same sentence eight times.
    /// Cleared by the first frame that arrives on time.</summary>
    private bool _heldControlsSaid;

    /// <summary>#825 · THE ONE STALENESS CLOCK: how far behind the wall the sim is, right now, in real
    /// seconds. The larger of the last frame's own span and the time since it — the first names a gap that
    /// has already happened, the second names one that is still happening, and a control asked in the middle
    /// of a freeze has to be answered by the second.</summary>
    private double SimStalenessSeconds
    {
        get
        {
            double sinceServiced = _frameServicedAtMs <= 0
                ? 0.0
                : Math.Max(0.0, (Environment.TickCount64 - _frameServicedAtMs) / 1000.0);
            return Math.Max(_frameGapSeconds, sinceServiced);
        }
    }

    /// <summary>#825 · Are the controls being held by a machine that cannot hand out frames? Read by the HUD
    /// banner and by every input path that would otherwise be quietly swallowed — one question, one
    /// threshold (<see cref="FrameGap.StallSeconds"/>), so the picture and the verb cannot disagree about
    /// whether the world is live.</summary>
    private bool ControlsAreHeld => FrameGap.IsStalling(SimStalenessSeconds);

    /// <summary>#825 · The frame loop's one line into this: a frame was serviced, and it spanned this long.
    /// A frame that arrives on time also clears the hold notice, so the next stall is announced afresh.</summary>
    private void MarkFrameServiced(double dtRealSeconds)
    {
        _frameGapSeconds = Math.Max(0.0, dtRealSeconds);
        _frameServicedAtMs = Environment.TickCount64;
        if (!FrameGap.IsStalling(_frameGapSeconds))
        {
            _heldControlsSaid = false;
        }
    }

    /// <summary>#825 · The stall banner the deck paints, or empty. Off ONE clock, through ONE threshold —
    /// this method exists so the HUD and <see cref="AcknowledgeHeldControls"/> cannot come to hold two
    /// different opinions about whether the world is live.</summary>
    private string TheStallBanner() => FrameGap.StallBanner(SimStalenessSeconds);

    /// <summary>#825 · A control used inside the gap. The order is NOT dropped — a clicked route is a queued
    /// target that outlives the stall and is walked the moment frames return — so this is a receipt rather
    /// than a refusal, and it is said exactly once per stall.</summary>
    private void AcknowledgeHeldControls()
    {
        if (!ControlsAreHeld || _heldControlsSaid)
        {
            return;
        }
        _heldControlsSaid = true;
        ShowPulseMessage(FrameGap.HeldLine);
    }
}
