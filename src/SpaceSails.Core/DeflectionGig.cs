namespace SpaceSails.Core;

/// <summary>
/// #394 — THE ASTEROID DEFLECTION. The owner's backlog ruling (the cruise, 2026-07-19): <b>"One more
/// mission type … asteroid deflection Armageddon movie style. 🫡😎"</b> and the target ruling
/// (2026-07-20): <b>"the asteroid must NEVER threaten Earth … canonical pick: Ringside Exchange, the He3
/// clearing-house."</b> A rare, LOUD emergency gig: a rock is inbound toward the Ringside Exchange, the
/// deflection WINDOW is the mission clock (the #370 away-window idiom, but the stakes are a port), the
/// crew lands ON the rock and drills a charge (the #393 door-force channel, longer — this is THE
/// drilling), and a completed burn ABLATES the rock enough to lift its rail off the station's orbit — the
/// Kepler rails make the deflection SHOWABLE on the nav map (the money shot). Homage, never reproduction:
/// our crew, our rock, no film likenesses.
///
/// <para>Owner addendum (2026-07-20, on Zubrin's asteroid taxonomy): <b>"the type would definitely be a
/// factor also 😎."</b> Every rock is a seeded <see cref="RockType"/> — the classic C/S/M composition —
/// and it factors HONESTLY: a carbonaceous C-type is soft to drill and ablates eagerly (volatiles flash to
/// gas and shove); a metallic M-type is brutal to bore and resists ablation ("bring patience and a bigger
/// charge"); a stony S-type is the firm textbook rock in between. One constants table, pinned by tests,
/// named on the mission card. (The rubble-pile structure axis was dropped 2026-07-20 — owner: "Rubble
/// piles is a sidequest to game… let's just not have those.")</para>
///
/// <para>This is the pure, deterministic Core spine (repo law §9 — determinism is law in Core): the rock's
/// colliding <see cref="RockRail"/> (a Kepler ellipse whose periapsis kisses the station's orbit at the
/// impact point), the honest closest-approach MISS math, the deflection that raises periapsis (miss ≈
/// raise), the drill CHANNEL lengths per type, the diced on-site COMPLICATIONS, the ablation + rotation
/// impulse model, the success BANDS, and the heroic PAYOUT. Every roll is seeded from sim state the caller
/// folds in, so client and any replay agree.</para>
/// </summary>
public static partial class DeflectionGig
{
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE RAIL — a Kepler ellipse whose periapsis kisses the target's circular orbit at the impact
    //  point, timed so the rock reaches that periapsis exactly at T-impact (a genuine collision). The
    //  rock rides real orbit rails so the nav map can DRAW the collision course; the deflection raises
    //  periapsis so the drawn rail visibly lifts off the station's orbit.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The eccentricity of the inbound rock's rail — mild enough that the rock stays close to the
    /// station's orbit (landable, and the crossing reads on the map) yet visibly elliptical. OWNER-TUNABLE.</summary>
    public const double RailEccentricity = 0.2;

    /// <summary>The rock's orbital period as a multiple of the target's — off-resonance so the rock sweeps
    /// through the station's lane rather than pacing it, and tuned (with the window) so the rock spawns
    /// comfortably inside one shuttle hop of a ship docked at the target. OWNER-TUNABLE.</summary>
    public const double RailPeriodFactor = 1.5;

    /// <summary>The default lead time (sim seconds) from the moment the gig is struck to T-impact — sets
    /// where on its rail the rock spawns (≈1.6e8 m off the berth for the canonical Ringside geometry, well
    /// inside a shuttle hop). The live doom clock is a real-seconds budget (<see cref="ImpactBudgetSeconds"/>);
    /// this only fixes the drawn approach geometry. OWNER-TUNABLE.</summary>
    public const double RailLeadSeconds = 3.0e5;

    /// <summary>A plausible little-rock radius (m) — same scale as an expedition site so the shuttle board
    /// reads it as a landable surface, never "basically sitting on it".</summary>
    public const double RockBodyRadiusMeters = 4.0e6;

    /// <summary>The runtime body id family every deflection rock carries (one at a time, like the expedition
    /// site). The concrete id appends the composition code so the surface + tests can route by id alone.</summary>
    public const string BodyId = "deflection-rock";

