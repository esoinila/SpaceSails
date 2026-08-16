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

// Map.Sim — the heartbeat: the rAF tick, the warp clock, the fixed-step accumulator that
// drives integration, plus the boot (start picker, world seed) and the raw pointer/key/wheel
// input that steers it. Split out of Map.razor for #251 — pure code motion, no behaviour change.
public partial class Map
{

    // The warp readout's text: paused, skipping (event + ETA), or the plain multiplier.
    private string WarpReadout =>
        Paused ? "∥"
        : _skipActive ? $"⏭ {FormatDuration(Math.Max(0, _skipTargetEpoch - SimTime))}"
        : $"{Warp}×";
    private const string CanvasId = "map-canvas";

    private readonly Camera _camera = new();
    private CanvasRenderer? _renderer;
    private ICelestialEphemeris? _ephemeris;
    private Simulator? _simulator;
    private PlasmaEnvironment? _plasma;
    private string _scenarioName = "";
    private ShipState _ship;
    private bool _started;
    private bool _worldReady;

    // #726 · static, so it outlives the component: the whole point is to remember across the Map
    // instances the router keeps building. Exposed internally so the guard can drive the same object
    // the page does rather than a copy of its rule.
    internal static readonly BootRegistryAnnouncement BerthRosterAnnouncement = new();

    // #318 false-hang follow-up: the coarse boot phase the loading door shows RIGHT NOW. The world build
    // runs a few seconds of synchronous planning (traffic generation) which, on the ~100×-slower dev
    // (Debug WASM) bundle, reads as a frozen tab if nothing paints. Each phase sets this then yields so
    // the door animates its own progress and the tab stays responsive, instead of a silent block.
    private string? _bootPhase;

    private int _viewportWidth = 1280;
    private int _viewportHeight = 800;

    private double SimTime;
    private int Warp = 1;
    private int _effectiveWarp = 1;
    private bool Paused;
    private bool FollowShip = true;

    // ===== #172 — "⏭ skip to next event". Acceleration WITH a destination, not teleportation: the
    // loop still integrates every tick, burns still fire, fuel still spends. The skip cranks warp
    // toward the next armed event (respecting the neighborhood caps in UpdateEffectiveWarp), eases in,
    // drops to 1× on arrival, and yields to ANY interruption — the yank paths clear _skipActive and the
    // DriveSkip catch-all stops on any external warp write. WarpSkip (Core, unit-tested) owns the pure
    // arithmetic; this is only the live wiring. =====
    private const int MaxWarpLevel = 10000;                       // the warp slider's ceiling

    private double? _lastTimestampMs;
    private double _lastHudUpdateMs;
    private bool _dragging;
    private double _lastPointerX;
    private double _lastPointerY;

    // M3 additions
    private double _simAccumulator;
    private double _lastPulseSimTime = -PulseCooldownSeconds; // so the very first pulse isn't rejected
    private int _reactionMassPulses = 500;
    private const double PulseCooldownSeconds = 1.0;
    // #693 · THE ONE SLOT, WITH A LAW ON IT. Was a bare string plus an expiry, overwritten by whoever wrote
    // last — which made the order of three blocks in Map.Surface load-bearing and left #592's climax losing
    // to the routine air line. PulseSlot (Core, and therefore sweepable) keeps the rank alongside the words.
    private PulseSlot _pulse = PulseSlot.Empty;

    // #768 · …AND THE SAYINGS THAT NEVER GOT AS FAR AS THE SLOT, because the same event raised a CARD over
    // them. The ranks cannot help there: the loser is not a lesser line, it is the whole HUD behind a
    // backdrop. The arrival holds them here and the card's dismissal lets the winner go (PulseHold, Core).
    private PulseHold _held = PulseHold.Empty;
    private const double AdaptiveWarpThreshold = 100; // below this, the historic fixed-1 s loop
    private const double AdaptiveWarpQuantum = 60;    // matches NpcTimeStep; frame-invariant
    private const double DaySeconds = 86400;
    private bool Adrift => _reactionMassPulses == 0 && !_docked;

    /// <summary>Cosmetic auto-slew: the hull swings to the firing bearing through the lock
    /// countdown, and swings back to prograde after the round leaves.</summary>
    private double ShipHeadingRad()
    {
        double prograde = _ship.Velocity.LengthSquared > 0
            ? Math.Atan2(_ship.Velocity.Y, _ship.Velocity.X)
            : 0;
        double target;
        double phase;
        if (FireLocked && _fireSolution is { } solution)
        {
            target = solution.BearingRad;
            phase = Math.Clamp((SimTime - (_fireAtSimTime - FireLockLeadSeconds)) / 30.0, 0, 1);
        }
        else if (!double.IsNaN(_slewUntilSimTime) && SimTime < _slewUntilSimTime)
        {
            target = _slewBearingRad;
            phase = Math.Clamp((_slewUntilSimTime - SimTime) / 120.0, 0, 1);
        }
        else
        {
            return prograde;
        }

        double diff = (target - prograde) % Math.Tau;
        if (diff > Math.PI) { diff -= Math.Tau; }
        if (diff < -Math.PI) { diff += Math.Tau; }
        return prograde + diff * phase;
    }

