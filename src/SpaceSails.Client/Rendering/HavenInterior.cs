using SpaceSails.Core.Interior;

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
///
/// <b>Doors that grow the world (Wednesday plan §3 PR-F / Tuesday vision §6).</b> A hatch that has
/// been cracked open is no longer decoration: its hall edge is <i>carved into a walkable doorway</i>
/// and a real back room is <i>welded on at runtime</i> — geometry as data (a Core
/// <see cref="DeckWing"/>). The first shipped case is Cinder Roost's Bonded Stores hatch (V-06),
/// behind which lies the fence's back room. And per the owner's ruling ("people cannot be static
/// furniture"), a roaming patron — the Magpie — keeps a sim-time <see cref="NpcSchedule"/>: found at
/// a bar table one watch, gone behind a locked door the next, waiting in the opened back room after
/// that.
/// </summary>
public static class HavenInterior
{
    /// <summary>One walkable station: which body, what it's called, and its themed dressing.</summary>
    private sealed record StationSpec(
        string BodyId, string Name, string Authority, string Quip, string BarName,
        string HallArt, string BarArt, string TshirtArt, string MagnetArt, string Gag);

    // The grey-market docks with walkable interiors, each themed to its world (vision par. 8). Gag =
    // the T-shirt one-liner (owner's "every place has a gift shop" joke).
    private static readonly StationSpec[] Specs =
    [
        new("the-space-bar", "THE RUSTY ROADSTEAD", "MARS", "most guests stay two weeks", "THE ROADSTEAD BAR",
            "art/the-rusty-roadstead-lobby.jpg", "art/the-roadstead-bar.jpg",
            "art/souvenir-roadstead-tshirt.jpg", "art/souvenir-roadstead-magnet.jpg",
            "“I visited Mars and all I got was this rusty T-shirt.”"),
        new("cinder-roost", "CINDER ROOST", "VENUS", "mind the sulphur, spacer", "THE CINDER LOUNGE",
            "art/cinder-roost-hall.jpg", "art/cinder-roost-bar.jpg",
            "art/souvenir-cinder-tshirt.jpg", "art/souvenir-cinder-magnet.jpg",
            "“I visited Venus and all I got was this lousy T-shirt.”"),
        new("ringside-exchange", "RINGSIDE EXCHANGE", "SATURN", "trade fast — the rings don't wait", "THE RINGSIDE BAR",
            "art/ringside-hall.jpg", "art/ringside-bar.jpg",
            "art/souvenir-ringside-tshirt.jpg", "art/souvenir-ringside-magnet.jpg",
            "“I went all the way to Saturn and all I got was this T-shirt.”"),
        new("the-tilt", "THE TILT", "URANUS", "everything's sideways out here", "THE TILT BAR",
            "art/the-tilt-hall.jpg", "art/the-tilt-bar.jpg",
            "art/souvenir-tilt-tshirt.jpg", "art/souvenir-tilt-magnet.jpg",
            "“I went to Uranus for the proctologist — they were fully booked.”"),
    ];

    // Keyed by "bodyId|<sorted opened-hatch ids>", so the locked concourse and the wing-grown variant
    // are cached side by side and a station is still built at most once per unlock state.
    private static readonly Dictionary<string, DeckPlan> Cache = new();

    /// <summary>Does this haven have a walkable interior (so docking should weld on a tube)?</summary>
    public static bool HasInterior(string bodyId) => System.Array.Exists(Specs, s => s.BodyId == bodyId);

