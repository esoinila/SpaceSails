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
    /// <summary>How close a Reever must get to lay hands on the digger: the two BODIES touching, in deck
    /// units — both are <c>DeckPlan.AvatarRadius</c> (0.7), so contact is their sum.
    ///
    /// <para>#469 (owner, 2026-07-27: "check all reever collisions… the radius must be used in every single
    /// one"). This was 1.2 — LESS than the sum of the radii — so a "catch" required the two bodies to
    /// visibly OVERLAP, while a melee swing (<see cref="CaptainCondition.TouchDistance"/>) landed at a true
    /// touch. Two different contact laws for the same event, and the tighter one needed a body to be inside
    /// another to fire. One number now: touching is touching.</para></summary>
    public const double CatchRadius = 1.4;

    /// <summary>Advance one Reever one step toward the digger, then hold it to its side of the
    /// crew-only door. It moves <paramref name="stepDistance"/> deck-units toward (<paramref name="avatarX"/>,
    /// <paramref name="avatarY"/>), then its Y is clamped to <paramref name="barrierY"/> so it can never
    /// climb the tube into the ship — the door won't open to it. Returns the Reever's new position.</summary>
    public static (double X, double Y) Step(
        double reeverX, double reeverY, double avatarX, double avatarY, double stepDistance, double barrierY) =>
        Step(reeverX, reeverY, avatarX, avatarY, stepDistance, barrierY, walls: null, radius: 0);

    /// <summary>A step this much shorter than the one asked for counts as spent — the hunter is flat
    /// against stone and the run must find another way round.</summary>
    private const double StallFraction = 0.25;

    /// <summary>PR-324 · The wall-obeying chase. As the plain <see cref="Step(double,double,double,double,double,double)"/>,
    /// but the step is a <see cref="SurfaceCollision.Slide"/> against <paramref name="walls"/> at the given
    /// <paramref name="radius"/> — the SAME bump-and-slide the captain's own boots make — so a Reever
    /// stops at a maze wall and grazes along it instead of clipping through.
    ///
    /// <para>#324 follow-up (owner, live 2026-07-26: "the reevers stopped progressing towards player as if
    /// there was a wall between… a clear bug"). A run driven STRAIGHT at a wall spends its whole step on the
    /// blocked axis, so the slide alone left the hunter pinned there for good and the pursuit died at the
    /// stone. The maze is meant to cost them the long way round, not end the chase. So when the direct step
    /// is spent, the Reever shambles ALONG the wall — the crude try-perpendicular the issue asks for ("the
    /// grid's own idiom; pathfinding libraries are not wanted"). <paramref name="wallSide"/> is this
    /// contact's fixed handedness (≥ 0 = left of the run, &lt; 0 = right): a STABLE side, so it rounds the
    /// corner instead of dithering at the face, and a pack of mixed hands flows around a slab from both
    /// ends. Only when both hands are walled too does it truly hold — then it is stationary and drops off
    /// the motion tracker (the motion-only law still composes the dread, now for a genuinely boxed-in
    /// hunter). The crew-only barrier still caps Y.</para></summary>
    public static (double X, double Y) Step(
        double reeverX, double reeverY, double avatarX, double avatarY, double stepDistance, double barrierY,
        IReadOnlyList<SurfaceCollision.Segment>? walls, double radius, int wallSide = 1)
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

            // The direct run is spent on the stone — try the wall itself as a handrail, preferred hand
            // first, then the other. Whichever moves, it takes; if neither does, it is boxed in and holds.
            if (dist > 1e-9 && stepDistance > 0 && Spent(reeverX, reeverY, nx, ny, stepDistance))
            {
                double hand = wallSide < 0 ? -1 : 1;
                // The perpendicular to the run, on this contact's side (rotate the unit heading a quarter turn).
                double perpX = -dy / dist * stepDistance * hand;
                double perpY = dx / dist * stepDistance * hand;

                (double ax, double ay) = SurfaceCollision.Slide(reeverX, reeverY, perpX, perpY, radius, walls);
                if (!Spent(reeverX, reeverY, ax, ay, stepDistance))
                {
                    (nx, ny) = (ax, ay);
                }
                else
                {
                    (double bx, double by) = SurfaceCollision.Slide(reeverX, reeverY, -perpX, -perpY, radius, walls);
                    if (!Spent(reeverX, reeverY, bx, by, stepDistance))
                    {
                        (nx, ny) = (bx, by);
                    }
                }
            }
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

    /// <summary>Did this move buy the hunter anything? False once the step has been ground away by a wall
    /// to a fraction of what was asked — the cue to try the wall as a handrail instead.</summary>
    private static bool Spent(double fromX, double fromY, double toX, double toY, double stepDistance)
    {
        double mx = toX - fromX, my = toY - fromY;
        double floor = stepDistance * StallFraction;
        return (mx * mx) + (my * my) < floor * floor;
    }

    /// <summary>True when a Reever is close enough to catch the digger. Only meaningful while the digger
    /// is still out on the surface — once they are up the tube past the door, the barrier in
    /// <see cref="Step"/> keeps every Reever out of reach by construction.</summary>
    public static bool Caught(double reeverX, double reeverY, double avatarX, double avatarY) =>
        (avatarX - reeverX) * (avatarX - reeverX) + (avatarY - reeverY) * (avatarY - reeverY)
            <= CatchRadius * CatchRadius;
}
