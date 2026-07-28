namespace SpaceSails.Core;

/// <summary>
/// #488 · THE LOITER — the autopilot mode that keeps the ship at shuttle range while the away team is
/// inside something that is neither a world nor a berth.
///
/// <para>Owner: <i>"our autopilot would have to have a new mode (that does not cost an arm and a leg) that
/// just keeps us at approximate relative shuttle-range position … that the ship does not leave them
/// behind :-D"</i></para>
///
/// <para><b>Lab 40 wrote this file's design.</b> The lab set out to price the hold by REGIME and found
/// that the regime barely matters — what matters is WHAT YOU HOLD:</para>
/// <list type="bullet">
/// <item><b>A fixed offset is a trap.</b> Parking at a constant displacement from the wreck and matching
/// its velocity puts the ship at a different distance from the parent, which is a DIFFERENT ORBIT, and
/// different orbits leave. Close over Jupiter that hold diverges in 4.7 days and each recovery is a
/// 33.29 km/s burn — 111 pulses of a 500-pulse tank. That is the arm and the leg, and it is entirely
/// self-inflicted.</item>
/// <item><b>A co-orbital hold is free.</b> Share the wreck's own orbit — same radius, same speed,
/// displaced only ALONG the track — and the measured cost was ZERO pulses per day in every regime the lab
/// swept. Not cheap: free. Two objects sharing an orbit are not fighting anything, so there is nothing to
/// pay for.</item>
/// </list>
///
/// <para>So this kernel does not price fuel, because there is no fuel to price. What it does instead is
/// guard the thing that actually decides whether the ride is still there: <b>the hand-off</b>. The lab
/// showed endurance is set almost entirely by the residual relative velocity when the shuttle launches,
/// which is why <see cref="Check"/> is a MATCH gate and not a tank gate.</para>
///
/// <para>Pure and deterministic — geometry in, verdict out. Determinism is law in Core.</para>
/// </summary>
public static class LoiterKeeping
{
    /// <summary>The leash the keeper defends, as a fraction of one shuttle hop. Holding the ship AT the
    /// edge of its own reach would be cruel — the away team's ride would be one drift from unreachable —
    /// so the working band is half of it, the same leash Lab 40 measured against.</summary>
    public const double HoldBandFraction = 0.5;

    /// <summary>The leash in metres: half a shuttle hop, 250,000 km.</summary>
    public const double HoldBandMeters = HoldBandFraction * ShuttleRange.RangeMeters;

    /// <summary>Relative speed at or under this (m/s) is a CLEAN hand-off — the lab's 0-to-1 m/s rows,
    /// which hold for weeks or never break at all. FLAGGED for tuning.</summary>
    public const double MatchedRelativeSpeedMps = 1.0;

    /// <summary>Relative speed at or under this is LOOSE but workable: the lab's 100 m/s case still bought
    /// a week everywhere, so it is a warning rather than a refusal. Above it, the ship is not station
    /// keeping — it is passing by. FLAGGED for tuning.</summary>
    public const double LooseRelativeSpeedMps = 100.0;

    /// <summary>How well the ship is sitting with the wreck at the moment the shuttle would launch.</summary>
    public enum MatchQuality
    {
        /// <summary>Sitting still relative to her. The hold will keep indefinitely.</summary>
        Matched,

        /// <summary>Close enough to work, but the leash will be doing real work. Say so and let the captain
        /// decide — the lab says even this buys about a week.</summary>
        Loose,

        /// <summary>Not station keeping at all — the ship is passing the wreck. Launching an away team into
        /// this is how you strand them, so the mode refuses.</summary>
        PassingBy,
    }

    /// <summary>Classify the hand-off from the relative speed alone. This is the mode's real precondition:
    /// Lab 40 found endurance is set by the match, not by the tank.</summary>
    public static MatchQuality Classify(double relativeSpeedMps)
    {
        double v = System.Math.Abs(relativeSpeedMps);
        if (v <= MatchedRelativeSpeedMps)
        {
            return MatchQuality.Matched;
        }
        return v <= LooseRelativeSpeedMps ? MatchQuality.Loose : MatchQuality.PassingBy;
    }

    /// <summary>Whether the away team may be launched at all. The one hard gate — everything else is
    /// advice.</summary>
    public static bool CanHold(MatchQuality quality) => quality != MatchQuality.PassingBy;

