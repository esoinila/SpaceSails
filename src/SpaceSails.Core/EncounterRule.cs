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
public static class EncounterRule
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

    /// <summary>One hunter, fitting out at the nearest policed body — parked there (riding the
    /// body's own orbital velocity) until <see cref="HunterFittingOutDays"/> pass.</summary>
    public static HunterState SpawnHunter(string id, string callsign, string originBodyId,
        Vector2d originPosition, Vector2d originVelocity, double simTime, string? warrant = null) =>
        new(id, callsign, originBodyId, simTime, simTime + HunterFittingOutDays * DaySeconds,
            new ShipState(originPosition, originVelocity, simTime), CaughtPlayer: false, BrokenOff: false,
            Warrant: warrant);

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

    /// <summary>#580 · THE CHASE HOLDS ITS BREATH WHILE THE CAPTAIN IS OFF THE SHIP.
    ///
    /// <para>Owner, walking Miranda with his ship docked and empty behind him: <i>"zero heat against empty
    /// ship (we don't have any playing there, clearly there is some lights on keeper on ship but we do not
    /// play that)"</i>, <i>"the heat must follow the captain, not the ship"</i>, and the play argument that
    /// settles it — <i>"we don't want to be guarding our parking lot ... that is not good game play :-D"</i>.</para>
    ///
    /// <para>He is right, and the bug was live: the pursuit loop kept running through an excursion, so a
    /// hunter could reach and CATCH a hull with nobody aboard it, opening a boarding demand at a captain
    /// standing in a suit on a moon. Whatever the collectors want, they want it from the person, and the
    /// person is not there.</para>
    ///
    /// <para>Holding is not the same as freezing. The hunter's clock is carried forward to
    /// <paramref name="simTime"/> so that coming back aboard resumes the chase from where it stood, rather
    /// than letting the pursuit integrate the whole excursion in one burst and land on the captain the
    /// instant they climb the ladder. Position and velocity are untouched: the wolf waited.</para></summary>
    public static HunterState HoldStation(HunterState hunter, double simTime) =>
        hunter with { State = hunter.State with { SimTime = simTime } };

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

        // Coasting cases — the hunter drifts on its current velocity, unable to refine the chase:
        // still fitting out, peeled off after a warning shot, or blinded by the sun behind the
        // player. In every case it stops closing until the condition clears.
        if (simTime < hunter.ActivationSimTime
            || simTime < hunter.PeeledUntilSimTime
            || SunBlinded(hunter.State.Position, player.Position))
        {
            Vector2d coasted = hunter.State.Position + hunter.State.Velocity * dt;
            return hunter with { State = new ShipState(coasted, hunter.State.Velocity, simTime) };
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

    /// <summary>
    /// Fire control's hunter special case (the aim-solution fork): a hunter flies the PURSUIT
    /// LAW, not gravity — dead-reckoning it through the Simulator (the standard freighter
    /// estimate) is wrong twice over: it adds a solar pull the hunter never feels AND drops the
    /// <see cref="HunterAccelMps2"/> it relentlessly adds toward the player. That error is
    /// ½·a·τ² ≈ 13,000 km on a 2 h slug flight, against OrdnanceRule's 5e5 m hit radius — a
    /// guaranteed structural miss on anything past a knife-fight. So REPLAY
    /// <see cref="AdvanceHunter"/> itself, in its own <see cref="HunterStepSeconds"/> quanta,
    /// against the player's PLOTTED course: to our own gun deck the pursuit law is public
    /// knowledge the way gravity is to PathPredictor — the only honest unknown is whether the
    /// player keeps to the plot (burn off it and you bend your own firing solution: the
    /// collector chases the real you, not the plan). Cost: horizon/60 s of plain additions and
    /// one recorded knot per <paramref name="maxKnots"/> stride — the hunter stays exactly as
    /// light as it flies. The hunter's true track is itself piecewise-linear at these quanta
    /// (Euler positions), so undecimated knots are EXACT, not sampled. A predicted catch or
    /// break-off freezes the track (a spent contact holds still). The final knot always lands
    /// exactly on the horizon, so <c>Samples[^1].Position</c> is the aim point at t_hit.
    /// </summary>
    public static IReadOnlyList<TrajectorySample> PredictHunterPath(
        HunterState hunter,
        IReadOnlyList<TrajectorySample> playerPath,
        double horizonSeconds,
        int maxKnots = 4000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxKnots);
        double end = hunter.State.SimTime + Math.Max(0, horizonSeconds);
        int totalSteps = (int)Math.Ceiling(Math.Max(0, horizonSeconds) / HunterStepSeconds);
        // Decimation for months-long horizons: integrate every quantum, record every stride-th.
        // Between recorded knots a linear read sags by at most ⅛·a·(stride·60)² — under the
        // 5e5 m hit radius up to ~40 min strides, i.e. horizons beyond a month.
        int stride = totalSteps / maxKnots + 1;

        var samples = new List<TrajectorySample>(Math.Min(totalSteps, maxKnots) + 2)
        {
            new(hunter.State.SimTime, hunter.State.Position),
        };

        HunterState h = hunter;
        int step = 0;
        int cursor = 0; // step times only grow — resume each path search where the last ended
        while (h.State.SimTime < end && !h.CaughtPlayer && !h.BrokenOff)
        {
            double stepTime = Math.Min(end, h.State.SimTime + HunterStepSeconds);
            h = AdvanceHunter(h, PlayerStateAt(playerPath, stepTime, ref cursor), stepTime);
            step++;
            if (step % stride == 0 || h.State.SimTime >= end)
            {
                samples.Add(new TrajectorySample(h.State.SimTime, h.State.Position));
            }
        }

        if (samples[^1].SimTime < end)
        {
            samples.Add(new TrajectorySample(end, samples[^1].Position));
        }

        return samples;
    }

    /// <summary>The player's plotted state at a sim time: position interpolated linearly along
    /// the path, velocity the local segment's slope (all <see cref="AdvanceHunter"/>'s steering
    /// and catch check need). Beyond either end the nearest leg extrapolates — a shot aimed past
    /// the plot's horizon is reaching past what the plot honestly knows anyway.
    /// <paramref name="cursor"/> is a monotonic resume hint: query times only ever grow, so the
    /// whole replay walks the path once instead of rescanning it every quantum.</summary>
    private static ShipState PlayerStateAt(IReadOnlyList<TrajectorySample> path, double simTime, ref int cursor)
    {
        if (path.Count == 0)
        {
            throw new ArgumentException("player path must hold at least one sample", nameof(path));
        }

        if (path.Count == 1)
        {
            return new ShipState(path[0].Position, Vector2d.Zero, simTime);
        }

        while (cursor < path.Count - 2 && path[cursor + 1].SimTime < simTime)
        {
            cursor++;
        }

        TrajectorySample a = path[cursor], b = path[cursor + 1];
        double span = b.SimTime - a.SimTime;
        if (span <= 0)
        {
            return new ShipState(b.Position, Vector2d.Zero, simTime);
        }

        Vector2d velocity = (b.Position - a.Position) / span;
        double f = (simTime - a.SimTime) / span;
        return new ShipState(a.Position + (b.Position - a.Position) * f, velocity, simTime);
    }

    /// <summary>The player has stayed hidden at a haven this long — the hunter loses the scent.
    /// <paramref name="hiddenDurationSeconds"/> is however long the caller has tracked continuous
    /// haven orbit; Map.razor owns that clock since it depends on the player's live flight path,
    /// not anything this pure function can see on its own.</summary>
    public static HunterState ApplyBreakOff(HunterState hunter, double hiddenDurationSeconds) =>
        !hunter.CaughtPlayer && !hunter.BrokenOff && hiddenDurationSeconds >= BreakOffHiddenDays * DaySeconds
            ? hunter with { BrokenOff = true }
            : hunter;

    // ── #962 · WHAT ENDS THE HUNT, SAID OUT LOUD, WITH THE LIVE NUMBER ────────────────────────────────
    //
    // Owner, docked at a haven with the heat gauge reading zero and a collector still inbound: "So we have
    // zero heat and are docked at haven ... why is this still hunting us?" The rules that would have
    // answered him were all RIGHT HERE and none of them was ever said to his face: the contract is bought
    // once and does not care what the heat gauge says afterwards; hiding two unbroken days at a haven is
    // what makes her lose the scent; warning shots erode her nerve until she voids it; a holed sail ends
    // it outright. He was reading a dossier that told him her callsign, her range, and nothing he could act
    // on.
    //
    // These live in EncounterRule, beside ApplyBreakOff and WarnOff, ON PURPOSE. This repo has a named bug
    // class for a sentence that reports one thing while the sim does another, and the only structural
    // defence is that the sentence and the rule read the SAME constant in the same file — so the countdown
    // below reaches zero on exactly the tick ApplyBreakOff fires, and the shot count below reaches zero on
    // exactly the shot WarnOff voids the contract on. Both agreements are swept in EncounterRuleTests.

    /// <summary>Who bought this contract. A collector is hired over ONE job — the hull you took — and where
    /// that name is known it is named, because "why is this still hunting us" is not a question a callsign
    /// can answer. The unattributed case says the true thing too: nobody the captain has ever met.</summary>
    public static string WarrantLine(HunterState hunter) =>
        hunter.Warrant is { Length: > 0 } hull
            ? $"⚖ writ served over the {hull} job — underwriters, not the law. Heat cools; a contract does not."
            : "⚖ writ served by underwriters you never met — recovery of an insured asset. Heat cools; a contract does not.";

    /// <summary>The hiding clause, with the clock running. <paramref name="hiddenDurationSeconds"/> is the
    /// caller's continuous haven-hiding clock — the same value it hands <see cref="ApplyBreakOff"/> — and
    /// <paramref name="hiddenNow"/> says whether that clock is running at all, because a clock that is not
    /// running is the single most useful thing a captain can be told while a collector closes.</summary>
    public static string HidingTerm(double hiddenDurationSeconds, bool hiddenNow)
    {
        // Invariant throughout: determinism is law in Core, and a decimal comma in a Finnish browser
        // would make this sentence differ from the one a test read.
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        string window = BreakOffHiddenDays.ToString("0.#", invariant);
        if (!hiddenNow)
        {
            return $"🏴 she loses the scent after {window} d hidden at a haven — the clock is NOT running";
        }

        double remaining = BreakOffHiddenDays * DaySeconds - hiddenDurationSeconds;
        return remaining <= 0
            ? "🏴 she has lost the scent — the haven kept you"
            : $"🏴 she loses the scent after {window} d hidden at a haven — "
              + $"{(remaining / DaySeconds).ToString("0.0", invariant)} d to go";
    }

    /// <summary>The nerve clause, counted down to the shot that actually voids the contract — the same
    /// arithmetic <see cref="WarnOff"/> does, off the same <see cref="NerveThreshold"/>.</summary>
    public static string NerveTerm(HunterState hunter, int playerHeat)
    {
        int remaining = NerveThreshold(hunter.Id, playerHeat) - hunter.WarningShotsTaken;
        return remaining <= 0
            ? "🎖 she has had enough already — the contract is void"
            : remaining == 1
                ? "🎖 …or ONE more warning shot near her voids the contract"
                : $"🎖 …or {remaining} more warning shots near her void the contract";
    }

    /// <summary>The third door, which needs no number: hole her sail and she is out of the chase for good.
    /// Said here so the card carries every way out, not the two that happen to have clocks.</summary>
    public const string SailTerm = "🎯 …or hole her sail — a holed collector breaks off for good";

    /// <summary>Deterministic per-collector disposition: does this one prize the good life over
    /// the fee? Such a collector voids the contract at the very first warning shot. Rarer as heat
    /// (the bounty) rises. Salted apart from <see cref="ComplianceOf"/> so a ship and a hunter that
    /// happen to share an id never correlate.</summary>
    public static bool PrefersTheGoodLife(string hunterId, int playerHeat)
    {
        double fraction = Math.Max(MinLaDolceVitaFraction,
            LaDolceVitaFraction - LaDolceVitaFractionPerHeatLevel * Math.Max(0, playerHeat));
        double roll = new DeterministicRandom(HashSeed(hunterId) ^ 0x4C61446F6C6365UL).NextDouble();
        return roll < fraction;
    }

    /// <summary>How many warning shots this collector weathers before giving up for good: the
    /// good-life sort quit at the first, everyone else needs <see cref="BaseHunterNerve"/>, plus
    /// one per heat level (a notorious captain draws grittier muscle).</summary>
    public static int NerveThreshold(string hunterId, int playerHeat) =>
        PrefersTheGoodLife(hunterId, playerHeat) ? 1 : BaseHunterNerve + Math.Max(0, playerHeat);

    /// <summary>A warning shot lands near the collector: its nerve erodes. Each shot buys a longer
    /// coast-off (it stops closing — see <see cref="AdvanceHunter"/>), and once the shots reach its
    /// <see cref="NerveThreshold"/> it voids the contract for good. A caught or already-broken
    /// hunter is unmoved. Pure: the peel window is derived from the shot count and sim time, no
    /// live RNG.</summary>
    public static HunterState WarnOff(HunterState hunter, int playerHeat, double simTime)
    {
        if (hunter.CaughtPlayer || hunter.BrokenOff)
        {
            return hunter;
        }

        int shots = hunter.WarningShotsTaken + 1;
        if (shots >= NerveThreshold(hunter.Id, playerHeat))
        {
            return hunter with { WarningShotsTaken = shots, BrokenOff = true };
        }

        double peelUntil = simTime + shots * HunterPeelStepDays * DaySeconds;
        return hunter with { WarningShotsTaken = shots, PeeledUntilSimTime = peelUntil };
    }

    /// <summary>Attack out of the sun: true when the player sits inside the glare cone as seen from
    /// the hunter (the sun is at the world origin) AND is on the sunward side — the hunter would be
    /// squinting into the star to find them. Pure geometry, no state.</summary>
    public static bool SunBlinded(Vector2d hunterPosition, Vector2d playerPosition)
    {
        double hunterDist = hunterPosition.Length;
        // Player must be nearer the sun than the hunter — i.e. genuinely between star and pursuer.
        if (playerPosition.Length >= hunterDist || hunterDist <= 0)
        {
            return false;
        }

        Vector2d toPlayer = playerPosition - hunterPosition;
        Vector2d toSun = -hunterPosition; // origin - hunter
        double toPlayerLen = toPlayer.Length;
        if (toPlayerLen <= 0)
        {
            return false;
        }

        double cosAngle = toPlayer.Dot(toSun) / (toPlayerLen * hunterDist);
        return cosAngle >= Math.Cos(SunGlareConeDegrees * Math.PI / 180.0);
    }

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
