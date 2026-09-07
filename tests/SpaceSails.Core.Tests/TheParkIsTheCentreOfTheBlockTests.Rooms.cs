using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #813 · <b>WHAT A RING ROOM IS, AND WHAT ITS PLATE IS ALLOWED TO SAY</b> — sections (g2), (g3) and (h) of
/// <see cref="TheParkIsTheCentreOfTheBlockTests"/>.
///
/// <para>What this part owns is the ring read from the inside: a plate says what the room is and never what
/// the place is for, no plate stands in its own doorway (a sign you have to step off the street to read is
/// not signage), and the rooms the owner asked for by name — the ones with the view — are the premium ones
/// while the corners are not.</para>
/// </summary>
public sealed partial class TheParkIsTheCentreOfTheBlockTests
{
    // ── (g2) WHAT IS WRITTEN ON THE RING ──────────────────────────────────────────────────────────────

    /// <summary>
    /// §13.8 · THE ROOMS WITH THE VIEW SAY WHAT A ROOM IS AND NEVER WHAT THE PLACE IS FOR.
    ///
    /// <para>The block's own register (<see cref="UndergroundComplex.ParkViewPlates"/>) is the one piece of
    /// new prose the Manhattan ruling ships, and it is six lines. Every one of them names a booking, a
    /// signature or an appointment; none of them names the facility, its purpose, or anything a captain
    /// could take to an inspector. The nearest any of them comes to a sentence is #770's negotiation room,
    /// and all that plate says is where you book it.</para>
    ///
    /// <para>…and it is HUNG WHERE THE VIEW IS, which is the amenity gradient (#775) as signage: a corner
    /// room, which stands past the end of the park's own wall, wears the corridors' ordinary vocabulary
    /// instead. Read along one wall of the block and the rooms get better as the green comes into
    /// view.</para>
    /// </summary>
    [Fact]
    public void TheRingsOwnPlatesSayNothingTheBuildingWouldNotSay()
    {
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];
        foreach (string plate in UndergroundComplex.ParkViewPlates)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, plate, StringComparison.OrdinalIgnoreCase);
            }
            Assert.Equal(plate.ToUpperInvariant(), plate);
            Assert.False(plate.EndsWith('.'), $"\"{plate}\" is a sentence — a stencil is not.");
        }
        Assert.Equal(
            UndergroundComplex.ParkViewPlates.Count,
            UndergroundComplex.ParkViewPlates.Distinct(StringComparer.Ordinal).Count());

        // …and it is on the ring, on the rooms with the view, and NOT on the corners.
        int viewed = 0, cornered = 0, blocks = 0;
        foreach ((string body, int level, UndergroundComplex.Hall _, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan _) in EveryBlock())
        {
            blocks++;
            if (UndergroundComplex.IsFound(body, level))
            {
                continue;   // a gallery has no plates at all (#677), which is a different law
            }
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (room.Side == UndergroundComplex.RingSide.Far)
                {
                    Assert.Contains(room.Plate, UndergroundComplex.ParkBackPlates);
                    continue;
                }
                if (room.HasView)
                {
                    viewed++;
                    Assert.Contains(room.Plate, UndergroundComplex.ParkViewPlates);
                }
                else
                {
                    cornered++;
                    Assert.DoesNotContain(room.Plate, UndergroundComplex.ParkViewPlates);
                }
            }
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were read — this proved little.");
        Assert.True(viewed > 100, $"only {viewed} rooms with a view were read — this proved little.");
        Assert.True(cornered > 40, $"only {cornered} corner rooms were read — the gradient is untested.");
    }

    // ── (g3) A SIGN YOU HAVE TO STEP OFF TO READ IS NOT SIGNAGE ───────────────────────────────────────

    /// <summary>
    /// #775's LESSON, RE-LEARNED ON FOURTEEN ROOMS A FLOOR: no ring room's plate stands on its own doorway.
    ///
    /// <para>#775 paid for this once already, on the hall's own front doors: <i>"a plate centred on its own
    /// doorway is a plate with the captain standing on top of it the moment they arrive — watched happen in
    /// the browser on the first boot of <c>?frontdoor=1</c>, the dot sitting squarely on the word
    /// CANTEEN."</i> The block shipped the identical mistake on every room of the ring, and it was found the
    /// identical way: booting <c>?parkwalk=1</c> and reading the pixels, with the avatar sitting in the
    /// middle of PRIVILEGED RECORDS · READING ROOM.</para>
    ///
    /// <para>Which is the argument for this guard rather than for a careful placer. The geometry was right,
    /// every other law in this file was green, and the only instrument that could see it was a screenshot.
    /// A law that can only be checked by eye gets checked once; this one is checked on every floor.</para>
    ///
    /// <para><b>Proven RED</b> by putting the plate back on the door's own centre
    /// (<c>plateAt = mid</c> in <c>RingBox</c>):</para>
    /// <code>
    /// 728 plate(s) are standing in their own doorway:
    ///   luna B1: ring room 1 (NEAR) — CONSENT FILES — reads from 0.0 du off the middle of its own door.
    ///   luna B1: ring room 2 (NEAR) — SENIOR ROTA · GREEN SIDE — reads from 0.0 du off the middle of its
    ///     own door.
    ///   luna B1: ring room 3 (NEAR) — PRIVILEGED RECORDS · READING ROOM — reads from 0.0 du off the middle
    ///     of its own door.
    ///   luna B1: ring room 5 (FAR) — 🚿 WASH-DOWN — reads from 0.0 du off the middle of its own door.
    /// </code>
    ///
    /// <para>Ring room 3 is the one in the screenshot: PRIVILEGED RECORDS · READING ROOM, with the avatar
    /// sitting on the word RECORDS.</para>
    /// </summary>
    [Fact]
    public void NoRingRoomsPlateStandsInItsOwnDoorway()
    {
        var wrong = new List<string>();
        int blocks = 0, plates = 0;

        foreach ((string body, int level, UndergroundComplex.Hall _, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;
            if (UndergroundComplex.IsFound(body, level))
            {
                continue;   // a gallery hangs no plates at all (#677)
            }

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                bool horizontal = room.Side is UndergroundComplex.RingSide.Near
                    or UndergroundComplex.RingSide.Far;
                double doorAt = horizontal
                    ? (room.Door.X1 + room.Door.X2) / 2.0
                    : (room.Door.Y1 + room.Door.Y2) / 2.0;
                (double lo, double hi) = Frontage(room);

                // The label this room hung: its own text, standing within the room's own span and within a
                // few du of the wall its door is in. Matched off the published box rather than by index,
                // because plates repeat and an index would pair a room with somebody else's sign.
                bool found = false;
                foreach (SurfaceLayout.Landmark mark in floor.Labels)
                {
                    if (!string.Equals(mark.Label, room.Plate, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    double along = horizontal ? mark.X : mark.Y;
                    double across = horizontal ? mark.Y : mark.X;
                    double street = horizontal
                        ? (room.Side == UndergroundComplex.RingSide.Near ? room.Y1 : room.Y0)
                        : (room.Side == UndergroundComplex.RingSide.West ? room.X0 : room.X1);
                    if (along < lo - 0.5 || along > hi + 0.5 || Math.Abs(across - street) > 4.0)
                    {
                        continue;   // somebody else's plate that happens to read the same
                    }

                    found = true;
                    if (Math.Abs(along - doorAt) < UndergroundComplex.DoorHalf)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                            + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate} — reads from "
                            + $"{Math.Abs(along - doorAt):F1} du off the middle of its own door.");
                    }

                    // …and it stays on its OWN frontage. A plate stepped aside so far that it hangs over
                    // the neighbour's wall is a different way of being unreadable.
                    if (along < lo + 0.5 || along > hi - 0.5)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number}'s plate at {along:F1} is "
                            + $"outside its own frontage ({lo:F1}…{hi:F1}).");
                    }
                }

                if (!found)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                        + $"({room.Side.ToString().ToUpperInvariant()}) hangs no plate the deck draws.");
                }
                else
                {
                    plates++;
                }
            }
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(plates > 500, $"only {plates} plates were read — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} plate(s) are standing in their own doorway:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    // ── (h) THE ROOMS THE OWNER ASKED FOR ─────────────────────────────────────────────────────────────

    /// <summary>
    /// THEY ARE THE BEST ROOMS IN THE BUILDING, and the plan says so in floor area. A ring room with a view
    /// is several times an ordinary chamber; a CORNER room — one that stands past the end of the park's own
    /// wall and has nothing to look at — is the cheap one, which is #775's amenity gradient drawn on the
    /// deck rather than written in a sentence.
    /// </summary>
    [Fact]
    public void TheRoomsWithTheViewArePremiumAndTheCornersAreNot()
    {
        var wrong = new List<string>();
        int blocks = 0;
        double chamber = UndergroundComplex.RoomWidthDu * UndergroundComplex.RoomHeightDu;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan _) in EveryBlock())
        {
            blocks++;

            Assert.True(park.Frontage.Count >= 8,
                $"{body} B{-level}: {park.Frontage.Count} rooms on the whole ring.");

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (room.FloorDu2 < chamber)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} is {room.FloorDu2:F0} du², "
                        + $"under the building's own chamber ({chamber:F0} du²).");
                }
                if (room.Plate.Length == 0 && !UndergroundComplex.IsFound(body, level))
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} has no plate on it.");
                }
            }

            // At least one corner room, and the corners have no view — the gradient exists.
            Assert.Contains(park.Frontage, r => !r.HasView);
            Assert.Contains(park.Frontage, r => r.HasView);

            // …and the park still dwarfs the hall that looks at it, which is #759's own law re-anchored.
            double hallFloor = (hall.X1 - hall.X0) * (hall.Y1 - hall.Y0);
            Assert.True(park.FloorDu2 >= 1.5 * hallFloor,
                $"{body} B{-level}: {park.FloorDu2:F0} du² of park behind {hallFloor:F0} du² of hall.");
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} ring room(s) are not what was asked for:\n" + string.Join("\n", wrong));
    }
}
