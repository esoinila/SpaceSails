namespace SpaceSails.Core;

/// <summary>
/// WHERE THE AWAY TEAM GETS IN, AND WHAT THEY WALK THROUGH — the spawn (deliberately AT a doorway, so
/// the first compartment is one step away), the compartment table, the doorway centres, the shuttle lock,
/// the lifeboat cradles, and the playable bounds an audit sweeps.
///
/// <para>The family's one static field lives here: <c>Compartments</c>, a literal table read by the
/// doorway centres and by <c>CompartmentAt</c>.</para>
///
/// <para>Split out of <c>WreckLayout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class WreckLayout
{
    /// <summary>Where the shuttle puts the away team down — just inside the wreck's airlock, on the spine.
    /// Deliberately AT a doorway, so the first compartment is one step away.</summary>
    public const double SpawnX = 18.0;

    /// <summary>Spawn Y — on the spine.</summary>
    public const double SpawnY = 0.0;

    /// <summary>Half-width of a doorway through the spine wall. The first cut used 1.0 and the wreck was
    /// unwalkable: a 2 du gap minus the avatar's 1.4 du diameter leaves a 0.6 du slot nobody can find.</summary>
    public const float DoorHalfWidth = 3.0f;

    /// <summary>The compartments, bow to aft. Bounds are CONTIGUOUS on purpose — one ends exactly where the
    /// next begins. Leaving gaps between them created 1 du dead slots, walled both sides and narrower than
    /// the captain: traps with no way in that existed only to go wrong.</summary>
    /// <para>The aft-most rooms run all the way to the transom and the bow-most stop where the hull starts
    /// tapering — otherwise each end leaves a strip of ship walled off from everything, which is the same
    /// dead-slot mistake as the gaps, just at the ends where it is easier to miss.</para>
    public static readonly (string Name, float X0, float X1, bool Top)[] Compartments =
    [
        ("BRIDGE", 13f, BowX - 6, true),
        ("CREW SPACES", 0f, 13f, true),
        // The bottom row had nothing at the bow — a strip of ship with no name, reachable but belonging to
        // nothing. The audit spotted the asymmetry; she gets a room instead of a remainder.
        ("FORWARD LOCKER", 13f, BowX - 6, false),
        ("LIFEBOAT CRADLES", 0f, 13f, false),
        ("DEEP HOLD", -15f, 0f, true),
        ("NEAR HOLD", -15f, 0f, false),
        ("ENGINEERING", AftX, -15f, true),
        ("REACTOR SPACES", AftX, -15f, false),
    ];

    /// <summary>Where the spine opens into each compartment. ONE list, read by the wall builder (which
    /// leaves the gaps) AND the door builder (which draws them), so a doorway can never be cut somewhere
    /// the player is not shown — a gap nobody can see is the same as no gap at all.
    ///
    /// <para>Ascending, because the wall walk runs aft-to-bow and consumes them in order.</para></summary>
    public static float[] DoorCentres()
    {
        float[] centres = [-24f, -7f, 7f, (float)SpawnX];
        System.Array.Sort(centres); // order-proof: never trust the literal's order
        return centres;
    }

    /// <summary>
    /// THE AWAY TEAM'S OWN LOCK, across the spine between the wreck and the shuttle. Owner: <i>"Let's keep
    /// the shuttle door locked in such a way that we don't vent our own shuttle by accident. Also we don't
    /// want any uninvited infestations going there."</i>
    ///
    /// <para>Two jobs, one bulkhead. It is the boundary the ship's atmosphere stops at — crack every valve
    /// on this hull and the shuttle never notices — and it is a CREW-ONLY door, the same rule the ship's own
    /// tube runs on: the away team work it, and nothing else aboard can. The pack has never operated a
    /// hatch and is not going to start.</para>
    ///
    /// <para>It is deliberately AFT of <see cref="ShuttleStation"/> and FORWARD of <see cref="SpawnX"/>, so
    /// the team lands inside the ship having already come through it.</para>
    /// </summary>
    public const float ShuttleLockX = 21f;

    /// <summary>
    /// THE CREW-ONLY RULE, as a function rather than a line buried in the walk loop. Given where something
    /// that is not the away team wants to be, return where it is actually allowed to be.
    ///
    /// <para>The lock bulkhead has a passage cut in it — it has to, or the captain could not get home — so
    /// walls alone would let the pack walk it exactly the way the captain does. What stops them is the same
    /// rule the ship's own tube runs on: a hatch keyed to the crew. It can reach the door. It cannot open
    /// the door.</para>
    ///
    /// <para>This lives in Core so the invariant is PINNED BY A TEST instead of by a comment. "Nothing
    /// uninvited reaches the shuttle" is the kind of promise that is quietly broken by a refactor three
    /// months from now, and the owner would find out by watching something follow him home.</para>
    /// </summary>
    public static double HeldAtLock(double x, double radius) =>
        System.Math.Min(x, ShuttleLockX - radius);

    /// <summary>Whether this position is on the shuttle's side of the lock — where only the away team
    /// ever gets to stand.</summary>
    public static bool PastTheLock(double x, double radius) => x > ShuttleLockX - radius;

    /// <summary>Half-height of the gap through the lock bulkhead. Three units of passage — wider than the
    /// captain with room to walk it badly, which is the bar <c>WreckLayoutTests</c> holds every doorway to.</summary>
    public const float ShuttleLockGapHalf = 1.5f;

    /// <summary>
    /// THE LIFEBOAT CRADLES, on the outboard wall where a ship actually keeps them. Owner, on walking into
    /// the compartment named for them and finding an empty box: <i>"Are the lifeboats there or not … we
    /// should somehow see this like slots that are filled or empty … on the wall."</i>
    ///
    /// <para>Right, and it is the cheapest evidence in the game: a row of cradles you can COUNT from the
    /// doorway. No console to read, no die to roll — how many are empty is a fact about the room, and what
    /// it means is the captain's problem. It is also the seam the safety-card lane
    /// (<c>docs/features/safety-card.md</c>) was filed against.</para>
    /// </summary>
    public const int CradleCount = 6;

    /// <summary>Where each cradle sits: evenly along the LIFEBOAT CRADLES compartment's outboard wall.</summary>
    public static IEnumerable<(float X, float Y)> CradleSpots()
    {
        (string _, float x0, float x1, bool _) = System.Array.Find(
            Compartments, c => c.Name == LifeboatCompartment);

        float span = x1 - x0;
        for (int i = 0; i < CradleCount; i++)
        {
            // Inset half a step at each end so the row reads as spaced along the wall rather than
            // running into the bulkheads.
            float t = (i + 0.5f) / CradleCount;
            yield return (x0 + (span * t), BottomY - 1.2f);
        }
    }

    /// <summary>The compartment the cradles are in.</summary>
    public const string LifeboatCompartment = "LIFEBOAT CRADLES";

    /// <summary>The playable bounds an audit sweeps — the hull with a margin.</summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) Bounds =>
        (AftX - 2, TopY - 2, BowX + 2, BottomY + 2);
}