    /// <summary>One rock's Kepler rail (parent-relative): semi-major axis, eccentricity, argument of
    /// periapsis (radians from +X), orbital period, and mean anomaly at epoch. Feeds straight into the
    /// same elliptical formula <see cref="CircularOrbitEphemeris"/> flies every eccentric body on.</summary>
    public readonly record struct RockRail(
        double SemiMajorAxis, double Eccentricity, double ArgPeriapsis, double OrbitPeriod, double InitialPhase)
    {
        /// <summary>Periapsis distance a(1−e) — how close the rail comes to the parent. Pre-deflection this
        /// equals the target's orbit radius (the kiss); a deflection raises it.</summary>
        public double PeriapsisMeters => SemiMajorAxis * (1.0 - Eccentricity);
    }

    /// <summary>Build the inbound COLLISION rail for a rock aimed at a target on a circular orbit of
    /// <paramref name="targetRadius"/> (period <paramref name="targetPeriod"/>, initial phase
    /// <paramref name="targetPhase"/>) around a shared parent, arriving at <paramref name="impactSimTime"/>.
    /// Periapsis points at where the target will be at impact and sits exactly on its orbit (the kiss), and
    /// the rock reaches periapsis at impact — so undeflected, it collides. Pure.</summary>
    public static RockRail BuildRail(double targetRadius, double targetPeriod, double targetPhase, double impactSimTime)
    {
        double thetaImpact = targetPhase + System.Math.Tau * impactSimTime / targetPeriod;
        double period = targetPeriod * RailPeriodFactor;
        double q = targetRadius;                          // periapsis kisses the target's orbit
        double a = q / (1.0 - RailEccentricity);
        double m0 = -System.Math.Tau * impactSimTime / period; // mean anomaly 0 (periapsis) at impact
        return new RockRail(a, RailEccentricity, thetaImpact, period, m0);
    }

    /// <summary>The same rail with periapsis raised by <paramref name="raiseMeters"/> — the deflection: the
    /// ablation shove lifts the low point of the orbit, so the rail no longer kisses the station's lane. The
    /// argument of periapsis, period and epoch phase are unchanged, so the rock still reaches its (now
    /// higher) periapsis at the impact instant — the miss distance is then exactly the raise (see
    /// <see cref="MissDistanceMeters"/>). Pure.</summary>
    public static RockRail RaisePeriapsis(RockRail rail, double raiseMeters)
    {
        double q = (rail.SemiMajorAxis * (1.0 - rail.Eccentricity)) + System.Math.Max(0.0, raiseMeters);
        double a = q / (1.0 - rail.Eccentricity);
        return rail with { SemiMajorAxis = a };
    }

    /// <summary>The rock's parent-relative position on its rail at <paramref name="simTime"/> — the same
    /// perifocal-then-rotate elliptical solve <see cref="CircularOrbitEphemeris"/> uses, so the drawn rail
    /// and any ephemeris body agree bit-for-bit.</summary>
    public static Vector2d RockPosition(RockRail rail, double simTime)
    {
        double meanAnomaly = rail.InitialPhase + System.Math.Tau * simTime / rail.OrbitPeriod;
        double e = rail.Eccentricity;
        double bigE = CircularOrbitEphemeris.SolveEccentricAnomaly(meanAnomaly, e);
        double a = rail.SemiMajorAxis;
        double px = a * (System.Math.Cos(bigE) - e);
        double py = a * System.Math.Sqrt(1.0 - e * e) * System.Math.Sin(bigE);
        double cosW = System.Math.Cos(rail.ArgPeriapsis), sinW = System.Math.Sin(rail.ArgPeriapsis);
        return new Vector2d(cosW * px - sinW * py, sinW * px + cosW * py);
    }

    /// <summary>A target's parent-relative position on its circular orbit at <paramref name="simTime"/>.</summary>
    public static Vector2d TargetPosition(double targetRadius, double targetPeriod, double targetPhase, double simTime)
    {
        double angle = targetPhase + System.Math.Tau * simTime / targetPeriod;
        return new Vector2d(targetRadius * System.Math.Cos(angle), targetRadius * System.Math.Sin(angle));
    }

