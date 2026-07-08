namespace SpaceSails.Client.Rendering;

/// <summary>
/// Walkable haven interiors — the "go ashore" side of docking (2026-07-07; walk-through tube +
/// round immigration hall + bar, 2026-07-08; spec-driven for every station, 2026-07-08).
///
/// The Expanse model (owner): docking mates the ship's airlock to the station by a <b>narrow
/// umbilical with automatic doors</b>, and you <b>walk</b> across — no teleport. Each station is far
/// bigger than the ship (small-airport-sized) and <b>each one is different</b>, named for and themed
/// to where it sits. So a docked haven welds, in one coordinate space: the ship (airlock a defensible
/// port vestibule) → the tube → a <b>big round entrance hall</b> (a 12-sided ring — 10 other berths'
/// hatches sealed, so it reads like a dozen ships are docked; a Total-Recall immigration desk;
/// signage) → a wide door → the <b>bar</b>, tables you walk up to. Confidential work (owner) changes
/// hands here, face to face at a table — no electronic trace. The top-down view follows you
/// (<see cref="DeckPlan.FollowCam"/>).
///
/// Every station shares one geometry <see cref="BuildComplex"/>; a <see cref="StationSpec"/> supplies
/// the name, the immigration authority, the deadpan quip, and the two Gen-AI backdrops (hall + bar).
/// </summary>
public static class HavenInterior
{
    /// <summary>One walkable station: which body, what it's called, and its themed dressing.</summary>
    private sealed record StationSpec(
        string BodyId, string Name, string Authority, string Quip, string BarName,
        string HallArt, string BarArt);

    // The grey-market docks with walkable interiors, each themed to its world (vision par. 8).
    private static readonly StationSpec[] Specs =
    [
        new("the-space-bar", "THE RUSTY ROADSTEAD", "MARS", "most guests stay two weeks", "THE ROADSTEAD BAR",
            "art/the-rusty-roadstead-lobby.jpg", "art/the-roadstead-bar.jpg"),
        new("cinder-roost", "CINDER ROOST", "VENUS", "mind the sulphur, spacer", "THE CINDER LOUNGE",
            "art/cinder-roost-hall.jpg", "art/cinder-roost-bar.jpg"),
        new("ringside-exchange", "RINGSIDE EXCHANGE", "SATURN", "trade fast — the rings don't wait", "THE RINGSIDE BAR",
            "art/ringside-hall.jpg", "art/ringside-bar.jpg"),
        new("the-tilt", "THE TILT", "URANUS", "everything's sideways out here", "THE TILT BAR",
            "art/the-tilt-hall.jpg", "art/the-tilt-bar.jpg"),
    ];

    private static readonly Dictionary<string, DeckPlan> Cache = new();

    /// <summary>Does this haven have a walkable interior (so docking should weld on a tube)?</summary>
    public static bool HasInterior(string bodyId) => System.Array.Exists(Specs, s => s.BodyId == bodyId);

    /// <summary>The docked complex for a body — ship + tube + hall + bar as one walkable plan — or
    /// null if that haven has no deck to walk. Built once per station, lazily, and shared.</summary>
    public static DeckPlan? DockedDeck(string bodyId)
    {
        if (System.Array.Find(Specs, s => s.BodyId == bodyId) is not { } spec)
        {
            return null;
        }
        if (!Cache.TryGetValue(bodyId, out DeckPlan? deck))
        {
            deck = BuildComplex(spec);
            Cache[bodyId] = deck;
        }
        return deck;
    }

    // --- The docking-tube umbilical (deck units), mouthing at the ship's airlock vestibule hatch ---
    private const float TubeLeft = 1f;      // the narrow walkway's port wall (hatch gap is x 1..4)
    private const float TubeRight = 4f;     // ...and starboard wall (3 du wide)
    private const float ShipHatchY = 14f;   // the ship's vestibule outer wall, where the tube mates

