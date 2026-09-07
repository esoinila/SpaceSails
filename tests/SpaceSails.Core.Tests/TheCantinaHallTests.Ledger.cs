using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// <b>WHAT THE HALL COSTS THE FLOOR, AND WHAT ITS CARDS SAY</b> — the claim ledger and the cards of
/// <see cref="TheCantinaHallTests"/>.
///
/// <para>What this part owns is the hall as an entry in somebody else's books: a hall costs the floor its
/// own column and nothing else, its card belongs to the carrier's canteen and to no other floor, and both
/// cards are wired verbatim and neither EXPLAINS anything.</para>
/// </summary>
public sealed partial class TheCantinaHallTests
{
    // ── THE CLAIM LEDGER ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A hall costs the floor exactly the column it stands on — and never a room the floor was going to
    /// build somewhere else.
    ///
    /// <para>The fact the PR body states, measured rather than asserted: on a hall floor the room stock is
    /// down by the two slots of one rib column against the same floor built without one, no room centre is
    /// inside the hall's box, and the floor is still worth the lift ride.</para>
    /// </summary>
    [Fact]
    public void AHallCostsTheFloorItsOwnColumnAndNothingElse()
    {
        int floors = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                UndergroundComplex.Amenity? hallAmenity = null;
                foreach (UndergroundComplex.Amenity a in floor.Amenities)
                {
                    if (a.Hall is not null)
                    {
                        hallAmenity = a;
                    }
                }
                if (hallAmenity is not { } amenity || amenity.Hall is not { } hall)
                {
                    Assert.False(UndergroundComplex.IsHallFloor(body, level),
                        $"{body} B{-level} is a hall floor and no hall was carved on it.");
                    continue;
                }

                floors++;

                // NOTHING ELSE IS STANDING ON IT. A room drawn inside the hall's walls is #585's stranded
                // room with better furniture.
                foreach ((double rx, double ry) in floor.RoomCentres)
                {
                    Assert.False(hall.Contains(rx, ry),
                        $"{body} B{-level}: a room at ({rx:F1}, {ry:F1}) is inside the hall.");
                }
                foreach (UndergroundComplex.EnSuite cell in floor.EnSuites)
                {
                    Assert.False(hall.Contains(cell.X, cell.Y),
                        $"{body} B{-level}: an en-suite is inside the hall.");
                }
                foreach (UndergroundComplex.LockedDoor door in floor.Locked)
                {
                    // #775 · …EXCEPT THE ONE THE ROOM OWNS. The goods hoist's shutter is a sealed door
                    // standing inside the hall on purpose: freight access is a fixture in the counter's own
                    // service band, and this building's grammar for "that will not open for you" is a locked
                    // door with a plate on it. The exemption is by IDENTITY — the very segment the hall
                    // published — so the law still catches a chamber's door swallowed by the room, which is
                    // the thing it was written about.
                    bool isTheHoist = hall.Freight is { } hoist
                        && Math.Abs(door.X1 - hoist.Shutter.X1) < 0.001
                        && Math.Abs(door.Y1 - hoist.Shutter.Y1) < 0.001
                        && Math.Abs(door.X2 - hoist.Shutter.X2) < 0.001
                        && Math.Abs(door.Y2 - hoist.Shutter.Y2) < 0.001;
                    Assert.True(
                        isTheHoist || !hall.Contains((door.X1 + door.X2) / 2, (door.Y1 + door.Y2) / 2),
                        $"{body} B{-level}: a sealed door is inside the hall.");
                }

                // …and neither car is, which are the two spots every excursion stands on (#801).
                foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
                {
                    Assert.False(hall.Contains(car.X, car.Y),
                        $"{body} B{-level}: the hall swallowed the {car.Kind} car.");
                    Assert.False(hall.Contains(car.Landing.X, car.Landing.Y),
                        $"{body} B{-level}: the hall swallowed the {car.Kind} car's doorstep.");
                }

                // The floor is still a facility rather than a flat — over the sites the game actually
                // ships. Scoped exactly the way TheHiveAmenitiesTests scopes the same law and for the same
                // stated reason: "at least four places" fails on a handful of GENERATED moons that were
                // tight long before any of this, and a guard that ships red teaches everybody to scroll
                // past it. The client's own AFloorIsWorthTheLiftRide is the shipped-sites law.
                if (Bodies.Contains(body))
                {
                    Assert.True(floor.RoomCentres.Count + floor.Amenities.Count >= 4,
                        $"{body} B{-level}: {floor.RoomCentres.Count} rooms and {floor.Amenities.Count} "
                        + "amenities — the hall ate the floor.");
                }

                // And the top floor keeps its washroom beside the wet stack it was always beside.
                if (UndergroundComplex.TopPressurisedFloor(body) == level)
                {
                    Assert.Contains(floor.Amenities, a => a.Use == UndergroundComplex.Comfort.Washroom);
                }
            }
        }

        Assert.True(floors > 70, $"only {floors} hall floors were walked — this proved little.");
    }

    // ── THE CARDS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The hall's card belongs to the room whose sign it quotes, and to no other floor of any
    /// building. It is the branch office's CANTEEN 1 — the head office's dining room has its own register
    /// and its own arrival card (#411).</summary>
    [Fact]
    public void TheHallCardBelongsToTheCarriersCanteenAndToNoOtherFloor()
    {
        var wrong = new List<string>();
        int fired = 0, floors = 0;

        foreach (string body in Sweep())
        {
            int? top = UndergroundComplex.TopPressurisedFloor(body);
            bool branch = !UndergroundComplex.IsHeadOffice(body);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                bool fires = UndergroundComplex.ShowsCantinaHallCard(body, level);
                bool want = branch && top == level;
                if (fires != want)
                {
                    wrong.Add($"  {body} B{-level}: {(fires ? "fired" : "silent")}, wanted {(want ? "yes" : "no")}");
                }
                if (fires)
                {
                    fired++;
                }
            }
        }

        Assert.True(floors > 300, $"only {floors} floors walked — this proved little.");
        Assert.True(fired > 40, $"the card fires on only {fired} floors — this proved little.");
        Assert.True(wrong.Count == 0, "the hall card fires on the wrong floors:\n" + string.Join("\n", wrong));

        // The head office is in the sweep and is the reason the clause exists.
        Assert.True(UndergroundComplex.IsHeadOffice("enceladus"), "the sweep no longer holds a head office.");
        Assert.False(UndergroundComplex.ShowsCantinaHallCard(
            "enceladus", UndergroundComplex.TopPressurisedFloor("enceladus")!.Value));
    }

    /// <summary>Both cards, verbatim, and neither explains a thing. The prose is the owner-approved text and
    /// this is where a reword goes red.</summary>
    [Fact]
    public void BothCardsAreWiredVerbatimAndNeitherEXPLAINSAnything()
    {
        Assert.Equal(
            "Carriers' canteen, the sign says, and the room says something else: steel tables wiped to a "
            + "shine, brass on the pillars, light somebody chose. On a rock with no name on any chart, the "
            + "company feeds its contractors like a hotel feeds guests it wants to keep — and nobody at the "
            + "tables finds that strange, because the pay is on the nail, the coffee is real, and questions "
            + "are the one thing on the menu that costs. Along the back wall, a row of doors. Cabinets, by "
            + "arrangement. The hall is loud. The doors are why.",
            UndergroundComplex.CantinaHallCard);

        Assert.Equal(
            "Six chairs, a table wiped past clean, and a door padded like a vault that dogs shut from "
            + "inside. The hall outside is loud the way a sea is loud — a noise you can hide a sentence in, "
            + "but every face out there sits in the counter's long memory. In here there is no memory: "
            + "whatever crosses this table crosses it once and leaves in the pockets it came in. There is a "
            + "telephone on the wall. It has no dial. Rooms like this are not on the menu — you arrange "
            + "them, or you are brought.",
            UndergroundComplex.CabinetCard);

        Assert.Equal(
            "A cabinet off the hall: six chairs, one door, and no line of sight to the counter. Rooms like "
            + "this are why the hall is loud.",
            UndergroundComplex.CabinetNote);

        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];
        foreach (string text in new[]
        {
            UndergroundComplex.CantinaHallCard, UndergroundComplex.CabinetCard,
            UndergroundComplex.CabinetNote, UndergroundComplex.CantinaHallLabel,
            UndergroundComplex.CabinetLabel,
        })
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        // The cabinet's card counts the chairs out loud, so the room had better have six of them.
        Assert.Equal(6, UndergroundComplex.CabinetSeats);
        Assert.Contains("Six chairs", UndergroundComplex.CabinetCard, StringComparison.Ordinal);
        Assert.Contains("six chairs", UndergroundComplex.CabinetNote, StringComparison.Ordinal);

        // Both name a slot that can actually be painted, and they are not the same picture.
        foreach (string url in new[]
            { UndergroundComplex.CantinaHallArtUrl, UndergroundComplex.CabinetArtUrl })
        {
            Assert.StartsWith("art/", url, StringComparison.Ordinal);
            Assert.EndsWith(".jpg", url, StringComparison.Ordinal);
        }
        Assert.NotEqual(UndergroundComplex.CantinaHallArtUrl, UndergroundComplex.CabinetArtUrl);
        Assert.NotEqual(UndergroundComplex.StaffMessArtUrl, UndergroundComplex.CantinaHallArtUrl);
    }
}
