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

        // #600 · THE DEPTH, PAINTED BY THE LIFT. Owner, riding between floors built from the same bones:
        // "something different in every floor so we visually spot some difference" / "we can use seriously
        // large numbers there :-D" / "or depths (in meters)".
        //
        // Two lines, stencilled on the wall beside the car the way a stairwell or a car park marks a level:
        // the depth, which is a fact about where you are standing, and the department, which is what this
        // floor was for. Together they are the glance that says which floor you stepped out on — and the
        // depth is the number that makes the walk back up mean something.
        // Owner, seeing the first cut: "Let's put the elevation next to the elevator... now it is too far
        // from it." It was 30 du off to one side, which is most of a screen — a number that far from the
        // thing it describes is not signage, it is litter. It sits just above the car's own mouth now, over
        // the 🛗 LIFT plate, which is where a building paints a level: on the wall you face when the doors
        // open.
        // #605 · THE PLATE BY THE CAR — depth over department, both painted at signage size.
        //
        // Owner, twice: "Let's put the elevation next to the elevator... now it is too far from it", then
        // "the name of the floor should read next to the elevator... we have them in the buttons let's have
        // them on the level also" and "it is way too small and too far from the elevator".
        //
        // Both complaints are the same fault. The name was pinned 26 du off down the spine at caption size,
        // which is neither next to the lift nor readable at a glance — so it was information the captain had
        // to go and look for, about the one thing they most need to know without looking.
        //
        // They are one plate now, directly over the car's mouth: the depth big because it is the number that
        // decides whether you can walk back up, the department under it because that is what the floor was
        // FOR — and it is what the panel's own buttons promised on the way in.
        // Owner, on why it has to dominate: "It is the where-am-I question answer when you come with the
        // elevator so it is like the most important thing to see."
        //
        // That is the whole brief. A captain steps out of a car onto one of twenty floors cut from identical
        // bones, and the first thing they need is not a console or a corridor — it is WHICH ONE. So the plate
        // sits directly over the car's mouth, in the eye-line of somebody who has just turned around, and it
        // is the largest thing drawn on the floor.
        //
        // And it is modelled on a real reflex, which is why it belongs here rather than in the HUD — owner:
        // "sometimes people get off the elevator at wrong floor so there is this instinct to always check
        // that the floor is correct." A number on the instrument panel would answer the question; a plate on
        // the WALL is the thing you actually look at, because looking at it is what people do.
        // #612 · AND WHETHER YOU CAN BREATHE HERE. Owner, reading the plate: "it should say if the floor is
        // pressurized also" — and, of the gauge: "where here does it say if I consume tanks or have air?"
        //
        // It is the same question twice, and the plate is the right place to answer it: a captain stepping
        // out of a car needs WHERE AM I and CAN I BREATHE in one glance, and the second one decides whether
        // everything they were about to do is affordable. Three lines, one plate, and the air line carries
        // the colour so it reads before it is read.
        bool holdsAir = UndergroundComplex.HoldsPressure(level);
        double signX = shaftX;
        double signY = shaftY + UndergroundComplex.CorridorHalf;
        var bigLabels = new List<(float X, float Y, string Text, float Px, int Tone)>
        {
            ((float)signX, (float)(signY + 10.6), UndergroundComplex.DepthPaint(level), 44f, 0),
            ((float)signX, (float)(signY + 7.8), UndergroundComplex.NameOf(bodyId, level), 19f, 0),
            ((float)signX, (float)(signY + 5.4),
                holdsAir ? "PRESSURISED · TANK STOPPED" : "NO ATMOSPHERE · TANK RUNNING",
                17f, holdsAir ? 1 : 2),
        };
        labels.Add(((float)shaftX - 30f, (float)(shaftY + 4.5), UndergroundComplex.TitleOf(kind)));

        return new DeckPlan(
            [.. walls], [.. consoles], [.. labels], [],
            spawnX: SpawnOn(field).X, spawnY: SpawnOn(field).Y,
            droidCount: droidCount, fillDroids: fillDroids,
            location: (_, _) => floor.Name,
            doors: [.. doors], shipFixtures: false, followCam: true,
            tables: DeckPlan.Ship.Tables,
            bigLabels: [.. bigLabels],
            // #605 · The floor's department livery. Null on the band nobody listed, so that concrete is the
            // one place down here left bare — the absence is the tell.
            hullInk: UndergroundComplex.LiveryFor(bodyId, level));
    }

    /// <summary>One key per room per floor, so a searched room on B2 is not a searched room on B3.</summary>
    public static int RoomKey(int level, int roomIndex) => (level * 1000) - roomIndex;
}
