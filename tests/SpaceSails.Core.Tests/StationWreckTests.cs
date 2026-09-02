using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #531 · The dead station. These hold the premise of the whole feature, which is unusual in that half of it
/// is a NEGATIVE: the parts of her you cannot walk to must genuinely be unwalkable. Every other layout audit
/// in this repo proves you CAN get somewhere; this one has to prove you cannot, because a blockage that
/// quietly stops blocking turns the shuttle hop into a decoration and nothing fails.
/// </summary>
[SlowGate] // #251 · 29 s over 15 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public class StationWreckTests
{
    private const double AvatarRadius = 0.7;

    // A spread of station ids, so nothing here passes by the luck of one seed.
    private static readonly string[] Stations =
        ["station-wreck-1", "station-wreck-2", "kepler-transfer", "the-orchard", "cold-harbour",
         "mining-stn-7", "relay-9", "the-old-yard"];

    private static (double, double, double, double) Bounds => StationWreck.Bounds;

    // ── The shape of the damage ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryStation_LosesTwoOrThreeTubes()
    {
        // One severed tube leaves a station you can still walk end to end round the hub, and the shuttle
        // becomes a formality. All four makes the hub an island with nothing to decide. Two or three is the
        // only band where the captain has a real routing problem.
        foreach (string id in Stations)
        {
            IReadOnlyList<StationWreck.ModuleId> cut = StationWreck.SeveredTubes(id);
            Assert.InRange(cut.Count, StationWreck.MinSevered, StationWreck.MaxSevered);
            Assert.Equal(cut.Count, cut.Distinct().Count()); // never the same arm twice
        }
    }

    [Fact]
    public void ItIsDeterministic_SoTheProblemYouLeftIsTheProblemYouComeBackTo()
    {
        foreach (string id in Stations)
        {
            Assert.Equal(StationWreck.SeveredTubes(id), StationWreck.SeveredTubes(id));
            Assert.Equal(
                StationWreck.Accesses(id).Select(a => (a.Module, a.Kind)),
                StationWreck.Accesses(id).Select(a => (a.Module, a.Kind)));
        }
    }

    [Fact]
    public void EveryStation_NeedsTheShuttle_ForAtLeastTwoSeparateTrips()
    {
        // The premise, stated as arithmetic. If this ever collapses to one group, the feature is dead and
        // nothing else in the suite would notice.
        foreach (string id in Stations)
        {
            Assert.True(StationWreck.WalkGroups(id).Count >= 3,
                $"{id}: only {StationWreck.WalkGroups(id).Count} walk-groups — she can nearly be walked.");
        }
    }

    // ── The negative law: a severed tube really blocks ────────────────────────────────────────────────────

    [Fact]
    public void ASeveredTube_CannotBeWalked_ProvedByAStar()
    {
        // The one that matters most, and the one a refactor would break silently. Walk from the middle of
        // the hub to the middle of each stranded arm and require FAILURE.
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            StationWreck.Module hub = StationWreck.ModuleOf(StationWreck.ModuleId.Hub);

            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                StationWreck.Module m = StationWreck.ModuleOf(arm);
                bool reached = DeckReachability.CanReach(
                    new(hub.CentreX, hub.CentreY), new(m.CentreX, m.CentreY),
                    walls, AvatarRadius, Bounds);

                Assert.False(reached,
                    $"{id}: {m.Name}'s tube is supposed to be severed and the captain walked into it anyway.");
            }
        }
    }

    [Fact]
    public void AnIntactTube_CanBeWalked_BothWays()
    {
        // The mirror. A tube drawn as open that nobody can pass is the LONG SHRIFT bug (#488) all over again.
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            StationWreck.Module hub = StationWreck.ModuleOf(StationWreck.ModuleId.Hub);

            foreach (StationWreck.ModuleId arm in StationWreck.Arms.Where(a => StationWreck.TubeIntact(id, a)))
            {
                StationWreck.Module m = StationWreck.ModuleOf(arm);
                var hubPoint = new DeckReachability.Point(hub.CentreX, hub.CentreY);
                var armPoint = new DeckReachability.Point(m.CentreX, m.CentreY);

                Assert.True(DeckReachability.CanReach(hubPoint, armPoint, walls, AvatarRadius, Bounds),
                    $"{id}: {m.Name}'s tube is intact and the captain cannot get down it.");
                Assert.True(DeckReachability.CanReach(armPoint, hubPoint, walls, AvatarRadius, Bounds),
                    $"{id}: {m.Name} is a one-way trip.");
            }
        }
    }

    // ── Ways in ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryModule_HasSomeWayIn()
    {
        // The spec's law: a module nobody can enter is content nobody will see. An arm without a working
        // lock gets a cut face — never nothing.
        foreach (string id in Stations)
        {
            IReadOnlyList<StationWreck.Access> ways = StationWreck.Accesses(id);
            foreach (StationWreck.Module m in StationWreck.Modules)
            {
                Assert.Contains(ways, a => a.Module == m.Id);
            }
        }
    }

    [Fact]
    public void TheHubsLockAlwaysCycles()
    {
        // A boarding party arrives here. A station whose FIRST door needed cutting would open on a puzzle
        // before it had earned one.
        foreach (string id in Stations)
        {
            StationWreck.Access hub = StationWreck.Accesses(id).Single(a => a.Module == StationWreck.ModuleId.Hub);
            Assert.Equal(StationWreck.AccessKind.ServiceableLock, hub.Kind);
        }
    }

    [Fact]
    public void SomeStations_MakeYouCutYourWayIn()
    {
        // Across the spread, cut faces must actually occur — otherwise the self-made hatch is a feature that
        // exists only in the enum. (Per station it is fine for every arm to cycle; across eight, not.)
        int cutFaces = Stations
            .SelectMany(StationWreck.Accesses)
            .Count(a => a.Kind == StationWreck.AccessKind.CutFace);

        Assert.True(cutFaces > 0, "no station anywhere makes the captain cut in — the cut is dead content.");
    }

    [Fact]
    public void EveryAccess_PutsTheAwayTeamSomewhereTheyCanStand()
    {
        // Spawning inside a wall is the #488 bug that put an away team in vacuum beside their own ship.
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            foreach (StationWreck.Access a in StationWreck.Accesses(id))
            {
                (double sx, double sy) = StationWreck.SpawnFor(a);
                Assert.True(DeckReachability.Standable(sx, sy, AvatarRadius, walls),
                    $"{id}: coming in at {a.Name} puts you inside a wall at ({sx:F1}, {sy:F1}).");
            }
        }
    }

    [Fact]
    public void ComingInAtAModulesOwnAccess_ReachesThatModule()
    {
        // The point of a second lock: it must actually land you in the module it belongs to, walkable from
        // the doorstep. A lock that opens onto somewhere you cannot leave is worse than no lock.
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            foreach (StationWreck.Access a in StationWreck.Accesses(id))
            {
                StationWreck.Module m = StationWreck.ModuleOf(a.Module);
                (double sx, double sy) = StationWreck.SpawnFor(a);

                Assert.True(
                    DeckReachability.CanReach(
                        new(sx, sy), new(m.CentreX, m.CentreY), walls, AvatarRadius, Bounds),
                    $"{id}: {a.Name} lands you somewhere you cannot walk into {m.Name} from.");
            }
        }
    }

    [Fact]
    public void TheWholeStation_IsCoveredByItsAccesses_SoNothingIsUnreachableByAnyMeans()
    {
        // The union of "what you can walk to from each way in" must be every module. This is the law the
        // spec asked for — every section reachable by SOME means — and it is what stops a seed from
        // producing a station with a module nobody will ever stand in.
        foreach (string id in Stations)
        {
            var covered = new HashSet<StationWreck.ModuleId>();
            foreach (StationWreck.Access a in StationWreck.Accesses(id))
            {
                foreach (StationWreck.ModuleId reachable in StationWreck.WalkableFrom(id, a.Module))
                {
                    covered.Add(reachable);
                }
            }

            foreach (StationWreck.Module m in StationWreck.Modules)
            {
                Assert.Contains(m.Id, covered);
            }
        }
    }

    // ── Inside a module ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryModule_IsWalkableCornerToCorner()
    {
        // Per-module, the law the single-hull wreck has had since #497: whatever else is true, the room you
        // flew all the way out to must not be internally sealed.
        foreach (string id in Stations)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = StationWreck.Walls(id);
            foreach (StationWreck.Module m in StationWreck.Modules)
            {
                const double inset = 2.5;
                var a = new DeckReachability.Point(m.X0 + inset, m.Y0 + inset);
                var b = new DeckReachability.Point(m.X1 - inset, m.Y1 - inset);

                Assert.True(DeckReachability.CanReach(a, b, walls, AvatarRadius, Bounds),
                    $"{id}: {m.Name} is sealed against itself.");
            }
        }
    }

    [Fact]
    public void TheStationIsBigEnoughThatWalkingHerWasNeverThePlan()
    {
        // Owner's scale ruling on this issue: several times the infested hull (~57 x 18 du). The topology is
        // what forces the shuttle, but the SIZE is what stops that feeling arbitrary.
        double hub = 4 * StationWreck.HubHalf * StationWreck.HubHalf;
        double arm = 2 * StationWreck.ArmHalfWidth * StationWreck.ArmLength;
        double total = hub + (4 * arm);
        double freighter = 60 * 18;

        Assert.True(total > 2.5 * freighter,
            $"the station is only {total / freighter:F1}x a small freighter — too small to justify flying round her.");
    }

    [Fact]
    public void EveryBlockage_ExplainsItselfInPlainWords()
    {
        // Owner: "A blockage should be readable from inside." You walk up to it, you see why she will not
        // let you through, and you understand the way on is outside — from in here, not from a menu.
        foreach (string id in Stations)
        {
            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                string line = StationWreck.BlockageLine(id, arm);
                Assert.True(line.Length > 80, $"{id}/{arm}: the blockage barely says anything: \"{line}\"");
            }
        }
    }

    [Fact]
    public void ABlockageNeverBlamesTheThingWeDoNotName()
    {
        // Same canon rule the outpost effects live under (#563): the Old Ones are never explained by a
        // label. A tube is folded, packed, open to space or loaded — all of them mundane, all of them
        // structural, none of them testimony.
        string[] forbidden = ["REEVER", "OLD ONE", "RESTORE", "CREATURE"];
        foreach (string id in Stations)
        {
            foreach (StationWreck.ModuleId arm in StationWreck.SeveredTubes(id))
            {
                string line = StationWreck.BlockageLine(id, arm).ToUpperInvariant();
                foreach (string word in forbidden)
                {
                    Assert.DoesNotContain(word, line, StringComparison.Ordinal);
                }
            }
        }
    }
}
