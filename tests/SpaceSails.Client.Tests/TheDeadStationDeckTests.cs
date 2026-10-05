using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #653 slice 1 · THE DEAD STATION'S DECK, BUILT THE WAY THE GAME BUILDS IT and walked with A* — from the arrival
/// square of EVERY dock of dozens of seeded stations.
///
/// <para>The premise is half a negative, as it is in Core's own <c>StationWreckTests</c>: the parts the tubes will
/// not let you walk to must genuinely be unwalkable from where the boat is mated, and must become walkable
/// exactly when she is flown there. A deck whose walls drifted from Core's would pass every Core test and fail
/// here — which is why this walks the DECK'S walls, not Core's.</para>
/// </summary>
public sealed class TheDeadStationDeckTests
{
    private const double AvatarRadius = 0.7;
    private const double Step = 0.5;

    private static readonly string[] Stations =
        [.. Enumerable.Range(1, 24).Select(i => $"audit-station-{i}"), "dev-station-6"];

    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds => StationWreck.Bounds;

    private static DeckPlan Deck(
        string id, StationWreck.ModuleId dock, params StationWreck.ModuleId[] cuts) =>
        StationInterior.StationDeck(
            id, new StationInterior.StationState(dock, cuts), droidCount: 0, fillDroids: static (_, _) => { });

