using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #602/#606 · THE LIFT HEAD'S SHED, as one object everybody reads — where the car comes up, how big
/// the hut around it is, which way its door faces, and the square the captain lands on stepping out.
///
/// <para>Owner, stepping out of the car: <i>"Oh I emerged into the wall on the surface… I cannot move
/// :-D"</i>, and then, on where he expected to be: <i>"I would expect to spawn into the elevator box
/// where we went down with."</i> These four numbers used to be <c>const</c> locals inside the wall
/// builder, so nothing outside that method could say where the shed was — and the lift's return path
/// invented its own answer and put him in a wall.</para>
///
/// <para>Split out of <c>MoonSurface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered. #1175 reported, and #1177 fixed, the mis-filed docblock that used to sit here:
/// <c>ExpeditionField</c>'s <c>&lt;summary&gt;</c> had been stranded above <c>LiftHeadBox</c> since long
/// before the cut, a hundred lines from its method, and is now filed with it in
/// <c>MoonSurface.Dig.cs</c>.</para>
/// </summary>
public static partial class MoonSurface
{
    /// <summary>#602 · THE LIFT HEAD'S SHED, as one object everybody reads.
    ///
    /// <para>Owner, stepping out of the car: <i>"Oh I emerged into the wall on the surface... I cannot move
    /// :-D"</i> — and then, on where he expected to be: <i>"I would expect to spawn into the elevator box
    /// where we went down with."</i></para>
    ///
    /// <para>These four numbers used to be <c>const</c> locals inside the wall builder, so nothing outside
    /// this method could say where the shed was or how big it is. The lift's return path therefore invented
    /// its own answer, and put the captain in a wall. A test that hard-coded 5.0 and 4.0 to check it would
    /// have been the same bug wearing a lab coat — the mirrored-constant failure this ground keeps paying
    /// for. So the shed is a value now: built from it, returned into it, and asserted against it.</para>
    ///
    /// <para>#606 · And it is a <see cref="SurfaceStructure.Spec"/> now, not a hand-typed rectangle, because
    /// the shed had to stop being the one building on the moon that was drawn in a different hand. Everything
    /// here is derived from that spec — the room's clear floor, the doorway the car opens toward, the spot
    /// the captain lands on — so the picture and the arithmetic cannot disagree about a building neither of
    /// them owns any more.</para></summary>
    public readonly record struct LiftHeadBox(
        SurfaceStructure.Spec Hut,
        double HalfW, double HalfH,
        double DoorX, double DoorY,
        double CarX, double CarY,
        double StepX, double StepY)
    {
        public double CentreX => Hut.CentreX;
        public double CentreY => Hut.CentreY;

        /// <summary>Is this point inside the hut's clear floor — the room the car opens into? Rotation-proof,
        /// because the hut is seeded an angle like every other building and an axis-aligned answer would be
        /// right on one site in a hundred.</summary>
        public bool Contains(double x, double y, double clearance = 0)
        {
            double c = Math.Cos(-Hut.AngleRad), s = Math.Sin(-Hut.AngleRad);
            double dx = x - Hut.CentreX, dy = y - Hut.CentreY;
            double lx = (dx * c) - (dy * s), ly = (dx * s) + (dy * c);
            return Math.Abs(lx) <= HalfW - clearance && Math.Abs(ly) <= HalfH - clearance;
        }

        /// <summary>Where the car sets the captain down: inside the room, a pace in from the door it came up
        /// beside, which is what riding a lift up into a shed actually looks like.</summary>
        public (double X, double Y) CarFloor => (CarX, CarY);

        /// <summary>#681 · Where a LANDING sets the captain down: a pace OUTSIDE the same door, facing it.
        ///
        /// <para>The exact mirror of <see cref="CarFloor"/>, and it exists for the exact reason that one
        /// does. <c>?secretlab=…&amp;land=1</c> computed its own answer — the head spot with 7.5 du taken off
        /// its Y — and that number was written when the head was a hand-typed 10 x 8 box whose half-height was
        /// 4. #606 made the head an ordinary hut: 14–19.6 du wide, 11–15.4 deep, walls up to 3 du of piled
        /// regolith, and a SEEDED ANGLE. Seven and a half deck units below the middle of that is not a pace
        /// outside the door; on most seeds it is the far wall, and on <c>?secretlab=deep</c> it is the wall
        /// segment the owner spent an excursion standing inside of.</para>
        ///
        /// <para>Same bug class as #602, one head further along: a caller doing its own geometry about a
        /// building it does not own. So the landing asks the shed where its doorstep is, exactly as the car
        /// asks it where its floor is, and neither of them keeps a number.</para></summary>
        public (double X, double Y) DoorStep => (StepX, StepY);
    }

    /// <summary>How far in from the doorway the car sets you down, and where the panel is. Far enough inside
    /// that the captain's own width clears the jamb; near enough that the panel answers [E] from where a
    /// person stands when they walk in — the #585 report was a button at a room's centre and a captain in the
    /// doorway being told there was nothing here.</summary>
    private const double CarStepIn = 2.4;

    /// <summary>#681 · How far OUTSIDE the door a landing sets you down. Measured from the door's own centre
    /// like <see cref="CarStepIn"/> is, so it clears the wall's outer face by this much whatever thickness the
    /// hut was seeded — which is the whole point. Deliberately the same distance in as out: the cheat's
    /// promise is "a pace outside the shed's door, facing it", and a pace is a pace either way.</summary>
    private const double DoorStepOut = CarStepIn;

    /// <summary>The hut for this body and site, already moved clear of the shelters and the outpost.</summary>
    public static LiftHeadBox LiftHead(string bodyId, string? siteSalt, in SurfaceLayout.Field field)
    {
        SurfaceStructure.Spec hut = SecretLab.HeadHut(bodyId, siteSalt, field);
        SurfaceStructure.Envelope env = SurfaceStructure.EnvelopeOf(hut);

        // The door the car opens beside is the hut's OWN first opening, taken from the builder rather than
        // guessed from a face index — the builder ranks faces by length and the ranking is its business, not
        // this function's. (#587's lesson, one floor up: a caller that re-derives a builder's choice is a
        // second source of truth wearing a coordinate.)
        SurfaceStructure.Doorway way = SurfaceStructure.Build(hut).Doorways[0];
        double dx = way.CentreX - hut.CentreX, dy = way.CentreY - hut.CentreY;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        double ux = len > 0.001 ? dx / len : 0, uy = len > 0.001 ? dy / len : -1;

        return new LiftHeadBox(
            hut, env.InnerHalfW, env.InnerHalfH,
            way.CentreX, way.CentreY,
            // Straight in from the doorway, along the line from the room's middle to it.
            way.CentreX - (ux * (CarStepIn + (env.Thickness / 2))),
            way.CentreY - (uy * (CarStepIn + (env.Thickness / 2))),
            // #681 · …and straight OUT of it, the same distance the other way, so a landing stands on the
            // doorstep rather than in whichever wall happens to be 7.5 du below the middle of the hut.
            way.CentreX + (ux * (DoorStepOut + (env.Thickness / 2))),
            way.CentreY + (uy * (DoorStepOut + (env.Thickness / 2))));
    }
}