    /// <summary>The Δv the ship must still spend to turn a passing-by approach into a hold: exactly the
    /// relative velocity, because a hold IS matching her. Priced through the same
    /// <see cref="OrbitRule.PulsesFor"/> kernel every other burn in the game uses.</summary>
    public static int PulsesToMatch(Vector2d shipVelocity, Vector2d wreckVelocity, double shipSpeedMps) =>
        OrbitRule.PulsesFor((wreckVelocity - shipVelocity).Length, shipSpeedMps);

    /// <summary>
    /// The state the ship should hold: the wreck's own trajectory, displaced ALONG THE TRACK by
    /// <paramref name="offsetMeters"/>. Same speed, same distance from whatever it is orbiting, so the two
    /// share a period and never phase apart — the co-orbital hold Lab 40 measured at zero cost.
    ///
    /// <para>The displacement is taken perpendicular to the wreck's velocity's own perpendicular — i.e.
    /// along its direction of travel — which is what keeps the radius identical. Displacing RADIALLY (the
    /// naive "park beside it") is the expensive mistake this whole kernel exists to avoid.</para>
    /// </summary>
    public static ShipState HoldStateFor(in ShipState wreck, double offsetMeters, double simTime)
    {
        Vector2d v = wreck.Velocity;
        Vector2d along = v.Length > 0 ? v.Normalized() : new Vector2d(1, 0);
        return new ShipState(wreck.Position + (along * offsetMeters), v, simTime);
    }

    /// <summary>Has the ship drifted off the leash and earned a trim? The owner's <i>"approximate"</i> is
    /// exactly this dead band: correct only when it leaves, never chase the exact point.</summary>
    public static bool NeedsTrim(double gapMeters, double bandMeters = HoldBandMeters) =>
        gapMeters > bandMeters;

    /// <summary>How unforgiving the neighbourhood is if the hold ever DOES break: the local orbital angular
    /// rate √(μ/r³). High rate means a small error phases into a big one fast — that is the honest reason
    /// a wreck deep inside a giant's well is a harder place to wait, even though the hold itself is free.
    /// Returns 0 for deep space (no parent to orbit), where nothing phases at all.</summary>
    public static double ErrorGrowthRate(double parentMu, double orbitRadiusMeters) =>
        parentMu <= 0 || orbitRadiusMeters <= 0
            ? 0.0
            : System.Math.Sqrt(parentMu / (orbitRadiusMeters * orbitRadiusMeters * orbitRadiusMeters));

    /// <summary>Above this angular rate (rad/s) the site is called out as a demanding place to wait — a
    /// small slip becomes a long chase. Set just under Lab 40's "close over Jupiter (3 Rj)" case, the one
    /// regime where the lab saw a hold fail. FLAGGED for tuning.</summary>
    public const double DemandingGrowthRate = 1.0e-4;

    /// <summary>Is this a demanding place to leave the ship waiting? Not a refusal — a warning worth
    /// printing, and the reason a wreck put here is a more interesting wreck.</summary>
    public static bool IsDemanding(double parentMu, double orbitRadiusMeters) =>
        ErrorGrowthRate(parentMu, orbitRadiusMeters) > DemandingGrowthRate;

    /// <summary>What the captain is told at arm time. The ordinary case genuinely reads FREE — Lab 40
    /// measured zero pulses per day for a co-orbital hold in every regime — which is a better promise than
    /// the orbit keeper's <c>trim ≈27 p/day</c> (#193), and an earned one.</summary>
    public readonly record struct Quote(bool CanHold, MatchQuality Match, bool Demanding, string Line);

    /// <summary>Build the arm-time quote for holding on a wreck.</summary>
    public static Quote Assess(double relativeSpeedMps, double parentMu, double orbitRadiusMeters)
    {
        MatchQuality q = Classify(relativeSpeedMps);
        bool demanding = IsDemanding(parentMu, orbitRadiusMeters);

        if (q == MatchQuality.PassingBy)
        {
            return new Quote(false, q, demanding,
                $"Not holding — {relativeSpeedMps:N0} m/s of drift on her. Match her first, or the away team " +
                "comes back to empty sky.");
        }

        string core = q == MatchQuality.Matched
            ? "Holding — free. She keeps our orbit; the ship will be here when you get back."
            : $"Holding — free, but loose: {relativeSpeedMps:N0} m/s on her. It will keep for days, not weeks.";

        return new Quote(true, q, demanding,
            demanding ? core + " Deep in this well, mind — a slip here turns into a long chase." : core);
    }
}
