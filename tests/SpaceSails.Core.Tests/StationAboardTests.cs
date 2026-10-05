using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #653 slice 1 · THE PURE HALF OF WALKING A DEAD STATION — the body id, the canon, and where the boat's own
/// fixtures stand. The geometry is audited with A* over <see cref="StationWreck.Walls"/> itself, so what is
/// proved here is what the captain walks.
///
/// <para>Every sweep runs over <see cref="Stations"/> — dozens of ids, not the one the dev start boots — because
/// the seeded severing and the seeded locks decide WHICH fixtures exist, and a guard that passes on one seed has
/// proved a seed.</para>
/// </summary>
public class StationAboardTests
{
    private const double AvatarRadius = 0.7;

    /// <summary>Forty ids: every seeded combination of severed arms and lock kinds turns up in a sweep this wide.</summary>
    private static readonly string[] Stations =
        [.. Enumerable.Range(1, 40).Select(i => $"audit-station-{i}"), "dev-station-6", "kepler-transfer"];

    // ── The body id ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ABodyIdRoundTripsAndOnlyAStationParses()
    {
        foreach (string id in Stations)
        {
            Assert.True(StationAboard.TryParseStationId(StationAboard.BodyIdFor(id), out string back));
            Assert.Equal(id, back);
        }

        // Not stations: a wreck, the bare prefix, nothing, a body that merely contains the word.
        Assert.False(StationAboard.TryParseStationId(Derelict.BodyIdFor("kestrel-3"), out _));
        Assert.False(StationAboard.TryParseStationId(StationAboard.BodyIdPrefix, out _));
        Assert.False(StationAboard.TryParseStationId(null, out _));
        Assert.False(StationAboard.TryParseStationId("a-station-x", out _));
    }

    [Fact]
    public void ADeadStationIsADeadHullToTheInstrumentsButNotADerelict()
    {
        string body = StationAboard.BodyIdFor("dev-station-6");
        Assert.True(Derelict.IsWreckBody(body));
        Assert.False(Derelict.TryParseWreckId(body, out _));
    }

    // ── The canon ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The issue's own words (#653, "CANON: slice 1's lines"), typed from the issue. Pinned because the
    /// lane's rule is that Core's lines stand and the canon fills only the gaps — so a drift in either is a
    /// deviation from Fable's text, not a refactor.</summary>
    [Fact]
    public void TheCanonIsVerbatim()
    {
        Assert.Equal("HUB — TRANSFERS & TALLY", StationAboard.PlateOf(StationWreck.ModuleId.Hub));
        Assert.Equal("HABITAT — 40 BERTHS, KEEP IT DOWN", StationAboard.PlateOf(StationWreck.ModuleId.Habitat));
        Assert.Equal("FOUNDRY — EAR PROTECTION PAST THIS LINE", StationAboard.PlateOf(StationWreck.ModuleId.Foundry));
        Assert.Equal("DOCKING — DECLARE BEFORE YOU BERTH", StationAboard.PlateOf(StationWreck.ModuleId.Docking));
        Assert.Equal("REACTOR — TWO-MAN RULE, NO EXCEPTIONS", StationAboard.PlateOf(StationWreck.ModuleId.Reactor));

        Assert.StartsWith("Your lamp is the only thing with an opinion.", StationAboard.FirstStandingLine);
        Assert.EndsWith("every door exactly as somebody left it.", StationAboard.FirstStandingLine);
        Assert.StartsWith("A dead station, tubes severed, books balanced.", StationAboard.FieldBookLine);
        Assert.EndsWith("which is two strange things, not one.", StationAboard.FieldBookLine);
        Assert.StartsWith("The lock is a standard pattern, forty years polite.", StationAboard.LockLine);
        Assert.EndsWith("patience is the one thing aboard in quantity.", StationAboard.LockLine);
        Assert.StartsWith("The face comes away clean.", StationAboard.CutFaceLine);
        Assert.EndsWith("That's the part you file.", StationAboard.CutFaceLine);
    }

