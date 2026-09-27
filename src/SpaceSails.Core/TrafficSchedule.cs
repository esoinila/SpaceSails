using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// One NPC cargo ship: its public departures-board entry plus its (hidden) flight plan.
/// <see cref="ActivationTime"/> is when the ship enters the live sim with
/// <see cref="InitialState"/> as truth — for mid-flight ships that is ~t=0 with the ship
/// already deep in its transfer, not its historical departure.
/// </summary>
public sealed record NpcShip(
    string Id,
    string Callsign,
    string CargoClass,
    string OriginId,
    string DestinationId,
    RoutePersonality Personality,
    double DepartureTime,
    double ActivationTime,
    ShipState InitialState,
    ManeuverPlan Plan,
    double EstimatedArrivalTime,
    int CargoUnits,
    double ManeuverBudget,
    bool IsPod,
    string? DepotBodyId = null,
    double DepotOrbitRadius = 0,
    double DepotPhase = 0,
    // False = a secretive hauler (He3 out of pirate country — worldbuilding notes §4). The ship
    // still flies and is still visible to sensors that get it in range; it just never appears on
    // the public departures board. The hook F6/F7 (tracking + intel economy) need.
    bool PublishesTimetable = true)
{
    /// <summary>Equivalent acceleration a pilot could plausibly hide between observations.
    /// Feeds the prediction cone; a mass-driver pod has no engine at all.</summary>
    public const double DefaultManeuverBudget = 0.3;
}

/// <summary>
/// Deterministic traffic generator: the same seed yields bit-identical schedules on client and
/// server. Physics note: prograde-only pulses mean outer-system transfers take sim-years, so
/// playable traffic is dominated by ships spawned mid-flight — already falling through the
/// inner system at t=0, 20–70 days from arrival — plus a few short inner-system runs departing
/// during the first weeks. The Saturn departure times on the board are honest history.
///
/// De-Earth-centering (vision ¶8): a scenario can supply a <see cref="TrafficDefinition"/>
/// (routes + pod launchers) instead of relying on the hardcoded Sol tables below. When present,
/// <see cref="Generate"/>/<see cref="GeneratePods"/> read it (via the optional parameter, or via
/// <see cref="ICelestialEphemeris.Traffic"/> when the ephemeris carries one from its scenario);
/// when absent, both fall back to the original fixed tables — byte-identical to pre-PR-3
/// behavior, so scenarios without a traffic section (e.g. the Wheel of the World) are unaffected.
/// </summary>
public static partial class TrafficSchedule
{
    /// <summary>
    /// Fixed timestep for live NPC integration. Coarser than the player's dt=1 s because a
    /// frame at high warp must step every NPC (8 × 10000 dt=1 steps/frame froze interpreted
    /// WASM at ~1 fps), and NPC accuracy needs are meters-scale at dt=60. One shared constant
    /// so client and server (M9) integrate NPCs identically — determinism is law.
    /// </summary>
    public const double NpcTimeStep = 60;

    /// <summary>#255 — the freeze class: the widest NPC catch-up gap that is still HONEST WARP. Past 30
    /// sim-days the gap is not warp, it is an epoch discontinuity — a long-haul jump, or a vault resumed at
    /// a far sim-epoch (the owner's 8.3-year "the-tilt" save) landing the world clock years ahead of a mover
    /// still seeded near epoch 0. The live loop can NEVER legitimately open a gap this wide in one frame: the
    /// player's clock advances at most MaxStepsPerFrame × 1 s ≈ 5.6 h per frame (accumulator-clamped, even on
    /// a tab resumed from suspension), and a scheduled mover activates the same frame its ActivationTime is
    /// crossed — so 30 days sits ~128× above the widest honest per-frame catch-up yet far below any real void.
    /// A gap past it means integrating the void at <see cref="NpcTimeStep"/> would grind millions of Steps and
    /// hard-freeze the tab; the mover belongs to the world we left, so it is retired and RefillTraffic
    /// repopulates fresh at the epoch (the same fate <c>ReseedWorldForJump</c> assigns for free).</summary>
    public const double NpcMaxCatchUpSeconds = 30.0 * Day;

    /// <summary>#255 — true when a live NPC catch-up gap is an epoch leap, not warp: integrate it and the tab
    /// freezes, so the caller must retire the mover instead. See <see cref="NpcMaxCatchUpSeconds"/>.</summary>
    public static bool IsCatchUpStale(double gapSeconds) => gapSeconds > NpcMaxCatchUpSeconds;

    // Coarse on purpose: catch-up replays years of transfer at startup in interpreted WASM.
    // The resulting state is *declared* truth, so coarseness costs accuracy of the fiction, not
    // determinism of the sim.
    private const double CatchUpTimeStep = 7200;
    private const double Day = 86400;

