namespace SpaceSails.Core;

/// <summary>
/// The seeded WHERE for the two KAAMOS fragments that need a deterministic "is it here?" — the cold
/// supply pod half-buried in an outer moon's regolith (<c>cold-pod</c>), and the rare berth-holder who
/// drinks at a bar some watches (<c>holders-tell</c>). Pure and seedable (issue #411's delivery lane), so
/// the client's surface/bar sites stay thin and a test can pin an exact find. No RNG, no wall clock — the
/// same square/watch always answers the same, so a captain who sweeps the ground methodically will find
/// the pod, and the holder "appears rarely" yet stays put while present.
/// </summary>
public static class KaamosFind
{
    /// <summary>The outer icy moons — the failed cargo run's waypoints on the long haul out to the ice
    /// moon. A cold KAAMOS supply pod can only surface in the regolith of one of these; a probe on any
    /// other body (Earth's dust, a hot inner rock) never turns one up. Enceladus itself is on the list for
    /// completeness, though it stays unreachable until the arc is earned.</summary>
    public static readonly IReadOnlyList<string> ColdPodBodies =
        ["europa", "ganymede", "callisto", "titan", "enceladus", "miranda", "triton"];

    /// <summary>Rarity of the pod: roughly one seeded beach-comber square in this many, on a cold-pod
    /// body, hides it. Keyed on (body, square) — not on the probe attempt — so the same square always
    /// answers the same and a methodical sweep is rewarded, not a slot-machine re-roll.</summary>
    public const int ColdPodOneInSquares = 17;

    /// <summary>True if THIS beach-comber square, on THIS body, is the one hiding the cold KAAMOS supply
    /// pod. Deterministic. The client checks it on a probe and, on the first hit, assembles <c>cold-pod</c>.
    ///
    /// <para><paramref name="forced"/> is the <c>/map?kaamos=pod</c> dev seat (2026-08-02 story pass): the
    /// pod is under whatever ground you are standing on, so fragment 2 can be EARNED with a shovel instead
    /// of granted by <c>?kaamos=N</c>. It is the only fragment with no direct quick start — one seeded square
    /// in seventeen, on seven bodies, is a scene nobody can reach on demand, and "a scene nobody can reach on
    /// demand is a scene that ships broken". The forced answer deliberately ignores the body list too: the
    /// point is to test the FIND, and a tester who has landed somewhere warm should not have to fly.</para></summary>
    public static bool IsColdPodSquare(string bodyId, int squareX, int squareY, bool forced = false)
    {
        if (forced)
        {
            return true;
        }

        if (string.IsNullOrEmpty(bodyId) || !ColdPodBodies.Contains(bodyId))
        {
            return false;
        }

        ulong h = DiceRule.Seed($"kaamos:coldpod:{bodyId}", squareX, squareY);
        return h % (ulong)ColdPodOneInSquares == 0;
    }

    /// <summary>Rarity of the holder: on roughly one bar-watch in this many, the KAAMOS berth-holder is
    /// drinking in the room. Keyed on (bar, watch-day) so the holder is stable across a single watch
    /// (asking twice the same day answers the same) yet only shows up now and then.</summary>
    public const int HolderOneInWatches = 4;

    /// <summary>True if the rare KAAMOS berth-holder is drinking at THIS bar on THIS watch-day (sim-day).
    /// Deterministic per (bar, day). The client offers the "ask about KAAMOS" seam when this holds and the
    /// <c>holders-tell</c> shard is not yet in hand.
    ///
    /// <para><paramref name="forced"/> is the <c>/map?kaamos=holder</c> dev seat (2026-08-02 story pass): the
    /// berth-holder is drinking at THIS bar, this watch, whichever bar you docked at. One watch in four, at
    /// whichever bar you happen to walk into, is not a scene anyone can open on purpose — and the tell is the
    /// arc's best-written beat, so it was also the one hardest to look at.</para></summary>
    public static bool HolderAtBar(string bodyId, int watchDay, bool forced = false)
    {
        if (forced)
        {
            return true;
        }

        if (string.IsNullOrEmpty(bodyId))
        {
            return false;
        }

        ulong h = DiceRule.Seed($"kaamos:holder:{bodyId}", watchDay);
        return h % (ulong)HolderOneInWatches == 0;
    }

    /// <summary>What a round on the counter costs to buy the KAAMOS coordinate (the <c>bought-coordinate</c>
    /// shard). A modest, flat price — a tip bought, never an economy. Authored here so the fiction's number
    /// lives with the fiction.</summary>
    public const int BoughtCoordinateCredits = 1200;
}
