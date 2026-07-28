using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #488 · THE WRECK YOU WALK THROUGH. A derelict is neither a world nor a berth, so it gets neither the
/// regolith field (<see cref="MoonSurface"/>) nor a station concourse (<see cref="HavenInterior"/>) — it
/// gets a dead ship: a spine corridor, compartments off it, and the airlock you came in through.
///
/// <para><b>The geometry itself lives in Core</b> (<see cref="WreckLayout"/>), the same split
/// <see cref="SurfaceLayout"/> already uses. That is not tidiness — it is the fix for a real bug. The
/// owner boarded the LONG SHRIFT and found her sealed in half by her own mutiny barricades, with every
/// build green, because the layout lived here where no test could walk it. Now
/// <c>WreckLayoutTests</c> walks every cause with A* on every CI run, and this file only dresses that
/// geometry into a DeckPlan.</para>
///
/// <para>What CHANGES with the cause is where the damage is and what the evidence consoles say —
/// <see cref="Derelict.Evidence"/> is a sentence, and this is where that sentence becomes somewhere you
/// stand. Deterministic: the same wreck always builds the same hull.</para>
/// </summary>
public static class WreckInterior
{
    /// <summary>Where the shuttle puts the away team down — just inside the wreck's airlock, on the spine
    /// and standing in a doorway, so the first compartment is one step away.</summary>
    public const double SpawnX = WreckLayout.SpawnX;

    /// <summary>Spawn Y — on the spine.</summary>
    public const double SpawnY = WreckLayout.SpawnY;

    /// <summary>Build the derelict's walkable interior for a given wreck.</summary>
    public static DeckPlan WreckDeck(
        in Derelict.Wreck wreck,
        System.Collections.Generic.IReadOnlySet<string> examined,
        bool salvaged,
        int droidCount,
        System.Action<double, DeckPlan.Droid[]> fillDroids)
    {
        System.ArgumentNullException.ThrowIfNull(fillDroids);
        examined ??= new System.Collections.Generic.HashSet<string>();

        var walls = new System.Collections.Generic.List<DeckPlan.Wall>();
        var consoles = new System.Collections.Generic.List<DeckPlan.ConsoleSpot>();
        var labels = new System.Collections.Generic.List<(float X, float Y, string Text)>();

        // ── The hull, the spine and the bulkheads — straight off Core's geometry, so what CI walks is
        //    exactly what the captain walks. IsWindow/IsHull are dressing the audit does not care about,
        //    so they are applied here by position rather than carried through Core.
        foreach (SurfaceCollision.Segment s in WreckLayout.Walls(wreck.Cause))
        {
            bool hull = IsHullEdge(s);
            bool window = IsBridgeWindow(s) || IsBreach(s, wreck.Cause);
            walls.Add(new DeckPlan.Wall((float)s.X1, (float)s.Y1, (float)s.X2, (float)s.Y2, window, hull));
        }

        // ── Compartment names. "You are in a room" is most of what makes a wreck a place, not a map.
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            labels.Add(((x0 + x1) / 2f, top ? WreckLayout.TopY + 2f : WreckLayout.BottomY - 1.5f, name));
        }

        // ── The doorways, DRAWN. A hole in a wall is not an affordance; a door is. Same auto-doors the
        //    ship's own tube uses, off the same centre list the walls were cut from, so a doorway can never
        //    be opened somewhere the player is not shown.
        var doors = new System.Collections.Generic.List<DeckPlan.Door>();
        foreach (float d in WreckLayout.DoorCentres())
        {
            doors.Add(new DeckPlan.Door(
                d - WreckLayout.DoorHalfWidth, -WreckLayout.SpineHalfHeight,
                d + WreckLayout.DoorHalfWidth, -WreckLayout.SpineHalfHeight));
            doors.Add(new DeckPlan.Door(
                d - WreckLayout.DoorHalfWidth, WreckLayout.SpineHalfHeight,
                d + WreckLayout.DoorHalfWidth, WreckLayout.SpineHalfHeight));
        }