    /// <summary>#161 · How many catch-up steps a mid-flight hauler's integration hands back the frame after.
    ///
    /// <para>The catch-up is the longest single piece of work left in the whole boot once the two route
    /// searches are yielded around: on the interpreted payload the worst of them measured 1.8–2.1 s. What
    /// makes the size of this number matter is how MANY steps that is, and the answer is not the one the
    /// "20–70 days" in the code above suggests. That is the LEAD — how long she still has to fly — and the
    /// catch-up is everything before it: a Saturn→Mars transfer takes years, the lead is clamped to weeks,
    /// so the integration behind her is of the order of <b>twelve thousand</b> steps at
    /// <see cref="CatchUpTimeStep"/>, not eight hundred.</para>
    ///
    /// <para><b>Which is why this was measured rather than guessed, and the first guess was wrong.</b> At
    /// 128 steps a slice the interpreted boot handed the browser 880 extra frames and went from 15 s to
    /// 31 s — the blocks were tiny and the boot had doubled, because every yield costs a browser
    /// turnaround. Two thousand and forty-eight puts a slice at about 0.3 s interpreted and 0.06 s AOT:
    /// comfortably under the route searches that are now the boot's worst blocks, and about thirty extra
    /// frames over the whole wave.</para>
    ///
    /// <para>It changes no number the sim produces: see <see cref="Simulator.RunSliceBySlice"/> for why the
    /// slicing cannot move the run, and <c>TheSameRunHoweverItIsSliced</c> for the law that says so.</para>
    /// </summary>
    private const int CatchUpStepsPerSlice = 2048;

    // Central-space vs. outer-reaches split for scenario-driven routes: a route touching
    // anything past ~Mars's orbit counts as "long haul" (mid-flight ships spawned already deep
    // in transfer); everything inside stays "short" (scheduled departures). Threshold sits
    // between Mars (2.28e11 m) and Jupiter (7.79e11 m).
    private const double LongHaulThresholdMeters = 4e11;

    /// <summary>The hauler names this world's merchant traffic actually flies under. Public since
    /// #1052 so the port rag can name a hull the reader might have seen on the board outside, instead
    /// of inventing a second ship-name table that contradicts the one on the scope.</summary>
    public static IReadOnlyList<string> Callsigns { get; } =
        ["Meridian", "Kestrel", "Long Haul", "Aurora", "Tycho's Due", "Windlass", "Half Hitch", "Barnacle", "Sable", "Pelican"];

    // Pods are dumb ballistic compute-core canisters — the pirate's milk run and the tutorial's
    // first prey — so they get a prey's nicknames rather than "Pod-1", "Pod-2". Kept distinct from
    // the hauler Callsigns above so a name alone tells you which board a contact came off.
    private static readonly string[] PodCallsigns =
        ["Milk Run", "Windfall", "Ripe Plum", "Fat Goose", "Easy Keeping", "Tin Kettle", "Slow Coach", "Ferryman's Due", "Sitting Duck", "Loose Change"];

    // OG vs. reinforcements (owner: "I want to know which were the OGs and which are the new"):
    // the founding traffic present at world-load carries no tag; every later refill wave stamps
    // its ships and pods with "·N" (N = wave number), visible right in the callsign on the board,
    // the scope and the map label. The id is namespaced too ("npc-w2-…"/"pod-w2-…").
    private static string WaveTag(int wave) => wave == 0 ? "" : $" ·{wave}";

    private static string NpcId(string kind, int wave, int index) =>
        wave == 0 ? $"{kind}-{index}" : $"{kind}-w{wave}-{index}";

    private static readonly (string Origin, string Destination, string Cargo)[] LongRoutes =
        [("saturn", "mars", "He3"), ("saturn", "earth", "He3"), ("jupiter", "mars", "He3")];

    private static readonly (string Origin, string Destination, string Cargo)[] ShortRoutes =
        [("mars", "earth", "Machinery"), ("earth", "mars", "Ice"), ("venus", "earth", "Alloys")];

    private static readonly string[] FixedPodDestinations = ["mars", "venus"];

    public static IReadOnlyList<NpcShip> Generate(ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition? traffic = null)
        => GenerateShipByShip(ephemeris, seed, count, traffic).ToList();

    /// <summary>
    /// #161 · ONE UNIT OF PLANNING WORK, AND THE SHIP WHEN THERE IS ONE.
    ///
    /// <para>The wave is handed over in steps rather than in ships because a SHIP is not a small enough
    /// thing. <see cref="Ship"/> is non-null on exactly the step that finishes hauler
    /// <see cref="ShipIndex"/>, and null on every step of work that gets part of the way there — so a
    /// caller that wants the frame back between blocks can take it at every one of them, and a caller
    /// that only wants the ships can filter, which is what <see cref="GenerateShipByShip"/> is.</para>
    /// </summary>
    public readonly record struct TrafficStep(int ShipIndex, int ShipCount, NpcShip? Ship);

