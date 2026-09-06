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
[SlowGate] // #251 · 11 s over 12 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
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
        AuditEveryFloor((body, level, floor) =>
        {
            if (UndergroundComplex.HoldsPressure(body, level))
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
        AuditEveryFloor((body, level, floor) =>
            !UndergroundComplex.HoldsPressure(body, level) || floor.Refuges.Count == 0
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
        // ── #801 · MEASURED FROM BOTH CARS, AND STATED AS "THE CARVE TOOK THE BEST IT HAD" ─────────────
        //
        // There are two ways onto a floor now. A refuge that is a detour from the cage and four steps from
        // the goods car has stopped costing anything, and the law as it stood — one shaft, straight-line —
        // would have gone on passing while the sentence it exists to protect died. It did not: widening it
        // to both cars took 332 of 1130 floors red, and the carve was fixed rather than the guard.
        //
        // What could not be fixed is arithmetic. Two cars a hundred and seventy du apart on a spine two
        // hundred and seventy-eight du long means a handful of the tightest generated floors — two to seven
        // chambers, all of them hanging off one or two corridors — have NOWHERE that is seventy du from
        // both. On those the carve takes the furthest room it has, and a guard that simply demanded the
        // number would be demanding a floor the generator cannot produce.
        //
        // So the law is stated as a comparison instead of as a constant, which is stronger: the refuge must
        // be the FURTHEST-from-both room the floor had, and it must clear the law wherever any room does.
        // The pool is reconstructed exactly — the refuge itself plus every remaining room centre, minus the
        // one room #592 reserves — so this cannot pass by measuring a world the carve never saw.
        int fellBack = 0, cleared = 0;
        double worstFallback = double.MaxValue;

        AuditEveryFloor((body, level, floor) =>
        {
            if (floor.Refuges.Count == 0)
            {
                return null;
            }
            UndergroundComplex.Refuge r = floor.Refuges[0];

            // The pool the carve chose from: this room, plus the ones it left. Amenities and refuges never
            // share a floor (one is plumbed and the other is not), so nothing else came out of it.
            int reserved = UndergroundComplex.KeyRoomFor(body) is { } key && key.Level == level
                ? key.RoomIndex
                : -1;
            var pool = new List<(double X, double Y)> { (r.X, r.Y) };
            for (int i = 0; i < floor.RoomCentres.Count; i++)
            {
                if (i != reserved)
                {
                    pool.Add(floor.RoomCentres[i]);
                }
            }

            double Nearest(double x, double y)
            {
                double near = double.MaxValue;
                foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
                {
                    double dx = x - car.X, dy = y - car.Y;
                    near = Math.Min(near, Math.Sqrt((dx * dx) + (dy * dy)));
                }
                return near;
            }

            double got = Nearest(r.X, r.Y);
            double best = 0;
            foreach ((double px, double py) in pool)
            {
                best = Math.Max(best, Nearest(px, py));
            }

            if (got >= UndergroundComplex.MinRefugeDetourDu)
            {
                cleared++;
                return null;
            }

            // It did not clear the law. That is only allowed where NOTHING on the floor could have.
            if (best >= UndergroundComplex.MinRefugeDetourDu)
            {
                return $"the refuge is {got:F0} du from the nearest car and this floor had a room {best:F0} "
                    + "du out — that is decoration, not a detour.";
            }
            fellBack++;
            worstFallback = Math.Min(worstFallback, got);
            return null;
        }, "a refuge is always a walk away from the lift — the furthest walk the floor had");

        // ── AND THE ESCAPE HATCH IS MEASURED, or it is not an escape hatch, it is a hole.
        Assert.True(cleared > 500, $"only {cleared} refuge(s) cleared the law outright — this proved little.");
        Assert.True(fellBack > 0,
            "no floor ever used the furthest-room fallback, so the branch above is never exercised and this "
            + "guard has been agreeing with itself.");
        Assert.True(fellBack * 20 < cleared,
            $"{fellBack} floor(s) fell back against {cleared} that cleared the law — the fallback has "
            + "stopped being the exception it is documented as.");
        Assert.True(worstFallback >= UndergroundComplex.MinRefugeDetourDu * 0.7,
            $"the worst fallback puts a refuge {worstFallback:F0} du from a car, and even a floor with "
            + "nowhere to put one may not put it beside the doors.");
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
        AuditEveryFloor((body, level, floor) =>
        {
            if (UndergroundComplex.HoldsPressure(body, level) || floor.RoomCentres.Count >= 1)
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

            // #1149 · §8's reserved word, on the family that now carries a CARD. The card is the one place
            // in this arc where explaining would be easiest and worst: a captain standing in a room whose
            // seal was cut open from the inside will supply their own answer, and the game may not.
            "monolith",
        ];

        var prose = new List<string>
        {
            UndergroundComplex.RefugeEntryLine(UndergroundComplex.RefugeState.Holding),
            UndergroundComplex.RefugeEntryLine(UndergroundComplex.RefugeState.Empty),
            UndergroundComplex.RefugeEntryLine(UndergroundComplex.RefugeState.Failed),
            UndergroundComplex.RefugeTankLabel,
            UndergroundComplex.RefugeGlyph,
            UndergroundComplex.RefugeFailedGlyph,
            UndergroundComplex.RefugeRowTag,
            UndergroundComplex.VacuumCard("miranda", -2, 600),
            UndergroundComplex.VacuumCard("miranda", -7, 90),

            // #1149 · The dry plate, the tag's two entries, and the card that tells the one that failed.
            // Every string this arc added, in the one sweep that already knows what a refuge may not say.
            UndergroundComplex.RefugeDryGlyph,
            UndergroundComplex.InspectionTagEntry,
            UndergroundComplex.InspectionTagSealReplaced,
            PaperHeads.TagTitle,
            PaperHeads.TagDocument,
            UndergroundComplex.InspectionTagLine("miranda", -2),
            UndergroundComplex.InspectionTagLine("miranda", -3),
            StoryBeats.Title(StoryBeats.Beat.RefugeFailed),
            StoryBeats.Caption(StoryBeats.Beat.RefugeFailed),
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
    public void NoPaperworkComesOutOfARoomThatCannotBeBreathedIn()
    {
        // ── #608 · THE OTHER HALF OF THE SAME RULE ───────────────────────────────────────────────────────
        //
        // The owner's reason a floor is pressurised at all, in his words: "any room that would house like
        // office work would be pressurized by that constraint ... like writing with a pen ... reading
        // documents etc.... that kind of thing would not happen at all in vacuum as a working environment".
        //
        // The refuge audits above enforce the safety half. This is the half about what is IN the rooms, and
        // it went unenforced for as long as this feature has existed: UndergroundComplex.InRoom branched on
        // designation, on IsFound and on IsUnlisted and never once on air — HoldsPressure did not appear in
        // the file at all — so three floors in four handed out "📋 Operational paper: rosters, routes, a
        // shipping schedule" and "🗃 A file, and it is not the file you were expecting" out of rooms where
        // by the owner's own rule nobody could have held the pen that wrote them.
        //
        // WHAT MAKES THIS GUARD ABLE TO FAIL, which is the house rule (revert the fix and watch it go red):
        // it counts what it swept and what it found on BOTH sides of the rule. An assertion that no vacuum
        // room holds paper would pass beautifully on a build where no room anywhere holds paper, or where
        // the sweep never reached a vacuum floor with rooms on it — so the pressurised floors have to still
        // be handing out both kinds of paperwork in quantity, the airless ones have to still be handing out
        // the crates and the emptiness the owner says a suit-work floor is made of, and both populations
        // have to be large.
        var offences = new List<string>();
        int airlessRooms = 0, pressurisedRooms = 0;
        int paperInAir = 0, filesInAir = 0;
        int cratesInVacuum = 0, strippedInVacuum = 0;

        foreach (string body in ManySites())
        {
            // #411 · The head office's designated sheet is an AUTHORED placement, not a roll, and it sits on
            // THE STANDING ORDER's own plate at B12 of a building where only every fourth floor breathes.
            // The air rule reaches the rolls; it does not reach through and delete the one piece of evidence
            // that arc is built on. That floor wanting air is the airlock half of #608 and is still open.
            (int Level, int RoomIndex)? order = UndergroundComplex.StandingOrderRoomFor(body);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                bool air = UndergroundComplex.HoldsPressure(body, level);

                // The floor's OWN rooms, and not a count typed here: a refuge is carved out of the room list
                // (CarveRefuges removes one), so the indices this sweep asks about are the indices the client
                // asks about and not a parallel building of the test's own invention.
                int rooms = UndergroundComplex.Build(body, level, Field).RoomCentres.Count;
                for (int room = 0; room < rooms; room++)
                {
                    UndergroundComplex.Haul haul = UndergroundComplex.InRoom(body, level, room);
                    if (air)
                    {
                        pressurisedRooms++;
                        if (haul == UndergroundComplex.Haul.Records)
                        {
                            paperInAir++;
                        }
                        if (haul == UndergroundComplex.Haul.Dirt)
                        {
                            filesInAir++;
                        }
                        continue;
                    }

                    airlessRooms++;
                    if (haul == UndergroundComplex.Haul.Equipment)
                    {
                        cratesInVacuum++;
                    }
                    if (haul == UndergroundComplex.Haul.Nothing)
                    {
                        strippedInVacuum++;
                    }

                    if (order is { } o && level == o.Level && room == o.RoomIndex)
                    {
                        continue;
                    }
                    if (UndergroundComplex.NeedsAir(haul))
                    {
                        offences.Add($"  {body} B{-level} room {room}: {haul} on a floor with no atmosphere "
                            + "— somebody wrote this in a suit.");
                    }
                }
            }
        }

        Assert.True(airlessRooms > 2000,
            $"only {airlessRooms} vacuum rooms swept — this net is not wide enough to mean anything.");
        Assert.True(pressurisedRooms > 500,
            $"only {pressurisedRooms} pressurised rooms swept — the pair below would prove nothing.");

        if (offences.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{offences.Count} of {airlessRooms} vacuum room(s) hold paperwork. The owner's "
                + "rule: office work does not happen in a suit, so a room of office work is a pressurised "
                + "room.");
            foreach (string line in offences.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        // THE VACUITY PAIR. The building has not simply stopped holding paper.
        Assert.True(paperInAir > pressurisedRooms / 10,
            $"only {paperInAir} of {pressurisedRooms} breathable rooms hold operational paper — the rule has "
            + "eaten the haul instead of placing it.");
        Assert.True(filesInAir > 0,
            $"no file on anybody survives anywhere in {pressurisedRooms} breathable rooms — dirt is gone from "
            + "the game and this guard was passing on an empty world.");

        // …and a suit-work floor is still a floor of suit-work, not a corridor of nothing.
        Assert.True(cratesInVacuum > airlessRooms / 10,
            $"only {cratesInVacuum} of {airlessRooms} vacuum rooms hold a crate — the storage floors have "
            + "been emptied by a rule about rosters.");
        Assert.True(strippedInVacuum > airlessRooms / 10,
            $"only {strippedInVacuum} of {airlessRooms} vacuum rooms are stripped — the emptiness is "
            + "load-bearing down here (§10.3) and it has gone.");
    }

    [Fact]
    public void TheSameRoomAnswersTheSameWayAfterTheAirRule()
    {
        // The substitution rolls its own seed (hive:suit-work), and a captain is meant to be able to walk
        // back to a room. Deterministic, and — the part worth pinning — it does not disturb the haul table
        // itself: a floor that breathes answers exactly as it did before there was a rule about air.
        foreach (string body in new[] { "miranda", "titan", "generated-moon-7" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                for (int room = 0; room < 8; room++)
                {
                    Assert.Equal(
                        UndergroundComplex.InRoom(body, level, room),
                        UndergroundComplex.InRoom(body, level, room));
                }
            }
        }

        // B1 breathes on every site, so its rooms must be untouched by the gate — the weightings above it
        // are still the office weightings and this is the floor that proves it.
        Assert.True(UndergroundComplex.HoldsPressure("miranda", -1));
        var onTheTopFloor = new HashSet<UndergroundComplex.Haul>();
        for (int room = 0; room < 400; room++)
        {
            onTheTopFloor.Add(UndergroundComplex.InRoom("miranda", -1, room));
        }
        Assert.Contains(UndergroundComplex.Haul.Records, onTheTopFloor);
        Assert.Contains(UndergroundComplex.Haul.Dirt, onTheTopFloor);
    }

    [Fact]
    public void ThePlateSaysWhatTheRoomIsAndStops()
    {
        // A captain who cannot find air is not being teased (#573's rule for the surface shelter's sign,
        // pointed underground). The plate is an inspectorate's: a number, an occupancy, and an instruction.
        for (int level = -2; level > -20; level--)
        {
            if (UndergroundComplex.HoldsPressure("miranda", level))
            {
                continue;
            }
            string sign = UndergroundComplex.RefugeSign("miranda", level, 0);
            Assert.Contains("PRESSURE REFUGE", sign, StringComparison.Ordinal);
            Assert.Contains("OCCUPANCY", sign, StringComparison.Ordinal);
            Assert.Equal(sign, UndergroundComplex.RefugeSign("miranda", level, 0));   // deterministic
        }
    }

    // ── #608 · AND WHAT THE DECADES DID TO IT ───────────────────────────────────────────────────────────
    //
    // The guards above are about what was BUILT, and none of them is touched: every airless floor still
    // carries a refuge, it is still never beside the lift, and the plan still marks it. What follows is the
    // other half of the issue's own "Done when" — "its state is part of the story (holds / holds but empty /
    // failed), and a working one can refill a tank" — and the reason it matters is the owner's warning in
    // the same comment: "If every ADMINISTRATION floor is safe, deep ADMINISTRATION floors stop costing
    // anything. The state of the seal is what keeps it honest."

    [Fact]
    public void ARefugeHoldsUnlessSomebodyDidSomethingToIt()
    {
        // #1149 · THE RARITY PIN, RE-MEASURED, and it is the owner's ruling turned into three numbers.
        // #1087 pinned 21.2 / 38.5 / 40.2 off a maintenance line that either survived or did not; that was
        // our mechanic. The world's answer is that a refuge is built to a robustness spec and holds — so
        // HOLDING is now the overwhelming majority, EMPTY is a visitor's footprint, and FAILED is an event
        // that most buildings simply do not have.
        //
        // WHAT MAKES THIS ABLE TO FAIL, which is the house rule this repo names out loud: it counts all
        // THREE states and demands all three in quantity, with a ceiling as well as a floor on each. An
        // assertion that "most refuges hold" would pass beautifully on a build where every refuge in the
        // game holds — which is the version of this feature that costs nothing and is exactly what the
        // owner's "if for dramatic suspense we need one that does not work" forbids. Both ends are nailed
        // down on all three, so the only build that goes green is one that deals all three states at these
        // rates.
        int holding = 0, empty = 0, failed = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                switch (UndergroundComplex.StateOfTheRefugeOn(body, level))
                {
                    case UndergroundComplex.RefugeState.Holding: holding++; break;
                    case UndergroundComplex.RefugeState.Empty: empty++; break;
                    case UndergroundComplex.RefugeState.Failed: failed++; break;
                    default: break;   // a floor that holds pressure has no refuge to have a state
                }
            }
        }

        int dead = holding + empty + failed;
        Assert.True(dead > 500, $"only {dead} dead floor(s) swept — this net proves nothing.");

        // Measured at this commit over 100 sites: 816 dead floors — 660 holding (80.9%), 133
        // drawn down (16.3%), 23 failed (2.8%), the last of those on 23 of the 100 sites.
        double works = 100.0 * holding / dead;
        Assert.True(works is > 72 and < 90,
            $"{holding} of {dead} refuges ({works:F1}%) still have air in the rack. Pinned at 80.9 %: "
            + "the owner's ruling is that these things almost never fail, and a build that put working air "
            + "on only half of them would be back to #1087's mechanic by a different route.");

        double drawn = 100.0 * empty / dead;
        Assert.True(drawn is > 9 and < 25,
            $"{empty} of {dead} racks were drawn down before the captain arrived ({drawn:F1}%) — pinned at "
            + "16.3 %. It is #573's footprint and not decay: rare enough that finding one still means "
            + "somebody was here, common enough that a captain meets one.");

        double gone = 100.0 * failed / dead;
        Assert.True(gone is > 1 and < 7,
            $"{failed} of {dead} seals have gone ({gone:F1}%) — pinned at 2.8 %. It is an EVENT and the "
            + "card with the painting is the whole of it; at this rate a captain who works a dozen sites "
            + "meets it a few times, which is a story rather than weather.");
    }

    [Fact]
    public void HoldingIsTheDefaultOnEveryDepartmentInEveryBand()
    {
        // #1149 · THE GUARD THAT REDDENS #1087. The old law was a biconditional — a refuge holds if and only
        // if its department kept a maintenance line (ADMINISTRATION and LABORATORIES, plus the head office,
        // minus the band nobody listed). This asserts the opposite, in the shape that can tell pass from
        // fail: every one of the eight departments, the head office, the branch offices AND the band nobody
        // listed all deal HOLDING refuges in quantity.
        //
        // Restore DepartmentsThatKeptTheLine and this goes red on six departments at once — and on the
        // unlisted band, which under the old law kept none of its seals because "a maintenance line is a
        // budget code and there is no budget code for a floor the building refuses to admit it has". That
        // sentence was good and it was ours; the inspectorate that made them build the room does not read
        // the org chart.
        var holdingBy = new Dictionary<string, int>(StringComparer.Ordinal);
        var seenBy = new Dictionary<string, int>(StringComparer.Ordinal);
        int unlistedHolding = 0, unlisted = 0, headHolding = 0, head = 0, branchHolding = 0, branch = 0;

        foreach (string body in ManySites())
        {
            bool headOffice = UndergroundComplex.IsHeadOffice(body);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level) is not { } state)
                {
                    continue;
                }
                bool holds = state == UndergroundComplex.RefugeState.Holding;
                string department = UndergroundComplex.DepartmentOf(body, level);

                seenBy[department] = seenBy.GetValueOrDefault(department) + 1;
                if (holds)
                {
                    holdingBy[department] = holdingBy.GetValueOrDefault(department) + 1;
                }

                if (UndergroundComplex.IsUnlisted(body, level))
                {
                    unlisted++;
                    if (holds) { unlistedHolding++; }
                }
                else if (headOffice)
                {
                    head++;
                    if (holds) { headHolding++; }
                }
                else
                {
                    branch++;
                    if (holds) { branchHolding++; }
                }
            }
        }

        // The world can tell pass from fail, and the shape of "every department" is not the whole plate
        // list — which is worth writing down because it is the sharpest thing this sweep found.
        //
        // A branch cycles eight plates (DepartmentsFor) and the top floor of every four-floor band holds
        // pressure (HoldsPressure), so the two plates at indices 0 and 4 — ADMINISTRATION and ARCHIVE — are
        // the LOBBY plates and never carry a refuge at all. #1087's law gave working air to
        // "ADMINISTRATION and LABORATORIES"; half of that was a department with no refuge in it on any
        // floor of any site in the game. Six branch plates are the ones a refuge can wear, and all six have
        // to be here.
        var expected = new List<string>();
        for (int i = 0; i < UndergroundComplex.Departments.Length; i++)
        {
            if (i % UndergroundComplex.FloorsPerShaft != 0)
            {
                expected.Add(UndergroundComplex.Departments[i]);
            }
        }
        Assert.Equal(6, expected.Count);

        Assert.True(unlisted > 20, $"only {unlisted} floor(s) of the band nobody listed — untested.");
        Assert.True(head > 10, $"only {head} head-office floor(s) — untested.");
        Assert.True(branch > 300, $"only {branch} branch-office floor(s) — untested.");

        foreach (string department in expected)
        {
            int seen = seenBy.GetValueOrDefault(department);
            Assert.True(seen > 30,
                $"{department}: only {seen} refuge(s) swept over a hundred sites — this says nothing.");
            int kept = holdingBy.GetValueOrDefault(department);
            Assert.True(100.0 * kept / seen > 60,
                $"{department}: {kept} of {seen} refuges hold ({100.0 * kept / seen:F1}%). A refuge is a "
                + "regulation and not a maintenance line — no department in this building may be a "
                + "department whose safety equipment does not work.");
        }

        // …and the head office's own un-repeated plates are in here too, so the rule is proved blind to
        // twenty-four more words and not merely to six.
        Assert.True(seenBy.Count > 20,
            $"only {seenBy.Count} distinct plates over a hundred sites — the head office is not in this "
            + "sweep and the rank exception is therefore untested.");

        Assert.True(100.0 * unlistedHolding / unlisted > 60,
            $"{unlistedHolding} of {unlisted} refuges on the band nobody listed hold "
            + $"({100.0 * unlistedHolding / unlisted:F1}%). The floor the building will not admit to still "
            + "had people working on it in suits, and the same inspectorate made somebody pay for the room.");
        Assert.True(100.0 * headHolding / head > 60, "the head office's refuges stopped holding.");
        Assert.True(100.0 * branchHolding / branch > 60, "a branch office's refuges stopped holding.");
    }

    [Fact]
    public void AtMostOneRefugeFailedPerSiteAndNeverTheFirstOneReached()
    {
        // #1149 · The owner's word is that a failed refuge is a thing that HAPPENED, and a thing that
        // happened happens once. Two on one site would be weather.
        //
        // And never the first one a captain reaches, which is the half that makes the beat work: a captain's
        // first refuge is where they learn what a refuge IS, and a first one that will not cycle teaches the
        // opposite of the truth. "First" is read off the order the plan already has (FloorsOf), so it is the
        // first door on any route rather than a second idea of first.
        //
        // WHAT MAKES IT ABLE TO FAIL: it counts the sites that HAVE one and the sites that do not, and
        // demands both in quantity. "At most one per site" is satisfied trivially by a build with none.
        int sitesWithOne = 0, sitesWithNone = 0, reached = 0;

        foreach (string body in ManySites())
        {
            int failedFloors = 0;
            int? first = UndergroundComplex.FirstRefugeFloorOf(body);
            Assert.NotNull(first);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level)
                    != UndergroundComplex.RefugeState.Failed)
                {
                    continue;
                }
                failedFloors++;
                Assert.True(level != first,
                    $"{body} B{-level}: the FIRST refuge a captain can reach on this site is the one that "
                    + "failed. That is the one room in the building that has to work — it is where the rule "
                    + "is taught, and the beat only lands against a rule the captain has already learnt.");
                reached++;

                // …and the site's own answer agrees with the floor's, which is what stops the card, the
                // plate, the fan and the suit reading two different rooms.
                Assert.Equal(level, UndergroundComplex.FailedRefugeFloorOf(body));
                Assert.True(UndergroundComplex.RefugeThatFailedIsOn(body, level));
            }

            Assert.True(failedFloors <= 1,
                $"{body}: {failedFloors} refuges failed on one site. It is an event, not a rate.");
            if (failedFloors == 1) { sitesWithOne++; } else { sitesWithNone++; }
        }

        Assert.True(sitesWithOne is > 10 and < 45,
            $"{sitesWithOne} of 100 sites carry the one that failed — pinned at one site in four. Rare is "
            + "measured against the thing it is rare among, and a captain works a site, not a floor.");
        Assert.True(sitesWithNone > 50,
            $"only {sitesWithNone} sites have no failed refuge at all — most buildings a captain walks are "
            + "buildings where the safety equipment simply works, which is the whole ruling.");
        Assert.True(reached > 10, "no failed refuge was ever reached — this guard proved nothing.");
    }

    [Fact]
    public void TheInspectionTagIsTheCanonsEntriesOnTheSitesOwnClock()
    {
        // #1149 · THE COVERT ORGANISATION'S PARADOX, ON PAPER. Owner: a secret lab's eternal struggle is
        // "not to asphyxiate from unmaintained safety equipment ... while avoiding traceable bureaucracy
        // that could prove complicity if leaked". So: complete, current, unsigned — and the paper says so
        // itself, as a house rule, which is the whole of the characterisation.
        //
        // The two entries are RETYPED from the issue here rather than read off the constants, for the reason
        // ThePapersOwnHeadsTests states: a guard asserting InspectionTagEntry == InspectionTagEntry passes
        // on any sentence anybody ever writes into it.
        const string Entry =
            "Refuge inspected. Rack full, seals within tolerance. No signature — none required.";
        const string Replaced = "Refuge inspected. Rack full. Seal replaced.";
        Assert.Equal(Entry, UndergroundComplex.InspectionTagEntry);
        Assert.Equal(Replaced, UndergroundComplex.InspectionTagSealReplaced);

        int ordinary = 0, failed = 0;
        var years = new HashSet<int>();

        foreach (string body in ManySites())
        {
            int year = UndergroundComplex.InspectionYearOf(body);
            int month = UndergroundComplex.InspectionMonthOf(body);
            years.Add(year);

            // The two stamps are a year apart to the month, off the site's own clock, and the clock sits in
            // the era the rest of the game keeps (ShipHistory lays hulls down 2270..2319 against a present
            // of roughly 2341) — so an inspection on this tag is decades old, which is the sentence every
            // other surface down here is already telling in words.
            Assert.InRange(year, 2270, 2319);
            Assert.InRange(month, 1, 12);
            string first = UndergroundComplex.InspectionTagStamp(year, month);
            string second = UndergroundComplex.InspectionTagStamp(year + 1, month);
            Assert.NotEqual(first, second);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level) is not { } state)
                {
                    // No refuge on the plan, no tag: the paper is a property of the room and there is no
                    // room. (A floor that breathes IS the refuge and carries no valve to hang one on.)
                    Assert.Null(UndergroundComplex.AuthoredPaperOf(
                        UndergroundComplex.FindId(body, level, UndergroundComplex.RefugeTagRoom)));
                    continue;
                }

                string tag = UndergroundComplex.InspectionTagLine(body, level);

                // Every tag in the game: the same entry twice, stamped a year apart, in that order.
                Assert.Contains(first + Entry, tag, StringComparison.Ordinal);
                Assert.Contains(second + Entry, tag, StringComparison.Ordinal);
                Assert.True(
                    tag.IndexOf(first, StringComparison.Ordinal)
                        < tag.IndexOf(second, StringComparison.Ordinal),
                    $"{body} B{-level}: the tag reads back to front.");

                if (state == UndergroundComplex.RefugeState.Failed)
                {
                    failed++;

                    // THE THIRD ENTRY, AND IT HAS NO DATE ON IT. That is the beat's second half and it is
                    // delivered by an absence: a book that has never once failed to stamp a line did not
                    // stamp this one.
                    //
                    // The assertion is that the second entry runs STRAIGHT into the third with one space
                    // between them, which is the only shape that leaves nowhere for a stamp to be. This was
                    // first written as "no year appears after the third entry begins", and that read the
                    // wrong side of the join: a stamp sits BEFORE its entry, so a dated third entry sailed
                    // through it. A guard handed a world that cannot tell pass from fail is a bug class this
                    // repo has a name for, and it was this one.
                    Assert.EndsWith(Entry + " " + Replaced, tag, StringComparison.Ordinal);
                    for (int y = year - 1; y <= year + 3; y++)
                    {
                        Assert.Equal(
                            state == UndergroundComplex.RefugeState.Failed && (y == year || y == year + 1),
                            tag.Contains(
                                y.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                StringComparison.Ordinal));
                    }
                }
                else
                {
                    ordinary++;
                    Assert.DoesNotContain(Replaced, tag, StringComparison.Ordinal);
                    Assert.EndsWith(Entry, tag, StringComparison.Ordinal);
                }

                // …and the paper is reachable through the one seam every other authored paper is read
                // through, on a room index no floor's room list can hold.
                string findId = UndergroundComplex.FindId(body, level, UndergroundComplex.RefugeTagRoom);
                Assert.Equal(PaperHeads.Paper.InspectionTag, UndergroundComplex.AuthoredPaperOf(findId));
                Assert.Equal("An inspection tag", FieldClue.Title(findId));
                Assert.Equal(Entry, FieldClue.Document(findId));
            }
        }

        // The world can tell pass from fail: both kinds of tag really occur, and the clock really is the
        // SITE'S — one year for every site would be a constant wearing a function's clothes.
        Assert.True(ordinary > 500, $"only {ordinary} ordinary tag(s) swept.");
        Assert.True(failed > 10, $"only {failed} failed refuge(s) swept — the third entry is untested.");
        Assert.True(years.Count > 20,
            $"only {years.Count} distinct inspection years over 100 sites — this is not a site's own clock.");
    }

    [Fact]
    public void TheRoomAgreesWithTheLawAboutItsOwnDoor()
    {
        // One answer, everywhere. The suit asks StateOfTheRefugeOn, the tracker asks it, the panel asks it,
        // and the ROOM the renderer draws carries the answer it was built with — so nothing on screen can
        // report a seal the sim is not running. A room holding a second opinion about its own door is this
        // repo's oldest and most expensive shape.
        AuditEveryFloor((body, level, floor) =>
        {
            UndergroundComplex.RefugeState? law = UndergroundComplex.StateOfTheRefugeOn(body, level);
            if (law is null)
            {
                return floor.Refuges.Count == 0 ? null
                    : "the law says this floor has no refuge and the generator built one.";
            }
            foreach (UndergroundComplex.Refuge r in floor.Refuges)
            {
                if (r.State != law)
                {
                    return $"the room says {r.State} and the law says {law}.";
                }
            }
            return floor.Refuges.Count > 0 ? null
                : "the law marks a refuge on the plan and the generator built none.";
        }, "the room the renderer draws carries the seal the suit is reading");
    }

    [Fact]
    public void TheSameSealIsFoundOnEveryVisit()
    {
        // A captain is meant to LEARN a building (the same reason the refuge itself never moves). A seal
        // that was re-rolled per ride would be worse than a random one, because the captain would have
        // walked back to a room that worked yesterday.
        foreach (string body in new[] { "miranda", "titan", "generated-moon-7", "generated-moon-61" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Assert.Equal(
                    UndergroundComplex.StateOfTheRefugeOn(body, level),
                    UndergroundComplex.StateOfTheRefugeOn(body, level));
            }
        }
    }

    [Fact]
    public void ThePanelSaysARefugeIsThereAndNeverWhetherItHolds()
    {
        // #608's hardest requirement is that the captain can learn about the air BEFORE they need it — "a
        // refuge you discover AFTER you needed it is a cruelty" — and the panel is where a captain looks
        // before a ride. So the row carries the plan's own fact, and the plan is a drawing made when the
        // building was new: it knows the room is there and it does not know whether the compressor turns.
        var shapesByState = new Dictionary<UndergroundComplex.RefugeState, HashSet<string>>();
        int tagged = 0, plain = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.LiftStop row in UndergroundComplex.LiftPanel(
                    body, level, UndergroundComplex.ShaftKind.Cage, []))
                {
                    Assert.True(row.HasRefuge == UndergroundComplex.RefugeOnThePlan(body, row.Level),
                        $"{body} B{-row.Level}: the row says HasRefuge={row.HasRefuge} and the plan says "
                        + $"{UndergroundComplex.RefugeOnThePlan(body, row.Level)}.");

                    if (row.HasRefuge) { tagged++; } else { plain++; }

                    // …AND THE ROW LOOKS THE SAME IN ALL THREE STATES. Every drawable thing about the button
                    // is written down and grouped by the seal behind it; if any of it leaked the state, one
                    // state would own a shape the others do not have.
                    if (UndergroundComplex.StateOfTheRefugeOn(body, row.Level) is not { } state)
                    {
                        continue;
                    }
                    string shape = string.Join('|',
                        row.HasRefuge, row.Pressurised, row.IsCurrent,
                        row.Refusal is null, row.OpenedBy is null, row.OpenedByChit);
                    if (!shapesByState.TryGetValue(state, out HashSet<string>? set))
                    {
                        shapesByState[state] = set = [];
                    }
                    set.Add(shape);
                }
            }
        }

        Assert.True(tagged > 500, $"only {tagged} tagged row(s) — this sweep proved little.");
        Assert.True(plain > 100, $"only {plain} untagged row(s) — the negative case is untested.");
        Assert.Equal(3, shapesByState.Count);   // all three seals really appear on the panel

        foreach (UndergroundComplex.RefugeState state in shapesByState.Keys)
        {
            foreach (UndergroundComplex.RefugeState other in shapesByState.Keys)
            {
                foreach (string shape in shapesByState[state])
                {
                    Assert.True(shapesByState[other].Contains(shape),
                        $"a row shape ({shape}) occurs behind a {state} seal and never behind a {other} "
                        + "one — the panel has started telling the captain which refuges work, and the walk "
                        + "to find out is the whole feature.");
                }
            }
        }

        // The tag itself says the room and nothing about the room's state.
        foreach (string word in new[] { "AIR", "SEAL", "EMPTY", "FAILED", "WORK", "DEAD" })
        {
            Assert.DoesNotContain(word, UndergroundComplex.RefugeRowTag, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheDeadAirCardStopsPromisingWhatTheBuildingCannotKeep()
    {
        // #608's last "Done when": the card said "there are no shelters down here" before the refuges
        // existed, and the day they landed that became the most dangerous sentence in the game. It now says
        // the opposite — and, since only a fifth of the racks have anything in them, it may not let the
        // captain read the good news as air either.
        int cards = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.HoldsPressure(body, level))
                {
                    continue;
                }
                string card = UndergroundComplex.VacuumCard(body, level, 900);
                cards++;
                Assert.Contains(
                    "The plan marks a refuge on this band. Whether it still holds is not on the plan.",
                    card, StringComparison.Ordinal);
                Assert.DoesNotContain("no shelters", card, StringComparison.OrdinalIgnoreCase);
            }
        }
        Assert.True(cards > 500, $"only {cards} dead-air card(s) read — this sweep proved little.");
    }
}
