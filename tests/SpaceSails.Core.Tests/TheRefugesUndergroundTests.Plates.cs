using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #608 · <b>WHAT THE ROOM SAYS, AND WHAT IT REFUSES TO SAY</b> — the second part of
/// <see cref="TheRefugesUndergroundTests"/>.
///
/// <para>What this part owns is the refuge as a thing with WRITING on it: the plate that names the room and
/// stops, the sign that never explains what this place was for, and the rule that no paperwork comes out of
/// a room nobody could have breathed in — with the determinism law that says the same room answers the same
/// way after the air rule has been applied to it. Where the refuge IS, and the air it holds, are in the file
/// that carries the class docblock.</para>
/// </summary>
public sealed partial class TheRefugesUndergroundTests
{
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
}
