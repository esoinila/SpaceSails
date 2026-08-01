using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #563 · A BUILDING — the parameterised structure generator, and the end of the one-pixel U-shape.
///
/// <para>Owner, 2026-08-01, across several messages: <i>"the old buildings on surface should have real
/// thickness to their walls not just 1 pixel line… 1 pixel wide variously rotated letter U-shapes are just
/// not buildings enough"</i>; <i>"I want rooms with doors we can hide behind while we reload our guns safe
/// from reevers"</i>; <i>"I suppose we need a function that takes parameters and generates those spaces in
/// various forms as the arguments it gets dictate. Some with round exterior walls, some with various angular
/// shapes"</i>; and the model to copy — <i>"Our ship cabin rooms are a good example… They are usually boxes,
/// so height, width, angle, num-doors and that's it."</i></para>
///
/// <para><b>Why the walls are thick, which is the good part.</b> <i>"On a cold world with in-situ building
/// resources it makes sense to build thick walls, especially if the structure needs to be a pressure vessel
/// also… Viking animal shelters on Greenland had several metres thick walls to save on heating during
/// winter."</i> That is a real justification, not a rendering flourish: if your material is the regolith
/// under your boots and your problems are heat loss and holding an atmosphere against vacuum, you pile it
/// up. Anything anybody actually lived in out here should look built, not drawn.</para>
///
/// <para><b>Thickness is SOLID, not a cavity.</b> A wall drawn as two parallel lines leaves a gap between
/// them, and a gap wider than the captain is somewhere they can stand — an unreachable slot inside a wall,
/// which is exactly the sealed-pocket failure the reachability flood exists to catch. So the space between
/// the faces is hatched at <see cref="HatchSpacing"/>, closer together than the captain is wide, and the
/// wall is genuinely impassable mass. It also happens to draw the way a plan drawing renders masonry.</para>
///
/// <para><b>Rotation is the cheapest thing here and the most valuable.</b> Every wall in a landing site has
/// been axis-aligned since the game began, which is most of why the ground reads as graph paper. An angle
/// costs one rotation per point.</para>
/// </summary>
public static class SurfaceStructure
{
    /// <summary>The shape of the outer wall.</summary>
    public enum Footprint
    {
        /// <summary>A box. The commonest thing anybody builds, and the ship's own cabins.</summary>
        Rectangular,

        /// <summary>A cut-cornered box — an octagon squashed to the footprint. Reads as engineered.</summary>
        Angular,

        /// <summary>A drum. What you build when the wall is also a pressure vessel and you would rather
        /// not have corners to worry about.</summary>
        Rounded,
    }

    /// <summary>Everything the generator needs. Deliberately the owner's own list — width, height, angle,
    /// doors — plus the wall thickness and the footprint style.</summary>
    public readonly record struct Spec(
        double CentreX, double CentreY,
        double Width, double Height,
        double AngleRad,
        int Doors,
        double WallThickness,
        Footprint Shape);

    /// <summary>An opening in the wall, given as the SEGMENT across it rather than a midpoint — because a
    /// caller that wants to hang a real door needs to know which way the passage runs, and a point cannot
    /// say. Taken at mid-thickness, so a door drawn on it sits inside the mass rather than on its face.</summary>
    public readonly record struct Doorway(double X1, double Y1, double X2, double Y2)
    {
        public double CentreX => (X1 + X2) / 2;
        public double CentreY => (Y1 + Y2) / 2;
    }

    /// <summary>What came out: the collidable walls, and every opening through them.</summary>
    public readonly record struct Built(
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        IReadOnlyList<Doorway> Doorways);

    /// <summary>Half a doorway. The captain is 1.4 du across; this leaves room to walk it badly.</summary>
    public const double DoorwayHalf = 1.6;

    /// <summary>How close the hatching across a wall's thickness must be. Under the captain's 1.4 du
    /// diameter, so no point inside the wall is ever clear enough to stand in.</summary>
    public const double HatchSpacing = 1.1;

    /// <summary>The thinnest a wall may be and still read as built rather than drawn.
    ///
    /// <para>Raised to 1.5 because of the hatching, and the reason is a good one. A hatch segment spans the
    /// wall's thickness, so a 1.2 du wall emits 1.2 du segments — and DegenerateWallScan rightly refuses any
    /// wall shorter than the captain's 1.4 du body, on the grounds that it reads as an invisible wall. The
    /// fix is not to exempt the hatching; it is that 1.2 du was never a thick wall in the first place. The
    /// owner asked for Greenland longhouse walls, several metres of piled regolith, and the guard was
    /// telling us we had not built them.</para></summary>
    public const double MinThickness = 1.5;

    /// <summary>The shortest outer face that can carry a doorway and still leave a jamb either side — any
    /// less and the "jamb" is a stub shorter than the body it blocks, which reads as an invisible wall.</summary>
    public const double MinDooredFace = (DoorwayHalf + 1.6) * 2;

