// #251 item 1 · THE TRACKING POST'S CODE-BEHIND, ON ITS OWN FILE.
//
// THE LIVE SCOPE BOXES (PR-D): one ScopeView per tracked slot, riding the shared FrameTick.
// With them the small readers the tracked-target cards are drawn from — the chip colour, the
// kind icons, the radar returns, the sight/beacon/transponder lines — and #765's wiring window.
//
// PURE MOTION: the members below are the members that stood in TrackingPost.razor's @code block,
// character for character, at the same indentation and in the same order. Nothing was renamed,
// resignatured or reordered — the whole change is which file the lines sit in. The desk is one
// `partial class TrackingPost`; the razor file keeps the markup and nothing else. (Map.razor's own
// code came out into 155 `Map.*.cs` partials in 2026-07 for exactly this reason; the tracking post
// is the desk that never got the same treatment, which is how it reached 1,473 lines.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

public partial class TrackingPost
{
    // ---- The live scope boxes (PR-D: one per tracked slot, on the shared render loop) ----

    private static string CardCanvasId(string shipId) => $"scope-card-{shipId}";

    /// <summary>Ledger entries sorted best-quality-first — stable card order.</summary>
    private List<TrackedTarget> OrderedTracks() =>
        _ledger.Entries.OrderByDescending(e => e.EffectiveQuality(SimTime)).ToList();

    // #239 · Which colour the chip wears; the glyph and word are Core's. Written out rather than built from
    // the enum, because a rule no source can be SEEN to write is a rule nobody can find (#1110).
    private static string StateChipClass(SensorTaskState state) => state switch
    {
        SensorTaskState.Running => "sensor-chip-running",
        SensorTaskState.Waiting => "sensor-chip-waiting",
        SensorTaskState.Done => "sensor-chip-done",
        _ => "sensor-chip-queued",
    };

    private static string KindIcon(SensorTaskKind kind) => kind switch
    {
        SensorTaskKind.TrackUpdate => "🎯",
        SensorTaskKind.AreaScan => "🔭",
        SensorTaskKind.CorridorSweep => "📦",
        _ => "🔍",
    };

    private static string FindIcon(DiscoveryKind kind) => kind switch
    {
        DiscoveryKind.Debris => "🗑",
        DiscoveryKind.Rock => "🪨",
        DiscoveryKind.ColdPod => "📦",
        _ => "🛰",
    };

    /// <summary>One pass of this task at the current vantage, in human time.</summary>
    private string TaskCostText(SensorTask task)
    {
        double seconds = DurationOf(task);
        return seconds < 3600 ? $"{seconds / 60:F0} min" : $"{seconds / 3600:F1} h";
    }

    /// <summary>Let a lost track go: the search task leaves the queue and the case closes.</summary>
    private void AbandonSearch(string shipId)
    {
        _schedule.Remove($"search:{shipId}");
        _lostTracks.Drop(shipId);
        _lostCenters.Remove(shipId);
        _lastSweepMessage = $"Case closed on {FindCandidate(shipId)?.Callsign ?? shipId} — let her go";
    }

    private void TogglePassiveWatch()
    {
        _passiveWatch = !_passiveWatch;
        if (!_passiveWatch && _passiveJobRunning)
        {
            _activeJob = null;
            _passiveJobRunning = false;
        }
    }

    private List<TrackingCandidate> RadarReturns()
    {
        var list = new List<TrackingCandidate>();
        foreach (TrackingCandidate c in Candidates)
        {
            if (RadarRule.InRange(ShipPosition, c.State.Position))
            {
                list.Add(c);
            }
        }

        list.Sort((a, b) => (a.State.Position - ShipPosition).LengthSquared
            .CompareTo((b.State.Position - ShipPosition).LengthSquared));
        return list;
    }

    private static string SightLine(SightAdvantage s) =>
        s.WeSeeThem && s.TheySeeUs ? "👁 mutual — both inside detection"
        : s.TheySeeUs ? "⚠ THEY see us — we're the loud one"
        : s.WeSeeThem ? "👁 we see them — they're blind to us"
        : s.Edge >= 0 ? $"eyes race: we'd spot them first (+{FormatWallDistance(s.Edge)})"
        : $"eyes race: they'd spot us first ({FormatWallDistance(-s.Edge)} edge)";

