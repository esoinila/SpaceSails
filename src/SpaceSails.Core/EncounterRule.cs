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
/// simple deterministic pursuit — dumb, relentless, sufficient for v1 (owner's framing).
/// <para><paramref name="Warrant"/> is #962's provenance: the callsign of the hull the contract was written
/// over. Owner, docked at a haven with the heat gauge at zero and a collector still inbound: <i>"So we have
/// zero heat and are docked at haven … why is this still hunting us?"</i> — a question a callsign alone
/// cannot answer, and the robbery that bought the collector had that name in hand and threw it away. Null
/// for the tutorial and cheat spawns, which were bought by nobody.</para></summary>
public readonly record struct HunterState(
    string Id,
    string Callsign,
    string OriginBodyId,
    double SpawnedAtSimTime,
    double ActivationSimTime,
    ShipState State,
    bool CaughtPlayer,
    bool BrokenOff,
    int WarningShotsTaken = 0,
    double PeeledUntilSimTime = double.NegativeInfinity,
    string? Warrant = null);

/// <summary>
/// The gun deck (vision ¶18): warning shots, compliance, threats, bribery and the HEAT a robbery
/// leaves behind. A warning shot inside weapon range makes a compliant freighter heave to (fast,
/// bloodless boarding); a stubborn one calls its own muscle instead. Bribery buys the same
/// compliance without the heat — an inside job, nobody calls the cavalry. Every decision here is
/// a pure function of its inputs (ship id hashes, sim time, player heat) — determinism is law in
/// Core. The client owns all mutable state (which ship was warned, which is bribed, the hunter
/// roster, the heat gauge) the same way NpcState.Boarded already tracks capture in Map.razor.
/// </summary>
public static partial class EncounterRule
{
    /// <summary>The gun deck's horizon: a shot fired now has to LAND inside this flight time or the
    /// solution is aiming at a guess. Three hours is this ship's doctrine — long enough for a mass-driver
    /// slug to cross a whole encounter, short enough that the target's own drift cannot have rewritten the
    /// geometry the solution was cut from.</summary>
    public const double EngagementFlightSeconds = 3 * 3600;

    /// <summary>
    /// #961/#962 · THE LASSO CANNOT REACH FURTHER THAN THE GUN. Owner, watching a collector close on him
    /// from outside any firing solution he could compute: <i>"If we don't have a firing solution then they
    /// cannot board us / catch us either. It is like being outside of bullets range but still getting
    /// lassoed. So the bullets need to fly faster."</i>
    ///
    /// <para>He is right, and the comment that stood here said the opposite ON PURPOSE — <i>"guns speak
    /// before shuttles fly: weapons reach less than half of CaptureRule's 5e8 m boarding envelope"</i>.
    /// 2·10⁸ m of reach against a 3·10⁸ m <see cref="CatchRadiusMeters"/> and a 5·10⁸ m
    /// <see cref="CaptureRule.CaptureRadiusMeters"/>: every range at which a hunter could lay hands on the
    /// captain was a range at which he could not answer. That is not tension, it is a cutscene.</para>
    ///
    /// <para>Reversed by the owner's ruling. Reach is DERIVED now —
    /// <see cref="OrdnanceRule.MassDriverMuzzleSpeedMps"/> × <see cref="EngagementFlightSeconds"/>, 66 km/s
    /// for three hours = 712,800 km — and it covers both envelopes with room to spare. The two sides of that
    /// inequality come from deliberately independent places: the muzzle from #961's physics, the envelopes
    /// from the shuttle and pursuit rules. Nothing makes them agree except the Core guard that pins the law
    /// — which is the point, since deriving one from the other would leave the guard asserting a
    /// tautology.</para>
    ///
    /// <para>It is also the ONE number the dossier quotes now. The card used to quote muzzle × one day
    /// (691,200 km) while <see cref="InWeaponRange"/> enforced 200,000 km — a sentence and a sim disagreeing
    /// by three and a half times, on the very card the owner was reading while he asked what was going on.
    /// That is this repo's own named bug class, and it was live.</para>
    /// </summary>
    public const double WeaponRangeMeters = OrdnanceRule.MassDriverMuzzleSpeedMps * EngagementFlightSeconds;

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

    // #380 item 4 · CatchFineCredits (500) USED TO LIVE HERE. It was the pre-BUSTED consequence — lose
    // the hold, pay a flat toll — and PR-BUSTED replaced the whole of it with <see cref="BustedRule"/>'s
    // submit / bribe / resist ladder without deleting the number. Nothing has read it since. It is removed
    // rather than left, because both guides went on quoting it for six weeks after the flow it described
    // stopped existing: a constant with no consumer is a claim with no owner, and the documentation was
    // reading it as if it were still the law. What the catch actually costs is BustedRule.CoinFraction,
    // BustedRule.BribeDemand and BustedRule.ResistCheck, and those are the only numbers a guide may quote.

    /// <summary>Stay hidden at a haven this long and a hunter loses the scent.</summary>
    public const double BreakOffHiddenDays = 2;

    /// <summary>Some collectors prize the good life over the fee: one warning shot near them and
    /// they sheer off for good. This baseline fraction thins as heat (the bounty) climbs — a fat
    /// contract draws hungry, gritty muscle, not sybarites.</summary>
    public const double LaDolceVitaFraction = 0.20;

    public const double LaDolceVitaFractionPerHeatLevel = 0.05;

    public const double MinLaDolceVitaFraction = 0.05;

    /// <summary>Warning shots a professional collector will weather before it voids the contract;
    /// grittier (needs one more per heat level) the more notorious the captain.</summary>
    public const int BaseHunterNerve = 3;

    /// <summary>Each warning shot buys a coast-off this many days long, multiplied by the number
    /// of shots so far — a rattled collector peels away longer each time before re-acquiring.</summary>
    public const double HunterPeelStepDays = 1.5;

    /// <summary>Attack out of the sun: the star sits at the world origin, so a collector astern of
    /// the player with the sun beyond is staring into glare. Inside this half-angle cone (and with
    /// the player on the sunward side) the hunter loses its fix and coasts, unable to close.</summary>
    public const double SunGlareConeDegrees = 12;

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
    /// reputation grows.
    ///
    /// <para>#534 · <b>A hull that is not a merchant does not answer as one.</b> A masked warship
    /// (<see cref="QShip.IsMasked"/>) never heaves to, at any heat, and that is the whole of what
    /// committing to her costs: no new rule about combat, no special case at the boarding gate — the
    /// ordinary machinery meeting a target that shoots back. Everything downstream already exists and
    /// resolves off this one answer (a warning shot buys nothing, the robbery costs the stubborn heat,
    /// and the muscle she calls is the muscle the heat already spawns). The read before the pass was the
    /// whole chance.</para></summary>
    public static ComplianceState ComplianceOf(NpcShip npc, int playerHeat)
    {
        if (npc.IsPod)
        {
            return ComplianceState.NothingToComply;
        }

        if (QShip.IsMasked(npc))
        {
            return ComplianceState.Stubborn;
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
}
