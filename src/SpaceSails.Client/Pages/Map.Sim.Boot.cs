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

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — the page's life: the first render, the phase yields, the warm-ups, and #737's abandoned boot.
public partial class Map
{

    // #161 · WHERE THE BOOT'S SECONDS ACTUALLY GO, SAID OUT LOUD.
    //
    // The issue's first instruction is "profile first", and every earlier attempt at this boot argued from
    // the shape of the code instead of from a number. So the boot times ITSELF, on every run, in the one
    // place that can see both the browser's clock and the stage boundaries — and prints one line per stage
    // to the console the same way the berth roster does (#726). That is #161's "boot phases visible", and
    // it is what the before/after table in the PR body is read off: nothing to arm, nothing to remember.
    //
    // STATIC, and that is not an accident. Every instance field of this page is swept by
    // EveryFrameLeavesTheSameFingerprintTests (the 793-field roster) and diffed against a virgin component
    // by TheBootBuildsTheSameWorldTests; a stopwatch field would join both ledgers and say nothing about
    // the game. A static is invisible to both sweeps (`!f.IsStatic`, `BindingFlags.Instance`) — and one
    // clock is the honest model anyway, because one page boots at a time.
    private static readonly System.Diagnostics.Stopwatch BootClock = new();

    /// <summary>Milliseconds on <see cref="BootClock"/> at the last phase line — so each line can quote
    /// what ITS OWN stage cost as well as the running total.</summary>
    private static long _bootClockMark;

    /// <summary>#161 · Start the boot clock. Called once, at the top of the boot.</summary>
    private static void StartTheBootClock()
    {
        BootClock.Restart();
        _bootClockMark = 0;
    }

    /// <summary>#161 · Say what the stage that JUST FINISHED cost, and what the boot has cost so far. Kept
    /// to the <c>[SpaceSails]</c> prefix the berth roster already uses, so one grep gathers the boot.
    ///
    /// <para>The name is always the work BEHIND the call, never the work ahead of it — a phase line that
    /// named the next stage would attribute every stage's cost to its successor, which is exactly the kind
    /// of number that sends a perf lane after the wrong file.</para></summary>
    private static void SayTheBootStageCost(string stageJustFinished)
    {
        long now = BootClock.ElapsedMilliseconds;
        Console.WriteLine($"[SpaceSails] boot · {stageJustFinished} — {now - _bootClockMark} ms (t+{now} ms)");
        _bootClockMark = now;
    }

    // #318 false-hang follow-up: announce a coarse boot phase and hand the frame back to the browser so
    // the queued render actually paints before the next synchronous planning block. Task.Delay(1) (vs a
    // bare Task.Yield) reliably parks on a browser timer, giving the compositor a chance to flush the
    // loading door — the animated ⚙ gear keeps turning on its own (CSS, compositor thread), the phase
    // text updates, and the tab never reads as a dead freeze even when the block runs long on Debug WASM.
    private async Task BootPhaseAsync(string phase, CancellationToken abandoned)
    {
        _bootPhase = phase;
        StateHasChanged();
        await Task.Delay(1, abandoned);
    }

    // #737 · THE PLAYER MAY LEAVE WHILE THE WORLD IS STILL BEING BUILT. Boot pegs the main thread for tens
    // of seconds, so backing out of a slow load is the ordinary case rather than the corner — and the boot
    // below is a long chain of awaits whose continuations used to resume into a component the router had
    // already torn down, ending at InitCanvas / StartLoop / FocusAsync, every one of which names DOM that
    // has left the page. renderer.js throws outright on a missing canvas ("no canvas element with id …"),
    // the exception escaped OnAfterRenderAsync, and the renderer logged it as an unhandled render
    // exception. Dispose cancels this; every yield point in the boot carries the token, so an abandoned
    // boot stops at the first await it reaches instead of finishing into a page that is gone.
    private readonly CancellationTokenSource _bootAbandoned = new();

