namespace SpaceSails.Core;

/// <summary>Deterministic per-ship disposition once a warning shot lands. Pirates here TAX trade
/// rather than sink it — see <see cref="EncounterRule"/>.</summary>
public enum ComplianceState
{
    /// <summary>No crew to negotiate with — a mass-driver pod.</summary>
    NothingToComply,

    /// <summary>Heaves to under a warning shot: boards fast, no return fire.</summary>
    Compliant,

    /// <summary>Escorted/insured — ignores the warning and calls its own muscle.</summary>
    Stubborn,
}

/// <summary>The player's current heat: how loudly the outer reaches are talking about them.
/// <see cref="RaisedAtSimTime"/> is also the decay clock's last checkpoint — every raise or
/// consumed decay period resets it, so <see cref="EncounterRule.DecayHeat"/> only ever measures
/// time since the last change.</summary>
public readonly record struct HeatState(int Level, double RaisedAtSimTime)
{
    public static readonly HeatState None = new(0, double.NegativeInfinity);
}

/// <summary>Hired muscle: one per heat event, fitting out at a policed body before it flies. A
/// simple deterministic pursuit — dumb, relentless, sufficient for v1 (owner's framing).</summary>
public readonly record struct HunterState(
    string Id,
    string Callsign,
    string OriginBodyId,
    double SpawnedAtSimTime,
    double ActivationSimTime,
    ShipState State,
    bool CaughtPlayer,
    bool BrokenOff);

/// <summary>
/// The gun deck (vision ¶18): warning shots, compliance, threats, bribery and the HEAT a robbery
/// leaves behind. A warning shot inside weapon range makes a compliant freighter heave to (fast,
/// bloodless boarding); a stubborn one calls its own muscle instead. Bribery buys the same
/// compliance without the heat — an inside job, nobody calls the cavalry. Every decision here is
/// a pure function of its inputs (ship id hashes, sim time, player heat) — determinism is law in
/// Core. The client owns all mutable state (which ship was warned, which is bribed, the hunter
/// roster, the heat gauge) the same way NpcState.Boarded already tracks capture in Map.razor.
/// </summary>
public static class EncounterRule
{
    /// <summary>Guns speak before shuttles fly: weapons reach less than half of CaptureRule's
    /// 5e8 m boarding envelope.</summary>
    public const double WeaponRangeMeters = 2e8;

    /// <summary>A compliant/bribed target heaves to: boarding shuttles cross in half the time
    /// <see cref="CaptureRule.RequiredSecondsFor"/> would otherwise demand.</summary>
    public const double ComplianceBoardingFactor = 0.5;

    /// <summary>Baseline fraction of ships that are "escorted" — insured, stubborn, call their
    /// own muscle rather than heave to. ~1 in 4, so a busy shipping lane still has soft targets.</summary>
    public const double BaseStubbornFraction = 0.25;

    /// <summary>Word travels: every heat level nudges the stubborn fraction up (targets get
    /// jumpier the more the outer reaches hear about you), capped well short of certainty.</summary>
    public const double StubbornFractionPerHeatLevel = 0.05;

    public const double MaxStubbornFraction = 0.6;

    /// <summary>Cheaper than the cargo's worth — that's the point (owner's design): an inside job
    /// costs less than an honest robbery pays.</summary>
    public const double BribePriceFraction = 0.35;

    public const int MaxHeatLevel = 3;

    /// <summary>Cooling-off rate away from a haven: one level per this many days.</summary>
    public const double HeatDecayDays = 20;

    /// <summary>Riding it out at a small-moon haven cools this many times faster.</summary>
    public const double HavenDecayMultiplier = 4;

    /// <summary>Hired muscle needs to fit out before it can fly.</summary>
    public const double HunterFittingOutDays = 5;

    /// <summary>Thrust-limited pursuit acceleration — dumb, relentless, not a warp-drive.</summary>
    public const double HunterAccelMps2 = 0.5;

    /// <summary>Pursuit integrates in the same coarse cadence NPC traffic does
    /// (<see cref="TrafficSchedule.NpcTimeStep"/>) — kept as its own constant since hunters are
    /// deliberately not part of the NPC schedule.</summary>
    public const double HunterStepSeconds = 60;

    /// <summary>Caught: inside this range...</summary>
    public const double CatchRadiusMeters = 3e8;

