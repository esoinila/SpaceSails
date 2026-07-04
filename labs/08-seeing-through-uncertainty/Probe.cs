// Lab 08 — Seeing through uncertainty
//
// Teaching voice: a sensor gives you one instant of ground truth — a position and a velocity at
// a timestamp — and nothing else. Everything the game says about where a target is a minute, a
// day, or a week later is a PREDICTION, not another observation, and the honest way to present a
// prediction is as a cone that grows with time since the last look (`PathPredictor`,
// `w0 + sigma_v * dt + 1/2 * budget * dt^2` — the last term is the target's plausible *unseen*
// maneuvering, not sensor noise). This is not textbook orbital mechanics (Curtis doesn't cover
// sensor fusion); it is the game's own estimation layer, and this lab holds it to its own
// promise: does the cone actually contain the truth, and by how much does it over-cover?
//
// This probe takes ONE real NPC ship out of the actual traffic generator
// (`TrafficSchedule.Generate`, seeded, reading `scenarios/sol.json`'s traffic section — the same
// code path the live game uses to populate the board), observes it once at its departure, and
// compares the ballistic dead-reckoned cone against the ship's TRUE flight (its real, hidden
// `ManeuverPlan`, run through the same `Simulator` the game flies NPCs with). Then it layers on
// `TrackedTarget`/`TrackingStation` (M-series telescope tracking): the same comparison at three
// track-quality levels, judged against the real boarding envelope pirates actually use
// (`CaptureRule`: 5e8 m at under 5000 m/s). Last, a genuinely stale observation is fed to
// `TrackedTargetLedger.TryConfirm` and the short look fails for real, on the real code path.
//
// IRONCLAD RULE: every number below came from running this probe. Change the code, rerun,
// re-paste — never hand-edit labs/08-seeing-through-uncertainty/README.md's tables.

using SpaceSails.Core;

string scenarioPath = Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json");
var ephemeris = CircularOrbitEphemeris.FromScenario(ScenarioLoader.LoadFile(scenarioPath));

// A deterministic slice of the real traffic board: same seed, same code path
// (TrafficSchedule.Generate) the live game uses. count=10 -> midFlight = 10*6/10 = 6, so index 6
// is the FIRST scheduled (not-yet-departed) short-haul ship: its Observation starts right at
// departure, before its own departure burst has fired — the cleanest case to watch a hidden burn
// happen inside the prediction window.
IReadOnlyList<NpcShip> traffic = TrafficSchedule.Generate(ephemeris, seed: 20260704, count: 10);
NpcShip ship = traffic[6];
Console.WriteLine($"Ship under observation: {ship.Callsign} ({ship.Id}), {ship.CargoClass}," +
    $" {ship.OriginId} -> {ship.DestinationId}, personality {ship.Personality}.");
Console.WriteLine($"Departs t={ship.DepartureTime:F0} s, plan has {ship.Plan.Nodes.Count} burn node(s)," +
    $" estimated arrival t={ship.EstimatedArrivalTime:F0} s ({(ship.EstimatedArrivalTime - ship.DepartureTime) / 86400:F1} days transit).");
foreach (ManeuverNode node in ship.Plan.Nodes)
{
    Console.WriteLine($"  - node: t={node.SimTime:F0} s (+{(node.SimTime - ship.DepartureTime) / 3600:F2} h from departure)," +
        $" {node.Action}, {node.Pulses} pulse(s)");
}

var observation = new Observation(ship.Id, ship.InitialState.SimTime, ship.InitialState.Position, ship.InitialState.Velocity);
var simulator60 = new Simulator(ephemeris, timeStepSeconds: TrafficSchedule.NpcTimeStep);

Vector2d TruePositionAt(double horizonSeconds) =>
    simulator60.Run(ship.InitialState, horizonSeconds, ship.Plan).Position;

Console.WriteLine();
Console.WriteLine("=== Section 1: is the ballistic cone honest? (5 horizons) ===");
Console.WriteLine();
Console.WriteLine("Dead-reckon forward with NO knowledge of the ship's real plan (hypothesis = null,");
Console.WriteLine("ballistic only) and compare the cone's half-width against the ACTUAL deviation of");
Console.WriteLine("the ship's true (plan-following) position from that same ballistic prediction.");
Console.WriteLine();

