namespace SpaceSails.Core;

/// <summary>
/// #441 · THE PACK KEEPS ITS ELBOWS OUT. Owner, live 2026-07-26: <i>"Reevers should not pile up"</i> …
/// <i>"multiple reevers on top of each other on same dot"</i> … <i>"reevers merging into a one blob… they
/// should not."</i>
///
/// <para>Nothing kept two Old Ones apart. Each was steered independently at the same aim point — and the
/// encirclement bias makes that target genuinely SHARED — so the pack converged to a point and stacked.
/// #435 made it constant rather than occasional: before, the first arrival pinned on a wall and the ones
/// behind pinned at different spots, which spaced them out by accident. Now they all round the same corner
/// along the same handrail and arrive in the same place. The fix for the stall exposed the crowding.</para>
///
/// <para>The blob is not a cosmetic bug. Four hunters that look like one invert the information the surface
/// is played on: you cannot count what is coming, the motion tracker shows fewer contacts than there are
/// bodies, and a pack occupying one square cannot actually cut an angle or surround anything — which is the
/// whole point of the cornering geometry (#324).</para>
///
/// <para>The remedy is a personal-space push, in the grid's own idiom — no flocking library, no steering
/// behaviours. Applied AFTER the chase step, so it never cancels forward progress and cannot deadlock a
/// queue at a corner back into the very stall #435 fixed. Deterministic: pairs resolve in index order, the
/// earlier index holds its ground, and a pair standing on exactly the same point separates along a fixed
/// per-index bearing rather than a random one.</para>
/// </summary>
public static class ReeverPack
{
    /// <summary>How much room an Old One insists on: a body's width (two <c>DeckPlan.AvatarRadius</c>).
    /// Close enough to still read as a pack closing on you, far enough that N bodies read as N dots.</summary>
    public const double PersonalSpace = 1.4;

    /// <summary>Relaxation passes per frame. Two settles the common cases (a pair, and a third arriving into
    /// a pair) without chasing perfection — the pack is single digits and the next frame relaxes again.</summary>
    private const int Passes = 2;

    /// <summary>Push a stepped pack apart so no two Old Ones share a dot.
    ///
    /// <para>Walks every pair in index order; when two are inside <see cref="PersonalSpace"/>, the LATER one
    /// gives way — the earlier index holds, which is what makes the outcome stable frame to frame instead of
    /// two bodies shoving each other back and forth. The give-way move goes through
    /// <see cref="SurfaceCollision.Slide"/> against <paramref name="walls"/>, so crowding can never squeeze a
    /// hunter into stone (the #324 law survives the shove). One that is boxed in simply stays where it is —
    /// overlapping for a frame is a far smaller lie than a Reever standing inside a wall.</para></summary>
    public static void KeepApart(
        System.Span<(double X, double Y)> pack,
        System.Collections.Generic.IReadOnlyList<SurfaceCollision.Segment>? walls,
        double radius)
    {
        if (pack.Length < 2)
        {
            return;
        }

        for (int pass = 0; pass < Passes; pass++)
        {
            for (int i = 0; i < pack.Length; i++)
            {
                for (int j = i + 1; j < pack.Length; j++)
                {
                    double dx = pack[j].X - pack[i].X;
                    double dy = pack[j].Y - pack[i].Y;
                    double distSq = (dx * dx) + (dy * dy);
                    if (distSq >= PersonalSpace * PersonalSpace)
                    {
                        continue; // already has its elbow room
                    }

                    double dist = System.Math.Sqrt(distSq);
                    double ux, uy;
                    if (dist > 1e-9)
                    {
                        ux = dx / dist;
                        uy = dy / dist;
                    }
                    else
                    {
                        // Exactly the same point — there is no "away" to read off the geometry, so fan them
                        // out on a fixed per-index bearing. Deterministic, and two coincident bodies leave in
                        // different directions instead of both picking the same one.
                        double bearing = j * 2.399963229728653; // the golden angle, in radians
                        ux = System.Math.Cos(bearing);
                        uy = System.Math.Sin(bearing);
                    }

                    double push = PersonalSpace - dist;
                    (double nx, double ny) = SurfaceCollision.Slide(
                        pack[j].X, pack[j].Y, ux * push, uy * push, radius, walls);
                    pack[j] = (nx, ny);
                }
            }
        }
    }
}
