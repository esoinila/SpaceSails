namespace SpaceSails.Core;

/// <summary>
/// #462 · ONE DOOR AT A TIME. Owner, 2026-07-27: <i>"The idea is that only one door in a tube is open at a
/// time… think of airlock"</i> — <i>"both doors being open at the same time defeats the purpose (an
/// abstraction though at this case)."</i>
///
/// <para>The tube's two doors used to be independent auto-doors, each opening whenever the captain came
/// near. Standing at the threshold therefore held BOTH ends retracted — and since the captain is always
/// standing at the threshold when a chase arrives, the Old Ones piled up against a gap painted wide open.
/// That is the whole "the door that does not open for them is MISSING" report: the barrier existed in the
/// arithmetic and nowhere on the screen.</para>
///
/// <para>Interlocked, the far end is ALWAYS shut. Walking out, the ship-end closes behind you before the
/// surface end opens; coming home, the outer shuts as the inner gives — which is both the visible thing the
/// pack stops at, and the reason a tailgater ends up sealed in the tube with the built-in gun (#461) instead
/// of following you aboard.</para>
///
/// <para>Pure geometry so the law is pinned in CI rather than living only in the renderer — the client has
/// no test project, and this is exactly the kind of rule that rots silently (see #442).</para>
/// </summary>
public static class Airlock
{
    /// <summary>May this door stand open? It must be within <paramref name="openRadius"/> of the captain
    /// AND be the nearest door of its interlock group. <paramref name="nearestPartnerDistance"/> is the
    /// distance to the closest OTHER door sharing the group — <see cref="double.PositiveInfinity"/> when the
    /// door has no partner, which reduces to the plain proximity rule.
    ///
    /// <para>Ties go to SHUT: if the captain is exactly between the two ends, neither opens rather than
    /// both. An airlock that guesses wrong should fail closed.</para></summary>
    public static bool MayOpen(double distance, double nearestPartnerDistance, double openRadius)
    {
        if (double.IsNaN(distance) || distance > openRadius)
        {
            return false;
        }
        if (double.IsNaN(nearestPartnerDistance))
        {
            return true; // nothing credible to interlock against
        }
        return distance < nearestPartnerDistance;
    }
}
