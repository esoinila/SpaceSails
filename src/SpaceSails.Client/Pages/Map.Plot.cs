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
    private enum FlightEditorKind { None, Burn, Sling, Skim, Insertion, Arrive }
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

                // #952 — REACH FOR THE WHOLE CAP WHILE THE PLAN'S ENDING IS OFF THE END OF THE LINE. The
                // arrival's epoch is not known independently of the ribbon: it is READ OFF the ribbon, so a
                // course too short to reach the body cannot say how much longer it needs. It can only say
                // "longer", and the honest answer to that is the projection cap the panel already budgets
                // for (PlotHorizonSeconds — the Saturn metric: one sit-down covers a whole sail). One
                // projection later the encounter is on the line, PlanFurthestEpochSeconds above counts it,
                // and RefreshArriveValidity's transition asks for the reprojection that settles this back
                // to encounter + margin. Auto only: a captain who put his own finger on Path length is
                // iterating, and that is the loop #952 is about — never overrule it.
                if (ArriveRibbonIsTooShort())
                {
                    horizon = PlotHorizonSeconds;
                }
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

        // #952 — THE PLAN'S LAST STEP IS, BY DEFINITION, ITS FURTHEST ENCOUNTER. The arrival is a step in the
        // list (PR-D1) but it was never in this reckoning, so "the plan's furthest encounter" meant the last
        // BURN: plot two burns off Earth and end the plan at Mars, and the auto ribbon stopped at burn + 90 d
        // — two hundred days short of the plan's own ending, with the arrival's ✗ computed off a pass that
        // was really just the end of the line. The arrival belongs here beside the destination pass; it is
        // the same kind of fact, read off the same sweep.
        if (_arrive is { } arrival && ArrivePassFor(arrival.BodyId) is { } arrivalPass)
        {
            furthest = Math.Max(furthest, arrivalPass.SimTime - now);
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
        // #955 NAV-1 — WHICH KIND OF STEP THIS ROW IS. Every plotted row used to be a burn, and the type
        // said so by saying nothing; the owner's dock-to-dock story needs the plan to be able to START AT
        // THE BERTH ("an undock step recorded topmost in the nav-burn list, then safe-harbour out-thrust
        // to clear the vicinity of the station, then the actual burns"). The kind rides on the node rather
        // than in a parallel list because the list IS the plan — one ordered thing, readable top to bottom,
        // is the whole point of UnifiedNavListNotes.md.
        public PlanStepKind Kind = PlanStepKind.Burn;

        // The haven a departure step belongs to: the berth whose clamp lets go, and the harbour the
        // clearance is measured from and thrusts away from. Null for an ordinary burn — a burn belongs to
        // no place.
        public string? HavenId;

        public double SimTime;
        public ManeuverAction Action;
        public int Pulses = 1;
        public double Percent = 10;      // per-pulse strength, any positive double
        public bool Stale;
        public bool Executed;
        public BurnMode Mode = BurnMode.Factor;  // Factor = ± prograde; Vector = X-Pilot point-and-burn
        public double HeadingDegrees;            // world-space heading for a Vector burn (0° = +X, CCW)
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
