namespace SpaceSails.Client.Rendering;

/// <summary>
/// A walkable interior — the single source of truth for every interior view (top-down,
/// first-person, and any future isometric mode). Deck units (du), origin midships, +X bow,
/// +Y port.
///
/// Once a hardcoded static ship singleton; **now a selectable plan** (go-ashore, 2026-07-07):
/// the two renderers and the avatar loop take a <see cref="DeckPlan"/> by reference, so the
/// ship is one plan (<see cref="Ship"/>) and a haven interior (see <c>HavenInterior</c>) is
/// another. Everything downstream — collision, raycasting, console interaction, sky windows —
/// works unchanged against whichever plan is active.
///
/// The ship, bow to stern: bridge (helm + nav post) → cantina with a panoramic hull window
/// (port) / three cabins + a space HEAD 🚽 (starboard) → midship corridor → shuttle bay (port,
/// where the boarding-droid infantry is stationed) / cargo hold (starboard) → engine room.
/// </summary>
public sealed class DeckPlan
{
    public enum ConsoleKind { None, Helm, NavPost, Scope, Vent, Cargo, Shuttle, Cantina, CommsSeat, TacticalSeat, TradeSeat, Head, Airlock, BarPatron }

    public readonly record struct Wall(float X1, float Y1, float X2, float Y2, bool IsWindow, bool IsHull);

    public readonly record struct ConsoleSpot(ConsoleKind Kind, float X, float Y, string Label);

    /// <summary>A room backdrop image: top-left at (X, Y) in deck units, W×H deck units, drawn
    /// under the vector overlay. The top-down renderer walks these; first-person textures walls.</summary>
    public readonly record struct Backdrop(string Url, float X, float Y, float W, float H, float Alpha);

    public const double InteractRadius = 3.0;
    public const double AvatarRadius = 0.7;

    /// <summary>Upper bound on droids in any one plan — the render buffers size to this.</summary>
    public const int MaxDroids = 8;

    public readonly record struct Droid(double X, double Y, double FacingRad, string Name);

    public Wall[] Walls { get; }
    public ConsoleSpot[] Consoles { get; }
    public (float X, float Y, string Text)[] RoomLabels { get; }
    public Backdrop[] Backdrops { get; }
    public double SpawnX { get; }
    public double SpawnY { get; }
    public int DroidCount { get; }

    private readonly Action<double, Droid[]> _fillDroids;
    private readonly Func<double, double, string> _location;

    public DeckPlan(
        Wall[] walls, ConsoleSpot[] consoles, (float X, float Y, string Text)[] roomLabels,
        Backdrop[] backdrops, double spawnX, double spawnY,
        int droidCount, Action<double, Droid[]> fillDroids, Func<double, double, string> location)
    {
        Walls = walls;
        Consoles = consoles;
        RoomLabels = roomLabels;
        Backdrops = backdrops;
        SpawnX = spawnX;
        SpawnY = spawnY;
        DroidCount = droidCount;
        _fillDroids = fillDroids;
        _location = location;
    }

    /// <summary>Fill <paramref name="buffer"/>[0..DroidCount) with this plan's droids at sim time.</summary>
    public void FillDroids(double simTime, Droid[] buffer) => _fillDroids(simTime, buffer);

    /// <summary>The room label for a deck position — the first-person HUD's location line.</summary>
    public string Location(double x, double y) => _location(x, y);

    // --- Collision ---

    public (double X, double Y) Move(double x, double y, double dx, double dy)
    {
        double nx = Collides(x + dx, y) ? x : x + dx;
        double ny = Collides(nx, y + dy) ? y : y + dy;
        return (nx, ny);
    }

    private bool Collides(double x, double y)
    {
        foreach (Wall w in Walls)
        {
            if (DistanceToSegment(x, y, w.X1, w.Y1, w.X2, w.Y2) < AvatarRadius)
            {
                return true;
            }
        }

        return false;
    }

