using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #563 · TERRAIN — the marks on a moon that are scenery rather than obstacles.
///
/// <para>Owner: <i>"put something more interesting in the landscape."</i> A landing site is a 78 × 64 field
/// holding between twelve and twenty-five wall segments; the rest is bare. Lab 47 measured Miranda's canon
/// ground — the monolith maze, the one with the story on it — at <b>12% of the field</b>. Everything else is
/// floor.</para>
///
/// <para><b>The fix is texture, not more walls.</b> The obvious move is to generate more geometry, and it is
/// the wrong one: every collidable thing added to a field is another chance to bottle the captain, another
/// A* audit to satisfy, and another step toward a labyrinth. What is missing is not obstruction, it is the
/// sense of standing somewhere. So this emits marks that are <b>drawn and never collide</b> — crater rims,
/// scree fans, ridge lines, rille channels, drift streaks. They cannot seal anything, cannot crowd a
/// console, and cannot fail the reachability flood, because the movement code never sees them.</para>
///
/// <para>That is the exact inverse of the field's own bound (<see cref="SurfaceEdge"/>), which collides and
/// is never drawn. Between the two, "what stops you" and "what you can see" have been fully separated —
/// which is only safe because each of them is now stated in one place.</para>
///
/// <para>Deliberately placed across the WHOLE field, flanks included. The edge lanes are kept clear of
/// generated features so a walk-around always exists, and the result was that the most walkable third of
/// every site was also the emptiest. Scenery does not obstruct, so it is free to go exactly there.</para>
/// </summary>
public static class SurfaceScenery
{
    /// <summary>A drawn-only mark: a line on the ground with no substance.</summary>
    public readonly record struct Mark(double X1, double Y1, double X2, double Y2, Kind Of);

    /// <summary>What a mark is, which is only ever a hint to the renderer about weight and tint. Nothing
    /// here has any effect on movement, sight, or anything a test of the ground would ask about.</summary>
    public enum Kind
    {
        /// <summary>A crater's rim — a broken ring, the commonest thing on any airless surface.</summary>
        CraterRim,

        /// <summary>A scree fan or debris apron — short strokes spilling downslope from something.</summary>
        Scree,

        /// <summary>A ridge or scarp line — a long low break in the ground.</summary>
        Ridge,

        /// <summary>A rille: a channel, drawn as two near-parallel banks.</summary>
        Rille,
    }

    /// <summary>How many terrain features a site carries. Generous — this is the layer that makes the
    /// ground stop looking like graph paper, and it costs nothing but strokes.</summary>
    public const int MinFeatures = 9;
    public const int MaxFeatures = 16;

    /// <summary>The whole site's terrain, seeded per (body, site salt). Pure and deterministic: a captain
    /// who lands twice on The Ridge Camp sees the same craters in the same places, which is what makes the
    /// ground a PLACE rather than a wallpaper that reshuffles.</summary>
    public static IReadOnlyList<Mark> For(string bodyId, string siteSalt, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        double leftX = field.LeftX, rightX = field.RightX;
        double bottomY = field.BottomY, bandY = field.LandingBandY;

        var marks = new List<Mark>();

        // #573 · Terrain scales with the field for the same reason the ruins do — a fixed dozen craters in a
        // field sixteen times the size is not weather, it is decoration nobody will walk past.
        double area = (rightX - leftX) * (bandY - bottomY);
        int scaled = System.Math.Clamp((int)(area / 260.0), MinFeatures, 260);
        int count = scaled + Face(bodyId, siteSalt, "count", MaxFeatures - MinFeatures + 1);

        for (int i = 0; i < count; i++)
        {
            // Across the WHOLE field — the flanks are kept clear of walls, not of weather.
            double cx = Lerp(leftX + 2, rightX - 2, Frac(bodyId, siteSalt, $"x:{i}"));
            double cy = Lerp(bottomY + 2, bandY - 2, Frac(bodyId, siteSalt, $"y:{i}"));
            double size = 3.0 + (9.0 * Frac(bodyId, siteSalt, $"size:{i}"));
            double turn = Frac(bodyId, siteSalt, $"turn:{i}") * Math.Tau;

            switch (Face(bodyId, siteSalt, $"kind:{i}", 4))
            {
                case 0: AddCrater(marks, cx, cy, size, turn, bodyId, siteSalt, i); break;
                case 1: AddScree(marks, cx, cy, size, turn, bodyId, siteSalt, i); break;
                case 2: AddRidge(marks, cx, cy, size, turn); break;
                default: AddRille(marks, cx, cy, size, turn); break;
            }
        }

        return marks;
    }

