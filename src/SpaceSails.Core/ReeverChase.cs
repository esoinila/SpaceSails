namespace SpaceSails.Core;

/// <summary>
/// PR-295 · How a Reever runs you down on the surface — and where it STOPS. Reevers are watchdogs,
/// not orbital mechanics: like the heat-hunters they close at a fixed pace with no cleverness, because
/// the whole play is a sprint back to the shuttle. The one law that matters lives here and is pure so a
/// test can pin it: <b>the shuttle door opens to crew only</b> (owner addendum, 2026-07-18: "The
/// shuttle doors would only open to our crew by default so the Reevers could not follow us to the
/// ship. :-D"). A Reever can chase you across the regolith to the very threshold, but it cannot cross
/// it — the chase ends by fiction (a door that won't open to them), never by a magic despawn.
///
/// <para>Deck units, matching <c>DeckPlan</c>. The surface hangs BELOW the ship's bottom hull, so the
/// safe side is toward the ship (greater Y) and the Reevers are penned on the surface side
/// (Y ≤ <c>barrierY</c>, the tube mouth). Movement is real-time client cosmetic (never saved, like any
/// NPC position); this rule is the deterministic core the client steps each frame and the bench pins.</para>
/// </summary>
public static class ReeverChase
{
    /// <summary>How close a Reever must get to lay hands on the digger (deck units).</summary>
    public const double CatchRadius = 1.2;

    /// <summary>Advance one Reever one step toward the digger, then hold it to its side of the
    /// crew-only door. It moves <paramref name="stepDistance"/> deck-units toward (<paramref name="avatarX"/>,
    /// <paramref name="avatarY"/>), then its Y is clamped to <paramref name="barrierY"/> so it can never
    /// climb the tube into the ship — the door won't open to it. Returns the Reever's new position.</summary>
    public static (double X, double Y) Step(
        double reeverX, double reeverY, double avatarX, double avatarY, double stepDistance, double barrierY) =>
        Step(reeverX, reeverY, avatarX, avatarY, stepDistance, barrierY, walls: null, radius: 0);

    /// <summary>PR-324 · The wall-obeying chase. As the plain <see cref="Step(double,double,double,double,double,double)"/>,
    /// but the step is a <see cref="SurfaceCollision.Slide"/> against <paramref name="walls"/> at the given
    /// <paramref name="radius"/> — the SAME bump-and-slide the captain's own boots make — so a Reever
    /// stops at a maze wall and grazes along it instead of clipping through. A Reever pinned flat against
    /// a wall makes no progress, which means it is momentarily STATIONARY and drops off the motion
    /// tracker for free (the motion-only law composes the dread). The crew-only barrier still caps Y.</summary>
    public static (double X, double Y) Step(
        double reeverX, double reeverY, double avatarX, double avatarY, double stepDistance, double barrierY,
        IReadOnlyList<SurfaceCollision.Segment>? walls, double radius)
    {
        double dx = avatarX - reeverX;
        double dy = avatarY - reeverY;
        double dist = System.Math.Sqrt((dx * dx) + (dy * dy));
        double moveX = 0, moveY = 0;
        if (dist > 1e-9 && stepDistance > 0)
        {
            moveX = dx / dist * stepDistance;
            moveY = dy / dist * stepDistance;
        }

        double nx, ny;
        if (walls is { Count: > 0 })
        {
            (nx, ny) = SurfaceCollision.Slide(reeverX, reeverY, moveX, moveY, radius, walls);
        }
        else
        {
            nx = reeverX + moveX;
            ny = reeverY + moveY;
        }

        // The crew-only threshold: a Reever is penned on the surface side and cannot follow past it.
        if (ny > barrierY)
        {
            ny = barrierY;
        }

        return (nx, ny);
    }

    /// <summary>True when a Reever is close enough to catch the digger. Only meaningful while the digger
    /// is still out on the surface — once they are up the tube past the door, the barrier in
    /// <see cref="Step"/> keeps every Reever out of reach by construction.</summary>
    public static bool Caught(double reeverX, double reeverY, double avatarX, double avatarY) =>
        (avatarX - reeverX) * (avatarX - reeverX) + (avatarY - reeverY) * (avatarY - reeverY)
            <= CatchRadius * CatchRadius;
}