double[] horizonsSeconds = [1800, 2 * 3600, 6 * 3600, 86400, 3 * 86400];
Console.WriteLine($"{"horizon",-14}{"cone half-width (m)",-22}{"actual deviation (m)",-22}{"cone contains truth?",-22}{"conservatism (x)",-16}");
foreach (double horizon in horizonsSeconds)
{
    PredictedPath predicted = PathPredictor.Predict(ephemeris, observation, hypothesis: null, horizon, ship.ManeuverBudget);
    Vector2d predictedCenter = predicted.Samples[^1].Position;
    Vector2d truePos = TruePositionAt(horizon);
    double deviation = (truePos - predictedCenter).Length;
    double halfWidth = predicted.HalfWidthAt(observation.SimTime + horizon);
    bool contains = deviation <= halfWidth;
    double conservatism = deviation > 0 ? halfWidth / deviation : double.PositiveInfinity;

    string label = horizon < 3600 ? $"{horizon / 60:F0} min" : $"{horizon / 3600:F1} h";
    Console.WriteLine($"{label,-14}{halfWidth,-22:E3}{deviation,-22:E3}{(contains ? "yes" : "NO"),-22}{conservatism,-16:F1}");
}

Console.WriteLine();
Console.WriteLine("Surprise: the cone is NOT honest at every horizon. The departure burst fires 1 h");
Console.WriteLine("after departure (BurnLeadSeconds) — 2 real, discrete +10% pulses, a step change in");
Console.WriteLine("velocity — and PathPredictor's cone only budgets for a slow CONTINUOUS acceleration");
Console.WriteLine("(ManeuverBudget, 0.3 m/s^2). Right after the real burn (2 h, 6 h) the actual");
Console.WriteLine("deviation OUTRUNS the still-small budgeted cone (conservatism 0.5x — the truth sits");
Console.WriteLine("outside it). The cone only catches back up once its own dt^2 growth swamps the");
Console.WriteLine("one-time impulsive jump (24 h on). So: honest on average over a long horizon,");
Console.WriteLine("genuinely wrong in the hour or two right after a real burn — because the model is");
Console.WriteLine("built for a continuously-thrusting evader, not the game's actual discrete-pulse");
Console.WriteLine("propulsion. That gap is exactly what the break-it below exploits.");

Console.WriteLine();
Console.WriteLine("=== Section 2: telescope tracking — quality shrinks the cone, and that's the whole");
Console.WriteLine("game for intercepts ===");
Console.WriteLine();
Console.WriteLine($"UncertaintyScale(quality) = 1 - 0.7*quality. Boarding envelope from CaptureRule:");
Console.WriteLine($"CaptureRadiusMeters = {CaptureRule.CaptureRadiusMeters:E3} m at under {CaptureRule.MaxRelativeSpeed:F0} m/s" +
    " relative speed.");
Console.WriteLine("'Hit' below means the scaled cone half-width alone already fits inside the capture");
Console.WriteLine("radius — a boarding shuttle aimed at the cone's center is GUARANTEED to have the real");
Console.WriteLine("target inside its envelope, no luck required. 'Miss' means the cone is still bigger");
Console.WriteLine("than the envelope: the target could be anywhere in it, including outside boarding range.");
Console.WriteLine();

(double Quality, string Label)[] qualityLevels =
[
    (0.0, "no track (quality 0.0)"),
    (TrackedTargetLedger.InitialQuality, $"fresh sweep detect (quality {TrackedTargetLedger.InitialQuality:F1})"),
    (1.0, "perfect reconfirm (quality 1.0)"),
];

foreach ((double quality, string label) in qualityLevels)
{
    double scale = 1 - 0.7 * quality;
    Console.WriteLine($"-- {label}: UncertaintyScale = {scale:F2} --");
    Console.WriteLine($"{"horizon",-14}{"scaled half-width (m)",-24}{"boarding envelope",-20}");
    foreach (double horizon in horizonsSeconds)
    {
        PredictedPath predicted = PathPredictor.Predict(ephemeris, observation, hypothesis: null, horizon, ship.ManeuverBudget);
        double halfWidth = predicted.HalfWidthAt(observation.SimTime + horizon) * scale;
        bool hit = halfWidth <= CaptureRule.CaptureRadiusMeters;
        string label2 = horizon < 3600 ? $"{horizon / 60:F0} min" : $"{horizon / 3600:F1} h";
        Console.WriteLine($"{label2,-14}{halfWidth,-24:E3}{(hit ? "HIT (guaranteed)" : "miss"),-20}");
    }

    Console.WriteLine();
}

