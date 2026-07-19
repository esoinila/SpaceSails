namespace SpaceSails.Core.Tests;

/// <summary>
/// #371 Phase 3 — EXPEDITION FOG OF WAR (owner, cruise 2026-07-19: "some kind of what is visible effect …
/// spaces behind corners in a newly opened door are not yet known. Open terrain is seen from the ship but
/// reevers behind cover might not show so clearly … some indication on map that here was movement before").
/// These pin the pure rule: the open field is seen (clear LOS reveals), a wall breaks the look (behind
/// cover is unknown), a chamber reveals only through its doorway, echoes fade linearly, and the captain
/// cell quantises so region visibility recomputes on a move, not per frame.
/// </summary>
public class ExpeditionVisibilityTests
{
    private static List<SurfaceCollision.Segment> Wall(double x1, double y1, double x2, double y2) =>
        [new SurfaceCollision.Segment(x1, y1, x2, y2)];

    [Fact]
    public void PointVisible_OpenGround_ClearLineOfSight_IsSeen()
    {
        Assert.True(ExpeditionVisibility.PointVisible(0, 0, 10, 0, []));
    }

    [Fact]
    public void PointVisible_WallBetween_IsHidden()
    {
        // A vertical wall at x=5 across the sightline from (0,0) to (10,0).
        List<SurfaceCollision.Segment> wall = Wall(5, -5, 5, 5);
        Assert.False(ExpeditionVisibility.PointVisible(0, 0, 10, 0, wall));
    }

    [Fact]
    public void PointVisible_BeyondSightRange_IsHidden_EvenWithClearLoS()
    {
        double far = ExpeditionVisibility.SightRange + 10.0;
        Assert.False(ExpeditionVisibility.PointVisible(0, 0, far, 0, []));
    }

    [Fact]
    public void PointVisible_WallOffToTheSide_DoesNotBlock()
    {
        // A wall well off the sightline never breaks the look.
        List<SurfaceCollision.Segment> wall = Wall(5, 20, 5, 30);
        Assert.True(ExpeditionVisibility.PointVisible(0, 0, 10, 0, wall));
    }

    // ── Region reveal: seen through the doorway, dark from behind a wall ──

    [Fact]
    public void RegionVisible_WhenHeartIsInClearSight_IsRevealed()
    {
        // Captain right in front of a room whose heart is at (10,0), no walls between.
        Assert.True(ExpeditionVisibility.RegionVisible(
            0, 0, heartX: 10, heartY: 0, minX: 8, minY: -3, maxX: 14, maxY: 3, walls: []));
    }

    [Fact]
    public void RegionVisible_WhenAWallHidesEverySample_StaysDark()
    {
        // A long wall between the captain and the whole room footprint (heart + corners) hides it all.
        List<SurfaceCollision.Segment> wall = Wall(5, -10, 5, 10);
        Assert.False(ExpeditionVisibility.RegionVisible(
            0, 0, heartX: 10, heartY: 0, minX: 8, minY: -3, maxX: 14, maxY: 3, walls: wall));
    }

    // ── Echo decay: a clamped linear fade to nothing ──

    [Fact]
    public void EchoAlpha_FadesLinearly_FromFullToZero()
    {
        double life = ExpeditionVisibility.EchoLifetimeSeconds;
        Assert.Equal(1.0, ExpeditionVisibility.EchoAlpha(0.0, life), 3);
        Assert.Equal(0.5, ExpeditionVisibility.EchoAlpha(life / 2.0, life), 3);
        Assert.Equal(0.0, ExpeditionVisibility.EchoAlpha(life, life), 3);
        Assert.Equal(0.0, ExpeditionVisibility.EchoAlpha(life + 5.0, life), 3); // past its life, gone
    }

    // ── Captain-cell quantisation: recompute region visibility on a move, not per frame ──

    [Fact]
    public void CaptainCell_QuantisesPosition_StableWithinACell_ChangesAcrossOne()
    {
        (int, int) a = ExpeditionVisibility.CaptainCell(0.1, 0.1);
        (int, int) b = ExpeditionVisibility.CaptainCell(0.9, 0.9); // same 2-du cell
        (int, int) c = ExpeditionVisibility.CaptainCell(3.0, 0.1); // a cell over in x
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
