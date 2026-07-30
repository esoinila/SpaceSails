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
    /// <summary>Which compartment's doorway a door centre belongs to. The spine has four openings serving
    /// eight rooms — one above, one below — so this is the same lookup the valve board's mimic uses.</summary>
    private static float DoorCentreFor(float x0, float x1)
    {
        foreach (float centre in WreckLayout.DoorCentres())
        {
            if (centre > x0 && centre < x1)
            {
                return centre;
            }
        }
        return (x0 + x1) / 2f;
    }

    /// <param name="heldDoors">Compartments whose door is held by a pressure differential — vacuum on one
    /// side, air on the other. These read Δp and offer the equalisation valve.</param>
    /// <param name="blockedDoors">Compartments the captain cannot walk into: held by pressure OR dogged
    /// shut by hand. A superset of <paramref name="heldDoors"/>.</param>
    public static DeckPlan WreckDeck(
        in Derelict.Wreck wreck,
        System.Collections.Generic.IReadOnlySet<string> examined,
        bool salvaged,
        int droidCount,
        System.Action<double, DeckPlan.Droid[]> fillDroids,
        System.Collections.Generic.IReadOnlySet<string>? heldDoors = null,
        System.Collections.Generic.IReadOnlySet<string>? blockedDoors = null)
    {
        System.ArgumentNullException.ThrowIfNull(fillDroids);
        examined ??= new System.Collections.Generic.HashSet<string>();

        var walls = new System.Collections.Generic.List<DeckPlan.Wall>();
        var consoles = new System.Collections.Generic.List<DeckPlan.ConsoleSpot>();
        var labels = new System.Collections.Generic.List<(float X, float Y, string Text)>();

        // #537 · HER STRUCTURE, FILLED. Owner, reading the deck after the padding shipped: "we should cover
        // those narrow spaces … all of them … if we can see into them from the hall then they don't hide
        // anything", and then how: "some kind of fill there would make it look like the space is filled with
        // stuff." Right on both counts — a run drawn as two lines round a black gap reads as a SPACE, and a
        // hiding place drawn as a space is not one.
        var structures = new System.Collections.Generic.List<DeckPlan.Structure>();
        foreach ((float sx0, float sy0, float sx1, float sy1) in WreckLayout.StructuralFills())
        {
            structures.Add(new(sx0, sy0, sx1, sy1));
        }

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
        // A door with a pressure differential across it is NOT an affordance — it is ten tonnes of
        // atmosphere in a frame, and the captain walks into a wall. `heldDoors` names the compartments
        // whose doorway is currently loaded; everything else opens as before.
        var doors = new System.Collections.Generic.List<DeckPlan.Door>();
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            float centre = DoorCentreFor(x0, x1);
            float y = top ? -WreckLayout.SpineHalfHeight : WreckLayout.SpineHalfHeight;
            float left = centre - WreckLayout.DoorHalfWidth;
            float right = centre + WreckLayout.DoorHalfWidth;

            bool blocked = blockedDoors is not null && blockedDoors.Contains(name);
            bool loaded = heldDoors is not null && heldDoors.Contains(name);

            if (blocked)
            {
                // Shut by physics or dogged by hand — either way the gap the spine wall was cut for is
                // filled, and the captain walks into a door rather than through one.
                walls.Add(new DeckPlan.Wall(left, y, right, y, false, false));
            }
            else
            {
                doors.Add(new DeckPlan.Door(left, y, right, y));
            }

            // EVERY doorway is a control, open or not: the gauge and the equalisation valve when it is
            // loaded, and the hand-dogs either way. Sealing a room is the only counterplay to somebody
            // cracking a valve two compartments away, so it has to be operable where you are standing.
            // ON THE CORRIDOR SIDE. The captain approaches a shut door from the spine, so a control placed
            // inside the compartment would only be reachable from the room they cannot get into.
            consoles.Add(new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.WreckPressureDoor, centre, y + (top ? 1.6f : -1.6f),
                loaded ? $"⛔ {name} — Δp" : blocked ? $"🚪 {name} — sealed" : $"🚪 {name}"));
        }

        // ── The way home ──────────────────────────────────────────────────────────────────────────────
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ShuttleAirlock, (float)WreckLayout.ShuttleStation.X, (float)WreckLayout.ShuttleStation.Y, "🛸 BACK TO THE SHUTTLE"));
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

        // ── Atmosphere control ────────────────────────────────────────────────────────────────────────
        // The bridge panel is DEAD (owner, borrowing from Battlestar Galactica) and the working valves are
        // aft in ENGINEERING. That placement IS the mechanic: on an infested hull you walk toward the thing
        // to reach the tool that kills it, then back out past whatever you chose not to vent. The dead
        // panel is a signpost, not a wall — nobody should have to guess the answer is aft.
        // ── The lifeboat cradles, ON THE WALL ─────────────────────────────────────────────────────────
        // Owner, after walking into the compartment named for them and finding nothing: "we should somehow
        // see this like slots that are filled or empty … on the wall." So they are drawn where a ship keeps
        // them, and they are COUNTABLE FROM THE DOORWAY — the cheapest evidence in the game, because how
        // many are empty is a fact about the room rather than something a console tells you.
        int launched = Derelict.LifeboatsLaunched(wreck.Id, wreck.Cause, WreckLayout.CradleCount);
        int cradleIndex = 0;
        foreach ((float cx, float cy) in WreckLayout.CradleSpots())
        {
            // The empty ones are the ones that LEFT, counted from the forward end — so a half-empty row
            // reads as a run of gaps rather than a random scatter, the way a boat bay actually empties.
            bool gone = cradleIndex < launched;
            labels.Add((cx, cy, gone ? "▫" : "▮"));
            cradleIndex++;
        }
        labels.Add((
            (float)WreckLayout.CradleSpots().Average(p => p.X),
            WreckLayout.BottomY - 2.6f,
            $"LIFEBOAT CRADLES  {WreckLayout.CradleCount - launched}/{WreckLayout.CradleCount}"));

        // ── Making sure ───────────────────────────────────────────────────────────────────────────────
        // The scuttling panel is deliberately NOT the valve board. That board is damage control, a thing
        // every ship has; this is the other kind of panel — keyed, placarded, chained, built for one job.
        // It sits with the reactor, so standing at it means standing next to the thing you are overloading.
        // Position comes from Core so the audit walks it and the separation test can see it. Placed by eye
        // here, it landed on top of the infested hull's nest station and the captain got the nest instead.
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.WreckScuttle,
            (float)WreckLayout.ScuttleStation.X, (float)WreckLayout.ScuttleStation.Y,
            "☢ SCUTTLING PANEL"));

        // EVERY SHIP HAS THESE, WHATEVER KILLED HER — see WreckLayout.StandardFittings for the law and the
        // reason. Owner: "even without reevers we should have those tech we used here available, to not give
        // a clue that they might not be needed."
        //
        // These were gated on Infested (and briefly on the vented hull too), which meant the CONSOLE WAS
        // THE SPOILER: a captain who found a valve board knew what had happened aboard before reading a
        // single line of evidence, and every misread the wreck was carefully built to allow was undone by
        // a fitting being present. A hull with nothing living in her still has valves; they just do not
        // help. That is what a red herring is supposed to feel like.
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.WreckBridgePanel,
            (float)WreckLayout.BridgePanelStation.X, (float)WreckLayout.BridgePanelStation.Y,
            "⚙ VENT PANEL (bridge)"));
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.WreckValves,
            (float)WreckLayout.ValveStation.X, (float)WreckLayout.ValveStation.Y,
            "⚙ ATMOSPHERE VALVES"));

        // And the sign that sends them there, at the lock every boarding walks through. A first-timer should
        // be able to go straight to the right compartment; the dead bridge panel only helps a captain who
        // happens to turn forward first.
        consoles.Add(new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.WreckPlacard,
            (float)WreckLayout.PlacardStation.X, (float)WreckLayout.PlacardStation.Y,
            $"🪧 ATMOSPHERE CONTROL → {HullVenting.ValveCompartment}"));

        // ── The decision ──────────────────────────────────────────────────────────────────────────────
        // Amidships in the near hold, where the cargo actually is. You cannot decide what to do with her
        // from the bridge — you have to go and look at what she was carrying.
        if (!salvaged)
        {
            consoles.Add(new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.WreckSalvage, (float)WreckLayout.CargoStation.X, (float)WreckLayout.CargoStation.Y, "📋 THE CARGO — AND WHAT TO DO ABOUT HER"));
        }

        return new DeckPlan(
            [.. walls], [.. consoles], [.. labels], [],
            spawnX: SpawnX, spawnY: SpawnY,
            droidCount: droidCount, fillDroids: fillDroids,
            location: LocationName,
            doors: [.. doors], shipFixtures: false, followCam: true, tables: [],
            structures: [.. structures]);
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
            ("log", (float)WreckLayout.LogStation.X, (float)WreckLayout.LogStation.Y, "🖥 THE BRIDGE LOG"),
            ("manifest", (float)WreckLayout.ManifestStation.X, (float)WreckLayout.ManifestStation.Y, "📦 THE CARGO MANIFEST"),
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
        Derelict.WreckCause.Infested => "🕷 THE NEST IN THE DEEP HOLD",
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
