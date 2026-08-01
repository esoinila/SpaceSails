using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #585 · THE HIVE. Owner: <i>"I just don't want the secret lab to be puny 2 door apartment, but look like it
/// could facilitate a large operation with serious funding."</i>
///
/// <para>What it replaced was one room, 16 × 14 du, appended from a hidden door — for a find the code itself
/// bills as the veterans' once-a-career payoff. These pin the things that make it a FACILITY instead, and the
/// three calls made on the owner's behalf when he said "go forward" without answering the open questions.</para>
/// </summary>
public sealed class TheHiveTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    private static IEnumerable<int> Floors()
    {
        for (int level = -1; level >= UndergroundComplex.DeepestFloor; level--)
        {
            yield return level;
        }
    }

    [Fact]
    public void AFloorIsAFACILITYAndNotAnApartment()
    {
        // The complaint, as a number. The old lab was ONE room; a floor of this place has corridors, rooms
        // down both sides of them, and doors that will not open.
        foreach (int level in Floors())
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("miranda", level, Field);

            Assert.True(floor.Walls.Count > 60,
                $"{floor.Name}: {floor.Walls.Count} wall segments — that is a flat, not a facility.");
            Assert.True(floor.RoomCentres.Count >= 4,
                $"{floor.Name}: only {floor.RoomCentres.Count} rooms you can enter.");
            Assert.True(floor.Locked.Count >= 3,
                $"{floor.Name}: {floor.Locked.Count} locked doors — nothing implies the space beyond.");
        }
    }

    [Fact]
    public void ItNEVERLeavesTheSurfacesOwnEnvelope()
    {
        // The architectural point, and the reason "down" was the right answer at all: a complex the size of
        // the whole field costs no new space, because it is not beside the field — it is under it. If a floor
        // ever spills past the envelope, the border problem is back and we have gained nothing.
        foreach (int level in Floors())
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("miranda", level, Field);
            foreach (SurfaceLayout.Wall w in floor.Walls)
            {
                Assert.InRange(w.X1, Field.LeftX, Field.RightX);
                Assert.InRange(w.X2, Field.LeftX, Field.RightX);
                Assert.InRange(w.Y1, Field.BottomY, Field.LandingBandY);
                Assert.InRange(w.Y2, Field.BottomY, Field.LandingBandY);
            }
        }
    }

    [Fact]
    public void TheLiftIsInTheSAMEPlaceOnEveryFloor()
    {
        // Going down must be legible and coming back up must never be a search. A shaft that wandered between
        // levels would turn the whole complex into a maze whose exit moves, which is a different and much
        // worse game than the one being built.
        (double x, double y) = UndergroundComplex.ShaftAt(Field);
        foreach (int level in Floors())
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("miranda", level, Field);
            Assert.Contains(floor.Labels, l =>
                Math.Abs(l.X - x) < 6 && Math.Abs(l.Y - y) < 14 && l.Label.Contains("LIFT", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void NoRibIsRunThroughTheLiftShaft()
    {
        // A cross corridor driven through the one thing you need to find again would be quietly cruel.
        (double shaftX, _) = UndergroundComplex.ShaftAt(Field);
        foreach (int level in Floors())
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("miranda", level, Field);
            foreach ((double x, double _) in floor.RoomCentres)
            {
                Assert.True(Math.Abs(x - shaftX) > UndergroundComplex.ShaftHalf,
                    $"{floor.Name}: a room sits on top of the lift.");
            }
        }
    }

    [Fact]
    public void THETopFloorHoldsPressureAndEverythingBelowItDoesNot()
    {
        // The owner's biggest open question, decided on his behalf and pinned here so it can be overruled in
        // one line. The beat is the point: the first floor lulls you (tank stops, nerve steadies) and every
        // stair below is paid for in air. Uniformly safe is a museum; uniformly hostile is a corridor shooter.
        Assert.True(UndergroundComplex.HoldsPressure(-1));
        Assert.False(UndergroundComplex.HoldsPressure(-2));
        Assert.False(UndergroundComplex.HoldsPressure(-3));

        // And the lines say so, in those words, so the player is never guessing which kind of floor they are on.
        Assert.Contains("stops drawing", UndergroundComplex.PressurisedLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("costs air", UndergroundComplex.DeadAirLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SomeCorridorsLeadSomewhereWeAreNotAllowedToGo()
    {
        // Owner, by name: "corridors that lead to somewhere far away where we dare not venture too far into."
        // A distance painted on a door that never opens is the cheapest possible way to say the world does not
        // stop at the edge of what you may walk.
        var far = new List<string>();
        foreach (int level in Floors())
        {
            far.AddRange(UndergroundComplex.Build("miranda", level, Field).Locked
                .Select(l => l.Sign)
                .Where(sign => sign.Contains("km", StringComparison.Ordinal)));
        }

        Assert.NotEmpty(far);
        Assert.All(far, sign => Assert.Contains("SECTOR", sign, StringComparison.Ordinal));
    }

    [Fact]
    public void EveryLockedDoorSaysWHATItIsRefusingYou()
    {
        // A corridor of blank shut doors is a wall. A corridor of doors reading INTAKE / GRADING / DISPATCH is
        // a building with a purpose you would rather not have worked out.
        foreach (int level in Floors())
        {
            foreach (UndergroundComplex.LockedDoor d in
                     UndergroundComplex.Build("miranda", level, Field).Locked)
            {
                Assert.False(string.IsNullOrWhiteSpace(d.Sign));
            }
        }
    }

    [Fact]
    public void EachKindOfSiteHasItsOwnVocabulary()
    {
        // "different clandestine sites in the spirit of world building" — most of what makes one unlike
        // another is the words on its doors, and that costs nothing but words.
        var seen = new List<string[]>();
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            string[] signs = UndergroundComplex.SignsFor(kind);
            Assert.True(signs.Length >= 6, $"{kind} has too few signs to fill a corridor.");
            Assert.False(string.IsNullOrWhiteSpace(UndergroundComplex.TitleOf(kind)));

            foreach (string[] other in seen)
            {
                Assert.True(signs.Intersect(other).Count() < 3,
                    $"{kind} reads like another kind of site — the vocabulary is doing no work.");
            }
            seen.Add(signs);
        }
    }

    [Fact]
    public void MoonsGetDifferentKindsOfSite()
    {
        var kinds = new HashSet<UndergroundComplex.Kind>();
        foreach (string body in new[] { "miranda", "luna", "phobos", "europa", "titan", "triton", "callisto" })
        {
            kinds.Add(UndergroundComplex.KindFor(body));
        }
        Assert.True(kinds.Count > 1, "every moon hides the same building.");
    }

    [Fact]
    public void ARoomIsUsuallySTRIPPEDAndDirtIsTheRarestThingInTheBuilding()
    {
        // Owner: "those sites should have good loot of stuff and information also... like dirt on potential
        // contacts ... the works." Dirt is the most valuable thing here because it is the only haul you spend
        // on a PERSON, so it has to be the scarcest or it stops being a find.
        var counts = new Dictionary<UndergroundComplex.Haul, int>();
        const int rooms = 900;
        for (int i = 0; i < rooms; i++)
        {
            UndergroundComplex.Haul haul = UndergroundComplex.InRoom("miranda", -2, i);
            counts[haul] = counts.GetValueOrDefault(haul) + 1;
        }

        Assert.True(counts[UndergroundComplex.Haul.Nothing] > rooms / 5,
            "nothing is ever stripped — the place reads as untouched, which it is not.");
        Assert.True(counts[UndergroundComplex.Haul.Dirt] > 0, "there is never any dirt.");
        Assert.True(counts[UndergroundComplex.Haul.Dirt] < counts[UndergroundComplex.Haul.Equipment],
            "a file on somebody is commoner than a crate — dirt has stopped being a find.");
    }

    [Fact]
    public void DirtNamesSomebodyTheCaptainCanActuallyGoAndMEET()
    {
        // Leverage you cannot spend is a lore note. Every subject is a standing role at a real berth, so the
        // file has somewhere to be used — and the game never once tells the captain to use it.
        string[] places = ["The Tilt", "Selene Gate", "Highport", "Roadstead", "Ringside", "The Deep"];
        for (int i = 0; i < 60; i++)
        {
            string dirt = UndergroundComplex.DirtOn("miranda", -2, i);
            Assert.True(places.Any(p => dirt.Contains(p, StringComparison.Ordinal)),
                $"this file is on nobody reachable: {dirt}");
        }
    }

    [Fact]
    public void NothingDownHereEXPLAINSAnything()
    {
        // The canon rule, which a facility this size is the most tempting possible place to break. It may be
        // enormous, expensive and obviously state-backed, and it may never say what it was for.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var text = new List<string> { UndergroundComplex.DescendingLine, UndergroundComplex.PressurisedLine,
            UndergroundComplex.DeadAirLine };
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            text.AddRange(UndergroundComplex.SignsFor(kind));
        }
        for (int i = 0; i < 40; i++)
        {
            foreach (UndergroundComplex.Haul haul in Enum.GetValues<UndergroundComplex.Haul>())
            {
                text.Add(UndergroundComplex.HaulLine(haul, "miranda", -2, i));
            }
        }

        foreach (string line in text)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void AFloorIsTheSameFloorEveryVisit()
    {
        // Determinism is law, and here it is also the design: a captain must be able to learn this place and
        // come back for the door they could not open.
        foreach (int level in Floors())
        {
            UndergroundComplex.FloorPlan a = UndergroundComplex.Build("miranda", level, Field);
            UndergroundComplex.FloorPlan b = UndergroundComplex.Build("miranda", level, Field);

            Assert.Equal(a.Walls.Count, b.Walls.Count);
            Assert.Equal(a.Locked.Count, b.Locked.Count);
            Assert.Equal(
                a.RoomCentres.Select(r => r.X).ToArray(),
                b.RoomCentres.Select(r => r.X).ToArray());
        }
    }

    [Fact]
    public void TwoFloorsOfOneBuildingAreNotTheSameFloorTwice()
    {
        UndergroundComplex.FloorPlan b1 = UndergroundComplex.Build("miranda", -1, Field);
        UndergroundComplex.FloorPlan b2 = UndergroundComplex.Build("miranda", -2, Field);

        Assert.NotEqual(b1.Name, b2.Name);
        Assert.True(
            b1.RoomCentres.Count != b2.RoomCentres.Count
            || b1.Locked.Count != b2.Locked.Count
            || !b1.RoomCentres.Select(r => r.Y).SequenceEqual(b2.RoomCentres.Select(r => r.Y)),
            "B1 and B2 are laid out identically — the lift is a very slow way to see the same room.");
    }
}
