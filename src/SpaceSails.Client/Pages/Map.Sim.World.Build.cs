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

// Subject: part of Map.Sim.World (#870 lane 7a; the header note lives in Map.Sim.World.cs) — the stages that BUILD the world: the scenario, the bodies the cheats hang off it, the ephemeris, the ship, the traffic, the camera and the renderer.
public partial class Map
{

    /// <summary>The one fetch the boot waits on: <c>scenarios/&lt;name&gt;.json</c>, parsed.</summary>
    private async Task<ScenarioDefinition> FetchTheScenarioAsync(BootQuery q, CancellationToken abandoned)
    {
        string json = await Http.GetStringAsync($"scenarios/{q.ScenarioName}.json", abandoned);
        ScenarioDefinition scenario = ScenarioLoader.Parse(json);
        return scenario;
    }

    /// <summary>Everything a <c>?query</c> hangs off the berth before the ephemeris is built. The order
    /// is the order the cheats are written in, and it is load-bearing: each of these may overwrite
    /// <c>q.DockCheat</c> with the berth its own rock co-orbits, and the last one to do so wins.</summary>
    private ScenarioDefinition AppendTheBodiesTheCheatsAskFor(ScenarioDefinition scenario, BootQuery q)
    {
        if (q.EllipseCheat)
        {
            scenario = scenario with { Bodies = [.. scenario.Bodies, KeplerDemoBody()] };
        }
        if (q.ExpeditionCheat is not null)
        {
            // #370: the site is a mission-spawned body, and the ephemeris is immutable once built — so the
            // ONLY clean way to a runtime LANDABLE rock is to append it to the scenario BEFORE FromScenario
            // (the ellipse-cheat idiom). Park it co-orbiting the berth we'll clamp onto, a fixed small hop
            // off, so it is always in shuttle range. Default the berth to Selene Gate when none was asked.
            string berthKey = q.DockCheat ?? "selene-gate";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? mappedBerth) ? mappedBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                ExpeditionFlavor flavor = q.ExpeditionCheat == "mining" ? ExpeditionFlavor.MiningSurvey : ExpeditionFlavor.Science;
                // Position/velocity are unused here (the rock rides real orbit rails); Spawn only fixes the
                // seeded KIND + display name deterministically.
                SiteSpawn spawn = ExpeditionSite.Spawn(ExpeditionCheatSeed(flavor), Vector2d.Zero, Vector2d.Zero, flavor);
                scenario = scenario with { Bodies = [.. scenario.Bodies, ExpeditionSiteBody(spawn, berthId)] };
                _pendingExpeditionCheat = (flavor, spawn.Kind, spawn.BodyId, spawn.DisplayName);
                q.DockCheat = berthId; // clamp onto the berth the rock co-orbits, so it's in reach at spawn
            }
        }
        if (q.DeflectionCheat is not null
            && scenario.Bodies.FirstOrDefault(b => b.Id == "ringside-exchange") is { } ring
            && scenario.Bodies.Any(b => b.Id == ring.ParentId))
        {
            // #394: the inbound rock is a mission-spawned body on a real COLLISION rail with the Ringside
            // Exchange — appended before FromScenario (the immutable-ephemeris idiom), timed so it kisses the
            // station's orbit at T-impact. Ship clamps onto Ringside (in shuttle reach of the rock at spawn).
            RockType type = q.DeflectionCheat switch
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
            q.DockCheat = "ringside-exchange"; // clamp onto the port under threat, in reach of the rock
        }
        if (q.TableSceneCheat)
        {
            // #875 · This is where ?tablescene=1 used to turn ?autowalk=1 on, because the last leg of the
            // scene is a walk across a canteen and clicking where you want to be is how this repo tests a
            // room. It no longer has to: the click is a control on every walked view now, so this boot —
            // and every other boot in DevStarts — arrives with both grips on the same walk.
            _tableSceneCheat = true;
        }


        if (q.SecretlabCheat)
        {
            // #409: append a plain landable Moon-kind rock co-orbiting the berth (the ellipse-cheat idiom),
            // comfortably inside one shuttle hop. Its surface is FORCED to hide a Vantar lab and to pre-reveal
            // the door (see ResolveSecretLab, keyed on _secretLabForceBodyId). Default the berth to Selene Gate.
            string berthKey = q.DockCheat ?? "selene-gate";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? mappedBerth) ? mappedBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                // #677 · Three rocks now, one cheat shape. `?found=1` is the deepest of them and it implies
                // `?secretlab=1`, because there is no other way down: the halls hang off a band nobody
                // listed, which hangs off a facility, which is reached through a shed.
                (string rockId, string rockName) = _foundCheat
                    ? (SecretLabFoundCheatBodyId, "The Hermit's Deep Rock")
                    : q.SecretlabDeep
                        ? (SecretLabDeepCheatBodyId, "The Deep Hermit's Rock")
                        : (SecretLabCheatBodyId, "The Hermit's Rock");
                scenario = scenario with
                {
                    Bodies = [.. scenario.Bodies, SecretLabSiteBody(berthId, rockId, rockName)],
                };
                _secretLabForceBodyId = rockId;
                q.DockCheat = berthId; // clamp onto the berth the rock co-orbits, so it's in reach at spawn
            }
        }
        if (q.WreckCheat)
        {
            // #488: append the derelict as a boardable site co-orbiting the berth — the same ellipse-cheat
            // idiom the expedition rock and the Hermit's Rock use. She is seeded from her id, so this cheat
            // always spawns the SAME ship with the same cause and the same cargo, which is what makes her
            // testable. Default the berth to The Tilt.
            string berthKey = q.DockCheat ?? "the-tilt";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? wreckBerth) ? wreckBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                Derelict.Wreck w = CheatWreck(q.WreckCauseCheat);
                scenario = scenario with { Bodies = [.. scenario.Bodies, WreckSiteBody(berthId, w)] };
                _wreck = w;
                q.DockCheat = berthId; // clamp onto the berth she hangs off, so she is in reach at spawn
            }
        }

        return scenario;
    }

    /// <summary>The immutable world: its name, its ephemeris, the berth roster the console gets when it
    /// is news (#726), and which of its bodies are off the charts until something charts them.</summary>
    private void BuildTheEphemerisAndAnnounceTheBerths(ScenarioDefinition scenario)
    {
        _scenarioName = scenario.Name;
        _ephemeris = CircularOrbitEphemeris.FromScenario(scenario);
        // #288: print the enumerable registry of every dockable berth to the browser console on boot, so
        // the bench never guesses an id — /map?dock=<id> boots already clamped on at any of these.
        // #726: this is per-COMPONENT boot, and the router builds a new Map every time you arrive at
        // /map — so walking Home → Map → Home → Map printed the identical list four times over. Say it
        // only when it is news (see BootRegistryAnnouncement); a different scenario still announces its
        // own berths, a second lap round the same world does not.
        string berthRoster = $"[SpaceSails] Dockable berths — /map?dock=<id>: {string.Join(", ", DockableHavens.AllIds(_ephemeris!))}";
        if (BerthRosterAnnouncement.IsNews(berthRoster))
        {
            Console.WriteLine(berthRoster);
        }
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
    }

    /// <summary>The three things built ONCE per scenario off the finished ephemeris — the mission
    /// catalog, the plasma environment, and the two simulators (the ship’s and the NPCs’).</summary>
    private void BuildWhatTheEphemerisFeeds(ScenarioDefinition scenario)
    {
        // PR-15, the captain's position: the mission catalog is scenario data (cargo classes,
        // route pairs, havens), so it's built once per scenario load alongside everything else
        // that reads _ephemeris — never recomputed per frame or per desk switch.
        _missionOptions = MissionCatalog.Build(_ephemeris!);
        _plasma = PlasmaEnvironment.FromScenario(scenario, _ephemeris!);
        _simulator = new Simulator(_ephemeris!, timeStepSeconds: 1.0, _plasma);
        _npcSimulator = new Simulator(_ephemeris!, TrafficSchedule.NpcTimeStep); // NPCs chargeless in M7
    }

    /// <summary>Where she starts, the arc that comes off it, and the takings and leftovers an operating
    /// ship arrives holding (owner, 2026-07-05, after the empty-purse screenshot).</summary>
    private void LayTheShipDownWithHerHistory()
    {
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
    }

    /// <summary>The four planners, each behind its own phase line and its own yield (#318) — pods,
    /// freighters, depots, and in the Sol family Luna’s mass drivers.</summary>
    private async Task PlanTheTrafficAsync(CancellationToken abandoned)
    {
        // Generate traffic once from the same deterministic Core planner the server uses. This does a
        // few seconds of planning work — and each coarse step (pods / freighters / depots / mass-driver
        // pods) is its own synchronous block. #318 follow-up: paint an honest phase line and yield to the
        // browser BEFORE each block, so the loading door shows progress and the tab stays paintable
        // instead of one silent multi-second freeze (badly amplified on the Debug/dev bundle). We do NOT
        // parallelise or restructure the planners — just phase-yield around them.
        // Pods first so "the Luna pod" is the top of the board and the obvious tutorial prey.
        await BootPhaseAsync("plotting the traffic lanes — pods…", abandoned);
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(_ephemeris!, seed: 43, count: 3);
        await BootPhaseAsync("plotting the traffic lanes — freighters…", abandoned);
        IReadOnlyList<NpcShip> traffic = TrafficSchedule.Generate(_ephemeris!, seed: 42, count: 8);
        // The derelict roadster is a dead wreck, not a trading post — it's a station body only so its
        // map label reads at a sane zoom (the fetch-mission target). Drop the depot GenerateDepots
        // would otherwise hang on it (any sun-orbiting body gets one).
        // A depot on a hidden body would leak it (the depot marker/menu would give the wreck away).
        // Filter generically on hidden+unrevealed, not the wreck's id — every future secret body is
        // covered for free (Tuesday plan PR-A).
        await BootPhaseAsync("plotting the traffic lanes — supply depots…", abandoned);
        IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(_ephemeris!, seed: 44)
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
            await BootPhaseAsync("plotting the traffic lanes — Luna's mass drivers…", abandoned);
            IReadOnlyList<NpcShip> lunaDriver = MassDriverSchedule.GenerateCadence(
                _ephemeris!, MassDriverSchedule.MassDriverRun.LunaMilkRun(), baseSimTime: _ship.SimTime, count: 4);

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
    }

    /// <summary>The vertex scratch the renderer writes through, and the first thing the camera sees.</summary>
    private void PointTheCameraAtHer()
    {
        _scratch = new float[_samples.Count * 2 + 4];

        _camera.MetersPerPixel = 3e8;
        _camera.CenterOn(_ship.Position);
    }

    /// <summary>#737 · The last stage that can be reached only in a browser: the module, the gate, the
    /// canvas renderer and its four views, the rAF loop, the art pre-decode — and the flag that says the
    /// world is ready. Everything from here on names DOM by id.</summary>
    private async Task WireTheRendererToTheBrowserAsync(CancellationToken abandoned)
    {
        await RendererInterop.EnsureModuleLoadedAsync();

        // #737 · THE LAST GATE BEFORE THE DOM. Everything from here on names elements by id — the canvas,
        // the scope inset, the focusable page div — and renderer.js throws rather than shrugging when the
        // id resolves to nothing. If the player left during any of the planning phases above, the page
        // those ids belong to is already gone, so this is where an abandoned boot must stop: no module
        // wiring, no FrameTick subscription on a component the router discarded, and above all no rAF loop
        // started against a dead canvas that nothing would ever stop.
        abandoned.ThrowIfCancellationRequested();

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
        _renderLoopRunning = true;

        // #371 Phase 1 (perf) · PRE-DECODE the deck/surface backdrop art at boot. RegisterImage fires the
        // JS decode fire-and-forget and caches by id; doing it now means the first deck or surface paint
        // never stalls waiting on an image decode (the study's cheap pre-warm). The surface plan reuses the
        // ship's backdrop set, so warming the ship's covers both.
        PredecodeDeckArt();

        _worldReady = true;
    }
}