    // M29: what THIS contact's picture of us is, beacon and optics combined — the per-observer
    // verdict on the lie. Null when the beacon adds nothing to the optical story.
    private string? BeaconLine(SightAdvantage optical) => TransponderRule.PictureFor(Transponder, optical) switch
    {
        BeaconPicture.Ghost => "🎭 they read the ghost — believed on course",
        BeaconPicture.LieBlown => "🚨 LIE BLOWN here — their own eyes beat the beacon",
        BeaconPicture.TrueContact when Transponder == TransponderMode.On && !optical.TheySeeUs
            => "📻 they hold our beacon — lit and honest",
        _ => null,
    };

    private string TransponderHint() => Transponder switch
    {
        TransponderMode.Dark => "Silent — they get only what their optics earn. Going dark near a held beacon contact is itself a tell.",
        TransponderMode.Fake => "The ghost flies the course we abandoned. Any observer whose own eyes see the real hull has us provably lying.",
        _ => $"Broadcasting our true state to everything within {FormatWallDistance(TransponderRule.BeaconRangeMeters)}.",
    };

    // #765 · THE PLAYER MAY LEAVE WHILE THE TELESCOPE IS STILL BEING WIRED. The same window #764 closed
    // in Map, one component down — and this one opens EARLIER and wider than Map's. Map.razor renders the
    // tracking post ALWAYS (FullScreen="true", d-none off-desk, so a desk switch never destroys the ledger),
    // which means this method runs on the map's very FIRST render, while the renderer module is still being
    // imported — Map itself only awaits that import at the END of a multi-second boot. Everything below the
    // await either names DOM by id (InitCanvas, and the CanvasRenderer built on the same id, which
    // renderer.js throws over rather than shrugging when the id resolves to nothing) or hangs this component
    // off a STATIC event. Back out of a slow load and the continuation resumes into a tracking post the
    // router has already discarded: it subscribes a dead component to RendererInterop.FrameTick AFTER its
    // Dispose has run, so nothing is ever going to unsubscribe it, and it draws into card canvases that left
    // the page with it — the WebAssemblyRenderer[100] shape from #734, plus a leak that outlives the page.
    private readonly CancellationTokenSource _leftTheDesk = new();

    /// <summary>#765 · Cancelled the moment this component is disposed — the tracking post's own
    /// "the player left". Every yield point in the wiring below carries it.</summary>
    internal CancellationToken LeftTheDeskToken => _leftTheDesk.Token;

    /// <summary>#765 · The wiring pass currently in flight. The renderer fires
    /// <c>OnAfterRenderAsync</c> and keeps only an error handler on the task it returns, so this is the one
    /// handle a guard can await to watch the continuation that used to outlive the page.</summary>
    internal Task? WiringTheScopes { get; private set; }

    /// <summary>#765 · True once this component has actually subscribed to the static
    /// <see cref="RendererInterop.FrameTick"/>. A wiring pass abandoned at the module load never did, and a
    /// subscription made after <see cref="Dispose"/> would never come off again.</summary>
    internal bool RidingTheFrameTick => _wallFrameTickSubscribed;

