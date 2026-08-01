using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #585 · ONE FLOOR OF THE HIVE, as a walkable deck.
///
/// <para>The client half of <see cref="UndergroundComplex"/>: it turns a floor's pure geometry into a
/// <see cref="DeckPlan"/> exactly the way <c>MoonSurface</c> turns a ground into one and <c>WreckInterior</c>
/// turns a dead hull into one. Nothing here decides anything — the layout, the signs, the hauls and the
/// pressure are all Core's, and this only draws them.</para>
///
/// <para>The whole architecture rests on one observation the owner made: <i>"we could go underground so that
/// we don't need to go out of the border on normal level."</i> A floor is laid inside the SURFACE'S OWN
/// envelope, so a facility the size of the entire field costs no new coordinate space. The renderer shows one
/// level at a time, which is the same deck swap the ship ↔ haven ↔ surface switch has always done.</para>
/// </summary>
public static class HiveInterior
{
    /// <summary>Where the captain stands when the lift doors open — just off the car, on the spine.</summary>
    public static (double X, double Y) SpawnOn(in SurfaceLayout.Field field)
    {
        (double x, double y) = UndergroundComplex.ShaftAt(field);
        return (x, y + 1.0);
    }

    /// <summary>Build one floor's deck.</summary>
    public static DeckPlan FloorDeck(
        string bodyId, int level, in SurfaceLayout.Field field,
        int droidCount, Action<double, DeckPlan.Droid[]> fillDroids,
        IReadOnlyCollection<int> emptiedRooms)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(bodyId, level, field);
        // #592 · The FLOOR's kind, not the site's: on the band nobody listed they differ, and the
        // title over the plan is where that lands first.
        UndergroundComplex.Kind kind = UndergroundComplex.KindOn(bodyId, level);

        var walls = new List<DeckPlan.Wall>();
        var doors = new List<DeckPlan.Door>();
        var consoles = new List<DeckPlan.ConsoleSpot>();
        var labels = new List<(float X, float Y, string Text)>();

        // The structure. Everything down here is MADE — poured, welded, bolted — so it draws in the ship's
        // own pressure-hull ink rather than in the body's stone (#589). That contrast is the point: you have
        // just left a world built out of what was under your boots and walked into something imported whole.
        foreach (SurfaceLayout.Wall w in floor.Walls)
        {
            // #585 · EVERY wall down here is hull-bright, not just the spine. Owner, standing in a corridor:
            // "this looks weird now... I am like outside the structure?" / "like in the regolith."
            //
            // He was reading the INK, and the ink was lying. Only the spine carried IsHull, so the ribs and
            // every room wall drew in the dim inner-line stroke — which is the same faint grey the surface
            // uses for rubble and scree. A captain who has just ridden a lift into a poured, powered,
            // still-lit facility was being shown regolith line-work and correctly concluded they were
            // outside.
            //
            // Nothing down here is rubble. It was cut, poured and bolted by people with a budget, so it draws
            // like the made thing it is — the brightest structure the game has shown since the ship.
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, IsHull: true));
        }

        foreach (SurfaceLayout.Doorway d in floor.Doorways)
        {
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: true));
        }

        // #585 · THE DOORS THAT NEVER OPEN. The owner asked for these by name as the illusion of scale, and
        // they are drawn Locked — cold, always shut, with a real wall behind them — so nothing about them
        // ever hints that a way through exists. [E] reads the sign; that is all it will ever do.
        foreach (UndergroundComplex.LockedDoor l in floor.Locked)
        {
            doors.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, Locked: true));
            walls.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, false, false));
            consoles.Add(new(DeckPlan.ConsoleKind.HiveSign,
                (float)((l.X1 + l.X2) / 2), (float)((l.Y1 + l.Y2) / 2), $"🔒 {l.Sign}"));
        }

        // What is in the rooms that DO open. An emptied room keeps its walls and its door — it stays a place
        // you have been, the #573 law — and simply stops offering anything.
        for (int i = 0; i < floor.RoomCentres.Count; i++)
        {
            if (emptiedRooms.Contains(RoomKey(level, i)))
            {
                continue;
            }
            (double rx, double ry) = floor.RoomCentres[i];
            consoles.Add(new(DeckPlan.ConsoleKind.HiveHaul, (float)rx, (float)ry, "🔦 SEARCH THE ROOM"));
        }

        // The lift, on every floor, in the same place.
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(field);
        consoles.Add(new(DeckPlan.ConsoleKind.HiveLift,
            (float)shaftX, (float)(shaftY + UndergroundComplex.CorridorHalf + 2.5), "🛗 LIFT"));

        foreach (SurfaceLayout.Landmark m in floor.Labels)
        {
            labels.Add(((float)m.X, (float)m.Y, m.Label));
        }
        labels.Add(((float)shaftX - 30f, (float)(shaftY + 4.5), UndergroundComplex.TitleOf(kind)));

        return new DeckPlan(
            [.. walls], [.. consoles], [.. labels], [],
            spawnX: SpawnOn(field).X, spawnY: SpawnOn(field).Y,
            droidCount: droidCount, fillDroids: fillDroids,
            location: (_, _) => floor.Name,
            doors: [.. doors], shipFixtures: false, followCam: true,
            tables: DeckPlan.Ship.Tables);
    }

    /// <summary>One key per room per floor, so a searched room on B2 is not a searched room on B3.</summary>
    public static int RoomKey(int level, int roomIndex) => (level * 1000) - roomIndex;
}
