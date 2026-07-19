using SpaceSails.Core;

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
    public enum ConsoleKind { None, Helm, NavPost, Scope, Vent, Cargo, Shuttle, Cantina, CommsSeat, TacticalSeat, TradeSeat, Head, Airlock, BarPatron, Hatch, ViewObject, Stash, ShuttleAirlock, Barkeep, DigSite, SurfaceAirlock, Kiosk, MedKit, Bunk }

    public readonly record struct Wall(float X1, float Y1, float X2, float Y2, bool IsWindow, bool IsHull);

    /// <summary>An airlock door across a passage. An automatic door slides open as the avatar nears
    /// (a top-down flourish; it never blocks — the passage is always walkable). A <c>Locked</c> door
    /// stays shut and is drawn cold — it marks another berth's sealed hatch, decoration only, and is
    /// backed by a real wall so you can't pass.</summary>
    public readonly record struct Door(float X1, float Y1, float X2, float Y2, bool Locked = false);

    /// <summary>An interaction point on the deck. A <see cref="ConsoleKind.ViewObject"/> spot also
    /// carries an <paramref name="ImageUrl"/> and <paramref name="Caption"/> — press E and the game
    /// pops up that Gen-AI image (a souvenir, a lore prop) with its caption.</summary>
    public readonly record struct ConsoleSpot(ConsoleKind Kind, float X, float Y, string Label,
        string? ImageUrl = null, string? Caption = null);

    /// <summary>A room backdrop image: top-left at (X, Y) in deck units, W×H deck units, drawn
    /// under the vector overlay. The top-down renderer walks these; first-person textures walls.</summary>
    public readonly record struct Backdrop(string Url, float X, float Y, float W, float H, float Alpha);

    public const double InteractRadius = 3.0;
    public const double AvatarRadius = 0.7;

    /// <summary>The SHUTTLE-BAY HATCH on the ship's bottom hull (#295): the wild-side threshold the
    /// down-tube mates to, mirroring the top airlock hatch that mates the station tube. The bare ship
    /// seals it; a surface excursion (see <c>MoonSurface</c>) carves it open and grows the tube below.
    /// The crew-only-door law lives here: Reevers may chase to this line but never cross it.</summary>
    public const float ShuttleHatchY = -10f;
    public const float ShuttleHatchX1 = -9f;
    public const float ShuttleHatchX2 = -5f;

    /// <summary>Upper bound on droids in any one plan — the render buffers size to this. Bumped to 9
    /// for the docked complex's roaming NPC (PR-F: a station patron on a sim-time rota, index 8), then
    /// to 10 for the bar's barkeep pacing behind the counter (#247, index 9). Lane-1 (owner, 2026-07-18):
    /// the surface tide needs room for the 3 crew + the engine ceiling on live Reevers (24), so the
    /// buffer grows to 27 — only the surface plan ever fills that far; the ship/complex still fill ≤10.</summary>
    public const int MaxDroids = 27;

    public readonly record struct Droid(double X, double Y, double FacingRad, string Name);

    public Wall[] Walls { get; }

    /// <summary>PR-324 · The walls as bare collidable/opaque segments — the single source the captain's
    /// avatar, the surface Reevers (<c>ReeverChase</c>), and the crude line-of-sight check all share, so
    /// the maze is law for everyone. Built once with the plan; both movers obey the same lines.</summary>
    public SurfaceCollision.Segment[] CollisionSegments { get; }

    public ConsoleSpot[] Consoles { get; }
    public (float X, float Y, string Text)[] RoomLabels { get; }
    public Backdrop[] Backdrops { get; }
    public Door[] Doors { get; }

    /// <summary>Round table tops drawn as a ring on the floor — cantina/bar dressing. Plan-driven so
    /// any room (the ship's cantina, a haven bar) can lay out its own.</summary>
    public (float X, float Y)[] Tables { get; }
    public double SpawnX { get; }
    public double SpawnY { get; }
    public int DroidCount { get; }

    /// <summary>Draw the ship-only dressing (cargo crates, reactor, shuttle cradle, cantina tables) —
    /// true for the ship and for a docked complex that contains it, false for a bare haven room.</summary>
    public bool ShipFixtures { get; }

    /// <summary>The top-down camera should scroll to keep the avatar centred rather than framing the
    /// whole plan at once — set for the docked complex (ship + tube + station), which is far too long
    /// to fit the fixed tactical frame. A lone room or the bare ship stays whole-frame.</summary>
    public bool FollowCam { get; }

    private readonly Action<double, Droid[]> _fillDroids;
    private readonly Func<double, double, string> _location;

    public DeckPlan(
        Wall[] walls, ConsoleSpot[] consoles, (float X, float Y, string Text)[] roomLabels,
        Backdrop[] backdrops, double spawnX, double spawnY,
        int droidCount, Action<double, Droid[]> fillDroids, Func<double, double, string> location,
        Door[]? doors = null, bool shipFixtures = false, bool followCam = false,
        (float X, float Y)[]? tables = null)
    {
        Walls = walls;
        CollisionSegments = new SurfaceCollision.Segment[walls.Length];
        for (int i = 0; i < walls.Length; i++)
        {
            CollisionSegments[i] = new SurfaceCollision.Segment(walls[i].X1, walls[i].Y1, walls[i].X2, walls[i].Y2);
        }
        Consoles = consoles;
        RoomLabels = roomLabels;
        Backdrops = backdrops;
        Doors = doors ?? [];
        Tables = tables ?? [];
        SpawnX = spawnX;
        SpawnY = spawnY;
        DroidCount = droidCount;
        _fillDroids = fillDroids;
        _location = location;
        ShipFixtures = shipFixtures;
        FollowCam = followCam;
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

    // PR-324 · The avatar's own collision is now the shared Core check (SurfaceCollision), the very same
    // one the surface Reevers obey — one wall law for everyone on the walked ground.
    private bool Collides(double x, double y) => SurfaceCollision.Blocked(x, y, AvatarRadius, CollisionSegments);

    public static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2) =>
        SurfaceCollision.DistanceToSegment(px, py, x1, y1, x2, y2);

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
            new(17, 10, 6, 10, IsWindow: true, IsHull: true),    // the cantina's panoramic window
            new(-1, 10, -18, 10, false, true),

            // --- Airlock vestibule (port bump-out, x -1..6, y 10..14) ---
            // The airlock moved off the galley (owner, Expanse consult): it sits at the port end of a
            // WIDE airlock corridor (the 7-du slot between shuttle bay and cantina), in a bumped-out
            // vestibule — a defensible kill-box. A 3-du hatch in the port wall; two blast walls flank
            // it for cover, so the crew can repel boarders from behind hard cover on both sides.
            new(-1, 10, -1, 14, false, true),   // vestibule port-side hull
            new(6, 10, 6, 14, false, true),     // vestibule starboard-side hull
            new(-1, 14, 1, 14, false, true),    // outer wall, port of the hatch
            new(6, 14, 4, 14, false, true),     // outer wall, starboard of the hatch
            new(1, 14, 4, 14, false, true),     // the hatch itself — sealed on the bare ship; the docked complex opens it and mates the tube
            new(1, 14, 1, 11, false, false),    // cover: port blast wall
            new(4, 14, 4, 11, false, false),    // cover: starboard blast wall
            new(-18, 10, -24, 7, false, true),
            new(-24, 7, -24, -7, false, true),
            new(-24, -7, -18, -10, false, true),
            // The bottom hull, split around the SHUTTLE-BAY HATCH (#295: the bay moved to the bottom
            // edge — the wild side). Sealed on the bare ship, exactly like the top airlock hatch; a
            // surface excursion opens it and mates the down-tube (see MoonSurface).
            new(-18, -10, ShuttleHatchX1, -10, false, true),
            new(ShuttleHatchX2, -10, 20, -10, false, true),
            new(ShuttleHatchX1, -10, ShuttleHatchX2, -10, false, true), // the hatch itself — sealed here

            // --- Bridge bulkhead (x=18), door on the centerline, 4 du wide ---
            new(18, 10, 18, 2, false, false),
            new(18, -2, 18, -10, false, false),

            // --- Cantina (port, x 6..18, y 3..10): corridor wall with a wide door ---
            // Its port wall pulled back to x=6 to open the wide airlock corridor slot (x -1..6).
            new(18, 3, 13, 3, false, false),
            new(9, 3, 6, 3, false, false),
            new(6, 10, 6, 3, false, false),

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

            // --- Cargo hold (port, x -12..-1, y 3..10): corridor wall, wide bay door (#295: the hold
            //     and the shuttle bay swapped sides so the bay meets the bottom hull — geography intact,
            //     walls unchanged, only the room's identity + fixtures moved). ---
            // Its starboard wall pulled back to x=-1 to open the wide airlock corridor slot (x -1..6).
            new(-1, 3, -3, 3, false, false),
            new(-7, 3, -12, 3, false, false),
            new(-1, 10, -1, 3, false, false),
            new(-12, 10, -12, 3, false, false),

            // --- Shuttle bay (bottom-port, x -12..2, y -10..-3): the wild-side bay, its hatch on the
            //     bottom hull. K-77 and R-3B are stationed here; the down-tube grows from the hatch. ---
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
            new(ConsoleKind.Cargo, -5, 6.5f, "CARGO"),        // #295: the hold is now the top-port room
            new(ConsoleKind.Shuttle, -10, -6.5f, "SHUTTLE BAY"), // #295: the bay is now bottom-port

            // The shuttle-bay airlock (#163; moved to the bottom hull hatch for #295): walk up and it
            // opens the "places in shuttle range" pop-up — the door you understand as a flight, and now
            // the hinge the surface excursion grows a down-tube from. Kept clear of the SHUTTLE BAY
            // console (−10, −6.5) so [E] doesn't grab the wrong one. Drawn as the amber airlock door.
            new(ConsoleKind.ShuttleAirlock, -6.5f, -8.7f, "🚀 SHUTTLE AIRLOCK"),
            new(ConsoleKind.Vent, -20, -4.5f, "VENT PANEL"),
            new(ConsoleKind.Head, 16.25f, -6.5f, "HEAD 🚽"), // the space toilet (3D-reno Phase 3)

            // MED BAY (owner's Evening-wind ruling, 2026-07-18: "change one cabin into med bay where
            // calming pills can be retrieved to help restore sanity to captain"). CABIN 3 [x 4..7.5] is
            // reborn as the med bay; its MED KIT console sits mid-berth (mirrors the HEAD's y), and [E]
            // there takes one calming pill (see InteractAtConsole's MedKit case). The [E] hint is drawn
            // automatically when the captain is near, so the label stays clean.
            new(ConsoleKind.MedKit, 5.75f, -6.5f, "MED KIT 💊"),

            // BUNK 🛏 (owner's live ruling, 2026-07-19: "Let's have a sanity restoring sleep action in one of
            // the cabins" — the REST half of Evening-wind #21). CABIN 1 [x 11..14.5], the tidy berth, keeps
            // its bunk; its console sits mid-berth (mirrors the HEAD's and MED KIT's y), and [E] there turns
            // in for a night's sleep (see InteractAtConsole's Bunk case → Sleep). Free but honest — a short
            // WELL-RESTED satiety stops it being the steady-hands grind (CabinComforts owns that law).
            new(ConsoleKind.Bunk, 12.75f, -6.5f, "BUNK 🛏"),

            // The gangway to a docked haven (go-ashore, 2026-07-07; moved to the airlock vestibule
            // 2026-07-08). In the docked complex you walk the tube; on the bare ship, pressing E here
            // just teaches "clamp on first" (see InteractAtConsole's Airlock case).
            new(ConsoleKind.Airlock, 2.5f, 11.5f, "⚓ GANGWAY"),

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
            (2.5f, 12f, "⚓ AIRLOCK"),
            (12.75f, -9f, "CABIN 1"), (9.25f, -9f, "CABIN 2"), (5.75f, -9f, "MED BAY"), // CABIN 3 → MED BAY (owner 2026-07-18)
            (-6, 8.5f, "CARGO HOLD"),
            (-6, -8.5f, "SHUTTLE BAY"),
            (-19, 5, "ENGINE ROOM"),
        ];

        // Room backdrops (3D-reno Phases 1 & 3): the cantina wears The Space Bar; each starboard
        // berth wears its own art so the crew reads as individuals; the HEAD wears a grimy toilet.
        Backdrop[] backdrops =
        [
            new("art/the-space-bar.jpg", 6, 10, 12, 7, 0.9f),   // CANTINA, zone x∈[6,18] y∈[3,10]
            new("art/cabin-tidy.jpg", 11, -3, 3.5f, 7, 0.9f),   // CABIN 1
            new("art/cabin-messy-a.jpg", 7.5f, -3, 3.5f, 7, 0.9f), // CABIN 2
            // CABIN 3 → MED BAY (owner 2026-07-18): the med-bay backdrop replaces the old messy-cabin art,
            // wired exactly as the cabin arts are (Grok-generated ship-med-bay.jpg, same zone/alpha).
            new("art/ship-med-bay.jpg", 4, -3, 3.5f, 7, 0.9f), // MED BAY (was CABIN 3)
            new("art/space-head.jpg", 14.5f, -3, 3.5f, 7, 0.9f), // HEAD 🚽
        ];

        // Cantina tables (plan-driven now): three tops with a view, port side.
        (float X, float Y)[] tables = [(8, 7.5f), (11, 6), (14, 7.5f)];

        // The shuttle-bay airlock door (#163; #295 moved it to the bottom hull hatch): an amber
        // auto-door across the SHUTTLE-BAY HATCH on the bottom hull. On the bare ship it sits on the
        // sealed hatch (the hull stays closed, the raycaster never escapes) — walking through it is the
        // shuttle flight, resolved by the "places in shuttle range" pop-up. A surface excursion opens
        // the hatch and grows a down-tube through it (see MoonSurface).
        Door[] doors =
        [
            new(ShuttleHatchX1, -10, ShuttleHatchX2, -10),
        ];

        return new DeckPlan(walls, consoles, roomLabels, backdrops,
            spawnX: 21, spawnY: 0, // on the bridge, facing the bow glass
            droidCount: 3, fillDroids: FillShipDroids, location: ShipLocation,
            doors: doors, shipFixtures: true, tables: tables);
    }

    // --- Droid pirate infantry 🤖🏴‍☠️ ---
    // Positions are pure functions of sim time: no state, deterministic, free.
    private static void FillShipDroids(double simTime, Droid[] droids)
    {
        // Two boarding troopers at parade rest in the shuttle bay (now bottom-port, #295), idle-swaying.
        double sway = 0.08 * Math.Sin(simTime * 0.0011);
        droids[0] = new Droid(-4.5 + sway, -7.5, Math.PI / 2, "K-77");
        droids[1] = new Droid(-2.5 - sway, -7.5, Math.PI / 2, "R-3B");

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
        if (x is > -1 and < 6 && y > 3) return y > 10 ? "AIRLOCK" : "AIRLOCK CORRIDOR";
        if (x > 6 && y > 3) return "CANTINA";
        if (x > 4 && y < -3) return x > 14.5 ? "HEAD" : x < 7.5 ? "MED BAY" : "CABINS"; // CABIN 3 → MED BAY (owner 2026-07-18)
        if (x > 4) return "CORRIDOR";
        if (x > -12) return y > 3 ? "CARGO HOLD" : y < -3 ? "SHUTTLE BAY" : "CORRIDOR";
        return "CORRIDOR";
    }
}
