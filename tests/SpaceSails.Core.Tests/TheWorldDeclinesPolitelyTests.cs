using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1068 · <b>THE WORLD DECLINES POLITELY</b> — two of the watchers' three manifestation channels, under
/// #672's owner-blessed doctrine (2026-09-01): <i>"we may show wonders, but a Scully must always be able to
/// plausibly say no."</i> Subtraction (a door that opened yesterday does not) and structured instrument
/// failure (the scope's one-shot never completes on one contact only).
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names — the revert is
/// listed on each one, in the shape this ground has used since #587's lesson: <b>a guard that has never
/// failed is a guard nobody has checked</b>.</para>
///
/// <para>The vacuity pairs matter more here than anywhere, because this feature's ordinary answer is
/// NOTHING. A world where the register never fills would pass "no door is ever taken" perfectly, and every
/// negative law below therefore ships beside its positive twin — the population the positives run over is
/// DERIVED out of the generator and named in the failure message, never typed (the fifth named bug class:
/// a guard handed a world that cannot tell pass from fail).</para>
/// </summary>
public sealed class TheWorldDeclinesPolitelyTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>
    /// <b>A GROUND OF THIS SUITE'S OWN — never a shipped moon and never the cheat site.</b>
    ///
    /// <para>The decline register is AMBIENT and xUnit runs test classes in parallel, so a guard that
    /// declines on a site another class builds shuts a door underneath that class's own audit. That is not
    /// hypothetical: the first full run of this lane reddened
    /// <c>TheRingIsWalkableTests.EveryRingRoomIsReachedFromTheCarWithoutCrossingAnother</c>, a test with
    /// nothing whatever to do with this feature, because both suites were building the found-band cheat
    /// site at the same moment. <see cref="Burial"/>'s own register is safe as an ambient precisely because
    /// it only ever changes the answer for the ids IN it — and that argument only holds if the ids in it
    /// belong to nobody else.</para>
    ///
    /// <para><b>Searched, not typed.</b> Which probe has a concourse with a door to spare is a fact about
    /// the seed, so the seed is asked: the first <c>decline-probe-N</c> whose building actually gives up a
    /// leaf. A typed id would be an implementer's guess that could quietly stop qualifying.</para>
    /// </summary>
    private static readonly string Ground = AGroundOfOurOwn();

    private static string AGroundOfOurOwn()
    {
        for (int i = 0; i < 200; i++)
        {
            string body = $"decline-probe-{i}";
            bool takes = false;
            WithDeclined([new PoliteDecline.Decline(body, 0)], () =>
            {
                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    if (!UndergroundComplex.DeclinesOn(body, level))
                    {
                        continue;
                    }
                    takes |= UndergroundComplex.Build(body, level, Field).Doorways.Count
                        != BuildOpen(body, level).Doorways.Count;
                }
            });
            if (takes)
            {
                return body;
            }
        }

        Assert.Fail("no probe site in 200 has a concourse with a door to spare — this suite proves nothing.");
        return "";
    }

    /// <summary>The same floor with the register empty — the building the captain walked out of.</summary>
    private static UndergroundComplex.FloorPlan BuildOpen(string bodyId, int level)
    {
        UndergroundComplex.FloorPlan plan = default;
        WithDeclined([], () => plan = UndergroundComplex.Build(bodyId, level, Field));
        return plan;
    }


    /// <summary>Install a register, run the body, put back what was there. Every guard in this file goes
    /// through here: the ambient is shared with the whole Core suite and xUnit runs classes in parallel, so a
    /// guard that installed and forgot would be handing some other class's world a shut door.</summary>
    private static void WithDeclined(IReadOnlyList<PoliteDecline.Decline> declined, Action body)
    {
        IReadOnlyList<PoliteDecline.Decline> had = PoliteDecline.Declined;
        try
        {
            PoliteDecline.Install(declined);
            body();
        }
        finally
        {
            PoliteDecline.Install(had);
        }
    }

    /// <summary>The one floor of the site the world declines on — the concourse, asked of the building and
    /// asserted to be exactly one floor.</summary>
    private static int TheConcourse()
    {
        int? found = null;
        WithDeclined([new PoliteDecline.Decline(Ground, 0)], () =>
        {
            foreach (int level in UndergroundComplex.FloorsOf(Ground))
            {
                if (UndergroundComplex.DeclinesOn(Ground, level))
                {
                    Assert.True(found is null, $"{Ground} declines on two floors — one door, one floor.");
                    found = level;
                }
            }
        });
        Assert.True(found is not null, $"{Ground} declines on no floor at all.");
        return found!.Value;
    }

    /// <summary>Every listed floor of a site, top to bottom — the floors the subtraction may touch.</summary>
    private static IEnumerable<int> ListedFloors(string bodyId)
    {
        for (int level = -1; level >= UndergroundComplex.DepthOf(bodyId); level--)
        {
            yield return level;
        }
    }

    /// <summary>THE FLOOR THE WORLD TOOK A DOOR ON, and the plan before and after — derived by building the
    /// same floor twice, once with the register installed and once without, so every guard below compares a
    /// declined building against the very building it was made from rather than against a description of one.
    /// </summary>
    private static (int Level, UndergroundComplex.FloorPlan Open, UndergroundComplex.FloorPlan Shut)
        TheFloorAndItsTwin(string bodyId, long window)
    {
        int? level = null;
        WithDeclined([new PoliteDecline.Decline(bodyId, window)], () =>
        {
            foreach (int candidate in ListedFloors(bodyId))
            {
                if (UndergroundComplex.DeclinesOn(bodyId, candidate))
                {
                    Assert.True(level is null,
                        $"{bodyId} declines on B{-level} AND B{-candidate} — one door, one floor.");
                    level = candidate;
                }
            }
        });

        Assert.True(level is not null, $"{bodyId} named no floor to decline on.");

        UndergroundComplex.FloorPlan open = BuildOpen(bodyId, level!.Value);
        UndergroundComplex.FloorPlan shut = default;
        WithDeclined([new PoliteDecline.Decline(bodyId, window)],
            () => shut = UndergroundComplex.Build(bodyId, level.Value, Field));
        return (level.Value, open, shut);
    }

    /// <summary>THE WINDOW ON WHICH THIS SITE ACTUALLY LOSES A DOOR, derived rather than typed. Which floor
    /// the seed names is a fact about the seed, and a floor can legitimately have no door it can spare (every
    /// chamber on it is the canteen, the refuge, a gallery or a room whose second way the fire code needs) —
    /// in which case the world declines nothing on that ground for that window, which is the honest answer.
    /// The guards that need a REAL subtraction ask for one here and refuse to run on a world without one.
    /// </summary>
    private static long AWindowThatTakesADoor(string bodyId)
    {
        for (long window = 0; window < 64; window++)
        {
            (_, UndergroundComplex.FloorPlan open, UndergroundComplex.FloorPlan shut) =
                TheFloorAndItsTwin(bodyId, window);
            if (shut.Doorways.Count == open.Doorways.Count - 1)
            {
                return window;
            }
        }

        Assert.Fail($"no window in 64 took a door on {bodyId} — this suite would prove nothing.");
        return 0;
    }

    // ══ THE TRIGGER ═════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE WORLD NEVER DECLINES ON THE VISIT THAT OPENED THE GROUND — a whole world window has to pass first,
    /// and the vacuity twin proves the register can fill at all.
    ///
    /// <para>The reason is #672's, written beside the threshold in <see cref="PoliteDecline"/>: a door that
    /// had stopped opening by the time the captain climbed back out of the seam he had just crossed would be
    /// an answer to what he had just done, arriving inside the hour, from something that was watching him do
    /// it — which is a sensor return by another name.</para>
    ///
    /// <para><b>RED against:</b> <c>WindowsBeforeDeclining = 0</c> — <i>"the world declined on the opening
    /// window itself"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingDeclinesInsideTheWindowTheGroundWasOpenedIn()
    {
        double t = 12_345.0;
        long window = DisclosureClock.WindowAt(t);
        IReadOnlyList<DisclosureClock.Opening> opened = [new DisclosureClock.Opening(Ground, window)];

        Assert.Empty(PoliteDecline.Note(opened, [], standingOn: null, t));

        // …and the twin, so this cannot pass by the register being unfillable.
        double later = t + (PoliteDecline.WindowsBeforeDeclining * DisclosureClock.WindowSeconds)
            + DisclosureClock.WindowSeconds;
        IReadOnlyList<PoliteDecline.Decline> after =
            PoliteDecline.Note(opened, [], standingOn: null, later);
        Assert.Single(after);
        Assert.Equal(Ground, after[0].BodyId);
    }

    /// <summary>
    /// THE WORLD DECLINES NOWHERE A CAPTAIN HAS NOT BEEN — an empty clock register declines nothing however
    /// much time passes, and a ground that is not IN the register is not declined even when another one is.
    ///
    /// <para><b>RED against:</b> <c>Note</c> folding every body it is handed rather than only the opened
    /// ones — <i>"the world declined on a ground nobody has been under"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingDeclinesOnAGroundNobodyOpened()
    {
        double far = 400 * DisclosureClock.WindowSeconds;
        Assert.Empty(PoliteDecline.Note(null, [], standingOn: null, far));
        Assert.Empty(PoliteDecline.Note([], [], standingOn: null, far));

        IReadOnlyList<PoliteDecline.Decline> one =
            PoliteDecline.Note([new DisclosureClock.Opening(Ground, 0)], [], standingOn: null, far);
        Assert.Single(one);
        WithDeclined(one, () =>
        {
            Assert.True(PoliteDecline.On(Ground));
            Assert.False(PoliteDecline.On("luna"));
            foreach (int level in ListedFloors("luna"))
            {
                Assert.False(UndergroundComplex.DeclinesOn("luna", level));
            }
        });
    }

    /// <summary>
    /// NOT WHILE HE IS STANDING ON IT. A leaf that swung shut while the captain watched would be an act with
    /// a witness, and an act with a witness is a thing he could describe.
    ///
    /// <para><b>RED against:</b> deleting the <c>standingOn</c> clause from <c>Note</c> —
    /// <i>"the world declined under the captain's own boots"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingDeclinesUnderTheCaptainsOwnBoots()
    {
        double far = 400 * DisclosureClock.WindowSeconds;
        IReadOnlyList<DisclosureClock.Opening> opened = [new DisclosureClock.Opening(Ground, 0)];

        Assert.Empty(PoliteDecline.Note(opened, [], standingOn: Ground, far));
        Assert.Single(PoliteDecline.Note(opened, [], standingOn: "luna", far));
    }

    /// <summary>
    /// A GROUND DECLINES ONCE AND STAYS DECLINED — the window first written is the one that counts, and the
    /// register is handed back BY REFERENCE when there is nothing to add, so a caller can compare and only
    /// then ask for a save.
    ///
    /// <para>This is the load-bearing half of the whole beat: the door is chosen against that window, so a
    /// register that re-noted a ground would shut a different leaf on the next visit — a lock that moved by
    /// itself, which is an event, which is a fact about somebody deciding.</para>
    ///
    /// <para><b>RED against:</b> <c>Note</c> appending unconditionally (dropping the already-declined check)
    /// — <i>"the same ground declined twice, in two different windows"</i>.</para>
    /// </summary>
    [Fact]
    public void AGroundDeclinesOnceAndTheWindowNeverMoves()
    {
        IReadOnlyList<DisclosureClock.Opening> opened = [new DisclosureClock.Opening(Ground, 0)];
        double first = 400 * DisclosureClock.WindowSeconds;

        IReadOnlyList<PoliteDecline.Decline> once =
            PoliteDecline.Note(opened, [], standingOn: null, first);
        Assert.Single(once);

        IReadOnlyList<PoliteDecline.Decline> again = PoliteDecline.Note(
            opened, once, standingOn: null, first + (50 * DisclosureClock.WindowSeconds));
        Assert.Same(once, again);
        Assert.Equal(once[0].Window, again[0].Window);
    }

    // ══ CHANNEL 1 · SUBTRACTION ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE SAME DOOR IS SHUT ON EVERY VISIT. A captain who comes back a third time finds the same leaf, in
    /// the same wall, wearing the same plate — which is what a locked door IS, and what a die would not be.
    /// Nothing in the choice is effort: it is (ground, window) and the floor's own candidate list.
    ///
    /// <para><b>RED against:</b> seeding the door on a call counter instead of the window —
    /// <i>"the floor came back with a different door shut on the second build"</i>.</para>
    /// </summary>
    [Fact]
    public void TheSameDoorIsShutOnEveryVisit()
    {
        long window = AWindowThatTakesADoor(Ground);
        (int level, _, UndergroundComplex.FloorPlan first) = TheFloorAndItsTwin(Ground, window);

        for (int again = 0; again < 3; again++)
        {
            UndergroundComplex.FloorPlan next = default;
            WithDeclined([new PoliteDecline.Decline(Ground, window)],
                () => next = UndergroundComplex.Build(Ground, level, Field));
            Assert.Equal(first.Locked.Count, next.Locked.Count);
            Assert.Equal(first.Locked[^1], next.Locked[^1]);
            Assert.Equal(first.Doorways.Count, next.Doorways.Count);
        }
    }

    /// <summary>
    /// WHAT WAS A WAY THROUGH IS NOW AN ORDINARY LOCKED LEAF — the same
    /// <see cref="UndergroundComplex.LockedDoor"/> idiom the building has hung forty of on every floor since
    /// #585, in the same wall, wearing the plate the room already had. <b>No new sign is authored anywhere.
    /// </b>
    ///
    /// <para>Exactly one doorway leaves the plan, exactly one locked leaf arrives, they are the same segment,
    /// and the room it belonged to loses that way out of <c>Room.Ways</c> — because a published way that is
    /// not a way is the drawn world and the sim disagreeing, which is a named bug class on this ground.</para>
    ///
    /// <para><b>RED against:</b> removing the <c>doorways.RemoveAt(inPlan)</c> line — <i>"the plan drew the
    /// leaf both open and locked"</i> — and separately against dropping the <c>Room.Ways</c> rewrite —
    /// <i>"a room published a way out through a locked door"</i>.</para>
    /// </summary>
    [Fact]
    public void TheTakenDoorIsAnOrdinaryLockedLeafWearingTheRoomsOwnPlate()
    {
        long window = AWindowThatTakesADoor(Ground);
        (_, UndergroundComplex.FloorPlan open, UndergroundComplex.FloorPlan shut) =
            TheFloorAndItsTwin(Ground, window);

        Assert.Equal(open.Doorways.Count - 1, shut.Doorways.Count);
        Assert.Equal(open.Locked.Count + 1, shut.Locked.Count);

        UndergroundComplex.LockedDoor taken = shut.Locked[^1];
        Assert.False(string.IsNullOrEmpty(taken.Sign));

        // The leaf that left the doorway list is the leaf that arrived in the locked list.
        SurfaceLayout.Doorway gone = open.Doorways.Single(
            d => !shut.Doorways.Any(s => Same(s, d)));
        Assert.Equal(gone.X1, taken.X1, 9);
        Assert.Equal(gone.Y1, taken.Y1, 9);
        Assert.Equal(gone.X2, taken.X2, 9);
        Assert.Equal(gone.Y2, taken.Y2, 9);

        // …and it wears the plate of the room it belonged to, which is now a room with one way fewer.
        UndergroundComplex.Room was = open.TheRooms.Single(r => r.Ways.Any(w => Same(w, gone)));
        Assert.Equal(was.Plate, taken.Sign);

        UndergroundComplex.Room now = shut.TheRooms.Single(
            r => r.X0 == was.X0 && r.Y0 == was.Y0 && r.X1 == was.X1 && r.Y1 == was.Y1);
        Assert.Equal(was.Ways.Count - 1, now.Ways.Count);
        Assert.DoesNotContain(now.Ways, w => Same(w, gone));
    }

    /// <summary>
    /// THE WORLD NEVER TAKES A DOOR A ROOM CANNOT SPARE. Every carved space on the declined floor still
    /// satisfies the standing fire code (<see cref="UndergroundComplex.Room.MeetsFireCode"/>) — two ways out,
    /// or bedroom-small — so the captain is never shut in.
    ///
    /// <para>And never the refuge, never an amenity, never the lift's own alcove and never the specimen
    /// recess: the first two are asserted here against the floor's own published lists, and the last two
    /// cannot be reached at all because neither is a room. Note the sweep runs over EVERY listed floor and
    /// not only the declined one, so a bug that shut a door on the wrong floor fails here too.</para>
    ///
    /// <para><b>RED against:</b> dropping the <c>MeetsFireCode</c> clause from <c>CanBeSpared</c> —
    /// <i>"a room was left with one way out"</i> — and separately against dropping the refuge/amenity
    /// clauses, which shuts the air rack's own door on an airless floor.</para>
    /// </summary>
    [Fact]
    public void TheWorldNeverTakesTheOnlyWayOutOfAnywhere()
    {
        int swept = 0, doorsTaken = 0;

        // SWEPT OVER MANY WINDOWS, not the one window that happens to take a door. Which leaf the seed picks
        // is a fact about the window, so a guard that asked once would be asking about one of two dozen
        // candidates and would pass on a rule that lets the other twenty-three seal a captain in. Watched
        // exactly that: with the third-door clause deleted, a single-window guard stayed green.
        for (long window = 0; window < 48; window++)
        {
            WithDeclined([new PoliteDecline.Decline(Ground, window)], () =>
            {
                foreach (int level in ListedFloors(Ground))
                {
                    UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(Ground, level, Field);
                    foreach (UndergroundComplex.Room room in floor.TheRooms)
                    {
                        swept++;
                        Assert.True(room.MeetsFireCode,
                            $"window {window}, {Ground} B{-level}: '{room.Plate}' was left with "
                            + $"{room.Exits} way(s) out.");

                        // …and never sealed outright, which MeetsFireCode lets a booth do: its own exemption
                        // is about how far you are from your one exit and has nothing to say about somebody
                        // taking it away.
                        Assert.True(room.Exits > 0,
                            $"window {window}, {Ground} B{-level}: '{room.Plate}' was sealed shut.");
                    }

                    foreach (UndergroundComplex.LockedDoor leaf in floor.Locked)
                    {
                        double mx = (leaf.X1 + leaf.X2) / 2.0, my = (leaf.Y1 + leaf.Y2) / 2.0;
                        foreach (UndergroundComplex.Refuge r in floor.Refuges)
                        {
                            Assert.True(Math.Abs(r.X - mx) > 1e-6 || Math.Abs(r.Y - my) > 1e-6,
                                $"window {window}: the world shut the refuge's own door.");
                        }
                    }
                }

                doorsTaken += UndergroundComplex.Build(Ground, TheConcourse(), Field).Doorways.Count;
            });
        }

        Assert.True(swept > 2_000, $"the fire-code sweep only saw {swept} room(s) — this proves little.");
        Assert.True(doorsTaken > 0, "the sweep never built the concourse.");
    }

    /// <summary>
    /// THE ROOM THE DOOR BELONGED TO IS NEVER THE REFUGE AND NEVER AN AMENITY. Asked of the declined floor
    /// directly rather than inferred: the box the taken leaf hangs on holds no air rack and no counter.
    ///
    /// <para><b>RED against:</b> deleting the two containment loops in <c>CanBeSpared</c> — on a site whose
    /// declined floor carries a refuge, <i>"the world shut the door of the room with the air in it"</i>.
    /// </para>
    /// </summary>
    [Fact]
    public void TheTakenDoorIsNeverTheRefugeAndNeverAnAmenity()
    {
        int level = TheConcourse();
        UndergroundComplex.FloorPlan open = BuildOpen(Ground, level);
        int checkedWindows = 0;

        // Swept over many windows for the reason the fire-code guard is: which leaf the seed picks is a fact
        // about the window, and one window is one of two dozen candidates.
        for (long window = 0; window < 48; window++)
        {
            UndergroundComplex.FloorPlan shut = default;
            WithDeclined([new PoliteDecline.Decline(Ground, window)],
                () => shut = UndergroundComplex.Build(Ground, level, Field));
            if (shut.Doorways.Count != open.Doorways.Count - 1)
            {
                continue;
            }

            SurfaceLayout.Doorway gone = open.Doorways.Single(d => !shut.Doorways.Any(s => Same(s, d)));
            UndergroundComplex.Room was = open.TheRooms.Single(r => r.Ways.Any(w => Same(w, gone)));
            checkedWindows++;

            Assert.True(was.Ways.Count - 1 >= UndergroundComplex.FireCodeMinExits,
                $"window {window}: the world took a door '{was.Plate}' could not spare "
                + $"({was.Ways.Count} way(s)).");
            foreach (UndergroundComplex.Refuge r in open.Refuges)
            {
                Assert.False(was.Contains(r.X, r.Y), $"window {window}: the world shut the refuge's door.");
            }
            foreach (UndergroundComplex.Amenity a in open.Amenities)
            {
                Assert.False(was.Contains(a.X, a.Y), $"window {window}: the world shut an amenity's door.");
            }
        }

        Assert.True(checkedWindows > 20,
            $"only {checkedWindows} window(s) took a door — this proves little about which door.");
    }

    /// <summary>
    /// ONE LEAF, AND NOTHING ELSE ON THE FLOOR MOVES. The declined building is the building the captain
    /// walked out of, down to the poster on the wall beside the door: same walls, same room boxes, same room
    /// centres in the same order, same amenities, same refuges, same bins, same specimen.
    ///
    /// <para>This is the guard that makes the beat readable at all. If shutting a door re-seeded the floor,
    /// a player would come back to forty differences and no way to tell which one was the world declining —
    /// and it is also why the subtraction is a post-pass rather than a flag inside the carve.</para>
    ///
    /// <para><b>RED against:</b> moving the decline into <c>AddRoomsAlong</c>'s <c>shut</c> flag —
    /// <i>"the room centres came back in a different order and the canteen moved"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingBUTTheLeafChanges()
    {
        long window = AWindowThatTakesADoor(Ground);
        (int level, UndergroundComplex.FloorPlan open, UndergroundComplex.FloorPlan shut) =
            TheFloorAndItsTwin(Ground, window);
        string where = $"{Ground} B{-level}";

        Assert.Equal(open.Walls.Count, shut.Walls.Count);
        Assert.Equal(open.RoomCentres, shut.RoomCentres);
        Assert.Equal(open.Amenities.Count, shut.Amenities.Count);
        Assert.Equal(open.Refuges.Count, shut.Refuges.Count);
        Assert.Equal(open.TheBins.Count, shut.TheBins.Count);
        Assert.Equal(open.TheRooms.Count, shut.TheRooms.Count);
        Assert.Equal(open.TheWalls.Count, shut.TheWalls.Count);
        Assert.Equal(open.Specimen, shut.Specimen);

        int movedBoxes = 0;
        for (int i = 0; i < open.TheRooms.Count; i++)
        {
            UndergroundComplex.Room a = open.TheRooms[i], b = shut.TheRooms[i];
            if (a.X0 != b.X0 || a.Y0 != b.Y0 || a.X1 != b.X1 || a.Y1 != b.Y1
                || !string.Equals(a.Plate, b.Plate, StringComparison.Ordinal) || a.Kind != b.Kind)
            {
                movedBoxes++;
            }
        }
        Assert.True(movedBoxes == 0, $"{where}: {movedBoxes} room(s) moved when one door was shut.");
    }

    /// <summary>
    /// AND ON EVERY GROUND THE WORLD HAS NOT DECLINED ON, NOTHING HAPPENS AT ALL — the vacuity twin of every
    /// subtraction guard above, run over the shipped moons with an empty register.
    ///
    /// <para><b>RED against:</b> <c>DeclineOneDoor</c> ignoring <c>FloorOn</c>'s null —
    /// <i>"luna B1 came back with a door shut in a world where nobody has been anywhere"</i>.</para>
    /// </summary>
    [Fact]
    public void AWorldNobodyHasOpenedLosesNoDoors()
    {
        string[] moons = ["luna", "phobos", "europa", "ganymede", "callisto", "titan", "triton", Ground];
        int floors = 0;

        WithDeclined([], () =>
        {
            foreach (string moon in moons)
            {
                foreach (int level in UndergroundComplex.FloorsOf(moon))
                {
                    UndergroundComplex.FloorPlan bare = UndergroundComplex.Build(moon, level, Field);
                    UndergroundComplex.FloorPlan again = UndergroundComplex.Build(moon, level, Field);
                    Assert.Equal(bare.Doorways.Count, again.Doorways.Count);
                    Assert.Equal(bare.Locked.Count, again.Locked.Count);
                    floors++;
                }
                foreach (int level in ListedFloors(moon))
                {
                    Assert.False(UndergroundComplex.DeclinesOn(moon, level));
                }
            }
        });

        Assert.True(floors > 60, $"the vacuity sweep only walked {floors} floor(s).");
    }

    // ══ CHANNEL 2 · STRUCTURED INSTRUMENT FAILURE ═══════════════════════════════════════════════════════

    /// <summary>Three moons in a row, a hundred gigametres apart, so a disc over one of them cannot
    /// accidentally hold another.</summary>
    private sealed class ThreeMoons : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } =
        [
            new CelestialBody(Ground, "A", null, 0, 1e6, 0, 0, 0),
            new CelestialBody("second-moon", "B", null, 0, 1e6, 1e11, 0, 0),
            new CelestialBody("third-moon", "C", null, 0, 1e6, 2e11, 0, 0),
        ];

        public Vector2d Position(string bodyId, double simTime) =>
            new(Bodies.First(b => b.Id == bodyId).OrbitRadius, 0);
    }

    /// <summary>
    /// THE SCOPE'S ONE-SHOT BRINGS NOTHING BACK ON THAT ONE GROUND ONLY. Every other contact in the same
    /// world behaves; the standing custody carousel is untouched; a zero-radius look at a hull is untouched.
    ///
    /// <para>#672: <i>"the sensor law survives because the sensor's failure IS the manifestation."</i> There
    /// is nothing to return and therefore nothing returned — no fault, no state, no caption. The predicate is
    /// the only thing this channel has, and what it must NOT do is nearly all of it.</para>
    ///
    /// <para><b>RED against:</b> dropping the <c>WindowOn(declined, body.Id) is null</c> continue —
    /// <i>"the pass over the second moon did not land either"</i> — and separately against dropping the
    /// <c>task.Recurring</c> clause, <i>"the standing custody pass stopped landing"</i>.</para>
    /// </summary>
    [Fact]
    public void TheGlassBringsNothingBackOnOneGroundOnly()
    {
        var sky = new ThreeMoons();
        const double radius = 1e10;

        WithDeclined([new PoliteDecline.Decline(Ground, 0)], () =>
        {
            foreach (CelestialBody body in sky.Bodies)
            {
                SensorTask look = SensorTask.AreaScan(sky.Position(body.Id, 0), radius, "vicinity");
                bool blank = PoliteDecline.BringsNothingBack(look, sky, 0);
                Assert.True(blank == (body.Id == Ground),
                    $"the one-shot over {body.Id} {(blank ? "did not land" : "landed")} — "
                    + $"only {Ground} declines.");
            }

            // The standing carousel is not the captain's one-shot and is never taken.
            Assert.False(PoliteDecline.BringsNothingBack(
                SensorTask.CorridorSweep("a", "b", "lane watch", recurring: true), sky, 0));
            Assert.False(PoliteDecline.BringsNothingBack(
                SensorTask.TrackUpdate("hull-1", "custody"), sky, 0));

            // …nor is a directed look at a hull: what declines is a GROUND.
            Assert.False(PoliteDecline.BringsNothingBack(
                SensorTask.SharpenFix("hull-1", "sharpen"), sky, 0));
        });

        // And the vacuity twin: in a world the watchers have declined nowhere, the very same order lands.
        WithDeclined([], () => Assert.False(PoliteDecline.BringsNothingBack(
            SensorTask.AreaScan(sky.Position(Ground, 0), radius, "vicinity"), sky, 0)));
    }

    /// <summary>
    /// A PASS AIMED SOMEWHERE ELSE LANDS, EVEN ON A DECLINED GROUND'S OWN MOON. The disc has to actually hold
    /// the body — the same containment the wreck reveal already uses — so a captain sweeping the sky beside a
    /// declined moon is not quietly blinded.
    ///
    /// <para><b>RED against:</b> replacing the containment test with <c>true</c> —
    /// <i>"a scan a hundred gigametres off the declined moon returned nothing"</i>.</para>
    /// </summary>
    [Fact]
    public void APassAimedElsewhereStillLands()
    {
        var sky = new ThreeMoons();
        WithDeclined([new PoliteDecline.Decline(Ground, 0)], () =>
        {
            Vector2d here = sky.Position(Ground, 0);
            Assert.True(PoliteDecline.BringsNothingBack(
                SensorTask.AreaScan(here, 1e10, "on it"), sky, 0));
            Assert.False(PoliteDecline.BringsNothingBack(
                SensorTask.AreaScan(here + new Vector2d(5e10, 0), 1e9, "beside it"), sky, 0));
        });
    }

    // ══ #672's LAWS, SWEPT ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// NEITHER CHANNEL PUBLISHES ONE STRING — no label, no line, no title, no caption, not one word. That is
    /// #672's <i>"no dialog explaining a declined door"</i> in its strongest available form, and it settles
    /// §8's reserved word for free: <b>a type with no strings in it cannot contain the reserved word.</b>
    /// The declined leaf's own sign is the plate the room already had, authored by the signage generator
    /// years before this feature existed.
    ///
    /// <para>Coverage floor included, exactly as <c>TheDisclosureClockTests</c> does it, so a rename that
    /// emptied the type could not turn this green by having nothing left to check.</para>
    ///
    /// <para><b>RED against:</b> adding <c>public const string Label = "◷ DECLINED";</c> to
    /// <see cref="PoliteDecline"/>.</para>
    /// </summary>
    [Fact]
    public void NeitherChannelPublishesAnyProseAtAll()
    {
        Type channels = typeof(PoliteDecline);
        var offenders = new List<string>();
        int surface = 0;

        const BindingFlags Public = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        foreach (FieldInfo f in channels.GetFields(Public))
        {
            surface++;
            if (f.FieldType == typeof(string))
            {
                offenders.Add($"field {f.Name}");
            }
        }
        foreach (PropertyInfo p in channels.GetProperties(Public))
        {
            surface++;
            if (p.PropertyType == typeof(string))
            {
                offenders.Add($"property {p.Name}");
            }
        }
        foreach (MethodInfo m in channels.GetMethods(Public))
        {
            surface++;
            if (m.ReturnType == typeof(string) && !m.IsSpecialName && m.DeclaringType == channels)
            {
                offenders.Add($"method {m.Name}");
            }
        }

        Assert.True(surface >= 8, $"the channels' public surface is only {surface} member(s) — nothing swept.");
        Assert.True(offenders.Count == 0,
            "a declined door is never explained, so neither channel publishes prose. Found: "
            + string.Join(", ", offenders));
    }

    /// <summary>
    /// §8's RESERVED WORD IS ABSENT FROM EVERYTHING THIS FEATURE CAN PUT ON SCREEN — which, the two channels
    /// having no prose of their own, means the plate on the leaf the world took. It is the room's own
    /// stencilled sign and it is swept here anyway, because "we did not author it" is not the same statement
    /// as "it does not say it".
    ///
    /// <para><b>RED against:</b> planting the word in the taken leaf's <c>Sign</c> —
    /// <i>"the declined leaf names the reserved object"</i>.</para>
    /// </summary>
    [Fact]
    public void TheDeclinedLeafNeverNamesTheReservedThing()
    {
        string[] forbidden =
        [
            "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
            "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
        ];

        long window = AWindowThatTakesADoor(Ground);
        (_, _, UndergroundComplex.FloorPlan shut) = TheFloorAndItsTwin(Ground, window);
        string sign = shut.Locked[^1].Sign;

        foreach (string word in forbidden)
        {
            Assert.False(sign.Contains(word, StringComparison.OrdinalIgnoreCase),
                $"the declined leaf's plate names the reserved thing: '{sign}'.");
        }
    }

    private static bool Same(in SurfaceLayout.Doorway a, in SurfaceLayout.Doorway b) =>
        Math.Abs(a.X1 - b.X1) < 1e-9 && Math.Abs(a.Y1 - b.Y1) < 1e-9
        && Math.Abs(a.X2 - b.X2) < 1e-9 && Math.Abs(a.Y2 - b.Y2) < 1e-9;
}