Console.WriteLine("Punchline: at quality 0 every horizon past the pre-burn instant misses the envelope");
Console.WriteLine("outright. A fresh sweep detect (quality 0.4, the value every new telescope contact");
Console.WriteLine("starts at) buys back only some of that. Only a well-tracked, recently reconfirmed");
Console.WriteLine("target (quality near 1.0) keeps the guaranteed-hit window open out to the horizons");
Console.WriteLine("above — track quality is not flavor text, it is the difference between an intercept");
Console.WriteLine("a pirate can plan and one they can only hope for.");
Console.WriteLine();
Console.WriteLine("Caveat that matters: 'HIT (guaranteed)' is only as good as Section 1's promise that");
Console.WriteLine("the cone contains the truth — and Section 1 found that promise genuinely broken at");
Console.WriteLine("the 2 h/6 h rows (right after the real burn). Every 'HIT' printed above at those two");
Console.WriteLine("horizons is a false guarantee for THIS ship: the target is actually outside its own");
Console.WriteLine("cone, let alone inside the boarding envelope. A small cone is only trustworthy where");
Console.WriteLine("the underlying cone was honest to begin with.");

Console.WriteLine();
Console.WriteLine("=== Break it: feed a stale observation, watch the short look fail for real ===");
Console.WriteLine();
Console.WriteLine("Register the SAME single departure observation in a TrackedTargetLedger and never");
Console.WriteLine("reconfirm it. Section 1 already found the failure window: 6 h out, right after the");
Console.WriteLine("evasive first burn, the true deviation already outruns the cone. Ask the ledger for");
Console.WriteLine("a short confirming look at the ship's true position at that same 6 h mark — the same");
Console.WriteLine("TryConfirm() the live game calls for a cheap re-acquire — and watch it fail for real.");
Console.WriteLine();

var ledger = new TrackedTargetLedger(maxTracks: 4);
ledger.Add(observation);

const double staleHorizon = 6 * 3600;
double confirmTime = observation.SimTime + staleHorizon;
ShipState trueAtConfirm = simulator60.Run(ship.InitialState, staleHorizon, ship.Plan);

var telescope = new TelescopeModel();
Vector2d observerPosition = ephemeris.Position(ship.OriginId, confirmTime) * 0.5; // a fixed lookout, not chasing the target

PredictedPath ballisticAtConfirm = PathPredictor.Predict(ephemeris, observation, hypothesis: null, staleHorizon, ship.ManeuverBudget);
Vector2d predictedPositionAtConfirm = ballisticAtConfirm.Samples[^1].Position;
double halfWidthAtConfirm = ballisticAtConfirm.HalfWidthAt(confirmTime);
double actualDeviationAtConfirm = (trueAtConfirm.Position - predictedPositionAtConfirm).Length;

bool confirmed = ledger.TryConfirm(ship.Id, ephemeris, telescope, observerPosition, trueAtConfirm, confirmTime);

Console.WriteLine($"Staleness: {staleHorizon / 3600:F0} h since the one and only observation" +
    $" (staleness horizon for QUALITY decay is {TrackedTargetLedger.StalenessHorizonSeconds / 86400:F0} days" +
    " — this is not even stale enough to cost quality, and it STILL breaks the re-acquire).");
Console.WriteLine($"Ballistic (no-burn) predicted position vs the ship's TRUE (post-burn) position:" +
    $" deviation = {actualDeviationAtConfirm:E3} m; cone half-width at that time = {halfWidthAtConfirm:E3} m.");
Console.WriteLine($"TrackedTargetLedger.TryConfirm(...) at the true position: {(confirmed ? "SUCCEEDED" : "FAILED")}.");
Console.WriteLine();
if (!confirmed)
{
    Console.WriteLine("It fails because TryConfirm dead-reckons ballistically (hypothesis: null) from the");
    Console.WriteLine("last observation, exactly like Section 1 — and Section 1 already showed that a");
    Console.WriteLine("real discrete burn outruns the small-continuous-acceleration cone within hours.");
    Console.WriteLine("A short, cheap re-acquire look genuinely cannot find a target that maneuvered while");
    Console.WriteLine("nobody was watching; only a fresh full sweep (paying the real cost) re-establishes");
    Console.WriteLine("the lock. Track quality decay is not a courtesy discount for laziness — an unwatched");
    Console.WriteLine("target that burns can be flatly, verifiably gone from where the ledger thinks it is,");
    Console.WriteLine("well before the staleness-horizon clock would have warned anyone.");
}
else
{
    Console.WriteLine("(On this run the burn wasn't large enough to escape the cone by this horizon — see");
    Console.WriteLine("the deviation-vs-half-width numbers above; Section 1's 2 h/6 h rows are the reliable");
    Console.WriteLine("failure window for this ship.)");
}