    /// <summary>The honest CLOSEST APPROACH (m) between the rock and the target over a window straddling
    /// <paramref name="impactSimTime"/> — the miss distance the success bands read. Both ride the SAME
    /// parent, so the parent's own motion cancels in the difference; we sample parent-relative positions.
    /// Undeflected this is ~0 (a hit); a periapsis raise of ΔR yields a miss of ΔR (pinned by tests).</summary>
    public static double MissDistanceMeters(
        RockRail rail, double targetRadius, double targetPeriod, double targetPhase, double impactSimTime)
    {
        // Sample a quarter of the rock's period each side of impact — the closest pass is right at periapsis.
        double span = 0.25 * rail.OrbitPeriod;
        const int steps = 240;
        double best = double.MaxValue;
        for (int i = -steps; i <= steps; i++)
        {
            double t = impactSimTime + (i * span / steps);
            double d = (RockPosition(rail, t) - TargetPosition(targetRadius, targetPeriod, targetPhase, t)).Length;
            if (d < best)
            {
                best = d;
            }
        }
        return best;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE ROCK TYPE — Zubrin's taxonomy, honestly costed (owner 2026-07-20). Composition sets how hard
    //  the rock is to drill and how eagerly it ablates: C soft-and-eager, S the firm middle, M brutal. One
    //  constants table. (The monolith-vs-rubble-pile structure axis was dropped 2026-07-20 — every rock is
    //  now a solid body — so composition alone costs the job.)
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The base drill-channel length (real seconds) to sink the charge into a stony rock — a
    /// touch under the whole excursion, and far longer than forcing a sealed door (#393, 5s): this is THE
    /// drilling. Scaled per composition by <see cref="RockProfile.DrillSeconds"/>. OWNER-TUNABLE.</summary>
    public const double DrillBaseSeconds = 14.0;

    /// <summary>The periapsis raise (m) a FULL charge delivers into a stony rock at perfect rotation
    /// alignment and 100% ablation efficiency — the ceiling the composition/alignment factors scale down.
    /// Tuned so even the worst rock (an M-type) can just clear the station on a flawless run, and a soft
    /// C-type clears it with wide margin. OWNER-TUNABLE.</summary>
    public const double MaxPeriapsisRaiseMeters = 4.5e7;

    /// <summary>Composition with its honestly-costed constants (owner 2026-07-20). Pure data.</summary>
    public static class RockProfile
    {
        // Composition drill hardness (× base): C soft, S firm, M brutal.
        public static double CompositionDrill(RockComposition c) => c switch
        {
            RockComposition.CType => 0.7,
            RockComposition.MType => 1.6,
            _ => 1.0, // S-type
        };

        // Composition ablation efficiency (× ceiling): volatiles-rich C ablates eagerly; M resists.
        public static double CompositionAblation(RockComposition c) => c switch
        {
            RockComposition.CType => 1.2,
            RockComposition.MType => 0.7,
            _ => 1.0, // S-type
        };

        /// <summary>Real seconds to drill the charge into a rock of <paramref name="type"/> — base × the
        /// composition hardness. C-type ≈ 9.8s; S-type = 14s; M-type ≈ 22.4s.</summary>
        public static double DrillSeconds(RockType type) =>
            DrillBaseSeconds * CompositionDrill(type.Composition);

        /// <summary>The ablation impulse efficiency [0..~1.2] of a rock of <paramref name="type"/> — the
        /// composition eagerness. C ≈ 1.2 (eager); S = 1.0; M = 0.7 (the stubborn worst).</summary>
        public static double AblationEfficiency(RockType type) =>
            CompositionAblation(type.Composition);
    }

    /// <summary>Seed a rock's composition from the gig seed — C/S/M drawn with the classic frequencies
    /// leaning stony (S most common, M rarest). Pure.</summary>
    public static RockType RollType(ulong seed)
    {
        int c = new DeterministicRandom(DiceRule.Seed(seed, "rock-comp")).NextInt(0, 100);
        RockComposition comp = c < 45 ? RockComposition.SType : c < 80 ? RockComposition.CType : RockComposition.MType;
        return new RockType(comp);
    }

    /// <summary>A house-voice name for the rock, seeded so it reads distinct per gig — "The Hammer",
    /// "Object KAAMOS-9", etc. The dread is in the plainness.</summary>
    public static string RockName(ulong seed)
    {
        string[] names = ["The Hammer", "The Widowmaker", "Object 2011-XR", "The Anvil", "Cold Lazarus", "The Bolide", "Object KAAMOS-9"];
        return names[new DeterministicRandom(DiceRule.Seed(seed, "rock-name")).NextInt(0, names.Length)];
    }

    // FNV-1a of the rock id → a stable long for the seed fold (matches AwayExpeditionEvents.HashId).
    private static long HashId(string id)
    {
        unchecked
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offsetBasis;
            foreach (char c in id ?? "")
            {
                hash ^= c;
                hash *= prime;
            }
            return (long)hash;
        }
    }
}

