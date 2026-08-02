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

    // Kepler rails (PR-B) dev cheat body for /map?ellipse=1: a sun-orbiting rock on a strongly
    // eccentric ellipse (e = 0.6) with periapsis tilted 40° off +X, semi-major axis ~1.4 AU, ~500-day
    // period. Not shipped in any scenario — appended only when the cheat is set — so it exists purely
    // to eyeball the elliptical ring and the non-uniform (fast at periapsis) tracking in-browser.
    private static BodyDefinition KeplerDemoBody() => new()
    {
        Id = "kepler-demo",
        Name = "Kepler Demo",
        ParentId = "sun",
        Mu = 0,
        BodyRadiusM = 4e9,          // oversized so it reads as a clear dot at system zoom
        OrbitRadiusM = 2.1e11,      // semi-major axis
        OrbitPeriodS = 4.32e7,      // ~500 days
        InitialPhaseRad = 0.0,      // mean anomaly at epoch — starts at periapsis
        Eccentricity = 0.6,
        ArgPeriapsisRad = 0.7,      // ~40° periapsis tilt so the ellipse is obviously not axis-aligned
        Kind = "planet",
    };

    // #370: the away-expedition site as a runtime BodyDefinition, appended before the ephemeris is built
    // (the ellipse-cheat idiom — the body list is immutable after). It co-orbits the berth <paramref
    // name="berthId"/> at a fixed small radius comfortably inside one shuttle hop, so a docked ship always
    // reads it as an in-range LANDABLE surface (a Moon-kind body with a parent). Its id carries the site
    // KIND, so the surface routes straight to the authored ground. The passing-asteroid flavor is narrated.
    private static BodyDefinition ExpeditionSiteBody(SiteSpawn spawn, string berthId) => new()
    {
        Id = spawn.BodyId,
        Name = spawn.DisplayName,
        ParentId = berthId,
        Mu = 0,
        BodyRadiusM = spawn.BodyRadiusMeters,
        OrbitRadiusM = ExpeditionSite.SpawnFraction * ShuttleRange.RangeMeters * 0.6, // ~1.5e8 m: well inside one 5e8 m hop
        OrbitPeriodS = 1.0e9,       // effectively a static co-orbiting offset — the rock just hangs alongside
        InitialPhaseRad = 0.0,
        Kind = "moon",
    };

    // A fixed, reproducible seed per flavor so the cheat always spawns the same rock (same kind + name).
    private static ulong ExpeditionCheatSeed(ExpeditionFlavor flavor) => flavor == ExpeditionFlavor.MiningSurvey ? 3702UL : 3701UL;

    // #394: the inbound rock as a runtime BodyDefinition, appended before the ephemeris is built (the
    // ellipse-cheat idiom). A Moon-kind body (landable surface) with the DeflectionGig collision RAIL —
    // an eccentric orbit around <paramref name="parentId"/> whose periapsis kisses the Ringside orbit at
    // T-impact. Its id is the DeflectionGig family id so the surface + landing route by id alone.
    private static BodyDefinition DeflectionRockBody(DeflectionGig.RockRail rail, string parentId) => new()
    {
        Id = DeflectionGig.BodyId,
        Name = "Inbound rock",
        ParentId = parentId,
        Mu = 0,
        BodyRadiusM = DeflectionGig.RockBodyRadiusMeters,
        OrbitRadiusM = rail.SemiMajorAxis,
        OrbitPeriodS = rail.OrbitPeriod,
        InitialPhaseRad = rail.InitialPhase,
        Eccentricity = rail.Eccentricity,
        ArgPeriapsisRad = rail.ArgPeriapsis,
        Kind = "moon",
    };

    // A fixed, reproducible seed so the deflection cheat always spawns the same rock (same type + name + spin).
    private const ulong DeflectionCheatSeed = 3940UL;

    // #488: the ?wreck=1 cheat's derelict — a boardable SITE co-orbiting the berth, well inside one shuttle
    // hop. She is a Moon-kind body so the whole board/land rail already knows what to do with her; her id
    // carries the wreck id (Derelict.BodyIdFor), so the deck builder and the excursion route by id alone.
    // The ship holds on her for free while the away team is inside — see LoiterKeeping / Lab 40.
    private const string WreckCheatId = "kestrel-3";

    /// <summary>The hull a wreck cheat boots you onto: the first seeded ship that died THAT way, or the
    /// default hull when no cause was asked for (and when the search finds none, which cannot happen for a
    /// cause the generator can produce, but a null here would be a crash for a typo).
    ///
    /// <para>ONE function rather than an expression at the parse site, because <c>?archive=1</c>'s guard has
    /// to be able to ask "does the hull this cheat produces actually carry a node?" — and a guard that asks a
    /// re-typed copy of the expression is guarding the copy.</para></summary>
    private static Derelict.Wreck CheatWreck(Derelict.WreckCause? cause) =>
        cause is { } forced
            ? Derelict.SeededWithCause(forced) ?? Derelict.Seeded(WreckCheatId)
            : Derelict.Seeded(WreckCheatId);

    private static BodyDefinition WreckSiteBody(string berthId, in Derelict.Wreck wreck) => new()
    {
        Id = Derelict.BodyIdFor(wreck.Id),
        Name = wreck.ShipName,
        ParentId = berthId,
        Mu = 0,
        BodyRadiusM = ExpeditionSite.BodyRadiusMeters,
        OrbitRadiusM = ExpeditionSite.SpawnFraction * ShuttleRange.RangeMeters * 0.6, // well inside one hop
        OrbitPeriodS = 1.0e9,       // effectively a static co-orbiting offset — she just hangs there
        InitialPhaseRad = 2.2,      // her own bearing off the berth, clear of the other cheat sites
        Kind = "moon",
    };

    // #409: the ?secretlab=1 cheat's landable rock — a plain Moon-kind body co-orbiting the berth, well inside
    // one shuttle hop, whose surface ResolveSecretLab forces to hide a Vantar lab with the door pre-revealed.
    private const string SecretLabCheatBodyId = "secret-lab-site";

    /// <summary>#592 · The ?secretlab=deep rock. A site's whole shape — how deep it goes, what kind of place
    /// it is, and whether it has a band nobody listed — is seeded off its BODY ID, so reaching the unlisted
    /// band from a URL is a matter of parking a rock with the right name rather than of overriding a Core
    /// fact from the client.
    ///
    /// <para>This one is a 20-floor clinic with an unlisted LABORATORY under it, down to the generator's own
    /// performance guard — which makes it the deepest, most awkward site the game can produce and therefore
    /// the right one to test on. Pinned by <c>TheUnlistedBandTests</c>: if a change to the seeding ever
    /// stopped it having a hidden band, the cheat would quietly stop reaching the feature it exists for.</para></summary>
    private const string SecretLabDeepCheatBodyId = "secret-lab-site-unlisted";

    private static BodyDefinition SecretLabSiteBody(string berthId, bool deep) => new()
    {
        Id = deep ? SecretLabDeepCheatBodyId : SecretLabCheatBodyId,
        Name = deep ? "The Deep Hermit's Rock" : "The Hermit's Rock",
        ParentId = berthId,
        Mu = 0,
        BodyRadiusM = ExpeditionSite.BodyRadiusMeters,
        OrbitRadiusM = ExpeditionSite.SpawnFraction * ShuttleRange.RangeMeters * 0.6, // ~1.5e8 m: well inside one 5e8 m hop
        OrbitPeriodS = 1.0e9,       // effectively a static co-orbiting offset — the rock just hangs alongside
        InitialPhaseRad = 1.0,      // a different bearing off the berth than the expedition rock
        Kind = "moon",
    };

    // The rock's seeded slow tumble — a spin period (30..90 s of on-site time) and a phase, so the firing
    // window comes around on its own schedule. Pure of the given seed.
    private static (double Period, double Phase) DeflectionSpin(ulong seed)
    {
        var rng = new DeterministicRandom(DiceRule.Seed(seed, "rock-spin"));
        return (rng.NextDouble(30.0, 90.0), rng.NextDouble(0.0, Math.Tau));
    }

    // #370: the cheat's resolved gig spec, stashed at build time (the site body is appended pre-ephemeris)
    // and consumed by InjectExpeditionCheat AFTER the berth clamp, so the accepted plan lands on a live world.
    private (ExpeditionFlavor Flavor, ExpeditionSiteKind Kind, string SiteBodyId, string SiteName)? _pendingExpeditionCheat;

    private readonly Camera _camera = new();
    private CanvasRenderer? _renderer;
    private ICelestialEphemeris? _ephemeris;
    private Simulator? _simulator;
    private PlasmaEnvironment? _plasma;
    private string _scenarioName = "";
    private ShipState _ship;
    private bool _started;
    private bool _worldReady;

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
    private string? _pulseMessage;
    private double _pulseMessageExpiresMs;
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
    private const double ArcChargeThreshold = 0.9;      // hull arcs (halo + system-wide visibility)
    private bool _wasArcing;                             // rising-edge detector for the thunder cue
    private const double VentCooldownSeconds = 1.0;     // separate budget from the thrust pulse cooldown
    private double _lastVentSimTime = -VentCooldownSeconds; // so the very first vent isn't rejected
    private int _ventLineSeed;                           // #369: rotates the static-charge flavor pool, one step per vent
    private float[] _streamScratch = new float[4];      // reused endpoints buffer for stream polylines
    private static readonly RgbaColor StreamColor = new(80, 200, 220, 36);
    private static readonly RgbaColor ArcHaloColor = new(255, 240, 120, 150);

    private void OnKeyUp(KeyboardEventArgs e)
    {
        if (_deckMode)
        {
            _deckKeys.Remove(Canonical(e.Key));
        }
    }

    private void OnFocusOut(FocusEventArgs e) => _deckKeys.Clear();

    // 2026-07-18 playtest: after a mouse affordance — closing the treasure-map card ("Into the ledger"),
    // clicking a desk tab — DOM focus stayed on the button, so the map div went deaf to the 0–7 desk keys
    // and E until the captain clicked the page again. The one idiom: every click that should hand the
    // keyboard back to the helm routes its state change through here, then pulls focus home to the map div.
    // Keyboard paths already own focus, so they never call this — this is the mouse's way back to the keys.
    private async Task RefocusMap() => await _focusableDiv.FocusAsync();

    // #470 · THE MOUSE'S WAY BACK, MADE GENERAL. The idiom above was right and was applied exactly four
    // times — to the four cards whose deafness the owner happened to hit and report. Nine others still took
    // the keyboard away and never gave it back, and every new card inherited the same trap (the first-ground
    // tutorial ended up switching off the three keys it had just taught).
    //
    // The fix is a seam rather than nine more copies. The Close*/Dismiss* methods stay plain synchronous
    // state changes — they are also called by the Esc handler and by the one-card-at-a-time chaining, and
    // BOTH of those are keyboard paths that already own focus. Only the mouse needs the way home, so only
    // the mouse routes through here: @onclick="() => Dismiss(CloseDossier)".
    private async Task Dismiss(Action close)
    {
        close();
        await RefocusMap();
    }

    private static string Canonical(string key) => key switch
    {
        "W" or "ArrowUp" => "w",
        "A" or "ArrowLeft" => "a",
        "S" or "ArrowDown" => "s",
        "D" or "ArrowRight" => "d",
        _ => key,
    };

    private bool InPlasmaAt(Vector2d position) =>
        _plasma is not null && _plasma.AmbientCharge(position, SimTime) >= 1.0;


    private CelestialBody? _nearestBody;
    private Vector2d _nearestBodyPosition;
    private Vector2d _nearestBodyVelocity;
    private ElementReference _focusableDiv;

    // #318 false-hang follow-up: announce a coarse boot phase and hand the frame back to the browser so
    // the queued render actually paints before the next synchronous planning block. Task.Delay(1) (vs a
    // bare Task.Yield) reliably parks on a browser timer, giving the compositor a chance to flush the
    // loading door — the animated ⚙ gear keeps turning on its own (CSS, compositor thread), the phase
    // text updates, and the tab never reads as a dead freeze even when the block runs long on Debug WASM.
    private async Task BootPhaseAsync(string phase)
    {
        _bootPhase = phase;
        StateHasChanged();
        await Task.Delay(1);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _started)
        {
            return;
        }

        _started = true;

        // /map?scenario=sol-eu loads scenarios/sol-eu.json; default sol. Name is sanitized to a
        // simple slug — it becomes a URL path segment. /map?start=space-bar jumps the freshly-built
        // world straight to a named start point (see StartPoints) — the playtest "skip the set-up"
        // shortcut, and the same registry the boot picker offers. Unknown start id → the picker shows.
        string scenarioName = "sol";
        string? startId = null;
        string? dockCheat = null;      // /map?dock=<haven-id>: boot already clamped onto ANY dockable haven (#288)
        int? fuelCheat = null;         // /map?fuel=N: boot with N reaction-mass pulses in the tank (#288)
        int? creditsCheat = null;      // /map?credits=N: boot with N credits in the purse (#288)
        string? fetchCheat = null;
        string? crackCheat = null;
        string? tipCheat = null;
        string? hoardCheat = null;
        string? slingCheat = null;
        string? skimCheat = null;
        string? backroomCheat = null;
        double? simHoursCheat = null;
        bool ellipseCheat = false; // /map?ellipse=1 injects a visibly eccentric demo body (Kepler rails, PR-B)
        string? expeditionCheat = null; // #370 /map?expedition=1|mining: spawn an away-team gig accepted + its site in shuttle range at the berth
        string? deflectionCheat = null; // #394 /map?deflection=1|C|S|M: spawn the deflection gig accepted, rock inbound, ship docked at Ringside
        bool wreckCheat = false; // #488 /map?wreck=1: spawn a derelict in shuttle range — board her, read her, then file or strip
        Derelict.WreckCause? wreckCauseCheat = null; // #488 /map?wreck=<cause>: board a wreck that died THAT way
        bool secretlabDeep = false;  // #592 /map?secretlab=deep: the rock whose site hides a band
        bool secretlabCheat = false; // #409 /map?secretlab=1: spawn a landable rock in shuttle range that hides a Vantar lab, door pre-revealed
        string? kaamosCheat = null; // #411 /map?kaamos=N|all: assemble N KAAMOS fragments (or all) so the readout + reach notice are testable; ?kaamos=pod|holder instead SEATS the rare find so it can be EARNED
        bool bondCheat = false; // #429 /map?bond=1: dock at a bar with strangers + force the next ambient scare to bond (the cognac beat)
        bool oracleCheat = false; // #428 /map?oracle=1: seat the station oracle at whatever bar you dock at (she's a coin-flip fixture otherwise)
        bool ashoreCheat = false; // #428 /map?ashore=1: boot docked AND already standing in the bar — the ship→tube→hall walk already walked
        int? nerveCheat = null;   // #428 /map?nerve=N: seed the nerve gauge at N of NervePips.MaxPips whole pips at boot
        string? nebulaCheat = null; // #422 /map?nebula=N|all: assemble N NEBULA fragments (or all) so the readout + truth notice are testable; ?nebula=adjuster instead SEATS the rare bar contact so the tell can be EARNED
        bool convergeCheat = false; // #422 /map?converge=1: seed enough of BOTH arcs to fire THE CONVERGENCE for a one-URL smoke test
        DeathCause? deathCheat = null; // #621 /map?death=<cause>: stage the REAL death at boot; the world you booted into decides the PLACE
        var revealCheats = new List<string>(); // /map?reveal=<bodyId> (repeatable): chart a hidden body at boot
        var uri = new Uri(Navigation.Uri);
        foreach (string pair in uri.Query.TrimStart('?').Split('&'))
        {
            if (pair.StartsWith("scenario=", StringComparison.OrdinalIgnoreCase))
            {
                string candidate = Uri.UnescapeDataString(pair["scenario=".Length..]);
                if (candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    scenarioName = candidate;
                }
            }
            else if (pair.StartsWith("start=", StringComparison.OrdinalIgnoreCase))
            {
                string candidate = Uri.UnescapeDataString(pair["start=".Length..]);
                if (StartPoints.Any(s => s.Id == candidate))
                {
                    startId = candidate;
                }
            }
            else if (pair.StartsWith("dock=", StringComparison.OrdinalIgnoreCase))
            {
                // #288 dev cheat: /map?dock=<haven-id> boots the ship already CLAMPED ON at that berth —
                // clean state, live services — so every dockable position smoke-tests without the long
                // navigate tax. Any dockable station haven works (DockableHavens; the full id list is
                // console-logged on boot and lives in docs/testing-guide.md), plus the friendly start
                // aliases (e.g. dock=ringside == dock=ringside-exchange). Validated once the world is built.
                string candidate = Uri.UnescapeDataString(pair["dock=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    dockCheat = candidate;
                }
            }
            else if (pair.StartsWith("fuel=", StringComparison.OrdinalIgnoreCase))
            {
                // #288 dev cheat: /map?fuel=N seeds the tank at boot (clamped to capacity), so a low-fuel
                // situation — the #262 "can I reach a pump?" test — is reachable in-situ without burning down.
                string candidate = Uri.UnescapeDataString(pair["fuel=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) && f >= 0)
                {
                    fuelCheat = f;
                }
            }
            else if (pair.StartsWith("credits=", StringComparison.OrdinalIgnoreCase))
            {
                // #288 dev cheat: /map?credits=N seeds the purse at boot, so a can-you-afford-it situation
                // (a fill-up, a bribe, an upgrade) is testable in-situ without grinding a run first.
                string candidate = Uri.UnescapeDataString(pair["credits=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) && c >= 0)
                {
                    creditsCheat = c;
                }
            }
            else if (pair.StartsWith("fetch=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?fetch=intel|active|picked injects the fetch mission at that stage so a
                // playtester can exercise each leg without the flights between. intel = the new first
                // stage (accepted, wreck hidden, tip in the ledger); active = post-scan (wreck charted,
                // backward-compatible); picked = charted + already lifted.
                string candidate = Uri.UnescapeDataString(pair["fetch=".Length..]).ToLowerInvariant();
                if (candidate is "intel" or "active" or "picked")
                {
                    fetchCheat = candidate;
                }
            }
            else if (pair.StartsWith("reveal=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?reveal=<bodyId> charts a hidden body straight away (repeatable).
                string candidate = Uri.UnescapeDataString(pair["reveal=".Length..]);
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    revealCheats.Add(candidate);
                }
            }
            else if (pair.StartsWith("crack=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?start=<station>&crack=active|picked injects the hatch-crack job at that
                // stage so a playtester can exercise the keypad / hand-off without taking the fetch first.
                string candidate = Uri.UnescapeDataString(pair["crack=".Length..]).ToLowerInvariant();
                if (candidate is "active" or "picked")
                {
                    crackCheat = candidate;
                }
            }
            else if (pair.StartsWith("tip=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?tip=route seeds a representative route tip (with provenance) into the
                // ledger so the Captain's-ledger Tips & intel rendering is reachable without walking a bar.
                string candidate = Uri.UnescapeDataString(pair["tip=".Length..]).ToLowerInvariant();
                if (candidate is "route")
                {
                    tipCheat = candidate;
                }
            }
            else if (pair.StartsWith("hoard=", StringComparison.OrdinalIgnoreCase))
            {
                // #223 dev cheat: /map?hoard=mine|rumor|both seeds the ledger's 🗺 section so the map
                // card and dig doors are reachable without flying a full bury run. mine = one of OUR
                // chests on Phobos; rumor = a bought rumour map to an NPC hoard; both = one of each.
                string candidate = Uri.UnescapeDataString(pair["hoard=".Length..]).ToLowerInvariant();
                if (candidate is "mine" or "rumor" or "both")
                {
                    hoardCheat = candidate;
                }
            }
            else if (pair.StartsWith("sling=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-G dev cheat: /map?sling=<bodyId> boots the ship on an inbound arc that already
                // has a close pass by that body ~12 days out, so the plot-desk ⤴ Sling panel is
                // reachable in seconds for testing.
                string candidate = Uri.UnescapeDataString(pair["sling=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    slingCheat = candidate;
                }
            }
            else if (pair.StartsWith("skim=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-I dev cheat: /map?skim=<bodyId> boots a fast hyperbolic inbound whose natural pass
                // grazes that body's cloud tops ~2 days out, so the plot-desk 🔥 Skim gauge is reachable
                // in seconds. Body must have an atmosphere (jupiter, earth, venus, saturn, titan).
                string candidate = Uri.UnescapeDataString(pair["skim=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    skimCheat = candidate;
                }
            }
            else if (pair.StartsWith("backroom=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-F dev cheat: /map?start=cinder-roost&backroom=open welds the V-06 back room open on
                // the spot; &backroom=quest stages the crack job (with its real code) so you can key the
                // pad yourself and watch the room grow. Testing is a feature (owner's rule).
                string candidate = Uri.UnescapeDataString(pair["backroom=".Length..]).ToLowerInvariant();
                if (candidate is "open" or "quest")
                {
                    backroomCheat = candidate;
                }
            }
            else if (pair.StartsWith("simhours=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-F dev cheat: /map?simhours=N jumps the sim clock to N hours at boot, so the roaming
                // Magpie's rota (bar → gone → back room, 4 sim-hours a stop) can be sampled without
                // waiting or warping. e.g. simhours=0 bar, 5 gone, 9 back room.
                string candidate = Uri.UnescapeDataString(pair["simhours=".Length..]);
                if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double h)
                    && h >= 0 && h < 1e6)
                {
                    simHoursCheat = h;
                }
            }
            else if (pair.StartsWith("ellipse=", StringComparison.OrdinalIgnoreCase))
            {
                // Kepler rails (PR-B) dev cheat: /map?ellipse=1 drops one visibly eccentric body onto
                // a sun orbit so the elliptical ring and its non-uniform tracking are checkable in the
                // browser. No effect on any shipped body — it's an extra body appended at load.
                string candidate = Uri.UnescapeDataString(pair["ellipse=".Length..]).ToLowerInvariant();
                ellipseCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("expedition=", StringComparison.OrdinalIgnoreCase))
            {
                // #370 dev cheat: /map?expedition=1 (scientists) or /map?expedition=mining (survey crew)
                // spawns an away-team gig ALREADY ACCEPTED, with its passing-rock site parked in shuttle
                // range at the berth, so the test loop is: spawn → shuttle door → take the team down → see
                // the away clock → come back. Documented in the PR body.
                string candidate = Uri.UnescapeDataString(pair["expedition=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes" or "science" or "mining")
                {
                    expeditionCheat = candidate;
                }
            }
            else if (pair.StartsWith("deflection=", StringComparison.OrdinalIgnoreCase))
            {
                // #394 dev cheat: /map?deflection=1 spawns the ASTEROID DEFLECTION gig ALREADY ACCEPTED — an
                // inbound rock on a collision rail with the Ringside Exchange, parked in shuttle range, ship
                // docked at Ringside. Pin the rock type with deflection=c|s|m (else seeded). The test loop is:
                // see the red threat line → shuttle to the rock → drill the charge → FIRE → watch the rail bend
                // off the station → home. Documented in the PR body.
                string candidate = Uri.UnescapeDataString(pair["deflection=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes" or "c" or "s" or "m")
                {
                    deflectionCheat = candidate;
                }
            }
            else if (pair.StartsWith("wreck=", StringComparison.OrdinalIgnoreCase))
            {
                // #488 dev cheat: /map?wreck=1 hangs a DERELICT in shuttle range off the berth. The test
                // loop is: shuttle door → board her → walk the spine → read the three evidence stations →
                // the cargo console → file the report (naming the cause) or strip her and say nothing.
                // She is seeded, so it is the same ship every time. Documented in docs/testing-guide.md.
                // …and ?wreck=<cause> (e.g. `infested`, `insurancejob`, `mutiny`) boards a wreck that died
                // THAT way on purpose, instead of re-rolling ids until the interesting one turns up.
                string candidate = Uri.UnescapeDataString(pair["wreck=".Length..]).ToLowerInvariant();
                wreckCheat = candidate is "1" or "true" or "yes";
                foreach (Derelict.WreckCause c in Enum.GetValues<Derelict.WreckCause>())
                {
                    if (candidate == c.ToString().ToLowerInvariant())
                    {
                        wreckCheat = true;
                        wreckCauseCheat = c;
                    }
                }
            }
            else if (pair.StartsWith("archive=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?archive=1&land=1 boards a derelict that is CARRYING A COLD-ARCHIVE NODE.
                // The whole beat — the dwell field, the throw, the visions, the handle — lives in one hold on
                // about one eligible wreck in three, and the house rule written next to these cheats is that
                // "a scene nobody can reach on demand is a scene that ships broken." So this boots the one
                // cause Core guarantees a node on (ArchiveCheatWreck): the ship one of her own opened to
                // space, where the node is the reason she died.
                //
                // It is deliberately NOT a "spawn a node anywhere" switch. The fiction the node belongs to
                // arrives with the hull; a node bolted into a drive failure would be a prop.
                string candidate = Uri.UnescapeDataString(pair["archive=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _archiveCheat = true;
                    wreckCheat = true;
                    wreckCauseCheat = ArchiveCheatCause;
                }
            }
            else if (pair.StartsWith("air=", StringComparison.OrdinalIgnoreCase))
            {
                // #564 dev cheat: /map?air=45 starts the excursion with 45 seconds in the tank instead of a
                // full one. A full tank is six minutes of walking by design — fine to play, useless to TEST,
                // and the owner should not have to stroll for six minutes to see the point-of-no-return
                // warning fire. Combine with dock/site/land:
                //   /map?dock=the-tilt&site=0&land=1&air=45
                string candidate = Uri.UnescapeDataString(pair["air=".Length..]);
                if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double secs))
                {
                    _airCheatSeconds = Math.Clamp(secs, 1, SuitAir.TankSeconds);
                }
            }
            else if (pair.StartsWith("collectors=", StringComparison.OrdinalIgnoreCase))
            {
                // #583 dev cheat: /map?collectors=20 forces a repo boat to follow you down and puts it on the
                // ground 20 seconds in, whatever the heat gauge reads. The scene is meant to be RARE and
                // mid-mission — which makes it nearly impossible to playtest on purpose, and a scene nobody
                // can reach on demand is a scene that ships broken. Combine with dock/site/land:
                //   /map?dock=the-tilt&site=0&land=1&collectors=20
                string candidate = Uri.UnescapeDataString(pair["collectors=".Length..]);
                if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double eta))
                {
                    _collectorCheatSeconds = Math.Max(0, eta);
                }
            }
            else if (pair.StartsWith("death=", StringComparison.OrdinalIgnoreCase))
            {
                // #621 dev cheat: /map?death=<cause> KILLS THE CAPTAIN AT BOOT, through the real pipeline.
                //
                // The death card is the one screen every player is guaranteed to see, and until now there
                // was no way to reach any of it on demand: the routes were ?floor=2&air=10 (walk until you
                // suffocate), ?reevers=8 (survive long enough to be overdrawn) and ?collectors=20 (lose the
                // Bolivia). Four causes, five stages, four places, one seeded line pool each — verified by
                // reading the source. This project's own rule, written beside these cheats: "a scene nobody
                // can reach on demand is a scene that ships broken."
                //
                // It stages the GENUINE trigger — TriggerSurfaceOverdrawDeath / TriggerImpact / a real
                // collector catch — never a mocked card, so what you see is what a player sees: the real
                // four-stage freeze beat, the real seeded narration, the real resurrection.
                //
                // There is deliberately NO ?place= parameter. WHERE you died is not an opinion the URL gets
                // to hold: the excursion's own floor and body id decide it, which is the classifier #609 was
                // filed about, and a cheat that could override it would be a second source of truth for the
                // exact fact that has now cost three death cards. You choose the place by booting into it:
                //   /map?death=impact                                   own ship
                //   /map?death=collector                                own ship (the BUSTED ladder)
                //   /map?death=suffocated&dock=the-tilt&land=1          landing party
                //   /map?death=reevers&wreck=1&land=1                   derelict
                //   /map?death=suffocated&secretlab=1&land=1&floor=2    underground
                string candidate = Uri.UnescapeDataString(pair["death=".Length..]).ToLowerInvariant();
                foreach (DeathCause c in Enum.GetValues<DeathCause>())
                {
                    if (candidate == c.ToString().ToLowerInvariant())
                    {
                        deathCheat = c;
                    }
                }
            }
            else if (pair.StartsWith("floor=", StringComparison.OrdinalIgnoreCase))
            {
                // #585 dev cheat: /map?secretlab=1&land=1&floor=3 rides you straight down to B3.
                //
                // Owner: "instruct to put the debug cheat start next to the lab so that it can be really
                // tested without playing to find it" / "I mean next to the elevator shaft". ?secretlab= now
                // sets you down AT the shed; this goes the rest of the way, because half the open work on
                // this feature is about what a FLOOR looks like, and riding four cars to reach B4 every time
                // is the same tax one level down.
                //
                // Positive number, read as a depth: floor=3 means B3. Clamped to the site's own bottom, so a
                // shallow facility cannot be asked for a floor it does not have.
                string candidate = Uri.UnescapeDataString(pair["floor=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int deep)
                    && deep > 0)
                {
                    _startingFloorCheat = -deep;
                }
            }
            else if (pair.StartsWith("outpost=", StringComparison.OrdinalIgnoreCase))
            {
                // #563 dev cheat: /map?outpost=1 guarantees the OUTPOST HUT on whatever site the excursion
                // lands on, so the lane can be playtested without hunting for a site that seeded one. Three
                // sites in four carry a hut anyway; this just removes the hunt. Combine with dock/site/land,
                // e.g. /map?dock=the-tilt&site=0&land=1&outpost=1 puts you on the regolith with one out there.
                string candidate = Uri.UnescapeDataString(pair["outpost=".Length..]).ToLowerInvariant();
                _outpostCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("secretlab=", StringComparison.OrdinalIgnoreCase))
            {
                // #409 dev cheat: /map?secretlab=1 spawns a plain LANDABLE rock parked in shuttle range at the
                // berth whose surface is GUARANTEED to hide one of Dr. Vantar's secret labs, with the hidden
                // door ALREADY REVEALED (a ⚙ HIDDEN DOOR console on the ground). The test loop is: shuttle
                // door → land → walk to the door → force it → read the logs → hit the core-log reveal.
                // Documented in the PR body. (Ordinary bodies hide labs rarely, off the seed — this is the
                // fast path.)
                string candidate = Uri.UnescapeDataString(pair["secretlab=".Length..]).ToLowerInvariant();
                secretlabCheat = candidate is "1" or "true" or "yes" or "deep";

                // #592 · ?secretlab=deep parks a rock whose site HAS a band nobody listed. The ordinary
                // cheat rock's site is seeded like any other and happens to be four floors of records annex
                // with nothing under it, so #592 could not be reached from a URL at all — which is the exact
                // tax these cheats exist to remove.
                secretlabDeep = candidate is "deep";
            }
            else if (pair.StartsWith("body=", StringComparison.OrdinalIgnoreCase))
            {
                // #585 dev cheat: /map?body=phobos&site=2&land=1 lands on THAT body's site 2, whatever is
                // nearest the berth. Owner: "let's go over all the sites we have not yet tested with the
                // url-arguments" — and until now that was impossible for most of them. ?land=1 takes the
                // first landable body in shuttle reach, so from the-tilt every URL in the world reaches
                // Miranda and nowhere else. Two thirds of the grounds we have just rebuilt had no way to be
                // opened and looked at, which for this project is the same as having no way to be tested:
                // "boot every scene and check all the parts are in the right place".
                string candidate = Uri.UnescapeDataString(pair["body=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    _forcedLandingBodyId = candidate;
                }
            }
            else if (pair.StartsWith("site=", StringComparison.OrdinalIgnoreCase))
            {
                // #320 dev cheat: /map?site=N pre-selects landing site N in the boarding panel, so a
                // playtester can board straight onto a specific ground and compare site A vs site B → a
                // visibly different surface deck-plan on the same body. Clamped to the body's real 2–4 set
                // when the panel opens. Documented in docs/testing-guide.md.
                string candidate = Uri.UnescapeDataString(pair["site=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int siteN) && siteN >= 0)
                {
                    _forcedSiteIndex = siteN;
                }
            }
            else if (pair.StartsWith("land=", StringComparison.OrdinalIgnoreCase))
            {
                // #464 dev cheat: /map?land=1 rides the shuttle down as soon as the world is ready, onto the
                // first landable body in reach (honouring ?site=N). The real BeginSurfaceExcursion and the
                // real descent — it skips only the walk to the hatch and the boarding panel, so a surface
                // playtest is one URL instead of two minutes of walking. Owner, 2026-07-27: "It is not ready
                // until it is playtested in the browser."
                string candidate = Uri.UnescapeDataString(pair["land=".Length..]).ToLowerInvariant();
                _landCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("kaamos=", StringComparison.OrdinalIgnoreCase))
            {
                // #411 dev cheat: /map?kaamos=N assembles the first N PROJEKTI KAAMOS fragments (canonical
                // order), /map?kaamos=all assembles every one — so the Captain's-ledger readout, its state
                // transitions, and the one-time reach notice are all reachable without a full playthrough.
                //
                // Those GRANT the fragments. Two of the six could only ever be granted, because their real
                // delivery is deliberately rare: the cold pod is one seeded probe square in seventeen on one
                // of seven outer moons, and the berth-holder drinks at a given bar roughly one watch in four.
                // So /map?kaamos=pod puts the pod under whatever ground this excursion lands on, and
                // /map?kaamos=holder seats the holder at whatever bar this captain docks at — the two beats
                // become playable on demand instead of merely grantable ("a scene nobody can reach on demand
                // is a scene that ships broken", and a granted shard proves nothing about the scene that
                // hands it over). Combine freely: /map?kaamos=holder&dock=ringside-exchange.
                string candidate = Uri.UnescapeDataString(pair["kaamos=".Length..]).ToLowerInvariant();
                if (candidate is "all" or "pod" or "holder"
                    || int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    kaamosCheat = candidate;
                }
            }
            else if (pair.StartsWith("bond=", StringComparison.OrdinalIgnoreCase))
            {
                // #429 dev cheat: /map?bond=1 boots docked at a bar (default The Space Bar, override with
                // ?dock=<id>) and FORCES the next ambient scare (shudder/buzzer/PA) to open a STRANGER-BOND —
                // a co-present stranger stands you a cognac (OLD PERIHELION), the hero beat. Documented in
                // docs/testing-guide.md.
                string candidate = Uri.UnescapeDataString(pair["bond=".Length..]).ToLowerInvariant();
                bondCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("oracle=", StringComparison.OrdinalIgnoreCase))
            {
                // #428 dev cheat: /map?oracle=1 boots docked at a bar (default The Space Bar, override with
                // ?dock=<id>) and SEATS the station oracle — Solenne "Static" Marsh (#425/#427) — in her
                // port-back corner, whatever her rota says this watch. She is a fixture only ~55% of watches
                // (OracleRant.PresenceChance), so the whole scene — the rant, the drink that widens the
                // channel, the room-goes-quiet tell, a true-line KAAMOS/Nebula shard landing in the ledger —
                // was a coin-flip to open, and no cheat GRANTED her lines either. The same seat idiom as
                // ?kaamos=holder / ?nebula=adjuster: it does not hand you a truth, it hands you the person.
                // Combine freely: /map?oracle=1&dock=ringside-exchange&credits=5000.
                string candidate = Uri.UnescapeDataString(pair["oracle=".Length..]).ToLowerInvariant();
                oracleCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("ashore=", StringComparison.OrdinalIgnoreCase))
            {
                // #428 dev cheat: /map?ashore=1 boots docked (default The Space Bar, override with ?dock= /
                // ?start=) and ALREADY STANDING IN THE BAR, one step inside the hall's north door, facing in.
                //
                // Every bar beat there is — the oracle (?oracle=1), the stranger-bond (?bond=1), the KAAMOS
                // berth-holder and the Nebula adjuster (?kaamos=holder / ?nebula=adjuster), the Magpie's rota
                // (?simhours=), the barkeep, the gift shop, the insurance poster — made you walk ship →
                // airlock → tube → immigration hall → bar on EVERY boot first. That walk is a pleasure to
                // play and a wall to test: an MCP-driven browser tab is `document.hidden`, so rAF is
                // throttled and WASD never lands, and not one bar beat could be smoke-tested at all.
                //
                // It seats nobody and grants nothing — it moves the captain, exactly as the walk would have.
                // The position is derived from the doorway the real walk crosses (HavenInterior.BarThreshold),
                // never typed in. Combine freely:
                //   /map?oracle=1&ashore=1                      the rant, one URL and one [E]
                //   /map?ashore=1&dock=cinder-roost&backroom=open
                //   /map?ashore=1&nebula=adjuster&simhours=9
                string candidate = Uri.UnescapeDataString(pair["ashore=".Length..]).ToLowerInvariant();
                ashoreCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("nerve=", StringComparison.OrdinalIgnoreCase))
            {
                // #428 dev cheat: /map?nerve=N seeds the nerve gauge at boot at N WHOLE PIPS — the same ten
                // the corner gauge draws (#480), not points out of a hundred — so N reads straight off the
                // pip row the player looks at. Out-of-range asks clamp to the gauge, the ?air=N idiom.
                //
                // The clamp is NOT applied here, deliberately. NervePips.FromPips already clamps to the
                // model's own MinPips..MaxPips on the way onto the pip lattice, and a second Math.Clamp on
                // this line would be a second place computing the gauge's bounds — the "one source of truth"
                // rule, and the reason a guard on the seed can only be honest if there is one clamp to break.
                //
                // Without it no sanity beat could be reached on demand: nerve only falls by being hunted for
                // minutes, so the overdraw death, the monolith's lump landing on an already-frayed captain
                // and the archive node's dwell were each a long walk away from any boot. One URL each now:
                //   /map?nerve=1&dock=the-tilt&site=0&land=1&reevers=1   one pip left, a hand inbound
                //   /map?nerve=3&dock=the-tilt&site=0&land=1             the monolith, hit at a low gauge
                //   /map?nerve=2&archive=1&land=1                        the dwell, with almost nothing to spend
                //
                // At N=1 the captain is NOT yet overdrawn (CaptainSuccession.EmptyThreshold sits under one
                // pip), so what you watch is the real two-step break — a hand takes the last pip, the NEXT
                // one breaks them — rather than an instant death the cheat invented.
                string candidate = Uri.UnescapeDataString(pair["nerve=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pips))
                {
                    nerveCheat = pips;
                }
            }
            else if (pair.StartsWith("reevers=", StringComparison.OrdinalIgnoreCase))
            {
                // #458 dev cheat: /map?reevers=N drops N Old Ones RIGHT ON the captain the moment they set
                // down, already aware — so the chase, the #441 spacing and the #453 exchange (block roll,
                // blood, the five blows) can be watched in seconds instead of hunted for on a long walk.
                // Owner, 2026-07-27: "don't forget to test that they also really work."
                string candidate = Uri.UnescapeDataString(pair["reevers=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                {
                    _reeverAmbushCheat = Math.Clamp(n, 0, 8);
                }
            }
            else if (pair.StartsWith("nebula=", StringComparison.OrdinalIgnoreCase))
            {
                // #422 dev cheat: /map?nebula=N assembles the first N NEBULA MUTUAL fragments (canonical
                // order), /map?nebula=all assembles every one — the Captain's-ledger readout, its state
                // transitions, and the one-time truth notice reachable without a full playthrough.
                //
                // Those GRANT the fragments. /map?nebula=adjuster instead SEATS the one that could only ever
                // be granted: the roving Nebula Mutual adjuster drinks at a given bar roughly one watch in
                // five, so the bar scene — the arc's best-written beat — was unopenable on purpose. Seated,
                // the "▓ Ask about NEBULA" seam is on the barkeep card at whatever bar you dock at.
                // Combine freely: /map?nebula=adjuster&dock=the-space-bar. (The KAAMOS twin is ?kaamos=holder.)
                string candidate = Uri.UnescapeDataString(pair["nebula=".Length..]).ToLowerInvariant();
                if (candidate is "all" or "adjuster"
                    || int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    nebulaCheat = candidate;
                }
            }
            else if (pair.StartsWith("converge=", StringComparison.OrdinalIgnoreCase))
            {
                // #422 dev cheat: /map?converge=1 seeds JUST ENOUGH of BOTH arcs (each side's joint
                // threshold) to fire THE CONVERGENCE — the marquee one-time reveal — from a single URL.
                string candidate = Uri.UnescapeDataString(pair["converge=".Length..]).ToLowerInvariant();
                convergeCheat = candidate is "1" or "true" or "yes";
            }
        }

        if (bondCheat)
        {
            // Arm the forced bond and make sure we boot INTO a bar (a scare on the bare ship deck has no room
            // of strangers). Default to The Space Bar; any ?dock=<id> the caller passed wins.
            _bondForce = true;
            dockCheat ??= "the-space-bar";
        }

        if (oracleCheat)
        {
            // #428 · Arm her seat BEFORE any deck is welded (SetDeckForDock reads _oracleForce and it rides
            // the deck cache key), and make sure we boot INTO a bar — an oracle with no bar to haunt is the
            // same non-scene as a scare with no room of strangers. Default The Space Bar; any ?dock= wins.
            _oracleForce = true;
            dockCheat ??= "the-space-bar";
        }

        // #428 · An ashore boot needs a bar to be ashore IN. Same idiom as the bond and the oracle above —
        // default a berth with a walkable interior — but guarded the way #621's death default is: `?dock=`
        // is read before `?start=` below, so defaulting one unconditionally would quietly outrank a
        // `?start=` the caller did pass. Anything the caller asked for still wins.
        if (ashoreCheat && dockCheat is null && startId is null)
        {
            dockCheat = "the-space-bar";
        }

        // #621 · A death needs somewhere to have happened. Without a start this boot ends at the front
        // door, and the death card would open OVER the picker — a modal on top of a menu, which is not a
        // scene anybody can read. Same idiom as the bond above: default a berth, and any ?dock= / ?start=
        // the caller passed still wins. Only for a death staged on her DECK; a landing cheat brings its
        // own ground and its own berth with it.
        // (…and only when NOTHING else chose a start: `?dock=` is read before `?start=` below, so
        // defaulting one unconditionally would quietly outrank a `?start=` the caller did pass.)
        if (deathCheat is not null && !_landCheat && dockCheat is null && startId is null)
        {
            dockCheat = "the-tilt";
        }

        // #310 honest boot state: if this boot will end at the load view (no direct start/dock cheat),
        // raise the front door NOW in its "warming the reactor" state, so the WASM warm-up never reads as
        // a broken, click-eating menu. It flips to the live slots once _worldReady flips below.
        if (dockCheat is null && startId is null && slingCheat is null && skimCheat is null)
        {
            _showStartPicker = true;
            StateHasChanged();
        }

        string json = await Http.GetStringAsync($"scenarios/{scenarioName}.json");
        ScenarioDefinition scenario = ScenarioLoader.Parse(json);
        if (ellipseCheat)
        {
            scenario = scenario with { Bodies = [.. scenario.Bodies, KeplerDemoBody()] };
        }
        if (expeditionCheat is not null)
        {
            // #370: the site is a mission-spawned body, and the ephemeris is immutable once built — so the
            // ONLY clean way to a runtime LANDABLE rock is to append it to the scenario BEFORE FromScenario
            // (the ellipse-cheat idiom). Park it co-orbiting the berth we'll clamp onto, a fixed small hop
            // off, so it is always in shuttle range. Default the berth to Selene Gate when none was asked.
            string berthKey = dockCheat ?? "selene-gate";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? mappedBerth) ? mappedBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                ExpeditionFlavor flavor = expeditionCheat == "mining" ? ExpeditionFlavor.MiningSurvey : ExpeditionFlavor.Science;
                // Position/velocity are unused here (the rock rides real orbit rails); Spawn only fixes the
                // seeded KIND + display name deterministically.
                SiteSpawn spawn = ExpeditionSite.Spawn(ExpeditionCheatSeed(flavor), Vector2d.Zero, Vector2d.Zero, flavor);
                scenario = scenario with { Bodies = [.. scenario.Bodies, ExpeditionSiteBody(spawn, berthId)] };
                _pendingExpeditionCheat = (flavor, spawn.Kind, spawn.BodyId, spawn.DisplayName);
                dockCheat = berthId; // clamp onto the berth the rock co-orbits, so it's in reach at spawn
            }
        }
        if (deflectionCheat is not null
            && scenario.Bodies.FirstOrDefault(b => b.Id == "ringside-exchange") is { } ring
            && scenario.Bodies.Any(b => b.Id == ring.ParentId))
        {
            // #394: the inbound rock is a mission-spawned body on a real COLLISION rail with the Ringside
            // Exchange — appended before FromScenario (the immutable-ephemeris idiom), timed so it kisses the
            // station's orbit at T-impact. Ship clamps onto Ringside (in shuttle reach of the rock at spawn).
            RockType type = deflectionCheat switch
            {
                "c" => new RockType(RockComposition.CType),
                "s" => new RockType(RockComposition.SType),
                "m" => new RockType(RockComposition.MType),
                _ => DeflectionGig.RollType(DeflectionCheatSeed),
            };
            double impactRailTime = DeflectionGig.RailLeadSeconds; // fresh boot: accept at sim-time ≈ 0
            DeflectionGig.RockRail rail = DeflectionGig.BuildRail(
                ring.OrbitRadiusM, ring.OrbitPeriodS, ring.InitialPhaseRad, impactRailTime);
            (double spinPeriod, double spinPhase) = DeflectionSpin(DeflectionCheatSeed);
            scenario = scenario with { Bodies = [.. scenario.Bodies, DeflectionRockBody(rail, ring.ParentId!)] };
            _pendingDeflectionCheat = (type, DeflectionGig.RockName(DeflectionCheatSeed), rail,
                ring.Id, "Ringside Exchange", ring.OrbitRadiusM, ring.OrbitPeriodS, ring.InitialPhaseRad,
                ring.ParentId!, impactRailTime, spinPeriod, spinPhase);
            dockCheat = "ringside-exchange"; // clamp onto the port under threat, in reach of the rock
        }
        if (secretlabCheat)
        {
            // #409: append a plain landable Moon-kind rock co-orbiting the berth (the ellipse-cheat idiom),
            // comfortably inside one shuttle hop. Its surface is FORCED to hide a Vantar lab and to pre-reveal
            // the door (see ResolveSecretLab, keyed on _secretLabForceBodyId). Default the berth to Selene Gate.
            string berthKey = dockCheat ?? "selene-gate";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? mappedBerth) ? mappedBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                scenario = scenario with { Bodies = [.. scenario.Bodies, SecretLabSiteBody(berthId, secretlabDeep)] };
                _secretLabForceBodyId = secretlabDeep ? SecretLabDeepCheatBodyId : SecretLabCheatBodyId;
                dockCheat = berthId; // clamp onto the berth the rock co-orbits, so it's in reach at spawn
            }
        }
        if (wreckCheat)
        {
            // #488: append the derelict as a boardable site co-orbiting the berth — the same ellipse-cheat
            // idiom the expedition rock and the Hermit's Rock use. She is seeded from her id, so this cheat
            // always spawns the SAME ship with the same cause and the same cargo, which is what makes her
            // testable. Default the berth to The Tilt.
            string berthKey = dockCheat ?? "the-tilt";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? wreckBerth) ? wreckBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                Derelict.Wreck w = CheatWreck(wreckCauseCheat);
                scenario = scenario with { Bodies = [.. scenario.Bodies, WreckSiteBody(berthId, w)] };
                _wreck = w;
                dockCheat = berthId; // clamp onto the berth she hangs off, so she is in reach at spawn
            }
        }
        _scenarioName = scenario.Name;
        _ephemeris = CircularOrbitEphemeris.FromScenario(scenario);
        // #288: print the enumerable registry of every dockable berth to the browser console on boot, so
        // the bench never guesses an id — /map?dock=<id> boots already clamped on at any of these.
        Console.WriteLine($"[SpaceSails] Dockable berths — /map?dock=<id>: {string.Join(", ", DockableHavens.AllIds(_ephemeris))}");
        // Tuesday plan PR-A: the scenario's off-the-charts bodies (e.g. the derelict roadster). They
        // stay dark until an intel-fed scan (or a dev reveal cheat) charts them.
        _hiddenBodyIds.Clear();
        _revealedBodyIds.Clear();
        foreach (BodyDefinition body in scenario.Bodies)
        {
            if (body.Hidden)
            {
                _hiddenBodyIds.Add(body.Id);
            }
        }
        // PR-15, the captain's position: the mission catalog is scenario data (cargo classes,
        // route pairs, havens), so it's built once per scenario load alongside everything else
        // that reads _ephemeris — never recomputed per frame or per desk switch.
        _missionOptions = MissionCatalog.Build(_ephemeris);
        _plasma = PlasmaEnvironment.FromScenario(scenario, _ephemeris);
        _simulator = new Simulator(_ephemeris, timeStepSeconds: 1.0, _plasma);
        _npcSimulator = new Simulator(_ephemeris, TrafficSchedule.NpcTimeStep); // NPCs chargeless in M7

        _ship = InitializeShipState();
        ReprojectTrajectory();

        // Owner (2026-07-05, after the empty-purse screenshot): an operating ship arrives with
        // history — the last run's takings in the purse and its leftovers in the hold, so
        // buying AND selling are exercisable from minute one.
        _credits = StartingCredits;
        foreach ((string cargoClass, int units) in StartingManifest)
        {
            _cargoUnits += units;
            _cargoValue += units * CargoMarket.UnitValue(cargoClass);
            _cargoByClass[cargoClass] = _cargoByClass.GetValueOrDefault(cargoClass) + units;
        }

        // Generate traffic once from the same deterministic Core planner the server uses. This does a
        // few seconds of planning work — and each coarse step (pods / freighters / depots / mass-driver
        // pods) is its own synchronous block. #318 follow-up: paint an honest phase line and yield to the
        // browser BEFORE each block, so the loading door shows progress and the tab stays paintable
        // instead of one silent multi-second freeze (badly amplified on the Debug/dev bundle). We do NOT
        // parallelise or restructure the planners — just phase-yield around them.
        // Pods first so "the Luna pod" is the top of the board and the obvious tutorial prey.
        await BootPhaseAsync("plotting the traffic lanes — pods…");
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(_ephemeris, seed: 43, count: 3);
        await BootPhaseAsync("plotting the traffic lanes — freighters…");
        IReadOnlyList<NpcShip> traffic = TrafficSchedule.Generate(_ephemeris, seed: 42, count: 8);
        // The derelict roadster is a dead wreck, not a trading post — it's a station body only so its
        // map label reads at a sane zoom (the fetch-mission target). Drop the depot GenerateDepots
        // would otherwise hang on it (any sun-orbiting body gets one).
        // A depot on a hidden body would leak it (the depot marker/menu would give the wreck away).
        // Filter generically on hidden+unrevealed, not the wreck's id — every future secret body is
        // covered for free (Tuesday plan PR-A).
        await BootPhaseAsync("plotting the traffic lanes — supply depots…");
        IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(_ephemeris, seed: 44)
            .Where(d => d.DepotBodyId is null || !IsBodyHidden(d.DepotBodyId)).ToList();

        // The tutorial's "first hunt" needs prey the player can actually catch from a standing start.
        // Interplanetary traffic screams past at 80–160 km/s relative — past the 5 km/s boarding
        // limit — so in the Sol family we seed one guaranteed-catchable pod abeam the ship: a fresh
        // Luna launch still co-moving with Earth, a short plotted burn away. (See StarterPod.)
        IEnumerable<NpcShip> initial = pods.Concat(traffic).Concat(depots);
        if (_scenarioName.Contains("Sol", StringComparison.OrdinalIgnoreCase))
        {
            // Luna's mass drivers, lobbing compute-core pods on a steady cadence (worldbuilding §1;
            // Lab 30 "The mass-driver timetable"): a modest run of ballistic pods fired retrograde
            // toward the inner system, half already in flight at world-load, so the "Luna's mass
            // drivers lobbing compute-core pods" the scenario description promises is literally on the
            // map as tiny moving objects. Zero maneuver budget, empty plan — they coast their conic.
            await BootPhaseAsync("plotting the traffic lanes — Luna's mass drivers…");
            IReadOnlyList<NpcShip> lunaDriver = MassDriverSchedule.GenerateCadence(
                _ephemeris, MassDriverSchedule.MassDriverRun.LunaMilkRun(), baseSimTime: _ship.SimTime, count: 4);

            // Ruling-2 (2026-07-18): the first-hunt soft catch is NO LONGER seeded at boot. With every
            // start now DOCKED (and never a T=0 Earth cast-off), the tutorial pod would spawn abeam a
            // free-flying Earth ship that is about to be clamped onto a station somewhere else entirely —
            // an orphan. Instead the target "gets going" when the lesson is TAKEN ON: the auto-greet at
            // the Selene Gate tutorial home (ApplyStart) and the Captain's-tab StartTutorial both call
            // SeedFirstHuntTarget, which places the pod relative to where the ship actually is THEN. The
            // stubborn Lark is likewise spawned only when the first hunt ends (SeedSecondHuntTarget).
            initial = lunaDriver.Concat(initial);
        }
        _npcStates = initial.Select(s => new NpcState { Ship = s }).ToArray();

        _scratch = new float[_samples.Count * 2 + 4];

        _camera.MetersPerPixel = 3e8;
        _camera.CenterOn(_ship.Position);

        await RendererInterop.EnsureModuleLoadedAsync();
        _renderer = new CanvasRenderer(CanvasId);
        RendererInterop.FrameTick += OnTick;
        RendererInterop.CanvasResized += OnCanvasResized;

        RendererInterop.InitCanvas(CanvasId, observeResize: true);
        RendererInterop.InitCanvas(ScopeCanvasId, observeResize: false);
        _scopeView = new ScopeView(new CanvasRenderer(ScopeCanvasId));
        _deckView = new DeckView(_renderer!);
        _fpView = new FirstPersonView(_renderer!);
        _shuttleView = new ShuttleFlightView(_renderer!);
        RendererInterop.StartLoop(CanvasId);

        // #371 Phase 1 (perf) · PRE-DECODE the deck/surface backdrop art at boot. RegisterImage fires the
        // JS decode fire-and-forget and caches by id; doing it now means the first deck or surface paint
        // never stalls waiting on an image decode (the study's cheap pre-warm). The surface plan reuses the
        // ship's backdrop set, so warming the ship's covers both.
        PredecodeDeckArt();

        _worldReady = true;

        // Start point: an explicit /map?start=<id> jumps straight there (the renderer is live now, so
        // a docked-&-ashore start's board cue is safe); with no param, offer the boot picker so a
        // playtester (or a player who'd rather not always cast off from Earth) can choose a locale.
        if (dockCheat is not null && ResolveDockStartId(dockCheat) is { } dockHaven)
        {
            StartDockedAtHaven(dockHaven); // #288: boot already clamped on at any dockable berth
        }
        else if (startId is not null)
        {
            ApplyStart(startId);
        }
        else
        {
            PeekSavedVault(); // #225: surface a "Continue — docked at <haven>" lead if a vault exists.
            _showStartPicker = true;
        }

        // #428 · ?ashore=1 — walk the walk for them. AFTER the clamp (the interior is welded by
        // SetDeckForDock, which the start above ran) and BEFORE any landing cheat, which brings its own
        // ground and takes the captain off this deck entirely.
        if (ashoreCheat)
        {
            ShowPulseMessage(StandAtTheBarThreshold()
                ? $"🍸 Test: you are ashore in {_havenName} — the ship → tube → hall walk is already behind you. [E] works the tables, the counter and the corners."
                : "🍸 Test: ?ashore=1 needs a berth with a walkable interior — this one has no bar to stand in. Try &dock=the-space-bar.");
        }

        // #428 · ?nerve=N — seed the gauge BEFORE the landing cheat rides the shuttle down and before any
        // ?death= is staged, because both READ the live nerve: the descent's first frames price the gauge,
        // and the death card asks CaptainSuccession.OverdrawQualifies(_nerve) whether the captain was
        // already empty. Seeding after them would hand the card the default steady gauge and caption a
        // shattered captain as merely mauled — the sentence saying one thing while the sim did another.
        if (nerveCheat is { } seedPips)
        {
            _nerve = NervePips.FromPips(seedPips);
        }

        if (_pendingExpeditionCheat is not null)
        {
            InjectExpeditionCheat(); // #370: after the clamp — the accepted gig lands on a live, docked world
        }

        if (_landCheat)
        {
            // #464: ride the shuttle down now that the berth is clamped and the ephemeris is live, so the
            // in-range board is real. Fire-and-forget: BeginSurfaceExcursion narrates its own descent
            // phases and yields between them, exactly as the hatch's own path does.
            // #621: …and ?death= waits for the boots to be on the ground, because the PLACE is read off the
            // live excursion. Killing the captain before the shuttle has landed would classify the death on
            // her deck and hand back the wrong card — which is the whole bug the cheat exists to hunt.
            _ = AutoLandThenStageDeathAsync(deathCheat);
        }
        else if (deathCheat is { } onHerDeck)
        {
            StageDeathCheat(onHerDeck);
        }

        if (_pendingDeflectionCheat is not null)
        {
            InjectDeflectionCheat(); // #394: after the clamp — rock inbound, ship docked at the threatened port
        }

        if (fetchCheat is not null)
        {
            InjectFetchCheat(fetchCheat); // after the start, so the dest can be the station we docked at
        }

        if (crackCheat is not null)
        {
            InjectCrackCheat(crackCheat); // needs the docked station's deck built (a locked hatch to target)
        }

        if (backroomCheat is not null)
        {
            InjectBackroomCheat(backroomCheat); // PR-F: weld the wing open, or stage the crack that opens it
        }

        if (tipCheat is not null)
        {
            InjectTipCheat(); // seed a representative route tip so the ledger's Tips & intel is reachable
        }

        if (hoardCheat is not null)
        {
            InjectHoardCheat(hoardCheat); // #223: seed a buried chest and/or a bought rumour map
        }

        if (kaamosCheat is not null)
        {
            SeedKaamosCheat(kaamosCheat); // #411: assemble N KAAMOS fragments (readout + reach notice), or seat the pod/holder so the find itself can be played
        }

        if (nebulaCheat is not null)
        {
            SeedNebulaCheat(nebulaCheat); // #422: assemble N NEBULA fragments (readout + truth notice), or seat the adjuster so the bar scene itself can be played
        }

        if (convergeCheat)
        {
            SeedConvergeCheat(); // #422: seed both arcs' joint threshold and fire THE CONVERGENCE reveal
        }

        if (_oracleForce)
        {
            // #428: say WHERE she is, not just that she's here — the corner is deliberately clear of every
            // other console, and a captain who can't find her reads the cheat as broken.
            ShowPulseMessage("🌀 Test: Static Marsh has the port-back corner of this bar, whatever the watch. Walk in, head aft along the left wall, and press E on ◈ “STATIC” MARSH.");
        }

        // Tuesday plan PR-A: ?start=wreck drops you 2 km off the roadster — you're on top of her, so
        // chart her quietly (no "found it!" fanfare when you were parked alongside all along). This
        // also keeps ?start=wreck&fetch=active green.
        if (startId == "wreck")
        {
            RevealBody("derelict-roadster", "", announce: false);
        }

        // ?reveal=<bodyId> (repeatable): chart any hidden body at boot for testing every downstream leg.
        foreach (string id in revealCheats)
        {
            RevealBody(id, $"🧪 Test: {BodyName(id)} charted.");
        }

        // ?sling=<bodyId>: boot onto an inbound arc with a close pass by that body (PR-G test hook).
        // Suppress the start picker — picking a berth would overwrite the seeded approach state.
        if (slingCheat is not null)
        {
            _showStartPicker = false;
            SeedSlingCheat(slingCheat);
        }

        // ?skim=<bodyId>: boot onto a hyperbolic inbound grazing that body's atmosphere (PR-I test hook).
        if (skimCheat is not null)
        {
            _showStartPicker = false;
            SeedSkimCheat(skimCheat);
        }

        // ?credits=N / ?fuel=N (#288): seed the purse and tank last, after any start has laid down the
        // defaults, so an in-situ situation (afford a fill-up, reach a pump) is set up straight from boot.
        if (creditsCheat is { } seedCredits)
        {
            _credits = seedCredits;
        }

        if (fuelCheat is { } seedPulses)
        {
            _reactionMassPulses = Math.Clamp(seedPulses, 0, ReactionMassCapacity);
        }

        // ?simhours=N: jump the sim clock at boot so the roaming Magpie's rota can be sampled (PR-F).
        // While docked, HoldAtDock re-pins the ship to the berth at the new time on the next tick.
        if (simHoursCheat is { } jumpHours)
        {
            _ship = _ship with { SimTime = jumpHours * 3600 };
            SimTime = _ship.SimTime;
        }

        StateHasChanged();
        await _focusableDiv.FocusAsync();

        // #371 Phase 1 (perf) · warm the cold surface DRAW path once, idle-time, now that the map is
        // interactive. Fire-and-forget and yield-fronted so it never lengthens the perceived boot stall;
        // see WarmSurfaceDrawPathAtBootAsync for the never-flash guard.
        _ = WarmSurfaceDrawPathAtBootAsync();
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

        if (_deckView is null || _renderer is null)
        {
            return;
        }
        try
        {
            // Throwaway plan — Miranda's ground, an empty own-cache set, a no-op droid fill. The memoized
            // layout it builds also warms the (now shared) SurfaceDeck cache for a first landing on Miranda.
            DeckPlan warm = MoonSurface.SurfaceDeck(
                "miranda", "Miranda",
                System.Array.Empty<(string, double, double, int)>(),
                DeckPlan.Ship.DroidCount, static (_, _) => { });

            if (_showStartPicker && _viewportWidth > 0 && _viewportHeight > 0)
            {
                var hud = new DeckView.SurfaceHud(
                    DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
                    Blips: System.Array.Empty<(double, double)>(), Cadence: 0, Readout: "",
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

    // --- Start points (2026-07-08; docked-starts rework 2026-07-18) ---
    // "Why should it always start from Earth?" — and the answer, owner ruling 2026-07-18: it never does.
    // Every named start now CLAMPS onto a station (DockedStarts maps the id → haven); the /map?start=<id>
    // URL routes through ApplyStart → the one shared clamp (StartDockedAtHaven). Body ids are
    // scenario data. Test:true starts are dev-only free-flying jumps (they exercise a free approach), hidden
    // from the picker. The picker itself no longer reads this list — it offers the live dockable-haven
    // registry (BerthStarts) — so this is now purely the /map?start= alias table with human labels.
    private sealed record StartPoint(string Id, string Icon, string Label, string Blurb, bool Test = false);

    private static readonly StartPoint[] StartPoints =
    [
        new("earth", "🌙", "Selene Gate — docked (Luna orbit)",
            "The classic first voyage, minus the Earth-centrism: begin clamped on at Selene Gate, in Luna's orbit, where the compute-core pods launch. The soft-catch lesson starts here."),
        new("cinder-roost", "🌋", "Cinder Roost — docked (Venus)",
            "In Venus' sulphur clouds — begin already clamped on at Cinder Roost, a short walk up the tube to The Cinder Lounge."),
        new("space-bar", "🍸", "The Rusty Roadstead — docked (Mars)",
            "Skip the haul to Mars — begin already clamped on at The Rusty Roadstead, a short walk up the tube to the bar's tables."),
        new("jupiter", "🪐", "The Red Eye — docked (Jupiter)",
            "Out among the Galilean moons — begin already clamped on at The Red Eye, Europa and Ganymede a short burn away."),
        new("saturn", "💍", "Ringside Exchange — docked (Saturn)",
            "In Saturn's rings — begin already clamped on at Ringside Exchange, a short walk up the tube to The Ringside Bar; Enceladus and Titan a burn away."),
        new("the-tilt", "❄️", "The Tilt — docked (Uranus)",
            "Way out at Uranus — begin already clamped on at The Tilt, a short walk up the tube to its cold, lonely bar."),
        new("the-deep", "🌀", "The Deep — docked (Neptune)",
            "At the edge of the charts — begin already clamped on at The Deep, above Neptune, a fuel pump and a long way from anyone."),
        new("wreck", "🚗", "The Derelict Roadster — alongside (test)",
            "Co-moving beside the lost roadster, sunward of Mars — for testing the fetch pickup.", Test: true),
        new("enceladus", "❄️", "Enceladus — alongside (test)",
            "Co-moving beside Enceladus, a short fall from its capture band — for testing the deep-well auto-orbit (#136).", Test: true),
    ];

    private bool _showStartPicker;

    // --- The dev start sites (#439) -----------------------------------------------------------------
    // Owner, 2026-07-26: "We should have a developer list of these quick starts in the UI also. We can
    // later disbale it." THIS is that switch: flip it to false and the whole section leaves the front door
    // (the catalogue itself lives in Core DevStarts, and docs/testing-guide.md keeps the prose twin). Left
    // collapsed by default so the logbook still opens on saves and berths, not on the service door.
    private const bool ShowDevStarts = true;
    private bool _showDevStarts;

    // Take a dev start. These are BOOT-TIME cheats — the world is built from the URL once, in OnInitialized
    // — so this is a full reload, not a router hop, or the cheat would be read against an already-built Sol.
    private void GoToDevStart(SpaceSails.Core.DevStarts.Entry entry) =>
        Navigation.NavigateTo(entry.Url, forceLoad: true);

    // Arrange the just-built (or, on a picker reopen, already-running) world for a chosen start.
    // Re-entrant: steps aboard and unclamps any current berth first, so it's safe to call any time.
    private void ApplyStart(string id)
    {
        _dockedHavenId = null;   // drop any prior clamp before the jump
        SetDeckForDock(null);    // back to the bare ship deck (pulls you aboard if you'd wandered ashore)

        // Owner ruling (2026-07-18): every start is a DOCKED start — clamp onto the haven the id names,
        // never a free-flying "fresh out of Earth orbit" spawn. The id resolves to a dockable haven via
        // DockedStarts (incl. the friendly aliases and the retired 'earth' → Selene Gate fallback), and
        // the ONE shared clamp lays it down — a co-moving berth, a welded interior (or the Nav map for a
        // pumps-only berth), HoldAtDock pinning it — so a picked start is byte-for-byte a real arrival.
        if (ResolveDockStartId(id) is { } havenId)
        {
            StartDockedAtHaven(havenId);
            MaybeGreetTutorialHome(havenId);
            return;
        }

        // The only non-docked starts left are the dev-only Test jumps (the derelict roadster, the
        // Enceladus capture band) that deliberately exercise a free-flying approach — kept for the bench.
        _ship = PlaceShipForStart(id);
        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        _showTutorial = false;
        _deckMode = false;
    }

    // #288: resolve a /map?dock=<id> value to a dockable-haven body id, or null if it names no berth.
    // Accepts both the haven's own body id (e.g. "the-tilt", "red-eye") and the friendly start aliases
    // (e.g. "ringside" → "ringside-exchange", "space-bar" → "the-space-bar"), so either form docks.
    private string? ResolveDockStartId(string idOrAlias)
    {
        if (_ephemeris is null)
        {
            return null;
        }

        string havenId = DockedStarts.TryGetValue(idOrAlias, out string? mapped) ? mapped : idOrAlias;
        return _ephemeris.Bodies.FirstOrDefault(b => b.Id == havenId && DockableHavens.IsDockable(b))?.Id;
    }

    // #288: boot already clamped onto ANY dockable station haven — the smoke-test hook that generalises
    // ApplyStart's docked branch (four curated DockedStarts) to every haven in the scenario. Rides the
    // one true clamp (ClampOntoHaven: co-moving berth via BerthState.CoMoving, welds any interior, pins
    // via HoldAtDock, saves the resume vault) so a docked-cheat start is byte-for-byte a real arrival.
    // Steps ashore where there's a walkable interior; otherwise leaves you on the bare ship deck at Nav.
    private void StartDockedAtHaven(string havenId)
    {
        if (_ephemeris is null || ResolveDockHaven(havenId) is not { } dock || !DockableHavens.IsDockable(dock.Body))
        {
            return;
        }

        _showStartPicker = false;
        _showTutorial = false;          // an outer berth is no place for the Earth-anchored checklist
        SetDeckForDock(null);           // drop any deck we might be jumping from
        ClampOntoHaven(dock.Body, dock.Pos);

        if (HavenInterior.HasInterior(havenId))
        {
            (_avatarX, _avatarY, _avatarHeading) = (2.5, 6, Math.PI / 2); // in the airlock, facing up the tube
            _deckMode = true;
            _activeDesk = ShipDesk.Deck;
        }
        else
        {
            _deckMode = false;          // no walkable complex out here — sit on the Nav map, clamped on
        }

        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
    }

    // The ship's state for a start point. Reuses InitializeShipState's finite-difference "co-moving
    // with a body" idiom, just keyed off a different body — a small radial offset keeps the ship clear
    // of the body's surface. "earth" (and any unknown id) falls back to the standard Earth spawn.
    private ShipState PlaceShipForStart(string id)
    {
        if (DockedStarts.TryGetValue(id, out string? dockBody))
        {
            return CoMovingBy(dockBody, 3_000); // just off the ~1 km station, well within dock reach
        }
        return id switch
        {
            "jupiter" => CoMovingBy("europa", 2e7),           // clear of Europa's surface, amid the Galilean system
            "saturn" => CoMovingBy("ringside-exchange", 2e7), // by the ring station, Enceladus/Titan a burn away
            "enceladus" => CoMovingBy("enceladus", 5e6),      // (test) co-moving alongside Enceladus, ~5 Hill radii out (#136)
            "wreck" => CoMovingBy("derelict-roadster", 2_000), // (test) alongside the wreck, inside fetch-pickup range
            _ => InitializeShipState(),
        };
    }

    // A ship state co-moving with a body at boot (SimTime 0), a given distance radially outward from it
    // (from the Sun's frame). offsetMeters 0 sits right on the body; a few thousand metres clears a
    // station, ~1e7+ a moon. Delegates to the shared BerthState.CoMoving construction (#269).
    private ShipState CoMovingBy(string bodyId, double offsetMeters)
        => BerthState.CoMoving(_ephemeris!, bodyId, 0, offsetMeters);

    // The Captain's "🧭 Set course to a start point…" button: bring the chooser back up mid-run so a
    // locale can be (re)picked from the chart-room, not just at boot. ApplyStart is re-entrant, so the
    // jump is safe from anywhere.
    private void ReopenStartPicker() => _showStartPicker = true;

    // #292/ruling-2 (owner 2026-07-18): the nav-screen checklist is no billboard. It greets ONLY a
    // brand-new captain beginning a fresh voyage at the cislunar tutorial home (Selene Gate, in Luna's
    // orbit — where the first lesson's compute-core pod actually launches), and seeds that pod RIGHT
    // HERE, at acceptance, relative to where the ship is NOW — never on a T=0 Earth clock. Any other
    // berth, or a captain who has already played, keeps the real estate clear; the Captain's Tutorials
    // tab (0) reopens a lesson deliberately (StartTutorial reseeds and re-shows). Called after the clamp
    // is laid, from every fresh-voyage start path (ApplyStart and the picker's ChooseBerthStart).
    private void MaybeGreetTutorialHome(string havenId)
    {
        if (havenId != TutorialHomeHavenId
            || !TutorialPromotion.ShouldPromote(TutorialStartMode.FreshFromEarth, _tutorialPlayed))
        {
            return;
        }

        _tutorialStep = 0;
        _showTutorial = true;
        SeedFirstHuntTarget(); // the target gets going when the lesson is taken on, wherever we are
    }

    private void OnCanvasResized(double widthPx, double heightPx)
    {
        if (widthPx <= 0 || heightPx <= 0)
        {
            return;
        }

        _viewportWidth = (int)Math.Round(widthPx);
        _viewportHeight = (int)Math.Round(heightPx);
    }

    private void OnTick(double highResTimestampMs)
    {
        if (_renderer is null || _ephemeris is null || _simulator is null)
        {
            return;
        }

        double dtRealSeconds = _lastTimestampMs is null
            ? 0
            : Math.Max(0, (highResTimestampMs - _lastTimestampMs.Value) / 1000.0);
        _lastTimestampMs = highResTimestampMs;
        _frameNowMs = highResTimestampMs;

        // #255: a long haul is crossing — the world is frozen mid-jump (the re-seed owns the clock, and
        // the void is never integrated). The overlay paints via Blazor; the canvas holds its last frame.
        if (_jumpInProgress)
        {
            return;
        }

        FlushVaultSaveIfDirty();  // #225: one debounced autosave write per frame when a durable event fired

        StepShudder(dtRealSeconds, highResTimestampMs); // #424 HULL-SHUDDER: the ambient interior-deck tremor
        StepSignal(dtRealSeconds, highResTimestampMs);  // #424 THE UNEXPLAINED SIGNAL: the shudder's colder sibling
        StepCaution(highResTimestampMs);                // #424 THE CAUTION ANNOUNCEMENT: the rough-passage PA

        UpdateNearestBody();
        CheckFetchPickup();     // coasting past the wreck grabs a fetch job's goods
        DriveSkip();            // #172: own the warp while skipping — arrive/announce, or yield to the helm
        UpdateEffectiveWarp();

        if (!Paused)
        {
            _simAccumulator += dtRealSeconds * _effectiveWarp;
            _simAccumulator = Math.Min(_simAccumulator, MaxStepsPerFrame * _simulator.TimeStep); // Clamp accumulator
        }

        double simTimeBefore = _ship.SimTime;
        // The pursuit quantum trail (see SteerHuntersByQuantumTrail — the abort switch): remember
        // where the ship actually IS through this frame's integration, at the hunter-quantum
        // cadence, so pursuit steering can look up sim-time positions instead of the frame-end
        // one. Only paid while hunters fly; a berthed ship skips it (HoldAtDock pins the truth
        // AFTER this loop, so the trail would be staler than _ship).
        bool recordTrail = SteerHuntersByQuantumTrail && _hunters.Count > 0 && _dockedHavenId is null;
        _pursuitTrail.Clear();
        if (recordTrail)
        {
            _pursuitTrail.Add(new TrajectorySample(_ship.SimTime, _ship.Position));
        }

        int stepsThisFrame = 0;
        // PR-I: watch the drag load through this frame's steps so a cloud-top dip can hole the sail. Only
        // paid near an atmosphere-bearing body (where warp auto-drops to 1 s steps, so the peak is caught).
        _frameMaxDragDecel = 0;
        bool watchDrag = _dockedHavenId is null && _nearestBody?.Atmosphere is not null;
        while (_simAccumulator >= _simulator.TimeStep)
        {
            if (stepsThisFrame >= MaxStepsPerFrame)
            {
                _simAccumulator = 0;
                break;
            }

            // M19: at high warp, consume the accumulator in fixed 60 s quanta on the planner's
            // adaptive clock — one leapfrog step instead of sixty in deep space, auto-refining
            // to 1 s steps near bodies (where warp auto-drop puts us back on the fixed path
            // anyway). Fixed quanta keep the trajectory independent of frame timing.
            bool useAdaptive = _effectiveWarp >= AdaptiveWarpThreshold && _simAccumulator >= AdaptiveWarpQuantum;
            double quantum = useAdaptive ? AdaptiveWarpQuantum : _simulator.TimeStep;

            // #146 split-advance: if a scheduled transfer burn epoch falls inside this quantum, advance
            // EXACTLY onto it first (the way Simulator.RunAdaptive lands on a ManeuverPlan node), so the
            // impulse is applied from the true drifted state — never from a state warped thousands of
            // sim-seconds past the epoch. A burn already due (epoch reached) fires this iteration with no
            // advance; otherwise the quantum is shortened to land on the epoch and the impulse follows.
            bool applyTransferBurnAfterStep = false;
            Vector2d pendingBurnDeltaV = default;
            if (_dockedHavenId is null && _armedOrbitBodyId is not null
                && _armedTransferSchedule is { } advSched && _armedTransferBurnsFired < advSched.Burns.Count)
            {
                TransferPlanner.BurnStep nextBurn = advSched.Burns[_armedTransferBurnsFired];
                double toBurn = nextBurn.SimTime - _ship.SimTime;
                if (toBurn <= 0)
                {
                    // Epoch already reached — apply the impulse now, from the current state, and re-loop
                    // (no clock advance this pass, so the accumulator is untouched; the next pass advances
                    // normally now that this burn has fired).
                    ApplyTransferBurn(nextBurn.DeltaV);
                    continue;
                }
                if (toBurn < quantum)
                {
                    quantum = toBurn; // land exactly on the burn epoch this step, then apply the impulse
                    applyTransferBurnAfterStep = true;
                    pendingBurnDeltaV = nextBurn.DeltaV;
                }
            }

            // #264: remember where this quantum started so a surface crossing can be caught across the
            // whole advance — the ship AND the body move, and SurfaceImpact interpolates both.
            Vector2d posBeforeStep = _ship.Position;
            double timeBeforeStep = _ship.SimTime;

            if (_dockedHavenId is not null)
            {
                // Clamped in a dock: don't run the gravity integrator at all — it would fling the
                // ship off the mass-less station each step, leaving HoldAtDock forever yanking it
                // back and the berth visibly wandering at warp. Advance the clock only; HoldAtDock
                // pins the position after the loop so the ship rides the dock, dead-steady.
                _ship = _ship with { SimTime = _ship.SimTime + quantum };
            }
            else if (useAdaptive || quantum < _simulator.TimeStep)
            {
                // Adaptive at warp, OR a shortened split step to land on a transfer burn epoch — either
                // way RunAdaptive lands exactly on the requested duration.
                _ship = _simulator.RunAdaptive(_ship, quantum, _plan);
            }
            else
            {
                // #264: StepGuarded, not Step — a deep, fast periapsis substeps so it stays energy-honest
                // instead of shedding km/s on integration error (the Uranus "flower"). Identical to Step
                // everywhere the pass isn't close and fast.
                _ship = _simulator.StepGuarded(_ship, _plan);
            }
            _simAccumulator -= quantum;
            stepsThisFrame++;

            // #264: the say-the-state law's missing consequence. If this integrated step actually reached
            // a body's surface radius, that is an impact — end the flight at the crossing (never having
            // flown the interior) through the shared BUSTED freeze-frame → clinic re-birth. Docked ships
            // took the clock-only branch above and havens carry no BodyRadius, so both are exempt.
            if (_dockedHavenId is null && _busted is null && _ephemeris is not null
                && SurfaceImpact.FirstCrossing(posBeforeStep, timeBeforeStep, _ship.Position, _ship.SimTime, _ephemeris)
                    is { } surfaceHit)
            {
                TriggerImpact(surfaceHit);
                _simAccumulator = 0;
                break; // the freeze-frame owns the moment; stop consuming the accumulator this frame
            }

            if (applyTransferBurnAfterStep)
            {
                ApplyTransferBurn(pendingBurnDeltaV); // impulse at the exact epoch (may loudly hand back)
            }
            if (watchDrag)
            {
                double decel = _simulator.DragAcceleration(_ship.Position, _ship.Velocity, _ship.SimTime).Length;
                if (decel > _frameMaxDragDecel)
                {
                    _frameMaxDragDecel = decel;
                }
            }
            if (recordTrail && _ship.SimTime - _pursuitTrail[^1].SimTime >= EncounterRule.HunterStepSeconds - 0.5)
            {
                _pursuitTrail.Add(new TrajectorySample(_ship.SimTime, _ship.Position));
            }
        }
        SimTime = _ship.SimTime;
        if (recordTrail && _pursuitTrail[^1].SimTime < _ship.SimTime)
        {
            _pursuitTrail.Add(new TrajectorySample(_ship.SimTime, _ship.Position));
        }

        // Clamped in a dock: the gravity integrator just coasted the ship off on its own arc, but a
        // berthed ship rides the station instead. Pin it back onto the dock at the new SimTime — this
        // is what lets you warp the heat away without steering (owner: "no guiding while docked").
        if (_dockedHavenId is not null)
        {
            HoldAtDock();
        }

        // M29: the fake beacon's ghost flies the abandoned course ballistically, kept in
        // step with the real clock — one extra body, integrated only while the lie is out.
        if (_beaconGhost is { } ghost && SimTime > ghost.SimTime)
        {
            _beaconGhost = _simulator.RunAdaptive(ghost, SimTime - ghost.SimTime);
        }

        if (stepsThisFrame > 0)
        {
            CheckSailHole(); // PR-I: a too-deep cloud-top dip holes the sail (before burns can fire)
            TrackAerobrakePass(); // #305: a completed haze pass rolls its 2D6 episode into the dice tray
            AccountForFiredNodes();
            if (_dockedHavenId is null)
            {
                CheckArmedInsertion(); // a clamped ship isn't flying an approach
            }
            CheckLockedFire();
        }

        StepNpcs();
        StepOrdnance();
        CheckPyramids();

        if (_ship.SimTime >= _nextSweepSimTime)
        {
            SweepSensors();
            _nextSweepSimTime = _ship.SimTime + SensorSweepSimSeconds;
        }

        UpdateDockStatus();
        UpdateDockAffordance(); // #212/#211/#213: recompute the one-truth ⚓ affordance (runs paused too)
        UpdateLandableInRange(); // #339-follow: cache which landable grounds the shuttle can reach now (map 🛬 bright state)
        UpdateOrbitedBody();
        UpdateCapture(dtRealSeconds);
        UpdateEncounters();
        UpdateLocalTrade(dtRealSeconds);
        // The archive node's two edges (walking into the field, walking to arm's length) BEFORE the nerve
        // step, so a throw forced by the approach is billed on the same tick the captain crossed the line.
        StepArchiveNode();
        StepNerve(dtRealSeconds); // #317: the nerve gauge advances every tick — regolith drains, the ship eases

        UpdatePrediction();

        if (_passDirty && highResTimestampMs - _lastReprojectMs > 300)
        {
            _passDirty = false;
            _closestPass = null;
            _armablePass = null;
            _destinationPass = null;
            _slingablePass = null;
            _skimmablePass = null;
            if (_ephemeris is not null)
            {
                double bestArmable = double.MaxValue;
                double bestSling = double.MaxValue;
                double bestSkim = double.MaxValue;
                foreach (ClosestApproach.Pass pass in ClosestApproach.Passes(_samples, _ephemeris))
                {
                    if (_closestPass is null || pass.Severity < _closestPass.Value.Severity)
                    {
                        _closestPass = pass;
                    }

                    // Armable = tightest pass by a PLANET, even when the sun ranks more severe.
                    if (PassIsOrbitable(pass) is not null && pass.Severity < bestArmable)
                    {
                        (bestArmable, _armablePass) = (pass.Severity, pass);
                    }

                    // Slingable = tightest planet pass inside the body's Hill sphere (a real flyby the
                    // crank can bend), even when it's too fast/far to orbit. PR-G's panel handle.
                    if (PassIsSlingable(pass) && pass.Severity < bestSling)
                    {
                        (bestSling, _slingablePass) = (pass.Severity, pass);
                    }

                    // Skimmable = tightest pass by an atmosphere-bearing body — PR-I's corridor gauge handle.
                    if (PassIsSkimmable(pass) && pass.Severity < bestSkim)
                    {
                        (bestSkim, _skimmablePass) = (pass.Severity, pass);
                    }

                    if (pass.BodyId == _destinationBodyId)
                    {
                        _destinationPass = pass;
                    }
                }

                // #246: the destination's OWN planet (the void mode stops at its capture range) and the
                // solved cheap DEPARTURE the offer quotes — recomputed on the reprojection cadence. The
                // departure solve (not the current-coast Project) is what the offer keys off, so the button
                // is reachable from a berth or any coast (#249 fix). The current-coast Project stays too, but
                // only for the manual-coast PROMISE verdict line ("does NOT reach — closest pass X AU").
                _longHaulPlanet = LongHaulTargetPlanet(_destinationBodyId); // null unless a real void to cross
                _longHaulReach = _longHaulPlanet is { } lhPlanet ? LongHaul.Project(_ship, _ephemeris, lhPlanet) : null;
                _longHaulDeparture = _longHaulPlanet is { } lhp2 ? LongHaul.SolveDeparture(_ship, _ephemeris, lhp2) : null;
                // #267: price the destination departure's surface-clearance verdict on THIS cadence (once,
                // not per render) so the chip/card offer gate reads it cheaply — the arc-sampling scan is too
                // heavy to run every frame.
                _longHaulClearanceBlock = _longHaulPlanet is { } lhp3 && _longHaulDeparture is { Ok: true } lhDep
                    ? LongHaulClearanceBlock(lhDep, lhp3)
                    : null;
            }

            UpdateInterceptEstimate(); // M27: the war room's clock rides the same recompute
            UpdateCourseOpportunities(); // M29: what does this course conveniently brush by?
        }

        if (_horizonDirty && highResTimestampMs - _lastHorizonReprojectMs > 250)
        {
            _horizonDirty = false;
            _lastHorizonReprojectMs = highResTimestampMs;
            ReprojectTrajectory();
        }

        if (_ship.SimTime >= _nextProjectionSimTime)
        {
            ReprojectTrajectory();
        }

        if (_pulseMessage is not null && highResTimestampMs > _pulseMessageExpiresMs)
        {
            _pulseMessage = null;
        }

        // Thunder on the rising edge of an arc (M10 polish) — once per arcing episode.
        bool arcing = _plasma is not null && _ship.Charge >= ArcChargeThreshold;
        if (arcing && !_wasArcing)
        {
            RendererInterop.PlayCue("arc");
        }
        _wasArcing = arcing;

        if (FollowShip)
        {
            _camera.CenterOn(_ship.Position);
        }

        if (_shuttleRun is not null)
        {
            // Guarded: an exception escaping a frame callback kills renderer.js's rAF chain
            // and silently freezes the whole game — degrade to aborting the run instead.
            try
            {
                UpdateShuttleRun(dtRealSeconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"shuttle update failed: {ex}");
                EndShuttleRun(boarded: false, $"Shuttle fault: {ex.GetType().Name}");
            }
            if (_shuttleRun is not null)
            {
                try
                {
                    _shuttleView!.Draw(_viewportWidth, _viewportHeight, SimTime, _shuttleRun,
                        _deckKeys.Contains("w"), _deckKeys.Contains("s"),
                        _deckKeys.Contains("a"), _deckKeys.Contains("d"),
                        _captureEngaged ? 1 : 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"shuttle draw failed: {ex}");
                    EndShuttleRun(boarded: false, $"Shuttle fault: {ex.GetType().Name}");
                }

                if (highResTimestampMs - _lastHudUpdateMs > 200)
                {
                    _lastHudUpdateMs = highResTimestampMs;
                    InvokeAsync(StateHasChanged);
                }
                return;
            }
        }

        if (_deckMode)
        {
            MoveAvatar(dtRealSeconds);
            StepSurface(dtRealSeconds); // #295/#313: dig channel, the Old Ones' converging chase, linger trickle
            DrawWalkFrame();

            if (_showScope && _scopeView is not null)
            {
                _scopeView.Draw(ScopeSizePx, SimTime, _ship.Position, _ship.Velocity, PickScopeTarget());
            }

            if (highResTimestampMs - _lastHudUpdateMs > 200)
            {
                _lastHudUpdateMs = highResTimestampMs;
                InvokeAsync(StateHasChanged);
            }
            return;
        }

        _camera.SetViewport(_viewportWidth, _viewportHeight);
        _renderer.BeginFrame(_viewportWidth, _viewportHeight, Background);

        // #135: re-anchor the co-moving plot frame to the frame body's CURRENT position, once per
        // frame. If the chosen body vanished (scenario reload), fall back to Sun/inertial.
        if (_plotFrameBodyId is not null && _ephemeris is not null)
        {
            if (_ephemeris.Bodies.Any(b => b.Id == _plotFrameBodyId))
            {
                _plotFrameAnchor = _ephemeris.Position(_plotFrameBodyId, SimTime);
            }
            else
            {
                _plotFrameBodyId = null;
            }
        }

        DrawStreams();
        if (LayerVisible("routes.lanes"))
        {
            // SundaySecondPlan PR-B, now layer-gated (#405 Routes → Trade lanes): lanes default ON
            // for the sensors chief and OFF everywhere else, and every desk can change its mind.
            DrawTradeCorridors();
        }
        DrawShipTrajectory();
        // #405 Routes → Flight plan & burns: the plotted autopilot path + its burn nodes (DrawNodeMarkers,
        // below). The ship's own live trajectory ribbon (DrawShipTrajectory, above) stays — that's the
        // nav essential, not part of the plan overlay.
        if (LayerVisible("routes.plan")) DrawAutopilotPlanPath();
        DrawPredictionCone();
        DrawPassEpochGhost();
        if (PlotMode)
        {
            DrawGhostBodies();
            DrawClosestPassMarker();
            DrawDestinationPassMarker();
        }
        RetireDeflectionIfDone(); // #394: a resolved gig clears once the crew is home at the saved port
        BeginFrameLabels();       // #402: reset the frame's de-collided label queue before the producers
        DrawCelestialBodies();
        DrawAsteroidThreat(); // #394: the inbound rock's rail + the ⚠ intersect + the threat line (bends on deflection)
        DrawCargoRunMarkers();
        if (LayerVisible("routes.plan")) DrawNodeMarkers(); // #405 Routes → Flight plan & burns (the burn nodes)
        if (PlotMode)
        {
            DrawGhostShip();
        }
        DrawNpcs();           // #402 follow-up: DEPOT name labels enqueue here, so the flush must follow it
        FlushNavLabels();     // #402: resolve overlapping body/threat/depot labels — priority wins, depots yield
        DrawHunters();
        DrawOrdnance();
        DrawPyramids();
        DrawShuttleRange();
        DrawBeaconGhost();
        if (_activeDesk == ShipDesk.Sensors)
        {
            // #405 Sensors family, split into two leaves: the active scan overlays (the wedge + the
            // pass flash) ride sensors.scans; the lost-contact search regions ride sensors.corridors.
            if (LayerVisible("sensors.scans"))
            {
                DrawScanWedge();
                DrawPassFlash();
            }
            if (LayerVisible("sensors.corridors"))
            {
                DrawLostSearchRegions();
            }
        }
        if (_activeDesk == ShipDesk.WarRoom)
        {
            // The orrery view: a cross-system shot's geometry on the live map behind the desk.
            DrawFirePlan();
        }
        if (_dockedHavenId is not null)
        {
            DrawDockArm();
        }
        DrawShip(_ship.Position);

        _renderer.EndFrame();

        if (_showScope && _scopeView is not null)
        {
            _scopeView.Draw(ScopeSizePx, SimTime, _ship.Position, _ship.Velocity, PickScopeTarget());
        }

        // #580 · THE SHIP'S VOICE DOES NOT REACH A CAPTAIN WHO IS NOT ABOARD HER. Owner, walking Miranda:
        // "in miranda here... why does the parrot talk about debt collectors now" / "we do not want any ship
        // type warnings received here on the surface ... that mechanic should not be active here" / "where
        // the player is not on empty ship".
        //
        // Right — the bird is on a perch on a ship that is docked and empty, and the captain is in a suit on
        // a moon. Everything below this line is the SHIP's channel: her alarm strip, her parrot, the long-
        // coast advert, the arrival-brake ask. None of it has a listener during an excursion, and squawking
        // it anyway does real damage: it drags the space fiction down onto the ground and buries the one
        // channel that IS live down there (air, tracker, nerve) under noise about somebody else's problem.
        //
        // Skipped wholesale rather than filtered, so nothing new added to the ship's side can leak down here
        // by forgetting to ask. On coming back aboard the detectors re-evaluate against live state, so a
        // condition that is still true announces itself then — which is when it can be acted on.
        if (_surface is null)
        {
            UpdateParrot(highResTimestampMs);
            UpdateShipAlerts(highResTimestampMs);
            EvaluateLongCoastAdvert(highResTimestampMs); // #172: next-event cache + long-coast squawk
            UpdateArrivalBrakeGate(highResTimestampMs);  // #304: the arrival-brake ask while the window is open
        }

        // M28: the CALCULATING FIRING SOLUTION reveal — one Newton iteration per beat.
        if (_fireSolution is { } fireSolution && _revealedIterations < fireSolution.Trace.Count
            && highResTimestampMs - _lastRevealMs > 250)
        {
            _lastRevealMs = highResTimestampMs;
            _revealedIterations++;
        }

        if (highResTimestampMs - _lastHudUpdateMs > 200)
        {
            _lastHudUpdateMs = highResTimestampMs;
            InvokeAsync(StateHasChanged);
        }
    }

    // The one walked-view paint — first person or the top-down deck — for whatever plan is welded on
    // right now. Pulled out of OnTick (#348) so the descent can render the FIRST surface frame once
    // under the still-up door (WarmFirstSurfaceFrameAsync): the cold DeckView.Draw of the enlarged
    // regolith is the last synchronous block that tripped Chrome's page-unresponsive dialog, and paying
    // it there — off the rAF loop, on its own yield — leaves the live loop warm.
    private void DrawWalkFrame()
    {
        if (_fpMode)
        {
            BuildSkyBodies();
            double deckWorldAngle = Math.Atan2(_ship.Velocity.Y, _ship.Velocity.X);
            _fpView!.Draw(_deckPlan, _viewportWidth, _viewportHeight, SimTime,
                _avatarX, _avatarY, _avatarHeading, deckWorldAngle, _skyBodies, LocationHint());
        }
        else
        {
            // #424 HULL-SHUDDER: a live tremor throws the whole frame a few pixels (added to the render pan,
            // never to an entity anchor) and — on the ship / a haven — freezes every patron in a unison held
            // breath (the frozen npc-hold time). Both are zero/null when no shudder is being felt.
            (double sdx, double sdy) = ShudderShakeOffset();
            _deckView!.Draw(_deckPlan, _viewportWidth, _viewportHeight, SimTime, new DeckView.State(
                _avatarX, _avatarY, _avatarHeading,
                _cargoUnits, _ship.Charge, ShuttleAway: _shuttleRun is not null, _plasma is not null,
                Docked: _dockedHavenId is not null && HavenInterior.HasInterior(_dockedHavenId),
                // #330: the nerve gauge rides every walk mode — full-size on the regolith, a compact
                // whisper aboard the ship or in a haven bar. (Flight never draws a DeckView, so it
                // stays gauge-free by construction.)
                Nerve: _nerve, NerveReadout: NerveModel.Readout(_nerve),
                ShowNerve: true, NerveCompact: _surface is null,
                // #453: the condition pips ride under the nerve bar, and only while skin is being counted —
                // off an excursion there is nothing to count, so they leave the corner entirely.
                HitsTaken: _surface?.HitsTaken ?? -1,
                // #480: the gauge never moves anonymously — the flash names the pip that just went, the
                // ledger keeps the last few so "what broke me?" has an answer after the fact.
                NerveFlash: LiveNerveFlash,
                NerveLedger: NerveLedgerLines),
                _deckPanX + sdx, _deckPanY + sdy, BuildSurfaceHud(), ShudderNpcHold(), SignalCrewGlancing());
        }
    }

    private void UpdateNearestBody()
    {
        double minDistanceSq = double.MaxValue;
        foreach (var body in _ephemeris!.Bodies)
        {
            if (IsBodyHidden(body.Id)) continue; // a hidden wreck is never "Nearest" until charted (PR-A)
            var bodyPos = _ephemeris.Position(body.Id, SimTime);
            double distSq = (_ship.Position - bodyPos).LengthSquared;
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                _nearestBody = body;
                _nearestBodyPosition = bodyPos;
            }
        }

        if (_nearestBody is not null)
        {
            // Same numeric derivative as the ship's initial state — can't disagree with the ephemeris.
            const double h = 1.0;
            _nearestBodyVelocity = (_ephemeris.Position(_nearestBody.Id, SimTime + h)
                                  - _ephemeris.Position(_nearestBody.Id, SimTime - h)) / (2 * h);
        }
    }

    private void UpdateEffectiveWarp()
    {
        // Clamped in a dock: the ship is held fast (HoldAtDock overrides the integrator), so there's
        // nothing to overshoot or collide with — warp freely. This is what makes lying low to bleed
        // off heat a quick fast-forward (heat cools ~5 sim-days/level at a haven) instead of an
        // hours-long crawl under the near-body warp cap.
        if (_dockedHavenId is not null)
        {
            _effectiveWarp = Warp;
            return;
        }

        // Bound to a planet (M20)? No encounter to overshoot — let the orbit spin at up to
        // 1000x instead of crawling on the near-body tiers.
        if (OrbitInfo() is { } orbitInfo
            && OrbitRule.IsBound(_ship, _nearestBodyPosition, _nearestBodyVelocity, orbitInfo.Body, orbitInfo.Hill))
        {
            _effectiveWarp = Math.Min(Warp, 1000);
            return;
        }

        if (_nearestBody == null)
        {
            _effectiveWarp = Warp;
            return;
        }

        // Absolute tiers with a body-radius floor so the Sun's huge radius still gets a sane
        // (small) zone while planets use encounter-scale distances. Pure BodyRadius multiples
        // don't work: ×5000 on the Sun caps warp across ~23 AU, i.e. the whole inner system.
        double distance = (_ship.Position - _nearestBodyPosition).Length;
        double encounterRadius = Math.Max(1e9, _nearestBody.BodyRadius * 30);   // ~3 lunar distances at Earth
        double closeRadius = Math.Max(1e8, _nearestBody.BodyRadius * 6);
        double grazingRadius = _nearestBody.BodyRadius * 3;

        int cap = int.MaxValue;
        if (distance < grazingRadius)
        {
            cap = 10;
        }
        else if (distance < closeRadius)
        {
            cap = 100;
        }
        else if (distance < encounterRadius)
        {
            cap = 1000;
        }

        _effectiveWarp = Math.Min(Warp, cap);

        // A live capture window is a close encounter by definition: cap warp so the 60 s window
        // is actually holdable. Selection alone doesn't cap — only an engaged window.
        NpcState? captureTarget = SelectedCaptureTarget();
        if (captureTarget is not null && CaptureRule.IsInWindow(_ship, captureTarget.State))
        {
            _effectiveWarp = Math.Min(_effectiveWarp, CaptureWarpCap);
        }

        // #136: a deep-well moon's parking band is only tens of km wide — far thinner than the
        // grazing-tier step at 10×. When armed for such a moon, cap warp so one tick advances only
        // a fraction of the distance still to close, easing to 1× right at the band the way the
        // 60 s unit test threads it. Inert for planets/roomy moons (band far outside the grazing
        // radius) and when not armed. Keyed off the nearest body, which IS the armed one on final.
        _effectiveWarp = Math.Min(_effectiveWarp, DeepWellInsertionWarpCap(distance));
    }

    // The warp ceiling that keeps an armed deep-well insertion holdable (issue #136). Returns
    // int.MaxValue (no cap) unless the ship is armed for the nearest body and that body is a deep
    // well whose whole parking band sits inside its grazing radius.
    private int DeepWellInsertionWarpCap(double distanceToNearest)
    {
        if (_armedOrbitBodyId is null || _ephemeris is null || _nearestBody is null
            || _armedOrbitBodyId != _nearestBody.Id || _nearestBody.ParentId is null)
        {
            return int.MaxValue;
        }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == _nearestBody.ParentId) { parent = candidate; break; }
        }
        if (parent is null) return int.MaxValue;

        double hill = OrbitRule.HillRadius(_nearestBody, parent.Mu);
        double park = OrbitRule.ParkingRadius(_nearestBody, hill);
        if (park >= _nearestBody.BodyRadius * 3 || distanceToNearest > OrbitRule.CaptureRange(hill))
        {
            return int.MaxValue; // roomy moon/planet, or not yet closing — the tiers suffice
        }

        // Advance at most ~⅓ of the room left to the band per 60 s tick; never below 1×. As the
        // ship reaches the band the room shrinks to a body radius and the cap eases to 1×.
        double closing = Math.Max(1.0, Math.Abs(OrbitRule.ClosingSpeed(_ship, _nearestBodyPosition, _nearestBodyVelocity)));
        double room = Math.Max(distanceToNearest - park, _nearestBody.BodyRadius);
        return Math.Max(1, (int)(room / (3 * 60 * closing)));
    }

    // Plasma stream ribbons (M7): one translucent wide segment per stream, between the two
    // endpoint bodies at the current sim time. Drawn first so everything else layers on top.
    // No-op outside an Electric Universe scenario.
    private void DrawStreams()
    {
        if (_plasma is null) return;

        // Drawn as flowing filaments, not one flat band — a single thick polyline read as "a
        // strange rectangle" (owner report). Four narrow ribbons undulate along the axis with
        // sim-time phase; alpha fades toward the edges.
        Span<float> pts = stackalloc float[34];
        foreach ((string fromId, string toId, double halfWidth) in _plasma.Streams)
        {
            Vector2d a = _ephemeris!.Position(fromId, SimTime);
            Vector2d b = _ephemeris.Position(toId, SimTime);
            Vector2d axis = b - a;
            double len = axis.Length;
            if (len <= 0) continue;
            Vector2d dir = axis / len;
            Vector2d perp = new(-dir.Y, dir.X);

            for (int ribbon = 0; ribbon < 4; ribbon++)
            {
                double lane = (ribbon - 1.5) / 1.5;              // -1 … 1 across the width
                double phase = SimTime * 4e-7 + ribbon * 1.7;
                for (int k = 0; k <= 16; k++)
                {
                    double t = k / 16.0;
                    double wobble = Math.Sin(t * 9.0 + phase) * 0.25;
                    Vector2d world = a + dir * (len * t) + perp * (halfWidth * (lane * 0.8 + wobble));
                    (float sx, float sy) = _camera.WorldToScreen(world);
                    pts[k * 2] = sx;
                    pts[k * 2 + 1] = sy;
                }
                byte alpha = (byte)(30 - 12 * Math.Abs(lane));
                float widthPx = (float)Math.Clamp(halfWidth * 0.5 / _camera.MetersPerPixel, 1, 60);
                _renderer!.DrawPolyline(pts, new RgbaColor(80, 220, 220, alpha), widthPx);
            }
        }
    }

    // #351 — the audit's keyboard cancel path: dismiss the top-most open deck/flight overlay, reusing
    // each card's existing house closer (a ✕/Cancel/Done button already lives on every one of these). The
    // order is most-modal first, so a stacked moment (a contact-drink offer sitting atop the bar menu)
    // peels one layer at a time. Deliberately EXCLUDED: the shuttle boarding panel (_boardTarget — another
    // lane is reworking it) and the save/start drawers (the scenario-starts region keeps its own chrome).
    // Returns true when it consumed the key by closing something.
    private bool TryDismissTopOverlay()
    {
        // #528: the story plate is the most modal thing there is — it opens without being asked for, over
        // whatever the captain was already doing (a bar menu, a counter, a dig). Esc takes it FIRST, or the
        // key would peel the card underneath it and leave the picture sitting there.
        if (_storyPlate is not null) { CloseStoryPlate(); return true; }
        if (_pendingContactDrink is not null) { CancelContactDrinkOffer(); return true; }
        if (_patronDrink is not null) { ClosePatronTable(); return true; }
        if (_pendingOffer is not null) { DeclineOffer(); return true; }
        if (_bankSession is not null) { CloseBank(); return true; }
        if (_barMenu is not null) { CloseBarkeep(); return true; }
        // #425 · The oracle's corner card was the ONE bar card this chain never knew about (story pass
        // 2026-08-02). She belongs to the same mutually-exclusive doorway family as the counter and the
        // patron's table — both of which open by shutting her — so Esc peeled every card in the bar except
        // hers, which sat there ignoring the key while everything else obeyed it. Her ✕ was always the
        // "Done" button; this just lets the house key close her too (#351's family).
        if (_oracleOpen) { CloseOracle(); return true; }
        if (_shuttleBayStops is not null) { CloseShuttleBayDoor(); return true; }
        if (_pinJob is not null) { CancelPin(); return true; }
        if (_expeditionRevealCard is not null) { _expeditionRevealCard = null; return true; }
        if (_expeditionBriefCard is not null) { _expeditionBriefCard = null; return true; }
        if (_treasureMapCard is not null) { _treasureMapCard = null; return true; }
        // #488: the operating-log card sits ON TOP of the valve board, so Esc must take it first — the
        // board underneath is still the thing the captain came to use.
        if (_ventReadCard is not null) { CloseVentReadCard(); return true; }
        if (_showVentPanel) { CloseVentPanel(); return true; }
        // The vision card sits above everything on a wreck — it is the loudest thing that can happen in that
        // hold, and it opens without being asked for.
        if (_archiveCard is not null) { CloseArchiveCard(); return true; }
        if (_wreckLook is not null) { CloseWreckLook(); return true; }
        if (_wreckOutcome is not null) { DismissWreckOutcome(); return true; }
        if (_showWreckChoice) { CloseWreckChoice(); return true; }
        if (_kioskCard is not null) { CloseKioskCard(); return true; }
        if (_viewObject is not null) { CloseViewObject(); return true; }
        if (_showRescueOffer) { _showRescueOffer = false; return true; }
        if (_celebration is not null) { DismissCelebration(); return true; }
        return false;
    }

    // #338 addendum · THE GAME'S FIRST SOUND — the master audio switch (default ON, remembered browser-side
    // in JS). _audioArmed also does double duty as item-4's gesture unlock: the first keypress of the
    // session both arms the WebAudio context (so a chirp fired later from the rAF loop can sound) and syncs
    // our on/off label from the remembered pref.
    private bool _audioEnabled = true;
    private bool _audioArmed;

    private void ToggleAudio()
    {
        _audioEnabled = !_audioEnabled;
        RendererInterop.SetAudioEnabled(_audioEnabled);
        ShowPulseMessage(_audioEnabled
            ? "🔊 Sound on — the tracker will chirp on first contact."
            : "🔇 Sound muted. (Press M to bring it back.)");
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        // #338 addendum item 4: unlock audio on the first keypress and adopt the remembered mute pref, so a
        // chirp fired later from the render loop isn't silently blocked.
        if (!_audioArmed)
        {
            _audioArmed = true;
            RendererInterop.ArmAudio();
            _audioEnabled = RendererInterop.GetAudioEnabled();
        }

        if (_shuttleRun is not null)
        {
            switch (e.Key)
            {
                case "w" or "W" or "ArrowUp" or "a" or "A" or "ArrowLeft"
                    or "s" or "S" or "ArrowDown" or "d" or "D" or "ArrowRight":
                    _deckKeys.Add(Canonical(e.Key));
                    return;
                case "q" or "Q":
                    EndShuttleRun(boarded: false, "Boarding run aborted — shuttle back in the cradle");
                    return;
                default:
                    return;
            }
        }

        // Desk switching (StationDesks.md rule 3): number keys 1-7 always win, even mid-deck-walk
        // (7 re-enters/toggles deck, 1-6 leave it) — checked before HandleDeckKey so WASD/E/F/Q
        // never shadow them, and before the pulse switch below so digits never fire a burn.
        // Inputs/sliders already stop propagation on their own keydown (see the plot panel's
        // range/number fields), so typing into them never reaches this handler at all.
        if (e.Key.Length == 1 && e.Key[0] is >= '1' and <= '7')
        {
            var deskKey = (ShipDesk)(e.Key[0] - '0');
            // #330: ashore, a desk shortcut can't silently yank the captain off the regolith — the desks
            // are a tube ride up. Deck (7) is where they already stand, so it stays a no-op switch.
            if (_surface is not null && deskKey != ShipDesk.Deck)
            {
                ShowPulseMessage("🧭 The nav desk is a tube ride away, captain — board the shuttle to get back to it.");
                return;
            }
            SwitchDesk(deskKey);
            return;
        }

        // PR-15: the captain's position is key `0` — same digit-key rules as 1-7 above (wins
        // mid-deck-walk, checked before HandleDeckKey/the pulse switch).
        if (e.Key == "0")
        {
            if (_surface is not null)
            {
                ShowPulseMessage("🧭 The captain's desk is a tube ride away — board the shuttle first.");
                return;
            }
            SwitchDesk(ShipDesk.Captain);
            return;
        }

        if (e.Key == "Escape")
        {
            // #351 (owner 2026-07-18: "No way to close this dialog? Where is cancel?") — Escape is the
            // keyboard CANCEL for the deck/flight cards: close the top-most open overlay first (reusing
            // each card's own house closer), and only fall through to the helm when nothing's open to
            // dismiss. Without this, Escape over an open offer card yanked the captain off the deck to Nav.
            if (TryDismissTopOverlay())
            {
                StateHasChanged();
                return;
            }
            // Ashore, Escape doesn't switch desks (that would leave the surface silently) — let it fall
            // through to nothing rather than yanking the captain up the tube.
            if (_surface is null)
            {
                SwitchDesk(ShipDesk.Nav);
            }
            return;
        }

        // Owner request: ` peeks at the map — hide every panel to read the sky, tap again to
        // restore. Works on any desk; the desk tab bar (and this key) bring the panels back.
        if (e.Key is "`" or "~")
        {
            TogglePeekMap();
            return;
        }

        // #338 addendum: M mutes/unmutes all sound (the first-contact chirp and every cue). Global — the
        // audio switch is not a surface-only affordance.
        if (e.Key is "m" or "M")
        {
            ToggleAudio();
            return;
        }

        // #406: `/` opens the Nav search box and hands it the keyboard — type a name to find & jump to a
        // target instead of zoom-hunting. Only on the desks that render the solar map (where the box
        // lives — the same Nav/Sensors/WarRoom gate). The box's own keydown stops propagation, so once
        // it has focus the typed keys never reach this handler to drive the ship.
        if (e.Key == "/" && _surface is null && _activeDesk is ShipDesk.Nav or ShipDesk.Sensors or ShipDesk.WarRoom)
        {
            _ = FocusNavSearch();
            return;
        }

        if (_deckMode && HandleDeckKey(e.Key))
        {
            return;
        }

        bool pulse = false;
        double factor = 1.0;

        if (e.Key is "o" or "O")
        {
            EnterOrbit();
            return;
        }


        // Shift = fine trim (±1%) for orbital finesse near planets; plain = the full ±10%.
        bool fine = e.ShiftKey;
        switch(e.Key)
        {
            case "+":
            case "=":
            case "ArrowUp":
                factor = fine ? 1.01 : ManeuverPlan.AccelerateFactor;
                pulse = true;
                break;
            case "-":
            case "_":
            case "ArrowDown":
                factor = fine ? 0.99 : ManeuverPlan.DecelerateFactor;
                pulse = true;
                break;
            case "p":
            case "P":
                TogglePlotMode();
                return;
            case "v":
            case "V":
                VentCharge();
                return;
        }

        if (pulse)
        {
            // PR-I: a holed sail can't thrust — the crew is still sewing (fires until the repair window closes).
            if (_sailHoled)
            {
                double daysLeft = Math.Max(0, (_sailRepairedAtSimTime - _ship.SimTime) / 86400.0);
                ShowPulseMessage($"Sail holed — no drive while the crew sews (~{daysLeft:F1} d)");
                return;
            }

            // Firing the drive breaks the clamps — you can't burn while bolted to a dock.
            if (_dockedHavenId is not null)
            {
                Undock();
            }

            if (_reactionMassPulses <= 0)
            {
                ShowPulseMessage("Out of reaction mass");
                return;
            }
            if (_ship.SimTime < _lastPulseSimTime + PulseCooldownSeconds)
            {
                ShowPulseMessage("Pulse drive cooling down…");
                return;
            }

            _ship = _ship with { Velocity = _ship.Velocity * factor };
            _reactionMassPulses--;
            _lastPulseSimTime = _ship.SimTime;
            ShowPulseMessage(factor > 1
                ? (fine ? "Trim: +1%" : "Pulse: accelerate +10%")
                : (fine ? "Trim: −1%" : "Pulse: decelerate −10%"));
            RendererInterop.PlayCue("pulse");

            // A live override invalidates every still-pending node (plan §4).
            bool anyStaled = false;
            foreach (PlanNode node in _planNodes)
            {
                if (!node.Stale && !node.Executed && node.SimTime > _ship.SimTime)
                {
                    node.Stale = true;
                    anyStaled = true;
                }
            }

            if (anyStaled)
            {
                RebuildPlan();
                ShowPulseMessage("Plan invalidated downstream");
            }

            ReprojectTrajectory();
        }
    }

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
            ShowPulseMessage("Vent recharging…");
            return;
        }

        _lastVentSimTime = _ship.SimTime;
        _ship = _ship with { Charge = _ship.Charge * 0.5 };
        // #369: the vent is automatic here, so each discharge reads a rotating flavor quip
        // (house voice) rather than a bare status line. Deterministic per vent via the counter.
        ShowPulseMessage(StaticCharge.LineFor(_ventLineSeed++));
        RendererInterop.PlayCue("vent");
    }

    private void ShowPulseMessage(string message)
    {
        _pulseMessage = message;
        // Owner 2026-07-18 ("it autodisappears which is not convenient"): a line lingers long enough to
        // READ — the dwell scales with its length (≈45 ms/char) so the words a player paid a round to
        // hear aren't gone before they land. Short status pulses keep the old brisk 1.5 s floor; long
        // intel lines get up to ~8 s. (The durable "overheard" book is the real record; this is the doorbell.)
        double dwell = Math.Clamp((message?.Length ?? 0) * 45.0, 1500.0, 8000.0);
        _pulseMessageExpiresMs = (_lastTimestampMs ?? 0) + dwell;
    }
    private bool _dragMoved;
    private double _downClientX, _downClientY;

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

    private int WarpSliderValue => (int)Math.Round(Math.Log10(Math.Clamp(Warp, 1, 10000)) * 25);

    private void OnWarpSliderInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int t))
        {
            SetWarp((int)Math.Round(Math.Pow(10, t / 25.0)));
        }
    }

    private void SetWarp(int level)
    {
        // #172: the captain's hand on the warp slider wins — cancel any skip (keep the level they chose).
        if (_skipActive)
        {
            _skipActive = false;
            LogAutopilotEvent("⏭ skip stopped — the captain set the warp");
        }
        PlotMode = false;
        Warp = level;
        Paused = false;
    }

    // Unpausing from inside plot mode is "press play": leave plotting properly (restores warp)
    // instead of running the sim with the plot card still open.
    private void TogglePause()
    {
        StopSkip(); // #172: pausing is the captain's hand — let go of any skip first.
        if (PlotMode && Paused)
        {
            ExitPlotMode();
            return;
        }

        Paused = !Paused;
    }

    private void ToggleFollow() => FollowShip = !FollowShip;

    private void OnWheel(WheelEventArgs e)
    {
        double factor = e.DeltaY > 0 ? 1.15 : 1 / 1.15;
        _camera.ZoomBy(factor, e.OffsetX, e.OffsetY);
    }

    // #237 — the wheel-free zoom: one REAL step per press (×1.6, vs the wheel's 1.15 crawl),
    // toward the viewport centre so the button never yanks the view sideways.
    private void ZoomStep(bool zoomIn) =>
        _camera.ZoomBy(zoomIn ? 1 / 1.6 : 1.6, _viewportWidth / 2.0, _viewportHeight / 2.0);

    private void OnPointerDown(PointerEventArgs e)
    {
        // A click that only dismisses an open menu must not immediately open the next one.
        _suppressClickMenu = _bodyMenuBody is not null || _shipMenuId is not null
            || _corridorMenuLane is not null || _skyMenuWorld is not null || _pickMenu is not null;

        if (_bodyMenuBody is not null)
        {
            CloseBodyMenu(); // any click on the map dismisses an open planet menu
        }

        if (_shipMenuId is not null)
        {
            CloseShipMenu(); // same rule for the contact menu
        }

        if (_corridorMenuLane is not null)
        {
            CloseCorridorMenu();
        }

        if (_skyMenuWorld is not null)
        {
            CloseSkyMenu();
        }

        if (_pickMenu is not null)
        {
            ClosePickMenu();
        }

        if (TrySelectNodeAt(e.OffsetX, e.OffsetY))
        {
            return; // clicked a thrust node: select it, don't start a drag
        }

        // The unified picker: one candidate under the click acts directly (old behavior); a
        // stack of neighbors opens the chooser instead of silently taking the topmost.
        List<PickCandidate> picks = CollectPointCandidates(e.OffsetX, e.OffsetY, PickRadiusPx);
        if (picks.Count == 1)
        {
            OpenPickCandidateAt(picks[0], e.OffsetX, e.OffsetY);
            return;
        }

        if (picks.Count > 1)
        {
            OpenPickMenu(picks, e.OffsetX, e.OffsetY);
            return;
        }

        _dragging = true;
        _dragMoved = false;
        _lastPointerX = e.ClientX;
        _lastPointerY = e.ClientY;
        _downClientX = e.ClientX;
        _downClientY = e.ClientY;
    }

    private void OnPointerMove(PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        double dx = e.ClientX - _lastPointerX;
        double dy = e.ClientY - _lastPointerY;
        _lastPointerX = e.ClientX;
        _lastPointerY = e.ClientY;
        if (Math.Abs(e.ClientX - _downClientX) + Math.Abs(e.ClientY - _downClientY) > 5)
        {
            _dragMoved = true; // a real pan, not a click with hand tremor
        }

        // In the top-down deck view the drag moves the DECK plan (its bow hides under the HUD
        // panel otherwise); in first person and on the map it pans the camera as before.
        if (_deckMode && !_fpMode)
        {
            _deckPanX += dx;
            _deckPanY += dy;
            return;
        }

        _camera.PanByPixels(dx, dy);
        FollowShip = false; // manual pan disengages follow-ship, same as most space-game maps.
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        // SundaySecondPlan PR-C: on the Sensors desk, EMPTY sky answers a click too — but only
        // a genuine click (no pan movement, and not the click that dismissed another menu).
        bool click = _dragging && !_dragMoved && !_suppressClickMenu;
        _dragging = false;
        if (!click || _activeDesk != ShipDesk.Sensors || _deckMode)
        {
            return;
        }

        // Near-miss forgiveness + the owner's rule that a lane is the LEAST likely meaning
        // near anything else: gather what sits within the loose radius; the lane and the
        // empty-sky scan join the chooser at the bottom.
        List<PickCandidate> near = CollectPointCandidates(e.OffsetX, e.OffsetY, PickNearRadiusPx);
        CorridorRegion? lane = LayerVisible("routes.lanes") ? CorridorAt(e.OffsetX, e.OffsetY) : null; // #405 Routes → Trade lanes
        if (near.Count == 0)
        {
            if (lane is { } directLane)
            {
                OpenCorridorMenuFor(CorridorKey(directLane), e.OffsetX, e.OffsetY);
                return;
            }

            OpenSkyMenu(e.OffsetX, e.OffsetY);
            return;
        }

        if (lane is { } nearLane)
        {
            near.Add(new PickCandidate('C', CorridorKey(nearLane), nearLane.Name, "🛣"));
        }

        near.Add(new PickCandidate('K', "", "scan this patch of sky", "🔭"));
        OpenPickMenu(near, e.OffsetX, e.OffsetY);
    }

    // Blazor re-renders the whole page after EVERY event by default; a held movement key
    // repeats ~30 events/s and collapsed the frame rate to ~1.5 fps (all M12/M13 scripted
    // walks came up short because of this). The game's HUD refresh is owned by OnTick's
    // 200 ms throttle, so events here run WITHOUT triggering automatic re-renders.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) =>
        callback.InvokeAsync(arg);

    public void Dispose()
    {
        RendererInterop.FrameTick -= OnTick;
        RendererInterop.CanvasResized -= OnCanvasResized;

        if (_started)
        {
            RendererInterop.StopLoop(CanvasId);
        }
    }

    private void CenterShipOnMap() => FollowShip = true;

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