    /// <summary>Build one structure. Pure: the same spec always yields the same walls.</summary>
    public static Built Build(in Spec spec)
    {
        double thickness = Math.Max(MinThickness, spec.WallThickness);
        // The floor is generous on purpose: a ring's faces are much shorter than its width, so a footprint
        // merely wide enough for a doorway can still produce faces that are not. Sized so even the smallest
        // ring this can emit has faces that clear MinDooredFace.
        double halfW = Math.Max(spec.Width, (MinDooredFace * 2.2) + (thickness * 2)) / 2;
        double halfH = Math.Max(spec.Height, (MinDooredFace * 1.9) + (thickness * 2)) / 2;

        var walls = new List<SurfaceLayout.Wall>();
        var doorways = new List<Doorway>();

        // The outer face as a closed loop of points, in local (unrotated) space around the origin.
        // SIDE COUNT IS DERIVED, NOT PICKED. A fixed twelve-sided drum on a 14 du footprint gives faces
        // about 3.7 du long — shorter than a doorway needs — so EVERY face was skipped and the building
        // came out with no way in at all. The count is whatever leaves faces long enough to hold a door.
        IReadOnlyList<(double X, double Y)> outer = spec.Shape switch
        {
            Footprint.Rounded => Ring(halfW, halfH, SidesThatCanHoldADoor(halfW, halfH, 12)),
            Footprint.Angular => Ring(halfW, halfH, SidesThatCanHoldADoor(halfW, halfH, 8)),
            _ => [(-halfW, -halfH), (halfW, -halfH), (halfW, halfH), (-halfW, halfH)],
        };

        // Face lengths first, because doors must be chosen from what can actually HOLD one.
        var lengths = new double[outer.Count];
        for (int i = 0; i < outer.Count; i++)
        {
            (double ax, double ay) = outer[i];
            (double bx, double by) = outer[(i + 1) % outer.Count];
            lengths[i] = Math.Sqrt(((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay)));
        }

        // #573 · DOORS GO ON THE LONGEST FACES, and a structure ALWAYS gets at least one.
        //
        // This used to pick faces by INDEX, spread evenly round the loop, and then drop any that turned out
        // too short to hold a doorway — with nothing taking over. On a drum whose faces sit just under the
        // minimum (a five-sided ring on a 12 x 10 footprint is within a fraction of it), the chosen face was
        // rejected and no other compensated, so the building came out SEALED: an O-shape with consoles
        // inside and no way in. The owner found one. The reachability flood should have caught it and did
        // not, because it was auditing a stale copy of the field envelope.
        //
        // Ranking by length makes "too short" impossible to hit while a longer face exists, and the floor of
        // one door means a structure can never be a sealed ring.
        var byLength = new int[outer.Count];
        for (int i = 0; i < byLength.Length; i++) { byLength[i] = i; }
        Array.Sort(byLength, (a, b) => lengths[b].CompareTo(lengths[a]));

        int doors = Math.Clamp(spec.Doors, 1, outer.Count);
        var doorFaces = new HashSet<int>();
        foreach (int face in byLength)
        {
            if (doorFaces.Count >= doors)
            {
                break;
            }
            if (lengths[face] >= MinDooredFace || doorFaces.Count == 0)
            {
                doorFaces.Add(face);
            }
        }

        for (int i = 0; i < outer.Count; i++)
        {
            (double ax, double ay) = outer[i];
            (double bx, double by) = outer[(i + 1) % outer.Count];
            AddThickFace(walls, doorways, spec, thickness, ax, ay, bx, by, doorFaces.Contains(i));
        }

        return new Built(walls, doorways);
    }

    /// <summary>One wall of real depth: an outer face, an inner face parallel to it, hatching between them
    /// so the space inside the wall can never be stood in, and — when this face carries the way in — a
    /// doorway cut through BOTH faces with its own two reveals lining the passage.</summary>
    private static void AddThickFace(
        List<SurfaceLayout.Wall> walls, List<Doorway> doorways,
        in Spec spec, double thickness,
        double ax, double ay, double bx, double by, bool doored)
    {
        double dx = bx - ax, dy = by - ay;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 0.01)
        {
            return;
        }

        // Unit along the face, and the inward normal (toward the building's middle, which is the origin in
        // local space — so inward is whichever normal points back at it).
        double ux = dx / len, uy = dy / len;
        double nx = -uy, ny = ux;
        double midX = (ax + bx) / 2, midY = (ay + by) / 2;
        if ((midX * nx) + (midY * ny) > 0)
        {
            (nx, ny) = (-nx, -ny);   // flip so the normal points inboard
        }

