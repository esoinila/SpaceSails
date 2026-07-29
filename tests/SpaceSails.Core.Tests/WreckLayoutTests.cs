namespace SpaceSails.Core.Tests;

/// <summary>
/// #488 · THE WALKABILITY AUDIT — the test the owner asked for after boarding a wreck that was sealed in
/// half: <i>"THERE IS NO WAY TO GO TO THE BACK OF THE SHIP … we need some kind of CI test to spot similar
/// problems."</i>
///
/// <para>Every build was green while the LONG SHRIFT's mutiny barricades cut her in two, because a wall
/// you cannot pass has no test that fails. Type checks do not walk rooms. So these do: A* from the airlock
/// to every station the player is expected to reach, for EVERY cause, on every run.</para>
/// </summary>
public class WreckLayoutTests
{
    private const double AvatarRadius = 0.7;   // DeckPlan.AvatarRadius — the captain's own half-width

    /// <summary>The width the ship must be walkable at to feel comfortable, not merely to be passable.
    /// Owner, after finally walking her end to end: <i>"Couple walkways are a bit narrow but it is
    /// navigatable now."</i> Passable and pleasant are different tests, so this is a different test — a
    /// captain who has to thread a needle is being told the geometry is a puzzle when it is not meant to be
    /// one. Half again the captain's own radius: any gap they can walk should have room to walk it badly.</summary>
    private const double ComfortRadius = AvatarRadius * 1.5;

    private static DeckReachability.Point Spawn =>
        new(WreckLayout.SpawnX, WreckLayout.SpawnY);

    public static TheoryData<Derelict.WreckCause> EveryCause()
    {
        var data = new TheoryData<Derelict.WreckCause>();
        foreach (Derelict.WreckCause c in System.Enum.GetValues<Derelict.WreckCause>())
        {
            data.Add(c);
        }
        return data;
    }

    // ── The audit ─────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(EveryCause))]
    public void EveryStationIsReachableFromTheAirlock(Derelict.WreckCause cause)
    {
        // THE regression guard. If a compartment, a console or the way home is ever walled off — by damage,
        // by a bulkhead, by a doorway that was cut in the wrong place — this names it.
        IReadOnlyList<string> sealedOff = DeckReachability.Unreachable(
            Spawn,
            WreckLayout.Stations(cause),
            WreckLayout.Walls(cause),
            AvatarRadius,
            WreckLayout.Bounds);

        Assert.True(
            sealedOff.Count == 0,
            $"{cause}: the captain cannot reach {string.Join(", ", sealedOff)} from the airlock.");
    }

    [Theory]
    [MemberData(nameof(EveryCause))]
    public void TheWholeShipIsWalkableForeAndAft(Derelict.WreckCause cause)
    {
        // The specific thing that broke: the spine is the only way between bow and stern, so a captain who
        // can reach the bridge must also be able to reach engineering. Two barricades spanning the corridor
        // made the entire aft half unreachable and nothing complained.
        var bow = new DeckReachability.Point(20, 0);
        var stern = new DeckReachability.Point(-30, 0);

        IReadOnlyList<SurfaceCollision.Segment> walls = WreckLayout.Walls(cause);

        Assert.True(
            DeckReachability.CanReach(Spawn, bow, walls, AvatarRadius, WreckLayout.Bounds),
            $"{cause}: cannot reach the bow.");
        Assert.True(
            DeckReachability.CanReach(Spawn, stern, walls, AvatarRadius, WreckLayout.Bounds),
            $"{cause}: cannot reach the stern — the ship is sealed in half.");
    }

    [Theory]
    [MemberData(nameof(EveryCause))]
    public void TheShipIsWalkableWithROOM_NotMerelyPassable(Derelict.WreckCause cause)
    {
        // "A bit narrow but navigatable" is a bug report, not a compliment. Walk the whole ship again at
        // half again the captain's width: if a route only exists for a captain threading a needle, the
        // geometry is setting a puzzle nobody designed.
        IReadOnlyList<SurfaceCollision.Segment> walls = WreckLayout.Walls(cause);

        IReadOnlyList<string> tight = DeckReachability.Unreachable(
            Spawn, WreckLayout.Stations(cause), walls, ComfortRadius, WreckLayout.Bounds);

        Assert.True(
            tight.Count == 0,
            $"{cause}: only a thinner captain could reach {string.Join(", ", tight)} — the way there is too tight.");

        Assert.True(
            DeckReachability.CanReach(Spawn, new(-30, 0), walls, ComfortRadius, WreckLayout.Bounds),
            $"{cause}: the walk aft is passable but too narrow to be comfortable.");
    }

