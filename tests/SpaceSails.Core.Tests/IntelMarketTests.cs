namespace SpaceSails.Core.Tests;

public class IntelMarketTests
{
    private const double Day = 86400;

    // ---- RouteIntel / IntelLedger freshness ----

    [Fact]
    public void IsFresh_TrueBeforeExpiry_FalseAfter()
    {
        var intel = new RouteIntel("npc-1", PurchasedAtSimTime: 1000, ValidForSeconds: 30 * Day, Price: 500);

        Assert.True(intel.IsFresh(1000));
        Assert.True(intel.IsFresh(1000 + 30 * Day));
        Assert.False(intel.IsFresh(1000 + 30 * Day + 1));
    }

    [Fact]
    public void IntelLedger_Knows_ReflectsFreshness()
    {
        var ledger = new IntelLedger();
        ledger.Add(new RouteIntel("npc-1", 0, 30 * Day, 500));

        Assert.True(ledger.Knows("npc-1", 0));
        Assert.True(ledger.Knows("npc-1", 30 * Day));
        Assert.False(ledger.Knows("npc-1", 30 * Day + 1));
        Assert.False(ledger.Knows("npc-unknown", 0));
    }

    [Fact]
    public void IntelLedger_PruneStale_DropsExpiredEntries()
    {
        var ledger = new IntelLedger();
        ledger.Add(new RouteIntel("npc-1", 0, 30 * Day, 500));

        ledger.PruneStale(30 * Day + 1);

        Assert.Empty(ledger.Entries);
        Assert.False(ledger.TryGet("npc-1", out _));
    }

    [Fact]
    public void IntelLedger_Add_Repurchase_RefreshesEntry()
    {
        var ledger = new IntelLedger();
        ledger.Add(new RouteIntel("npc-1", 0, 30 * Day, 500));
        ledger.Add(new RouteIntel("npc-1", 10 * Day, 30 * Day, 400));

        Assert.True(ledger.TryGet("npc-1", out RouteIntel intel));
        Assert.Equal(10 * Day, intel.PurchasedAtSimTime);
        Assert.Equal(400, intel.Price);
        Assert.Single(ledger.Entries);
    }

    // ---- Pricing: monotonic in cargo value and distance from Earth ----

    [Fact]
    public void BuyPrice_IsMonotonicallyIncreasing_InCargoValue()
    {
        double distance = 2e11; // fixed, well inside the unclamped region

        int previous = -1;
        foreach (int cargoValue in new[] { 0, 1000, 5000, 10000, 24000 })
        {
            int price = IntelMarket.BuyPrice(cargoValue, distance);
            Assert.True(price > previous, $"Price should increase with cargo value; {previous} -> {price}");
            previous = price;
        }
    }

    [Fact]
    public void BuyPrice_IsMonotonicallyDecreasing_InDistanceFromEarth()
    {
        int cargoValue = 12000; // fixed

        double previous = double.MaxValue;
        // Kept within the unclamped region of DistanceFactor (d in [4e11, 4.9e12]) so the
        // monotonicity isn't hidden behind the [0.3, 3] clamp at either end.
        foreach (double distance in new[] { 5e11, 1e12, 2e12, 3e12, 4e12 })
        {
            double price = IntelMarket.BuyPrice(cargoValue, distance);
            Assert.True(price < previous, $"Price should decrease with distance from Earth; {previous} -> {price}");
            previous = price;
        }
    }

    [Fact]
    public void BuyPrice_IsDeterministic()
    {
        int a = IntelMarket.BuyPrice(5000, 3e11);
        int b = IntelMarket.BuyPrice(5000, 3e11);

        Assert.Equal(a, b);
    }

    [Fact]
    public void DistanceFactor_IsClamped()
    {
        Assert.Equal(3.0, IntelMarket.DistanceFactor(0), 6);
        Assert.Equal(0.3, IntelMarket.DistanceFactor(1e15), 6);
    }

    // ---- Selling your own tracks: scales with quality and target cargo value ----

    [Fact]
    public void SellPrice_ScalesWithQuality()
    {
        int cargoValue = 1200 * 10; // He3, 10 units

        int low = IntelMarket.SellPrice(0.5, cargoValue);
        int mid = IntelMarket.SellPrice(0.75, cargoValue);
        int high = IntelMarket.SellPrice(1.0, cargoValue);

        Assert.True(low < mid);
        Assert.True(mid < high);
    }

    [Fact]
    public void SellPrice_ScalesWithCargoValue()
    {
        int low = IntelMarket.SellPrice(0.8, 1000);
        int high = IntelMarket.SellPrice(0.8, 10000);

        Assert.True(low < high);
    }