    // M7 additions — Electric Universe layer (only live when _plasma is not null)
    // #523: the threshold lives in Core now (HullCharge.ArcThreshold) so the sim and the charge board cannot
    // disagree about when she is arcing. This alias keeps the call sites readable.
    private const double ArcChargeThreshold = HullCharge.ArcThreshold;
    private bool _wasArcing;                             // rising-edge detector for the thunder cue
    private const double VentCooldownSeconds = 1.0;     // separate budget from the thrust pulse cooldown
    private double _lastVentSimTime = -VentCooldownSeconds; // so the very first vent isn't rejected
    private int _ventLineSeed;                           // #369: rotates the static-charge flavor pool, one step per vent
    private float[] _streamScratch = new float[4];      // reused endpoints buffer for stream polylines
    private static readonly RgbaColor StreamColor = new(80, 200, 220, 36);
    private static readonly RgbaColor ArcHaloColor = new(255, 240, 120, 150);

    private bool InPlasmaAt(Vector2d position) =>
        _plasma is not null && _plasma.AmbientCharge(position, SimTime) >= 1.0;


    private CelestialBody? _nearestBody;
    private Vector2d _nearestBodyPosition;
    private Vector2d _nearestBodyVelocity;
    private ElementReference _focusableDiv;

    // Vent pulse (M7): halve hull charge. No-op outside an Electric Universe scenario. Unlike a
    // thrust pulse this costs no reaction mass and never stales plan nodes — it only bleeds charge.
    private void VentCharge()
    {
        if (_plasma is null)
        {
            return;
        }
        if (_ship.SimTime < _lastVentSimTime + VentCooldownSeconds)
        {
            // #736 · The dump is pressed from the charge board as often as from the key, and the board's own
            // backdrop is over the HUD — so both answers go wherever the captain actually is.
            SayItWhereTheyAreLooking("Vent recharging…");
            return;
        }

        _lastVentSimTime = _ship.SimTime;
        double shed = _ship.Charge * 0.5;
        _ship = _ship with { Charge = _ship.Charge * 0.5 };

        // #528 / Lab 43 · light the plume at the mast, and tell the story once in a while. Both are cooled by
        // their own rules — the flash by its 600 ms, the card by StoryBeats.CadenceOf — so a captain who dumps
        // every minute is not narrated at every minute.
        _lastDischargeMs = _lastTimestampMs ?? 0;
        if (shed >= HullCharge.ContactorHoldsAt)
        {
            RaiseStoryBeat(StoryBeats.Beat.ChargeLetGo);
        }
        // #369: the vent is automatic here, so each discharge reads a rotating flavor quip
        // (house voice) rather than a bare status line. Deterministic per vent via the counter.
        SayItWhereTheyAreLooking(StaticCharge.LineFor(_ventLineSeed++));
        RendererInterop.PlayCue("vent");
    }

    /// <summary>Say it on the HUD's one pulse line.
    ///
    /// <para>#693 · <paramref name="rank"/> is what the line IS, and it decides who wins that slot when
    /// several want it in the same breath: a lower-ranked line may not displace a higher-ranked one that is
    /// still held (<see cref="PulseSlot"/>). It defaults to <see cref="PulseRank.Status"/>, which is what
    /// every instrument, price and refusal in the game is — the ranks exist for the handful of authored
    /// sentences a whole feature was built to say, and a status line dressed up as a climax to make it win
    /// is the same bug with better manners.</para>
    ///
    /// <para>The dwell is unchanged (owner 2026-07-18, "it autodisappears which is not convenient"): a line
    /// lingers long enough to READ, scaled by its length, so the words a player paid a round to hear aren't
    /// gone before they land. The durable "overheard" book is the real record; this is the doorbell.</para>
    /// </summary>
    private void ShowPulseMessage(string message, PulseRank rank = PulseRank.Status) =>
        _pulse = _pulse.Write(message, rank, _lastTimestampMs ?? 0);

    private string BodyName(string id)
    {
        foreach (CelestialBody body in _ephemeris!.Bodies)
        {
            if (body.Id == id)
            {
                return body.Name;
            }
        }

        return id;
    }

    private static string FormatSimTime(double simTime)
    {
        TimeSpan span = TimeSpan.FromSeconds(Math.Clamp(simTime, 0, TimeSpan.MaxValue.TotalSeconds - 1));
        return $"{(int)span.TotalDays}d {span.Hours:00}h {span.Minutes:00}m";
    }
    
    private static string FormatDistance(double meters)
    {
        const double metersPerAu = 1.495978707e11;
        if (meters >= metersPerAu / 10)
            return $"{meters/metersPerAu:F2} AU";
        if (meters >= 1e9)
            return $"{meters/1e9:F2} M km";
        return $"{meters/1000:F0} km";
    }

    private static string FormatZoom(double metersPerPixel)
    {
        const double metersPerAu = 1.495978707e11;
        return metersPerPixel >= metersPerAu / 100
            ? $"{metersPerPixel / metersPerAu:F4} AU/px"
            : $"{metersPerPixel:E2} m/px";
    }

    // Blazor re-renders the whole page after EVERY event by default; a held movement key
    // repeats ~30 events/s and collapsed the frame rate to ~1.5 fps (all M12/M13 scripted
    // walks came up short because of this). The game's HUD refresh is owned by OnTick's
    // 200 ms throttle, so events here run WITHOUT triggering automatic re-renders.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) =>
        callback.InvokeAsync(arg);

    private static string FormatDuration(double seconds) =>
        seconds < 86400 ? $"{seconds / 3600:F0} h" : FormatHorizon(seconds);

    // The body carrying this id, or null.
    private CelestialBody? BodyById(string? id)
    {
        if (id is null || _ephemeris is null) return null;
        foreach (CelestialBody b in _ephemeris.Bodies)
        {
            if (b.Id == id) return b;
        }
        return null;
    }
}
