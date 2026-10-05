using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #653 slice 1 · THE DEAD STATION YOU WALK. <see cref="StationWreck"/> has always known where its walls,
/// modules and ways in are — it even returns <see cref="SurfaceCollision.Segment"/>s and is walked by
/// <see cref="DeckReachability"/> in its own tests. It was a builder and a route away from being a place.
/// This is the builder: the mirror of <see cref="WreckInterior.WreckDeck"/>, and like it a dressing of
/// geometry that lives in Core — what CI walks is exactly what the captain walks.
///
/// <para><b>No grand abstraction.</b> A <see cref="DeckPlan"/>, a segment list, a body-id prefix: the five
/// seams the house idiom already carries. What the station adds is the one thing a derelict does not have —
/// the boat is mated to ONE of its accesses at a time, and flying her to another is how a captain crosses a
/// severed tube. So the deck is built from <see cref="StationState"/>: where the boat is, and which faces have
/// been cut. Nothing else about the station changes between builds.</para>
/// </summary>
public static class StationInterior
{
    /// <summary>What the deck is built from. Everything else is a seeded fact of the station itself.</summary>
    /// <param name="Dock">The module whose access the boat is mated to — where the captain came in, and where
    /// the way home is.</param>
    /// <param name="CutFaces">The modules this away team has cut into (permanent for the visit): their hop
    /// stops being refused.</param>
    public readonly record struct StationState(
        StationWreck.ModuleId Dock, System.Collections.Generic.IReadOnlyCollection<StationWreck.ModuleId> CutFaces);

    /// <summary>Where the boat puts the away team down at a module's access — Core's own answer.</summary>
    public static (double X, double Y) SpawnAt(string stationId, StationWreck.ModuleId dock) =>
        StationWreck.SpawnFor(StationAboard.AccessOf(stationId, dock));

    /// <summary>Build the station's walkable interior.</summary>
    public static DeckPlan StationDeck(
        string stationId,
        in StationState state,
        int droidCount,
        System.Action<double, DeckPlan.Droid[]> fillDroids)
    {
        System.ArgumentNullException.ThrowIfNull(stationId);
        System.ArgumentNullException.ThrowIfNull(fillDroids);

        var walls = new System.Collections.Generic.List<DeckPlan.Wall>();
        var consoles = new System.Collections.Generic.List<DeckPlan.ConsoleSpot>();
        var labels = new System.Collections.Generic.List<(float X, float Y, string Text)>();
        var doors = new System.Collections.Generic.List<DeckPlan.Door>();

        // ── The shell, straight off Core, so what CI walks is what the captain walks. One hull, one ink.
        foreach (SurfaceCollision.Segment s in StationWreck.Walls(stationId))
        {
            walls.Add(new DeckPlan.Wall((float)s.X1, (float)s.Y1, (float)s.X2, (float)s.Y2, false, true));
        }

        // ── The plates. Door-plate idiom, one per module, every one enforcing rules on nobody.
        foreach (StationWreck.Module m in StationWreck.Modules)
        {
            labels.Add(((float)m.CentreX, (float)m.CentreY, StationAboard.PlateOf(m.Id)));
        }

        // ── The tubes. An intact tube's two mouths are DRAWN as doors (a hole in a wall is not an
        //    affordance); a severed one has solid faces, and is read from either end.
        foreach (StationWreck.ModuleId arm in StationWreck.Arms)
        {
            if (StationWreck.TubeIntact(stationId, arm))
            {
                AddTubeDoors(doors, arm);
                continue;
            }

            foreach ((StationWreck.ModuleId _, double x, double y) in StationAboard.TubeEnds(arm))
            {
                consoles.Add(new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.StationTube, (float)x, (float)y, "🕳 THE TUBE"));
            }
        }

        // ── The boat, and the places she can take you.
        StationWreck.Access dock = StationAboard.AccessOf(stationId, state.Dock);
        (double lx, double ly) = StationAboard.DockFixtureAt(dock, 0);
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ShuttleAirlock, (float)lx, (float)ly, "🛸 BACK TO THE SHUTTLE"));

        int slot = 1;
        foreach ((StationWreck.ModuleId target, StationHop.Quote quote)
                 in StationHop.Destinations(stationId, state.Dock, state.CutFaces))
        {
            StationWreck.Access arrival = StationAboard.AccessOf(stationId, target);
            (double hx, double hy) = StationAboard.DockFixtureAt(dock, slot++);
            consoles.Add(new DeckPlan.ConsoleSpot(DeckPlan.ConsoleKind.StationHop, (float)hx, (float)hy,
                HopLabel(StationAboard.WithTheLock(quote, arrival), arrival)));
        }

        (double sx, double sy) = SpawnAt(stationId, state.Dock);
        return new DeckPlan(
            [.. walls], [.. consoles], [.. labels], [],
            spawnX: sx, spawnY: sy,
            droidCount: droidCount, fillDroids: fillDroids,
            location: LocationName,
            doors: [.. doors], shipFixtures: false, followCam: true, tables: []);
    }

    /// <summary>What a hop console says. The destination is Core's own access name; the number is the one
    /// the clock will be charged — flight plus lock — so label, cast-off line and clock agree.</summary>
    public static string HopLabel(in StationHop.Quote quote, in StationWreck.Access arrival) =>
        quote.Refused == StationHop.Refusal.None
            ? $"🛸 → {arrival.Name} · {quote.Seconds:F0}s"
            : $"✂ {arrival.Name}";

    /// <summary>Both mouths of an intact tube, as doors: the hub's face and the arm's.</summary>
    private static void AddTubeDoors(System.Collections.Generic.List<DeckPlan.Door> doors, StationWreck.ModuleId arm)
    {
        StationWreck.Module hub = StationWreck.ModuleOf(StationWreck.ModuleId.Hub);
        StationWreck.Module m = StationWreck.ModuleOf(arm);
        float b = StationWreck.TubeHalfBore;
        switch (arm)
        {
            case StationWreck.ModuleId.Habitat:
                doors.Add(new DeckPlan.Door(-b, hub.Y1, b, hub.Y1));
                doors.Add(new DeckPlan.Door(-b, m.Y0, b, m.Y0));
                break;
            case StationWreck.ModuleId.Foundry:
                doors.Add(new DeckPlan.Door(-b, hub.Y0, b, hub.Y0));
                doors.Add(new DeckPlan.Door(-b, m.Y1, b, m.Y1));
                break;
            case StationWreck.ModuleId.Docking:
                doors.Add(new DeckPlan.Door(hub.X1, -b, hub.X1, b));
                doors.Add(new DeckPlan.Door(m.X0, -b, m.X0, b));
                break;
            default:
                doors.Add(new DeckPlan.Door(hub.X0, -b, hub.X0, b));
                doors.Add(new DeckPlan.Door(m.X1, -b, m.X1, b));
                break;
        }
    }

    /// <summary>Which part of her a point stands in — the header line the HUD reads. A module's own name from
    /// Core; the tube and the hull are what is left.</summary>
    private static string LocationName(double x, double y) =>
        StationAboard.ModuleAt(x, y)?.Name
        ?? (System.Math.Abs(x) <= StationWreck.TubeHalfBore || System.Math.Abs(y) <= StationWreck.TubeHalfBore
            ? "THE TUBE"
            : "THE HULL");
}
