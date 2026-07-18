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

    private readonly Camera _camera = new();
    private CanvasRenderer? _renderer;
    private ICelestialEphemeris? _ephemeris;
    private Simulator? _simulator;
    private PlasmaEnvironment? _plasma;
    private string _scenarioName = "";
    private ShipState _ship;
    private bool _started;
    private bool _worldReady;

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
        }

        string json = await Http.GetStringAsync($"scenarios/{scenarioName}.json");
        ScenarioDefinition scenario = ScenarioLoader.Parse(json);
        if (ellipseCheat)
        {
            scenario = scenario with { Bodies = [.. scenario.Bodies, KeplerDemoBody()] };
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
        // few seconds of planning work; yield first so the initial (empty) map paints and stays
        // responsive, then build the NPC live-state wrappers.
        await Task.Yield();
        // Pods first so "the Luna pod" is the top of the board and the obvious tutorial prey.
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(_ephemeris, seed: 43, count: 3);
        IReadOnlyList<NpcShip> traffic = TrafficSchedule.Generate(_ephemeris, seed: 42, count: 8);
        // The derelict roadster is a dead wreck, not a trading post — it's a station body only so its
        // map label reads at a sane zoom (the fetch-mission target). Drop the depot GenerateDepots
        // would otherwise hang on it (any sun-orbiting body gets one).
        // A depot on a hidden body would leak it (the depot marker/menu would give the wreck away).
        // Filter generically on hidden+unrevealed, not the wreck's id — every future secret body is
        // covered for free (Tuesday plan PR-A).
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
            IReadOnlyList<NpcShip> lunaDriver = MassDriverSchedule.GenerateCadence(
                _ephemeris, MassDriverSchedule.MassDriverRun.LunaMilkRun(), baseSimTime: _ship.SimTime, count: 4);

            // The pod is the first hunt's soft catch, placed relative to the player so the hunt is
            // always deliverable (see StarterPod). The second hunt's stubborn Lark is NOT seeded here
            // — she's spawned when the first hunt ends (SeedSecondHuntTarget), co-moving with the
            // player's state then, so her escape jink is never stale from a slow first hunt.
            initial = new[] { TrafficSchedule.StarterPod(_ship) }.Concat(lunaDriver).Concat(initial);
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

    // --- Start points (2026-07-08) ---
    // "Why should it always start from Earth?" Named starts that jump the just-built world to a
    // locale — either as a playtest shortcut (skip the long haul to test the interesting bit) or as a
    // genuine choice of where a run begins. One registry feeds both the /map?start=<id> URL and the
    // boot picker overlay. The heavy lifting mirrors the tutorial Seed* pattern: place the ship (and,
    // for a docked berth, clamp and step ashore), then frame the camera. Body ids are scenario data.
    // Test:true starts are reachable by /map?start=<id> but hidden from the boot picker — dev-only
    // jumps for exercising a mission stage without the flight to it.
    private sealed record StartPoint(string Id, string Icon, string Label, string Blurb, bool Test = false);

    private static readonly StartPoint[] StartPoints =
    [
        new("earth", "🌍", "Earth — the usual berth",
            "The standard opening: fresh out of Earth orbit, last run's takings in the purse and hold."),
        new("cinder-roost", "🌋", "Cinder Roost — docked",
            "In Venus' sulphur clouds — begin already clamped on at Cinder Roost, a short walk up the tube to The Cinder Lounge."),
        new("space-bar", "🍸", "The Rusty Roadstead — docked",
            "Skip the haul to Mars — begin already clamped on at The Rusty Roadstead, a short walk up the tube to the bar's tables."),
        new("jupiter", "🪐", "Jupiter — the Galilean moons",
            "Out at Jupiter, co-moving beside Europa — fly Ganymede and Callisto without the long cruise from the inner system."),
        new("saturn", "🌙", "Saturn — the moons",
            "Among Saturn's moons by the Ringside Exchange — Enceladus and Titan a short burn away."),
        new("ringside", "💍", "Ringside Exchange — docked",
            "In Saturn's rings — begin already clamped on at Ringside Exchange, a short walk up the tube to The Ringside Bar."),
        new("the-tilt", "❄️", "The Tilt — docked",
            "Way out at Uranus — begin already clamped on at The Tilt, a short walk up the tube to its cold, lonely bar."),
        new("wreck", "🚗", "The Derelict Roadster — alongside (test)",
            "Co-moving beside the lost roadster, sunward of Mars — for testing the fetch pickup.", Test: true),
        new("enceladus", "❄️", "Enceladus — alongside (test)",
            "Co-moving beside Enceladus, a short fall from its capture band — for testing the deep-well auto-orbit (#136).", Test: true),
    ];

    private bool _showStartPicker;

    // Arrange the just-built (or, on a picker reopen, already-running) world for a chosen start.
    // Re-entrant: steps aboard and unclamps any current berth first, so it's safe to call any time.
    private void ApplyStart(string id)
    {
        _dockedHavenId = null;   // drop any prior clamp before the jump
        SetDeckForDock(null);    // back to the bare ship deck (pulls you aboard if you'd wandered ashore)

        _ship = PlaceShipForStart(id);
        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);

        // The tutorial checklist is Earth-anchored ("the Luna pod", "dock at Earth and sell") —
        // riding along to Saturn it's just noise (owner, first Saturn milk-run playtest). Any
        // non-Earth start hides it; the Captain's Tutorials tab reopens a lesson deliberately
        // (StartTutorial reseeds and re-shows), so nothing is lost, only misplaced.
        if (id != "earth")
        {
            _showTutorial = false;
        }

        // A docked locale: clamp onto the berth (the tick's HoldAtDock then pins the ship to the
        // station's drift), weld on the walk-through complex, and drop the avatar aboard by the gangway
        // facing the tube — a couple of steps from walking straight across into the bar.
        if (DockedStarts.TryGetValue(id, out string? dockBody))
        {
            Vector2d dockPos = _ephemeris!.Position(dockBody, 0);
            _dockedHavenId = dockBody;
            _dockOffset = _ship.Position - dockPos;
            SetDeckForDock(dockBody);
            (_avatarX, _avatarY, _avatarHeading) = (2.5, 6, Math.PI / 2); // in the airlock corridor, facing up the tube
            _deckMode = true;
            _activeDesk = ShipDesk.Deck;
        }
        else
        {
            // A free-flying locale (Earth, Jupiter, Saturn): leave any ashore/deck view we jumped FROM
            // so the ship map actually shows (mattered when the picker was reopened while docked — the
            // ship moved but the deck stayed on screen). ChooseStart then brings up the Nav desk.
            _deckMode = false;
        }
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

    // Boot-picker (or Captain-desk reopen) choice: dismiss the overlay and jump to the locale. Always
    // ApplyStart — even "Earth": at boot the world is already there so it's a no-op re-place, but when
    // the picker is reopened mid-run (e.g. while docked at a Venus bar) Earth is a genuine jump that
    // has to unclamp and move the ship. A docked start lands you ashore on the deck; any other locale
    // drops you on the Nav map so the jump is actually visible. Focus returns so keys drive ship/walk.
    private async Task ChooseStart(string id)
    {
        _showStartPicker = false;
        ApplyStart(id);
        if (!_deckMode && _activeDesk != ShipDesk.Nav)
        {
            SwitchDesk(ShipDesk.Nav);
        }
        StateHasChanged();
        await _focusableDiv.FocusAsync();
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
        UpdateOrbitedBody();
        UpdateCapture(dtRealSeconds);
        UpdateEncounters();
        UpdateLocalTrade(dtRealSeconds);

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
            if (_fpMode)
            {
                BuildSkyBodies();
                double deckWorldAngle = Math.Atan2(_ship.Velocity.Y, _ship.Velocity.X);
                _fpView!.Draw(_deckPlan, _viewportWidth, _viewportHeight, SimTime,
                    _avatarX, _avatarY, _avatarHeading, deckWorldAngle, _skyBodies, LocationHint());
            }
            else
            {
                _deckView!.Draw(_deckPlan, _viewportWidth, _viewportHeight, SimTime, new DeckView.State(
                    _avatarX, _avatarY, _avatarHeading,
                    _cargoUnits, _ship.Charge, ShuttleAway: _shuttleRun is not null, _plasma is not null,
                    Docked: _dockedHavenId is not null && HavenInterior.HasInterior(_dockedHavenId)),
                    _deckPanX, _deckPanY);
            }

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
        if (LayerVisible("lanes"))
        {
            // SundaySecondPlan PR-B, now layer-gated: lanes default ON for the sensors chief
            // and OFF everywhere else, and every desk can change its mind in 🗺 Layers.
            DrawTradeCorridors();
        }
        DrawShipTrajectory();
        DrawAutopilotPlanPath();
        DrawPredictionCone();
        DrawPassEpochGhost();
        if (PlotMode)
        {
            DrawGhostBodies();
            DrawClosestPassMarker();
            DrawDestinationPassMarker();
        }
        DrawCelestialBodies();
        DrawCargoRunMarkers();
        DrawNodeMarkers();
        if (PlotMode)
        {
            DrawGhostShip();
        }
        DrawNpcs();
        DrawHunters();
        DrawOrdnance();
        DrawPyramids();
        DrawShuttleRange();
        DrawBeaconGhost();
        if (_activeDesk == ShipDesk.Sensors && LayerVisible("scans"))
        {
            DrawScanWedge();
            DrawLostSearchRegions();
            DrawPassFlash();
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

        UpdateParrot(highResTimestampMs);
        UpdateShipAlerts(highResTimestampMs);
        EvaluateLongCoastAdvert(highResTimestampMs); // #172: refresh the next-event cache + long-coast squawk

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

    private void OnKeyDown(KeyboardEventArgs e)
    {
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
            SwitchDesk((ShipDesk)(e.Key[0] - '0'));
            return;
        }

        // PR-15: the captain's position is key `0` — same digit-key rules as 1-7 above (wins
        // mid-deck-walk, checked before HandleDeckKey/the pulse switch).
        if (e.Key == "0")
        {
            SwitchDesk(ShipDesk.Captain);
            return;
        }

        if (e.Key == "Escape")
        {
            SwitchDesk(ShipDesk.Nav);
            return;
        }

        // Owner request: ` peeks at the map — hide every panel to read the sky, tap again to
        // restore. Works on any desk; the desk tab bar (and this key) bring the panels back.
        if (e.Key is "`" or "~")
        {
            TogglePeekMap();
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
        ShowPulseMessage("Venting charge");
        RendererInterop.PlayCue("vent");
    }

    private void ShowPulseMessage(string message)
    {
        _pulseMessage = message;
        _pulseMessageExpiresMs = (_lastTimestampMs ?? 0) + 1500;
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
        CorridorRegion? lane = LayerVisible("lanes") ? CorridorAt(e.OffsetX, e.OffsetY) : null;
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
