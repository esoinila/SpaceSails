using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1135 · THE PRESS PAINTS ON ITS OWN TICK — the swallowed re-render, driven rather than read.
///
/// <para><see cref="NoSurfaceSwallowsARerenderTests"/> is a source law: it reads the parameter's type and the
/// page's method and says what OUGHT to follow from them. This file presses the buttons and looks at the
/// screen, because the whole class of bug is one where every source guard in the repo stays green — the write
/// lands, the field is right, and the glass shows the old word. #1134 described the symptom exactly: the
/// desk's <c>Start sweep</c> would have kept saying <c>Start sweep</c> for up to 200 ms after it had already
/// started sweeping.</para>
///
/// <para><b>The one thing that makes this test able to fail.</b> <see cref="DeskBench.RenderAsync"/> calls
/// <c>StateHasChanged</c> on every visit after the first — which is precisely what Map's 200 ms HUD throttle
/// does. A law that pressed a button and then called <c>RenderAsync</c> would be reading the TICK'S paint and
/// would pass on a control that swallowed the re-render completely. So every assertion below reads
/// <see cref="DeskBench.CurrentPaint"/>, which asks the page for nothing and hands back the frames the
/// renderer is holding — the screen as it stands in the ~200 ms gap.</para>
///
/// <para><b>What driving it found out, and it is worth knowing.</b> The first red proof tried was #1134's
/// own before-state — <c>ScopeControls.StartSweep</c> put back to <c>[Parameter] public Action</c> — and
/// <see cref="TheSweepButtonSaysTheNewWordOnThePress"/> <b>stayed green</b>. So did passing it down as
/// <c>StartSweep="@(() =&gt; StartSweep())"</c>. The renderer's own batch says why: the desk (component 11)
/// repainted on the press in both. <c>EventCallbackFactory</c> resolves the receiver as
/// <c>callback.Target as IHandleEvent ?? receiver as IHandleEvent</c>, and a page method group — or a page
/// lambda that captures only <c>this</c>, which C# emits as an instance method — has the PAGE for a target.
/// The surface is only ever the FALLBACK, and it is reached exactly when the delegate is a closure over a
/// local, which is what every per-row <c>@onclick="() =&gt; …(entry.Id)"</c> is. That is why this file's two
/// tests are the two shapes they are, and why the fix for the second one is on the page.</para>
///
/// <para><b>Proven RED:</b>
/// <list type="bullet">
/// <item><see cref="TheSweepButtonSaysTheNewWordOnThePress"/> — give <c>TrackingPost.razor</c> Map's own
/// <c>@implements IHandleEvent</c> pass-through, i.e. take away the desk's automatic re-render. The press
/// leaves the button saying <c>Start sweep</c>. (<c>NoSurfaceSwallowsARerenderTests</c> clause 1 reddens on
/// the same plant, from the source side.)</item>
/// <item><see cref="ThePrioritizeButtonRepaintsTheDeskAndNotOnlyItsOwnSurface"/> — take the
/// <c>StateHasChanged()</c> back out of <c>TrackingPost.PrioritizeSearch</c>:
/// <i>"Assert.Contains() Failure — Collection: [18], Not found: 11"</i>. Eighteen is
/// <c>LostLockBoard</c>, the surface that owns the button; eleven is the desk that owns the queue the press
/// reordered.</item>
/// </list></para>
/// </summary>
[SlowGate] // #1135 · 11 s over 3 test(s), measured 2026-09-06; see TheSlowGateRosterTests.
public sealed class ThePressPaintsOnItsOwnTickTests
{
    /// <summary>One of <c>EveryDeskBootsTests</c>'s own worlds — free-flying beside the derelict, which is
    /// the cheapest of the five and needs no berth. The desk under test is the same in all of them.</summary>
    private const string TheWorld = "/map?start=wreck";

    private static readonly BindingFlags Hidden = TestTree.PrivateOnAnInstance;

    /// <summary>
    /// #1134's case, driven. <c>Start sweep</c> lives on <c>ScopeControls</c>, a surface; the state it
    /// changes (<c>_activeJob</c>) lives on <c>TrackingPost</c>, the desk; and the word on the button is
    /// drawn from that state, across the boundary, in both directions. Press it and the word must have
    /// changed before anything else touches the page — this is the control #1135 was filed about, and
    /// nothing had ever pressed it.
    /// </summary>
    [Fact]
    public async Task TheSweepButtonSaysTheNewWordOnThePress()
    {
        using DeskBench bench = await DeskBench.BootAsync(TheWorld);
        await bench.SwitchAsync(ShipDesk.Sensors);
        await bench.RenderAsync();

        // The scope draws exactly one of the two words, on the desk's own `@if (_activeJob is null)`. M27's
        // passive watch holds the instrument by default, so the desk is stood down first — the subject here
        // is the manual button, and a law that quietly pressed the other one would be green on the bug.
        (TrackingPost desk, _) = bench.Onscreen<TrackingPost>();
        Set(desk, "_passiveWatch", false);
        Set(desk, "_activeJob", null);

        DeskBench.Painted idle = await bench.RenderAsync();
        Assert.DoesNotContain(idle.Root.SelfAndDescendants(), n => n.Name == "Stop sweep");

        // ── The press, and the word ──────────────────────────────────────────────────────────────────
        await bench.PressAsync(TheButtonSaying(idle, "Start sweep").Handlers["onclick"]);

        Assert.True(Field(desk, "_activeJob") is not null,
            "pressing `Start sweep` did not start a sweep at all — this law cannot say anything about the "
            + "paint until the thing it is meant to paint has happened.");

        DeskBench.Painted sweeping = bench.CurrentPaint();
        Assert.True(
            sweeping.Root.SelfAndDescendants().Any(n => n.Name == "Stop sweep"),
            "#1135 · the sweep is running (_activeJob is set) and the button still says `Start sweep` — the "
            + "desk was not the receiver of the press, so nothing re-rendered it, and the control is lying "
            + "about what it just did until the HUD's 200 ms tick comes round.");

        // ── And back, off the tree the press itself drew ─────────────────────────────────────────────
        await bench.PressAsync(TheButtonSaying(sweeping, "Stop sweep").Handlers["onclick"]);

        Assert.Null(Field(desk, "_activeJob"));
        Assert.True(
            bench.CurrentPaint().Root.SelfAndDescendants().Any(n => n.Name == "Start sweep"),
            "#1135 · the sweep has been aborted and the button still says `Stop sweep`.");
    }