    // --- The round entrance hall (a regular 12-gon, far bigger than the ship) ---
    private const int HallSides = 12;
    private const float HallCenterX = 2.5f;
    private const float HallCenterY = 40f;
    private const float HallR = 17f;        // vertex radius (~34 du across — much bigger than the 20-wide ship)
    private static readonly float HallApothem = (float)(HallR * System.Math.Cos(System.Math.PI / HallSides));
    private static readonly float HallBottomY = HallCenterY - HallApothem; // the tube mates here (south edge)
    private static readonly float HallTopY = HallCenterY + HallApothem;    // the bar opens off here (north edge)

    // --- The bar, off the hall's north door — big and cavernous, a local-planet view along the back ---
    private const float BarLeft = -14f;
    private const float BarRight = 19f;
    private static readonly float BarTopY = HallTopY + 22f;

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static DeckPlan BuildComplex(StationSpec spec)
    {
        DeckPlan ship = DeckPlan.Ship;

        // The bare ship seals its airlock hatch (x 1..4); the complex opens it and mates the tube.
        var hatch = new DeckPlan.Wall(1, ShipHatchY, 4, ShipHatchY, false, true);

        var walls = new List<DeckPlan.Wall>(ship.Walls.Where(w => !w.Equals(hatch)));
        var doors = new List<DeckPlan.Door>();
        var labels = new List<(float X, float Y, string Text)>(ship.RoomLabels);

        // Tube: the umbilical from the ship's hatch up to the hall's south edge.
        walls.Add(new(TubeLeft, ShipHatchY, TubeLeft, HallBottomY, false, true));
        walls.Add(new(TubeRight, ShipHatchY, TubeRight, HallBottomY, false, true));
        doors.Add(new(TubeLeft, ShipHatchY + 1, TubeRight, ShipHatchY + 1)); // ship-end auto door
        doors.Add(new(TubeLeft, HallBottomY - 1, TubeRight, HallBottomY - 1)); // hall-end auto door

        // The round hall ring. Vertices at (15 + 30k)°, so edges are centred on the compass points;
        // edge 8 faces south (our tube) and edge 2 faces north (the bar). Every other edge is a
        // sealed berth — a real wall with a cold "locked" hatch drawn on it and a BERTH sign inside.
        var v = new (float X, float Y)[HallSides];
        for (int k = 0; k < HallSides; k++)
        {
            double a = (15 + 30 * k) * System.Math.PI / 180.0;
            v[k] = (HallCenterX + HallR * (float)System.Math.Cos(a), HallCenterY + HallR * (float)System.Math.Sin(a));
        }

        int berth = 2; // our own berth is #1
        for (int k = 0; k < HallSides; k++)
        {
            (float X, float Y) a = v[k], b = v[(k + 1) % HallSides];
            if (k == 8) // south edge: our tube mouth (gap x 1..4)
            {
                walls.Add(new(a.X, a.Y, TubeLeft, a.Y, false, true));
                walls.Add(new(TubeRight, b.Y, b.X, b.Y, false, true));
            }
            else if (k == 2) // north edge: the wide door to the bar (gap x -1..6)
            {
                walls.Add(new(a.X, a.Y, 6, a.Y, false, true));
                walls.Add(new(-1, b.Y, b.X, b.Y, false, true));
                doors.Add(new(-1, a.Y, 6, a.Y)); // wide auto door
            }
            else // a sealed berth
            {
                walls.Add(new(a.X, a.Y, b.X, b.Y, false, true));
                doors.Add(new(Lerp(a.X, b.X, 0.25f), Lerp(a.Y, b.Y, 0.25f),
                              Lerp(a.X, b.X, 0.75f), Lerp(a.Y, b.Y, 0.75f), Locked: true));
                float mx = (a.X + b.X) / 2, my = (a.Y + b.Y) / 2;
                labels.Add((HallCenterX + (mx - HallCenterX) * 0.8f, HallCenterY + (my - HallCenterY) * 0.8f, $"⚓{berth:00}"));
                berth++;
            }
        }

        // Immigration desk (Total Recall): two counters with a central GATE aligned to the tube, so
        // you walk straight off the umbilical through the checkpoint. Officer to one side.
        float deskY = HallBottomY + 6;
        walls.Add(new(-7, deskY, 1, deskY, false, false)); // counter, port of the gate
        walls.Add(new(4, deskY, 9, deskY, false, false));  // counter, starboard of the gate (gate gap x 1..4)
        labels.Add((HallCenterX, HallBottomY + 7.5f, $"{spec.Authority} IMMIGRATION"));
        labels.Add((HallCenterX, HallBottomY + 2.5f, spec.Quip));
        labels.Add((HallCenterX, HallCenterY + 3, $"⚓ {spec.Name}"));

        // The bar, off the hall's north door.
        walls.Add(new(BarLeft, HallTopY, -1, HallTopY, false, true));   // bar floor wall, port of the door
        walls.Add(new(6, HallTopY, BarRight, HallTopY, false, true));   // bar floor wall, starboard of the door
        walls.Add(new(BarLeft, HallTopY, BarLeft, BarTopY, false, true));
        walls.Add(new(BarRight, HallTopY, BarRight, BarTopY, false, true));
        walls.Add(new(BarLeft, BarTopY, BarRight, BarTopY, true, true)); // spinward window onto space
        labels.Add((HallCenterX, BarTopY - 3, spec.BarName));

        // The bar's regulars, each at a table — walk up and press E. Drop the ship's ⚓ gangway.
        var consoles = new List<DeckPlan.ConsoleSpot>(ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock))
        {
            new(DeckPlan.ConsoleKind.BarPatron, -9, HallTopY + 6, "◈ ONE-EYE SILAS"),
            new(DeckPlan.ConsoleKind.BarPatron, 14, HallTopY + 6, "◈ MADAM COIL"),
            new(DeckPlan.ConsoleKind.BarPatron, 2.5f, HallTopY + 11, "◈ GILT-EYE"),
        };

