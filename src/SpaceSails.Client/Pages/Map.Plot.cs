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

// Map.Plot — the drawn future: trajectory projection and the fading ribbon, plot frames, the
// plan nodes and burn editors, sling and skim solves, and the map's celestial/ghost rendering. #251.
public partial class Map
{

    // Live flight-plan steps: non-stale burns still on the board plus the armed insertion, if any.
    private int FlightPlanStepCount()
    {
        int n = _armedOrbitBodyId is not null ? 1 : 0;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale) n++;
        }
        return n;
    }

    // 1-based index of the step being worked now: the earliest pending burn, or the insertion when the
    // approach is being flown / no burns remain. (Executed burns are pruned from _planNodes, so the
    // count reflects what is still ahead rather than the original plan length.)
    private int FlightPlanCurrentStep()
    {
        int idx = 0;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Stale) continue;
            idx++;
            if (!node.Executed && node.SimTime > SimTime) return idx; // first pending burn
        }
        return Math.Max(1, idx + (_armedOrbitBodyId is not null ? 1 : 0));
    }

    private static string BurnStepLabel(PlanNode node)
    {
        string dir = node.Mode == BurnMode.Vector ? "✚"
            : node.Action == ManeuverAction.Accelerate ? "▲" : "▼";
        return $"burn {dir} {node.Pulses} p";
    }

    // PR-D2: the glanceable collapsed line for a burn step — type + direction + countdown, so the whole
    // trip reads top to bottom without opening anything (GeminiUINotes.md: "type + target + countdown").
    private string BurnGlanceLine(PlanNode node)
    {
        string arrow = node.Mode == BurnMode.Vector ? "✚"
            : node.Action == ManeuverAction.Accelerate ? "▲" : "▼";
        // #201: mirror the input's convention — ship-relative by default ("+90° rel"), absolute on toggle.
        string dir = node.Mode == BurnMode.Vector
            ? (_burnAngleAbsolute
                ? $"{node.HeadingDegrees.ToString("0", CultureInfo.InvariantCulture)}°"
                : $"{BurnHeadingConvention.WorldToRelative(HeadingAlongCourseAt(node.SimTime), node.HeadingDegrees).ToString("+0;-0;0", CultureInfo.InvariantCulture)}° rel")
            : node.Action == ManeuverAction.Accelerate ? "prograde" : "retrograde";
        string when = node.Executed ? "fired"
            : node.SimTime <= SimTime ? "now"
            : $"in {FormatDuration(node.SimTime - SimTime)}";
        return $"burn {arrow} {node.Pulses} p → {dir} · {when}";
    }

    // PR-D2: the flight-plan accordion. Clicking a step's line (or its ribbon node) expands its editor;
    // opening one collapses any other — exactly one editor is open at a time. _selectedPlanNode is the
    // burn's identity, shared by the list click and the map-node pick so both resolve to one selection.
    private void ToggleBurnEditor(PlanNode node)
    {
        if (_openEditor == FlightEditorKind.Burn && ReferenceEquals(_selectedPlanNode, node))
        {
            _openEditor = FlightEditorKind.None;
            _selectedPlanNode = null;
        }
        else
        {
            _openEditor = FlightEditorKind.Burn;
            _selectedPlanNode = node;
        }
    }

    // The NOW / next readout AND the full banner row list, built through the shared Core helper so the
    // banner, the Nav header, and the desk chips can never contradict (#159/#184). The queue below NOW
    // names every step still ahead top to bottom: each pending burn in time order, then the armed
    // orbit-insert — so the approach and the orbit step are SEPARATE, plain-language rows (#171/#173).
    private FlightPlanStatus FlightNowNext()
    {
        var steps = new List<FlightPlanStep>();

        // Pending burns, earliest first.
        var pending = new List<PlanNode>();
        foreach (PlanNode node in _planNodes)
        {
            if (node.Stale || node.Executed || node.SimTime <= SimTime) continue;
            pending.Add(node);
        }
        pending.Sort((a, b) => a.SimTime.CompareTo(b.SimTime));
        foreach (PlanNode node in pending)
        {
            steps.Add(new FlightPlanStep(BurnStepLabel(node), $"in {FormatDuration(node.SimTime - SimTime)}", FlightStepState.Planned));
        }

        // The armed orbit-insert — named in plain language ("will it orbit or crash?" → it says so),
        // with the parked altitude when we know it, and the insertion's Armed/Active step state. Only
        // while still FLYING to it — once the park is kept there is no insertion step pending (Friday §0).
        if (_armedOrbitBodyId is not null && !_orbitKept)
        {
            string eta = ArmedInsertionSimTime is { } t ? $"in {FormatDuration(t - SimTime)}" : "at window";
            // #204: a μ=0 station is never orbit-inserted — the plan's final step is the ⚓ dock. For an
            // honest errand the autopilot clamps itself ("⚓ auto-dock"); a hostile-flagged run keeps the
            // captain's-word grammar ("⚓ Dock — your call").
            string label = BodyById(_armedOrbitBodyId) is { } armed && IsDockableHaven(armed)
                ? (AutoDockHonest(armed)
                    ? $"⚓ auto-dock at {armed.Name}"
                    : $"⚓ Dock at {armed.Name} — your call in the envelope")
                : InsertStepLabel();
            steps.Add(new FlightPlanStep(
                label, eta, FlightPlanStatusBuilder.InsertionState(AutopilotFlyingApproach)));
        }

        // Friday §0 (owner ruling): the kept-orbit NOW line, composed HERE and fed through main's
        // #190 HoldingLine seam — ONE code path in the builder, which slots it below Docked and above
        // every flying phase: "🛰 AUTOPILOT HOLDS THE ORBIT — <body>, <alt>, trim ≈N p/day", never
        // "you have the ship".
        // #220 / #203: quote the PARK altitude (the held value the keeper trims back to,
        // ParkingRadius − surface, "alt N km"), NOT the instantaneous body-relative radius — the kept
        // orbit's forced eccentricity makes that raw radius oscillate, so the snapshot disagreed with
        // itself (holding said "278 km" while Nearest said "217 km"). The live wobble stays on the
        // Nearest line; the holding line states the steady park the autopilot is keeping.
        string? holdingLine = null;
        if (_orbitKept && _armedOrbitBodyId is not null && _ephemeris is not null)
        {
            // The steady park the keeper trims back to: ParkingRadius − surface, "alt N km" (#203). The
            // kept body IS the bound body while keeping, so its Hill radius is UpdateOrbitedBody's cached
            // one. If (defensively) that isn't available, fall back to the instantaneous radius so the
            // NOW line never vanishes — but the park altitude is the value that stays steady on this line.
            CelestialBody? keptBody = BodyById(_armedOrbitBodyId);
            double parkAlt = keptBody is not null && _orbitedBodyId == _armedOrbitBodyId && _orbitedBodyHillRadius > 0
                ? OrbitRule.ParkingRadius(keptBody, _orbitedBodyHillRadius) - keptBody.BodyRadius
                : 0;
            string alt = parkAlt > 0
                ? FormatAltitude(parkAlt)
                : FormatDistance((_ship.Position - _ephemeris.Position(_armedOrbitBodyId, SimTime)).Length);
            string trim = _keepTrimPulsesPerDay > 0 ? $", trim ≈{_keepTrimPulsesPerDay} p/day" : "";
            holdingLine = $"🛰 AUTOPILOT HOLDS THE ORBIT — {BodyName(_armedOrbitBodyId)}, {alt}{trim}";
        }

        return FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: NavLockedByDock,
            DockedHavenName: _havenName,
            AutopilotArmed: _armedOrbitBodyId is not null,
            AutopilotFlyingApproach: AutopilotFlyingApproach,
            AutopilotBodyName: _armedOrbitBodyId is null ? null : BodyName(_armedOrbitBodyId),
            NextStepLabel: null,
            NextStepEta: null,
            // #147: the persistent "you have the ship" reason — a decline or a loud handback — so the
            // now line survives high warp instead of a 1.5-s toast. Only when not armed (else the ship
            // is flying again and the armed line wins).
            HandbackReason: _armedOrbitBodyId is null ? _autopilotStandDownReason : null,
            // Friday §0 priority lane: the kept-orbit NOW line, or null while still flying / manual.
            HoldingLine: holdingLine,
            UpcomingSteps: steps.Count > 0 ? steps : null));
    }

    // The armed-arrival step's plain-language label — routed through the Core one-voice vocabulary
    // (#203) so a real orbit reads "orbit-insert at Enceladus (alt 313 km)" while a μ≤0 dock haven
    // reads "dock envelope at Cinder Roost — slow to ≤8 km/s" (no phantom orbit, no "(0 km)"). The
    // parked ALTITUDE above the surface (not the orbital radius) is the captain-facing number.
    private string InsertStepLabel()
    {
        HarborClass harbor = HarborClassOf(_armedOrbitBodyId);
        string body = BodyName(_armedOrbitBodyId!);
        string? altitude = null;
        if (harbor == HarborClass.Orbit && OrbitInfo() is { } oi && oi.Body.Id == _armedOrbitBodyId)
        {
            double parkAlt = OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius;
            if (parkAlt > 0)
            {
                altitude = FormatAltitude(parkAlt);
            }
        }
        return HarborVocabulary.ArrivalStep(harbor, body, altitude);
    }

    // ===== #261 — the COMPUTED skip: reckon a jump-scale coast, never grind it =====
    // The freeze is the near-body fixed-1 s regime during a long arrival coast (warp auto-drops, the loop
    // grinds ~86_400 gravity steps per day). The law is already ruled — the void is computed, not slogged.
    // The clean, tested path is the long haul's own machinery: for a BALLISTIC leg in OPEN heliocentric
    // cruise, advance the ship along its closed-form conic to the target epoch, re-seed the world there, and
    // say the state. Otherwise (a burn inside the leg, or the ship deep in a well where the heliocentric
    // conic is a lie against the n-body integrator) fall back to chunked integration that PAINTS between
    // chunks — never a dead frame. Correctness beats elegance: closed form only where it is honest.

    // The upcoming burn epochs the ballistic test reads — the same plotted-node + armed-transfer sources
    // NextSkippableEvent's Burn candidate is built from, so the two agree on where impulses fire.
    private IEnumerable<double> UpcomingBurnEpochs()
    {
        double now = SimTime;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && !node.Executed && node.SimTime > now)
            {
                yield return node.SimTime;
            }
        }

        if (_armedTransferSchedule is { } sch)
        {
            for (int i = _armedTransferBurnsFired; i < sch.Burns.Count; i++)
            {
                yield return sch.Burns[i].SimTime;
            }
        }
    }
    private const int OrbitSegments = 128;

    private static readonly RgbaColor Background = new(6, 9, 18);
    private static readonly RgbaColor OrbitColor = new(80, 100, 130, 130);
    private static readonly RgbaColor ShipColor = new(255, 210, 80);
    private static readonly RgbaColor TrajectoryColor = new(255, 165, 0);
    // #148: the autopilot's INTENDED path — the rehearsed flight, not the ballistic loops the ship
    // will never fly while armed. Teal, matching the autopilot's info theme, so it never reads as the
    // amber ballistic ribbon.
    private static readonly RgbaColor AutopilotPlanColor = new(90, 230, 200, 230);
    private static readonly RgbaColor LabelColor = new(224, 228, 236);
    private static readonly RgbaColor GhostShipColor = new(255, 210, 80, 120);
    private static readonly RgbaColor AccelNodeColor = new(80, 220, 120);
    private static readonly RgbaColor DecelNodeColor = new(240, 120, 80);
    private static readonly RgbaColor StaleNodeColor = new(140, 140, 140, 128);
    private static readonly RgbaColor XPilotVectorColor = new(120, 210, 255);
    private const byte GhostBodyAlpha = 90; // ~35% of 255

    // PR-3 outer-reaches rendering: stations read as built things (a synthetic teal, not a
    // planet's mineral palette); havens get a subtle crimson wash on top — pirate country, at a
    // glance. Stations stay tiny blips even zoomed in, so their label needs its own threshold.
    private static readonly RgbaColor StationColor = new(120, 220, 210);
    private static readonly RgbaColor DestinationColor = new(120, 220, 255);
    private static readonly RgbaColor HavenAccent = new(200, 60, 90);
    private static readonly RgbaColor HavenLabelColor = new(232, 190, 200);
    private const double LabelZoomThresholdForStations = 5e9; // m/px

    // The 🛬 landable mark (owner, 2026-07-19 playtest: "some similar meme as the anchor for places that
    // can be landed to with the shuttle on the map"). The sibling of the ⚓ dock glyph: every landable
    // moon carries it, in two states — dim regolith tan = landable in principle; bright + a size up =
    // within the shuttle's reach of YOUR ship right now, so a docked captain sees at a glance where the
    // shuttle could go this moment. The bright set is the SAME range truth the shuttle-bay board reads.
    private static readonly RgbaColor LandableBaseColor = new(196, 180, 150, 120);
    private static readonly RgbaColor LandableInRangeColor = new(240, 226, 190, 245);

    private static RgbaColor Tinted(RgbaColor c, RgbaColor accent, double amount) => new(
        (byte)Math.Clamp(c.R * (1 - amount) + accent.R * amount, 0, 255),
        (byte)Math.Clamp(c.G * (1 - amount) + accent.G * amount, 0, 255),
        (byte)Math.Clamp(c.B * (1 - amount) + accent.B * amount, 0, 255),
        c.A);
    private IReadOnlyList<TrajectorySample> _samples = [];
    private float[] _scratch = [];
    // Gravity bends the real path away from a ribbon projected in the past, so re-project after
    // this much sim time even without a pulse (≈2 real seconds at max warp).
    private const double ProjectionRefreshSimSeconds = 6 * 3600;
    private double _nextProjectionSimTime;
    private const int MaxStepsPerFrame = 20000;

    // M4 additions — plotting mode
    // The ribbon/projection horizon. 60 days from "now"; ProjectAdaptive keeps this cheap
    // (coarse dt in deep space, fine near a mass) and is the single source of truth for the
    // ribbon polyline, the scrub ghost-ship, and node markers.
    // The Saturn metric (owner): one plotting sit-down must cover a whole Earth->Saturn sail.
    // The probe's reference plan (accel 12 @ day 82) arrives day 278; two years leaves slack
    // for lazier sails. The ribbon projects at maxTimeStep 3 h to keep 730-day re-projections
    // cheap in interpreted WASM (~5.8k steps); plan-node times still land exactly.
    private const double PlotHorizonSeconds = 730 * 24 * 3600;

    // #209 — the auto projection length. With no plan the ribbon holds this local default; with a plan
    // it reaches the plan's furthest encounter plus the margin, clamped to PlotHorizonSeconds.
    private const double AutoHorizonMinSeconds = 30 * 86400;
    private const double AutoHorizonMarginSeconds = 90 * 86400;

    // #145 — in a co-moving frame around a Hill-sphere body the full solar-scale ribbon draws as a
    // spirograph coil (the owner's 7-day Titan approach = ~8-10 laps of Saturn). So the DRAWN length
    // is truncated to ~this many LOCAL orbital periods at the ship's current radius around the frame
    // body; the projection/ETA math in _samples stays full length. Sun frame is untouched.
    private const double FrameWindowLocalPeriods = 1.25;
    // Never truncate below this — the near-term course must always be readable ("a few hours").
    private const double FrameWindowFloorSeconds = 6 * 3600;
    // …and never hide an imminent plan node: the window is stretched to the next future node plus
    // this margin, so the step sits comfortably inside the solid ribbon, not right at the fade edge.
    private const double FrameWindowNodeMarginSeconds = 12 * 3600;
    // The truncated ribbon ends SOFTLY, not with a hard chop: the last slice of the window fades to
    // nothing using the #110 time-fade idiom (per-segment alpha, quantized into buckets so a long
    // ribbon still strokes in a handful of DrawPolyline runs).
    private const double FrameRibbonFadeFraction = 0.22;   // fade over the last ~22% of the window
    private const double FrameRibbonFadeMinSeconds = 2 * 3600;
    private const double FrameRibbonFadeMaxSeconds = 2 * 86400;
    private const int FrameRibbonFadeBuckets = 12;

    // Owner request: the future path is adjustable. AUTO follows the plan — last burn + 90
    // days (min 30 d) — so ship-to-ship work stays tight and a plotted Saturn sail stretches
    // the ribbon automatically; presets override.
    private string _horizonChoice = "auto";
    private const double HorizonMinDays = 5, HorizonMaxDays = 730;

    // Log-scale mapping so the slider is as precise at 7 days as at 2 years.
    private int HorizonSliderValue =>
        _horizonChoice == "auto"
            ? (int)Math.Round(100 * Math.Log(Math.Clamp(CurrentPlotHorizonSeconds / 86400, HorizonMinDays, HorizonMaxDays) / HorizonMinDays) / Math.Log(HorizonMaxDays / HorizonMinDays))
            : (int)Math.Round(100 * Math.Log(double.Parse(_horizonChoice) / HorizonMinDays) / Math.Log(HorizonMaxDays / HorizonMinDays));

    private static double SliderToDays(int t) =>
        HorizonMinDays * Math.Pow(HorizonMaxDays / HorizonMinDays, t / 100.0);

    private bool _horizonDirty;
    private double _lastHorizonReprojectMs;

    // #201: the burn-angle input's convention. Default false = ship-relative (0 ahead, +90 starboard,
    // −90 port); toggled true reads/writes the absolute world heading. The stored HeadingDegrees is
    // always world-space regardless — only the display and parse are translated (BurnHeadingConvention).
    private bool _burnAngleAbsolute;

    // M18: the planner's proximity warning. Computed AFTER edits settle (300 ms idle), not per
    // drag tick — the scan touches every body along up to 8000 samples, too heavy for a
    // slider's oninput in interpreted WASM.
    private ClosestApproach.Pass? _closestPass;
    private ClosestApproach.Pass? _armablePass;
    private ClosestApproach.Pass? _destinationPass;   // M25: the plotted path's closest pass by THE destination
    private ClosestApproach.Pass? _slingablePass;     // PR-G: the tightest PLANET pass inside its Hill sphere — the sling handle

    private bool _passDirty;
    private double _lastReprojectMs;

    // PR-D2 · the flight plan is an accordion: exactly ONE step editor is expanded at a time. This
    // enum is the single source of truth for "which editor is open" (docs/WednesdayPlan/GeminiUINotes.md
    // "selection single-source-of-truth"); for a burn, the identity of the open node is _selectedPlanNode,
    // which a ribbon-node click and a list click both resolve to — map and list are two views of one plan.
    private enum FlightEditorKind { None, Burn, Sling, Skim, Insertion }
    private FlightEditorKind _openEditor = FlightEditorKind.None;

    // PR-G · the sling — the plotting-desk panel that bends the track off a close planetary pass
    // (SlingPlanner in Core does the b-plane aiming). All UI state; the solver runs synchronously on
    // SOLVE (bounded cost; docs/MondayPonder/ThreadedFireControlPlan.md is the future home for slicing).
    private SlingPlanner.PassSide _slingSide = SlingPlanner.PassSide.Lead;
    private double _slingPassRadii = 8;               // requested pass distance in body radii (floor 2 R)
    private bool _slingSolving;
    private SlingPlanner.Result? _slingResult;        // the QUANTIZED summary — what "Add the burn" will fly
    private string? _slingFailure;                    // honest refusal text when the solve can't be met
    private int _slingPulses;                         // Vector-burn pulses the solved Δv rounds to
    private double _slingHeadingDeg;                  // world-space heading of the solved Δv (Vector burn)
    private double _slingBurnTime;                    // the burn node time the solve used
    private const double SlingBurnPercent = 1.0;      // per-pulse Vector-burn strength (% of entry speed) the sling emits
    private const double SlingMinRadii = 2.0;         // the labs' point-mass floor
    private const double SlingMaxRadii = 40.0;
    // A sling is a leveraged NUDGE — the flyby does the work. Cap the aiming burn to a modest budget
    // (also bounded by the tank) so a request that would need a brute redirect fails honestly instead
    // of quietly spending half the reaction mass; bigger course changes are a plain burn's job.
    private const double SlingMaxAimDeltaV = 1200.0;

    // PR-I · the skim & skip — the sling's sibling on the plot desk. On a close pass by an
    // atmosphere-bearing body, aim a periapsis INSIDE the shell and read a three-zone corridor gauge
    // (too shallow / the corridor / too deep = holes the sail). The aim reuses the sling's perp-⟂-v_rel
    // frame, but the SOLVE is a cheap VACUUM-periapsis bisection (SlingPlanner's coarse heliocentric
    // b-plane strategy can't resolve — or correctly sign — a target hundreds of km deep, well under its
    // 0.1 R measurement floor); the shown numbers then come from ONE RunAdaptiveWithDrag flight of the
    // quantized plan, so the gauge is what actually flies (the same honesty rule the sling keeps).
    private ClosestApproach.Pass? _skimmablePass;     // tightest pass by a body that has an Atmosphere
    private double _skimAltKm;                         // requested periapsis altitude (km) inside the shell
    private bool _skimSolving;
    private SkimGauge? _skimResult;                    // the QUANTIZED, flown gauge — what "Add the burn" flies
    private string? _skimFailure;
    private int _skimPulses;                           // fine Vector-burn pulses the aim rounds to
    private double _skimHeadingDeg;                    // world heading of the quantized aim Δv
    private double _skimBurnTime;                      // the aiming-burn node time (placed close to the pass)
    private const double SkimBurnPercent = 0.006;      // per-pulse aim strength (% of entry speed) — a VERY fine trim; the grazing corridor is ~1 m/s wide
    private const double SkimBurnLeadSeconds = 12 * 3600; // aim node placed 12 h before periapsis: a modest lever, quantization-friendly
    private const double SkimMaxAimDeltaV = 200.0;     // aim budget cap (also bounded by the tank)
    private const double SkimCorridorFloorMps = 30.0;  // Δv shed below this = "too shallow, nothing worth doing"

    // PR-I · the live consequence (the plan's default damage currency — the same sail-holed disable the
    // gun inflicts, now self-inflicted). In live flight, if drag deceleration crosses the Core damage
    // line, the sail holes: thrust and pending burns are disabled for a fixed repair window. Deterministic
    // (a pure function of the flown state — no RNG). OWNER OPEN QUESTION 1: this is the plan's default;
    // alternatives (burn pulses as ablation, a new hull meter) are unanswered — flagged for the owner.
    private bool _sailHoled;
    private double _sailRepairedAtSimTime;             // sim-time the rigging is sewn shut again
    private const double SailRepairSeconds = 2 * 86400.0; // ~2 sim-days in the loft — a constant, documented window
    private double _frameMaxDragDecel;                  // peak drag deceleration seen across this frame's steps

    // The flown skim gauge (one RunAdaptiveWithDrag pass of the quantized aim). Every number is measured
    // off the real drag flight, never the requested slider value — the slider is a target, the gauge is truth.
    private readonly record struct SkimGauge(
        int Pulses, double HeadingDeg, double BurnTime,
        double MinAltMeters, double ShedMps, double PeakG, double PulsesSaved,
        bool Captured, double ExitVinfMps, bool ArrivalHyperbolic,
        double RequestedAltKm, double AchievedAltKm)
    {
        public bool TooDeep => PeakG >= Atmosphere.SailHoleDecelG;                 // holes the sail
        public bool InCorridor => !TooDeep && ShedMps >= SkimCorridorFloorMps;     // useful braking under the damage line
        public bool TooShallow => !TooDeep && ShedMps < SkimCorridorFloorMps;      // nothing worth doing (or a skip, if hyperbolic)
    }

    private void OnHorizonSliderInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int t))
        {
            _horizonChoice = SliderToDays(t).ToString("F0");
            _horizonDirty = true; // OnTick reprojects, throttled — live ribbon while dragging
        }
    }

    private void SetHorizonAuto()
    {
        _horizonChoice = "auto";
        ReprojectTrajectory();
    }

    private static string FormatHorizon(double seconds)
    {
        double days = seconds / 86400;
        return days >= 365 ? $"{days / 365:F1} yr" : $"{days:F0} d";
    }
    private double CurrentPlotHorizonSeconds
    {
        get
        {
            double horizon;
            if (_horizonChoice != "auto" && double.TryParse(_horizonChoice, out double days))
            {
                horizon = days * 86400;
            }
            else
            {
                // #209: auto is PLAN-AWARE. The projection reaches the plan's furthest encounter (the
                // rehearsal's arrival, the plotted destination pass, or the furthest live burn node) plus a
                // margin; with no plan it holds the local default. PlanFurthestEpochSeconds is the ONE truth
                // (no second estimator); the clamp stops a runaway plan asking for an un-affordable reproject.
                horizon = PlotHorizon.AutoProjectionSeconds(
                    PlanFurthestEpochSeconds(), AutoHorizonMinSeconds, AutoHorizonMarginSeconds, PlotHorizonSeconds);
            }

            // #265: once the achieved orbit is BOUND to a body, cap the horizon at ~one revolution so the
            // ribbon draws a single closing loop, not a precessing bouquet (the owner's Uranus flower). A
            // plotted departure (PlanFurthestEpochSeconds > 0) or an unbound transfer/hyperbolic leg keeps
            // the full length. Capping the PROJECTION, not just the draw, also spares the integrator the
            // deep-periapsis passes whose drift painted the extra petals in the first place.
            return PlotHorizon.BoundOrbitHorizon(horizon, BoundOrbitPeriodSeconds(), PlanFurthestEpochSeconds());
        }
    }

    // #209 — the plan's furthest encounter, in seconds AHEAD of now: the single length the auto ribbon
    // must reach. Reads the one true schedule — the autopilot rehearsal's arrival (the #148 intended
    // path's last sample), the plotted destination closest-pass, and the furthest live burn node — never
    // a second estimator. 0 when there is no plan (no armed autopilot, no destination pass, no nodes).
    private double PlanFurthestEpochSeconds()
    {
        double now = SimTime;
        double furthest = 0;

        // Armed autopilot: the rehearsed path's final sample IS the arrival/insertion instant (#146/#148).
        if (_armedOrbitBodyId is not null && _autopilotPlanPath is { Count: > 0 } path)
        {
            furthest = Math.Max(furthest, path[^1].SimTime - now);
        }

        // The plotted path's closest pass by the destination — the encounter the captain is aiming at.
        if (_destinationPass is { } dp)
        {
            furthest = Math.Max(furthest, dp.SimTime - now);
        }

        // The furthest future burn node — so a plotted departure's ribbon reaches at least its last burn.
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && node.SimTime > now)
            {
                furthest = Math.Max(furthest, node.SimTime - now);
            }
        }

        return furthest;
    }
    private const int MaxNodePulses = 20;
    private const int MinNodePulses = 1;
    private bool PlotMode;
    private int _warpBeforePlot = 1;
    private double _scrubOffsetSeconds;
    private readonly List<PlanNode> _planNodes = [];
    private ManeuverPlan _plan = ManeuverPlan.Empty;

    // #135 — the plot map's reference frame. null = Sun/inertial (the default; the pre-#135 draw path,
    // byte-identical). Otherwise the id of the body the plotted ribbon/ghosts/markers are re-expressed
    // co-moving with, so a moon-to-moon flight near a gas giant reads against the giant instead of
    // drowning in its ~10 km/s solar orbit. RENDERING ONLY — the projection stays heliocentric.
    // Held in-memory, so the pick persists across desk switches / Plot⇄Play for the session (a full
    // page reload resets to Sun).
    private string? _plotFrameBodyId;
    private Vector2d _plotFrameAnchor;   // frame body's position at "now", refreshed once per drawn frame

    private double ScrubTime => _ship.SimTime + _scrubOffsetSeconds;

    private static Vector2d SamplePositionAtTime(IReadOnlyList<TrajectorySample> samples, double simTime)
    {
        foreach (TrajectorySample sample in samples)
        {
            if (sample.SimTime >= simTime)
            {
                return sample.Position;
            }
        }

        return samples[^1].Position;
    }

    // Editable client-side node. ManeuverNode is an immutable value type with no notion of
    // "stale"/"executed", so plotting mode tracks those flags here and rebuilds the immutable
    // ManeuverPlan from the non-stale nodes after every edit.
    private sealed class PlanNode
    {
        public double SimTime;
        public ManeuverAction Action;
        public int Pulses = 1;
        public double Percent = 10;      // per-pulse strength, any positive double
        public bool Stale;
        public bool Executed;
        public BurnMode Mode = BurnMode.Factor;  // Factor = ± prograde; Vector = X-Pilot point-and-burn
        public double HeadingDegrees;            // world-space heading for a Vector burn (0° = +X, CCW)
    }

    // ---- M24: destination — the body the captain MEANS to reach. Clicking a planet on the
    // map sets it; the orbit-assist panel then coaches the approach to that body instead of
    // whichever happens to be nearest ("Orbit Venus?" while aiming for Mercury). ----
    private string? _destinationBodyId;

    private void SetDestination(string? bodyId)
    {
        if (bodyId is null && _armedOrbitBodyId is not null && _armedOrbitBodyId == _destinationBodyId)
        {
            _armedOrbitBodyId = null; // clearing the destination also stands down its auto-insert
        }

        _destinationBodyId = bodyId;
        _bodyMenuBody = null;
        _passDirty = true; // recompute the destination's closest pass on the next tick

        // M26: keep the captain's articles honest — a nav destination becomes a Fly to order
        // when the ship had no standing order; a bigger order (Hunt, Trade run…) stands.
        if (bodyId is not null && _mission.Kind is MissionKind.FreeSailing or MissionKind.FlyTo)
        {
            _mission = new ShipMission(MissionKind.FlyTo, DestinationBodyId: bodyId);
        }
        else if (bodyId is null && _mission.Kind == MissionKind.FlyTo)
        {
            _mission = ShipMission.Default;
        }

        bool destDocks = bodyId is not null
            && _ephemeris?.Bodies.FirstOrDefault(b => b.Id == bodyId) is { } destBody
            && IsDockableHaven(destBody);
        ShowPulseMessage(bodyId is null
            ? "Destination cleared"
            : destDocks
                ? $"Destination set — {BodyName(bodyId)} ⚓ dock assist tracks it now"
                : $"Destination set — {BodyName(bodyId)} 🎯 orbit assist tracks it now");
        StateHasChanged();
    }

    // M26: bound into orbit at the destination — the voyage is over, the orders complete.
    private void ArrivedAt(string bodyId)
    {
        if (_destinationBodyId == bodyId)
        {
            _destinationBodyId = null;
        }

        if (_mission.Kind == MissionKind.FlyTo && _mission.DestinationBodyId == bodyId)
        {
            _mission = ShipMission.Default;
        }
    }

    // M26: time to destination, from the projected course's closest pass by it. A ballpark by
    // design — the projection refreshes every few sim-hours and on every plan edit.
    private string? DestinationEta()
    {
        // #147: while the autopilot has stood down, do NOT quote an arrival ETA on any desk chip —
        // nothing is flying the ship there, so "ETA 6 d" would be the very lie the owner caught.
        if (AutopilotStoodDown || _destinationBodyId is null || _destinationPass is not { } dp
            || dp.BodyId != _destinationBodyId || dp.SimTime <= SimTime)
        {
            return null;
        }

        return $"ETA {FormatDuration(dp.SimTime - SimTime)}";
    }

    // A closest pass can be turned into a planned insertion when it is a planet (not the sun)
    // and tight enough to matter. Returns the estimated pulse cost, or null when not orbitable.
    private int? PassIsOrbitable(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null) return null;
        CelestialBody? body = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == cp.BodyId) { body = candidate; break; }
        }
        if (body is null || body.ParentId is null || body.Kind == BodyKind.Station) return null;

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; break; }
        }
        if (parent is null) return null;
        // Armable within capture range: from there the autopilot (M25) flies the rest.
        if (cp.Distance > OrbitRule.CaptureRange(OrbitRule.HillRadius(body, parent.Mu))) return null;

        // Estimate the burn from the sampled path: finite-difference ship velocity at the pass.
        Vector2d shipVel = SampledVelocityAt(cp.SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(cp.BodyId, cp.SimTime + h) - _ephemeris.Position(cp.BodyId, cp.SimTime - h)) / (2 * h);
        var passState = new ShipState(cp.ShipPosition, shipVel, cp.SimTime);
        Vector2d bodyPos = _ephemeris.Position(cp.BodyId, cp.SimTime);
        return OrbitRule.PulseCost(passState, bodyPos, bodyVel, body);
    }

    // M25: everything the navigation-target panel says about the destination's closest pass.
    private readonly record struct DestPassInfo(double CaptureRange, bool InRange, double RelSpeed, int EstPulses);

    private DestPassInfo? DestinationPassInfo(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null) return null;
        CelestialBody? body = null;
        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == cp.BodyId) { body = candidate; }
        }
        if (body?.ParentId is null) return null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; }
        }
        if (parent is null) return null;

        double captureRange = OrbitRule.CaptureRange(OrbitRule.HillRadius(body, parent.Mu));
        Vector2d shipVel = SampledVelocityAt(cp.SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(cp.BodyId, cp.SimTime + h) - _ephemeris.Position(cp.BodyId, cp.SimTime - h)) / (2 * h);
        Vector2d bodyPos = _ephemeris.Position(cp.BodyId, cp.SimTime);
        var passState = new ShipState(cp.ShipPosition, shipVel, cp.SimTime);
        int estPulses = OrbitRule.PulseCost(passState, bodyPos, bodyVel, body);
        return new DestPassInfo(captureRange, cp.Distance <= captureRange, (shipVel - bodyVel).Length, estPulses);
    }

    private void ScrubToDestinationPass(ClosestApproach.Pass cp) =>
        _scrubOffsetSeconds = Math.Max(0, cp.SimTime - _ship.SimTime);

    private Vector2d SampledVelocityAt(double simTime)
    {
        for (int i = 1; i < _samples.Count; i++)
        {
            if (_samples[i].SimTime >= simTime)
            {
                double dt = _samples[i].SimTime - _samples[i - 1].SimTime;
                return dt <= 0 ? _ship.Velocity : (_samples[i].Position - _samples[i - 1].Position) / dt;
            }
        }
        return _ship.Velocity;
    }

    // ---- PR-G · the sling (SlingPlanner behind a plot-desk panel) ----

    // A closest pass the crank can work: a planet (not the sun, not a station), the pass ahead of us,
    // above the surface, and inside the body's Hill sphere where a flyby actually bends the track.
    private bool PassIsSlingable(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null || cp.SimTime <= SimTime + 60)
        {
            return false;
        }

        CelestialBody? body = SlingBody(cp.BodyId, out CelestialBody? parent);
        if (body is null || parent is null || body.Kind == BodyKind.Station)
        {
            return false;
        }

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        return cp.Distance > body.BodyRadius * 2 && cp.Distance < hill;
    }

    private CelestialBody? SlingBody(string bodyId, out CelestialBody? parent)
    {
        parent = null;
        CelestialBody? body = null;
        foreach (CelestialBody candidate in _ephemeris!.Bodies)
        {
            if (candidate.Id == bodyId) { body = candidate; }
        }
        if (body?.ParentId is null)
        {
            return null;
        }
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; }
        }
        return body;
    }

    private double SlingBodyRadius() =>
        _slingablePass is { } cp && SlingBody(cp.BodyId, out _) is { } b ? b.BodyRadius : 1.0;

    // PR-D2: open/close the sling compose editor through the accordion (closes any other open step).
    // Opening seeds a sane default pass distance from the current natural pass (rounded to whole radii,
    // clamped to the floor), so SOLVE has something reasonable to aim at.
    private void ToggleSlingPanel()
    {
        bool opening = _openEditor != FlightEditorKind.Sling;
        _openEditor = opening ? FlightEditorKind.Sling : FlightEditorKind.None;
        _selectedPlanNode = null;
        _slingResult = null;
        _slingFailure = null;
        if (opening && _slingablePass is { } cp)
        {
            double naturalR = cp.Distance / SlingBodyRadius();
            _slingPassRadii = Math.Clamp(Math.Round(naturalR), SlingMinRadii, SlingMaxRadii);
        }
    }

    private void SetSlingSide(SlingPlanner.PassSide side)
    {
        _slingSide = side;
        _slingResult = null;
        _slingFailure = null;
    }

    private void OnSlingRadiiInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
        {
            _slingPassRadii = Math.Clamp(r, SlingMinRadii, SlingMaxRadii);
            _slingResult = null;
            _slingFailure = null;
        }
    }

    // The burn node the solve aims from: a NEW node at scrub time when the scrub sits between now and
    // the pass, else now + 10 min (the solver is free to propose its own — we note which we used).
    private double SlingBurnTime(double passSimTime)
    {
        double scrub = Math.Floor(ScrubTime);
        bool scrubUsable = scrub > _ship.SimTime + 60 && scrub < passSimTime - 3600;
        return scrubUsable ? scrub : Math.Floor(_ship.SimTime) + 600;
    }

    private string SlingNodeNoteNow()
    {
        if (_slingablePass is not { } cp)
        {
            return "";
        }
        double t = SlingBurnTime(cp.SimTime);
        return t <= Math.Floor(_ship.SimTime) + 600 + 0.5
            ? "burn node: now + 10 min (scrub isn't before the pass)"
            : $"burn node: scrub {FormatSimTime(t)}";
    }

    // SOLVE: run the Core solver, then quantize Δv to whole Vector-burn pulses and RE-SUMMARIZE at the
    // quantized Δv, so every number shown is what "Add the burn" will actually fly (the honesty rule).
    private async Task RunSlingSolveAsync()
    {
        if (_ephemeris is null || _simulator is null || _slingablePass is not { } cp)
        {
            return;
        }

        _slingSolving = true;
        _slingResult = null;
        _slingFailure = null;
        StateHasChanged();
        await Task.Yield(); // let the "solving…" state paint before the synchronous solve

        double tBurn = SlingBurnTime(cp.SimTime);
        _slingBurnTime = tBurn;

        ShipState burnState = _simulator.RunAdaptive(_ship, Math.Max(1.0, tBurn - _ship.SimTime), _plan);
        double burnSpeed = burnState.Velocity.Length;
        double perPulseDv = Math.Max(1.0, burnSpeed * SlingBurnPercent / 100.0);
        int availablePulses = Math.Max(1, _reactionMassPulses - PlannedPulseTotal());
        double cap = Math.Min(SlingMaxAimDeltaV, Math.Max(perPulseDv, perPulseDv * availablePulses));

        var request = new SlingPlanner.Request(
            burnState, cp.BodyId, cp.SimTime,
            RequestedPassDistance: _slingPassRadii * SlingBodyRadius(),
            Side: _slingSide,
            MaxDeltaV: cap,
            PulseDeltaV: perPulseDv);

        // The client's WASM is IL-interpreted; the full 22-body ephemeris makes the dozens of
        // near-planet flights the solve needs unbearably slow. Run the SOLVE on a reduced ephemeris
        // (the sun + the target's parent chain — the only bodies that shape a flyby at this range),
        // then re-summarize the shown verdict on the FULL ephemeris so every displayed number, and the
        // burn the plan flies, are honest to the real physics.
        (Simulator solveSim, ICelestialEphemeris solveEph) = BuildSlingSolveContext(cp.BodyId);

        SlingPlanner.Result raw = SlingPlanner.Solve(solveSim, solveEph, request, maxIterations: 30);
        if (!raw.Ok)
        {
            _slingFailure = raw.Failure;
            _slingResult = null;
            _slingSolving = false;
            StateHasChanged();
            return;
        }

        // Quantize to whole pulses, then re-summarize at the quantized Δv on the FULL ephemeris.
        int pulses = Math.Max(1, (int)Math.Round(raw.DeltaVMagnitude / perPulseDv));
        Vector2d dir = raw.DeltaV.Normalized();
        Vector2d quantizedDv = dir * (pulses * perPulseDv);
        _slingPulses = pulses;
        _slingHeadingDeg = Math.Atan2(quantizedDv.Y, quantizedDv.X) * 180.0 / Math.PI;

        _slingResult = SlingPlanner.Summarize(_simulator, _ephemeris, request, quantizedDv);
        _slingSolving = false;
        StateHasChanged();
    }

    // A reduced ephemeris/simulator for the SOLVE only: the primary (sun), the target, its parent
    // chain, and the target's OWN MOONS. A flyby is shaped by the sun, the target's well, and — when
    // the pass threads the target's moon system, as a Jupiter pass does the Galilean moons — those
    // moons; every other body (sibling planets, their moons, distant stations, all >4 AU away for a
    // Jupiter pass) is negligible there yet dominates the per-step cost on IL-interpreted WASM.
    // Dropping only the negligible bodies keeps the solved trajectory faithful; the full-ephemeris
    // re-summary then reports the true, honest pass.
    private (Simulator Sim, ICelestialEphemeris Eph) BuildSlingSolveContext(string targetId)
    {
        var ids = new HashSet<string>();
        // Target + its parent chain up to the root.
        string? cur = targetId;
        while (cur is not null && ids.Add(cur))
        {
            cur = _ephemeris!.Bodies.FirstOrDefault(b => b.Id == cur)?.ParentId;
        }
        // The target's own moons (children) — they share the encounter region.
        foreach (CelestialBody b in _ephemeris!.Bodies)
        {
            if (b.ParentId == targetId)
            {
                ids.Add(b.Id);
            }
        }
        // Ensure the primary (heaviest parentless body — the sun) anchors the heliocentric frame.
        CelestialBody? root = _ephemeris.Bodies
            .Where(b => b.ParentId is null)
            .OrderByDescending(b => b.Mu)
            .FirstOrDefault();
        if (root is not null)
        {
            ids.Add(root.Id);
        }

        var bodies = _ephemeris.Bodies.Where(b => ids.Contains(b.Id)).ToList();
        var eph = new CircularOrbitEphemeris(bodies);
        return (new Simulator(eph, timeStepSeconds: 1.0), eph);
    }

    // Add the solved sling as a Vector-burn node (per #84 semantics: Δv along HeadingDegrees, per-pulse
    // = Percent% of entry speed), reproject, and let the ribbon bend through the pass.
    private void AddSlingBurn()
    {
        if (_slingResult is null || _slingPulses < 1)
        {
            return;
        }
        if (PlannedPulseTotal() + _slingPulses > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass for the aiming burn");
            return;
        }

        _planNodes.Add(new PlanNode
        {
            SimTime = _slingBurnTime,
            Action = ManeuverAction.Accelerate, // ignored for a Vector burn, but a sane default
            Pulses = _slingPulses,
            Percent = SlingBurnPercent,
            Mode = BurnMode.Vector,
            HeadingDegrees = _slingHeadingDeg,
        });
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
        ShowPulseMessage($"Sling burn laid in — {_slingPulses} pulse{(_slingPulses == 1 ? "" : "s")} at {_slingHeadingDeg:F0}° ⤴");
        _openEditor = FlightEditorKind.None; // PR-D2: committed — collapse the scratchpad; the step now lives in the list
        _slingResult = null;
    }

    // Precomputed verdict lines (no inner quotes / no markup in @onclick — the plot-desk idiom).
    private string SlingPassLine(SlingPlanner.Result r) =>
        $"Pass: {FormatDistance(r.AchievedPassDistance)} ({r.AchievedPassDistance / SlingBodyRadius():F1} R) at {FormatSimTime(r.PassEpoch)}";

    private string SlingBurnLine(SlingPlanner.Result r) =>
        $"Aiming burn: {r.DeltaVMagnitude:F0} m/s · {_slingPulses} pulse{(_slingPulses == 1 ? "" : "s")} (Vector, {_slingHeadingDeg:F0}°)";

    private string SlingOutcomeLine(SlingPlanner.Result r) =>
        (r.SpeedGain >= 0 ? $"Crank: +{r.SpeedGain:F0} m/s" : $"Crank: {r.SpeedGain:F0} m/s")
        + " · " + (r.Escapes ? "escapes the sun" : $"apoapsis {r.ApoapsisAU:F2} AU");

    private string SlingLeverLine(SlingPlanner.Result r) =>
        $"Lever: ±1 pulse of aim ⇒ ±{r.LeverGm:F1} Gm at the far end — re-trim after the pass";

    // PR-G dev cheat (/map?sling=<bodyId>): place the ship on an inbound arc whose closest pass by the
    // body lands ~12 days out (inside the default 30-day plot), so the ⤴ Sling panel is testable at
    // once. Deterministic seed (labs' aiming setup): a slow, Hohmann-ish v_inf retrograde to the
    // body's orbital motion, backed off along the ship's velocity so a coast reaches an off-center
    // point at the encounter — the body's own gravity then draws it into a genuine close flyby.
    private void SeedSlingCheat(string bodyId)
    {
        if (_ephemeris is null)
        {
            return;
        }

        CelestialBody? body = SlingBody(bodyId, out _);
        if (body is null)
        {
            ShowPulseMessage($"🧪 sling cheat: '{bodyId}' isn't a body with a parent to sling past");
            return;
        }

        double now = _ship.SimTime;
        const double passLead = 14 * 86400.0;
        double tCA = now + passLead;
        const double h = 1.0;
        Vector2d jCA = _ephemeris.Position(bodyId, tCA);
        Vector2d jVel = (_ephemeris.Position(bodyId, tCA + h) - _ephemeris.Position(bodyId, tCA - h)) / (2 * h);

        Vector2d vinf = -jVel.Normalized() * 9000.0;         // slow retrograde arrival — both flanks gain
        Vector2d vShipCA = jVel + vinf;
        Vector2d vHat = vShipCA.Normalized();
        var perp = new Vector2d(-vHat.Y, vHat.X);
        Vector2d startPos = jCA + perp * (18 * body.BodyRadius) - vShipCA * passLead;

        _ship = new ShipState(startPos, vShipCA, now, _ship.Charge);
        _destinationBodyId = bodyId;
        _armedOrbitBodyId = null;
        _planNodes.Clear();
        RebuildPlan();
        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        ShowPulseMessage($"🧪 sling cheat: inbound to {body.Name} — a close pass ~12 days out. Open Plot ▸ ⤴ Sling.");
    }

    // ---- PR-I · the skim & skip (the corridor gauge behind a plot-desk panel) ----

    // A close pass we can dip the cloud tops on: a planet/moon with an ATMOSPHERE, the pass ahead of us,
    // above the surface, and inside the Hill sphere where the aim can bend the periapsis into the shell.
    private bool PassIsSkimmable(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null || cp.SimTime <= SimTime + 60)
        {
            return false;
        }

        CelestialBody? body = SlingBody(cp.BodyId, out CelestialBody? parent);
        if (body is null || parent is null || body.Kind == BodyKind.Station || body.Atmosphere is null)
        {
            return false;
        }

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        return cp.Distance > body.BodyRadius && cp.Distance < hill;
    }

    private Atmosphere? SkimAtmosphere() =>
        _skimmablePass is { } cp && SlingBody(cp.BodyId, out _) is { } b ? b.Atmosphere : null;

    private double SkimShellTopKm() => (SkimAtmosphere()?.TopAltitude ?? 4.0e5) / 1000.0;

    // Open/close the panel. Opening seeds a mid-corridor default depth (a fraction of the shell top that
    // lands in the useful-braking band for the tuned gas giants — the gauge shows the truth per body).
    // PR-D2: open/close the skim compose editor through the accordion (closes any other open step).
    private void ToggleSkimPanel()
    {
        bool opening = _openEditor != FlightEditorKind.Skim;
        _openEditor = opening ? FlightEditorKind.Skim : FlightEditorKind.None;
        _selectedPlanNode = null;
        _skimResult = null;
        _skimFailure = null;
        if (opening && SkimAtmosphere() is { } atm)
        {
            _skimAltKm = Math.Round(0.4 * atm.TopAltitude / 1000.0); // mid-corridor default
        }
    }

    private void OnSkimAltInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double km))
        {
            _skimAltKm = Math.Clamp(km, 1, SkimShellTopKm());
            _skimResult = null;
            _skimFailure = null;
        }
    }

    // SOLVE: aim the periapsis to the requested altitude by a VACUUM-periapsis bisection on a signed
    // perp-⟂-v_rel trim (the sling's aim frame, but the oracle is a cheap gravity-only pass because a
    // sub-shell periapsis is far under SlingPlanner's 0.1 R measurement floor), quantize to whole fine
    // pulses, then fly the quantized plan ONCE through the pass with RunAdaptiveWithDrag — the gauge
    // numbers ARE that flight. Runs on a reduced ephemeris (sun + the target's well + its moons), the
    // same WASM-affordability trick the sling uses; the in-shell pass is entirely the target's to shape.
    private async Task RunSkimSolveAsync()
    {
        if (_ephemeris is null || _simulator is null || _skimmablePass is not { } cp
            || SlingBody(cp.BodyId, out _) is not { Atmosphere: { } atm } body)
        {
            return;
        }

        _skimSolving = true;
        _skimResult = null;
        _skimFailure = null;
        StateHasChanged();
        await Task.Yield(); // let "flying the pass…" paint before the synchronous solve

        double R = body.BodyRadius, mu = body.Mu;
        double shellTop = atm.TopAltitude;

        // The aiming-burn node: a modest lever close to the pass (12 h before), else 10 min from now.
        double tBurn = cp.SimTime - SkimBurnLeadSeconds;
        if (tBurn <= _ship.SimTime + 300)
        {
            tBurn = Math.Floor(_ship.SimTime) + 600;
        }
        _skimBurnTime = tBurn;

        ShipState burnState = _simulator.RunAdaptive(_ship, Math.Max(1.0, tBurn - _ship.SimTime), _plan);
        double burnSpeed = burnState.Velocity.Length;
        double perPulseDv = Math.Max(0.3, burnSpeed * SkimBurnPercent / 100.0);
        int availablePulses = Math.Max(1, _reactionMassPulses - PlannedPulseTotal());
        double cap = Math.Min(SkimMaxAimDeltaV, Math.Max(perPulseDv, perPulseDv * availablePulses));

        // Reduced ephemerides: one gravity-only (the bisection oracle), one carrying the target's air (the gauge).
        (Simulator vacSim, Simulator airSim, ICelestialEphemeris redEph) = BuildSkimContext(cp.BodyId);

        Vector2d BodyVel(ICelestialEphemeris eph, string id, double t) =>
            (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

        Vector2d vRel = burnState.Velocity - BodyVel(redEph, body.Id, tBurn);
        Vector2d vHat = vRel.Normalized();
        var perp = new Vector2d(-vHat.Y, vHat.X);

        // Vacuum periapsis (m) of the aim burnState + perp*alpha, measured against the reduced grav field.
        double horizon = (cp.SimTime - tBurn) + 4 * 86400.0;
        double VacPeri(double alpha)
        {
            var start = new ShipState(burnState.Position, burnState.Velocity + perp * alpha, tBurn);
            IReadOnlyList<TrajectorySample> path = vacSim.ProjectAdaptive(
                start, null, horizon, minTimeStep: 5, maxTimeStep: 3 * 3600, dynamicalTimeFraction: 1.0 / 96, maxSamples: 30_000);
            double best = double.MaxValue;
            foreach (TrajectorySample s in path)
            {
                if (s.SimTime < tBurn)
                {
                    continue;
                }
                double d = (redEph.Position(body.Id, s.SimTime) - s.Position).Length;
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        double target = R + _skimAltKm * 1000.0;
        double fLo = VacPeri(-cap) - target;
        double fHi = VacPeri(+cap) - target;
        if (double.IsNaN(fLo) || double.IsNaN(fHi) || fLo * fHi > 0)
        {
            _skimFailure = "no aim this cheap threads that depth — widen the budget or ease the dip";
            _skimSolving = false;
            StateHasChanged();
            return;
        }

        // Bisect the signed perp trim to the requested periapsis (monotonic in alpha; ~24 cheap flights).
        double aLo = -cap, aHi = +cap;
        for (int i = 0; i < 24; i++)
        {
            double aMid = 0.5 * (aLo + aHi);
            double fMid = VacPeri(aMid) - target;
            if (fMid * fLo <= 0)
            {
                aHi = aMid;
            }
            else
            {
                aLo = aMid;
                fLo = fMid;
            }
        }

        double alphaStar = 0.5 * (aLo + aHi);
        int pulses = Math.Max(0, (int)Math.Round(Math.Abs(alphaStar) / perPulseDv));
        double signedMag = Math.Sign(alphaStar) * pulses * perPulseDv;
        Vector2d quantizedDv = perp * signedMag;
        _skimPulses = pulses;
        _skimHeadingDeg = pulses > 0
            ? Math.Atan2(quantizedDv.Y, quantizedDv.X) * 180.0 / Math.PI
            : 0.0;

        // Fly the QUANTIZED aim through the pass with drag — the gauge is this flight, not the request.
        _skimResult = FlySkimGauge(airSim, redEph, body, burnState, quantizedDv, mu, R, shellTop, burnSpeed);
        _skimSolving = false;
        StateHasChanged();
    }

    // One RunAdaptiveWithDrag pass of the quantized aim: peak g, Δv shed, min altitude, exit verdict —
    // every gauge number measured off the real drag flight. SINGLE-PASS numbers (fine-step accurate);
    // multi-pass planning is out of scope (see the panel's fine print).
    private SkimGauge FlySkimGauge(
        Simulator airSim, ICelestialEphemeris eph, CelestialBody body, ShipState burnState, Vector2d aimDv,
        double mu, double R, double shellTop, double burnSpeed)
    {
        Vector2d BodyPos(double t) => eph.Position(body.Id, t);
        Vector2d BodyVel(double t) => (eph.Position(body.Id, t + 1.0) - eph.Position(body.Id, t - 1.0)) / 2.0;

        var start = new ShipState(burnState.Position, burnState.Velocity + aimDv, burnState.SimTime);

        // Arrival hyperbolic about the body? (sets whether "too shallow" reads as a skip).
        Vector2d vRel0 = start.Velocity - BodyVel(start.SimTime);
        double r0 = (start.Position - BodyPos(start.SimTime)).Length;
        bool arrivalHyperbolic = vRel0.LengthSquared / 2.0 - mu / r0 > 0;

        // Find the pass epoch under the aim, then fly the shell crossing at fine resolution.
        double horizon = 8 * 86400.0;
        IReadOnlyList<TrajectorySample> path = airSim.ProjectAdaptive(
            start, null, horizon, minTimeStep: 5, maxTimeStep: 3 * 3600, dynamicalTimeFraction: 1.0 / 96, maxSamples: 40_000);
        double bestD = double.MaxValue, tPass = start.SimTime;
        foreach (TrajectorySample s in path)
        {
            if (s.SimTime < start.SimTime)
            {
                continue;
            }
            double d = (BodyPos(s.SimTime) - s.Position).Length;
            if (d < bestD)
            {
                (bestD, tPass) = (d, s.SimTime);
            }
        }

        double shellR = R + shellTop;
        ShipState s2 = airSim.RunAdaptive(start, Math.Max(1.0, (tPass - 2 * 3600) - start.SimTime));
        double peak = 0, shed = 0, minAlt = double.PositiveInfinity, t0 = s2.SimTime;
        bool entered = false;
        while (s2.SimTime - t0 < 12 * 3600)
        {
            (ShipState next, Simulator.DragReport rep) =
                airSim.RunAdaptiveWithDrag(s2, 20.0, null, minTimeStep: 0.1, maxTimeStep: 1.0);
            peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
            shed += rep.DeltaVShedMetersPerSecond;
            if (!double.IsNaN(rep.MinAltitudeMeters))
            {
                minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
            }
            s2 = next;
            double r = (BodyPos(s2.SimTime) - s2.Position).Length;
            if (r < shellR)
            {
                entered = true;
            }
            else if (entered)
            {
                break;
            }
        }

        // Clean post-pass energy about the body (propagate clear of the shell), for capture / exit v∞.
        ShipState post = airSim.RunAdaptive(s2, 12 * 3600);
        double rr = (BodyPos(post.SimTime) - post.Position).Length;
        double relv = (post.Velocity - BodyVel(post.SimTime)).Length;
        double e = relv * relv / 2.0 - mu / rr;
        bool captured = e < 0;
        double exitVinf = captured ? 0 : Math.Sqrt(2 * e);

        double achievedAlt = double.IsPositiveInfinity(minAlt) ? shellTop : minAlt;
        double pulsesSaved = shed / Math.Max(1.0, 0.10 * burnSpeed); // vs a −10% drive pulse at the pass entry speed

        return new SkimGauge(
            _skimPulses, _skimHeadingDeg, _skimBurnTime,
            achievedAlt, shed, peak / 9.80665, pulsesSaved,
            captured, exitVinf, arrivalHyperbolic,
            _skimAltKm, achievedAlt / 1000.0);
    }

    // A reduced ephemeris for the skim: the primary (sun), the target, its parent chain, and the
    // target's moons — the only bodies that shape an in-shell pass. Returns a gravity-only sim (the
    // bisection oracle), an atmosphere-carrying sim (the gauge flight), and the shared ephemeris. Same
    // reduction rationale as BuildSlingSolveContext: drop the negligible bodies that dominate WASM cost.
    private (Simulator VacSim, Simulator AirSim, ICelestialEphemeris Eph) BuildSkimContext(string targetId)
    {
        var ids = new HashSet<string>();
        string? cur = targetId;
        while (cur is not null && ids.Add(cur))
        {
            cur = _ephemeris!.Bodies.FirstOrDefault(b => b.Id == cur)?.ParentId;
        }
        foreach (CelestialBody b in _ephemeris!.Bodies)
        {
            if (b.ParentId == targetId)
            {
                ids.Add(b.Id);
            }
        }
        CelestialBody? root = _ephemeris.Bodies.Where(b => b.ParentId is null).OrderByDescending(b => b.Mu).FirstOrDefault();
        if (root is not null)
        {
            ids.Add(root.Id);
        }

        var airBodies = _ephemeris.Bodies.Where(b => ids.Contains(b.Id)).ToList();
        var vacBodies = airBodies.Select(b => b with { Atmosphere = null }).ToList();
        var airEph = new CircularOrbitEphemeris(airBodies);
        var vacEph = new CircularOrbitEphemeris(vacBodies);
        return (new Simulator(vacEph, 1.0), new Simulator(airEph, 1.0), airEph);
    }

    // Add the solved skim as a fine Vector-burn node (like the sling). Allowed even for a too-deep plan —
    // a captain may fly into the red; the gauge warned honestly.
    private void AddSkimBurn()
    {
        if (_skimResult is not { } g)
        {
            return;
        }
        if (g.Pulses < 1)
        {
            ShowPulseMessage("No aim burn needed — this depth is the natural pass; add a burn only to change it");
            return;
        }
        if (PlannedPulseTotal() + g.Pulses > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass for the aiming burn");
            return;
        }

        _planNodes.Add(new PlanNode
        {
            SimTime = _skimBurnTime,
            Action = ManeuverAction.Accelerate,
            Pulses = g.Pulses,
            Percent = SkimBurnPercent,
            Mode = BurnMode.Vector,
            HeadingDegrees = g.HeadingDeg,
        });
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
        ShowPulseMessage(g.TooDeep
            ? $"Skim burn laid in — into the RED at {g.HeadingDeg:F0}° 🔥 mind the sail"
            : $"Skim burn laid in — {g.Pulses} pulse{(g.Pulses == 1 ? "" : "s")} at {g.HeadingDeg:F0}° 🔥");
        _openEditor = FlightEditorKind.None; // PR-D2: committed — collapse the scratchpad; the step now lives in the list
        _skimResult = null;
    }

    private string SkimDepthLine(SkimGauge g)
    {
        bool onTarget = Math.Abs(g.RequestedAltKm - g.AchievedAltKm) <= 1.0;
        string aim = onTarget ? "on target" : $"asked {g.RequestedAltKm:F0}";
        string pulses = g.Pulses == 1 ? "" : "s";
        return $"Periapsis: {g.AchievedAltKm:F0} km ({aim}) · aim {g.Pulses} pulse{pulses}";
    }

    private string SkimShedLine(SkimGauge g) =>
        g.TooDeep
            ? $"Δv shed {g.ShedMps:F0} m/s · peak {g.PeakG:F1} g — WOULD HOLE THE SAIL"
            : $"Δv shed {g.ShedMps:F0} m/s (≈{g.PulsesSaved:F1} pulses saved) · peak {g.PeakG:F2} g";

    private string SkimOutcomeLine(SkimGauge g)
    {
        if (g.TooShallow && g.ArrivalHyperbolic)
        {
            return $"Skip — she bounces back out at v∞ {g.ExitVinfMps / 1000:F1} km/s";
        }
        if (g.TooShallow)
        {
            return "Too shallow — barely touches the air";
        }
        return g.Captured ? "Captured — the air bound her into orbit" : $"Exits at v∞ {g.ExitVinfMps / 1000:F1} km/s";
    }

    private string SkimFinePrint() =>
        "single-pass numbers (fine-step); each dip creeps deeper — plan pass by pass";

    // PR-I dev cheat (/map?skim=<bodyId>): boot the ship on a fast HYPERBOLIC inbound whose natural pass
    // grazes the body's cloud tops ~3 days out, so the 🔥 Skim panel's corridor gauge is reachable at
    // once (the natural pass already sits mid-corridor). Reuses the sling cheat's proven construction —
    // a retrograde hyperbolic excess about the body, backed off along the heliocentric velocity — and
    // BISECTS the impact parameter so the flown natural periapsis lands mid-corridor against the real
    // integrator (the encounter geometry is not analytic, so we solve it numerically).
    private void SeedSkimCheat(string bodyId)
    {
        if (_ephemeris is null)
        {
            return;
        }

        CelestialBody? body = SlingBody(bodyId, out _);
        if (body is null || body.Atmosphere is null)
        {
            ShowPulseMessage($"🧪 skim cheat: '{bodyId}' has no atmosphere to skim");
            return;
        }

        double now = _ship.SimTime;
        double R = body.BodyRadius;
        const double passLead = 3.0 * 86400.0;         // ~3 days out — inside the plot horizon, geometry still clean
        double tCA = now + passLead;
        Vector2d jCA = _ephemeris.Position(bodyId, tCA);
        Vector2d jVel = (_ephemeris.Position(bodyId, tCA + 1.0) - _ephemeris.Position(bodyId, tCA - 1.0)) / 2.0;

        const double vInfMag = 14000.0;                 // solidly hyperbolic at the body (the heliocentric approach bleeds some off): shallow → skip out, deep → holes the sail
        Vector2d vinf = -jVel.Normalized() * vInfMag;   // retrograde arrival
        Vector2d vShipCA = jVel + vinf;
        Vector2d vInfHat = vinf.Normalized();
        var perp = new Vector2d(-vInfHat.Y, vInfHat.X); // the impact-parameter direction

        // Flown natural periapsis for an impact parameter b: start off-axis, backed off along the
        // heliocentric velocity so a coast reaches the offset point at the encounter; the body's gravity
        // then focuses it into a genuine close pass.
        ShipState BuildAt(double b) => new(jCA + perp * b - vShipCA * passLead, vShipCA, now, _ship.Charge);
        double NaturalPeri(double b)
        {
            IReadOnlyList<TrajectorySample> path = _simulator!.ProjectAdaptive(
                BuildAt(b), null, passLead + 3 * 86400.0, minTimeStep: 20, maxTimeStep: 3 * 3600, dynamicalTimeFraction: 1.0 / 96, maxSamples: 30_000);
            double best = double.MaxValue;
            foreach (TrajectorySample s in path)
            {
                if (s.SimTime < now)
                {
                    continue;
                }
                double d = (_ephemeris.Position(bodyId, s.SimTime) - s.Position).Length;
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        // Bisect the impact parameter so the flown natural periapsis lands mid-corridor (gravity focuses
        // a ~12 R offset down to a graze, so the periapsis rises monotonically with b past the impact b).
        double targetPeri = R + 0.4 * body.Atmosphere.TopAltitude; // mid-corridor, matching the panel's default depth
        double lo = 3 * R, hi = 30 * R;
        for (int i = 0; i < 34; i++)
        {
            double mid = 0.5 * (lo + hi);
            if (NaturalPeri(mid) > targetPeri)
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        _ship = BuildAt(0.5 * (lo + hi));
        _destinationBodyId = bodyId;
        _armedOrbitBodyId = null;
        _planNodes.Clear();
        RebuildPlan();
        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        ShowPulseMessage($"🧪 skim cheat: hyperbolic inbound to {body.Name}, cloud tops ~2 days out. Open Plot ▸ 🔥 Skim.");
    }

    private void StaleFutureNodes()
    {
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && !node.Executed && node.SimTime > _ship.SimTime)
            {
                node.Stale = true;
            }
        }
        RebuildPlan();
        ReprojectTrajectory();
    }

    // PR-I · the live consequence. A cloud-top dip whose peak drag deceleration crosses the Core damage
    // line (Atmosphere.SailHoleDecelG) holes the sail: thrust and every pending burn are disabled for a
    // fixed repair window while the crew sews. Deterministic — driven only by the flown drag, no RNG.
    private void CheckSailHole()
    {
        if (!_sailHoled && _frameMaxDragDecel / 9.80665 >= Atmosphere.SailHoleDecelG)
        {
            _sailHoled = true;
            _sailRepairedAtSimTime = _ship.SimTime + SailRepairSeconds;
            StaleFutureNodes(); // the burns she can no longer make
            ShowPulseMessage("🔥 The rigging screams — sail holed in the cloud tops; the crew is sewing");
            RendererInterop.PlayCue("board");
        }
        else if (_sailHoled && _ship.SimTime >= _sailRepairedAtSimTime)
        {
            _sailHoled = false;
            ShowPulseMessage("🪡 Sail sewn shut — the drive answers again");
        }
    }

    // #135 — the ONE place a world sample gets re-expressed in the active frame before it hits the
    // camera. Sun/inertial (no frame body) returns the sample untouched — the pre-#135 path,
    // byte-identical. Otherwise it's ReferenceFrame.CoMoving with the frame body's ephemeris position
    // at the SAMPLE time and at "now" (_plotFrameAnchor, refreshed once per drawn frame). Every
    // time-parameterized draw funnels through this so nothing diverges. Anything drawn at "now" (live
    // bodies, the ship, NPCs) is the identity here anyway (bodyF(now) − bodyF(now) = 0), so those keep
    // their untouched WorldToScreen call and never move under a frame change.
    //
    // #143 — the frame governs BOTH views now, not just Plot. The owner hit a heliocentric ribbon/scrub
    // path while auto-orbiting Titan inside Saturn's system: #138 deliberately gated this on Plot mode,
    // which left the Play-view ribbon and prediction cone solar. One selection, both views — so there is
    // no PlotMode gate here anymore. The anchor is refreshed every drawn frame regardless of mode, and
    // the Sun frame (_plotFrameBodyId is null) is still the byte-identical short-circuit.
    private Vector2d PlotFrame(Vector2d world, double simTime)
    {
        if (_plotFrameBodyId is null || _ephemeris is null)
        {
            return world;
        }
        return ReferenceFrame.CoMoving(world, _ephemeris.Position(_plotFrameBodyId, simTime), _plotFrameAnchor);
    }

    // #135 — one entry in the plot's frame selector. Id == null is Sun/inertial.
    private sealed record FrameOption(string? Id, string Label, string Title, bool Suggested);

    // The frames worth offering, in reading order: Sun (inertial), then any body whose Hill sphere
    // currently holds the ship (the local giant) and that giant's moons, then the nav target / picked
    // contact, and always the frame in use so you can never get stranded in a frame with no chip.
    private List<FrameOption> FrameOptions()
    {
        string? suggestId = SuggestedFrameBodyId();
        var opts = new List<FrameOption> { new(null, "Sun", "Heliocentric (inertial) — the default solar frame", false) };
        if (_ephemeris is null)
        {
            return opts;
        }

        var seen = new HashSet<string>();
        void Add(string? id)
        {
            if (id is null || id == "sun" || !seen.Add(id))
            {
                return;
            }
            CelestialBody? b = _ephemeris.Bodies.FirstOrDefault(x => x.Id == id);
            if (b is null)
            {
                return;
            }
            opts.Add(new FrameOption(b.Id, b.Name, $"Draw the plotted course co-moving with {b.Name}", b.Id == suggestId));
        }

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (!ShipInsideHill(body))
            {
                continue;
            }
            Add(body.Id);
            foreach (CelestialBody child in _ephemeris.Bodies)
            {
                if (child.ParentId == body.Id && child.Kind != BodyKind.Station)
                {
                    Add(child.Id);
                }
            }
        }
        Add(_destinationBodyId);
        if (_selectedTargetId is not null && _ephemeris.Bodies.Any(b => b.Id == _selectedTargetId))
        {
            Add(_selectedTargetId);
        }
        Add(_plotFrameBodyId);   // never orphan the active frame
        return opts;
    }

    // The frame to nudge the player toward: the giant (a body that owns a moon system) whose Hill
    // sphere currently holds the ship. Largest Hill wins, so from inside a moon's Hill sphere it's
    // still the giant that gets suggested, not the moon. Never auto-applied — just highlighted.
    private string? SuggestedFrameBodyId()
    {
        if (_ephemeris is null)
        {
            return null;
        }
        string? best = null;
        double bestHill = 0;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            bool ownsMoons = _ephemeris.Bodies.Any(c => c.ParentId == body.Id && c.Kind != BodyKind.Station);
            if (!ownsMoons || !ShipInsideHill(body, out double hill))
            {
                continue;
            }
            if (hill > bestHill)
            {
                (bestHill, best) = (hill, body.Id);
            }
        }
        return best;
    }

    private bool ShipInsideHill(CelestialBody body) => ShipInsideHill(body, out _);

    private bool ShipInsideHill(CelestialBody body, out double hill)
    {
        hill = 0;
        if (_ephemeris is null || body.ParentId is null)
        {
            return false;
        }
        CelestialBody? parent = _ephemeris.Bodies.FirstOrDefault(b => b.Id == body.ParentId);
        if (parent is null)
        {
            return false;
        }
        hill = OrbitRule.HillRadius(body, parent.Mu);
        double distance = (_ship.Position - _ephemeris.Position(body.Id, SimTime)).Length;
        return distance < hill;
    }

    // #135 — the ship's speed IN the selected frame, labelled with it, so the number on the plot
    // panel never silently disagrees with the frame chip (the documented mixed-frame trap).
    private string FrameSpeedReadout()
    {
        if (_ephemeris is null)
        {
            return string.Empty;
        }
        Vector2d frameVel = Vector2d.Zero;
        if (_plotFrameBodyId is not null)
        {
            const double h = 1.0;
            frameVel = (_ephemeris.Position(_plotFrameBodyId, SimTime + h) - _ephemeris.Position(_plotFrameBodyId, SimTime - h)) / (2 * h);
        }
        double relKmps = (_ship.Velocity - frameVel).Length / 1000.0;
        string label = _plotFrameBodyId is null ? "v helio" : $"v rel {BodyName(_plotFrameBodyId)}";
        return $"{label}: {relKmps.ToString("N1", CultureInfo.InvariantCulture)} km/s";
    }

    private void SetPlotFrame(string? bodyId)
    {
        _plotFrameBodyId = bodyId;
        if (bodyId is not null && _ephemeris is not null)
        {
            _plotFrameAnchor = _ephemeris.Position(bodyId, SimTime);
        }
    }

    // #206 — the every-body frame overflow. Controlled: its value mirrors the live frame (empty = the
    // "frame… ▾" placeholder, which is the Sun / inertial default), so picking never leaves the control
    // out of step with the active origin. The pick also flows through SetPlotFrame, so the chip row and
    // both views stay one truth (#144).
    private void OnFramePicked(ChangeEventArgs e)
    {
        string? picked = e.Value?.ToString();
        SetPlotFrame(string.IsNullOrEmpty(picked) ? null : picked);
    }

    // #206 — every body in the scenario, grouped by parent for the overflow picker: the Sun's children
    // (the planets, plus any sun-orbiting station) under "Planets", then each planet's moons + stations
    // under the planet's name. Body order within a group follows the ephemeris. A parent with no
    // children yields no group.
    private List<(string Label, List<CelestialBody> Members)> FramePickerGroups()
    {
        var groups = new List<(string, List<CelestialBody>)>();
        if (_ephemeris is null)
        {
            return groups;
        }
        foreach (CelestialBody parent in _ephemeris.Bodies)
        {
            List<CelestialBody> members = _ephemeris.Bodies.Where(b => b.ParentId == parent.Id).ToList();
            if (members.Count == 0)
            {
                continue;
            }
            groups.Add((parent.Id == "sun" ? "Planets" : parent.Name, members));
        }
        return groups;
    }

    // #145 — the DISPLAYED trajectory length, scaled to the frame's local timescale. null in the
    // Sun/inertial frame (draw the full ribbon, byte-identical to pre-#145) or when the frame body
    // has no local orbit to scale to (the Sun itself, a mass-less dock). Otherwise: how many seconds
    // of the ribbon to draw — ~1.25 local orbital periods at the ship's CURRENT radius around the
    // frame body (one sqrt), floored so the imminent step is never hidden and ceilinged at the full
    // projection. Recomputed per frame, so the arc tightens as the ship falls in.
    private double? FrameDisplayWindowSeconds()
    {
        if (FrameLocalWindowSeconds() is null)
        {
            return null; // the Sun / inertial frame, or a mass-less dock — no truncation, full ribbon
        }
        return ResolveRibbon().DrawnSeconds;
    }

    // #145/#209 — ~1.25 local orbital periods of the current frame body (one sqrt), or null when there
    // is nothing to scale to: the Sun / inertial frame, or a mass-less dock. Null is the byte-identical
    // full-length draw path; a value is the local timescale the drawn ribbon is scaled to.
    private double? FrameLocalWindowSeconds()
    {
        if (_plotFrameBodyId is null || _ephemeris is null)
        {
            return null;
        }
        CelestialBody? frame = _ephemeris.Bodies.FirstOrDefault(b => b.Id == _plotFrameBodyId);
        if (frame is null || frame.ParentId is null || frame.Mu <= 0)
        {
            return null;
        }
        double radius = (_ship.Position - _ephemeris.Position(frame.Id, SimTime)).Length;
        if (!(radius > 0))
        {
            return null;
        }
        return FrameWindowLocalPeriods * OrbitRule.LocalOrbitPeriod(radius, frame.Mu);
    }

    // #145.5 — the frame-INDEPENDENT floor: never hide the near-term course. A few hours, stretched to
    // the next imminent FUTURE plan node + margin so the flight plan's NEXT line and the ribbon can't
    // contradict. Plan-awareness (the FURTHEST encounter) is folded in by PlotHorizon.DrawnWindow.
    private double FrameWindowBaseFloorSeconds()
    {
        double floor = FrameWindowFloorSeconds;
        double nextNode = double.PositiveInfinity;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && node.SimTime > SimTime && node.SimTime < nextNode)
            {
                nextNode = node.SimTime;
            }
        }
        if (double.IsFinite(nextNode))
        {
            floor = Math.Max(floor, (nextNode - SimTime) + FrameWindowNodeMarginSeconds);
        }
        return floor;
    }

    // #145/#209 — the resolved drawn ribbon window + the honest note explaining its length. ONE call, so
    // the drawn ribbon (DrawShipTrajectory) and the panel note (RibbonHorizonNote) never disagree.
    private PlotHorizon.RibbonResult ResolveRibbon() =>
        PlotHorizon.DrawnWindow(
            CurrentPlotHorizonSeconds, FrameLocalWindowSeconds() ?? 0, PlanFurthestEpochSeconds(), FrameWindowBaseFloorSeconds());

    // #209 — the say-the-state note for the Plotting panel: when the ribbon is cropped shorter than the
    // full picture, name WHY, so the captain never reads a silently short path. null when the ribbon
    // reaches the plan / the full projection.
    private (string Text, bool Warn)? RibbonHorizonNote()
    {
        PlotHorizon.RibbonResult r = ResolveRibbon();
        return r.Note switch
        {
            // FrameLocalPeriods implies a scalable frame body (local window > 0), so it is never null here.
            PlotHorizon.RibbonNote.FrameLocalPeriods =>
                ($"ribbon: {FrameWindowLocalPeriods.ToString("0.##", CultureInfo.InvariantCulture)} {BodyName(_plotFrameBodyId!)} periods (frame auto)", false),
            PlotHorizon.RibbonNote.CappedShortOfPlan =>
                ($"ribbon capped at {FormatHorizon(r.DrawnSeconds)} — plan runs to {FormatHorizon(PlanFurthestEpochSeconds())}", true),
            _ => null,
        };
    }

    private void DrawShipTrajectory()
    {
        // #148: while the autopilot has the ship and a rehearsed plan to draw, the ballistic ribbon
        // shows loops the ship will never fly (the owner's report) — hand the line over to the
        // intended-path draw. The ballistic ribbon stays for manual flight.
        if (_armedOrbitBodyId is not null && _autopilotPlanPath is { Count: >= 2 }) return;
        if (_samples.Count < 2) return;

        int requiredSize = _samples.Count * 2;
        if (_scratch.Length < requiredSize)
        {
            _scratch = new float[requiredSize];
        }

        for (int i = 0; i < _samples.Count; i++)
        {
            (float x, float y) = _camera.WorldToScreen(PlotFrame(_samples[i].Position, _samples[i].SimTime));
            _scratch[i * 2] = x;
            _scratch[i * 2 + 1] = y;
        }

        // Sun frame (or a frame with no local orbit): the pre-#145 flat polyline, byte-identical.
        double? window = FrameDisplayWindowSeconds();
        if (window is null)
        {
            _renderer!.DrawPolyline(_scratch.AsSpan(0, _samples.Count * 2), TrajectoryColor);
            return;
        }

        // A Hill-sphere frame: draw only the first `window` seconds of the ribbon, ending SOFTLY with
        // the #110 fade. The full-length data stays in _samples — scrubbing, ETA, closest-pass and
        // node markers all keep reading it; only the drawn ribbon is clipped. (Node markers past the
        // window still draw as dots — just without the connecting ribbon; the floor rule guarantees
        // the NEXT node is inside the solid part.)
        double cutoff = _ship.SimTime + window.Value;
        double fadeSpan = Math.Clamp(window.Value * FrameRibbonFadeFraction, FrameRibbonFadeMinSeconds, FrameRibbonFadeMaxSeconds);
        double fadeStart = cutoff - fadeSpan;

        // Last vertex to draw: the first sample at/after the cutoff (so the fade reaches zero just past
        // it), capped to the data.
        int end = _samples.Count - 1;
        for (int i = 1; i < _samples.Count; i++)
        {
            if (_samples[i].SimTime >= cutoff)
            {
                end = i;
                break;
            }
        }
        if (end < 1)
        {
            end = 1;
        }

        // Coalesce consecutive same-alpha segments into single DrawPolyline runs (the #110 idiom):
        // alpha is quantized into buckets, so a long ribbon still strokes in a handful of runs. Runs
        // are contiguous index ranges into _scratch and share a vertex, so the line stays connected.
        int runStartVertex = 0;
        int runBucket = FadeBucket(0.5 * (_samples[0].SimTime + _samples[1].SimTime), fadeStart, fadeSpan);
        for (int seg = 2; seg <= end; seg++)
        {
            int bucket = FadeBucket(0.5 * (_samples[seg - 1].SimTime + _samples[seg].SimTime), fadeStart, fadeSpan);
            if (bucket != runBucket)
            {
                EmitRibbonRun(runStartVertex, seg - 1, runBucket);
                runStartVertex = seg - 1;
                runBucket = bucket;
            }
        }
        EmitRibbonRun(runStartVertex, end, runBucket);
    }

    // One run of the faded ribbon: vertices [firstVertex..lastVertex] (a contiguous slice of _scratch)
    // stroked at TrajectoryColor scaled by the run's alpha bucket. Bucket 0 = invisible, skipped.
    private void EmitRibbonRun(int firstVertex, int lastVertex, int bucket)
    {
        if (lastVertex <= firstVertex || bucket <= 0)
        {
            return;
        }
        float a = (float)bucket / FrameRibbonFadeBuckets;
        RgbaColor color = TrajectoryColor with { A = (byte)(TrajectoryColor.A * a) };
        _renderer!.DrawPolyline(_scratch.AsSpan(firstVertex * 2, (lastVertex - firstVertex + 1) * 2), color);
    }

    // The #110 time-fade ramp, quantized: full strength up to fadeStart, then linear to zero across
    // fadeSpan. Segments past the cutoff land in bucket 0 (invisible).
    private static int FadeBucket(double midTime, double fadeStart, double fadeSpan)
    {
        double alpha = midTime <= fadeStart
            ? 1.0
            : Math.Max(0.0, 1.0 - (midTime - fadeStart) / fadeSpan);
        return (int)Math.Round(alpha * FrameRibbonFadeBuckets);
    }

    // #148: draw the autopilot's rehearsed INTENDED path (the arc it will actually fly to capture),
    // teal and dashed so it never reads as the amber ballistic ribbon. Only the part still ahead of
    // the ship is drawn; routed through PlotFrame like every time-parameterized track (#144).
    private void DrawAutopilotPlanPath()
    {
        if (_armedOrbitBodyId is null || _autopilotPlanPath is not { Count: >= 2 } plan)
        {
            return;
        }

        int startIdx = 0;
        while (startIdx < plan.Count - 1 && plan[startIdx].SimTime < _ship.SimTime)
        {
            startIdx++;
        }
        int remaining = plan.Count - startIdx;
        if (remaining < 2)
        {
            return;
        }

        int stride = Math.Max(1, remaining / 220);
        int maxPoints = remaining / stride + 2;
        if (_autopilotPlanScratch.Length < maxPoints * 2)
        {
            _autopilotPlanScratch = new float[maxPoints * 2];
        }

        int w = 0;
        for (int i = startIdx; i < plan.Count; i += stride)
        {
            (float x, float y) = _camera.WorldToScreen(PlotFrame(plan[i].Position, plan[i].SimTime));
            _autopilotPlanScratch[w] = x;
            _autopilotPlanScratch[w + 1] = y;
            w += 2;
        }
        (float lx, float ly) = _camera.WorldToScreen(PlotFrame(plan[^1].Position, plan[^1].SimTime));
        _autopilotPlanScratch[w] = lx;
        _autopilotPlanScratch[w + 1] = ly;
        w += 2;

        // Dashed: draw every other 2-point segment, so the teal plan reads as a distinct dashed arc.
        for (int i = 0; i + 3 < w; i += 4)
        {
            _renderer!.DrawPolyline(_autopilotPlanScratch.AsSpan(i, 4), AutopilotPlanColor, 2f);
        }
    }

    private void DrawClosestPassMarker()
    {
        if (_closestPass is not { } cp || cp.Severity > 25)
        {
            return; // beyond 25 radii nobody is embarrassed
        }

        (float sx, float sy) = _camera.WorldToScreen(PlotFrame(cp.ShipPosition, cp.SimTime));
        RgbaColor color = cp.Impact
            ? new RgbaColor(255, 80, 80, 230)
            : cp.Severity < 5 ? new RgbaColor(255, 200, 80, 220) : new RgbaColor(170, 190, 210, 160);
        _renderer!.DrawCircle(sx, sy, 8f, null, color, 1.5f);
        _renderer!.DrawCircle(sx, sy, 2f, color, color);
        _renderer!.DrawText(sx, sy - 14,
            cp.Impact ? $"IMPACT {cp.BodyName}" : $"min {cp.BodyName} {cp.Severity:0.0}R",
            color, "10px monospace", TextAlign.Center);
    }

    // M25: the target lock — loud enough to find at any zoom (owner: the destination was
    // impossible to spot in plot view). A ring plus four range ticks, like a gun-camera reticle.
    private void DrawTargetLock(float sx, float sy, float bodyRadiusPx, RgbaColor color, string? label)
    {
        float r = Math.Max(bodyRadiusPx + 8f, 16f);
        _renderer!.DrawCircle(sx, sy, r, null, color, 1.6f);
        Span<float> tick = stackalloc float[4];
        for (int i = 0; i < 4; i++)
        {
            float dx = i switch { 0 => 1f, 1 => -1f, _ => 0f };
            float dy = i switch { 2 => 1f, 3 => -1f, _ => 0f };
            tick[0] = sx + dx * (r + 2); tick[1] = sy + dy * (r + 2);
            tick[2] = sx + dx * (r + 10); tick[3] = sy + dy * (r + 10);
            _renderer.DrawPolyline(tick, color, 1.6f);
        }

        if (label is not null)
        {
            _renderer.DrawText(sx + r + 6, sy - r, label, color);
        }
    }

    // M25: where the plotted course comes nearest the destination — the pass point on the
    // ribbon, the body's position at that moment under its own lock, and the miss distance
    // drawn as a line between them.
    private void DrawDestinationPassMarker()
    {
        if (_destinationPass is not { } dp || dp.BodyId != _destinationBodyId || _ephemeris is null)
        {
            return;
        }

        (float px, float py) = _camera.WorldToScreen(PlotFrame(dp.ShipPosition, dp.SimTime));
        (float bx, float by) = _camera.WorldToScreen(PlotFrame(_ephemeris.Position(dp.BodyId, dp.SimTime), dp.SimTime));

        Span<float> line = stackalloc float[4];
        line[0] = px; line[1] = py; line[2] = bx; line[3] = by;
        _renderer!.DrawPolyline(line, DestinationColor with { A = 130 }, 1.2f);

        _renderer.DrawCircle(px, py, 5f, null, DestinationColor, 1.6f);
        _renderer.DrawCircle(px, py, 1.5f, DestinationColor, DestinationColor);
        _renderer.DrawText(px, py - 16, $"pass {FormatDistance(dp.Distance)}", DestinationColor, "10px monospace", TextAlign.Center);

        // The far end of the miss line is just a dot — only the scrub-time ghost wears a lock.
        _renderer.DrawCircle(bx, by, 3f, DestinationColor with { A = 170 }, DestinationColor with { A = 170 });
    }

    // The sling made this visible: the ribbon bends where the pass body WILL BE, which at twelve
    // plotted days is hundreds of pixels from where the body is drawn now — without scrubbing,
    // the kink hangs in empty sky (owner: "the curvature happens at a spot Jupiter is not at").
    // So the pass body's ghost at the pass epoch shows where that curve pins to.
    //
    // #124 (owner playtest): PR #117 left this ALWAYS-on, and on any close planetary pass the ghost
    // planet + tether read as "a slingshot the game plotted for me — that I never selected or set".
    // With no sling engaged there is no ribbon kink to anchor, so the ghost is pure noise wearing a
    // sling costume. Gate it on real sling INTENT — the ⤴ Sling panel is open, or a sling has been
    // SOLVEd — and key it to the body actually being slung (_slingablePass), labelled as a sling
    // pass so planned-vs-hypothetical is unambiguous. The plain closest-pass marker / scrub ghosts
    // (DrawClosestPassMarker, DrawGhostBodies — both PlotMode) are untouched and keep doing their job.
    private void DrawPassEpochGhost()
    {
        if (_openEditor != FlightEditorKind.Sling && _slingResult is not { Ok: true })
        {
            return; // no sling engaged → no sling ghost (the owner never asked for one)
        }
        if (_slingablePass is not { } cp || cp.Severity > 25 || _ephemeris is null)
        {
            return; // same embarrassment threshold as the pass marker
        }

        (float sx, float sy) = _camera.WorldToScreen(PlotFrame(_ephemeris.Position(cp.BodyId, cp.SimTime), cp.SimTime));
        (float nx, float ny) = _camera.WorldToScreen(PlotFrame(_ephemeris.Position(cp.BodyId, SimTime), SimTime));
        if (Math.Abs(sx - nx) < 6 && Math.Abs(sy - ny) < 6)
        {
            return; // the ghost would sit on the live disc anyway (imminent pass or far zoom-out)
        }

        Span<float> tether = stackalloc float[4];
        tether[0] = nx; tether[1] = ny; tether[2] = sx; tether[3] = sy;
        _renderer!.DrawPolyline(tether, new RgbaColor(180, 200, 220, 30), 1f);

        float radiusPx = (float)Math.Max(3.5, cp.BodyRadius / _camera.MetersPerPixel);
        RgbaColor ghost = BodyColor(cp.BodyId) with { A = 110 };
        _renderer!.DrawCircle(sx, sy, radiusPx, ghost, ghost);
        _renderer!.DrawCircle(sx, sy, radiusPx + 2.5f, null, new RgbaColor(220, 230, 245, 90), 1.2f);
        _renderer.DrawText(sx, sy + radiusPx + 12, $"{cp.BodyName} at sling pass",
            new RgbaColor(220, 230, 245, 140), "10px monospace", TextAlign.Center);
    }

    // Ghosts of every body at the scrub time. Deliberately loud: a filled dot with an outline
    // ring and a faint tether from the live body — 2 px at 35% alpha vanished against the
    // plasma stream ribbons (the owner read Venus and Mercury as "stuck").
    private void DrawGhostBodies()
    {
        ICelestialEphemeris ephemeris = _ephemeris!;
        double t = ScrubTime;
        Span<float> tether = stackalloc float[4];
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            Vector2d position = ephemeris.Position(body.Id, t);
            (float sx, float sy) = _camera.WorldToScreen(PlotFrame(position, t));
            (float nx, float ny) = _camera.WorldToScreen(PlotFrame(ephemeris.Position(body.Id, SimTime), SimTime));

            tether[0] = nx; tether[1] = ny; tether[2] = sx; tether[3] = sy;
            _renderer!.DrawPolyline(tether, new RgbaColor(180, 200, 220, 40), 1f);

            float radiusPx = (float)Math.Max(3.5, body.BodyRadius / _camera.MetersPerPixel);
            RgbaColor baseColor = BodyColor(body.Id);
            RgbaColor ghost = baseColor with { A = 150 };
            _renderer!.DrawCircle(sx, sy, radiusPx, ghost, ghost);
            _renderer!.DrawCircle(sx, sy, radiusPx + 2.5f, null, new RgbaColor(220, 230, 245, 120), 1.2f);
            if (body.Id == _destinationBodyId)
            {
                // The destination's projected position wears the full lock — the owner couldn't
                // find the planet at all among the ghosts.
                DrawTargetLock(sx, sy, radiusPx, DestinationColor, "DEST");
            }
        }
    }

    // Ghost ship marker at the projected path position for the scrub time.
    private void DrawGhostShip()
    {
        if (_samples.Count == 0) return;
        Vector2d position = SamplePositionAt(ScrubTime);
        (float sx, float sy) = _camera.WorldToScreen(PlotFrame(position, ScrubTime));
        _renderer!.DrawCircle(sx, sy, 4f, GhostShipColor, GhostShipColor);
    }

    // Filled dots on the ribbon where each maneuver node fires.
    private void DrawNodeMarkers()
    {
        if (_planNodes.Count == 0 || _samples.Count == 0) return;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Executed) continue;
            Vector2d position = SamplePositionAt(node.SimTime);
            (float sx, float sy) = _camera.WorldToScreen(PlotFrame(position, node.SimTime));
            RgbaColor color = node.Stale
                ? StaleNodeColor
                : node.Action == ManeuverAction.Accelerate ? AccelNodeColor : DecelNodeColor;
            bool selected = ReferenceEquals(node, _selectedPlanNode);

            // X-Pilot burn: draw the nose vector — the heading the burn thrusts along — anchored at
            // the node's position on the ribbon (the "scrub line position" the owner asked for).
            if (node.Mode == BurnMode.Vector && !node.Stale)
            {
                DrawNoseVector(sx, sy, node.HeadingDegrees, selected);
            }

            _renderer!.DrawCircle(sx, sy, selected ? 6.5f : 5f, color, color);
            if (selected)
            {
                _renderer!.DrawCircle(sx, sy, 10f, null, new RgbaColor(255, 255, 255, 160), 1.5f);
            }
            if (Math.Abs(node.Percent - 10) > 0.001)
            {
                _renderer!.DrawText(sx, sy - 12, $"{node.Percent:0.##}%", new RgbaColor(150, 220, 255, 200), "9px monospace", TextAlign.Center);
            }
        }
    }

    // The X-Pilot nose vector: a fixed-length arrow from the node marker along the burn heading —
    // "the direction of nose when the burn is planned, on the scrub-line position" (owner). The
    // camera is an axis-aligned scale with Y flipped (WorldToScreen maps +X→right, +Y→up), so a
    // world heading (cosθ, sinθ) becomes the screen direction (cosθ, −sinθ) directly — projecting a
    // unit world offset instead would round to the same pixel at solar-system zoom and vanish.
    private void DrawNoseVector(float sx, float sy, double headingDegrees, bool selected)
    {
        double rad = headingDegrees * Math.PI / 180.0;
        float dx = (float)Math.Cos(rad);
        float dy = -(float)Math.Sin(rad);

        const float shaft = 34f;   // fixed screen length so the heading reads at any zoom
        float tipX = sx + dx * shaft, tipY = sy + dy * shaft;
        RgbaColor c = selected ? new RgbaColor(190, 235, 255) : XPilotVectorColor;
        float width = selected ? 2.5f : 1.8f;

        Span<float> shaftPts = [sx, sy, tipX, tipY];
        _renderer!.DrawPolyline(shaftPts, c, width);

        // Arrowhead: two barbs swept back from the tip.
        const float barb = 8f;
        Span<float> headPts =
        [
            tipX + (-dx * barb - dy * barb * 0.6f), tipY + (-dy * barb + dx * barb * 0.6f),
            tipX, tipY,
            tipX + (-dx * barb + dy * barb * 0.6f), tipY + (-dy * barb - dx * barb * 0.6f),
        ];
        _renderer!.DrawPolyline(headPts, c, width);
    }

    // Position on the projected path at a given sim time, linearly interpolated between the two
    // bracketing samples. Clamps to the endpoints outside the projected horizon.
    private Vector2d SamplePositionAt(double simTime)
    {
        IReadOnlyList<TrajectorySample> s = _samples;
        if (s.Count == 0) return _ship.Position;
        if (simTime <= s[0].SimTime) return s[0].Position;
        if (simTime >= s[^1].SimTime) return s[^1].Position;

        int lo = 0, hi = s.Count - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) >> 1;
            if (s[mid].SimTime <= simTime) lo = mid; else hi = mid;
        }

        TrajectorySample a = s[lo];
        TrajectorySample b = s[hi];
        double span = b.SimTime - a.SimTime;
        double f = span > 0 ? (simTime - a.SimTime) / span : 0;
        return a.Position + (b.Position - a.Position) * f;
    }

    private void DrawCelestialBodies()
    {
        ICelestialEphemeris ephemeris = _ephemeris!;
        Span<float> ring = stackalloc float[(OrbitSegments + 1) * 2];

        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (IsBodyHidden(body.Id)) continue; // off the charts until an intel-fed scan finds it (PR-A)
            if (body.OrbitPeriod != 0 && body.OrbitRadius > 0)
            {
                Vector2d parentPosition = body.ParentId is null ? Vector2d.Zero : ephemeris.Position(body.ParentId, SimTime);
                // Kepler rails (PR-B): a circular body's ring is a circle of radius OrbitRadius; an
                // eccentric body's is its true ellipse, traced by sweeping the eccentric anomaly over a
                // full turn (even spacing in E, one perifocal point rotated by ω per vertex — no
                // per-vertex Kepler solve). e == 0 keeps the exact circular sweep.
                double e = body.Eccentricity;
                double semiMinor = e == 0.0 ? body.OrbitRadius : body.OrbitRadius * Math.Sqrt(1.0 - e * e);
                double cosW = Math.Cos(body.ArgPeriapsis);
                double sinW = Math.Sin(body.ArgPeriapsis);
                for (int i = 0; i <= OrbitSegments; i++)
                {
                    double t = Math.Tau * i / OrbitSegments;
                    Vector2d world;
                    if (e == 0.0)
                    {
                        world = parentPosition + new Vector2d(body.OrbitRadius * Math.Cos(t), body.OrbitRadius * Math.Sin(t));
                    }
                    else
                    {
                        double px = body.OrbitRadius * (Math.Cos(t) - e);
                        double py = semiMinor * Math.Sin(t);
                        world = parentPosition + new Vector2d(cosW * px - sinW * py, sinW * px + cosW * py);
                    }

                    (float x, float y) = _camera.WorldToScreen(world);
                    ring[i * 2] = x;
                    ring[i * 2 + 1] = y;
                }

                _renderer!.DrawPolyline(ring, OrbitColor);
            }

            Vector2d position = ephemeris.Position(body.Id, SimTime);
            (float sx, float sy) = _camera.WorldToScreen(position);
            bool isStation = body.Kind == BodyKind.Station;
            float radiusPx = (float)Math.Max(isStation ? 1.5 : 2.0, body.BodyRadius / _camera.MetersPerPixel);
            if (isStation)
            {
                radiusPx = Math.Min(radiusPx, 3.5f); // a built thing, not a world — stays a small blip
            }

            RgbaColor color = BodyColor(body);

            _renderer!.DrawCircle(sx, sy, radiusPx, color, color);
            bool isDestination = body.Id == _destinationBodyId;
            if (isDestination && !PlotMode)
            {
                // The chosen destination reads at any zoom — full gun-camera lock (M25).
                // In plot mode the GHOST carries the one and only lock (owner: three targets on
                // screen made the scrub read as frozen — he was watching the live body).
                DrawTargetLock(sx, sy, radiusPx, DestinationColor, "DEST");
            }
            if (isDestination || _camera.MetersPerPixel < body.BodyRadius * 500 || (isStation && _camera.MetersPerPixel < LabelZoomThresholdForStations))
            {
                RgbaColor labelColor = body.IsHaven ? HavenLabelColor : LabelColor;
                // A ⚓ flags the mass-less grey-market docks — the havens you can clamp onto to lie
                // low (moon havens you orbit instead, so they carry the pink wash but no anchor).
                string label = IsDockableHaven(body) ? $"⚓ {body.Name}" : body.Name;
                _renderer!.DrawText(sx + radiusPx + 4, sy - radiusPx, label, labelColor);

                // The ⚓'s sibling: a 🛬 under any shuttle-landable ground (a moon, by the same pure
                // ShuttleExcursion.IsLandableSurface the destination board uses — never a hardcoded body
                // list, so it lights up correctly whatever the moons' phases). Bright + a size up when
                // that ground is within the shuttle's reach of the ship right now (the _landableInRange
                // set, the board's own range truth); dim regolith tan when landable only in principle.
                if (ShuttleExcursion.IsLandableSurface(body.Kind))
                {
                    bool inReach = _landableInRangeIds.Contains(body.Id);
                    _renderer.DrawText(sx + radiusPx + 4, sy + radiusPx + 20, "🛬",
                        inReach ? LandableInRangeColor : LandableBaseColor,
                        inReach ? "13px sans-serif" : "11px sans-serif", TextAlign.Left);
                }
            }
        }
    }

    private static readonly RgbaColor CargoMarkerColor = new(235, 190, 120); // parcel amber — reads on the star field

    // #175: while a cargo run is in hand, its destination carries a 📦 so the delivery point isn't
    // invisible on the map. Modest — a small tag under the body's own label, drawn only for the
    // Active run's destination (it clears the instant the parcel is delivered).
    private void DrawCargoRunMarkers()
    {
        if (_ephemeris is null) return;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (IsBodyHidden(body.Id) || ActiveCargoRunTo(body.Id) is null) continue;
            Vector2d position = _ephemeris.Position(body.Id, SimTime);
            (float sx, float sy) = _camera.WorldToScreen(position);
            bool isStation = body.Kind == BodyKind.Station;
            float radiusPx = (float)Math.Max(isStation ? 1.5 : 2.0, body.BodyRadius / _camera.MetersPerPixel);
            if (isStation) radiusPx = Math.Min(radiusPx, 3.5f);
            _renderer!.DrawText(sx + radiusPx + 4, sy + radiusPx + 8, $"📦 deliver to {body.Name}", CargoMarkerColor,
                "11px sans-serif", TextAlign.Left);
        }
    }
    private static readonly RgbaColor PassFlashColor = new(150, 255, 210);

    private double _frameNowMs;

    private void DrawPassFlash()
    {
        if (_trackingPost?.LastPassFlash is not { } flash)
        {
            return;
        }

        double ageMs = (DateTime.UtcNow - flash.WallTime).TotalMilliseconds;
        if (ageMs is < 0 or > 1200 || ContactPosition(flash.ShipId) is not { } position)
        {
            return;
        }

        (float sx, float sy) = _camera.WorldToScreen(position);
        byte alpha = (byte)(220 * (1 - ageMs / 1200));
        DrawCornerBrackets(sx, sy, 13f, PassFlashColor with { A = alpha });
        _renderer!.DrawText(sx + 16, sy - 10, "updating fix", PassFlashColor with { A = alpha },
            "10px monospace", TextAlign.Left);
    }

    private void DrawCornerBrackets(float sx, float sy, float r, RgbaColor color)
    {
        Span<float> corner = stackalloc float[6];
        for (int xSign = -1; xSign <= 1; xSign += 2)
        {
            for (int ySign = -1; ySign <= 1; ySign += 2)
            {
                corner[0] = sx + xSign * r;
                corner[1] = sy + ySign * (r - 5);
                corner[2] = sx + xSign * r;
                corner[3] = sy + ySign * r;
                corner[4] = sx + xSign * (r - 5);
                corner[5] = sy + ySign * r;
                _renderer!.DrawPolyline(corner, color, 1.5f);
            }
        }
    }

    private static string FormatFlightTime(double seconds) =>
        seconds < 3600 ? $"{seconds / 60:F0} min"
        : seconds < 86400 ? $"{seconds / 3600:F1} h"
        : $"{seconds / 86400:F1} d";

    private void DrawWorldPolyline(IReadOnlyList<TrajectorySample> samples, RgbaColor color, float widthPx)
    {
        if (samples.Count < 2)
        {
            return;
        }

        int stride = Math.Max(1, samples.Count / 160);
        int points = (samples.Count + stride - 1) / stride + 1;
        float[] xy = new float[points * 2];
        int w = 0;
        for (int i = 0; i < samples.Count; i += stride)
        {
            (xy[w], xy[w + 1]) = _camera.WorldToScreen(samples[i].Position);
            w += 2;
        }

        (xy[w], xy[w + 1]) = _camera.WorldToScreen(samples[^1].Position);
        w += 2;
        _renderer!.DrawPolyline(xy.AsSpan(0, w), color, widthPx);
    }

    private void DrawShip(Vector2d shipPosition)
    {
        (float sx, float sy) = _camera.WorldToScreen(shipPosition);
        // Arc halo: a hollow bright ring around a hull hot enough to arc (EU scenarios only).
        if (_plasma is not null && _ship.Charge >= ArcChargeThreshold)
        {
            _renderer!.DrawCircle(sx, sy, 9f, null, ArcHaloColor);
        }

        // M28: the hull has a facing now — cosmetic on the map, but it SLEWS: toward the
        // firing bearing through a lock countdown, back to prograde after the round leaves.
        double heading = ShipHeadingRad();
        Vector2d barrelTip = shipPosition
            + new Vector2d(Math.Cos(heading), Math.Sin(heading)) * (12 * _camera.MetersPerPixel);
        (float bx, float by) = _camera.WorldToScreen(barrelTip);
        Span<float> barrel = stackalloc float[4];
        barrel[0] = sx; barrel[1] = sy; barrel[2] = bx; barrel[3] = by;
        _renderer!.DrawPolyline(barrel, ShipColor with { A = 200 }, 2f);

        _renderer!.DrawCircle(sx, sy, 4f, ShipColor, ShipColor);
        _renderer!.DrawText(sx + 8, sy - 6, "Ship", ShipColor);
    }

    private void ReprojectTrajectory()
    {
        _samples = _simulator!.ProjectAdaptive(_ship, _plan, CurrentPlotHorizonSeconds, maxTimeStep: 3 * 3600, maxSamples: 8000);
        _nextProjectionSimTime = _ship.SimTime + ProjectionRefreshSimSeconds;
        _passDirty = true;
        _lastReprojectMs = _lastTimestampMs ?? 0;
    }

    // ---- Plotting mode ----

    private void TogglePlotMode()
    {
        if (PlotMode)
        {
            ExitPlotMode();
        }
        else
        {
            EnterPlotMode();
        }
    }

    private void EnterPlotMode()
    {
        StopSkip(); // #172: plotting is the captain taking the helm — stop skipping (and don't save a
                    // cranked warp as _warpBeforePlot). StopSkip drops Warp to 1× before we snapshot it.
        _warpBeforePlot = Warp;
        PlotMode = true;
        Paused = true;
        _scrubOffsetSeconds = 0;
        ReprojectTrajectory();
    }

    private void ExitPlotMode()
    {
        PlotMode = false;
        Paused = false;
        Warp = _warpBeforePlot <= 0 ? 1 : _warpBeforePlot;
        ReprojectTrajectory();
    }

    // Rebuild the immutable plan the sim executes from the non-stale nodes. Past/executed nodes are
    // harmless to include (their firing window has passed), so the same plan serves projection too.
    private void RebuildPlan()
    {
        _plan = new ManeuverPlan(
            _planNodes.Where(n => !n.Stale)
                      .Select(n => new ManeuverNode(n.SimTime, n.Action, n.Pulses, Fine: false, Percent: n.Percent,
                                                    Mode: n.Mode, HeadingDegrees: n.HeadingDegrees)));
    }

    // Reaction-mass claimed by still-pending (non-stale, future) nodes.
    private int PlannedPulseTotal()
    {
        int total = 0;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && node.SimTime > _ship.SimTime)
            {
                total += node.Pulses;
            }
        }

        return total;
    }

    private void AddBurnAtScrub()
    {
        // Never refuse over the scrub sitting in the past (owner: the control must never be
        // in a position that blocks the action) — clamp to one minute out and proceed.
        double t = Math.Max(Math.Floor(ScrubTime), Math.Floor(_ship.SimTime) + 60);
        if (PlannedPulseTotal() + 1 > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass");
            return;
        }

        var newNode = new PlanNode { SimTime = t, Action = ManeuverAction.Accelerate, Pulses = 1 };
        _planNodes.Add(newNode);
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();

        // PR-D2: a freshly added burn opens its own editor (accordion) so its controls are right there.
        _openEditor = FlightEditorKind.Burn;
        _selectedPlanNode = newNode;

        // Tutorial step 2: first plan node added while a pod is selected.
        if (_selectedTargetId is not null && FindNpc(_selectedTargetId) is { Ship.IsPod: true })
        {
            AdvanceTutorial(1);
        }
    }

    private void SetAction(PlanNode node, ManeuverAction action)
    {
        if (node.Action == action)
        {
            return;
        }

        node.Action = action;
        RebuildPlan();
        ReprojectTrajectory();
    }

    // Flip a node between the classic ± factor burn and the X-Pilot heading burn. Switching *into*
    // Vector mode seeds the heading with the ship's velocity direction at that point, so the burn
    // starts neutral (straight ahead — identical to a Factor Accelerate) and the pilot then rotates
    // it toward the pod they mean to chase.
    private void ToggleBurnMode(PlanNode node)
    {
        if (node.Mode == BurnMode.Factor)
        {
            node.Mode = BurnMode.Vector;
            node.HeadingDegrees = HeadingAlongCourseAt(node.SimTime);
        }
        else
        {
            node.Mode = BurnMode.Factor;
        }

        RebuildPlan();
        ReprojectTrajectory();
    }

    // World-space heading (degrees, 0° = +X, CCW) of the projected velocity at a plotted time.
    private double HeadingAlongCourseAt(double simTime)
    {
        Vector2d v = SampledVelocityAt(simTime);
        if (v.LengthSquared == 0)
        {
            return 0;
        }

        double deg = Math.Atan2(v.Y, v.X) * 180.0 / Math.PI;
        return deg < 0 ? deg + 360 : deg;
    }

    private void SetHeading(PlanNode node, ChangeEventArgs e)
    {
        string raw = (e.Value?.ToString() ?? string.Empty).Replace(',', '.');
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double deg))
        {
            // #201: the field is ship-relative by default (0 ahead, +90 starboard, −90 port). Map it
            // back to the world heading the physics burns along; absolute mode types the world angle direct.
            node.HeadingDegrees = _burnAngleAbsolute
                ? WrapDegrees(deg)
                : BurnHeadingConvention.RelativeToWorld(HeadingAlongCourseAt(node.SimTime), deg);
            RebuildPlan();
            ReprojectTrajectory();
        }
    }

    private void NudgeHeading(PlanNode node, double delta)
    {
        node.HeadingDegrees = WrapDegrees(node.HeadingDegrees + delta);
        RebuildPlan();
        ReprojectTrajectory();
    }

    private static double WrapDegrees(double deg)
    {
        deg %= 360;
        return deg < 0 ? deg + 360 : deg;
    }

    private void SetPulses(PlanNode node, ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            return;
        }

        value = Math.Clamp(value, MinNodePulses, MaxNodePulses);
        if (value == node.Pulses)
        {
            return;
        }

        // Budget check counts this node's new pulses in place of its old ones.
        int othersTotal = PlannedPulseTotal();
        if (!node.Stale && node.SimTime > _ship.SimTime)
        {
            othersTotal -= node.Pulses;
        }
        if (othersTotal + value > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass");
            return;
        }

        node.Pulses = value;
        RebuildPlan();
        ReprojectTrajectory();
    }

    // Re-time to the scrub time. Un-stales the node (plan §4: re-timing repairs it).
    private void RetimeToScrub(PlanNode node)
    {
        // Same clamp as AddBurnAtScrub: a past scrub re-times to one minute out, never errors.
        double t = Math.Max(Math.Floor(ScrubTime), Math.Floor(_ship.SimTime) + 60);

        // If it was stale/executed it re-enters the budget; check it fits.
        int othersTotal = PlannedPulseTotal();
        bool wasPending = !node.Stale && node.SimTime > _ship.SimTime;
        if (wasPending)
        {
            othersTotal -= node.Pulses;
        }
        if (othersTotal + node.Pulses > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass");
            return;
        }

        node.SimTime = t;
        node.Stale = false;
        node.Executed = false;
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
    }

    private void SetPercent(PlanNode node, ChangeEventArgs e)
    {
        // Accept either decimal separator: the field renders with an invariant '.', but a user on a
        // comma-locale keyboard will type ',' — normalize before the invariant parse.
        string raw = (e.Value?.ToString() ?? string.Empty).Replace(',', '.');
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double p))
        {
            node.Percent = Math.Clamp(p, 0.01, 50);
            RebuildPlan();
            ReprojectTrajectory();
        }
    }

    private PlanNode? _selectedPlanNode;

    // M24: planets are clickable too — a small menu offers "set destination", so the orbit
    // assist coaches the approach to the body the captain MEANS, not whichever is nearest.
    // Click a thrust node on the ribbon to select it: highlights its row and jumps the scrub
    // to its time (owner request, M16). Returns true when a node was hit.
    private bool TrySelectNodeAt(double clientX, double clientY)
    {
        if (!PlotMode || _planNodes.Count == 0 || _samples.Count == 0)
        {
            return false;
        }

        const double hitRadiusPx = 14;
        PlanNode? best = null;
        double bestSq = hitRadiusPx * hitRadiusPx;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Executed)
            {
                continue;
            }

            // #143 — hit-test against where the marker is actually DRAWN (frame-transformed), else a
            // non-Sun frame makes every ribbon-node click miss. DrawNodeMarkers uses the same PlotFrame.
            (float nx, float ny) = _camera.WorldToScreen(PlotFrame(SamplePositionAt(node.SimTime), node.SimTime));
            double dx = clientX - nx, dy = clientY - ny;
            double d = dx * dx + dy * dy;
            if (d < bestSq)
            {
                bestSq = d;
                best = node;
            }
        }

        if (best is null)
        {
            return false;
        }

        // PR-D2: a ribbon-node click selects AND opens that step's editor — the map and the list are two
        // views of one plan, resolving to the same _selectedPlanNode + accordion state.
        _selectedPlanNode = best;
        _openEditor = FlightEditorKind.Burn;
        _scrubOffsetSeconds = Math.Max(0, best.SimTime - _ship.SimTime);
        return true;
    }

    private void DeleteNode(PlanNode node)
    {
        _planNodes.Remove(node);
        // PR-D2: if the deleted step was the open one, collapse the accordion so nothing dangles.
        if (ReferenceEquals(node, _selectedPlanNode))
        {
            _selectedPlanNode = null;
            if (_openEditor == FlightEditorKind.Burn)
            {
                _openEditor = FlightEditorKind.None;
            }
        }
        RebuildPlan();
        ReprojectTrajectory();
    }

    private void SortNodes() => _planNodes.Sort((a, b) => a.SimTime.CompareTo(b.SimTime));

    // After live stepping, settle mass for any node whose firing window has passed. The window rule
    // in Simulator.Step fires each node once; this mirrors that once for the mass budget/HUD.
    private void AccountForFiredNodes()
    {
        int firedPulses = 0;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Executed && !node.Stale && node.SimTime < _ship.SimTime)
            {
                node.Executed = true;
                firedPulses += node.Pulses;
            }
        }

        if (firedPulses > 0)
        {
            _reactionMassPulses = Math.Max(0, _reactionMassPulses - firedPulses);
            ShowPulseMessage($"Plan: {firedPulses} pulse{(firedPulses == 1 ? "" : "s")} fired");
        }

        // Spent burns clean themselves off the plot card (owner request): once a node's time
        // is past it either fired (Executed) or never will (Stale) — either way it's history.
        int removed = _planNodes.RemoveAll(n => n.SimTime < _ship.SimTime && (n.Executed || n.Stale));
        if (removed > 0 && _selectedPlanNode is { } sel && !_planNodes.Contains(sel))
        {
            _selectedPlanNode = null;
            if (_openEditor == FlightEditorKind.Burn)
            {
                _openEditor = FlightEditorKind.None; // PR-D2: the open step fired/expired — collapse it
            }
        }
    }

    // Departures board: mid-flight ships carry a virtual past departure — show it as history ("-42d").
    private static RgbaColor BodyColor(string id) => id switch
    {
        "sun" => new RgbaColor(255, 214, 10),
        "mercury" => new RgbaColor(160, 160, 160),
        "venus" => new RgbaColor(230, 200, 140),
        "earth" => new RgbaColor(70, 130, 230),
        "mars" => new RgbaColor(210, 100, 60),
        "jupiter" => new RgbaColor(210, 170, 120),
        "saturn" => new RgbaColor(220, 200, 150),
        "uranus" => new RgbaColor(150, 220, 230),
        "neptune" => new RgbaColor(90, 110, 230),
        _ => new RgbaColor(200, 200, 200),
    };

    // Kind/haven-aware map marker color (PR-3): a station reads as built (synthetic teal)
    // regardless of its id; a haven gets a subtle crimson wash on top of whatever it is.
    private static RgbaColor BodyColor(CelestialBody body)
    {
        RgbaColor color = body.Kind == BodyKind.Station ? StationColor : BodyColor(body.Id);
        return body.IsHaven ? Tinted(color, HavenAccent, 0.35) : color;
    }
}