    /// <summary>
    /// The other half, and the one a type change cannot reach: PRIORITIZE REDISCOVERY is an <c>@onclick</c>
    /// LAMBDA on <c>LostLockBoard</c>, so the receiver is that surface however the parameter is declared —
    /// while what the press reorders is the telescope QUEUE, drawn by <c>SensorTaskQueue</c> next door. The
    /// only thing that can put the sibling right is the desk repainting itself, so that is what is asserted:
    /// the batch the press produced contains the DESK and not merely the surface that owns the button.
    /// </summary>
    [Fact]
    public async Task ThePrioritizeButtonRepaintsTheDeskAndNotOnlyItsOwnSurface()
    {
        using DeskBench bench = await DeskBench.BootAsync(TheWorld);
        await bench.SwitchAsync(ShipDesk.Sensors);
        await bench.RenderAsync();

        (TrackingPost desk, int deskId) = bench.Onscreen<TrackingPost>();

        // A cold case on the board, filed through the shipping ledger's own door: the button under test is
        // drawn per lost track, so without one there is nothing on screen to press.
        var lost = (LostTrackLedger)Field(desk, "_lostTracks")!;
        var seen = new Observation("kestrel", 0, new Vector2d(1e9, 0), new Vector2d(0, 3e3));
        lost.AddFrom(new TrackedTarget("kestrel", seen, 0, 0.5), 0);

        DeskBench.Painted board = await bench.RenderAsync();
        DeskBench.Painted.Node prioritize = TheButtonSaying(board, "⏫ PRIORITIZE REDISCOVERY");

        bench.ForgetRepaints();
        await bench.PressAsync(prioritize.Handlers["onclick"]);

        Assert.Contains(deskId, bench.Repainted);
    }

    /// <summary>
    /// #1135 · The bench this law is stated through can tell pass from fail. <see cref="DeskBench.CurrentPaint"/>
    /// is the whole point of the file, so it is worth proving that it does NOT quietly re-render: a field
    /// changed behind the page's back must be invisible to it and visible to <c>RenderAsync</c>. A
    /// CurrentPaint that secretly painted would make both tests above pass on any code at all.
    /// </summary>
    [Fact]
    public async Task THE_DRIVEN_PROOF_CanTellPassFromFail()
    {
        using DeskBench bench = await DeskBench.BootAsync(TheWorld);
        await bench.SwitchAsync(ShipDesk.Sensors);
        await bench.RenderAsync();

        (TrackingPost desk, int deskId) = bench.Onscreen<TrackingPost>();
        Assert.True(deskId > 0);

        // File a cold case behind the page's back. Nothing has been told to paint, so the board that draws
        // one must not be on the screen…
        var lost = (LostTrackLedger)Field(desk, "_lostTracks")!;
        lost.AddFrom(
            new TrackedTarget("kestrel", new Observation("kestrel", 0, new Vector2d(1e9, 0), new Vector2d(0, 3e3)), 0, 0.5),
            0);

        Assert.DoesNotContain(bench.CurrentPaint().Root.SelfAndDescendants(), n => n.HasClass("sensor-lost-box"));

        // …and the tick's own paint finds it at once, so the two halves of the bench really are two
        // different questions and CurrentPaint is not quietly rendering behind the law's back.
        Assert.Contains((await bench.RenderAsync()).Root.SelfAndDescendants(), n => n.HasClass("sensor-lost-box"));
    }

    private static DeskBench.Painted.Node TheButtonSaying(DeskBench.Painted painted, string words) =>
        painted.Root.SelfAndDescendants().FirstOrDefault(
            n => n.Handlers.ContainsKey("onclick") && n.Name == words)
        ?? throw new InvalidOperationException(
            $"the sensors desk drew no control saying `{words}` — this law presses the button the tree "
            + "actually drew, and a law that cannot find its own button proves nothing.");

    private static object? Field(TrackingPost desk, string name) => TheField(name).GetValue(desk);

    private static void Set(TrackingPost desk, string name, object? value) => TheField(name).SetValue(desk, value);

    private static FieldInfo TheField(string name) =>
        typeof(TrackingPost).GetField(name, Hidden)
        ?? throw new InvalidOperationException(
            $"TrackingPost has no field {name} — this law reads the shipping desk by name, and that name "
            + "has moved.");
}