        // Seven tables spread across the big room — three taken by the regulars, four open (for a
        // stranger to drift over, later) — plus the ship's own cantina tables.
        var tables = new List<(float X, float Y)>(ship.Tables)
        {
            (-9, HallTopY + 6), (14, HallTopY + 6), (2.5f, HallTopY + 11),
            (-9, HallTopY + 16), (14, HallTopY + 16), (-3, HallTopY + 18), (8, HallTopY + 18),
        };

        DeckPlan.Backdrop[] backdrops =
        [
            .. ship.Backdrops,
            // Concourse art across the round hall — sized ~16:9 to match the image so the domed ceiling
            // isn't stretched; fills the hall's width, floor showing at the very top/bottom.
            new(spec.HallArt, HallCenterX - 16, HallCenterY + 9, 32, 18, 0.95f),
            new(spec.BarArt, BarLeft, BarTopY, BarRight - BarLeft, BarTopY - HallTopY, 0.95f),
        ];

        return new DeckPlan(walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops,
            spawnX: 2.5, spawnY: 6, // aboard, in the airlock corridor, facing up the tube
            droidCount: 7, fillDroids: FillComplexDroids,
            location: (x, y) => y > HallTopY ? spec.BarName
                              : y > HallBottomY ? $"{spec.Authority} IMMIGRATION"
                              : y > ShipHatchY ? "GANGWAY"
                              : DeckPlan.Ship.Location(x, y),
            doors: doors.ToArray(), shipFixtures: true, followCam: true, tables: tables.ToArray());
    }

    // Ship's three droids, the immigration officer, and the three seated bar regulars. Shared across
    // every station (one geometry); deterministic in sim time, stateless.
    private static void FillComplexDroids(double simTime, DeckPlan.Droid[] buffer)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // fills [0..3)
        double sway = 0.05 * System.Math.Sin(simTime * 0.0009);
        buffer[3] = new DeckPlan.Droid(6.5, HallBottomY + 7, -System.Math.PI / 2, "Customs"); // officer beside the gate
        buffer[4] = new DeckPlan.Droid(-9, HallTopY + 7 + sway, -System.Math.PI / 2, "Silas");
        buffer[5] = new DeckPlan.Droid(14, HallTopY + 7 - sway, -System.Math.PI / 2, "Coil");
        buffer[6] = new DeckPlan.Droid(2.5, HallTopY + 12 + sway, -System.Math.PI / 2, "Gilt-Eye");
    }
}
