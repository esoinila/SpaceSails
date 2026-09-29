using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1318 (owner ruling 2026-09-29) · <b>THE FACILITY PLATE STANDS BESIDE THE SHAFT.</b> <i>"Our UI should not be
/// the challenge."</i> The ▣ plate stood 30 du left of the car, at the screen's left edge under the AIR readout at
/// boot, so B21's payoff (a different building's name) had to be walked to. On every floor that carries it — B1
/// of every site, and the unlisted band's lobby (B21 on <c>?secretlab=deep</c>) — it now stands just right of
/// the car's pocket: within a few du of the pocket's wall, clear of the depth plate over the car, of every
/// console's [E] reach and of every door.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFacilityPlateStandsBesideTheShaftTests
{
    private static IEnumerable<(string Body, int Level)> FloorsWithThePlate()
    {
        foreach (string body in new[] { "luna", "phobos", "titan", "europa", "miranda", "secret-lab-site-unlisted" })
        {
            for (int level = -1; level >= -30; level--)
            {
                if (UndergroundComplex.ShowsFacilityPlate(body, level))
                {
                    yield return (body, level);
                }
            }
        }
    }

    /// <summary>
    /// BESIDE THE CAR, ON SCREEN, ON NOTHING. For every floor that carries it: the plate's centre is right of the
    /// car's pocket and no more than a dozen du from the shaft (the boot's own camera is on the shaft); it is at
    /// least 4 du below the depth plate's lowest line; it is further than [E]'s reach from every console; and no
    /// door's leaf lies within 4 du of it. Includes B21 of the deep site (<c>▣ THE TRANSIT STATION</c>).
    ///
    /// <para><b>RED</b> on revert (the plate back at <c>shaftX - 30</c>): every floor's plate stood 30 du from the
    /// shaft.</para>
    /// </summary>
    [Fact]
    public void ThePlateStandsBesideTheShaftAndOnNothing()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        (double sx, double sy) = UndergroundComplex.ShaftAt(field);
        int floors = 0;
        bool sawB21 = false;
        foreach ((string body, int level) in FloorsWithThePlate())
        {
            DeckPlan deck = HiveInterior.FloorDeck(body, level, field, 0, (_, _) => { }, []);
            string title = UndergroundComplex.TitleOf(UndergroundComplex.KindOn(body, level));
            var plate = Assert.Single(deck.RoomLabels, l => l.Text == title);
            floors++;
            sawB21 |= body == "secret-lab-site-unlisted" && level == -21 && title == "▣ THE TRANSIT STATION";

            double off = plate.X - sx;
            Assert.True(off > UndergroundComplex.ShaftHalf && off <= 12.5,
                $"{body} B{-level}: the plate stands {off:F1} du from the shaft's centre");
            Assert.True(Math.Abs(plate.Y - (sy + 4.5)) < 1e-3, $"{body} B{-level}: the plate left its height on the wall");
            float lowestDepthLine = deck.BigLabels.Min(b => b.Y);
            Assert.True(lowestDepthLine - plate.Y >= 4, $"{body} B{-level}: the plate crowds the depth plate");

            foreach (DeckPlan.ConsoleSpot c in deck.Consoles)
            {
                double d = Math.Sqrt(((c.X - plate.X) * (c.X - plate.X)) + ((c.Y - plate.Y) * (c.Y - plate.Y)));
                Assert.True(d > DeckPlan.InteractRadius, $"{body} B{-level}: the plate is on {c.Label} ({d:F1} du)");
            }

            foreach (DeckPlan.Door door in deck.Doors)
            {
                double cx = Math.Clamp(plate.X, Math.Min(door.X1, door.X2), Math.Max(door.X1, door.X2));
                double cy = Math.Clamp(plate.Y, Math.Min(door.Y1, door.Y2), Math.Max(door.Y1, door.Y2));
                double d = Math.Sqrt(((cx - plate.X) * (cx - plate.X)) + ((cy - plate.Y) * (cy - plate.Y)));
                Assert.True(d > 4, $"{body} B{-level}: the plate is on a door ({d:F1} du)");
            }
        }

        Assert.True(floors >= 6, $"premise: only {floors} floor(s) carry the plate");
        Assert.True(sawB21, "premise: B21 of the deep site carries ▣ THE TRANSIT STATION");
    }
}
