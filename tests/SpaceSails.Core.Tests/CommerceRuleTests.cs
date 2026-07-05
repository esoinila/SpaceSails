namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-5, orbital commerce (vision par. 10): trade requires being in orbit at the same body as the
/// counterpart, or course-matched with a moving partner long enough for drones to fly the gap.
/// </summary>
public class CommerceRuleTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    private static ShipState At(Vector2d position, Vector2d velocity) =>
        new(position, velocity, SimTime: 0);

    // ---- CanTrade truth table ----

    [Fact]
    public void CanTrade_SameOrbitBody_IsAlwaysTrue_EvenFarApartOrFastRelative()
    {
        var player = At(new Vector2d(1e11, 0), new Vector2d(0, 30000));
        // Deliberately far and fast relative — same body should override the envelope entirely.
        var partnerPos = new Vector2d(1e11, 5e9);
        var partnerVel = new Vector2d(20000, -20000);

        Assert.True(CommerceRule.CanTrade(player, partnerPos, partnerVel, "earth", "earth"));
    }

    [Fact]
    public void CanTrade_DifferentOrbitBodies_FallsBackToEnvelope_AndFailsWhenOutsideIt()
    {
        var player = At(new Vector2d(1e11, 0), new Vector2d(0, 30000));
        // M29 changed this case: 5e9 apart at zero rel is SHUTTLE REACH now, so the honest
        // "no deal" needs to sit beyond even the shuttles — or be too fast for them.
        var farPos = new Vector2d(1e11, 5e10);
        Assert.False(CommerceRule.CanTrade(player, farPos, new Vector2d(0, 30000), "earth", "mars"));

        var fastPos = new Vector2d(1e11, 5e9);
        var fastVel = new Vector2d(0, 30000 + 6000); // 6 km/s rel — a drive-by, not a pass
        Assert.False(CommerceRule.CanTrade(player, fastPos, fastVel, "earth", "mars"));
    }

    // ---- M29: the shuttle tier ----

    [Fact]
    public void Classify_TiersInOrder_OrbitThenDronesThenShuttles()
    {
        var player = At(new Vector2d(1e11, 0), new Vector2d(0, 30000));

        Assert.Equal(CommerceRule.TradeMode.SameOrbit,
            CommerceRule.Classify(player, new Vector2d(1e11, 5e9), new Vector2d(9000, 0), "earth", "earth"));

        // Tight and slow: drones.
        Assert.Equal(CommerceRule.TradeMode.DroneMatch,
            CommerceRule.Classify(player, new Vector2d(1e11, 3e8), new Vector2d(0, 31500), null, null));

        // The owner's live pass: ~10 M km out, 4.4 km/s relative — shuttles fly it.
        Assert.Equal(CommerceRule.TradeMode.Shuttle,
            CommerceRule.Classify(player, new Vector2d(1e11, 9.99e9), new Vector2d(0, 30000 - 4400), null, null));

        // Past shuttle reach, or too fast for the pass: no deal.
        Assert.Equal(CommerceRule.TradeMode.None,
            CommerceRule.Classify(player, new Vector2d(1e11, 1.3e10), new Vector2d(0, 30000), null, null));
        Assert.Equal(CommerceRule.TradeMode.None,
            CommerceRule.Classify(player, new Vector2d(1e11, 9.99e9), new Vector2d(0, 30000 - 5500), null, null));
    }

    [Fact]
    public void TransferSeconds_ShuttlesPayForTheCorridor_DronesStayCheapClose()
    {
        // The owner's pass: 5 units, 9.99e9 m, 4.4 km/s rel — a real but playable wait.
        double shuttle = CommerceRule.TransferSeconds(CommerceRule.TradeMode.Shuttle, 4400, 9.99e9, 5);
        Assert.InRange(shuttle, 120, 900);

        // Distance is paid for honestly: half the corridor is meaningfully cheaper.
        double nearer = CommerceRule.TransferSeconds(CommerceRule.TradeMode.Shuttle, 4400, 5e9, 5);
        Assert.True(nearer < shuttle * 0.75);

        // The close tiers keep the old drone math untouched.
        Assert.Equal(
            CommerceRule.DroneTransferSeconds(500, 2e8, 5),
            CommerceRule.TransferSeconds(CommerceRule.TradeMode.DroneMatch, 500, 2e8, 5), precision: 9);
    }

    [Fact]
    public void ContactsAt_ChildrenRideRailsWithRealVelocity()
    {
        var ephemeris = Sol();
        IReadOnlyList<CommerceRule.LocalContact> contacts =
            CommerceRule.ContactsAt(ephemeris, [], simTime: 0, "earth");

        // Luna (and any station child) must report a nonzero on-rails velocity — a zero made
        // course-matching a station mathematically impossible (M29 fix).
        Assert.Contains(contacts, c => c.Kind != CommerceRule.LocalContactKind.Depot
            && c.Velocity.Length > 1000);
    }

    [Fact]
    public void ContactsWithinShuttleRange_FindsTheOpportunity_AndOnlyThen()
    {
        var ephemeris = Sol();
        Vector2d earth = ephemeris.Position("earth", 0);
        Vector2d earthVel = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;

        // Drifting 8e9 m off Earth: Luna-adjacent stations/havens keyed to Earth space are
        // inside shuttle reach and must be offered.
        var near = At(earth + new Vector2d(8e9, 0), earthVel);
        IReadOnlyList<CommerceRule.LocalContact> offers =
            CommerceRule.ContactsWithinShuttleRange(ephemeris, [], 0, near);
        Assert.NotEmpty(offers);
        Assert.All(offers, c => Assert.True(c.DistanceMeters <= CommerceRule.ShuttleRangeMeters));
        Assert.All(offers, c => Assert.True((c.Actions & CommerceRule.ActionKind.Trade) != 0));

        // Deep space: nothing to offer.
        var deep = At(earth + new Vector2d(9e10, 9e10), earthVel);
        Assert.Empty(CommerceRule.ContactsWithinShuttleRange(ephemeris, [], 0, deep));
    }

    [Fact]
    public void CanTrade_NullOrbitBodies_StillAllowsCourseMatchedTrading()
    {
        var player = At(Vector2d.Zero, new Vector2d(1000, 0));
        var partnerPos = new Vector2d(1e8, 0);
        var partnerVel = new Vector2d(1000, 0); // zero relative speed

        Assert.True(CommerceRule.CanTrade(player, partnerPos, partnerVel, null, null));
    }

    [Fact]
    public void CanTrade_CourseMatched_DistanceBoundary()
    {
        var player = At(Vector2d.Zero, Vector2d.Zero);
        Vector2d justInside = new(CommerceRule.CourseMatchDistanceMeters * 0.999, 0);
        Vector2d justOutside = new(CommerceRule.CourseMatchDistanceMeters * 1.001, 0);

        // M29: crossing the drone envelope hands the deal to the SHUTTLE tier, not to "no".
        Assert.Equal(CommerceRule.TradeMode.DroneMatch,
            CommerceRule.Classify(player, justInside, Vector2d.Zero, null, null));
        Assert.Equal(CommerceRule.TradeMode.Shuttle,
            CommerceRule.Classify(player, justOutside, Vector2d.Zero, null, null));
    }

    [Fact]
    public void CanTrade_CourseMatched_RelativeSpeedBoundary()
    {
        var player = At(Vector2d.Zero, Vector2d.Zero);
        Vector2d partnerPos = new(1e7, 0); // well within distance envelope
        Vector2d slowEnough = new(0, CommerceRule.CourseMatchMaxRelativeSpeed * 0.999);
        Vector2d tooFast = new(0, CommerceRule.CourseMatchMaxRelativeSpeed * 1.001);

        // M29: too fast for drones is still fine for the shuttles up to their own 5 km/s.
        Assert.Equal(CommerceRule.TradeMode.DroneMatch,
            CommerceRule.Classify(player, partnerPos, slowEnough, null, null));
        Assert.Equal(CommerceRule.TradeMode.Shuttle,
            CommerceRule.Classify(player, partnerPos, tooFast, null, null));
        Assert.Equal(CommerceRule.TradeMode.None,
            CommerceRule.Classify(player, partnerPos, new Vector2d(0, CommerceRule.ShuttleMaxRelativeSpeed * 1.001), null, null));
    }

    // ---- DroneTransferSeconds monotonicity ----

    [Fact]
    public void DroneTransferSeconds_GrowsWithRelativeSpeed()
    {
        double slow = CommerceRule.DroneTransferSeconds(relativeSpeed: 0, distance: 0, units: 1);
        double fast = CommerceRule.DroneTransferSeconds(relativeSpeed: 2000, distance: 0, units: 1);

        Assert.True(fast > slow);
    }

    [Fact]
    public void DroneTransferSeconds_GrowsWithDistance()
    {
        double near = CommerceRule.DroneTransferSeconds(relativeSpeed: 0, distance: 0, units: 1);
        double far = CommerceRule.DroneTransferSeconds(relativeSpeed: 0, distance: 1e9, units: 1);

        Assert.True(far > near);
    }

    [Fact]
    public void DroneTransferSeconds_GrowsWithUnits_AndClampsToAtLeastOne()
    {
        double one = CommerceRule.DroneTransferSeconds(relativeSpeed: 100, distance: 1e7, units: 1);
        double ten = CommerceRule.DroneTransferSeconds(relativeSpeed: 100, distance: 1e7, units: 10);
        double zeroOrNegative = CommerceRule.DroneTransferSeconds(relativeSpeed: 100, distance: 1e7, units: 0);

        Assert.True(ten > one);
        Assert.Equal(one, zeroOrNegative); // clamps to at least 1 unit's worth
    }

    [Fact]
    public void DroneTransferSeconds_PerfectMatch_EqualsBaseRateTimesUnits()
    {
        double seconds = CommerceRule.DroneTransferSeconds(relativeSpeed: 0, distance: 0, units: 3);
        Assert.Equal(CommerceRule.DroneBaseSecondsPerUnit * 3, seconds, 6);
    }

    [Fact]
    public void DroneTransferSeconds_IsFasterThanBoardingShapeAtTheSameEnvelopeCorner()
    {
        // At the boarding envelope's own sloppy corner (5 km/s, capture radius), a single-unit
        // drone run should still be much cheaper than the one-shot boarding time, because the
        // drone constants are deliberately gentler (cooperative partner, not a raid).
        double droneSeconds = CommerceRule.DroneTransferSeconds(
            relativeSpeed: CaptureRule.MaxRelativeSpeed, distance: CaptureRule.CaptureRadiusMeters, units: 1);
        var player = new ShipState(Vector2d.Zero, new Vector2d(CaptureRule.MaxRelativeSpeed, 0), 0);
        var target = new ShipState(new Vector2d(CaptureRule.CaptureRadiusMeters, 0), Vector2d.Zero, 0);
        double boardingSeconds = CaptureRule.RequiredSecondsFor(player, target);

        Assert.True(droneSeconds < boardingSeconds);
    }

    // ---- LocalContact enumeration ----

    [Fact]
    public void ContactsAt_FindsDepotStationChildAndInHillShipAndTagsActions()
    {
        var ephemeris = Sol();
        double simTime = 0;
        IReadOnlyList<NpcShip> depotShips = TrafficSchedule.GenerateDepots(ephemeris, seed: 44);

        CommerceRule.LocalShip depotAtSaturn = default;
        bool foundDepot = false;
        foreach (NpcShip d in depotShips)
        {
            if (d.DepotBodyId == "saturn")
            {
                depotAtSaturn = new CommerceRule.LocalShip(d.Id, d.Callsign, d.InitialState, d.DepotBodyId);
                foundDepot = true;
                break;
            }
        }
        Assert.True(foundDepot);

        // A ship parked well inside Saturn's Hill sphere but not a depot.
        Vector2d saturnPos = ephemeris.Position("saturn", simTime);
        var nearbyShip = new CommerceRule.LocalShip(
            "npc-test", "Test Hauler", new ShipState(saturnPos + new Vector2d(1e7, 0), Vector2d.Zero, simTime), null);

        var ships = new List<CommerceRule.LocalShip> { depotAtSaturn, nearbyShip };
        IReadOnlyList<CommerceRule.LocalContact> contacts = CommerceRule.ContactsAt(ephemeris, ships, simTime, "saturn");

        CommerceRule.LocalContact depotContact = Assert.Single(contacts, c => c.Kind == CommerceRule.LocalContactKind.Depot);
        Assert.Equal(CommerceRule.ActionKind.Trade, depotContact.Actions);

        CommerceRule.LocalContact shipContact = Assert.Single(contacts, c => c.Kind == CommerceRule.LocalContactKind.Ship);
        Assert.Equal(CommerceRule.ActionKind.Board, shipContact.Actions);

        // Titan is a child body of Saturn — an ordinary moon, no depot of its own, so no actions.
        CommerceRule.LocalContact titanContact = Assert.Single(contacts, c => c.Id == "titan");
        Assert.Equal(CommerceRule.LocalContactKind.Moon, titanContact.Kind);
        Assert.Equal(CommerceRule.ActionKind.None, titanContact.Actions);

        // Ringside Exchange is both a station and a haven child of Saturn: Trade + Fence.
        CommerceRule.LocalContact ringsideContact = Assert.Single(contacts, c => c.Id == "ringside-exchange");
        Assert.Equal(CommerceRule.LocalContactKind.Haven, ringsideContact.Kind);
        Assert.Equal(CommerceRule.ActionKind.Trade | CommerceRule.ActionKind.Fence, ringsideContact.Actions);

        // Enceladus is a haven moon child of Saturn: Trade + Fence too, tagged as a haven not a moon.
        CommerceRule.LocalContact enceladusContact = Assert.Single(contacts, c => c.Id == "enceladus");
        Assert.Equal(CommerceRule.LocalContactKind.Haven, enceladusContact.Kind);
        Assert.Equal(CommerceRule.ActionKind.Trade | CommerceRule.ActionKind.Fence, enceladusContact.Actions);
    }

    [Fact]
    public void ContactsAt_ShipOutsideHillSphere_IsExcluded()
    {
        var ephemeris = Sol();
        double simTime = 0;
        Vector2d saturnPos = ephemeris.Position("saturn", simTime);
        // Far outside Saturn's Hill sphere (Saturn's Hill radius is on the order of 6.5e10 m).
        var farShip = new CommerceRule.LocalShip(
            "npc-far", "Far Hauler", new ShipState(saturnPos + new Vector2d(5e11, 0), Vector2d.Zero, simTime), null);

        IReadOnlyList<CommerceRule.LocalContact> contacts = CommerceRule.ContactsAt(
            ephemeris, [farShip], simTime, "saturn");

        Assert.DoesNotContain(contacts, c => c.Id == "npc-far");
    }

    [Fact]
    public void ContactsAt_UnknownBodyId_ReturnsEmpty()
    {
        var ephemeris = Sol();
        IReadOnlyList<CommerceRule.LocalContact> contacts = CommerceRule.ContactsAt(ephemeris, [], 0, "does-not-exist");
        Assert.Empty(contacts);
    }

    [Fact]
    public void ContactsAt_IsDeterministic()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> depotShips = TrafficSchedule.GenerateDepots(ephemeris, seed: 44);
        List<CommerceRule.LocalShip> ships = [.. depotShips.Select(d =>
            new CommerceRule.LocalShip(d.Id, d.Callsign, d.InitialState, d.DepotBodyId))];

        IReadOnlyList<CommerceRule.LocalContact> first = CommerceRule.ContactsAt(ephemeris, ships, 12345, "earth");
        IReadOnlyList<CommerceRule.LocalContact> second = CommerceRule.ContactsAt(ephemeris, ships, 12345, "earth");

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }
}
