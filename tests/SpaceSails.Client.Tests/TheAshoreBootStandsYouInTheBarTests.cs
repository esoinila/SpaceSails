using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE BAR, WITHOUT THE WALK (issue #428, the <c>?ashore=1</c> boot cheat).
///
/// <para>Every bar beat this game has — the oracle (<c>?oracle=1</c>), the stranger-bond (<c>?bond=1</c>),
/// the KAAMOS berth-holder and the Nebula adjuster, the Magpie's rota, the barkeep, the gift shop, the
/// insurance poster — began with the same ship → airlock → tube → immigration hall → bar walk on every
/// boot. In an MCP-driven browser tab the page is <c>document.hidden</c>: rAF is throttled and WASD never
/// lands, so that walk is not slow, it is <b>impossible</b>, and not one bar beat could be smoke-tested at
/// all. <c>?ashore=1</c> stands the captain at the threshold the walk would have ended on.</para>
///
/// <para><b>What this file is really guarding is bug class 1 — unaudited client geometry literals</b>, wrong
/// three times out of three. A boot position is exactly that shape: a pair of numbers nothing derives and
/// nothing checks, which look right in a comment and put the captain inside a wall. So
/// <see cref="HavenInterior.BarThreshold"/> is derived from the doorway the real walk crosses, and every
/// assertion below is made against the SHIPPING <see cref="DeckPlan"/> — found by console kind and by the
/// deck's own <see cref="DeckPlan.Location"/>, never against a coordinate typed into this file. A guard
/// that compared one literal to another literal would agree with itself all the way into the wall.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheAshoreBootStandsYouInTheBarTests
{
    // Every station with a walkable interior — the bars ?ashore=1 can default to or be pointed at.
    private static readonly string[] Bars =
    [
        "the-space-bar", "cinder-roost", "ringside-exchange", "selene-gate", "the-tilt", "the-deep", "red-eye",
    ];

    private const double Watch = OracleRant.WatchSeconds;

    private static DeckPlan DeckFor(string bar, double simTime = 1.0) =>
        HavenInterior.DockedDeck(bar, null, simTime)!;

    private static (double X, double Y) Threshold()
    {
        (double x, double y, double _) = HavenInterior.BarThreshold;
        return (x, y);
    }

    // The walk's bounding box, read off the deck's own walls — so the pathfinder is handed the world the
    // renderer draws rather than an invented field (the "a test is only as honest as the world you hand
    // it" law: a Hive guard once ran against a made-up 78 du field and reported zero rooms everywhere).
    private static (double MinX, double MinY, double MaxX, double MaxY) BoundsOf(DeckPlan deck)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (SurfaceCollision.Segment s in deck.CollisionSegments)
        {
            minX = Math.Min(minX, Math.Min(s.X1, s.X2));
            maxX = Math.Max(maxX, Math.Max(s.X1, s.X2));
            minY = Math.Min(minY, Math.Min(s.Y1, s.Y2));
            maxY = Math.Max(maxY, Math.Max(s.Y1, s.Y2));
        }
        return (minX - 2, minY - 2, maxX + 2, maxY + 2);
    }

    private static DeckPlan.ConsoleSpot Barkeep(DeckPlan deck) =>
        deck.Consoles.First(c => c.Kind == DeckPlan.ConsoleKind.Barkeep);

    // A console that is unambiguously in the CONCOURSE rather than the bar: the ring's own customs hatch.
    private static DeckPlan.ConsoleSpot CustomsHatch(DeckPlan deck) =>
        deck.Consoles.First(c => c.Kind == DeckPlan.ConsoleKind.Hatch
                                 && c.Label.Contains("CUSTOMS", StringComparison.Ordinal));

    // ── 0 · The world this file hands the guards can tell pass from fail ──────────────────────────────

    [Fact]
    public void TheThreeRoomsThisFileArguesAboutAreActuallyThreeDifferentRooms()
    {
        // The fifth bug class: a guard handed a world where every answer is the same answer. If the deck's
        // Location() could not tell the ship from the concourse from the bar, every assertion below would
        // pass on a cheat wired to nothing at all. Prove the three rooms are distinguishable FIRST.
        foreach (string bar in Bars)
        {
            DeckPlan deck = DeckFor(bar);
            string aboard = deck.Location(deck.SpawnX, deck.SpawnY);
            DeckPlan.ConsoleSpot customs = CustomsHatch(deck);
            DeckPlan.ConsoleSpot keep = Barkeep(deck);

            string concourse = deck.Location(customs.X, customs.Y);
            string barRoom = deck.Location(keep.X, keep.Y);

            Assert.True(aboard != concourse && concourse != barRoom && aboard != barRoom,
                $"{bar}: the deck cannot tell its own three rooms apart — aboard '{aboard}', "
                + $"concourse '{concourse}', bar '{barRoom}'.");
        }
    }

    [Fact]
    public void TheShipsOwnSpawnIsNotAlreadyInTheBar()
    {
        // The whole premise: a plain docked boot leaves you ABOARD, which is why the walk exists and why
        // the cheat has anything to do. If the spawn were already in the bar, "you are in the bar" would
        // be true of a cheat that moved nothing.
        foreach (string bar in Bars)
        {
            DeckPlan deck = DeckFor(bar);
            Assert.NotEqual(
                deck.Location(Barkeep(deck).X, Barkeep(deck).Y),
                deck.Location(deck.SpawnX, deck.SpawnY));
        }
    }

    // ── 1 · The threshold is in the bar — at every bar, on any watch ──────────────────────────────────

    [Fact]
    public void TheAshoreBootPutsTheCaptainInTheBarRoomItself()
    {
        (double x, double y) = Threshold();
        foreach (string bar in Bars)
        {
            DeckPlan deck = DeckFor(bar);
            string here = deck.Location(x, y);

            Assert.Equal(deck.Location(Barkeep(deck).X, Barkeep(deck).Y), here);
            Assert.NotEqual(deck.Location(CustomsHatch(deck).X, CustomsHatch(deck).Y), here);
            Assert.NotEqual(deck.Location(deck.SpawnX, deck.SpawnY), here);
        }
    }

    [Fact]
    public void TheRotaNeverMovesTheThresholdOutOfTheBar()
    {
        // Every watch re-seats the regulars and re-rolls the oracle's corner, and a full house is the case
        // that could crowd the door. Walk forty watches per bar with the oracle forced in as well.
        (double x, double y) = Threshold();
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 40; w++)
            {
                double t = (w * Watch) + 1;
                foreach (bool forced in new[] { false, true })
                {
                    DeckPlan deck = HavenInterior.DockedDeck(bar, null, t, forced)!;
                    Assert.Equal(deck.Location(Barkeep(deck).X, Barkeep(deck).Y), deck.Location(x, y));
                }
            }
        }
    }

    // ── 2 · It is a tile the captain can actually STAND on ────────────────────────────────────────────

    [Fact]
    public void TheThresholdIsATileTheCaptainCanStandOn()
    {
        // The same predicate the live movement uses (DeckPlan.Move → SurfaceCollision.Blocked), so the
        // audit and the game agree by construction rather than by comment.
        (double x, double y) = Threshold();
        foreach (string bar in Bars)
        {
            DeckPlan deck = DeckFor(bar);
            Assert.True(DeckReachability.Standable(x, y, DeckPlan.AvatarRadius, deck.CollisionField),
                $"{bar}: the ashore boot stands the captain inside a wall at ({x:F2}, {y:F2}).");
        }
    }

    [Fact]
    public void TheThresholdIsInTheDoorwayTheWalkWouldHaveComeThrough()
    {
        // Not merely "somewhere in the bar" — in the MOUTH of the hall's north door, the one gap the real
        // walk crosses. The proof is that you can walk straight back out the way you came: step south and
        // the move must actually take. Anywhere else along that wall this is a step into the wall, so this
        // is the assertion that fails if the threshold's X ever drifts off the doorway.
        (double x, double y) = Threshold();
        foreach (string bar in Bars)
        {
            DeckPlan deck = DeckFor(bar);
            (double bx, double by) = deck.Move(x, y, 0, -2 * DeckPlan.AvatarRadius);
            Assert.True(by < y,
                $"{bar}: cannot step back out of the bar from the ashore threshold — it is not in the doorway.");
            Assert.Equal(x, bx, 6);
        }
    }

    // ── 3 · Nothing is under the captain's hand before they choose it ─────────────────────────────────

    [Fact]
    public void TheBootDoesNotLandTheCaptainOnTopOfAConsole()
    {
        // [E] on boot must find NOTHING. A threshold inside another console's InteractRadius would make the
        // very first key-press grab a console the tester never walked to — and, worse, would make WHICH one
        // depend on the watch's seating rota. The first [E] of a smoke test has to be the tester's own.
        (double x, double y) = Threshold();
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 40; w++)
            {
                double t = (w * Watch) + 1;
                DeckPlan deck = HavenInterior.DockedDeck(bar, null, t, forceOracle: true)!;
                DeckPlan.ConsoleSpot? under = deck.NearestConsoleSpot(x, y);
                Assert.True(under is null,
                    $"{bar} watch {w}: booting ashore puts '{under?.Label}' under [E] before the tester moves.");
            }
        }
    }

    // ── 4 · From there you can walk to the beats — AND back to your own ship ──────────────────────────

    [Fact]
    public void EveryBarBeatIsWalkableFromWhereTheAshoreBootLeavesYou()
    {
        // The cheat's whole promise: the beats are now reachable. Walk (A*, the shipping collision field)
        // from the threshold to the barkeep, to the oracle's forced corner, and to every regular the rota
        // seated this watch. A console drawn but not walkable is the shape of the bug the Hive kept paying
        // for; it is worth asking of a room this small too.
        (double x, double y) = Threshold();
        var from = new DeckReachability.Point(x, y);

        foreach (string bar in Bars)
        {
            DeckPlan deck = HavenInterior.DockedDeck(bar, null, 1.0, forceOracle: true)!;
            (double MinX, double MinY, double MaxX, double MaxY) bounds = BoundsOf(deck);

            var targets = deck.Consoles
                .Where(c => c.Kind is DeckPlan.ConsoleKind.Barkeep or DeckPlan.ConsoleKind.BarPatron)
                .Select(c => (Name: c.Label, At: new DeckReachability.Point(c.X, c.Y)))
                .ToList();

            Assert.NotEmpty(targets);
            IReadOnlyList<string> stranded = DeckReachability.Unreachable(
                from, targets, deck.CollisionField, DeckPlan.AvatarRadius, bounds);
            Assert.True(stranded.Count == 0,
                $"{bar}: booted ashore and cannot reach {string.Join(", ", stranded)}.");
        }
    }

    [Fact]
    public void YouCanStillWalkHomeToYourOwnShip()
    {
        // #600's law, applied to a cheat: an audit proves you can REACH a thing, never that it is a way
        // BACK. A boot that stands you ashore behind a door you cannot re-open would strand the captain in
        // the bar with her ship on the other side of it, and every reachability assertion above would still
        // be green. So walk the whole route in reverse — threshold → the ship's own spawn.
        (double x, double y) = Threshold();
        foreach (string bar in Bars)
        {
            DeckPlan deck = DeckFor(bar);
            Assert.True(
                DeckReachability.CanReach(
                    new DeckReachability.Point(x, y),
                    new DeckReachability.Point(deck.SpawnX, deck.SpawnY),
                    deck.CollisionField, DeckPlan.AvatarRadius, BoundsOf(deck)),
                $"{bar}: booted ashore with no way back aboard her.");
        }
    }
}
