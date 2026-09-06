using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: A PLAN THAT GROWS — the #371 Phase 3 append, and the one removal that answers it (part of
// DeckPlan).
//
// Everything else about a DeckPlan is settled the moment it is built. This is the part that is not: a
// forced hatch, a claimed cache, a tile welded on at a crossing, all of them arriving on a plan the
// captain is already standing on, with the existing geometry keeping its indices so nothing already on
// the ground moves under their boots.
//
// #251 · MOVED HERE BY PURE MOTION out of `DeckPlan.cs` — see the note at the head of that file for why
// it was cut and what "pure motion" is holding across the family.

public sealed partial class DeckPlan
{
    // =====================================================================================
    //  #371 Phase 3 · APPEND-ABLE REGIONS. A DeckPlan was an immutable blob (every change rebuilt the
    //  whole surface). The door-open dream needs the world to GROW without a rebuild: a forced sealed
    //  door APPENDS a region — walls, consoles, labels, an optional backdrop — onto the live plan. Only
    //  the appended walls' collision segments are computed; the existing geometry, droids and positions
    //  are untouched, and per-frame stepping/rendering simply iterate the (now larger) arrays. The ctor
    //  stays exactly as it was, so every current caller (the ship, havens, the base surface) is unchanged.
    //  Scope discipline (#371): only expedition sites use this today — the follow-up would regionise the
    //  moon/ship surfaces.
    // =====================================================================================

    /// <summary>A block of new interior to append to a live plan: walls (collision law for everyone),
    /// interactable consoles, room labels, and any backdrops. Any array may be empty.</summary>
    public readonly record struct DeckRegion(
        Wall[] Walls, ConsoleSpot[] Consoles,
        (float X, float Y, string Text)[] Labels, Backdrop[] Backdrops,
        Structure[]? Structures = null,
        // #563 · …and TERRAIN. The treadmill's tiles arrive through this door like everything else that
        // grows a live plan, and a tile is mostly weather: craters, scree, rille banks. Scenery is drawn and
        // never collides (SurfaceScenery), so appending it cannot seal anything and cannot fail an audit —
        // it is the one part of a tile that is pure picture.
        SpaceSails.Core.SurfaceScenery.Mark[]? Scenery = null,
        // #563 slice 2 · …and DOORS, last so every existing positional caller is untouched. Everything off
        // the home tile arrived through this door as bare walls, so a ruin a hundred du out had the openings
        // the generator hands back and nothing hung in them — word for word the complaint #573 was filed
        // about, reintroduced by the lattice. A door is not collision (the wall either side of the gap is),
        // so appending one can never seal anything and cannot fail an audit.
        Door[]? Doors = null);

    /// <summary>Grow this plan by one region. The walls (and ONLY the new walls) get fresh collision
    /// segments appended after the existing ones; consoles, labels and backdrops concatenate. Existing
    /// entries keep their indices — no geometry already on the ground moves. Cheap and rebuild-free: no
    /// generation re-runs, the segment array grows by exactly <c>region.Walls.Length</c>.</summary>
    public void AppendRegion(in DeckRegion region)
    {
        if (region.Walls is { Length: > 0 } newWalls)
        {
            int baseLen = Walls.Length;
            var grownWalls = new Wall[baseLen + newWalls.Length];
            Array.Copy(Walls, grownWalls, baseLen);
            Array.Copy(newWalls, 0, grownWalls, baseLen, newWalls.Length);

            var grownSegs = new SurfaceCollision.Segment[baseLen + newWalls.Length];
            Array.Copy(CollisionSegments, grownSegs, baseLen);
            for (int i = 0; i < newWalls.Length; i++)
            {
                grownSegs[baseLen + i] = new SurfaceCollision.Segment(
                    newWalls[i].X1, newWalls[i].Y1, newWalls[i].X2, newWalls[i].Y2);
            }
            Walls = grownWalls;
            CollisionSegments = grownSegs;
            CollisionField = SurfaceCollision.WallIndex.Build(grownSegs); // #448: the grid grows with them
        }

        Consoles = Concat(Consoles, region.Consoles);
        Doors = Concat(Doors, region.Doors);
        RoomLabels = Concat(RoomLabels, region.Labels);
        Backdrops = Concat(Backdrops, region.Backdrops);
        Structures = Concat(Structures, region.Structures);
        Scenery = Concat(Scenery, region.Scenery);
        AppendedRegionCount++;
    }

    private static T[] Concat<T>(T[] a, T[]? b)
    {
        if (b is not { Length: > 0 })
        {
            return a;
        }
        var grown = new T[a.Length + b.Length];
        Array.Copy(a, grown, a.Length);
        Array.Copy(b, 0, grown, a.Length, b.Length);
        return grown;
    }

    /// <summary>Drop the console nearest to (<paramref name="x"/>, <paramref name="y"/>) that matches
    /// <paramref name="kind"/> within a tight tolerance — used when a sealed door is forced (its console
    /// becomes the open doorway) or a discovery cache is claimed. Only the small consoles array is rebuilt;
    /// walls and their segments are never touched. Returns true if one was removed.</summary>
    public bool RemoveConsoleAt(double x, double y, ConsoleKind kind, double tolerance = 0.1)
    {
        int found = -1;
        double bestSq = tolerance * tolerance;
        for (int i = 0; i < Consoles.Length; i++)
        {
            if (Consoles[i].Kind != kind)
            {
                continue;
            }
            double dx = Consoles[i].X - x, dy = Consoles[i].Y - y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 <= bestSq)
            {
                bestSq = d2;
                found = i;
            }
        }
        if (found < 0)
        {
            return false;
        }
        var trimmed = new ConsoleSpot[Consoles.Length - 1];
        Array.Copy(Consoles, 0, trimmed, 0, found);
        Array.Copy(Consoles, found + 1, trimmed, found, Consoles.Length - found - 1);
        Consoles = trimmed;
        return true;
    }

}
