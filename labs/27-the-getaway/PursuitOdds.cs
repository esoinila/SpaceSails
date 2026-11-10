namespace SpaceSails.Core;

/// <summary>
/// The wolves' honesty contract, as a Core table (Friday-second §3, Lab 27 "The getaway"). The
/// collectors chase with <see cref="EncounterRule"/>'s thrust-only law (owner's standing ruling: NO
/// gravity, NO autopilot) and mark a catch only inside <see cref="EncounterRule.CatchRadiusMeters"/>
/// under <see cref="EncounterRule.CatchRelativeSpeedMetersPerSecond"/>. Lab 27 flew that law across a
/// head-start × flee-speed grid and measured the three escapes (the sling, the skim heat-bleed, the
/// phasing juke); this class turns the measured envelope into a pure query the PR-BUSTED boarding
/// pop-up's RESIST/RUN dice draw modifiers from — so the odds the game quotes are the numbers the
/// physics earned, not rubber-banding.
///
/// <para><b>Data + a pure query only.</b> Nothing here wires gameplay: it classifies a geometry, and
/// returns the honest chance-class (and a dice modifier) of each escape from that geometry. The
/// BUSTED lane reads it; it never reaches back.</para>
///
/// <para><b>GROUNDED IN THE LAB.</b> The boundary constants are <see cref="EncounterRule"/>'s own
/// catch numbers (Lab 27 Section B found the stern-chase catch boundary IS essentially the catch
/// radius, tightening as the runner's speed climbs). The per-trick odds are the classification of
/// Lab 27's measured margins (Sections C/D/E). If the pursuit constants or the lab's margins change,
/// rerun the lab and re-verify this table (repo law: every number came from a probe that printed
/// it — Lab 27 Section F prints this very table).</para>
/// </summary>
public static class PursuitOdds
{
    /// <summary>Head start (initial separation) at or under which the thrust-only wolf can still make
    /// a clean, under-cap grab — Lab 27 Section B: the boundary is the catch radius itself (past it
    /// the wolf's closest pass is above the speed cap, so it roars through and cannot grab).</summary>
    public const double JawsHeadStartMeters = EncounterRule.CatchRadiusMeters;

    /// <summary>The relative speed above which a runner is never grabbed even at the boundary — the
    /// wolf arrives too hot (<see cref="EncounterRule.CatchRelativeSpeedMetersPerSecond"/>).</summary>
    public const double RunnerCatchCapMps = EncounterRule.CatchRelativeSpeedMetersPerSecond;

    /// <summary>How the (head start, relative speed) geometry reads against the flown envelope.</summary>
    public enum GeometryClass
    {
        /// <summary>Inside the catch radius and under the speed cap — a grab is earnable now.</summary>
        InItsJaws,

        /// <summary>At the catch radius but running hot (over the cap): the coin-flip band where the
        /// wolf's pass is fast — a nudge either way decides it.</summary>
        EvenChase,

        /// <summary>Past the catch radius, inside a couple of radii: the wolf is closing but every pass
        /// so far has been an overshoot. A stern chase you are winning.</summary>
        SternChase,

        /// <summary>Well past the catch radius: the wolf provably arrives too hot to grab. Open water.</summary>
        Clear,
    }

    /// <summary>The honest chance-class of an escape, coarse on purpose (the dice add the texture).</summary>
    public enum EscapeOdds
    {
        Forlorn,
        Slim,
        EvenMoney,
        Likely,
        Certain,
    }

    /// <summary>The player's move. <see cref="Run"/> is the baseline (just fly); the other three are
    /// Lab 27's measured escapes.</summary>
    public enum Trick
    {
        Run,
        Sling,
        Skim,
        PhasingJuke,
    }

    /// <summary>Classify a chase geometry against the flown envelope (Lab 27 Section B). Pure.</summary>
    public static GeometryClass Classify(double headStartMeters, double relativeSpeedMps)
    {
        if (headStartMeters <= JawsHeadStartMeters)
        {
            return relativeSpeedMps < RunnerCatchCapMps ? GeometryClass.InItsJaws : GeometryClass.EvenChase;
        }

        return headStartMeters <= 2 * JawsHeadStartMeters ? GeometryClass.SternChase : GeometryClass.Clear;
    }

    /// <summary>A one-line description of a geometry class (printed by the lab, shown by the pop-up).</summary>
    public static string Describe(GeometryClass g) => g switch
    {
        GeometryClass.InItsJaws => $"within R ({JawsHeadStartMeters / 1e3:N0} km) and under the {RunnerCatchCapMps:N0} m/s cap — a grab is earnable now",
        GeometryClass.EvenChase => $"at ~R but running hot (over {RunnerCatchCapMps:N0} m/s) — the coin-flip band",
        GeometryClass.SternChase => $"past R, inside ~2R — the wolf is closing but overshooting",
        GeometryClass.Clear => $"well past R — the thrust-only wolf arrives too hot to grab",
        _ => "unknown",
    };

    // The measured odds table (Lab 27 Sections C/D/E, 2026-07-17). Rows: Run, Sling, Skim, Juke.
    // Cols: InItsJaws, EvenChase, SternChase, Clear. Each cell is the classification of a flown
    // margin — see labs/27-the-getaway/README.md for the number behind every entry.
    private static readonly EscapeOdds[,] Table =
    {
        // Run — the envelope itself: hopeless in the jaws, a coin-flip while hot at the edge, yours once past R.
        { EscapeOdds.Forlorn, EscapeOdds.EvenMoney, EscapeOdds.Likely, EscapeOdds.Certain },
        // Sling — a flyby donates km/s of heliocentric velocity the wolf would need days of thrust to match:
        // decisive with any room, still a slim out even from the jaws (if a planet is in reach).
        { EscapeOdds.Slim, EscapeOdds.Likely, EscapeOdds.Certain, EscapeOdds.Certain },
        // Skim — an atmosphere pass sheds speed the dragless wolf keeps, forcing an overshoot: strong once
        // there is room to turn, even money from the jaws (needs a shell body in reach).
        { EscapeOdds.EvenMoney, EscapeOdds.Likely, EscapeOdds.Likely, EscapeOdds.Certain },
        // Phasing juke — staling the intercept grows with room and time; weak from the jaws (the wolf re-aims
        // to your live position), decisive with a lane and a head start.
        { EscapeOdds.Slim, EscapeOdds.EvenMoney, EscapeOdds.Likely, EscapeOdds.Certain },
    };

    /// <summary>The honest chance-class of <paramref name="trick"/> from a geometry class.</summary>
    public static EscapeOdds OddsFor(Trick trick, GeometryClass geometry) => Table[(int)trick, (int)geometry];

    /// <summary>The dice modifier the PR-BUSTED RESIST/RUN roll adds for this odds class — the ladder
    /// the boarding pop-up shows the captain ("+2, RUN favored"). Kept modest so a purchasable helper
    /// (owner §0: dice modifiers, never OP) still matters at the table.</summary>
    public static int ModifierFor(EscapeOdds odds) => odds switch
    {
        EscapeOdds.Forlorn => -3,
        EscapeOdds.Slim => -1,
        EscapeOdds.EvenMoney => 0,
        EscapeOdds.Likely => +2,
        EscapeOdds.Certain => +4,
        _ => 0,
    };

    /// <summary>The dice modifier for <paramref name="trick"/> from a geometry class (convenience:
    /// <see cref="ModifierFor"/> of <see cref="OddsFor"/>).</summary>
    public static int DiceModifier(Trick trick, GeometryClass geometry) => ModifierFor(OddsFor(trick, geometry));
}