    [Theory]
    [MemberData(nameof(EveryCause))]
    public void EveryCompartmentCanBeEntered(Derelict.WreckCause cause)
    {
        // A named room the captain cannot get into is scenery pretending to be a place.
        IReadOnlyList<SurfaceCollision.Segment> walls = WreckLayout.Walls(cause);
        var unreachable = new List<string>();

        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            // Aim for the middle of the room, a comfortable way in from its bulkheads.
            var middle = new DeckReachability.Point((x0 + x1) / 2.0, top ? -6.0 : 6.0);
            if (!DeckReachability.CanReach(Spawn, middle, walls, AvatarRadius, WreckLayout.Bounds))
            {
                unreachable.Add(name);
            }
        }

        Assert.True(unreachable.Count == 0, $"{cause}: sealed compartments — {string.Join(", ", unreachable)}");
    }

    [Fact]
    public void TheSpawnIsSomewhereTheCaptainCanActuallyStand()
    {
        // An unwalkable spawn is worse than an unreachable room: the away team arrives inside a wall.
        foreach (Derelict.WreckCause c in System.Enum.GetValues<Derelict.WreckCause>())
        {
            Assert.True(
                DeckReachability.Standable(WreckLayout.SpawnX, WreckLayout.SpawnY, AvatarRadius, WreckLayout.Walls(c)),
                $"{c}: the away team spawns inside a wall.");
        }
    }

    // ── The geometry rules the audit exists to protect ────────────────────────────────────────────────

    [Fact]
    public void TheSpawnSitsInADoorway_SoTheFirstCompartmentIsOneStepAway()
    {
        Assert.Contains((float)WreckLayout.SpawnX, WreckLayout.DoorCentres());
    }

    [Fact]
    public void TheWayHomeIsClearOfTheSpawn_SoLeavingIsDeliberate()
    {
        // It sat 4 du from the spawn, inside the interact radius of the doorway the away team arrives in
        // and must pass through to reach anything — stepping bow-ward at all bounced them off the wreck.
        // An exit is somewhere you go on purpose, not something you fall through on your way past.
        const double interactRadius = 3.0;   // DeckPlan.InteractRadius
        double gap = System.Math.Abs(WreckLayout.ShuttleStation.X - WreckLayout.SpawnX);

        Assert.True(gap > interactRadius + 1.0, $"the exit is only {gap:0.0} du from the spawn");
    }

    [Fact]
    public void DoorwaysAreWiderThanTheCaptain()
    {
        // The first cut left a 0.6 du slot and the wreck was unwalkable. A doorway must be a passage.
        Assert.True(WreckLayout.DoorHalfWidth * 2 > AvatarRadius * 2 + 1.0);
    }

    [Fact]
    public void TheSpineIsNeverWalledEndToEnd()
    {
        // The generator once listed door centres bow-to-aft while the walk ran aft-to-bow, cutting exactly
        // one doorway and then re-walling over it with a REVERSED trailing segment. Segments must run
        // forward, and there must be a gap for every door.
        var runs = WreckLayout.SpineSegments().ToList();
        Assert.All(runs, r => Assert.True(r.X1 > r.X0, $"reversed spine segment ({r.X0}, {r.X1})"));

        foreach (float centre in WreckLayout.DoorCentres())
        {
            Assert.DoesNotContain(runs, r => centre > r.X0 && centre < r.X1);
        }
    }

    [Fact]
    public void TheEndsOfTheShipAreNotWalledOffStrips()
    {
        // Same dead-slot mistake as the gaps, but at the ends where it is easier to miss: the aft-most rooms
        // must reach the transom and the bow-most must stop where the hull tapers, or each end leaves a
        // sliver of ship enclosed by walls and reachable from nowhere.
        foreach (bool top in new[] { true, false })
        {
            var row = WreckLayout.Compartments.Where(c => c.Top == top).OrderBy(c => c.X0).ToList();
            Assert.Equal(WreckLayout.AftX, row[0].X0);
            Assert.Equal(WreckLayout.BowX - 6, row[^1].X1);
        }
    }

    [Fact]
    public void CompartmentsShareBulkheads_LeavingNoDeadSlots()
    {
        // Gaps between neighbouring compartments were 1 du slots, walled both sides and narrower than the
        // captain — traps with no way in that existed only to go wrong.
        foreach (bool top in new[] { true, false })
        {
            var row = WreckLayout.Compartments.Where(c => c.Top == top).OrderBy(c => c.X0).ToList();
            for (int i = 1; i < row.Count; i++)
            {
                Assert.Equal(row[i - 1].X1, row[i].X0);
            }
        }
    }

    [Fact]
    public void EveryCompartmentHasExactlyOneDoorwayOntoTheSpine()
    {
        // Four openings serve eight rooms — each one is the way into the compartment above it AND the one
        // below. The valve board draws its doors from this, so a room with two door centres in its span
        // would be drawn with a way through that the walls do not have, and a room with none would be shown
        // sealed shut while the captain walks straight in.
        foreach ((string name, float x0, float x1, bool _) in WreckLayout.Compartments)
        {
            int doors = WreckLayout.DoorCentres().Count(d => d > x0 && d < x1);
            Assert.True(doors == 1, $"{name} has {doors} doorways onto the spine, not 1");
        }
    }

    [Theory]
    [MemberData(nameof(EveryCause))]
    public void NoTwoStationsShareADoorstep(Derelict.WreckCause cause)
    {
        // TWO CONSOLES IN THE SAME PLACE IS ONE CONSOLE. The deck hands the captain whatever NearestConsole
        // picks, so the loser is unreachable and its label draws over the winner's — and nothing anywhere
        // complains. The scuttling panel was placed by eye in the renderer and landed exactly on the
        // infested hull's nest station; pressing E at the panel gave you the nest (owner: "I don't see the
        // scuttling panel here"). Writing this test then immediately found a SECOND one that had been
        // shipped for far longer: on a piracy wreck the cargo decision and the cause station were the same
        // point to the decimal.
        //
        // The bar is the interact radius: closer than that and the captain cannot choose between them.
        const double interactRadius = 3.0;   // DeckPlan.InteractRadius

        IReadOnlyList<(string Name, DeckReachability.Point At)> stations = WreckLayout.Stations(cause);

        for (int i = 0; i < stations.Count; i++)
        {
            for (int j = i + 1; j < stations.Count; j++)
            {
                double dx = stations[i].At.X - stations[j].At.X;
                double dy = stations[i].At.Y - stations[j].At.Y;
                double gap = System.Math.Sqrt((dx * dx) + (dy * dy));

                Assert.True(
                    gap > interactRadius,
                    $"{cause}: '{stations[i].Name}' and '{stations[j].Name}' are {gap:0.0} du apart — " +
                    "the captain cannot press one without the other.");
            }
        }
    }

    [Fact]
    public void TheShuttleLockSitsBetweenTheAwayTeamAndTheirRideHome()
    {
        // The team lands INSIDE the ship having already come through their own lock, so the bulkhead has to
        // be forward of the spawn and aft of the shuttle. Put it the wrong side of either and it is either
        // decoration or a wall across the way home.
        Assert.True(WreckLayout.ShuttleLockX > WreckLayout.SpawnX,
            "the lock is aft of the spawn — the away team would have to walk backwards through it");
        Assert.True(WreckLayout.ShuttleLockX < WreckLayout.ShuttleStation.X,
            "the lock is forward of the shuttle — it guards nothing");
    }

    [Fact]
    public void NothingUninvitedEverGetsPastTheLock()
    {
        // The promise: whatever is loose on that hull can reach the door and cannot open it. The bulkhead
        // has a passage in it — it must, or the away team could not get home — so walls alone would let the
        // pack walk it exactly as the captain does. This is the rule that stops them, and it is pinned HERE
        // rather than in a comment inside the walk loop, because "nothing follows you home" is exactly the
        // kind of promise a refactor breaks quietly and the owner discovers by watching it happen.
        const double radius = AvatarRadius;

        foreach (double wants in new[] { 21.5, 24.0, 26.0, 100.0 })
        {
            Assert.True(WreckLayout.PastTheLock(wants, radius), $"{wants} should be past the lock");
            Assert.False(
                WreckLayout.PastTheLock(WreckLayout.HeldAtLock(wants, radius), radius),
                $"a contact wanting x={wants} was not held on the ship's side of the lock");
        }
    }

    [Fact]
    public void TheLockNeverDragsAContactFORWARD()
    {
        // The clamp is a ceiling, not a teleport: something shambling around amidships must be left exactly
        // where it is. A min() written as a max() would haul the whole pack up to the shuttle door.
        foreach (double x in new[] { -30.0, -5.0, 0.0, 12.0, 19.0 })
        {
            Assert.Equal(x, WreckLayout.HeldAtLock(x, AvatarRadius));
        }
    }

    [Fact]
    public void TheCaptainCanStandWhereTheLockHoldsThePack()
    {
        // The held line has to be somewhere walkable, or contacts pile into a wall and the lane in front of
        // the lock — the one the captain retreats down — is not actually a lane.
        double held = WreckLayout.HeldAtLock(99, AvatarRadius);

        Assert.True(
            DeckReachability.Standable(held, 0, AvatarRadius, WreckLayout.Walls(Derelict.WreckCause.Infested)),
            "the pack is held against a position nothing can occupy");
    }

    [Fact]
    public void TheLockHasAPassageWideEnoughToWalkBadly()
    {
        // Same bar as every other doorway on this hull: passable is not the test, comfortable is.
        Assert.True(WreckLayout.ShuttleLockGapHalf * 2 > ComfortRadius * 2,
            "the lock passage is tighter than the comfort bar the rest of the ship is held to");
    }

    [Theory]
    [MemberData(nameof(EveryCause))]
    public void TheLockNeverBlocksTheWayHome(Derelict.WreckCause cause)
    {
        // It is a bulkhead across the ONLY corridor, which is exactly the shape of the bug that started
        // this whole audit. If the passage is ever cut wrong, the away team is sealed away from the shuttle
        // and the wreck becomes a tomb.
        Assert.True(
            DeckReachability.CanReach(
                Spawn,
                new(WreckLayout.ShuttleStation.X, WreckLayout.ShuttleStation.Y),
                WreckLayout.Walls(cause), ComfortRadius, WreckLayout.Bounds),
            $"{cause}: the away team cannot get back through their own lock.");
    }

    // ── The audit can actually fail (a guard that only ever passes guards nothing) ────────────────────

    [Fact]
    public void TheAuditCatchesAShipSealedInHalf()
    {
        // Rebuild the ORIGINAL bug — barricades spanning the full corridor — and prove the audit sees it.
        // Without this, a green suite might only mean the walk never says no.
        List<SurfaceCollision.Segment> walls = [.. WreckLayout.Walls(Derelict.WreckCause.DriveFailure)];
        walls.Add(new(-1f, -WreckLayout.SpineHalfHeight, -1f, WreckLayout.SpineHalfHeight));
        walls.Add(new(4f, -WreckLayout.SpineHalfHeight, 4f, WreckLayout.SpineHalfHeight));

        var stern = new DeckReachability.Point(-30, 0);

        Assert.False(
            DeckReachability.CanReach(Spawn, stern, walls, AvatarRadius, WreckLayout.Bounds),
            "the audit must FAIL on a ship sealed in half, or it is guarding nothing");
    }

    [Fact]
    public void APathIsReturnedSoAFailureIsDiagnosable()
    {
        // A* over a flood fill: the route is what makes a red test readable.
        DeckReachability.Walk walk = DeckReachability.Path(
            Spawn, new(-30, 0), WreckLayout.Walls(Derelict.WreckCause.Mutiny),
            AvatarRadius, WreckLayout.Bounds);

        Assert.True(walk.Reached);
        Assert.True(walk.Steps > 1);
        Assert.NotEmpty(walk.Path);
    }
}
