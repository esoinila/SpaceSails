namespace SpaceSails.Client.Rendering;

/// <summary>
/// The pirate ship's deck plan — single source of truth for every interior view (top-down,
/// first-person, and any future isometric mode). Deck units (du), origin midships, +X bow,
/// +Y port. Roughly 54×20 du. All doors are ≥ 3.5 du wide (the M12 plan's were too tight).
///
/// Rooms, bow to stern: bridge (helm + nav post) → cantina with a panoramic hull window
/// (port) / three cabins (starboard) → midship corridor → shuttle bay (port, where the
/// boarding-droid infantry is stationed) / cargo hold (starboard) → engine room.
/// </summary>
public static class DeckPlan
{
    public enum ConsoleKind { None, Helm, NavPost, Scope, Vent, Cargo, Shuttle, Cantina, CommsSeat, TacticalSeat, TradeSeat }

    public readonly record struct Wall(float X1, float Y1, float X2, float Y2, bool IsWindow, bool IsHull);

    public readonly record struct ConsoleSpot(ConsoleKind Kind, float X, float Y, string Label);

    public const double InteractRadius = 3.0;
    public const double AvatarRadius = 0.7;

    public static readonly Wall[] Walls =
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

        // --- Cabins (starboard, x 4..18, y -10..-3): corridor wall with three doors ---
        new(18, -3, 16.5f, -3, false, false),
        new(13, -3, 11.5f, -3, false, false),
        new(8, -3, 6.5f, -3, false, false),
        new(4, -3, 3, -3, false, false),
        new(4, -10, 4, -3, false, false),
        // Cabin dividers
        new(13, -3, 13, -10, false, false),
        new(8, -3, 8, -10, false, false),

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

    public static readonly ConsoleSpot[] Consoles =
    [
        new(ConsoleKind.Helm, 24, 2.5f, "HELM"),
        new(ConsoleKind.NavPost, 24, -2.5f, "NAV POST"),
        new(ConsoleKind.Scope, 20, 7, "SCOPE"),
        new(ConsoleKind.Cantina, 11, 7.5f, "CANTINA"),
        new(ConsoleKind.Cargo, -5, -6.5f, "CARGO"),
        new(ConsoleKind.Shuttle, -8, 6.5f, "SHUTTLE BAY"),
        new(ConsoleKind.Vent, -20, -4.5f, "VENT PANEL"),

        // --- Bridge seats (PR-14, StationDesks.md #14): pressing E opens the matching desk
        // without leaving the ship's own deck plan — three free spots on the bridge (x > 18),
        // clear of the helm/nav-post/scope trio, each other, and (importantly) the avatar's own
        // spawn point (SpawnX/SpawnY below) by a comfortable margin — nobody should see an [E]
        // prompt before they've taken a single step.
        new(ConsoleKind.CommsSeat, 20, -7, "COMMS SEAT"),      // mirrors Scope (20, 7) to starboard
        new(ConsoleKind.TacticalSeat, 19.5f, 4, "TACTICAL SEAT"), // port side, near the bridge door
        new(ConsoleKind.TradeSeat, 27, 0, "TRADE SEAT"),       // the bow-tip nook between helm and nav post
    ];

    public static readonly (float X, float Y, string Text)[] RoomLabels =
    [
        (22, -7, "BRIDGE"),
        (11, 5, "CANTINA"),
        (15.5f, -6.5f, "CABIN 1"), (10.5f, -6.5f, "CABIN 2"), (6, -6.5f, "CABIN 3"),
        (-5, 8.5f, "SHUTTLE BAY"),
        (-5, -8.5f, "CARGO HOLD"),
        (-19, 5, "ENGINE ROOM"),
    ];

    /// <summary>Avatar spawn: on the bridge, facing the bow glass.</summary>
    public const double SpawnX = 21, SpawnY = 0;

    // --- Droid pirate infantry 🤖🏴‍☠️ ---
    // Positions are pure functions of sim time: no state, deterministic, free.

    public readonly record struct Droid(double X, double Y, double FacingRad, string Name);

    public static void GetDroids(double simTime, Droid[] droids)
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

    public const int DroidCount = 3;

    // --- Collision ---

    public static (double X, double Y) Move(double x, double y, double dx, double dy)
    {
        double nx = Collides(x + dx, y) ? x : x + dx;
        double ny = Collides(nx, y + dy) ? y : y + dy;
        return (nx, ny);
    }

    private static bool Collides(double x, double y)
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

    public static ConsoleKind NearestConsole(double x, double y)
    {
        foreach (ConsoleSpot c in Consoles)
        {
            double d = Math.Sqrt((x - c.X) * (x - c.X) + (y - c.Y) * (y - c.Y));
            if (d <= InteractRadius)
            {
                return c.Kind;
            }
        }

        return ConsoleKind.None;
    }

    /// <summary>
    /// Nearest wall hit along a ray, for the raycaster. Returns false when the ray escapes
    /// (should not happen inside a closed hull). <paramref name="along"/> is the position on
    /// the wall in du (for texture banding).
    /// </summary>
    public static bool CastRay(double ox, double oy, double dirX, double dirY,
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
}