/// <summary>Zubrin's composition taxonomy (owner 2026-07-20). C carbonaceous (soft, volatile-rich), S
/// stony (the common firm rock), M metallic (brutal, resists ablation).</summary>
public enum RockComposition { CType, SType, MType }

/// <summary>A seeded rock's type — its C/S/M composition — with house-voice labels for the mission card
/// and the briefing. (The monolith-vs-rubble-pile structure axis was dropped 2026-07-20.)</summary>
public readonly record struct RockType(RockComposition Composition)
{
    /// <summary>The one-letter composition code (C/S/M) for a terse tag.</summary>
    public string Code => Composition switch
    {
        RockComposition.CType => "C",
        RockComposition.MType => "M",
        _ => "S",
    };

    /// <summary>The full house-voice label — "M-type metallic".</summary>
    public string Label => Composition switch
    {
        RockComposition.CType => "C-type carbonaceous",
        RockComposition.MType => "M-type metallic",
        _ => "S-type stony",
    };

    /// <summary>A one-line spectrometry read for the briefing, warning the captain what the type costs.</summary>
    public string BriefLine => Composition switch
    {
        RockComposition.MType =>
            "Spectrometry reads M-type metallic — dense, stubborn, brutal to drill and slow to ablate. Bring patience and a bigger charge.",
        RockComposition.CType =>
            "Spectrometry reads C-type carbonaceous — volatile-rich and soft. It drills quick and ablates eager; the charge will bite deep.",
        _ =>
            "Spectrometry reads S-type stony — a firm, honest rock. Drills clean, ablates fair. The textbook job, if there is one.",
    };
}

/// <summary>The band one on-site complication landed in.</summary>
public enum DeflectionBand
{
    /// <summary>The drill bit snapped — drill progress lost, re-channel from there.</summary>
    DrillSnap,

    /// <summary>A crew member's nerve broke and they bolted — retrieved, or lost (diced).</summary>
    CrewBolts,

    /// <summary>A tremor ran the rock — a nerve lump, no lasting harm.</summary>
    Tremor,

    /// <summary>A steady beat — the drilling continues, nothing stirs.</summary>
    Steady,

    /// <summary>The bore ran true — a little drill progress banked back.</summary>
    GoodBite,
}

/// <summary>One rolled complication: the dice event to SHOW, the band, a nerve lump to shock through, the
/// change to drill progress (negative = a snap set the bit back, positive = a good bite gained), and
/// whether a crew member was lost to the fall (docks the payout).</summary>
public readonly record struct DeflectionComplication(
    DiceEvent Event, DeflectionBand Band, double NerveHit, double DrillProgressDelta, bool CrewLost);

/// <summary>How the deflection resolved — the success band the payout and the storyboard read.</summary>
public enum DeflectionOutcome
{
    /// <summary>The rock cleared the station with margin — heroic. The 4-panel money shot.</summary>
    FullDeflection,

    /// <summary>A grazing miss — the rock scraped past; the port is bleeding but standing. Reduced pay.</summary>
    GrazingMiss,

    /// <summary>The rock hit (burn never fired / aborted) — Ringside takes heavy damage but SURVIVES as
    /// canon. Bounded consequences, honest narration; never destroyed.</summary>
    Impact,
}

/// <summary>How the doom clock reads.</summary>
public enum ImpactClock
{
    /// <summary>Counting down with time to work.</summary>
    Counting,

    /// <summary>Inside the critical margin — fire or evacuate NOW.</summary>
    LastCall,

    /// <summary>T-0 — the rock is at the station.</summary>
    Impact,
}