    /// <summary>...at under this relative speed — a hunter roaring past at speed doesn't count.</summary>
    public const double CatchRelativeSpeedMetersPerSecond = 3000;

    /// <summary>Adrift-style consequence (M6's flow, reused): lose the hold, pay the toll.</summary>
    public const int CatchFineCredits = 500;

    /// <summary>Stay hidden at a haven this long and a hunter loses the scent.</summary>
    public const double BreakOffHiddenDays = 2;

    /// <summary>Central/policed space vs. the outer reaches — the same split TrafficSchedule uses
    /// for long-haul traffic. A planet past this threshold is pirate country, not a source of
    /// muscle.</summary>
    public const double PolicedThresholdMeters = 4e11;

    private const double DaySeconds = 86400;

    public static bool InWeaponRange(ShipState player, ShipState target) =>
        (player.Position - target.Position).LengthSquared <= WeaponRangeMeters * WeaponRangeMeters;

    /// <summary>Deterministic per-ship "type": hashes the ship's id rather than drawing from any
    /// live RNG stream, so asking twice (or asking on client and server) always agrees. Heat
    /// nudges the odds — the same ship can flip from compliant to stubborn as the player's
    /// reputation grows.</summary>
    public static ComplianceState ComplianceOf(NpcShip npc, int playerHeat)
    {
        if (npc.IsPod)
        {
            return ComplianceState.NothingToComply;
        }

        double stubbornFraction = Math.Min(MaxStubbornFraction,
            BaseStubbornFraction + StubbornFractionPerHeatLevel * Math.Max(0, playerHeat));
        double roll = new DeterministicRandom(HashSeed(npc.Id)).NextDouble();
        return roll < stubbornFraction ? ComplianceState.Stubborn : ComplianceState.Compliant;
    }

    private static readonly string[] SurrenderLines =
    [
        "\"Heaving to! Don't shoot — we're insured for the delay, not the hull.\"",
        "\"Take the cargo, take it all. Just log this as 'pirates', not 'incompetence'.\"",
        "\"She strikes her colours. No heroics aboard this bucket.\"",
    ];

    private static readonly string[] DefianceLines =
    [
        "\"We've got friends with bigger guns. Enjoy the head start.\"",
        "\"Fire away — the underwriters will send someone to discuss it with you.\"",
        "\"Not today. Not ever. The muscle's already on the wire.\"",
    ];

    /// <summary>Canned hail response — pirate-flavored, deterministic by ship id so hailing the
    /// same ship twice never changes its story.</summary>
    public static string ThreatOutcome(NpcShip npc, ComplianceState compliance)
    {
        if (compliance == ComplianceState.NothingToComply)
        {
            return "No answer — just telemetry and a ballistic trajectory. Nothing aboard to threaten.";
        }

        string[] lines = compliance == ComplianceState.Stubborn ? DefianceLines : SurrenderLines;
        int index = new DeterministicRandom(HashSeed(npc.Id) ^ 0x54687265617421UL).NextInt(0, lines.Length);
        return lines[index];
    }

    /// <summary>Cheaper than the cargo's worth — reuses <see cref="CargoMarket"/>'s per-unit fence
    /// prices so the discount is always honest relative to what the robbery would actually pay.</summary>
    public static int BribePrice(NpcShip npc) =>
        (int)Math.Round(npc.CargoUnits * CargoMarket.UnitValue(npc.CargoClass) * BribePriceFraction);

    public static HeatState RaiseHeat(HeatState state, int amount, double simTime)
    {
        int level = Math.Clamp(state.Level + amount, 0, MaxHeatLevel);
        return new HeatState(level, simTime);
    }

    /// <summary>Pure decay: one level per <see cref="HeatDecayDays"/>, <see cref="HavenDecayMultiplier"/>×
    /// faster while <paramref name="atHavenOrbit"/>. Call every tick with the current sim time;
    /// state only actually changes once a full decay period has elapsed since the last raise or
    /// decay, so repeated calls with the same inputs are idempotent.</summary>
    public static HeatState DecayHeat(HeatState state, double simTime, bool atHavenOrbit)
    {
        if (state.Level <= 0)
        {
            return state;
        }

        double periodSeconds = HeatDecayDays * DaySeconds / (atHavenOrbit ? HavenDecayMultiplier : 1);
        double elapsed = simTime - state.RaisedAtSimTime;
        if (elapsed < periodSeconds)
        {
            return state;
        }

        int levelsLost = (int)(elapsed / periodSeconds);
        int newLevel = Math.Max(0, state.Level - levelsLost);
        double consumed = levelsLost * periodSeconds;
        return new HeatState(newLevel, state.RaisedAtSimTime + consumed);
    }