    private static SurfaceCollision.Segment[] Segments(DeckPlan deck) =>
        [.. deck.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))];

    private static DeckReachability.Point Spawn(DeckPlan deck) => new(deck.SpawnX, deck.SpawnY);

    // ── The walk ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FromTheDockEveryModuleOfHerWalkGroupCanBeWalkedToAndNoOtherCan()
    {
        int reachedModules = 0, blockedModules = 0;
        foreach (string id in Stations)
        {
            foreach (StationWreck.Module dock in StationWreck.Modules)
            {
                DeckPlan deck = Deck(id, dock.Id);
                SurfaceCollision.Segment[] walls = Segments(deck);
                var group = StationWreck.WalkableFrom(id, dock.Id).ToHashSet();

                foreach (StationWreck.Module target in StationWreck.Modules)
                {
                    bool reached = DeckReachability.CanReach(
                        Spawn(deck), new(target.CentreX, target.CentreY), walls, AvatarRadius, Bounds, Step);

                    Assert.True(reached == group.Contains(target.Id),
                        $"{id}: boat at {dock.Id}, {target.Name} — walkable={reached}, Core's walk-group says {group.Contains(target.Id)}.");

                    if (reached) { reachedModules++; } else { blockedModules++; }
                }
            }
        }

        // Anti-vacuity: both answers were exercised, many times, so a deck that let everything through or
        // blocked everything could not pass.
        Assert.True(reachedModules > 100 && blockedModules > 100,
            $"the sweep saw {reachedModules} reachable and {blockedModules} blocked — it cannot tell pass from fail.");
    }

    [Fact]
    public void EveryConsoleOnTheDeckIsReachableFromTheSideItIsReadFrom()
    {
        foreach (string id in Stations)
        {
            foreach (StationWreck.Module dock in StationWreck.Modules)
            {
                DeckPlan deck = Deck(id, dock.Id);
                SurfaceCollision.Segment[] walls = Segments(deck);
                var group = StationWreck.WalkableFrom(id, dock.Id).ToHashSet();

                foreach (DeckPlan.ConsoleSpot c in deck.Consoles)
                {
                    // A tube-end console is read from its own module: reachable iff that module is in the
                    // boat's walk group. Everything else stands at the dock and is reachable from the arrival square.
                    bool shouldReach = c.Kind != DeckPlan.ConsoleKind.StationTube
                        || group.Contains(StationAboard.ModuleAt(c.X, c.Y)!.Value.Id);

                    bool reached = DeckReachability.CanReach(
                        Spawn(deck), new(c.X, c.Y), walls, AvatarRadius, Bounds, Step);
                    Assert.True(reached == shouldReach,
                        $"{id}: boat at {dock.Id}, '{c.Label}' ({c.Kind}) reachable={reached}, expected {shouldReach}.");
                }
            }
        }
    }

    [Fact]
    public void ConsolesDoNotCrowdEachOther()
    {
        foreach (string id in Stations)
        {
            foreach (StationWreck.Module dock in StationWreck.Modules)
            {
                DeckPlan.ConsoleSpot[] consoles = Deck(id, dock.Id).Consoles;
                for (int i = 0; i < consoles.Length; i++)
                {
                    for (int j = i + 1; j < consoles.Length; j++)
                    {
                        double apart = Math.Sqrt(
                            Math.Pow(consoles[i].X - consoles[j].X, 2) + Math.Pow(consoles[i].Y - consoles[j].Y, 2));
                        Assert.True(apart >= 2.0,
                            $"{id}/{dock.Id}: '{consoles[i].Label}' and '{consoles[j].Label}' are {apart:0.00} du apart.");
                    }
                }
            }
        }
    }

    // ── What is on it ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryModuleWearsItsPlateAndTheDockHasExactlyOneWayHome()
    {
        foreach (string id in Stations)
        {
            DeckPlan deck = Deck(id, StationWreck.ModuleId.Hub);
            foreach (StationWreck.Module m in StationWreck.Modules)
            {
                var plate = Assert.Single(deck.RoomLabels, l => l.Text == StationAboard.PlateOf(m.Id));
                Assert.Equal(m.Id, StationAboard.ModuleAt(plate.X, plate.Y)?.Id);
            }

            Assert.Single(deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.ShuttleAirlock);
        }
    }

    [Fact]
    public void ATubeIsADoorAtBothEndsWhenIntactAndAReadableDeadEndWhenSevered()
    {
        foreach (string id in Stations)
        {
            DeckPlan deck = Deck(id, StationWreck.ModuleId.Hub);
            int severed = StationWreck.SeveredTubes(id).Count;

            Assert.Equal(2 * (StationWreck.Arms.Count - severed), deck.Doors.Length);
            Assert.Equal(2 * severed, deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.StationTube));
        }
    }

    [Fact]
    public void TheDockOffersOneConsolePerDestinationAndTheLabelNamesCoresOwnAccessAndTheChargedSeconds()
    {
        foreach (string id in Stations)
        {
            foreach (StationWreck.Module dock in StationWreck.Modules)
            {
                DeckPlan deck = Deck(id, dock.Id);
                var hops = deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.StationHop).ToArray();
                var destinations = StationHop.Destinations(id, dock.Id);
                Assert.Equal(destinations.Count, hops.Length);

                foreach ((StationWreck.ModuleId module, StationHop.Quote quote) in destinations)
                {
                    StationWreck.Access arrival = StationAboard.AccessOf(id, module);
                    string expected = StationInterior.HopLabel(StationAboard.WithTheLock(quote, arrival), arrival);
                    Assert.Contains(hops, h => h.Label == expected);

                    // Refused ones are SHOWN, because "the Foundry needs cutting" is information a captain plans around.
                    Assert.Equal(quote.Refused == StationHop.Refusal.NeedsACut, expected.StartsWith('✂'));
                    Assert.Contains(arrival.Name, expected);
                }
            }
        }
    }

    [Fact]
    public void ACutFaceStopsBeingRefusedOnTheLabelOnceItIsCutAndPricesTheFlightAndTheLock()
    {
        int seen = 0;
        foreach (string id in Stations)
        {
            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                StationWreck.Access arrival = StationAboard.AccessOf(id, arm);
                if (arrival.Kind != StationWreck.AccessKind.CutFace)
                {
                    continue;
                }

                DeckPlan before = Deck(id, StationWreck.ModuleId.Hub);
                DeckPlan after = Deck(id, StationWreck.ModuleId.Hub, arm);

                Assert.Contains(before.Consoles, c => c.Label == $"✂ {arrival.Name}");
                Assert.DoesNotContain(after.Consoles, c => c.Label == $"✂ {arrival.Name}");

                double seconds = StationHop.QuoteHop(id, StationWreck.ModuleId.Hub, arm, [arm]).Seconds;
                Assert.Contains(after.Consoles, c => c.Label == $"🛸 → {arrival.Name} · {seconds:F0}s");
                seen++;
            }
        }
        Assert.True(seen >= 10, $"only {seen} cut faces in the sweep — this proves little.");
    }

    [Fact]
    public void TheSpawnIsStandableAndTheDeckIsTheStationsOwnShellWallForWall()
    {
        foreach (string id in Stations)
        {
            foreach (StationWreck.Module dock in StationWreck.Modules)
            {
                DeckPlan deck = Deck(id, dock.Id);
                Assert.True(DeckReachability.Standable(deck.SpawnX, deck.SpawnY, AvatarRadius, Segments(deck)));
                Assert.Equal((deck.SpawnX, deck.SpawnY), StationInterior.SpawnAt(id, dock.Id));
                Assert.Equal(
                    StationWreck.Walls(id).Select(s => (s.X1, s.Y1, s.X2, s.Y2)),
                    Segments(deck).Select(s => (s.X1, s.Y1, s.X2, s.Y2)));
            }
        }
    }

    [Fact]
    public void TheHeaderNamesWhereYouStand()
    {
        DeckPlan deck = Deck("dev-station-6", StationWreck.ModuleId.Hub);
        Assert.Equal("THE DRUM", deck.Location(0, 0));
        Assert.Equal("THE FOUNDRY", deck.Location(0, -30));
        Assert.Equal("THE TUBE", deck.Location(0, 13));
    }
}