    /// <summary>
    /// The docked complex for a body — ship + tube + hall + bar as one walkable plan — or null if that
    /// haven has no deck to walk. <paramref name="unlockedHatchIds"/> is the session's set of cracked
    /// hatch ids for this station (bare ids like "V-06"); any that grow a wing weld their back room on.
    /// Built once per (station, unlock-state), lazily, and shared.
    /// </summary>
    public static DeckPlan? DockedDeck(string bodyId, IReadOnlySet<string>? unlockedHatchIds = null)
    {
        if (System.Array.Find(Specs, s => s.BodyId == bodyId) is not { } spec)
        {
            return null;
        }
        IReadOnlyList<DeckWing> active = unlockedHatchIds is null
            ? []
            : DeckExpansions.ActiveWings(WingCatalog(bodyId), bodyId, unlockedHatchIds).ToList();
        string key = active.Count == 0
            ? bodyId
            : bodyId + "|" + string.Join(",", active.Select(w => w.UnlockHatchId).OrderBy(s => s, System.StringComparer.Ordinal));
        if (!Cache.TryGetValue(key, out DeckPlan? deck))
        {
            deck = BuildComplex(spec, active);
            Cache[key] = deck;
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

    // --- The roaming Magpie (PR-F, the owner's "people cannot be static furniture" ruling) ---
    // A fence's runner who never sits still: a bar table one watch, out of reach the next, waiting in
    // the opened Bonded Stores back room after that. Four sim-hours a stop; a full loop is half a day,
    // so a docked captain who warps the clock (or a ?simhours= cheat) sees the swap without waiting.
    private const double MagpiePostSeconds = 4 * 3600;
    private static readonly (double X, double Y, double Facing) MagpieBarPost = (8, HallTopY + 18, -System.Math.PI / 2);
    private static readonly (double X, double Y, double Facing) MagpieBackPost = (-24.13, 31.28, System.Math.PI / 4);

    /// <summary>The Magpie's sim-time rota (bar → gone → back room), the pure schedule from Core.</summary>
    public static readonly NpcSchedule MagpieRota = new("The Magpie", MagpiePostSeconds,
    [
        new NpcPost("THE CINDER LOUNGE", MagpieBarPost.X, MagpieBarPost.Y, MagpieBarPost.Facing, Present: true),
        new NpcPost("GONE", 0, 0, 0, Present: false),
        new NpcPost("BACK ROOM", MagpieBackPost.X, MagpieBackPost.Y, MagpieBackPost.Facing, Present: true),
    ]);

    /// <summary>Where the Magpie is at <paramref name="simTime"/>. If the rota would place them in the
    /// back room but it hasn't been cracked open yet, they're simply out of reach (the GONE slot) —
    /// so the deck never draws them standing inside a wall that isn't there.</summary>
    public static NpcPost ResolveMagpie(double simTime, bool backRoomOpen)
    {
        NpcPost p = MagpieRota.Resolve(simTime);
        return p.Location == "BACK ROOM" && !backRoomOpen ? MagpieRota.PostAt(1) : p;
    }

    // --- Runtime wings (Core DeckWing catalog) ------------------------------------------------------
    // Authored per station against the hall geometry. v1 ships one: Cinder Roost's Bonded Stores back
    // room (V-06). Rooms gate on quests (you must crack the hatch) and quests gate on rooms (the
    // fence's package can only be lifted once the room exists) — see Map.razor.
    private static readonly Dictionary<string, DeckWing[]> WingCatalogs = new()
    {
        ["cinder-roost"] = [DeckExpansions.Validate(BondedBackRoom("cinder-roost", "V-06"))],
    };

    /// <summary>The wings authored for a station (possibly none).</summary>
    public static IReadOnlyList<DeckWing> WingCatalog(string bodyId) =>
        WingCatalogs.TryGetValue(bodyId, out DeckWing[]? w) ? w : [];

    /// <summary>Does cracking this hatch open a real room (rather than just blinking a lock green)?</summary>
    public static bool HatchGrowsWing(string bodyId, string hatchId) =>
        DeckExpansions.GrowsBehind(WingCatalog(bodyId), bodyId, hatchId);

    private static (float X, float Y) HallVertex(int k)
    {
        double a = (15 + 30 * k) * System.Math.PI / 180.0;
        return (HallCenterX + HallR * (float)System.Math.Cos(a), HallCenterY + HallR * (float)System.Math.Sin(a));
    }

    // The fence's back room behind a station's BONDED STORES hatch (edge 6 of the ring). The room is a
    // funnel off the doorway (the doorway itself is carved by BuildComplex, so the wing carries only
    // the walls beyond it), with the fence's stash on the back shelf and the Magpie's back-room booth.
    private static DeckWing BondedBackRoom(string bodyId, string hatchId)
    {
        (float ax, float ay) = HallVertex(6);
        (float bx, float by) = HallVertex(7);
        (WingWall stubA, WingWall stubB, _) = DeckExpansions.CarveDoorway(ax, ay, bx, by, 0.30f, 0.70f);
        double p30x = stubA.X2, p30y = stubA.Y2;  // doorway mouth, 30% along the edge
        double p70x = stubB.X1, p70y = stubB.Y1;  // doorway mouth, 70% along the edge

        // Outward-normal / edge-tangent frame, so the room sits squarely outside the hall.
        double mx = (ax + bx) / 2, my = (ay + by) / 2;
        double nx = mx - HallCenterX, ny = my - HallCenterY;
        double nl = System.Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;
        double tx = bx - ax, ty = by - ay;
        double tl = System.Math.Sqrt(tx * tx + ty * ty); tx /= tl; ty /= tl;
        const double d1 = 5, widen = 4, d2 = 12;
        double s30x = p30x + nx * d1 - tx * widen, s30y = p30y + ny * d1 - ty * widen;   // left shoulder
        double s70x = p70x + nx * d1 + tx * widen, s70y = p70y + ny * d1 + ty * widen;   // right shoulder
        double bk30x = s30x + nx * d2, bk30y = s30y + ny * d2;                            // back-left corner
        double bk70x = s70x + nx * d2, bk70y = s70y + ny * d2;                            // back-right corner
        double rcx = (s30x + s70x + bk30x + bk70x) / 4, rcy = (s30y + s70y + bk30y + bk70y) / 4;
        double stashx = (bk30x + bk70x) / 2 - nx * 2.5, stashy = (bk30y + bk70y) / 2 - ny * 2.5;

        var walls = new List<WingWall>
        {
            new((float)p30x, (float)p30y, (float)s30x, (float)s30y),   // left flare
            new((float)s30x, (float)s30y, (float)bk30x, (float)bk30y), // left side
            new((float)bk30x, (float)bk30y, (float)bk70x, (float)bk70y), // back wall
            new((float)bk70x, (float)bk70y, (float)s70x, (float)s70y), // right side
            new((float)s70x, (float)s70y, (float)p70x, (float)p70y),   // right flare
        };
        var consoles = new List<WingConsole>
        {
            new(WingConsoleKind.Stash, (float)stashx, (float)stashy, "📦 FENCE'S STASH"),
            new(WingConsoleKind.Patron, (float)MagpieBackPost.X, (float)MagpieBackPost.Y, "◈ THE MAGPIE"),
        };
        var labels = new List<WingLabel>
        {
            new((float)rcx, (float)rcy, "BONDED STORES · BACK ROOM"),
        };
        // No wing-owned doors: the doorway (an unlocked auto-door) is carved by BuildComplex.
        return new DeckWing($"{bodyId}-bonded-backroom", bodyId, hatchId, "BONDED STORES BACK ROOM",
            walls, [], consoles, labels);
    }

    private static DeckPlan.ConsoleKind MapConsoleKind(WingConsoleKind kind) => kind switch
    {
        WingConsoleKind.Hatch => DeckPlan.ConsoleKind.Hatch,
        WingConsoleKind.Stash => DeckPlan.ConsoleKind.Stash,
        WingConsoleKind.Patron => DeckPlan.ConsoleKind.BarPatron,
        WingConsoleKind.ViewObject => DeckPlan.ConsoleKind.ViewObject,
        _ => DeckPlan.ConsoleKind.None,
    };

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static DeckPlan BuildComplex(StationSpec spec, IReadOnlyList<DeckWing> activeWings)
    {
        DeckPlan ship = DeckPlan.Ship;
        bool backRoomOpen = activeWings.Count > 0; // the Magpie's back-room stop is reachable once a wing is welded on

        // Hatch ids whose edge has grown a wing — carve a doorway there instead of a sealed wall.
        var openHatchIds = new HashSet<string>(activeWings.Select(w => w.UnlockHatchId));

        // The bare ship seals its airlock hatch (x 1..4); the complex opens it and mates the tube.
        var hatch = new DeckPlan.Wall(1, ShipHatchY, 4, ShipHatchY, false, true);

        var walls = new List<DeckPlan.Wall>(ship.Walls.Where(w => !w.Equals(hatch)));
        // Seed from the ship's own doors so the shuttle-bay airlock (#163) travels with the ship into
        // every docked complex — that is the captain's ride home, so the return hop is never stranded.
        var doors = new List<DeckPlan.Door>(ship.Doors);
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
            v[k] = HallVertex(k);
        }

        // The ring's sealed edges: a few other captains' berths and the station's own departments,
        // nearly all locked to us — so the concourse reads as one hub of a much bigger complex. Each
        // is a numbered Hatch console: walk up and it names itself + shows locked; press E to knock.
        // A cracked hatch that grows a wing is drawn open (📂) and its edge is a real doorway.
        string[] ringTags =
        [
            "⚓ BERTH", "🔒 CUSTOMS", "🔒 HABITAT RING", "⚓ BERTH", "🔒 MEDBAY",
            "🔒 BONDED STORES", "⚓ BERTH", "🔒 DOCKMASTER", "🔒 TRANSIT", "🔒 SECURITY",
        ];
        var hatches = new List<DeckPlan.ConsoleSpot>();
        int sealedIdx = 0;
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
            else // a sealed berth / department — or an opened expansion joint
            {
                string tag = ringTags[sealedIdx % ringTags.Length];
                string id = $"{spec.Authority[0]}-{k:D2}"; // e.g. M-05: findable, distinct per station
                sealedIdx++;
                float px = HallCenterX + ((a.X + b.X) / 2 - HallCenterX) * 0.9f;
                float py = HallCenterY + ((a.Y + b.Y) / 2 - HallCenterY) * 0.9f;
                if (openHatchIds.Contains(id))
                {
                    // Cracked: carve a walkable doorway (two stubs + an unlocked auto-door), and draw
                    // the panel open (📂). The wing's own walls, added below, close the room beyond.
                    (WingWall stubA, WingWall stubB, WingDoor door) =
                        DeckExpansions.CarveDoorway(a.X, a.Y, b.X, b.Y, 0.30f, 0.70f);
                    walls.Add(new(stubA.X1, stubA.Y1, stubA.X2, stubA.Y2, false, true));
                    walls.Add(new(stubB.X1, stubB.Y1, stubB.X2, stubB.Y2, false, true));
                    doors.Add(new(door.X1, door.Y1, door.X2, door.Y2)); // unlocked — you walk through
                    string dept = string.Join(' ', tag.Split(' ').Where(t => t.All(char.IsLetter)));
                    hatches.Add(new(DeckPlan.ConsoleKind.Hatch, px, py, $"📂 {dept} · {id}"));
                }
                else
                {
                    // Sealed: a real wall with a cold locked hatch drawn on it and a knockable panel.
                    walls.Add(new(a.X, a.Y, b.X, b.Y, false, true));
                    doors.Add(new(Lerp(a.X, b.X, 0.25f), Lerp(a.Y, b.Y, 0.25f),
                                  Lerp(a.X, b.X, 0.75f), Lerp(a.Y, b.Y, 0.75f), Locked: true));
                    hatches.Add(new(DeckPlan.ConsoleKind.Hatch, px, py, $"{tag} · {id}"));
                }
            }
        }

        // Immigration desk (Total Recall): two counters with a central GATE aligned to the tube, so
        // you walk straight off the umbilical through the checkpoint. Officer to one side.
        float deskY = HallBottomY + 6;
        walls.Add(new(-7, deskY, 1, deskY, false, false)); // counter, port of the gate
        walls.Add(new(4, deskY, 9, deskY, false, false));  // counter, starboard of the gate (gate gap x 1..4)
        labels.Add((HallCenterX, HallBottomY + 7.5f, $"{spec.Authority} IMMIGRATION"));
        labels.Add((HallCenterX, HallBottomY + 2.5f, spec.Quip));
        // A big lobby welcome poster so you know at a glance which port you're standing in.
        labels.Add((HallCenterX, HallCenterY + 8, $"★  WELCOME TO {spec.Name}  ★"));
        labels.Add((HallCenterX, HallCenterY + 3, $"⚓ {spec.Authority} ORBIT"));

        // The bar, off the hall's north door.
        walls.Add(new(BarLeft, HallTopY, -1, HallTopY, false, true));   // bar floor wall, port of the door
        walls.Add(new(6, HallTopY, BarRight, HallTopY, false, true));   // bar floor wall, starboard of the door
        walls.Add(new(BarLeft, HallTopY, BarLeft, BarTopY, false, true));
        walls.Add(new(BarRight, HallTopY, BarRight, BarTopY, false, true));
        walls.Add(new(BarLeft, BarTopY, BarRight, BarTopY, true, true)); // spinward window onto space
        labels.Add((HallCenterX, BarTopY - 6.5f, spec.BarName));
        labels.Add((8f, HallTopY + 1.5f, "🎁 GIFT SHOP")); // every place has one (owner)

        // #247 — the bar counter, and the BARKEEP behind it. Owner ashore at the Rusty Roadstead: "How
        // do I get a drink at the Rusty bar here? Did we forget to add the bar-keep :-D". The counter is
        // a real wall (you belly up, you don't walk through it); the barkeep console sits on the players'
        // side of it, so E leans in for the house special. The keep's name + drink come from Core.
        //
        // 2026-07-18 ("Evening wind" plan) — the per-image correction. The first pass shared ONE counter
        // for all four bars and pinned it three du off the far wall (BarTopY − 3), which dropped the keep
        // and the pacing droid up in the window/ceiling band of every backdrop. The owner ruled per-image:
        // "the bar-keep service position … needs to be AT that desk … not the middle of the empty floor …
        // Not on top of a window — and the bar to be on top of the bar in the picture." So each bar now
        // reads its desk off its OWN art (Core BarDesks), and the counter is placed there — down the LEFT,
        // mid-depth, where every backdrop actually draws it. The service point (S) is the [E] spot on the
        // players' side; the counter wall sits just BEHIND it (toward the window) and the droid paces
        // behind that (see FillComplexDroids). A safe fallback keeps any unlisted bar sane.
        BarDesk desk = BarDesks.For(spec.BodyId) ?? new BarDesk(spec.BodyId, 0.26f, 0.60f, 4.5f);
        float serviceX = desk.ServiceX;
        float serviceY = HallTopY + desk.ServiceYOffset;   // mid-depth on the desk, clear of the window
        float counterY = serviceY + 1f;                     // the counter wall, one du behind the service line
        walls.Add(new(serviceX - desk.CounterHalfWidth, counterY, serviceX + desk.CounterHalfWidth, counterY, false, false)); // waist-high bar counter, on the pictured desk
        Barkeep? keep = Barkeeps.For(spec.BodyId);
        string keepLabel = keep is { } bk ? $"🍺 BARKEEP · {bk.Name}" : "🍺 BARKEEP";

        // Two locked back-room hatches off the bar — more of the place you can't get into (yet).
        char lvl = spec.Authority[0];
        doors.Add(new(BarLeft, HallTopY + 9, BarLeft, HallTopY + 13, Locked: true));
        hatches.Add(new(DeckPlan.ConsoleKind.Hatch, BarLeft + 2, HallTopY + 11, $"🔒 CELLAR · {lvl}-B1"));
        doors.Add(new(BarRight, HallTopY + 9, BarRight, HallTopY + 13, Locked: true));
        hatches.Add(new(DeckPlan.ConsoleKind.Hatch, BarRight - 2, HallTopY + 11, $"🔒 STOREROOM · {lvl}-B2"));

        // The bar's regulars, each at a table — walk up and press E. Drop the ship's ⚓ gangway.
        var consoles = new List<DeckPlan.ConsoleSpot>(ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock))
        {
            new(DeckPlan.ConsoleKind.BarPatron, -9, HallTopY + 6, "◈ ONE-EYE SILAS"),
            new(DeckPlan.ConsoleKind.BarPatron, 14, HallTopY + 6, "◈ MADAM COIL"),
            new(DeckPlan.ConsoleKind.BarPatron, 2.5f, HallTopY + 11, "◈ GILT-EYE"),
            new(DeckPlan.ConsoleKind.BarPatron, -9, HallTopY + 16, "◈ THE FIXER"), // back-corner table: confidential, off-the-books work
            // The Magpie's bar stop — a roaming patron (PR-F). They aren't always here; walk up and the
            // game reads their rota, so an empty chair means they've drifted off (bar → gone → back room).
            new(DeckPlan.ConsoleKind.BarPatron, (float)MagpieBarPost.X, (float)MagpieBarPost.Y, "◈ THE MAGPIE"),
            // #247 — the barkeep service console, ON the desk drawn in this bar's art (owner 2026-07-18,
            // "Evening wind": "the bar-keep service position … needs to be AT that desk … the bar to be on
            // top of the bar in the picture"). It sits at the desk's service point (S) — down the LEFT,
            // mid-depth — on the players' (hall-door) side of the counter wall, so the captain bellies up
            // from below and the [E] radius leans in for the house special. Kept > InteractRadius from
            // One-Eye Silas's stool (−9, HallTopY+6) so E never grabs the wrong regular.
            new(DeckPlan.ConsoleKind.Barkeep, serviceX, serviceY, keepLabel),
            // The gift shop: walk up, press E, view the Gen-AI souvenir + its location gag. Kept clear
            // of the bar patrons (Coil at x14) so E doesn't grab the wrong console.
            new(DeckPlan.ConsoleKind.ViewObject, 6, HallTopY + 3, "👕 SOUVENIR TEE", spec.TshirtArt, spec.Gag),
            new(DeckPlan.ConsoleKind.ViewObject, 9.5f, HallTopY + 3, "🧲 FRIDGE MAGNET", spec.MagnetArt,
                $"A little {spec.Name} to stick on the fridge back home."),
        };
        consoles.AddRange(hatches); // the ring departments + bar back-rooms, as knockable locked hatches