    [Fact]
    public void NoCanonLineStatesACauseOrWhetherAnyoneRemained()
    {
        // §13.8 discipline for dead infrastructure: the station explains nothing, ever.
        string[] lines =
        [
            StationAboard.FirstStandingLine, StationAboard.FieldBookLine, StationAboard.LockLine,
            StationAboard.CutFaceLine,
            .. StationWreck.Modules.Select(m => StationAboard.PlateOf(m.Id)),
        ];
        foreach (string banned in new[] { "because", "died", "killed", "survivor", "alive", "corpse", "crew died", "murder" })
        {
            foreach (string line in lines)
            {
                Assert.DoesNotContain(banned, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── Where the boat's fixtures stand ──────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryDockFixtureOfEveryDockCanBeStoodAtAndWalkedToFromTheArrivalSquare()
    {
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            foreach (StationWreck.Access dock in StationWreck.Accesses(id))
            {
                (double sx, double sy) = StationWreck.SpawnFor(dock);
                Assert.True(DeckReachability.Standable(sx, sy, AvatarRadius, walls),
                    $"{id}/{dock.Module}: the arrival square is inside a wall.");

                for (int slot = 0; slot <= StationAboard.MostDestinations; slot++)
                {
                    (double fx, double fy) = StationAboard.DockFixtureAt(dock, slot);
                    var at = new DeckReachability.Point(fx, fy);
                    Assert.True(DeckReachability.CanReach(new(sx, sy), at, walls, AvatarRadius, StationWreck.Bounds, 0.5),
                        $"{id}/{dock.Module}: fixture {slot} at ({fx:0.##}, {fy:0.##}) cannot be walked to.");

                    // …and it is inside the module the dock belongs to, not out in a tube or the void.
                    Assert.Equal(dock.Module, StationAboard.ModuleAt(fx, fy)?.Id);
                }
            }
        }
    }

    [Fact]
    public void DockFixturesStayClearOfEachOtherAndOfTheTubeEndsSoNoLabelSitsOnAnother()
    {
        // The console-crowding law's own clearance is 2.0 du.
        foreach (string id in Stations)
        {
            var tubeEnds = StationWreck.SeveredTubes(id)
                .SelectMany(a => StationAboard.TubeEnds(a)).ToArray();

            foreach (StationWreck.Access dock in StationWreck.Accesses(id))
            {
                // Only the slots this dock actually uses: the hub can lose at most three arms, so its fourth
                // slot is never drawn (and would sit on the Reactor tube's hub-side fixture).
                int used = StationHop.Destinations(id, dock.Module).Count;
                var points = Enumerable.Range(0, used + 1)
                    .Select(s => StationAboard.DockFixtureAt(dock, s))
                    .Concat(tubeEnds.Select(t => (t.X, t.Y))).ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    for (int j = i + 1; j < points.Length; j++)
                    {
                        double apart = Math.Sqrt(
                            Math.Pow(points[i].Item1 - points[j].Item1, 2) + Math.Pow(points[i].Item2 - points[j].Item2, 2));
                        Assert.True(apart >= 2.0,
                            $"{id}/{dock.Module}: fixtures {i} and {j} are {apart:0.00} du apart.");
                    }
                }
            }
        }
    }

    [Fact]
    public void ASeveredTubesTwoEndsAreStandableAndReadableFromTheirOwnSideOnly()
    {
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            StationWreck.Access hub = StationAboard.AccessOf(id, StationWreck.ModuleId.Hub);

            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                StationWreck.Access armLock = StationAboard.AccessOf(id, arm);
                foreach ((StationWreck.ModuleId side, double x, double y) in StationAboard.TubeEnds(arm))
                {
                    Assert.True(DeckReachability.Standable(x, y, AvatarRadius, walls), $"{id}/{arm}: the {side} end is in a wall.");
                    Assert.Equal(arm, StationAboard.ArmOfTubeEnd(x, y));

                    // Walkable from the side it belongs to…
                    (double sx, double sy) = StationWreck.SpawnFor(side == StationWreck.ModuleId.Hub ? hub : armLock);
                    Assert.True(DeckReachability.CanReach(new(sx, sy), new(x, y), walls, AvatarRadius, StationWreck.Bounds, 0.5),
                        $"{id}/{arm}: the {side} end of the tube cannot be walked to.");

                    // …and NOT from the other: that is what makes it a severed tube and not a corridor.
                    (double ox, double oy) = StationWreck.SpawnFor(side == StationWreck.ModuleId.Hub ? armLock : hub);
                    Assert.False(DeckReachability.CanReach(new(ox, oy), new(x, y), walls, AvatarRadius, StationWreck.Bounds, 0.5),
                        $"{id}/{arm}: the {side} end of a SEVERED tube is reachable from across it.");
                }
            }
        }
    }

    [Fact]
    public void AnythingThatIsNotATubeEndIsNoArm()
    {
        Assert.Null(StationAboard.ArmOfTubeEnd(0, 0));
        Assert.Null(StationAboard.ArmOfTubeEnd(-9.8, 4.95));
    }

    // ── Destinations: the press and the label can never name different places ───────────────────────────

