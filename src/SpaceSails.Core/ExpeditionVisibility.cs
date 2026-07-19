using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #371 Phase 3 — EXPEDITION-SITE FOG OF WAR (owner, cruise 2026-07-19, verbatim): <b>"some kind of what
/// is visible effect to the map on expedition … spaces behind corners in a newly opened door are not yet
/// known. Open terrain is seen from the ship but reevers behind cover might not show so clearly on it.
/// Maybe some indication on map that here was movement before. Then the motion detector is much more
/// exciting to see."</b>
///
/// <para>The pure, deterministic visibility rule — the natural companion of the appended regions. It reuses
/// the ONE wall law (<see cref="SurfaceCollision.HasLineOfSight"/>): the open field is overwatched from the
/// ship (always drawn), but a point behind cover — a freshly-forced chamber's interior, or an Old One with
/// a wall between it and the captain — is unknown until the captain's own line of sight reaches it. The
/// MOTION TRACKER is untouched: it still HEARS everything through the walls (that is now its whole glory).</para>
///
/// <para>Kept cheap by design (owner: keep it coarse): region-level LOS from the captain's position, meant
/// to be recomputed per captain-cell move (see <see cref="CaptainCell"/>), not per pixel per frame. Echo
/// marks — the "here was movement before" ripples a lost contact leaves — decay by a pure linear fade.</para>
/// </summary>
public static class ExpeditionVisibility
{
    /// <summary>The movement-echo lifetime (real seconds): when a seen, moving contact slips behind cover it
    /// leaves a ripple that fades to nothing over this long. Client state only (like the swept grid), seeded
    /// by nothing. OWNER-TUNABLE.</summary>
    public const double EchoLifetimeSeconds = 12.0;

    /// <summary>The coarse cell edge (deck units) the captain's position is quantised to, so region
    /// visibility is recomputed only when the captain crosses into a new cell — not every frame.</summary>
    public const double CaptainCellSize = 2.0;

    /// <summary>Beyond this range a point is treated as unseen even with clear LOS — a generous cap sized
    /// PAST the field's own diagonal (~100 du) so it never hides open terrain (the ship's overwatch), only
    /// serving as a sanity bound. Behind-cover interiors go dark by their own walls breaking LOS, not by
    /// range. OWNER-TUNABLE.</summary>
    public const double SightRange = 120.0;

    /// <summary>The captain's coarse visibility cell — recompute region visibility only when this changes.</summary>
    public static (int Cx, int Cy) CaptainCell(double x, double y) =>
        ((int)System.Math.Floor(x / CaptainCellSize), (int)System.Math.Floor(y / CaptainCellSize));

    /// <summary>Can the captain at (<paramref name="cx"/>, <paramref name="cy"/>) see the point
    /// (<paramref name="tx"/>, <paramref name="ty"/>)? Clear line of sight over the wall segments AND within
    /// <see cref="SightRange"/>. This is the single primitive both the born-dark reveal and the behind-cover
    /// contact-hiding read.</summary>
    public static bool PointVisible(double cx, double cy, double tx, double ty,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double ddx = tx - cx, ddy = ty - cy;
        if ((ddx * ddx) + (ddy * ddy) > SightRange * SightRange)
        {
            return false;
        }
        return SurfaceCollision.HasLineOfSight(cx, cy, tx, ty, walls);
    }

    /// <summary>Is a region visible right now? True when the captain can see ANY of its sample points (its
    /// heart, and its bounds corners) — so a chamber reveals only once the captain sees through its doorway,
    /// never through a wall. Coarse and cheap; a region seen once is kept "explored" by the caller.</summary>
    public static bool RegionVisible(double cx, double cy,
        double heartX, double heartY, double minX, double minY, double maxX, double maxY,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        if (PointVisible(cx, cy, heartX, heartY, walls))
        {
            return true;
        }
        // The four interior-ish corners, pulled a little inward so a corner sample never sits exactly ON a
        // wall segment (which the exact intersection test would read as blocked).
        const double inset = 0.6;
        double ix0 = minX + inset, ix1 = maxX - inset, iy0 = minY + inset, iy1 = maxY - inset;
        return PointVisible(cx, cy, ix0, iy0, walls)
            || PointVisible(cx, cy, ix1, iy0, walls)
            || PointVisible(cx, cy, ix0, iy1, walls)
            || PointVisible(cx, cy, ix1, iy1, walls);
    }

    /// <summary>A movement echo's alpha (1 → 0) for its <paramref name="ageSeconds"/> over
    /// <paramref name="lifetimeSeconds"/> — a clamped linear fade. Past its life it is fully gone (0).</summary>
    public static double EchoAlpha(double ageSeconds, double lifetimeSeconds)
    {
        if (lifetimeSeconds <= 0.0 || ageSeconds <= 0.0)
        {
            return ageSeconds <= 0.0 ? 1.0 : 0.0;
        }
        double a = 1.0 - (ageSeconds / lifetimeSeconds);
        return a < 0.0 ? 0.0 : a > 1.0 ? 1.0 : a;
    }
}
