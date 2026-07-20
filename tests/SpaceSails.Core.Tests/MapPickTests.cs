namespace SpaceSails.Core.Tests;

/// <summary>
/// #402 follow-up — the click-pick hit-radius rule (MapPick). Pins the deflection gig's acceptance
/// bar the owner's live smoke test flagged: the inbound THREAT ROCK must always be one click away in
/// the tight Ringside cluster, even though its own disc is a pinprick sitting a few pixels off the
/// station knot where the click lands.
/// </summary>
public class MapPickTests
{
    private const double BaseTapPx = 15; // Map.PickRadiusPx — the forgiving direct-hit radius

    [Fact]
    public void BodyHit_FloorsAPinprickToTheMinimum()
    {
        // A sub-pixel disc still answers a tap out to the floor, but never tighter than the base tap.
        Assert.Equal(BaseTapPx, MapPick.BodyHitRadiusPx(drawnPx: 0.2, baseRadiusPx: BaseTapPx));
        Assert.Equal(MapPick.MinBodyHitPx, MapPick.BodyHitRadiusPx(drawnPx: 0.2, baseRadiusPx: 0));
    }

    [Fact]
    public void BodyHit_CapsAZoomedInWorld()
    {
        // A huge drawn disc is capped so it doesn't swallow every camera drag on the screen.
        Assert.Equal(MapPick.MaxBodyHitPx, MapPick.BodyHitRadiusPx(drawnPx: 5000, baseRadiusPx: BaseTapPx));
    }

    [Fact]
    public void ThreatRock_IsAlwaysAtLeastTheWideTolerance()
    {
        // Whatever the zoom — pinprick disc or not — the rock's pick radius is never below the widened
        // threat tolerance, and it's strictly wider than a body's own would be at that size.
        double rockHit = MapPick.ThreatRockHitRadiusPx(drawnPx: 1.0, baseRadiusPx: BaseTapPx);
        Assert.Equal(MapPick.ThreatRockRadiusPx, rockHit);
        Assert.True(rockHit > MapPick.BodyHitRadiusPx(drawnPx: 1.0, baseRadiusPx: BaseTapPx),
            "the threat rock's pick tolerance must be wider than a plain body's");
    }

    [Fact]
    public void ThreatRock_LandsAClickThatAPlainBodyWouldMiss()
    {
        // The live finding: the click hit the station knot ~30 px from the rock's screen dot. A plain
        // body's radius (base tap, pinprick disc) would miss it; the widened threat radius catches it.
        const double clickDistPx = 30;
        double bodyHit = MapPick.BodyHitRadiusPx(drawnPx: 1.0, baseRadiusPx: BaseTapPx);
        double rockHit = MapPick.ThreatRockHitRadiusPx(drawnPx: 1.0, baseRadiusPx: BaseTapPx);

        Assert.True(clickDistPx > bodyHit, "a plain-body radius would have missed this cluster click");
        Assert.True(clickDistPx <= rockHit, "the threat rock must still be offered at this click distance");
    }

    [Fact]
    public void ThreatRock_NeverNarrowsAWorldThatIsAlreadyWider()
    {
        // A big rock (rare, but honest): the widening is a floor, never a cap — a disc drawn larger
        // than the threat tolerance keeps its own (capped) radius.
        Assert.Equal(MapPick.MaxBodyHitPx, MapPick.ThreatRockHitRadiusPx(drawnPx: 5000, baseRadiusPx: BaseTapPx));
    }
}