    /// <summary>#737 · Cancelled the moment this component is disposed — the boot's own "the player left".</summary>
    internal CancellationToken BootAbandonedToken => _bootAbandoned.Token;

    /// <summary>#737 · True once <c>StartLoop</c> has actually run for this component. <c>_started</c> only
    /// means the boot BEGAN: a boot abandoned before the renderer stage has no rAF loop to stop, and no
    /// canvas left to name in the stopping.</summary>
    private bool _renderLoopRunning;

    /// <summary>#737 · The running boot. The renderer fires <c>OnAfterRenderAsync</c> and keeps only an
    /// error handler on the task it returns, so this is the one handle a guard can await to watch the
    /// continuation that used to outlive the page.</summary>
    internal Task? Boot { get; private set; }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        // #997 · The "bring the open step into view" pass that used to run here on EVERY paint is now the
        // CappedScrollPanel's own: the panel is the thing with the scroll in it, so the panel is where the
        // after-render comparison belongs. #992's rule survives the move unchanged — ask whether WHICH
        // editor is open has changed since the last paint, rather than setting a flag at each of the five
        // places an editor can be opened from and forgetting it at the fifth.
        if (!firstRender || _started)
        {
            return Task.CompletedTask;
        }

        _started = true;
        return Boot = BootWithinTheLifeOfThePageAsync();
    }

    private async Task BootWithinTheLifeOfThePageAsync()
    {
        try
        {
            await BootTheWorldAsync(_bootAbandoned.Token);
        }
        catch (OperationCanceledException) when (_bootAbandoned.IsCancellationRequested)
        {
            // #737: the player navigated away mid-boot. Half a world and no page to put it on — there is
            // nothing to unwind (every field belongs to this instance, which the router has discarded) and
            // nothing to report. Letting this escape is exactly what raised WebAssemblyRenderer[100].
        }
    }

    // #371 Phase 1 (perf) · register (decode) the ship's room-backdrop art up front. Idempotent and cheap
    // (RegisterImage just fires the JS decode and caches by id), so this only ever moves the decode earlier.
    private void PredecodeDeckArt()
    {
        if (_renderer is null)
        {
            return;
        }
        try
        {
            foreach (DeckPlan.Backdrop bd in DeckPlan.Ship.Backdrops)
            {
                _renderer.RegisterImage(bd.Url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"backdrop pre-decode skipped: {ex}");
        }
    }

    // #371 Phase 1 (perf) · pay the first, cold-interpreted surface DRAW once at boot, invisibly, so the
    // live rAF loop never has to (the same #358/#348 idiom the descent uses, pointed at game start). We
    // build a THROWAWAY surface plan (Miranda's) into a local — never assigned to _deckPlan, never touching
    // _surface/_avatar/game state — and, only while the start-picker backdrop covers the canvas, paint it
    // ONCE to tier up DeckView.Draw + its text JSON. The picker cover is the never-flash guard: if a start
    // cheat skipped the picker (the canvas is live), we skip the paint and let the build alone warm the
    // heavy SurfaceLayout.For / array paths. Yield-fronted and try/caught — a warm-up is a nicety that can
    // only help; if anything is not ready, the live loop simply pays the frame as before.
    private async Task WarmSurfaceDrawPathAtBootAsync()
    {
        // Idle-time: let the boot settle and the first real frames land before we do throwaway work.
        await Task.Yield();
        await Task.Delay(250);

        // A quarter of a second is plenty of time for the player to leave; a throwaway paint into a canvas
        // that has gone with them is worth nothing and costs a JS throw (#737).
        if (_deckView is null || _renderer is null || _bootAbandoned.IsCancellationRequested)
        {
            return;
        }
        try
        {
            DeckPlan warm = BootWarmUpPlan();

            if (_showStartPicker && _viewportWidth > 0 && _viewportHeight > 0)
            {
                var hud = new DeckView.SurfaceHud(
                    DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
                    Blips: System.Array.Empty<(double, double, bool)>(), Cadence: 0, Readout: "",
                    CacheMarks: System.Array.Empty<(double, double, bool)>(),
                    Nerve: NerveModel.Steady, NerveReadout: "");
                _deckView.Draw(
                    warm, _viewportWidth, _viewportHeight, SimTime,
                    new DeckView.State(
                        MoonSurface.SpawnX, MoonSurface.SpawnY, 0, 0, 0,
                        ShuttleAway: false, ElectricUniverse: false),
                    0, 0, hud);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"boot surface warm-up skipped: {ex}");
        }
    }

    /// <summary>
    /// The THROWAWAY plan the boot warm-up paints once — Miranda's ground, an empty own-cache set, and a
    /// deck with NOBODY ON IT. The memoized layout it builds also warms the (shared) SurfaceDeck cache for a
    /// first landing on Miranda.
    ///
    /// <para><b>#962 · Nought figures, and it must SAY nought.</b> This used to claim
    /// <c>DeckPlan.Ship.DroidCount</c> figures while handing in a no-op fill, which is a plan lying about
    /// its own buffer: <c>DeckView.DrawTheFigures</c> walks <c>DroidCount</c> entries, so it read three
    /// default <see cref="DeckPlan.Droid"/> structs whose <c>Name</c> is null and died in
    /// <c>IsSweeper(null)</c> with a <see cref="NullReferenceException"/>. The warm-up's own try/catch ate
    /// it, so nothing was ever seen on screen — but the paint it exists for never happened either, and the
    /// #371 Phase-1 cold-draw cost the player was supposed to stop paying went right back on the first live
    /// frame, every boot, silently. A count and a fill are one statement; this is the honest one.</para>
    ///
    /// <para>Internal so the regression guard can build the very expression the boot builds, rather than a
    /// re-typed look-alike that could go on passing after this one changed.</para>
    /// </summary>
    internal static DeckPlan BootWarmUpPlan() =>
        MoonSurface.SurfaceDeck(
            "miranda", "Miranda",
            System.Array.Empty<(string, double, double, int)>(),
            droidCount: 0, static (_, _) => { });

    private ShipState InitializeShipState()
    {
        double h = 1.0;
        Vector2d p1 = _ephemeris!.Position("earth", -h);
        Vector2d p2 = _ephemeris!.Position("earth", h);
        Vector2d initialVelocity = (p2 - p1) / (2 * h);

        // Start well clear of Earth's gravity well (~5e9 m radially outward, ~0.03 AU) so the ship is
        // effectively in Earth's heliocentric orbit and its ±10% pulses steer that solar orbit —
        // rather than dropped just above Earth's surface, where Earth's gravity dominates and the ship
        // simply falls in. Velocity stays Earth's, so it starts co-moving.
        Vector2d earthPosition = _ephemeris.Position("earth", 0);
        Vector2d initialPosition = earthPosition + earthPosition.Normalized() * 5e9;
        return new ShipState(initialPosition, initialVelocity, 0);
    }

    public void Dispose()
    {
        // #737 · First, before anything else: tell a boot that is still running that its page is gone. The
        // CTS is deliberately NOT disposed — a continuation parked on one of the boot's awaits is still
        // holding this token and will read it as it resumes; a disposed source would answer that read with
        // an ObjectDisposedException, which is the very shape of failure this cancellation exists to end.
        _bootAbandoned.Cancel();

        RendererInterop.FrameTick -= OnTick;
        RendererInterop.CanvasResized -= OnCanvasResized;

        // Only a loop that was actually started can be stopped (#737): _started means the boot BEGAN, and
        // a boot abandoned before the renderer stage never registered this canvas with renderer.js.
        if (_renderLoopRunning)
        {
            RendererInterop.StopLoop(CanvasId);
        }
    }
}
