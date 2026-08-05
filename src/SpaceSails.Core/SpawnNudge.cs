namespace SpaceSails.Core;

/// <summary>
/// #681 · THE PAD CREW WAVE YOU CLEAR. Owner, stuck in a wall at the moment the world finished loading:
/// <i>"The second url put me into the wall... I cannot move."</i> — and, while stuck: <i>"Maybe some code to
/// move the character either side instead of spawning it so it cannot move?"</i>
///
/// <para>This is that code, and it is a NET rather than a fix. Every place the sim <b>places</b> the captain
/// — the landing, the car coming up, the tube mouth — hands its chosen square through here first; if the
/// square is inside collision, the captain is walked out to the nearest one that is not.</para>
///
/// <para><b>Three properties, and each of them is load-bearing.</b></para>
/// <list type="bullet">
/// <item><b>Deterministic.</b> A fixed ladder of rings and a fixed order of spokes, no dice and no clock, so
/// the same blocked spawn always produces the same rescue. A net that moved you somewhere else each boot
/// would make the bug unreproducible, which is worse than the bug.</item>
/// <item><b>Bounded.</b> <see cref="MaxDu"/> and no further. A spawn that needs eleven paces of searching is
/// not a tight fit, it is a site built wrong — and a net that stretches far enough will happily hide a whole
/// generator's worth of them.</item>
/// <item><b>It says so.</b> <see cref="ClearedLine"/> goes to the pulse. A nudge that happened quietly is a
/// bug that shipped quietly; the owner has to be able to see it in play and in a report.</item>
/// </list>
///
/// <para><b>And the audit still counts a nudge as a failure.</b> The guard ladder in
/// <c>TheLandingPutsYouSomewhereYouCanWALKTests</c> asserts the <i>un-nudged</i> square, because the moment
/// this quietly absorbs placer bugs the generator rots behind it — the same way the swept apron hid #574.
/// Net catches the captain; audit catches the bug.</para>
/// </summary>
public static class SpawnNudge
{
    /// <summary>How finely the spiral samples, in deck units. The SAME step the A* audits walk
    /// (<see cref="DeckReachability.DefaultStep"/>), so a square this rescue lands on is a square the
    /// reachability guard would have called a step — one lattice, not two.</summary>
    public const double Step = DeckReachability.DefaultStep;

    /// <summary>The furthest the crew will walk you. Past this the ground is broken rather than tight, and
    /// the caller must say so out loud instead of papering over it.</summary>
    public const double MaxDu = 6.0;

    /// <summary>How many bearings each ring tries. Sixteen is finer than any doorway in the game is narrow,
    /// and the order is fixed (east, then round) so the answer never depends on anything but the geometry.</summary>
    private const int Spokes = 16;

    /// <summary>Where the captain actually ends up. <paramref name="MovedDu"/> is 0 when the asked-for square
    /// was already clear; <paramref name="Failed"/> is the loud case — nothing standable within
    /// <see cref="MaxDu"/>, so the coordinates are handed back unchanged and the caller shouts.</summary>
    public readonly record struct Result(double X, double Y, double MovedDu, bool Failed)
    {
        /// <summary>True when the crew had to walk you somewhere. Worth a line in the pulse.</summary>
        public bool Moved => MovedDu > 0;
    }

    /// <summary>Take a square the sim wants to stand the captain on and return one they can actually stand
    /// on. Pure and deterministic in (point, radius, walls).</summary>
    public static Result Clear(
        double x, double y, double radius,
        IReadOnlyList<SurfaceCollision.Segment>? walls, double maxDu = MaxDu)
    {
        if (!SurfaceCollision.Blocked(x, y, radius, walls))
        {
            return new Result(x, y, 0, false);
        }

        // Outward ring by ring, so the FIRST clear square found is also the nearest one — which is what makes
        // the rescue read as "a step to the left" rather than as a teleport.
        for (double r = Step; r <= maxDu + 1e-9; r += Step)
        {
            for (int i = 0; i < Spokes; i++)
            {
                double a = i * System.Math.Tau / Spokes;
                double cx = x + (r * System.Math.Cos(a));
                double cy = y + (r * System.Math.Sin(a));
                if (!SurfaceCollision.Blocked(cx, cy, radius, walls))
                {
                    return new Result(cx, cy, r, false);
                }
            }
        }

        return new Result(x, y, 0, true);
    }

    /// <summary>What the captain hears when the net catches them. The owner's own image — <i>"the pad crew
    /// waved you clear of the wall"</i> — and deliberately unbothered: the people who work a pad have done
    /// this for everybody who ever set down here and would not think to write it down.</summary>
    public static string ClearedLine(double movedDu) =>
        $"🧤 Somebody in a scuffed hardsuit takes your elbow and walks you {Paces(movedDu)} off the wall they "
        + "set you down against. They do not say anything about it. Nobody who works a pad ever does.";

    /// <summary>What it says when the ground is beyond waving anybody clear of. Loud on purpose: a site with
    /// no standing room at its own spawn is broken, and a captain who is told "you are fine" by a game that
    /// has just failed to place them will spend the next ten minutes believing the rest of it too.</summary>
    public static string BrokenGroundLine(string where) =>
        $"⚠ {where} — and there is not one square you could stand on within {MaxDu:F0} paces of it. This "
        + "ground is built wrong. Take nothing else it tells you on trust.";

    private static string Paces(double du) => du switch
    {
        < 0.75 => "half a pace",
        < 1.25 => "a pace",
        < 1.75 => "a pace and a half",
        _ => $"{du:F0} paces",
    };
}