    [Theory]
    [InlineData(0.49, false)]
    [InlineData(0.5, true)]
    [InlineData(0.9, true)]
    public void CanSellTrack_RequiresMinimumQuality(double quality, bool expected)
    {
        Assert.Equal(expected, IntelMarket.CanSellTrack(quality));
    }

    // ---- CanTradeIntelAt truth table ----

    private static CelestialBody Haven() =>
        new("haven-1", "Backwater Rock", "sun", 1e10, 1e5, 5e11, 1e7, 0, BodyKind.Moon, IsHaven: true);

    private static CelestialBody InnerStation() =>
        new("inner-station", "Mercury Compute Farm", "sun", 1e10, 1e5, 5.8e10, 1e7, 0, BodyKind.Station);

    private static CelestialBody OuterStation() =>
        new("outer-station", "Far Trading Post", "sun", 1e10, 1e5, 6e11, 1e7, 0, BodyKind.Station);

    private static CelestialBody Planet() =>
        new("mars", "Mars", "sun", 4.28e13, 3.39e6, 2.28e11, 5.94e7, 0, BodyKind.Planet);

    [Fact]
    public void CanTradeIntelAt_Haven_IsAlwaysTrue()
    {
        CelestialBody haven = Haven();
        Assert.True(IntelMarket.CanTradeIntelAt(haven, distanceFromSunMeters: 1e10)); // even if "close"
    }

    [Fact]
    public void CanTradeIntelAt_InnerStation_IsFalse()
    {
        CelestialBody station = InnerStation();
        Assert.False(IntelMarket.CanTradeIntelAt(station, distanceFromSunMeters: station.OrbitRadius));
    }

    [Fact]
    public void CanTradeIntelAt_OuterStation_IsTrue()
    {
        CelestialBody station = OuterStation();
        Assert.True(IntelMarket.CanTradeIntelAt(station, distanceFromSunMeters: station.OrbitRadius));
    }

    [Fact]
    public void CanTradeIntelAt_Planet_IsFalse()
    {
        CelestialBody planet = Planet();
        // Even at Saturn's distance, an ordinary planet (not a haven, not a station) never trades intel.
        Assert.False(IntelMarket.CanTradeIntelAt(planet, distanceFromSunMeters: 1.4e12));
    }

    // ---- Laser ranging / tight-beam (Core/ActiveSensors.cs) ----

    [Fact]
    public void LaserRange_ReturnsExactObservation_AndPingEvent()
    {
        var playerPos = new Vector2d(1e11, 0);
        var targetPos = new Vector2d(1.2e11, 5e9);
        var targetVel = new Vector2d(1000, -200);

        (Observation obs, PingEvent ping) = ActiveSensors.LaserRange("npc-7", playerPos, targetPos, targetVel, simTime: 12345);

        Assert.Equal("npc-7", obs.TargetId);
        Assert.Equal(12345, obs.SimTime);
        Assert.Equal(targetPos, obs.Position);
        Assert.Equal(targetVel, obs.Velocity);

        Assert.Equal("npc-7", ping.TargetId);
        Assert.Equal(playerPos, ping.SourcePosition);
        Assert.Equal(12345, ping.SimTime);
    }

    [Fact]
    public void LaserRange_IsDeterministic()
    {
        var playerPos = new Vector2d(3e10, 4e10);
        var targetPos = new Vector2d(-2e10, 6e10);
        var targetVel = new Vector2d(500, 500);

        (Observation obsA, PingEvent pingA) = ActiveSensors.LaserRange("t", playerPos, targetPos, targetVel, 500);
        (Observation obsB, PingEvent pingB) = ActiveSensors.LaserRange("t", playerPos, targetPos, targetVel, 500);

        Assert.Equal(obsA, obsB);
        Assert.Equal(pingA, pingB);
    }

    [Fact]
    public void CanTightBeam_RangeBoundary()
    {
        var playerPos = Vector2d.Zero;
        var justInside = new Vector2d(ActiveSensors.TightBeamMaxRangeMeters - 1, 0);
        var exactlyAt = new Vector2d(ActiveSensors.TightBeamMaxRangeMeters, 0);
        var justOutside = new Vector2d(ActiveSensors.TightBeamMaxRangeMeters + 1, 0);

        Assert.True(ActiveSensors.CanTightBeam(playerPos, justInside));
        Assert.True(ActiveSensors.CanTightBeam(playerPos, exactlyAt));
        Assert.False(ActiveSensors.CanTightBeam(playerPos, justOutside));
    }

    [Fact]
    public void CanTightBeam_CustomMaxRange_IsRespected()
    {
        var playerPos = Vector2d.Zero;
        var target = new Vector2d(1000, 0);

        Assert.True(ActiveSensors.CanTightBeam(playerPos, target, maxRangeMeters: 1000));
        Assert.False(ActiveSensors.CanTightBeam(playerPos, target, maxRangeMeters: 999));
    }
}
