namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for the shuttle-bay ferry rule (#163, "the door you understand as a flight"). The reach and
/// the cruise speed are honest derivations of constants the rest of Core already flies with
/// (<see cref="CaptureRule"/>, <see cref="DockRule"/>), so these tests pin both the derivations and the
/// in-range / out-of-range classification and the travel-time cost.
/// </summary>
public class ShuttleRangeTests
{
    // ---- construction: the numbers come straight from the boarding-shuttle reach and the clamp speed ----

    [Fact]
    public void Reach_IsTheBoardingShuttleCrossing()
    {
        Assert.Equal(CaptureRule.CaptureRadiusMeters, ShuttleRange.RangeMeters);
        Assert.Equal(5e8, ShuttleRange.RangeMeters, 6); // 500,000 km
    }

    [Fact]
    public void CruiseSpeed_IsTheClampMatchSpeed()
    {
        Assert.Equal(DockRule.MatchSpeed, ShuttleRange.CruiseSpeedMps);
        Assert.Equal(8000.0, ShuttleRange.CruiseSpeedMps, 6); // 8 km/s
    }

    // ---- in-range / out-of-range classification ----

    [Fact]
    public void InRange_AtTheBerth_IsInRange()
    {
        Assert.True(ShuttleRange.InRange(0.0));
    }

    [Fact]
    public void InRange_JustInside_IsInRange()
    {
        Assert.True(ShuttleRange.InRange(ShuttleRange.RangeMeters - 1.0));
    }

    [Fact]
    public void InRange_ExactlyAtReach_IsInRange()
    {
        Assert.True(ShuttleRange.InRange(ShuttleRange.RangeMeters));
    }

    [Fact]
    public void InRange_JustBeyondReach_IsOutOfRange()
    {
        Assert.False(ShuttleRange.InRange(ShuttleRange.RangeMeters + 1.0));
    }

    [Fact]
    public void InRange_NextPlanetGap_IsOutOfRange()
    {
        // Neighbouring planets sit ~1e11 m apart — a shuttle never reaches across.
        Assert.False(ShuttleRange.InRange(1e11));
    }

    [Fact]
    public void InRange_NegativeDistance_IsNeverInRange()
    {
        Assert.False(ShuttleRange.InRange(-1.0));
    }

    // A moon-from-its-planet's-station hop (the #164 Phobos case): The Space Bar orbits Mars at
    // a≈1.2e7 m, Phobos at a≈9.4e6 m, so the two are at most ~2.1e7 m apart — comfortably one hop.
    [Fact]
    public void InRange_MoonFromCloseInStation_IsReachable()
    {
        const double stationToMoonWorstCase = 1.2e7 + 9.4e6; // radial extremes, same planet
        Assert.True(ShuttleRange.InRange(stationToMoonWorstCase));
    }

    // ---- trip-cost rule: straight-line gap flown at cruise speed ----

    [Fact]
    public void TravelSeconds_IsGapOverCruiseSpeed()
    {
        Assert.Equal(ShuttleRange.RangeMeters / ShuttleRange.CruiseSpeedMps,
            ShuttleRange.TravelSeconds(ShuttleRange.RangeMeters), 6);
        // 5e8 / 8000 = 62,500 s ≈ 17.4 h at the edge of range.
        Assert.Equal(62_500.0, ShuttleRange.TravelSeconds(ShuttleRange.RangeMeters), 6);
    }

    [Fact]
    public void TravelSeconds_ShortHopIsCheap()
    {
        // A ~5,000 km pad hop (Enceladus-alongside test start) is ~10 minutes, not hours.
        double t = ShuttleRange.TravelSeconds(5e6);
        Assert.Equal(625.0, t, 6);
        Assert.True(t < 3600, "a 5,000 km hop should cost well under an hour");
    }

    [Fact]
    public void TravelSeconds_FartherIsLonger()
    {
        Assert.True(ShuttleRange.TravelSeconds(4e8) > ShuttleRange.TravelSeconds(2e7));
    }

    [Fact]
    public void TravelSeconds_NonPositiveGap_CostsNoTime()
    {
        Assert.Equal(0.0, ShuttleRange.TravelSeconds(0.0), 6);
        Assert.Equal(0.0, ShuttleRange.TravelSeconds(-1.0), 6);
    }
}