        // Seven tables spread across the big room — three taken by the regulars, four open (for a
        // stranger to drift over, later) — plus the ship's own cantina tables.
        var tables = new List<(float X, float Y)>(ship.Tables)
        {
            (-9, HallTopY + 6), (14, HallTopY + 6), (2.5f, HallTopY + 11),
            (-9, HallTopY + 16), (14, HallTopY + 16), (-3, HallTopY + 18), (8, HallTopY + 18),
        };

        var backdrops = new List<DeckPlan.Backdrop>(ship.Backdrops)
        {
            // Concourse art across the round hall — sized ~16:9 to match the image so the domed ceiling
            // isn't stretched; fills the hall's width, floor showing at the very top/bottom.
            new(spec.HallArt, HallCenterX - 16, HallCenterY + 9, 32, 18, 0.95f),
            new(spec.BarArt, BarLeft, BarTopY, BarRight - BarLeft, BarTopY - HallTopY, 0.95f),
        };

        // Weld on each active wing's geometry (Wednesday plan §3 PR-F): walls, any doors, consoles
        // (translated to deck console kinds), and floor labels. The doorway into each was already
        // carved above; here the room itself grows.
        foreach (DeckWing wing in activeWings)
        {
            foreach (WingWall w in wing.Walls)
            {
                walls.Add(new(w.X1, w.Y1, w.X2, w.Y2, w.IsWindow, w.IsHull));
            }
            foreach (WingDoor d in wing.Doors)
            {
                doors.Add(new(d.X1, d.Y1, d.X2, d.Y2, d.Locked));
            }
            foreach (WingConsole c in wing.Consoles)
            {
                consoles.Add(new(MapConsoleKind(c.Kind), c.X, c.Y, c.Label, c.ImageUrl, c.Caption));
            }
            foreach (WingLabel l in wing.Labels)
            {
                labels.Add((l.X, l.Y, l.Text));
            }
        }