        // ── The way home ──────────────────────────────────────────────────────────────────────────────
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ShuttleAirlock, (float)SpawnX + 4f, 0f, "🛸 BACK TO THE SHUTTLE"));
        labels.Add(((float)SpawnX + 4f, 5.5f, "— " + wreck.ShipName.ToUpperInvariant() + " —"));

        // ── The evidence ──────────────────────────────────────────────────────────────────────────────
        // Three stations to read her by: the cause's own damage, and the ship's own record — the log and
        // the manifest — which is what lets a careful captain catch a wreck that lies.
        foreach ((string id, float x, float y, string label) in EvidenceSpots(wreck.Cause))
        {
            bool done = examined.Contains(id);
            consoles.Add(new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.WreckEvidence, x, y, done ? "✔ " + label : label));
        }

        // ── The decision ──────────────────────────────────────────────────────────────────────────────
        // Amidships in the near hold, where the cargo actually is. You cannot decide what to do with her
        // from the bridge — you have to go and look at what she was carrying.
        if (!salvaged)
        {
            consoles.Add(new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.WreckSalvage, -7f, 6f, "📋 THE CARGO — AND WHAT TO DO ABOUT HER"));
        }

        return new DeckPlan(
            [.. walls], [.. consoles], [.. labels], [],
            spawnX: SpawnX, spawnY: SpawnY,
            droidCount: droidCount, fillDroids: fillDroids,
            location: LocationName,
            doors: [.. doors], shipFixtures: false, followCam: true, tables: []);
    }

    // The outer shell reads as hull; everything inside is interior partition.
    private static bool IsHullEdge(SurfaceCollision.Segment s) =>
        s.Y1 == WreckLayout.TopY && s.Y2 == WreckLayout.TopY
        || (s.Y1 == WreckLayout.BottomY && s.Y2 == WreckLayout.BottomY)
        || (s.X1 == WreckLayout.AftX && s.X2 == WreckLayout.AftX)
        || s.X1 >= WreckLayout.BowX - 6;

    private static bool IsBridgeWindow(SurfaceCollision.Segment s) =>
        s.X1 == WreckLayout.BowX && s.X2 == WreckLayout.BowX;

    // The breach's two holes are drawn as windows — you can see the stars through her.
    private static bool IsBreach(SurfaceCollision.Segment s, Derelict.WreckCause cause) =>
        cause == Derelict.WreckCause.HullBreach && s.X1 == -2f && s.X2 == 2f;

    /// <summary>The three places you read her by. The cause's station comes from Core so the audit and the
    /// console agree on where it stands — they were separate literals once, and the audit promptly found a
    /// station placed on top of a bulkhead.</summary>
    private static (string Id, float X, float Y, string Label)[] EvidenceSpots(Derelict.WreckCause cause)
    {
        DeckReachability.Point at = WreckLayout.CauseStation(cause);
        return
        [
            ("cause", (float)at.X, (float)at.Y, CauseLabel(cause)),
            ("log", 20f, -6f, "🖥 THE BRIDGE LOG"),
            ("manifest", -7f, -6f, "📦 THE CARGO MANIFEST"),
        ];
    }

    private static string CauseLabel(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.ReactorCascade => "☢ THE REACTOR SPACES",
        Derelict.WreckCause.DriveFailure => "🔧 THE DRIVE BELLS",
        Derelict.WreckCause.HullBreach => "🕳 THE HOLE THROUGH HER",
        Derelict.WreckCause.LifeSupportFailure => "🌬 THE SCRUBBER STACKS",
        Derelict.WreckCause.NavigationalError => "🧭 THE NAV POST",
        Derelict.WreckCause.Mutiny => "🔒 THE ARMS LOCKER",
        Derelict.WreckCause.Piracy => "📦 THE STRIPPED HOLD",
        Derelict.WreckCause.InsuranceJob => "🚀 THE LIFEBOAT CRADLES",
        _ => "THE WRECK",
    };

    /// <summary>Which compartment a point stands in — the header line the HUD reads.</summary>
    private static string LocationName(double x, double y)
    {
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            bool inX = x >= x0 && x <= x1;
            bool inY = top ? y < -WreckLayout.SpineHalfHeight : y > WreckLayout.SpineHalfHeight;
            if (inX && inY)
            {
                return name;
            }
        }
        return System.Math.Abs(y) <= WreckLayout.SpineHalfHeight ? "THE SPINE" : "THE HULL";
    }
}