    /// <summary>#765 · The one yield point in the wiring below, and the only seam a bench can hold open:
    /// off a browser the real module import fails at once, so there would be no in-flight instant for a
    /// test to navigate away in. Production always loads the real <c>renderer.js</c> module — this exists
    /// so the guard can drive the shipping <see cref="OnAfterRenderAsync"/>, the shipping cancellation and
    /// the shipping <see cref="Dispose"/> against a load it can release on command.</summary>
    internal virtual Task LoadTheRendererModuleAsync() => RendererInterop.EnsureModuleLoadedAsync();

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!FullScreen)
        {
            return Task.CompletedTask;
        }

        return WiringTheScopes = WireTheScopesWithinTheLifeOfThePageAsync();
    }

    private async Task WireTheScopesWithinTheLifeOfThePageAsync()
    {
        try
        {
            await WireTheScopesAsync(_leftTheDesk.Token);
        }
        catch (OperationCanceledException) when (_leftTheDesk.IsCancellationRequested)
        {
            // #765: the page went while the module was still importing. There is nothing to unwind — every
            // field belongs to this instance, which the router has discarded — and nothing to report.
            // Letting this escape is exactly what raises WebAssemblyRenderer[100]. The `when` clause is
            // deliberate: a cancellation that is NOT ours is somebody else's fault and still an error.
        }
    }

    private async Task WireTheScopesAsync(CancellationToken left)
    {
        // EnsureModuleLoadedAsync takes no token of its own (it hands out one shared, cached import task
        // that every component on the page is waiting on), so WaitAsync is how THIS component's "the player
        // left" reaches the one place this method can park.
        await LoadTheRendererModuleAsync().WaitAsync(left);

        // #765 · THE LAST GATE BEFORE THE DOM AND THE STATIC EVENT. Past this line the tracking post names
        // canvases by id and hangs itself off RendererInterop.FrameTick. If the player left during the
        // import, the page those ids belong to is gone and the Dispose that would have unsubscribed us has
        // already run — so this is where an abandoned wiring pass must stop.
        //
        // Not redundant with the WaitAsync above, and the reason is a fast path worth knowing:
        // Task.WaitAsync tests IsCompleted BEFORE it tests the token, so a wiring pass whose import had
        // already landed sails through it no matter how cancelled the token is. This line is the one that
        // answers for that case.
        left.ThrowIfCancellationRequested();

        if (!_wallFrameTickSubscribed)
        {
            _wallFrameTickSubscribed = true;
            RendererInterop.FrameTick += OnWallFrameTick;
        }

        // PR-D: wire a ScopeView to every card canvas rendered this pass. Re-running InitCanvas
        // on an id renderer.js already knows just refreshes its element/context — that heals a
        // canvas Blazor re-created. observeResize:false — CRITICAL (same note as Map.razor's
        // scope inset): a resize-observing tile canvas would report its own small size back as
        // the map viewport and shrink the whole world.
        foreach (TrackedTarget entry in _ledger.Entries)
        {
            if (!_cardScopes.ContainsKey(entry.ShipId))
            {
                RendererInterop.InitCanvas(CardCanvasId(entry.ShipId), observeResize: false);
                _cardScopes[entry.ShipId] = new ScopeView(new CanvasRenderer(CardCanvasId(entry.ShipId)));
            }
        }

        if (_cardScopes.Count > _ledger.Entries.Count)
        {
            List<string>? gone = null;
            foreach (string shipId in _cardScopes.Keys)
            {
                if (!_ledger.IsTracked(shipId))
                {
                    (gone ??= []).Add(shipId);
                }
            }

            if (gone is not null)
            {
                foreach (string shipId in gone)
                {
                    _cardScopes.Remove(shipId);
                }
            }
        }
    }

    /// <summary>Redraws every card's live scope box once per animation frame — riding the same
    /// global <see cref="RendererInterop.FrameTick"/> event Map.razor's scope inset uses, so the
    /// boxes stay live at full frame rate independent of Blazor's HUD render throttle.</summary>
    private void OnWallFrameTick(double highResTimestampMs)
    {
        if (!FullScreen || !Visible)
        {
            return;
        }

        foreach (TrackedTarget entry in _ledger.Entries)
        {
            if (_cardScopes.TryGetValue(entry.ShipId, out ScopeView? scope))
            {
                scope.Draw(CardScopeSizePx, SimTime, ShipPosition, ShipVelocity, BuildWallTarget(entry));
            }
        }
    }

    private ScopeView.Target BuildWallTarget(TrackedTarget entry)
    {
        TrackingCandidate? candidate = FindCandidate(entry.ShipId);
        Vector2d position = candidate?.State.Position ?? entry.LastObservation.Position;
        Vector2d velocity = candidate?.State.Velocity ?? entry.LastObservation.Velocity;
        bool isPod = candidate?.IsPod ?? false;
        string name = candidate?.Callsign ?? entry.ShipId;

        return new ScopeView.Target(
            isPod ? ScopeView.TargetKind.Pod : ScopeView.TargetKind.Freighter,
            name, candidate?.CargoDetail, position, velocity, 0, WallTargetColor, false);
    }

    private static string FormatOppDuration(double seconds) =>
        seconds < 3600 ? "now" : seconds < 86400 ? $"in {seconds / 3600:F0} h" : $"in {seconds / 86400:F0} d";

    private static string FormatWallDistance(double meters)
    {
        const double au = 1.495978707e11;
        if (meters >= au / 10) return $"{meters / au:F2} AU";
        if (meters >= 1e9) return $"{meters / 1e9:F1} M km";
        return $"{meters / 1000:N0} km";
    }

    public void Dispose()
    {
        // #765 · First, before anything else: tell a wiring pass still parked on the module import that its
        // page is gone. The source is deliberately NOT disposed — a continuation parked on that import is
        // still holding this token and will read it as it resumes; a disposed source would answer that read
        // with an ObjectDisposedException, which is the very shape of failure this cancellation exists to
        // end.
        _leftTheDesk.Cancel();

        if (_wallFrameTickSubscribed)
        {
            RendererInterop.FrameTick -= OnWallFrameTick;
            _wallFrameTickSubscribed = false;
        }
    }
}