    /// <summary>A crater as a BROKEN ring — eight arcs with one or two missing. A closed circle reads as a
    /// drawn symbol; a gapped one reads as ground.</summary>
    private static void AddCrater(
        List<Mark> marks, double cx, double cy, double r, double turn, string body, string salt, int i)
    {
        const int arcs = 8;
        int missing = 1 + Face(body, salt, $"gap:{i}", 2);
        int firstGap = Face(body, salt, $"gapat:{i}", arcs);

        for (int a = 0; a < arcs; a++)
        {
            bool gapped = false;
            for (int g = 0; g < missing; g++)
            {
                gapped |= a == (firstGap + g) % arcs;
            }
            if (gapped)
            {
                continue;
            }

            double t0 = turn + (a * Math.Tau / arcs);
            double t1 = turn + ((a + 1) * Math.Tau / arcs);
            // Craters are wider than they are deep on a top-down plan of a rolling surface.
            marks.Add(new(
                cx + (r * Math.Cos(t0)), cy + (r * 0.62 * Math.Sin(t0)),
                cx + (r * Math.Cos(t1)), cy + (r * 0.62 * Math.Sin(t1)),
                Kind.CraterRim));
        }
    }

    /// <summary>A debris apron: short strokes fanning downslope, all roughly parallel.</summary>
    private static void AddScree(
        List<Mark> marks, double cx, double cy, double size, double turn, string body, string salt, int i)
    {
        int strokes = 4 + Face(body, salt, $"scree:{i}", 4);
        for (int k = 0; k < strokes; k++)
        {
            double spread = ((k / (double)(strokes - 1)) - 0.5) * size;
            double len = size * (0.25 + (0.5 * Frac(body, salt, $"screelen:{i}:{k}")));
            double ox = cx + (spread * Math.Cos(turn + (Math.PI / 2)));
            double oy = cy + (spread * Math.Sin(turn + (Math.PI / 2)));
            marks.Add(new(ox, oy, ox + (len * Math.Cos(turn)), oy + (len * Math.Sin(turn)), Kind.Scree));
        }
    }

    /// <summary>A scarp: one long low break, drawn as three slightly kinked runs so it is not a ruled line.</summary>
    private static void AddRidge(List<Mark> marks, double cx, double cy, double size, double turn)
    {
        double len = size * 2.2;
        double px = cx - (len / 2 * Math.Cos(turn)), py = cy - (len / 2 * Math.Sin(turn));
        for (int k = 0; k < 3; k++)
        {
            double kink = ((k % 2 == 0) ? 0.14 : -0.14);
            double a = turn + kink;
            double nx = px + (len / 3 * Math.Cos(a)), ny = py + (len / 3 * Math.Sin(a));
            marks.Add(new(px, py, nx, ny, Kind.Ridge));
            (px, py) = (nx, ny);
        }
    }

    /// <summary>A rille: two banks running near-parallel, converging slightly, the way a collapsed lava
    /// channel does. Miranda's own site 1 is called The Shadowed Rille and had no rille in it.</summary>
    private static void AddRille(List<Mark> marks, double cx, double cy, double size, double turn)
    {
        double len = size * 2.6;
        double half = 0.9 + (size * 0.1);
        for (int side = -1; side <= 1; side += 2)
        {
            double ox = cx + (side * half * Math.Cos(turn + (Math.PI / 2)));
            double oy = cy + (side * half * Math.Sin(turn + (Math.PI / 2)));
            double taper = side < 0 ? 0.82 : 1.0;   // the banks are not parallel
            marks.Add(new(
                ox - (len / 2 * Math.Cos(turn)), oy - (len / 2 * Math.Sin(turn)),
                ox + (len / 2 * taper * Math.Cos(turn)), oy + (len / 2 * taper * Math.Sin(turn)),
                Kind.Rille));
        }
    }

    private const int Resolution = 4096;

    private static double Frac(string bodyId, string siteSalt, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"scenery:{bodyId}:{siteSalt}:{tag}"), Resolution).Face;
        return (face - 1) / (double)Resolution;
    }

    private static int Face(string bodyId, string siteSalt, string tag, int sides) =>
        DiceRule.Roll(DiceRule.Seed($"scenery:{bodyId}:{siteSalt}:{tag}"), sides).Face - 1;

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}
