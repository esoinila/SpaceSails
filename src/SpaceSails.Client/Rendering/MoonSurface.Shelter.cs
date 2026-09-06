using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #573/#563 slice 3 · ONE SHELTER, PUT ON A PLAN — its shell, its imported door, its two services and
/// its label; plus the apron and the shadow that go under a landed boat, and the ship's own hatch door
/// that has to be dropped so the tube's doors own the threshold.
///
/// <para>The builder was inline and laid only the FIRST shelter while the beacons were switched to
/// every one of them, so rings pointed at buildings that had never been built. It is a named
/// procedure now, and every line of it is the line that was inline, in the order it was in.</para>
///
/// <para>Split out of <c>MoonSurface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class MoonSurface
{
    /// <summary>
    /// #573/#563 slice 3 · ONE SHELTER, PUT ON A PLAN — its shell, its imported door, its two services and
    /// its label.
    ///
    /// <para><b>One body, two grounds.</b> The home tile's build (above) and the lattice's tile compose
    /// (<c>Map.TileRegion</c>) both lay shelters now, and a rule expressed twice is this project's fourth
    /// named bug class — the version that ships is the one where only one of the two got edited. #573's own
    /// scar is exactly that shape: the beacons were switched to every shelter and the builder was left laying
    /// the first, so rings pointed at buildings that had never been built.</para>
    ///
    /// <para>Every line below is the line that was inline here, in the order it was in, so the ground at the
    /// tube is byte for byte what it was.</para>
    /// </summary>
    public static void FurnishShelter(
        in SurfaceStructure.Spec shelter,
        List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors,
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels)
    {
        SurfaceStructure.Built built = SurfaceStructure.Build(shelter);
        foreach (SurfaceLayout.Wall w in built.Walls)
        {
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false, IsStone: true));
        }
        foreach (SurfaceStructure.Doorway d in built.Doorways)
        {
            // #592 · A shelter's door is ALWAYS imported, and that is not a hint — it is the truth about
            // the building. Nobody swages a pressure door out of regolith; somebody flew this out here
            // for strangers to find. It also means the one door on the field that will definitely save
            // your life is the one that reads differently from every wall around it.
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: true));
            // #585 · ITS OWN KIND, NOT THE SHIP'S HATCH. Owner, mid-excursion: "how did I just go to ship
            // from a shelter ... what happened" / "I was at surface shelter and now at ship shuttle bay
            // ... how". Because this console was laid as SurfaceAirlock — the SAME kind as the down
            // tube's "BOARD THE SHUTTLE" — so [E] on a shelter door ran the boarding path and flew him
            // home from the middle of the field, excursion and all.
            //
            // A console kind is a VERB, and two different doors were sharing one. Reusing it read as
            // tidy ("they are both doors") and was the same two-things-one-name mistake as every other
            // expensive bug on this ground.
            consoles.Add(new(DeckPlan.ConsoleKind.ShelterDoor,
                (float)d.CentreX, (float)d.CentreY, SurfaceShelter.DoorLabel));
        }
        consoles.Add(new(DeckPlan.ConsoleKind.ShelterTank,
            (float)shelter.CentreX, (float)shelter.CentreY, SurfaceShelter.TankLabel));
        consoles.Add(new(DeckPlan.ConsoleKind.ShelterLocker,
            (float)shelter.CentreX, (float)(shelter.CentreY - 5.5), SurfaceShelter.LockerLabel));
        labels.Add(((float)shelter.CentreX, (float)(shelter.CentreY - 7), "⛺ SHELTER"));
    }

    /// <summary>#649 · Lay a swept apron ring around the deep anchor, or lay nothing at all. One function so
    /// the two objects that have ceremony cannot drift apart in how it is drawn, and so the grounds that have
    /// none get nothing rather than somebody else's.</summary>
    private static void AddApron(List<SurfaceScenery.Mark> scenery, (double Radius, int Segments)? apron)
    {
        if (apron is not { } a || a.Segments <= 0)
        {
            return;
        }
        for (int i = 0; i < a.Segments; i++)
        {
            double a0 = i / (double)a.Segments * Math.Tau;
            double a1 = (i + 1) / (double)a.Segments * Math.Tau;
            scenery.Add(new SurfaceScenery.Mark(
                AnchorX + (Math.Cos(a0) * a.Radius), AnchorY + (Math.Sin(a0) * a.Radius),
                AnchorX + (Math.Cos(a1) * a.Radius), AnchorY + (Math.Sin(a1) * a.Radius),
                SurfaceScenery.Kind.Ridge));
        }
    }

    /// <summary>#649 · THE MONOLITH'S SHADOW — a lane of dark running up-field from the slab's face to the
    /// far end of the walked world, drawn and never collided.
    ///
    /// <para>Every number is the object's own (<see cref="Monolith.ShadowLengthDu"/>, <c>HalfWidth</c>,
    /// <c>ShadowSpread</c>) and none of them is typed here, which is the point: this is a picture OF a
    /// height, and if the height ever changes the picture has to change with it or the drawing starts lying
    /// about the thing it is drawn from — bug class 3, the one this ground keeps paying for.</para>
    ///
    /// <para>It reaches past the top of the field and is clamped there. Nothing marks where it ends, because
    /// the true edge of it is over the horizon and saying so would be the game explaining itself.</para>
    /// </summary>
    private static void AddShadow(List<SurfaceScenery.Mark> scenery, in SurfaceLayout.Field field)
    {
        double nearY = field.AnchorY + Monolith.HalfHeight;          // the lit face; the shade starts here
        double farY = Math.Min(field.LandingBandY, nearY + Monolith.ShadowLengthDu);
        if (farY <= nearY)
        {
            return;
        }

        double nearHalf = Monolith.HalfWidth;
        double farHalf = Monolith.HalfWidth * Monolith.ShadowSpread;   // a low sun's penumbra is not subtle

        // Nine strokes: the two edges of the umbra and seven streaks inside it. Enough that a captain crossing
        // it reads a REGION of shade rather than a channel with banks — a rille is two lines and this must
        // never be mistaken for one.
        const int Strokes = 9;
        for (int i = 0; i < Strokes; i++)
        {
            double t = (i / (double)(Strokes - 1) * 2.0) - 1.0;       // −1 … +1 across the lane
            // The inner streaks stop short and at staggered lengths, so the far end frays out instead of
            // ending on a line. A shadow with a hem is a wall's shadow; this one belongs to something whose
            // top nobody has seen.
            double reach = i == 0 || i == Strokes - 1 ? 1.0 : 0.55 + (0.4 * ((i * 7 % 5) / 4.0));
            scenery.Add(new SurfaceScenery.Mark(
                field.AnchorX + (t * nearHalf), nearY,
                field.AnchorX + (t * farHalf * reach), nearY + ((farY - nearY) * reach),
                SurfaceScenery.Kind.Rille));
        }
    }

    // The ship carries one amber shuttle-airlock door across the (bottom) hatch; drop it so the tube's
    // own doors take over the threshold.
    private static bool IsHatchDoor(DeckPlan.Door d) =>
        Math.Abs(d.Y1 - DeckPlan.ShuttleHatchY) < 0.01f && Math.Abs(d.Y2 - DeckPlan.ShuttleHatchY) < 0.01f;
}