    [Fact]
    public void EveryDestinationIsFoundAgainFromItsFixtureAndNothingElseIs()
    {
        int checkedOne = 0;
        foreach (string id in Stations)
        {
            foreach (StationWreck.Module m in StationWreck.Modules)
            {
                StationWreck.Access dock = StationAboard.AccessOf(id, m.Id);
                var destinations = StationHop.Destinations(id, m.Id);
                Assert.InRange(destinations.Count, 1, StationAboard.MostDestinations);

                for (int i = 0; i < destinations.Count; i++)
                {
                    (double fx, double fy) = StationAboard.DockFixtureAt(dock, i + 1);
                    var found = StationAboard.DestinationAt(id, m.Id, null, fx, fy);
                    Assert.Equal(destinations[i].Module, found?.Module);
                    Assert.Equal(destinations[i].Quote, found?.Quote);
                    checkedOne++;
                }

                // The boat's own lock (slot 0) and a slot past the last destination name nothing.
                (double lx, double ly) = StationAboard.DockFixtureAt(dock, 0);
                Assert.Null(StationAboard.DestinationAt(id, m.Id, null, lx, ly));
                (double ex, double ey) = StationAboard.DockFixtureAt(dock, destinations.Count + 1);
                Assert.Null(StationAboard.DestinationAt(id, m.Id, null, ex, ey));
            }
        }
        Assert.True(checkedOne > 100, "the sweep found almost no destinations — it proves little.");
    }

    [Fact]
    public void AHopFromTheHubGroupNamesExactlyTheSeveredArmsAndAnArmNamesTheHubAndTheRest()
    {
        foreach (string id in Stations)
        {
            var fromHub = StationHop.Destinations(id, StationWreck.ModuleId.Hub).Select(d => d.Module).ToHashSet();
            Assert.True(fromHub.SetEquals(StationWreck.SeveredTubes(id)),
                $"{id}: a flight from the hub should name exactly the arms the tubes will not let you walk to.");

            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                var fromArm = StationHop.Destinations(id, arm).Select(d => d.Module).ToHashSet();
                Assert.Equal(StationWreck.Modules.Count - 1, fromArm.Count);
                Assert.DoesNotContain(arm, fromArm);
            }
        }
    }

    // ── The lock is folded into the price ────────────────────────────────────────────────────────────────

    [Fact]
    public void ALockedArrivalPaysTheLockOnTopOfTheFlightAndNothingElseDoes()
    {
        foreach (string id in Stations)
        {
            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                StationWreck.Access arrival = StationAboard.AccessOf(id, arm);
                var allCut = new[] { arm };
                StationHop.Quote flown = StationHop.QuoteHop(id, StationWreck.ModuleId.Hub, arm, allCut);
                Assert.Equal(StationHop.Refusal.None, flown.Refused);

                StationHop.Quote priced = StationAboard.WithTheLock(flown, arrival);
                double extra = arrival.Kind == StationWreck.AccessKind.ServiceableLock ? StationAboard.LockCycleSeconds : 0;
                Assert.Equal(flown.Seconds + extra, priced.Seconds, 6);
                Assert.Equal(flown.LandX, priced.LandX);
                Assert.Equal(flown.Destination, priced.Destination);

                // A refused quote is returned untouched: there is no flight to price.
                StationHop.Quote refused = StationHop.QuoteHop(id, StationWreck.ModuleId.Hub, StationWreck.ModuleId.Hub);
                Assert.Equal(refused, StationAboard.WithTheLock(refused, StationAboard.AccessOf(id, StationWreck.ModuleId.Hub)));
            }
        }
    }

    // ── "Back at the boat" ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BackAtTheBoatIsASphereAroundTheMatedAccessAndNothingElse()
    {
        StationWreck.Access dock = StationAboard.AccessOf("dev-station-6", StationWreck.ModuleId.Hub);
        (double sx, double sy) = StationWreck.SpawnFor(dock);

        Assert.True(StationAboard.AtTheDock(dock, sx, sy));
        Assert.True(StationAboard.AtTheDock(dock, dock.X + StationAboard.DockReachDu - 0.01, dock.Y));
        Assert.False(StationAboard.AtTheDock(dock, dock.X + StationAboard.DockReachDu + 0.01, dock.Y));
        Assert.False(StationAboard.AtTheDock(dock, 0, 0));   // the middle of the drum is away from the boat

        Assert.Equal(0.0, StationAboard.HowFarFromTheDock(dock, dock.X, dock.Y), 9);
        double far = StationAboard.HowFarFromTheDock(dock, 41, 41);
        Assert.InRange(far, 0.5, 1.0);
        Assert.True(StationAboard.HowFarFromTheDock(dock, 0, 0) < far);
    }

    [Fact]
    public void AModuleIsFoundByWhereYouStandAndATubeIsNone()
    {
        Assert.Equal(StationWreck.ModuleId.Hub, StationAboard.ModuleAt(0, 0)?.Id);
        Assert.Equal(StationWreck.ModuleId.Habitat, StationAboard.ModuleAt(0, 30)?.Id);
        Assert.Equal(StationWreck.ModuleId.Reactor, StationAboard.ModuleAt(-30, 0)?.Id);
        Assert.Null(StationAboard.ModuleAt(0, 13));    // the tube between the drum and the habitat
        Assert.Null(StationAboard.ModuleAt(100, 100)); // outside her
    }
}