    public static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double lengthSq = dx * dx + dy * dy;
        double t = lengthSq > 0 ? Math.Clamp(((px - x1) * dx + (py - y1) * dy) / lengthSq, 0, 1) : 0;
        double cx = x1 + t * dx, cy = y1 + t * dy;
        return Math.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
    }

    public ConsoleKind NearestConsole(double x, double y) => NearestConsoleSpot(x, y)?.Kind ?? ConsoleKind.None;

    /// <summary>The nearest interactable console within reach, or null — lets a caller read the
    /// specific spot's label (e.g. which bar patron you walked up to), not just its kind.</summary>
    public ConsoleSpot? NearestConsoleSpot(double x, double y)
    {
        foreach (ConsoleSpot c in Consoles)
        {
            double d = Math.Sqrt((x - c.X) * (x - c.X) + (y - c.Y) * (y - c.Y));
            if (d <= InteractRadius)
            {
                return c;
            }
        }

        return null;
    }

    /// <summary>
    /// Nearest wall hit along a ray, for the raycaster. Returns false when the ray escapes
    /// (should not happen inside a closed hull). <paramref name="along"/> is the position on
    /// the wall in du (for texture banding).
    /// </summary>
    public bool CastRay(double ox, double oy, double dirX, double dirY,
        out double distance, out bool isWindow, out bool isHull, out double along)
    {
        distance = double.MaxValue;
        isWindow = false;
        isHull = false;
        along = 0;

        foreach (Wall w in Walls)
        {
            double ex = w.X2 - w.X1, ey = w.Y2 - w.Y1;
            double denom = dirX * ey - dirY * ex;
            if (Math.Abs(denom) < 1e-9)
            {
                continue;
            }

            double qx = w.X1 - ox, qy = w.Y1 - oy;
            double t = (qx * ey - qy * ex) / denom;       // along the ray
            double u = (qx * dirY - qy * dirX) / denom;   // along the segment [0,1]
            if (t > 0.02 && u >= 0 && u <= 1 && t < distance)
            {
                distance = t;
                isWindow = w.IsWindow;
                isHull = w.IsHull;
                along = u * Math.Sqrt(ex * ex + ey * ey);
            }
        }

        return distance < double.MaxValue;
    }

    // =====================================================================================
    //  The pirate ship — the default plan. Roughly 54×20 du. All doors are ≥ 3.5 du wide
    //  (the M12 plan's were too tight).
    // =====================================================================================

    /// <summary>The player's own ship. Reference-compared by the renderers to draw ship-only
    /// dressing (cargo crates, the reactor, the shuttle cradle, cantina tables).</summary>
    public static DeckPlan Ship { get; } = BuildShip();

    private static DeckPlan BuildShip()
    {
        Wall[] walls =
        [
            // --- Hull (bow point at x=30) ---
            new(30, 0, 20, 10, IsWindow: true, IsHull: true),    // bow-port slant: bridge glass
            new(30, 0, 20, -10, IsWindow: true, IsHull: true),   // bow-starboard slant: bridge glass
            new(20, 10, 17, 10, false, true),
            new(17, 10, 5, 10, IsWindow: true, IsHull: true),    // the cantina's panoramic window
            new(5, 10, -18, 10, false, true),
            new(-18, 10, -24, 7, false, true),
            new(-24, 7, -24, -7, false, true),
            new(-24, -7, -18, -10, false, true),
            new(-18, -10, 20, -10, false, true),

            // --- Bridge bulkhead (x=18), door on the centerline, 4 du wide ---
            new(18, 10, 18, 2, false, false),
            new(18, -2, 18, -10, false, false),

            // --- Cantina (port, x 4..18, y 3..10): corridor wall with a wide door ---
            new(18, 3, 13, 3, false, false),
            new(9, 3, 4, 3, false, false),
            new(4, 10, 4, 3, false, false),

            // --- Cabins + HEAD (starboard, x 4..18, y -10..-3): corridor wall with four doors.
            //     3D-reno Phase 3 split the old three-cabin block into three cabins + a space HEAD 🚽.
            //     Stern-to-bow: CABIN 3 [4,7.5], CABIN 2 [7.5,11], CABIN 1 [11,14.5], HEAD [14.5,18];
            //     each berth is 3.5 du with a 2.5 du door (jamb stubs are the ~0.5 du wall bits). ---
            new(18, -3, 17.5f, -3, false, false), new(15, -3, 14.5f, -3, false, false),   // HEAD door
            new(14.5f, -3, 14, -3, false, false), new(11.5f, -3, 11, -3, false, false),   // CABIN 1 door
            new(11, -3, 10.5f, -3, false, false), new(8, -3, 7.5f, -3, false, false),     // CABIN 2 door
            new(7.5f, -3, 7, -3, false, false), new(4.5f, -3, 4, -3, false, false),       // CABIN 3 door
            new(4, -3, 3, -3, false, false),          // stern corner stub
            new(4, -10, 4, -3, false, false),         // cabin-block stern wall
            // Berth dividers (full depth, corridor to hull)
            new(14.5f, -3, 14.5f, -10, false, false), // CABIN 1 / HEAD
            new(11, -3, 11, -10, false, false),       // CABIN 2 / CABIN 1
            new(7.5f, -3, 7.5f, -10, false, false),   // CABIN 3 / CABIN 2

            // --- Shuttle bay (port, x -12..2, y 3..10): corridor wall, wide bay door ---
            new(2, 3, -3, 3, false, false),
            new(-7, 3, -12, 3, false, false),
            new(2, 10, 2, 3, false, false),
            new(-12, 10, -12, 3, false, false),

            // --- Cargo hold (starboard, x -12..2, y -10..-3) ---
            new(2, -3, -3, -3, false, false),
            new(-7, -3, -12, -3, false, false),
            new(2, -10, 2, -3, false, false),
            new(-12, -10, -12, -3, false, false),

            // --- Engine bulkhead (x=-14), centerline door 4 du wide ---
            new(-14, 10, -14, 2, false, false),
            new(-14, -2, -14, -10, false, false),
        ];

        ConsoleSpot[] consoles =
        [
            new(ConsoleKind.Helm, 24, 2.5f, "HELM"),
            new(ConsoleKind.NavPost, 24, -2.5f, "NAV POST"),
            new(ConsoleKind.Scope, 20, 7, "SCOPE"),
            new(ConsoleKind.Cantina, 11, 7.5f, "CANTINA"),
            new(ConsoleKind.Cargo, -5, -6.5f, "CARGO"),
            new(ConsoleKind.Shuttle, -8, 6.5f, "SHUTTLE BAY"),
            new(ConsoleKind.Vent, -20, -4.5f, "VENT PANEL"),
            new(ConsoleKind.Head, 16.25f, -6.5f, "HEAD 🚽"), // the space toilet (3D-reno Phase 3)

            // The gangway to a docked haven (go-ashore, 2026-07-07): a hatch on the cantina window
            // wall, clear of the CANTINA console. Present always; pressing E off a dock just teaches
            // "clamp on first" (see InteractAtConsole's Airlock case).
            new(ConsoleKind.Airlock, 6, 9, "⚓ GANGWAY"),

            // --- Bridge seats (PR-14, StationDesks.md #14): pressing E opens the matching desk
            // without leaving the ship's own deck plan — three free spots on the bridge (x > 18),
            // clear of the helm/nav-post/scope trio, each other, and (importantly) the avatar's own
            // spawn point (SpawnX/SpawnY below) by a comfortable margin — nobody should see an [E]
            // prompt before they've taken a single step.
            new(ConsoleKind.CommsSeat, 20, -7, "COMMS SEAT"),      // mirrors Scope (20, 7) to starboard
            new(ConsoleKind.TacticalSeat, 19.5f, 4, "TACTICAL SEAT"), // port side, near the bridge door
            new(ConsoleKind.TradeSeat, 27, 0, "TRADE SEAT"),       // the bow-tip nook between helm and nav post
        ];

        (float X, float Y, string Text)[] roomLabels =
        [
            (22, -7, "BRIDGE"),
            (11, 5, "CANTINA"),
            (12.75f, -9f, "CABIN 1"), (9.25f, -9f, "CABIN 2"), (5.75f, -9f, "CABIN 3"),
            (-5, 8.5f, "SHUTTLE BAY"),
            (-5, -8.5f, "CARGO HOLD"),
            (-19, 5, "ENGINE ROOM"),
        ];

        // Room backdrops (3D-reno Phases 1 & 3): the cantina wears The Space Bar; each starboard
        // berth wears its own art so the crew reads as individuals; the HEAD wears a grimy toilet.
        Backdrop[] backdrops =
        [
            new("art/the-space-bar.jpg", 4, 10, 14, 7, 0.9f),   // CANTINA, zone x∈[4,18] y∈[3,10]
            new("art/cabin-tidy.jpg", 11, -3, 3.5f, 7, 0.9f),   // CABIN 1
            new("art/cabin-messy-a.jpg", 7.5f, -3, 3.5f, 7, 0.9f), // CABIN 2
            new("art/cabin-messy-b.jpg", 4, -3, 3.5f, 7, 0.9f), // CABIN 3
            new("art/space-head.jpg", 14.5f, -3, 3.5f, 7, 0.9f), // HEAD 🚽
        ];

        return new DeckPlan(walls, consoles, roomLabels, backdrops,
            spawnX: 21, spawnY: 0, // on the bridge, facing the bow glass
            droidCount: 3, fillDroids: FillShipDroids, location: ShipLocation);
    }

    // --- Droid pirate infantry 🤖🏴‍☠️ ---
    // Positions are pure functions of sim time: no state, deterministic, free.
    private static void FillShipDroids(double simTime, Droid[] droids)
    {
        // Two boarding troopers at parade rest in the shuttle bay, idle-swaying.
        double sway = 0.08 * Math.Sin(simTime * 0.0011);
        droids[0] = new Droid(-4.5 + sway, 7.5, -Math.PI / 2, "K-77");
        droids[1] = new Droid(-2.5 - sway, 7.5, -Math.PI / 2, "R-3B");

        // One patrolling the corridor, bow to stern and back (triangle wave, ~40 s loop).
        double phase = simTime % 40000 / 40000.0;
        double tri = phase < 0.5 ? phase * 2 : 2 - phase * 2;
        double x = -10 + tri * 26; // corridor x from -10 to 16
        droids[2] = new Droid(x, 0.6, phase < 0.5 ? 0 : Math.PI, "V-1K");
    }

    private static string ShipLocation(double x, double y)
    {
        if (x > 18) return "BRIDGE";
        if (x < -14) return "ENGINE ROOM";
        if (x > 4) return y > 3 ? "CANTINA" : y < -3 ? (x > 14.5 ? "HEAD" : "CABINS") : "CORRIDOR";
        if (x > -12) return y > 3 ? "SHUTTLE BAY" : y < -3 ? "CARGO HOLD" : "CORRIDOR";
        return "CORRIDOR";
    }
}
