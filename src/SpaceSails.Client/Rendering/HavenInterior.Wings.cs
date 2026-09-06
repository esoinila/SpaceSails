using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>Part of <see cref="HavenInterior"/> (the header note lives in HavenInterior.cs) — DOORS THAT
/// GROW THE WORLD. A hatch that has been cracked open is not decoration: its hall edge is carved into a
/// walkable doorway and a real back room is welded on at runtime, as geometry-as-data (a Core
/// <see cref="DeckWing"/>). The catalogue is authored per station against the hall geometry and ships one
/// — Cinder Roost's Bonded Stores (V-06), behind which is the fence's back room — plus the hall-vertex
/// maths that room is written against and the console-kind map that hands Core's kinds to the plan.
///
/// <para>Safe to live in its own file for the reason stated in HavenInterior.cs: <c>WingCatalogs</c> is
/// the one static field here and every coordinate it reaches is a <c>const</c>, so no initializer of it
/// depends on one that is somewhere else.</para></summary>
public static partial class HavenInterior
{
    // --- Runtime wings (Core DeckWing catalog) ------------------------------------------------------
    // Authored per station against the hall geometry. v1 ships one: Cinder Roost's Bonded Stores back
    // room (V-06). Rooms gate on quests (you must crack the hatch) and quests gate on rooms (the
    // fence's package can only be lifted once the room exists) — see Map.razor.
    private static readonly Dictionary<string, DeckWing[]> WingCatalogs = new()
    {
        ["cinder-roost"] = [DeckExpansions.Validate(BondedBackRoom("cinder-roost", "V-06"))],
    };

    /// <summary>The wings authored for a station (possibly none).</summary>
    public static IReadOnlyList<DeckWing> WingCatalog(string bodyId) =>
        WingCatalogs.TryGetValue(bodyId, out DeckWing[]? w) ? w : [];

    /// <summary>Does cracking this hatch open a real room (rather than just blinking a lock green)?</summary>
    public static bool HatchGrowsWing(string bodyId, string hatchId) =>
        DeckExpansions.GrowsBehind(WingCatalog(bodyId), bodyId, hatchId);

    private static (float X, float Y) HallVertex(int k)
    {
        double a = (15 + 30 * k) * System.Math.PI / 180.0;
        return (HallCenterX + HallR * (float)System.Math.Cos(a), HallCenterY + HallR * (float)System.Math.Sin(a));
    }

    // The fence's back room behind a station's BONDED STORES hatch (edge 6 of the ring). The room is a
    // funnel off the doorway (the doorway itself is carved by BuildComplex, so the wing carries only
    // the walls beyond it), with the fence's stash on the back shelf and the Magpie's back-room booth.
    private static DeckWing BondedBackRoom(string bodyId, string hatchId)
    {
        (float ax, float ay) = HallVertex(6);
        (float bx, float by) = HallVertex(7);
        (WingWall stubA, WingWall stubB, _) = DeckExpansions.CarveDoorway(ax, ay, bx, by, 0.30f, 0.70f);
        double p30x = stubA.X2, p30y = stubA.Y2;  // doorway mouth, 30% along the edge
        double p70x = stubB.X1, p70y = stubB.Y1;  // doorway mouth, 70% along the edge

        // Outward-normal / edge-tangent frame, so the room sits squarely outside the hall.
        double mx = (ax + bx) / 2, my = (ay + by) / 2;
        double nx = mx - HallCenterX, ny = my - HallCenterY;
        double nl = System.Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;
        double tx = bx - ax, ty = by - ay;
        double tl = System.Math.Sqrt(tx * tx + ty * ty); tx /= tl; ty /= tl;
        const double d1 = 5, widen = 4, d2 = 12;
        double s30x = p30x + nx * d1 - tx * widen, s30y = p30y + ny * d1 - ty * widen;   // left shoulder
        double s70x = p70x + nx * d1 + tx * widen, s70y = p70y + ny * d1 + ty * widen;   // right shoulder
        double bk30x = s30x + nx * d2, bk30y = s30y + ny * d2;                            // back-left corner
        double bk70x = s70x + nx * d2, bk70y = s70y + ny * d2;                            // back-right corner
        double rcx = (s30x + s70x + bk30x + bk70x) / 4, rcy = (s30y + s70y + bk30y + bk70y) / 4;
        double stashx = (bk30x + bk70x) / 2 - nx * 2.5, stashy = (bk30y + bk70y) / 2 - ny * 2.5;

        var walls = new List<WingWall>
        {
            new((float)p30x, (float)p30y, (float)s30x, (float)s30y),   // left flare
            new((float)s30x, (float)s30y, (float)bk30x, (float)bk30y), // left side
            new((float)bk30x, (float)bk30y, (float)bk70x, (float)bk70y), // back wall
            new((float)bk70x, (float)bk70y, (float)s70x, (float)s70y), // right side
            new((float)s70x, (float)s70y, (float)p70x, (float)p70y),   // right flare
        };
        var consoles = new List<WingConsole>
        {
            new(WingConsoleKind.Stash, (float)stashx, (float)stashy, "📦 FENCE'S STASH"),
            new(WingConsoleKind.Patron, (float)MagpieBackPost.X, (float)MagpieBackPost.Y, "◈ THE MAGPIE"),
        };
        var labels = new List<WingLabel>
        {
            new((float)rcx, (float)rcy, "BONDED STORES · BACK ROOM"),
        };
        // No wing-owned doors: the doorway (an unlocked auto-door) is carved by BuildComplex.
        return new DeckWing($"{bodyId}-bonded-backroom", bodyId, hatchId, "BONDED STORES BACK ROOM",
            walls, [], consoles, labels);
    }

    private static DeckPlan.ConsoleKind MapConsoleKind(WingConsoleKind kind) => kind switch
    {
        WingConsoleKind.Hatch => DeckPlan.ConsoleKind.Hatch,
        WingConsoleKind.Stash => DeckPlan.ConsoleKind.Stash,
        WingConsoleKind.Patron => DeckPlan.ConsoleKind.BarPatron,
        WingConsoleKind.ViewObject => DeckPlan.ConsoleKind.ViewObject,
        _ => DeckPlan.ConsoleKind.None,
    };

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
