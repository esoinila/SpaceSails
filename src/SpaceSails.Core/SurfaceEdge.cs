using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #563 · WHERE THE GROUND RUNS OUT — the field's outer bound, as a shape rather than a rectangle.
///
/// <para>Owner, on the first pass at this: <i>"Do we really need the square border around the area ... it
/// seems artificial on a Moon"</i>, then the rule — <i>"The space ships come with outside borders but the
/// landing site out-doors should not... at least not obviously so with square area"</i> — and when the fence
/// had been made invisible but not reshaped, the question that mattered: <b><i>"But the limit to movement is
/// still a box here?"</i></b></para>
///
/// <para>It was. Hiding the rectangle and darkening the approach to it changed what you SAW and nothing
/// about what stopped your boots. This is the bound itself: a wandering polyline, seeded per site, so the
/// limit to movement is not a box in the collision either.</para>
///
/// <para><b>It only ever bulges OUTWARD.</b> The nominal rectangle stays the innermost the bound can come,
/// which is what makes this safe to drop under a game that is already generating things near the edge — the
/// outpost huts (#563) are built into the far edge lane, and a boundary free to wander inward would
/// eventually eat one. Bulging outward can only ever add bare regolith, so nothing that exists can be lost
/// and the walk-around lane can only get wider.</para>
///
/// <para><b>The bulge tapers to nothing at both ends of every edge.</b> Three independent wandering edges
/// would not meet at the corners, and a gap in the bound is not a scenic detail — it is a captain walking
/// out of the world. Tapering guarantees each edge arrives exactly at the nominal corner its neighbour
/// leaves from, so the chain is closed by construction rather than by luck.</para>
///
/// <para>ONE source for the shape, deliberately. The collision walls, the renderer's darkness falloff and
/// the lab's drawing all read this — because an edge that is drawn in one place and enforced in another is
/// precisely the "the sim does one thing and the picture says another" failure this project keeps paying
/// for, and an invisible bound is the worst possible place to have it.</para>
/// </summary>
public static class SurfaceEdge
{
    /// <summary>How far out the bound may wander, in deck units. Big enough that the eye cannot recover the
    /// rectangle, small enough that the field stays a field.</summary>
    public const double MaxBulgeDu = 9.0;

    /// <summary>How long a straight run the bound is ever allowed. Every segment is at most this long, so
    /// the shape is sampled finely enough that no stretch reads as a ruled line.</summary>
    public const double SegmentDu = 4.0;

    /// <summary>Which edge of the field a point belongs to. The top is absent on purpose: that one is the
    /// ship's own underside and is a real hull, which must stay straight and stay visible.</summary>
    public enum Side
    {
        /// <summary>The port flank, running down the field's left.</summary>
        Port,

        /// <summary>The starboard flank, running down the field's right.</summary>
        Starboard,

        /// <summary>The deep rim, along the bottom — the far edge you walk toward.</summary>
        Deep,
    }

    /// <summary>How far out the bound has wandered at fraction <paramref name="t"/> (0..1) along one edge.
    ///
    /// <para>Deliberately LOW frequency: a high-frequency hash makes neighbouring samples uncorrelated and
    /// the result reads as a jagged comb, which is just as obviously generated as a straight line — only
    /// uglier. Two slow waves beat against each other so the edge undulates like a shadow line, with bays
    /// and headlands rather than teeth.</para>
    ///
    /// <para>The <c>sin(pi*t)</c> taper is the part that matters structurally: it is 0 at both ends, so
    /// every edge starts and finishes exactly on the nominal corner.</para></summary>
    public static double Bulge(string bodyId, string siteSalt, Side side, double t)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        t = Math.Clamp(t, 0.0, 1.0);
        double phase = Frac(bodyId, siteSalt, $"phase:{side}") * Math.Tau;
        double phase2 = Frac(bodyId, siteSalt, $"phase2:{side}") * Math.Tau;

        // Two slow waves, beating. Normalised to 0..1, then tapered to nothing at both ends.
        double wave = (Math.Sin((t * Math.Tau * 1.5) + phase) + Math.Sin((t * Math.Tau * 2.5) + phase2)) / 4.0;
        double taper = Math.Sin(Math.PI * t);
        return MaxBulgeDu * (0.5 + wave) * taper;
    }

    /// <summary>The field's outer bound as a closed chain of segments: port flank down, deep rim across,
    /// starboard flank up. The ship's underside closes the top and is NOT included — that is real hull, and
    /// the caller draws it as such.
    ///
    /// <para>Read by the client for collision AND for the darkness that hangs off it, so what stops you and
    /// what you can see are the same line by construction.</para></summary>
    public static IReadOnlyList<(double X1, double Y1, double X2, double Y2)> Bound(
        string bodyId, string siteSalt, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        // Copied out of the `in` parameter so the per-edge samplers below may close over them.
        double leftX = field.LeftX, rightX = field.RightX, topY = field.TopY, bottomY = field.BottomY;

        var chain = new List<(double, double, double, double)>();
        double height = topY - bottomY;
        double width = rightX - leftX;

        // Port flank: from the top-left corner down to the bottom-left. Outward is -x.
        AddRun(chain, height,
            t => (leftX - Bulge(bodyId, siteSalt, Side.Port, t), topY - (t * height)));

        // Deep rim: bottom-left to bottom-right. Outward is -y.
        AddRun(chain, width,
            t => (leftX + (t * width), bottomY - Bulge(bodyId, siteSalt, Side.Deep, t)));

        // Starboard flank: bottom-right back up to the top-right. Outward is +x.
        AddRun(chain, height,
            t => (rightX + Bulge(bodyId, siteSalt, Side.Starboard, 1.0 - t), bottomY + (t * height)));

        return chain;
    }

    /// <summary>Walk one edge in steps of at most <see cref="SegmentDu"/>, joining consecutive samples.</summary>
    private static void AddRun(
        List<(double, double, double, double)> chain, double length, Func<double, (double X, double Y)> at)
    {
        int steps = Math.Max(2, (int)Math.Ceiling(length / SegmentDu));
        (double x, double y) prev = at(0.0);
        for (int i = 1; i <= steps; i++)
        {
            (double x, double y) next = at(i / (double)steps);
            chain.Add((prev.x, prev.y, next.x, next.y));
            prev = next;
        }
    }

    private const int Resolution = 4096;

    private static double Frac(string bodyId, string siteSalt, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"surfaceedge:{bodyId}:{siteSalt}:{tag}"), Resolution).Face;
        return (face - 1) / (double)Resolution;
    }
}
