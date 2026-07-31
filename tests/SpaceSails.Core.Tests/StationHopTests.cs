using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#531 · The shuttle hop between parts of one dead station — the first case of the owner's
/// "instead of always going to or from our ship, we go between two or more away places".</summary>
public class StationHopTests
{
    private static readonly string[] Stations =
        ["station-wreck-1", "station-wreck-2", "kepler-transfer", "the-orchard", "cold-harbour",
         "mining-stn-7", "relay-9", "the-old-yard"];

    private const double AvatarRadius = 0.7;

    [Fact]
    public void EveryStrandedModule_CanBeReachedByFlying_OnceYouCanGetIn()
    {
        // The feature, stated plainly: whatever the damage did, the shuttle plus a cutting torch always
        // gets a captain into every part of her. If this fails, a seed has produced content nobody can see.
        foreach (string id in Stations)
        {
            var everyCut = StationWreck.Accesses(id)
                .Where(a => a.Kind == StationWreck.AccessKind.CutFace)
                .Select(a => a.Module)
                .ToHashSet();

            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                StationHop.Quote q = StationHop.QuoteHop(id, StationWreck.ModuleId.Hub, arm, everyCut);
                Assert.Equal(StationHop.Refusal.None, q.Refused);
                Assert.True(q.Seconds > 0);
            }
        }
    }

    [Fact]
    public void AModuleWithNoServiceableLock_IsRefusedUntilItIsCut()
    {
        // The boat can come alongside anything; it cannot open a hull that has no door. This is what earns
        // the self-made hatch its place in the game rather than it being a flavour word on a menu.
        foreach (string id in Stations)
        {
            foreach (StationWreck.Access a in StationWreck.Accesses(id)
                         .Where(x => x.Kind == StationWreck.AccessKind.CutFace))
            {
                // Skip anything already walkable from the hub — that is a different refusal.
                if (StationWreck.WalkableFrom(id, StationWreck.ModuleId.Hub).Contains(a.Module))
                {
                    continue;
                }

                StationHop.Quote before = StationHop.QuoteHop(id, StationWreck.ModuleId.Hub, a.Module);
                Assert.Equal(StationHop.Refusal.NeedsACut, before.Refused);

                StationHop.Quote after = StationHop.QuoteHop(
                    id, StationWreck.ModuleId.Hub, a.Module, new[] { a.Module });
                Assert.Equal(StationHop.Refusal.None, after.Refused);
            }
        }
    }

    [Fact]
    public void FlyingSomewhereYouCouldWalk_IsRefused()
    {
        // A kindness, not a rule lawyer's nicety: burning a minute of a live clock to reach a module twenty
        // paces down an intact tube is the interface letting a captain down, not bad judgement.
        foreach (string id in Stations)
        {
            foreach (StationWreck.ModuleId walkable in
                     StationWreck.WalkableFrom(id, StationWreck.ModuleId.Hub))
            {
                StationHop.Quote q = StationHop.QuoteHop(id, StationWreck.ModuleId.Hub, walkable);
                Assert.Equal(StationHop.Refusal.AlreadyWalkable, q.Refused);
            }
        }
    }

    [Fact]
    public void TheDestinationList_OffersOnlyPlacesYouCannotWalkTo_AndHidesNothingElse()
    {
        // Refused destinations stay ON the list. "The Foundry needs cutting" is exactly what a captain
        // routes around, and hiding it would make the station unreadable from the inside.
        foreach (string id in Stations)
        {
            var offered = StationHop.Destinations(id, StationWreck.ModuleId.Hub)
                .Select(d => d.Module).ToList();
            var walkable = StationWreck.WalkableFrom(id, StationWreck.ModuleId.Hub).ToHashSet();

            Assert.All(offered, m => Assert.DoesNotContain(m, walkable));

            // Every module is either walkable or offered — nothing falls between the two.
            foreach (StationWreck.Module m in StationWreck.Modules)
            {
                Assert.True(walkable.Contains(m.Id) || offered.Contains(m.Id),
                    $"{id}: {m.Name} is neither walkable nor flyable — it is simply gone.");
            }
        }
    }

    [Fact]
    public void AHopAlwaysCostsRealTime_SoRoutingIsADecision()
    {
        // The cost of a hop is the clock, because nothing hunting you pauses while you fly. A free hop
        // makes the severed tube a formality.
        foreach (string id in Stations)
        {
            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                double s = StationHop.SecondsBetween(id, StationWreck.ModuleId.Hub, arm);
                Assert.True(s >= StationHop.CastOffSeconds,
                    $"{id}: a hop to {arm} costs {s:F0}s — cheaper than casting off, which cannot be.");
            }
        }
    }

    [Fact]
    public void HoppingIsNeverCheaperThanWalkingAnIntactTube()
    {
        // If flying beat walking, a captain would fly everywhere and the station's topology would stop
        // meaning anything. The cast-off floor is what guarantees this.
        double walkAnIntactTube = (StationWreck.TubeLength + StationWreck.ArmLength) / 9.0; // 9 du/s deck walk
        Assert.True(StationHop.CastOffSeconds > walkAnIntactTube,
            "casting off is quicker than walking an arm — nobody would ever use a tube again.");
    }

    [Fact]
    public void AHopLandsYouSomewhereYouCanStand_AndInTheModuleYouAskedFor()
    {
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            foreach ((StationWreck.ModuleId module, StationHop.Quote q) in
                     StationHop.Destinations(id, StationWreck.ModuleId.Hub))
            {
                Assert.True(DeckReachability.Standable(q.LandX, q.LandY, AvatarRadius, walls),
                    $"{id}: a hop to {q.Destination} puts you inside a wall.");

                StationWreck.Module m = StationWreck.ModuleOf(module);
                Assert.True(
                    DeckReachability.CanReach(
                        new(q.LandX, q.LandY), new(m.CentreX, m.CentreY),
                        walls, AvatarRadius, StationWreck.Bounds),
                    $"{id}: a hop to {q.Destination} lands somewhere you cannot walk into it from.");
            }
        }
    }

    [Fact]
    public void ARelocateIsNotARest()
    {
        // THE rule this whole lane hangs on, and the reason StationHop returns what it returns. A Quote
        // carries a refusal, a duration, a place and a name — and deliberately NOTHING that could be
        // restored. If a field ever appears here for nerve, air, rounds or condition, the excursion loop
        // has been handed a "hop out whenever it gets hard" button and the supply line (#562) is dead.
        //
        // Written as a reflection check rather than a comment because a comment does not fail CI.
        string[] allowed = ["Refused", "Seconds", "LandX", "LandY", "Destination"];
        var actual = typeof(StationHop.Quote)
            .GetProperties()
            .Select(p => p.Name)
            .Where(n => n != "EqualityContract")
            .ToArray();

        Assert.Equal(allowed.OrderBy(n => n), actual.OrderBy(n => n));
    }

    [Fact]
    public void TheCastOffLine_SaysTheClockIsStarting()
    {
        StationHop.Quote q = StationHop.QuoteHop(
            "station-wreck-2", StationWreck.ModuleId.Hub, StationWreck.SeveredTubes("station-wreck-2")[0],
            StationWreck.Accesses("station-wreck-2").Select(a => a.Module).ToHashSet());

        string line = StationHop.CastOffLine(q);
        Assert.Contains("Casting off", line, System.StringComparison.Ordinal);
        Assert.Contains("waits while you fly", line, System.StringComparison.Ordinal);
    }

    [Fact]
    public void EveryRefusal_ExplainsItselfInTheCaptainsTerms()
    {
        foreach (string id in Stations)
        {
            foreach ((StationWreck.ModuleId _, StationHop.Quote q) in
                     StationHop.Destinations(id, StationWreck.ModuleId.Hub))
            {
                if (q.Refused == StationHop.Refusal.None)
                {
                    continue;
                }
                Assert.True(StationHop.RefusalLine(q, "THE DRUM").Length > 30);
            }
        }
    }
}