        // Where the doorway sits along this face, as a span [g0, g1] in face-local distance.
        double g0 = 0, g1 = 0;
        if (doored)
        {
            double centre = len / 2;
            g0 = centre - DoorwayHalf;
            g1 = centre + DoorwayHalf;
            // The opening as a SPAN across the passage, at mid-thickness — what a real door is hung on.
            (double jx0, double jy0) = Local(spec, ax + (ux * g0) + (nx * thickness / 2),
                                                   ay + (uy * g0) + (ny * thickness / 2));
            (double jx1, double jy1) = Local(spec, ax + (ux * g1) + (nx * thickness / 2),
                                                   ay + (uy * g1) + (ny * thickness / 2));
            doorways.Add(new Doorway(jx0, jy0, jx1, jy1));
        }

        // The two faces, each split around the doorway if there is one.
        AddRun(walls, spec, ax, ay, ux, uy, 0, 0, len, doored, g0, g1);
        AddRun(walls, spec, ax, ay, ux, uy, nx * thickness, ny * thickness, len, doored, g0, g1);

        // The hatching that makes the thickness solid. Stepped finer than the captain is wide, so the wall
        // is mass rather than a slot. Skipped across the doorway, which is a passage on purpose.
        for (double t = 0; t <= len + 0.001; t += HatchSpacing)
        {
            if (doored && t > g0 - 0.2 && t < g1 + 0.2)
            {
                continue;
            }
            AddSeg(walls, spec,
                ax + (ux * t), ay + (uy * t),
                ax + (ux * t) + (nx * thickness), ay + (uy * t) + (ny * thickness));
        }

        // A doorway is a passage THROUGH the thickness, so it gets reveals — the two short walls lining it.
        if (doored)
        {
            AddSeg(walls, spec, ax + (ux * g0), ay + (uy * g0),
                ax + (ux * g0) + (nx * thickness), ay + (uy * g0) + (ny * thickness));
            AddSeg(walls, spec, ax + (ux * g1), ay + (uy * g1),
                ax + (ux * g1) + (nx * thickness), ay + (uy * g1) + (ny * thickness));
        }
    }

    /// <summary>One face line, split around a doorway span when there is one.</summary>
    private static void AddRun(
        List<SurfaceLayout.Wall> walls, in Spec spec,
        double ax, double ay, double ux, double uy, double offX, double offY,
        double len, bool doored, double g0, double g1)
    {
        if (!doored)
        {
            AddSeg(walls, spec, ax + offX, ay + offY, ax + (ux * len) + offX, ay + (uy * len) + offY);
            return;
        }
        AddSeg(walls, spec, ax + offX, ay + offY, ax + (ux * g0) + offX, ay + (uy * g0) + offY);
        AddSeg(walls, spec, ax + (ux * g1) + offX, ay + (uy * g1) + offY,
            ax + (ux * len) + offX, ay + (uy * len) + offY);
    }

    /// <summary>Emit a segment, rotating and translating it out of local space into the world.</summary>
    private static void AddSeg(
        List<SurfaceLayout.Wall> walls, in Spec spec, double x1, double y1, double x2, double y2)
    {
        (double wx1, double wy1) = Local(spec, x1, y1);
        (double wx2, double wy2) = Local(spec, x2, y2);
        if (Math.Abs(wx2 - wx1) < 0.01 && Math.Abs(wy2 - wy1) < 0.01)
        {
            return;   // never emit a degenerate stub (DegenerateWallScan is right to refuse them)
        }
        walls.Add(new(wx1, wy1, wx2, wy2, true));
    }

    /// <summary>Local (centred, unrotated) coordinates to world.</summary>
    private static (double X, double Y) Local(in Spec spec, double x, double y)
    {
        double c = Math.Cos(spec.AngleRad), s = Math.Sin(spec.AngleRad);
        return (spec.CentreX + (x * c) - (y * s), spec.CentreY + (x * s) + (y * c));
    }

    /// <summary>The most sides this footprint can carry while every face stays long enough for a doorway.
    /// Rounder is nicer, but a drum nobody can enter is not a building.</summary>
    private static int SidesThatCanHoldADoor(double halfW, double halfH, int most)
    {
        double perimeter = Math.Tau * ((halfW + halfH) / 2);   // good enough for a near-circle
        int fits = (int)Math.Floor(perimeter / MinDooredFace);
        return Math.Clamp(fits, 4, most);
    }

    /// <summary>An n-sided ring inscribed in the footprint — the rounded and angular exteriors.</summary>
    private static IReadOnlyList<(double X, double Y)> Ring(double halfW, double halfH, int sides)
    {
        var pts = new List<(double, double)>(sides);
        for (int i = 0; i < sides; i++)
        {
            double a = (i * Math.Tau / sides) + (Math.PI / sides);
            pts.Add((halfW * Math.Cos(a), halfH * Math.Sin(a)));
        }
        return pts;
    }
}