    /// <summary>
    /// #161 · THE SAME WAVE, HANDED OVER ONE STEP OF WORK AT A TIME — the finer half of
    /// <see cref="GenerateShipByShip"/>.
    ///
    /// <para><b>Why one ship was not small enough.</b> #1114 took the boot's fourteen-second block down to
    /// eight by handing the wave over ship by ship, and the boot's own clock then said what the remaining
    /// cost actually is: <c>2,743 + 3,081 + 3,137 + 4,234 + 83 + 98 + 435 + 277 ms</c> on the interpreted
    /// payload. The wave is not eight equal ships; it is four mid-flight haulers costing seconds each and
    /// four scheduled departures costing tenths, and the longest single block a browser was still being
    /// handed was <b>one ship</b> at four and a quarter seconds — well inside the range Chrome will put a
    /// "page unresponsive" dialog over.</para>
    ///
    /// <para><b>What a mid-flight hauler is made of.</b> Three pieces of work, each independently expensive:
    /// a PROBE route planned from the wave's base time (which is how long the crossing takes, and therefore
    /// how far back her virtual departure has to be), the REAL route planned from that departure, and a
    /// catch-up integration forward over the 20–70 days she has already been flying. A scheduled departure
    /// is one route search and nothing else. So this iterator yields after each of them: three steps for a
    /// mid-flight hauler, one for a scheduled one, and the longest block the main thread is ever handed is
    /// a third of the worst ship instead of the whole of her.</para>
    ///
    /// <para><b>It is the same wave, not a similar one</b> — the identical argument
    /// <see cref="GenerateShipByShip"/>'s own docs make, one level down. An iterator suspends and resumes on
    /// the same <see cref="DeterministicRandom"/> in the same state, and no draw happens between two of
    /// these yields that did not happen between the same two lines before. <c>TheSkyIsTheSameSkyOneShip
    /// AtATimeTests</c> walks the wave BOTH ways and compares it ship by ship, field by field.</para>
    /// </summary>
    public static IEnumerable<TrafficStep> GenerateStepByStep(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { Routes.Count: > 0 }
            ? GenerateFromScenario(ephemeris, seed, count, effective)
            : GenerateFromFixedTables(ephemeris, seed, count);
    }

    /// <summary>
    /// #161 · THE SAME WAVE, HANDED OVER ONE SHIP AT A TIME.
    ///
    /// <para>The boot's own measurement: planning eight founding freighters is a single synchronous block
    /// of fourteen seconds on the interpreted WASM payload — ninety-five percent of the whole boot, and by
    /// itself the browser's "page unresponsive" dialog. There is nothing to make faster here (each ship is
    /// two <c>RoutePlanner.PlanRoute</c> searches and a catch-up integration, and they are the world's
    /// physics), but there IS something to make INTERRUPTIBLE: the loop's iterations are independent of
    /// each other except through the one <see cref="DeterministicRandom"/> they share, so a caller that
    /// wants to hand the frame back to the browser between ships can, simply by walking this instead.</para>
    ///
    /// <para><b>It is the same wave, not a similar one.</b> An iterator suspends between yields and resumes
    /// exactly where it stopped, on the same rng in the same state — so ship <c>i</c> is drawn from the
    /// identical sequence whether the caller materialises the list in one breath (which is what
    /// <see cref="Generate"/> above now does) or takes a frame between each. <c>TheSkyIsTheSameSkyOneShip
    /// AtATimeTests</c> holds that: the two ways of walking it are compared ship by ship.</para>
    /// </summary>
    public static IEnumerable<NpcShip> GenerateShipByShip(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition? traffic = null)
        // #161 · …and one step finer underneath, because one SHIP was still a four-second block. This is
        // the same walk with the part-way steps filtered out, so everything the wave is — the rng, the
        // order, the ships — is stated exactly once (see GenerateStepByStep). The argument check stays
        // eager because the call below is made HERE rather than inside an iterator body.
        => GenerateStepByStep(ephemeris, seed, count, traffic)
            .Where(step => step.Ship is not null)
            .Select(step => step.Ship!);

    /// <summary>
    /// The world keeps living (owner, 2026-07-05: after every ship arrived the sky emptied —
    /// "THERE IS NOBODY IN SPACE"): a fresh wave of traffic planned relative to
    /// <paramref name="nowSimTime"/> — mid-flight ships already deep in transfer as of NOW,
    /// short runs departing over the following weeks — with ids namespaced by
    /// <paramref name="waveNumber"/> so waves never collide. Deterministic per (seed, wave).
    /// </summary>
    public static IReadOnlyList<NpcShip> GenerateWave(
        ICelestialEphemeris ephemeris, ulong seed, int count, double nowSimTime, int waveNumber, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        IEnumerable<TrafficStep> steps = effective is { Routes.Count: > 0 }
            ? GenerateFromScenario(ephemeris, seed, count, effective, nowSimTime, waveNumber)
            : GenerateFromFixedTables(ephemeris, seed, count, nowSimTime, waveNumber);
        return steps.Where(step => step.Ship is not null).Select(step => step.Ship!).ToList();
    }

    /// <summary>A fresh pod wave, launching over the days after <paramref name="nowSimTime"/> —
    /// the milk run never dries up.</summary>
    public static IReadOnlyList<NpcShip> GeneratePodsWave(
        ICelestialEphemeris ephemeris, ulong seed, int count, double nowSimTime, int waveNumber, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { PodLaunchers.Count: > 0 }
            ? GeneratePodsFromScenario(ephemeris, seed, count, effective, nowSimTime, waveNumber)
            : GeneratePodsFromFixedLauncher(ephemeris, seed, count, nowSimTime, waveNumber);
    }
}