        return new DeckPlan(walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(),
            spawnX: 2.5, spawnY: 6, // aboard, in the airlock corridor, facing up the tube
            droidCount: 10, fillDroids: (simTime, buffer) => FillComplexDroids(simTime, buffer, backRoomOpen, serviceX, serviceY),
            location: (x, y) => x < -14.5 && y is > 15 and < 37 ? "BONDED STORES · BACK ROOM"
                              : y > HallTopY ? spec.BarName
                              : y > HallBottomY ? $"{spec.Authority} IMMIGRATION"
                              : y > ShipHatchY ? "GANGWAY"
                              : DeckPlan.Ship.Location(x, y),
            doors: doors.ToArray(), shipFixtures: true, followCam: true, tables: tables.ToArray());
    }

    // Ship's three droids, the immigration officer, the four seated bar patrons (three regulars + The
    // Fixer), and — index 8 — the roaming Magpie, placed by their sim-time rota. Shared across every
    // station (one geometry); deterministic in sim time, stateless. When the Magpie's rota puts them
    // out of reach (gone, or the back room before it's open), they're parked far off-frame.
    private static void FillComplexDroids(double simTime, DeckPlan.Droid[] buffer, bool backRoomOpen,
        double barkeepX, double barkeepServiceY)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // fills [0..3)
        double sway = 0.05 * System.Math.Sin(simTime * 0.0009);
        buffer[3] = new DeckPlan.Droid(6.5, HallBottomY + 7, -System.Math.PI / 2, "Customs"); // officer beside the gate
        buffer[4] = new DeckPlan.Droid(-9, HallTopY + 7 + sway, -System.Math.PI / 2, "Silas");
        buffer[5] = new DeckPlan.Droid(14, HallTopY + 7 - sway, -System.Math.PI / 2, "Coil");
        buffer[6] = new DeckPlan.Droid(2.5, HallTopY + 12 + sway, -System.Math.PI / 2, "Gilt-Eye");
        buffer[7] = new DeckPlan.Droid(-9, HallTopY + 17 - sway, -System.Math.PI / 2, "The Fixer"); // back corner

        NpcPost m = ResolveMagpie(simTime, backRoomOpen);
        buffer[8] = m.Present
            ? new DeckPlan.Droid(m.X + sway, m.Y, m.FacingRad, "Magpie")
            : new DeckPlan.Droid(-9999, -9999, 0, "Magpie"); // out of reach this watch — off-frame

        // #247 — the barkeep, pacing their patch BEHIND the counter (owner: "a barkeep pacing their bar
        // area is fine"; and 2026-07-18, "Evening wind": "in all bars that have a bar-desk in their
        // graphics the barkeep is positioned behind the bar desk"). No rota (they don't leave the bar): a
        // deterministic sine sweep, the same idiom as the seated regulars' sway. Centred on THIS bar's
        // service point (BarDesks), one du further back than the counter wall — so the keep works the far
        // side of the desk drawn in the art, never the window band the first pass parked them in. Facing
        // south (−π/2), across the bar toward the captain.
        double pace = 1.5 * System.Math.Sin(simTime * 0.00035);
        buffer[9] = new DeckPlan.Droid(barkeepX + pace, barkeepServiceY + 2, -System.Math.PI / 2, "Barkeep");
    }
}
