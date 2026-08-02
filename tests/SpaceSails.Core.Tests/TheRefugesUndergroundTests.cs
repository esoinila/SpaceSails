using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #608 · AT LEAST ONE AIR REPLENISH STATION IN EACH OF THE AIRLESS LABS.
///
/// <para>Owner, 2026-08-02, in the order he said it after suffocating on B2: <i>"I thought there is air in
/// the base?"</i> → <i>"there should be a warning or something :-D ... the rooms should have airlocks etc
/// ... some havens :-D"</i> → <i>"like the basement is more dangerous than the surface now :-D"</i> →
/// <i>"on surface there are emergency shelters :-D"</i> → <i>"Still for safety there would need to be a
/// couple of places with air lock and air refilling, because otherwise the elevator being busy could kill
/// employees"</i> → and then the ruling this file exists to enforce:</para>
///
/// <para><b><i>"there should be like at least one air replenish station in each of the airless labs
/// underground... for pure safety"</i></b></para>
///
/// <para>EACH. Not most, not a rare one — which is why this walks every floor of every band on many sites
/// rather than checking a floor and believing it. A safety regulation that a seed can talk its way out of on
/// one moon in forty is not a regulation, and the moon it fails on is the one somebody is standing on.</para>
///
/// <para>The reason the rule is right is the owner's too, and it is better than the mechanic it costs: he
/// ruled that a floor is pressurised at all because <i>"it is very difficult to work in the suit. So all
/// work would happen out of it"</i> — <i>"writing with a pen ... reading documents ... any kind of fine
/// motor skill stuff"</i> would not happen in vacuum. So an airless floor is not an abandoned floor. It is a
/// floor of SUIT-WORK, staffed all day by people in suits, and a building that staffs one and gives them
/// nowhere to go is one busy lift away from killing somebody.</para>
/// </summary>
public sealed class TheRefugesUndergroundTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The scenario's own moons, plus a wide net of generated ids. The scenario ten prove the game
    /// people actually play; the generated ones prove the GENERATOR, which is what the law is about — a
    /// clandestine site is a category, not a fixed list (#585), and the next moon added must inherit the
    /// regulation rather than have to be told about it.</summary>
    private static IEnumerable<string> ManySites()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 90; i++)
        {
            yield return $"generated-moon-{i}";
        }
    }

    /// <summary>Every floor of every site, reported as one table rather than one failure at a time — the
    /// same shape the Hive's other audits use, because a guard that names one floor when forty are wrong
    /// sends you round the loop forty times.</summary>
    private static void AuditEveryFloor(Func<string, int, UndergroundComplex.FloorPlan, string?> check, string law)
    {
        var bad = new List<string>();
        int floors = 0;
        foreach (string body in ManySites())
        {
            // #592 · FloorsOf, never "−1 down to the depth": a site with a band nobody listed has a GAP in
            // the middle where nothing was dug, so counting from a depth audits a building nobody ships.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                if (check(body, level, UndergroundComplex.Build(body, level, Field)) is { } complaint)
                {
                    bad.Add($"  {body} B{-level}: {complaint}");
                }
            }
        }

        Assert.True(floors > 500, $"only {floors} floors audited — this net is not wide enough to mean much.");
        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} of {floors} floor(s) break the law: {law}");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    [Fact]
    public void EveryAirlessFloorHasARefuge()
    {
        // THE acceptance criterion, and the only one the owner stated as an absolute: "there should be like
        // at least one air replenish station in each of the airless labs underground... for pure safety".
        AuditEveryFloor((_, level, floor) =>
        {
            if (UndergroundComplex.HoldsPressure(level))
            {
                return null;
            }
            return floor.Refuges.Count >= 1 ? null
                : "a vacuum floor with nowhere to breathe — the one thing the inspectorate would not sign off.";
        }, "every airless floor carries at least one pressure refuge");
    }

    [Fact]
    public void APressurisedFloorCarriesNoRefuge()
    {
        // The band tops hold pressure (#585), so the whole floor is the refuge. A room labelled AIR in a
        // corridor that already has air is an instrument disagreeing with the room — and worse, it would
        // teach a captain that the plate means "here and nowhere else", which on the floor below is a lie
        // that costs a tank.
        AuditEveryFloor((_, level, floor) =>
            !UndergroundComplex.HoldsPressure(level) || floor.Refuges.Count == 0
                ? null
                : $"{floor.Refuges.Count} refuge(s) on a floor that already holds pressure.",
            "a floor that holds pressure needs no refuge");
    }

    [Fact]
    public void ARefugeIsNeverBesideTheLift()
    {
        // #608 · "Never on the way. If it sits beside the lift it is decoration; it earns its existence by
        // being somewhere you have to decide to detour to."
        //
        // This is the line that keeps #585 alive. Depth is paid for in air and every stair down is a
        // decision about getting back up — and if the air were always four steps off the car, that sentence
        // would be dead and the deep floors would cost nothing. A refuge changes the VERB (from "how long
        // dare I stay" to "can I get from the car to the refuge to the room I want and back"); it does not
        // remove the price.
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);
        AuditEveryFloor((_, _, floor) =>
        {
            foreach (UndergroundComplex.Refuge r in floor.Refuges)
            {
                double dx = r.X - shaftX, dy = r.Y - shaftY;
                double range = Math.Sqrt((dx * dx) + (dy * dy));
                if (range < UndergroundComplex.MinRefugeDetourDu)
                {
                    return $"the refuge is {range:F0} du from the car — that is decoration, not a detour.";
                }
            }
            return null;
        }, "a refuge is always a walk away from the lift");
    }

    [Fact]
    public void ARefugeIsNotAlsoARoomToTurnOver()
    {
        // A pressure vessel somebody maintained is not a drawer. Practically it also matters that the two
        // console kinds never land on the same coordinates: NearestConsoleSpot picks ONE, so a refuge that
        // was also a haul room would have a fifty-fifty chance of its [E] searching the room instead of
        // reading the rack — the affordance visible and the affordance you get being different things,
        // which is the #212 fault in its most confusing form.
        AuditEveryFloor((_, _, floor) =>
        {
            foreach (UndergroundComplex.Refuge r in floor.Refuges)
            {
                foreach ((double rx, double ry) in floor.RoomCentres)
                {
                    if (Math.Abs(rx - r.X) < 0.001 && Math.Abs(ry - r.Y) < 0.001)
                    {
                        return $"the refuge at ({r.X:F0}, {r.Y:F0}) is still listed as a room to search.";
                    }
                }
            }
            return null;
        }, "a refuge stops being a haul room when it becomes one");
    }

    [Fact]
    public void CarvingTheRefugeNeverEMPTIESAFloor()
    {
        // The refuge is carved OUT of the rooms the floor built, so it costs one — and the complaint that
        // started the whole Hive was "I just don't want the secret lab to be puny 2 door apartment". The
        // floor-count law itself lives in the client (`AFloorIsWorthTheLiftRide`, which counts rooms AND
        // refuges, because a refuge is a room's worth of building rather than a deletion). What is asserted
        // HERE is the harder floor under it: however tight a seeded floor is, taking the refuge never leaves
        // a lift ride that opens on nothing to do.
        AuditEveryFloor((_, level, floor) =>
        {
            if (UndergroundComplex.HoldsPressure(level) || floor.RoomCentres.Count >= 1)
            {
                return null;
            }
            return "the refuge took the last room — this floor is now a corridor and a cupboard.";
        }, "a refuge never empties the floor it stands on");
    }

    [Fact]
    public void TheWayDownStillHasARoomToBeFoundIn()
    {
        // #592 · Room 0 of the last floor a site with a hidden band admits to is the authority card, and it
        // is a DESIGNATED index precisely because a rolled one sometimes misses — a site whose Key never
        // rolled would have its hidden band unreachable not for that visit but FOREVER, with every test
        // green and nothing on screen ever saying so. The refuge is carved out of that same room list, so it
        // is now a second way to reach through and take that room away.
        //
        // WHAT THIS CAN AND CANNOT SAY, written down because the first draft of it could say neither. It
        // checked that no refuge sat on the coordinates of `RoomCentres[0]` — which is structurally blind,
        // because the carve REMOVES a room and every index after it shifts, so on a broken build the
        // comparison would have been against a different room entirely and passed. (It did: the break that
        // was meant to redden it did not.) The checkable consequence is the one that matters anyway — the
        // designated index must still name a room that exists, and that room must still be the card.
        //
        // CarveRefuges also refuses the designated index outright. That is defensive rather than load
        // bearing (an index that still exists is still the card, whichever poured box it now points at) and
        // it is not what this asserts.
        int checkedSites = 0;
        foreach (string body in ManySites())
        {
            if (UndergroundComplex.KeyRoomFor(body) is not { } key)
            {
                continue;
            }
            checkedSites++;

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, key.Level, Field);
            Assert.True(floor.RoomCentres.Count > key.RoomIndex,
                $"{body} B{-key.Level}: the card's room {key.RoomIndex} is not on the floor any more — the "
                + "band nobody listed is now unreachable on this world forever.");
            Assert.Equal(UndergroundComplex.Haul.Key,
                UndergroundComplex.InRoom(body, key.Level, key.RoomIndex));
        }
        Assert.True(checkedSites >= 5, $"only {checkedSites} sites with a hidden band — nothing was proved.");
    }

    [Fact]
    public void TheSameFloorHasTheSameRefugeEveryVisit()
    {
        // Core stays deterministic (DiceRule only, no clock, no Random), and here it is load-bearing rather
        // than housekeeping: a captain is meant to LEARN a building. A refuge that moved between rides in
        // the same lift would be worse than none, because they would walk to where it was.
        foreach (string body in new[] { "miranda", "titan", "generated-moon-7", "generated-moon-61" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                IReadOnlyList<UndergroundComplex.Refuge> first =
                    UndergroundComplex.Build(body, level, Field).Refuges;
                IReadOnlyList<UndergroundComplex.Refuge> again =
                    UndergroundComplex.Build(body, level, Field).Refuges;
                Assert.Equal(first, again);
            }
        }
    }

    [Fact]
    public void TheAirVOLUMEIsTheOneUsedEverywhere()
    {
        // One containment law (UndergroundComplex.RefugeHolds), read by Core, by the audit and by the live
        // suit — so nothing can ever put the captain inside the room and outside the air. The corners are
        // the reason it is a rectangle: this is a poured box, not the shelter's regolith drum, and an
        // inscribed ellipse would leave somebody standing plainly inside a sealed room watching their tank
        // tick down.
        var refuge = new UndergroundComplex.Refuge(100, 50, "plate");

        Assert.True(refuge.Contains(100, 50));
        Assert.True(refuge.Contains(
            100 + UndergroundComplex.RefugeHalfWidth - 0.05,
            50 + UndergroundComplex.RefugeHalfHeight - 0.05));   // the corner counts
        Assert.False(refuge.Contains(100 + UndergroundComplex.RefugeHalfWidth + 0.5, 50));
        Assert.False(refuge.Contains(100, 50 - UndergroundComplex.RefugeHalfHeight - 0.5));

        // And the room really does hold it: the refuge's air must fit inside the poured box it was carved
        // from, or the volume claims ground the walls do not enclose.
        Assert.True(UndergroundComplex.RefugeHalfWidth < 15.0 / 2);
        Assert.True(UndergroundComplex.RefugeHalfHeight < 12.0 / 2);
    }

    [Fact]
    public void TheREFUGENeverExplainsWhatThisPlaceWasFor()
    {
        // Canon, owner ruling 2026-07-30, and this is a tempting place to break it: a safety plate is the
        // one thing down here allowed to be plain, and "plain" is one word away from "explanatory". A
        // refuge may say what the ROOM is for. It may never say what the BUILDING is for.
        string[] forbidden =
        [
            "old one", "old ones", "reever", "restore", "backup", "brain", "kaamos", "minister",
            "ancient", "alien", "experiment", "specimen",
        ];

        var prose = new List<string>
        {
            UndergroundComplex.RefugeBreathingLine,
            UndergroundComplex.RefugeTankLabel,
            UndergroundComplex.RefugeGlyph,
            UndergroundComplex.VacuumCard(-2, 600),
            UndergroundComplex.VacuumCard(-7, 90),
        };
        for (int i = 0; i < 12; i++)
        {
            prose.Add(UndergroundComplex.RefugeSign("miranda", -i - 2, 0));
        }

        foreach (string line in prose)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void ThePlateSaysWhatTheRoomIsAndStops()
    {
        // A captain who cannot find air is not being teased (#573's rule for the surface shelter's sign,
        // pointed underground). The plate is an inspectorate's: a number, an occupancy, and an instruction.
        for (int level = -2; level > -20; level--)
        {
            if (UndergroundComplex.HoldsPressure(level))
            {
                continue;
            }
            string sign = UndergroundComplex.RefugeSign("miranda", level, 0);
            Assert.Contains("PRESSURE REFUGE", sign, StringComparison.Ordinal);
            Assert.Contains("OCCUPANCY", sign, StringComparison.Ordinal);
            Assert.Equal(sign, UndergroundComplex.RefugeSign("miranda", level, 0));   // deterministic
        }
    }
}
