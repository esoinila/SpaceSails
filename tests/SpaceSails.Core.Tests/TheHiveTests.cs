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
        // #592 · The site's REAL floor list: a rare site has a band nobody listed, with a gap above
        // it, so "−1 down to the depth" is no longer the shape of a building.
        foreach (int level in UndergroundComplex.FloorsOf("miranda"))
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
        //
        // #605 · Asserted against the ALCOVE ITSELF rather than against a label near it. This used to look
        // for a landmark reading "LIFT", which was a proxy — and when the signage was reworked (the plate by
        // the car now answers WHERE YOU ARE, so a third plate saying LIFT was one too many for one wall) the
        // guard went red without the law having changed at all. A test that pins the decoration reports on
        // the decoration; the law here is about ROCK, so it is the walls that get asserted.
        // #801 · BOTH cars. This walked ShaftAt and meant "the lift"; there are two now, at opposite ends
        // of the corridor and on opposite faces of it, and the law is the same law about each of them — a
        // car that wandered between levels is a maze whose exit moves whichever car it is.
        double side = UndergroundComplex.ShaftHalf;

        foreach (int level in Floors())
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("miranda", level, Field);

            foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
            {
                // The alcove's mouth is on the face this car opens off, which is the one its own doorstep
                // is a pace beyond — never a sign written here.
                bool up = car.Landing.Y > car.Y;
                double mouth = car.Y + ((up ? 1 : -1) * UndergroundComplex.CorridorHalf);

                // Both sides of the car's alcove, standing off the spine at the same spot on every floor.
                foreach (double wallX in new[] { car.X - side, car.X + side })
                {
                    Assert.Contains(floor.Walls, w =>
                        Math.Abs(w.X1 - wallX) < 0.001 && Math.Abs(w.X2 - wallX) < 0.001
                        && Math.Abs((up ? Math.Min(w.Y1, w.Y2) : Math.Max(w.Y1, w.Y2)) - mouth) < 0.001);
                }
            }
        }
    }

    [Fact]
    public void EveryMouthCutInTheSpineIsSTILLOpenWhenTheWallIsFinished()
    {
        // #587 · THE DEFECT, STATED IN CORE, IN MILLISECONDS.
        //
        // Each spine face is built as segments by a cursor sweeping left to right, laying wall between one
        // mouth and the next. That only works if the mouths arrive in x order, and one of them did not: the
        // lift alcove is APPENDED to the rib list after the ribs, at the shaft's own x, which sits left of
        // the right-most rib. The sweep ran out past that rib, met the alcove behind it, and emitted a
        // segment running BACKWARDS across everything between the two — one long wall lying over the two
        // mouths it had just been asked to open.
        //
        // The plan was right, the mouths were right, and the collision field was a wall. Nothing but an A*
        // flood over the CLIENT's real deck could see it: 35 floors of drawn, unenterable rooms, found in a
        // playtest and chased for an evening. This states it where it happens — a mouth that was cut must
        // still be a hole once the rest of the face is built.
        //
        // Note it is a SEMANTIC guard, not a shape one. "No wall runs backwards" looks like the same law and
        // is not: clamping the cursor forward stops the reversed segment and leaves the mouth just as sealed.
        // The only thing worth asserting is whether you can walk through the gap.
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);
        double half = UndergroundComplex.CorridorHalf;

        foreach (string body in new[] { "miranda", "luna", "phobos", "europa", "titan", "callisto" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);

                // Every rib opens off the face it points away from; a car's alcove off the face it stands
                // on. #801 · Both cars, off the one published list — this test seeded its mouth list with a
                // hard-coded singleton, so a second alcove walled over on the LOWER face (the exact #587
                // shape, one face down) would never have been looked at.
                var mouths = new List<(double X, double Y, string What)>();
                foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
                {
                    mouths.Add((
                        car.X,
                        car.Y + ((car.Landing.Y > car.Y ? 1 : -1) * half),
                        $"the {car.Kind} car's alcove"));
                }
                foreach (UndergroundComplex.Rib r in floor.Ribs)
                {
                    mouths.Add((r.X, r.Down ? shaftY - half : shaftY + half, $"the rib at x={r.X:F0}"));
                }

                foreach ((double mx, double my, string what) in mouths)
                {
                    foreach (SurfaceLayout.Wall w in floor.Walls)
                    {
                        // Only the spine's own long faces can seal a spine mouth: horizontal, on that face's y.
                        if (Math.Abs(w.Y1 - w.Y2) > 0.001 || Math.Abs(w.Y1 - my) > 0.001)
                        {
                            continue;
                        }

                        double lo = Math.Min(w.X1, w.X2), hi = Math.Max(w.X1, w.X2);

                        // A doorway is only a doorway if the captain fits. Anything intruding past the mouth's
                        // own edges is narrowing it, and narrowing is how this failed the first two times.
                        Assert.False(lo < mx + half - 0.001 && hi > mx - half + 0.001,
                            $"{body} {floor.Name}: a wall from x={lo:F1} to x={hi:F1} lies across {what} " +
                            $"at x={mx:F0} — the mouth was cut and then walled over again (#587).");
                    }
                }
            }
        }
    }

    [Fact]
    public void NoRibIsRunThroughTheLiftShaft()
    {
        // A cross corridor driven through the one thing you need to find again would be quietly cruel.
        //
        // #801 · …and there are two of those things now. A law stated about ShaftAt would have gone on
        // passing while a chamber was laid over the second car, which is the shape of hole a widened
        // feature leaves in a narrow guard.
        foreach (int level in Floors())
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("miranda", level, Field);
            foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
            {
                foreach ((double x, double _) in floor.RoomCentres)
                {
                    Assert.True(Math.Abs(x - car.X) > UndergroundComplex.ShaftHalf,
                        $"{floor.Name}: a room sits on top of the {car.Kind} car.");
                }
            }
        }
    }

    [Fact]
    public void THETopFloorHoldsPressureAndEverythingBelowItDoesNot()
    {
        // The owner's biggest open question, decided on his behalf and pinned here so it can be overruled in
        // one line. The beat is the point: the first floor lulls you (tank stops, nerve steadies) and every
        // stair below is paid for in air. Uniformly safe is a museum; uniformly hostile is a corridor shooter.
        // #677 · Asked of a building with nothing under it that it did not dig, and it says so out loud: a
        // found band breathes on every floor, so a site with halls would answer differently down here and be
        // right to. This law is about the part of the hole somebody paid for.
        const string plain = "miranda";
        Assert.False(UndergroundComplex.HasFoundBand(plain),
            $"{plain} has halls under it now — this law is about the floors the building dug, so pick a site "
            + "without them rather than weakening the assertions.");

        Assert.True(UndergroundComplex.HoldsPressure(plain, -1));
        Assert.False(UndergroundComplex.HoldsPressure(plain, -2));
        Assert.False(UndergroundComplex.HoldsPressure(plain, -3));
        Assert.False(UndergroundComplex.HoldsPressure(plain, -4));

        // Generalised for unbounded depth: it is the TOP OF EVERY SHAFT BAND that still has power, because
        // that is where a facility puts its lobbies. A captain who finds the next shaft gets exactly one
        // floor of relief before the dark again, and relief is always the minority of a deep building.
        Assert.True(UndergroundComplex.HoldsPressure(plain, -1 - UndergroundComplex.FloorsPerShaft));

        int powered = 0;
        for (int level = -1; level >= -20; level--)
        {
            if (UndergroundComplex.HoldsPressure(plain, level)) { powered++; }
        }
        Assert.True(powered * 3 < 20, "too much of the building is a refuge.");

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
                text.Add(UndergroundComplex.HaulLine(haul, "miranda", -2, i,
                    UndergroundComplex.CardInRoom("miranda", -2)));
                text.Add(UndergroundComplex.HaulLine(haul, "miranda", -2, i, null));
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