    /// <summary>One hunter, fitting out at the nearest policed body — parked there (riding the
    /// body's own orbital velocity) until <see cref="HunterFittingOutDays"/> pass.</summary>
    public static HunterState SpawnHunter(string id, string callsign, string originBodyId,
        Vector2d originPosition, Vector2d originVelocity, double simTime) =>
        new(id, callsign, originBodyId, simTime, simTime + HunterFittingOutDays * DaySeconds,
            new ShipState(originPosition, originVelocity, simTime), CaughtPlayer: false, BrokenOff: false);

    /// <summary>Nearest planet inside <see cref="PolicedThresholdMeters"/> that isn't a haven —
    /// where hired muscle comes from (Earth/Mars in Sol; central, policed space generally). Null
    /// if nothing policed is reachable — a pure outer-reaches scenario has no cavalry to call.</summary>
    public static CelestialBody? NearestPolicedBody(ICelestialEphemeris ephemeris, Vector2d playerPosition, double simTime)
    {
        CelestialBody? best = null;
        double bestDistance = double.MaxValue;
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.IsHaven || body.ParentId is null)
            {
                continue; // havens shelter pirates, not hunters; the sun itself isn't a "body"
            }

            Vector2d position = ephemeris.Position(body.Id, simTime);
            if (position.Length >= PolicedThresholdMeters)
            {
                continue; // outer reaches — no policed muscle stationed out here
            }

            double distance = (position - playerPosition).Length;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = body;
            }
        }

        return best;
    }

    /// <summary>Dumb, relentless pursuit: thrust-limited acceleration toward the player's CURRENT
    /// position, integrated over whatever <paramref name="simTime"/> delta the caller advances by
    /// (Map.razor calls this in <see cref="HunterStepSeconds"/> quanta to match the NPC cadence).
    /// Before <see cref="HunterState.ActivationSimTime"/> the hunter just coasts on the velocity
    /// it was parked with (still fitting out); once caught or broken off it holds still, a spent
    /// contact the caller is free to retire.</summary>
    public static HunterState AdvanceHunter(HunterState hunter, ShipState player, double simTime)
    {
        if (hunter.CaughtPlayer || hunter.BrokenOff)
        {
            return hunter;
        }

        double dt = simTime - hunter.State.SimTime;
        if (dt <= 0)
        {
            return hunter;
        }

        if (simTime < hunter.ActivationSimTime)
        {
            Vector2d parked = hunter.State.Position + hunter.State.Velocity * dt;
            return hunter with { State = new ShipState(parked, hunter.State.Velocity, simTime) };
        }

        Vector2d toPlayer = player.Position - hunter.State.Position;
        Vector2d accelDirection = toPlayer.Normalized();
        Vector2d newVelocity = hunter.State.Velocity + accelDirection * HunterAccelMps2 * dt;
        Vector2d newPosition = hunter.State.Position + hunter.State.Velocity * dt;
        var newState = new ShipState(newPosition, newVelocity, simTime);

        double distance = (newPosition - player.Position).Length;
        double relativeSpeed = (newVelocity - player.Velocity).Length;
        bool caught = distance < CatchRadiusMeters && relativeSpeed < CatchRelativeSpeedMetersPerSecond;

        return hunter with { State = newState, CaughtPlayer = caught };
    }

    /// <summary>The player has stayed hidden at a haven this long — the hunter loses the scent.
    /// <paramref name="hiddenDurationSeconds"/> is however long the caller has tracked continuous
    /// haven orbit; Map.razor owns that clock since it depends on the player's live flight path,
    /// not anything this pure function can see on its own.</summary>
    public static HunterState ApplyBreakOff(HunterState hunter, double hiddenDurationSeconds) =>
        !hunter.CaughtPlayer && !hunter.BrokenOff && hiddenDurationSeconds >= BreakOffHiddenDays * DaySeconds
            ? hunter with { BrokenOff = true }
            : hunter;

    // FNV-1a 64-bit: stable across processes and platforms (unlike string.GetHashCode, which is
    // randomized per run) — determinism is law, and ship ids seed every deterministic roll here.
    private static ulong HashSeed(string id)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (char c in id)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}
