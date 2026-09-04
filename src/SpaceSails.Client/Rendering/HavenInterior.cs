using SpaceSails.Core;
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
        // Selene Gate — the oldest port in the system, in orbit off Luna (#352, owner playtest 2026-07-18:
        // docked but "there is nothing here to walk to"). The immigration authority is LUNA (→ hatch ids
        // L-05 …), the deadpan quip customs' been-there tone, the bar the EARTHRISE off its home-in-the-
        // window backdrop. Scene art (hall + bar) and now the dedicated souvenir tee/magnet are all
        // Grok-generated — the outer havens no longer reuse their backdrops as gift-shop postcards
        // (owner 2026-07-19, browsing The Red Eye: "The eye bar has two T-shirts and no magnets :-D").
        new("selene-gate", "SELENE GATE", "LUNA", "oldest gate in the system — customs has seen it all", "THE EARTHRISE BAR",
            "art/selene-gate-hall.jpg", "art/selene-gate-bar.jpg",
            "art/souvenir-selene-tshirt.jpg", "art/souvenir-selene-magnet.jpg",
            "“I visited Luna, the oldest port in the system, and all I got was this regolith-grey T-shirt.”"),
        // The Red Eye — the storm-watcher port in orbit off Jupiter (#352 follow-through, night shift
        // 2026-07-18→19). Selene Gate closed the Luna gap; these two outer havens (#289) were the last
        // berths that docked to "nothing to walk to". Pilgrims come to stare at the Great Red Spot, so the
        // immigration authority is JUPITER (→ hatch ids J-05 …), the quip a customs stare-down, the bar THE
        // STORMWATCH BAR off its Spot-in-the-window backdrop. Grok-generated scene art (hall + bar), and now
        // a dedicated Grok souvenir tee/magnet — this is the port the owner was standing in when the
        // placeholder reuse showed (owner 2026-07-19: "The eye bar has two T-shirts and no magnets :-D";
        // the tee showed the hall backdrop and the "magnet" the bar backdrop, so nothing read as a magnet).
        new("red-eye", "THE RED EYE", "JUPITER", "the Spot doesn't blink — try to match it", "THE STORMWATCH BAR",
            "art/red-eye-hall.jpg", "art/red-eye-bar.jpg",
            "art/souvenir-redeye-tshirt.jpg", "art/souvenir-redeye-magnet.jpg",
            "“I made the pilgrimage to the Great Red Spot and all I got was this T-shirt.”"),
        // The Deep — the farthest port in the system, in orbit off Neptune (#352 follow-through, night
        // shift 2026-07-18→19). Cold, half-empty, frost on the pipes, icicles down the dome: the end of
        // every road. Immigration authority NEPTUNE (→ hatch ids N-05 …), the quip the last stamp before
        // the dark, the bar THE DEEP END off its Neptune-in-the-window backdrop. Grok-generated scene art
        // (hall + bar), and now a dedicated Grok souvenir tee/magnet — no more backdrop-as-postcard reuse
        // (owner 2026-07-19, on The Red Eye: "The eye bar has two T-shirts and no magnets :-D").
        new("the-deep", "THE DEEP", "NEPTUNE", "last port before the dark — dress warm", "THE DEEP END",
            "art/the-deep-hall.jpg", "art/the-deep-bar.jpg",
            "art/souvenir-deep-tshirt.jpg", "art/souvenir-deep-magnet.jpg",
            "“I reached the end of the system at Neptune and all I got was this frost-bitten T-shirt.”"),
    ];

    // Keyed by "bodyId|<sorted opened-hatch ids>", so the locked concourse and the wing-grown variant
    // are cached side by side and a station is still built at most once per unlock state.
    //
    // #649 · CONCURRENT, for exactly the reason MoonSurface's layout cache already is (#585): in WASM the
    // game is single-threaded and a plain Dictionary is safe, but xUnit runs test classes IN PARALLEL, so
    // two of them building haven decks at once corrupt it — "Operations that change non-concurrent
    // collections must have exclusive access." It surfaced here as TheOracleCanBeSeatedOnDemandTests failing
    // about one run in three with an InvalidOperationException that has nothing to do with the oracle, which
    // is the worst kind of failure there is: a flaky audit teaches you to ignore audits.
    //
    // Found by an unrelated change to the surface renderer shifting the timing enough to lose the race. It
    // was always there. Building a deck is deterministic, so a racing double-build is pure waste and never a
    // wrong answer — only the dictionary itself ever needed protecting.
    //
    // #1112 · …and BOUNDED, which it was not. The key carries the docking watch, and the watch advances for
    // ever: a long voyage left one built station in memory per watch, permanently, because nothing here ever
    // took one out again. MoonSurface's twin memo has had a cap and a flush since #371 and this one never
    // grew one — so the cap is not written here either. Both twins now hold the same BoundedMemo, whose whole
    // reason for existing is that a cache policy kept in two call sites is a cache policy that drifts.
    private static readonly BoundedMemo<string, DeckPlan> Cache = new(BoundedMemo.DefaultCap);

    /// <summary>#1112 · How many built stations the memo is holding, for the guard that holds it to its cap.
    /// Test-visible only — nothing in the game may care how warm a cache is.</summary>
    internal static int DeckCacheCount => Cache.Count;

    /// <summary>#1112 · …and the cap it is held to.</summary>
    internal static int DeckCacheCap => Cache.Cap;

    /// <summary>Does this haven have a walkable interior (so docking should weld on a tube)?</summary>
    public static bool HasInterior(string bodyId) => System.Array.Exists(Specs, s => s.BodyId == bodyId);

    /// <summary>Every haven that HAS a deck, so the deck audit can walk all of them rather than the ones
    /// somebody remembered to list. A test can only hold what it can enumerate.</summary>
    public static IReadOnlyList<string> InteriorBodyIds
    {
        get
        {
            var ids = new List<string>(Specs.Length);
            foreach (StationSpec spec in Specs)
            {
                ids.Add(spec.BodyId);
            }
            return ids;
        }
    }

    /// <summary>
    /// The docked complex for a body — ship + tube + hall + bar as one walkable plan — or null if that
    /// haven has no deck to walk. <paramref name="unlockedHatchIds"/> is the session's set of cracked
    /// hatch ids for this station (bare ids like "V-06"); any that grow a wing weld their back room on.
    /// <paramref name="simTime"/> is the docking watch: the seated regulars' rota (<see cref="PatronRota"/>)
    /// is resolved at this clock, so who's at the bar and which chair they took is baked for this visit —
    /// re-dock a watch later and the room reads different. Built once per (station, unlock-state, watch),
    /// lazily, and shared.
    /// </summary>
    /// <param name="forceOracle">The <c>?oracle=1</c> seat cheat (#428): plant the oracle's corner console
    /// whatever her rota says this watch. Part of the cache key — a deck built before the cheat was armed
    /// can never be handed back for a forced boot.</param>
    /// <param name="fillWalkers">#973 L0 · The page's own walker band, written into the slots after the room's
    /// seated figures — handed <c>(buffer, firstSlot)</c> on every frame the plan is drawn. Null for a deck
    /// nobody is walking across, which is every caller that only wants the geometry (and every test that has
    /// always asked for one).
    ///
    /// <para><b>A plan with a filler is NOT cached, and that is deliberate.</b> The cache is shared process-wide
    /// and xUnit runs test classes in parallel — the concurrent dictionary above exists because two of them
    /// building haven decks at once corrupted it. A delegate closed over ONE page, handed back to a second page
    /// out of a shared cache, would be one buffer written by two rooms: the named bug class, with the flakiest
    /// possible symptom. Building costs a few hundred objects and happens twice per docking, so there is
    /// nothing to save here anyway.</para></param>
    /// <param name="churn">#731 · What this evening has done to the room — who has walked out and who has come
    /// in and sat down (<see cref="RoomChurn"/>). Null, or a churn with nothing in it, is the rota's own
    /// answer. A churn that HAS something in it is part of the cache key, for the reason the watch is: two
    /// rooms with different people in them are two rooms.</param>
    /// <param name="tier">#380 item 10 · Which tube this berth earned (<see cref="ArrivalTube.TierFor"/>), so the
    /// customs desk at the immigration gate can say what the gate is for. It is a PARAMETER and not something
    /// this file works out, because the tier is derived from the scenario's traffic and this renderer has no
    /// ephemeris — passing the page's own answer in is what makes the desk and the arrival plate one reading of
    /// one berth rather than two. Null is "nobody asked": the desk is left off, which is what every caller that
    /// only wants the geometry has always got. Part of the cache key, for the reason the watch is.</param>
    public static DeckPlan? DockedDeck(string bodyId, IReadOnlySet<string>? unlockedHatchIds = null, double simTime = 0,
        bool forceOracle = false, System.Action<DeckPlan.Droid[], int>? fillWalkers = null,
        RoomChurn? churn = null, ArrivalTube.Tier? tier = null)
    {
        if (System.Array.Find(Specs, s => s.BodyId == bodyId) is not { } spec)
        {
            return null;
        }
        IReadOnlyList<DeckWing> active = unlockedHatchIds is null
            ? []
            : DeckExpansions.ActiveWings(WingCatalog(bodyId), bodyId, unlockedHatchIds).ToList();
        if (fillWalkers is not null)
        {
            return BuildComplex(spec, active, simTime, forceOracle, fillWalkers, churn, tier);
        }
        long watch = PatronRota.WatchIndex(simTime);
        string wingKey = active.Count == 0
            ? bodyId
            : bodyId + "|" + string.Join(",", active.Select(w => w.UnlockHatchId).OrderBy(s => s, System.StringComparer.Ordinal));
        string room = churn is { Anything: true } c ? "+" + c.Signature : "";
        string gate = tier is { } t ? "+" + t : "";
        string key = $"{wingKey}@{watch}{(forceOracle ? "+oracle" : "")}{room}{gate}"; // the seated-regular rota re-rolls each watch, so it keys the cache
        // #1112 · Held to a cap, and on overflow the memo starts fresh. A rebuilt deck is the deck that was
        // thrown away — every input to BuildComplex here is in the key — so an eviction costs the few hundred
        // objects of one build and nothing else.
        return Cache.GetOrBuild(key, () => BuildComplex(spec, active, simTime, forceOracle, null, churn, tier));
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

    /// <summary>Where the customs officer stands, beside the immigration gate — the droid in
    /// <see cref="FillComplexDroids"/> AND the card [E] raises at him (#380 item 10). One constant, because a
    /// figure and the console that speaks for him standing a du apart is a man talking from the next square.</summary>
    private static readonly (float X, float Y) CustomsDesk = (6.5f, HallBottomY + 7);

    // --- The bar, off the hall's north door — big and cavernous, a local-planet view along the back ---
    private const float BarLeft = -14f;
    private const float BarRight = 19f;
    private static readonly float BarTopY = HallTopY + 22f;

    // --- The wide door from the round hall INTO the bar (the hall's north edge, edge 2) --------------
    // The gap the captain walks through at the end of the ship → tube → immigration hall → bar walk.
    // Named because four things have to mean the SAME doorway: the two wall stubs either side of it on
    // the hall ring, the two bar-floor walls either side of it on the bar's south side, the auto-door
    // itself, and — since #428 — where <see cref="BarThreshold"/> stands a captain who booted ashore.
    // They agreed as five typed-in literals; two places computing one fact is the bug even then.
    private const float BarDoorLeft = -1f;
    private const float BarDoorRight = 6f;

    /// <summary>WHERE THE WALK ENDS — the position the <c>?ashore=1</c> boot cheat (#428) stands the
    /// captain at: one step past the hall's north door, the exact spot the REAL walk (ship → tube →
    /// immigration hall → this door) puts them the moment the bar becomes the room they are standing in.
    ///
    /// <para>Derived from the doorway itself — its two jambs and the hall's north edge — and never typed
    /// in. A cheat that invented its own coordinates would be a second source of truth for a fact the
    /// geometry already owns, and unaudited client geometry literals are this project's oldest and most
    /// reliably wrong bug class. The heading is <c>+Y</c>: facing into the room, the way you were walking
    /// when you came through (the deck's own convention — <c>atan2(dy, dx)</c>).</para></summary>
    public static (double X, double Y, double Heading) BarThreshold =>
        ((BarDoorLeft + BarDoorRight) / 2.0, HallTopY + AshoreStepDeckUnits, System.Math.PI / 2);

    /// <summary>How far past the door line the ashore boot stands: one avatar ACROSS, so the captain is
    /// wholly inside the room rather than straddling the door line they just crossed.</summary>
    private const double AshoreStepDeckUnits = 2 * DeckPlan.AvatarRadius;

    // ── #973 L0 · THE BAR AS A ROOM WITH A METABOLISM ────────────────────────────────────────────────────
    //
    // Owner's favourite room is The Red Eye's bar, and until this lane it was the one place in the game where
    // nobody could move: eleven droid slots that are a stateless function of sim time, no band, no doors an
    // NPC could come out of, no floor anybody but the captain walked. #731's whole beat — a regular goes out
    // through a leaf the captain's own TRY is refused at, and no line explains it — worked on a Hive canteen
    // floor and nowhere else.
    //
    // What is published below is the room's OWN geometry, verbatim, and never a second copy of it: the two
    // back-room leaves are the same records BuildComplex hangs on the wall, and the tops are the same list it
    // draws. A band computed here and a room drawn there would be this repo's oldest and most reliably wrong
    // bug class with a body walking through it.

    /// <summary>#973 L0 · The bar's seven tops, as ONE list. Consumed by <see cref="BuildComplex"/> (which
    /// draws them) and by <see cref="BarBand"/> (which walks people to them), so the room somebody crosses is
    /// the room the captain is looking at.</summary>
    private static readonly (float X, float Y)[] BarTops =
    [
        (-9f, HallTopY + 6f), (14f, HallTopY + 6f), (2.5f, HallTopY + 11f),
        (-9f, HallTopY + 16f), (14f, HallTopY + 16f), (-3f, HallTopY + 18f), (8f, HallTopY + 18f),
    ];

    /// <summary>
    /// #973 L0 · THE TWO LEAVES OFF THE BAR, as the building's own <see cref="UndergroundComplex.LockedDoor"/>
    /// — the type <see cref="Egress"/> insists on, and it insists for the reason that IS this beat: every
    /// member of that list is refused to the captain by construction, so a walker cannot be given a public
    /// door to vanish through by an oversight.
    ///
    /// <para>They are the cellar and the storeroom the bar has always had, hung on the room's two side walls
    /// (which are unbroken stone — the leaf is drawn on the wall, not cut into it), with the plate that is
    /// already painted on them. <see cref="BuildComplex"/> takes its doors and its knockable hatch consoles
    /// from this one call, so the sign a walker carries and the sign the captain reads are one string.</para>
    /// </summary>
    private static UndergroundComplex.LockedDoor[] BarBackRoomLeaves(char authority) =>
    [
        new(BarLeft, HallTopY + 9, BarLeft, HallTopY + 13, $"🔒 CELLAR · {authority}-B1"),
        new(BarRight, HallTopY + 9, BarRight, HallTopY + 13, $"🔒 STOREROOM · {authority}-B2"),
    ];

    /// <summary>
    /// #973 L0 · WHAT A DOCKED STATION'S BAR IS, TO SOMEBODY WHO HAS TO WALK ACROSS IT.
    /// </summary>
    /// <param name="BodyId">The berth, which is also the rota key — <see cref="SpaceSails.Core.NebulaRep"/>'s
    /// presence law is keyed on the BODY being visited, so a docked station needs no second kind of id.</param>
    /// <param name="FloorY">The bar's south wall. North of it is the room; the immigration hall is south. The
    /// one line that answers "is the captain in the bar", and it is the wall the room is built off rather than
    /// a threshold typed in somewhere else.</param>
    /// <param name="Doors">The leaves somebody may come out of, in the room's own order.</param>
    /// <param name="Fixtures">Where a person with nothing to do stands — the counter's service point, which is
    /// the spot this bar's own art draws its desk at (<c>BarDesks</c>) and the same one the captain bellies up
    /// to.</param>
    /// <param name="Tops">The room's tables, by their centres. Where a BODY stands at one is the caller's to
    /// sound against the stone, exactly as a canteen top's chair is.</param>
    public readonly record struct BarFloor(
        string BodyId,
        double FloorY,
        IReadOnlyList<UndergroundComplex.LockedDoor> Doors,
        IReadOnlyList<DeckReachability.Point> Fixtures,
        IReadOnlyList<DeckReachability.Point> Tops);

    /// <summary>#973 L5b · What this berth's bar is CALLED — THE STORMWATCH BAR, THE EARTHRISE, THE DEEP END.
    /// The same string the deck's own location strip reads off the spec, published because the strip's company
    /// clause needs it: a top in a station bar that announced itself as a canteen table was the sentence and
    /// the room disagreeing, and it was found by looking.</summary>
    public static string? BarNameOf(string bodyId) =>
        System.Array.Find(Specs, s => s.BodyId == bodyId) is { } spec ? spec.BarName : null;

    /// <summary>#973 L5b · How many a bar top seats. The room's own number, stated once — the sitting says it
    /// in chairs ("one of them is yours now"), and a second count anywhere would be the panel and the picture
    /// disagreeing about how alone the captain is.</summary>
    public const int BarTopSeats = 4;

    /// <summary>#973 L5b · The plate over a top nobody is at. The same shape the canteen's free top wears —
    /// the verb is TAKE THE TABLE, and the label is what the captain reads before pressing [E].</summary>
    public const string BarTopLabel = "🍸 A TOP NOBODY'S AT";

    /// <summary>
    /// #973 L5b · WHERE A BODY STANDS AT A BAR TOP — one body-width off its centre, on the first side the
    /// stone allows, hall side sounded first because that is the side somebody crossing this room comes from.
    ///
    /// <para>Published here, with the tops themselves, rather than kept private to whoever asked first. TWO
    /// callers need it and they must not disagree: the walker planning a crossing (<c>Map.BarWalkers</c>) and
    /// the seat putting the captain in a chair (<c>Seating.BarTop</c>). A second sounding would put the woman
    /// and the captain on the same square, which is the drawn room and the walked room disagreeing about a
    /// lap — this repository's third named bug class, at a table for two.</para>
    /// </summary>
    /// <returns>The place, or null when the stone allows no side of this top at all.</returns>
    /// <param name="clearOf">#973 L5b · Somebody who is already standing (or sitting) at this top, whose side
    /// is therefore taken. A top is a place for more than one body now — the captain in a chair at it and the
    /// woman who crossed the room to it — and a sounding that could not be told about the first would put the
    /// two of them on one square. Null when nobody is there yet.</param>
    public static DeckReachability.Point? BesideATop(
        DeckReachability.Point top, double radius, IReadOnlyList<SurfaceCollision.Segment> walls,
        DeckReachability.Point? clearOf = null)
    {
        double off = 2 * radius;
        (double X, double Y)[] sides =
        [
            (top.X, top.Y - off), (top.X + off, top.Y), (top.X - off, top.Y), (top.X, top.Y + off),
        ];
        foreach ((double x, double y) in sides)
        {
            if (SurfaceCollision.Blocked(x, y, radius, walls))
            {
                continue;
            }

            if (clearOf is { } taken)
            {
                double dx = x - taken.X;
                double dy = y - taken.Y;
                if ((dx * dx) + (dy * dy) < off * off)
                {
                    continue;   // that side is somebody's; a second body on it is a lap, not a table.
                }
            }

            return new DeckReachability.Point(x, y);
        }

        return null;
    }

    /// <summary>#973 L0 · The walkable band of a docked station's bar, or null at a berth with no interior to
    /// walk. Pure: it reads the same constants the room is carved from and builds nothing.</summary>
    public static BarFloor? BarBand(string bodyId)
    {
        if (System.Array.Find(Specs, s => s.BodyId == bodyId) is not { } spec)
        {
            return null;
        }

        BarDesk desk = BarDesks.For(spec.BodyId) ?? DefaultBarDesk(spec.BodyId);
        var tops = new List<DeckReachability.Point>(BarTops.Length);
        foreach ((float X, float Y) top in BarTops)
        {
            tops.Add(new DeckReachability.Point(top.X, top.Y));
        }

        return new BarFloor(
            spec.BodyId,
            HallTopY,
            BarBackRoomLeaves(spec.Authority[0]),
            [new DeckReachability.Point(desk.ServiceX, HallTopY + desk.ServiceYOffset)],
            tops);
    }

    /// <summary>The safe fallback desk for a bar whose art has not been measured — one place, so the band and
    /// the build cannot come to two different views of where the counter is.</summary>
    private static BarDesk DefaultBarDesk(string bodyId) => new(bodyId, 0.26f, 0.60f, 4.5f);

    /// <summary>#973 L0 · How many figures the docked complex draws before any walker: the ship's three, the
    /// customs officer, the four seated regulars, the Magpie, the barkeep and the oracle's corner. Named
    /// because <see cref="BuildComplex"/> hands it to the plan and the walker band is written after it, and a
    /// buffer offset that is two opinions about one number is how this game has twice thrown
    /// <c>IndexOutOfRangeException</c> at the renderer.</summary>
    public const int SeatedFigureCount = 11;

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

    // --- The roving SEATED regulars (issue #410, owner 2026-07-20 "Are the contacts moving and not in
    // same seats in same bars?"). The four regulars used to be one shared roster nailed to four fixed
    // chairs in every bar. Now each is present at a given port only SOMETIMES (PatronRota, seeded by
    // station + sim-time watch) and, when they are, takes a DIFFERENT seat from this pool. The pool is
    // authored so any assignment is safe: every seat is > InteractRadius (3 du) from the barkeep counter,
    // the Magpie's stool (8,+18), the gift-shop/poster consoles and the bar back-room hatches, and every
    // pair of seats is > 3 du apart — so E never grabs the wrong console whichever chair fills.
    private static readonly (float X, float Y)[] PatronSeats =
    [
        (-9f, HallTopY + 6f),    // 0 — near-left stool (One-Eye Silas's old perch, the barkeep-clearance case)
        (14f, HallTopY + 6f),    // 1 — near-right
        (2.5f, HallTopY + 11f),  // 2 — mid-room
        (-9f, HallTopY + 16f),   // 3 — back-left corner (the confidential, off-the-books table)
        (14f, HallTopY + 16f),   // 4 — back-right
        (-3f, HallTopY + 18f),   // 5 — back-centre-left
        (2.5f, HallTopY + 6f),   // 6 — front-centre
    ];

    /// <summary>The bar's seated-regular pool size (issue #410) — the number of chairs the rota seeds
    /// present regulars into. Exposed for tests that assert distinct, in-range seat assignment.</summary>
    public static int PatronSeatCount => PatronSeats.Length;

    /// <summary>#731 · ONE OF THE BAR'S NUMBERED CHAIRS, BY ITS INDEX — the pool above, published rather than
    /// copied.
    ///
    /// <para>Somebody who comes out of the back and takes a seat has to be WALKED to it, and a walk needs a
    /// coordinate. Measured on the page it would be a second opinion about where this bar's chairs are, which
    /// is the one kind of number a client file is never allowed to hold twice (§13.15) — so the room answers
    /// it, off the same array the rota seats people into and the deck draws them at.</para>
    ///
    /// <para>Null for an index outside the pool, which is the honest answer and never a chair invented at the
    /// origin.</para></summary>
    public static DeckReachability.Point? PatronSeatAt(int index) =>
        index < 0 || index >= PatronSeats.Length
            ? null
            : new DeckReachability.Point(PatronSeats[index].X, PatronSeats[index].Y);

    /// <summary>
    /// #731 · <b>WHAT THE WATCH HAS DONE TO THIS ROOM SINCE THE CAPTAIN WALKED INTO IT.</b>
    ///
    /// <para><b>Owner, 2026-09-01:</b> <i>"also just other customers arriving and leaving in the bars already
    /// does a lot… they can go behind doors that are locked to us."</i> The rota (<see cref="PatronRota"/>)
    /// answers who is drinking here on a WATCH, and until this lane that answer was frozen for the whole
    /// visit: a regular seated when you docked was seated when you cast off. The owner's own complaint about
    /// this room was exactly that — <i>"on the bar now they have to wait for us to leave before they can sit
    /// up… or leave the bar."</i></para>
    ///
    /// <para>This is the room's memory of the two things that can happen to it while somebody is standing in
    /// it, and it is applied INSIDE <see cref="ResolveRegulars"/> so that every reader — the consoles the [E]
    /// key finds, the figures the renderer draws, and the barkeep's own line about who is in tonight — reads
    /// one answer. A churn applied in one of those three places and not the others is the drawn room and the
    /// walked room disagreeing, which is this repository's third named bug class.</para>
    ///
    /// <para>It belongs to the PAGE, because it is a fact about one evening rather than about a watch: WHO
    /// churns and WHEN is dealt off the frozen watch and is deterministic (<see cref="Egress"/>), but whether
    /// it has happened yet is how long the captain has been standing there.</para>
    /// </summary>
    /// <param name="Left">Who has stood up and walked out. Their chair is empty and their console is gone —
    /// [E] finds nothing there, exactly as it finds nothing at an away regular's chair.</param>
    /// <param name="CameIn">…and who has come out of the back and sat down, by the chair they took. A chair
    /// nobody else holds, allotted by the caller, because a free chair is a fact about a room.</param>
    public readonly record struct RoomChurn(
        IReadOnlySet<string> Left, IReadOnlyDictionary<string, int> CameIn)
    {
        /// <summary>Has anything happened at all? A room nobody has left and nobody has come into is the room
        /// the rota already describes, so the deck may be shared out of the cache untouched.</summary>
        public bool Anything => Left.Count > 0 || CameIn.Count > 0;

        /// <summary>What tells this churn from another, for a cache key. Ordered, so two rooms with the same
        /// people in them cannot be told apart by the order somebody was added.</summary>
        public string Signature =>
            string.Join(",", Left.OrderBy(s => s, System.StringComparer.Ordinal))
            + "/"
            + string.Join(",", CameIn.OrderBy(p => p.Key, System.StringComparer.Ordinal)
                                     .Select(p => $"{p.Key}@{p.Value}"));
    }

    // --- THE STATION ORACLE (issue #425): Solenne "Static" Marsh, the ranting-drunk oracle. A bar fixture
    // present SOME watches and a drifted-off empty stool others (OracleRant.PresentAt, the #414 patron/rota
    // idiom), planted in the port-back corner of the bar — clear of every other console: > InteractRadius
    // (3 du) from the nearest patron stool (−9,+16 → 3.6 du), the barkeep desk (down the left, mid-depth),
    // the cellar hatch (−12,+11), the bar back-room door (x −14) and the spinward window (BarTopY). So E at
    // her corner never grabs the wrong console whoever else the rota seats. She wears a BarPatron console
    // (the client routes E on it to the oracle flow by matching OracleRant.Nickname, exactly as the Magpie
    // is matched) and a deck droid so she reads as a hunched figure nursing a wrong-frequency drink.
    private static readonly (float X, float Y) OracleCorner = (-11f, HallTopY + 19f);

    /// <summary>Is the oracle at this bar on this docking watch? The pure Core rota (OracleRant.PresentAt);
    /// exposed so the interaction gate and the deck build agree on whether her corner holds anyone.
    /// <paramref name="forced"/> is the <c>?oracle=1</c> seat cheat (#428) — passed straight through to Core,
    /// so the console the deck plants and the gate the E-key reads can never disagree about it.</summary>
    public static bool OraclePresent(string bodyId, double simTime, bool forced = false) =>
        SpaceSails.Core.OracleRant.PresentAt(bodyId, simTime, forced);

    /// <summary>One resolved seated regular for a bar watch: the same shout-name id the contact systems
    /// key on, whether they're at a table this watch, and — when present — the deck coords of their seat
    /// (with a per-regular seeded facing so two visits don't line up identically). Away regulars carry
    /// <see cref="Present"/> = false and are parked off-frame by the droid fill.</summary>
    /// <param name="State">WHY they are or aren't here — at a table, stepped out, or away in the back.
    /// The rota has always computed this; until the bar could SAY it, an away regular was an empty chair
    /// with no console and therefore no sentence, and the distinction lived only in Core.</param>
    public readonly record struct SeatedRegular(string Id, string Label, string ShortName, bool Present, double X, double Y, double Facing, ulong Seed, PatronState State);

    // The floating deck-label short-name per regular (the droid tag, kept as it read before #410); the
    // full shout-name id lives on the console. Unknown ids fall back to the id itself.
    private static string ShortNameFor(string id) => id switch
    {
        "ONE-EYE SILAS" => "Silas",
        "MADAM COIL" => "Coil",
        "GILT-EYE" => "Gilt-Eye",
        "THE FIXER" => "The Fixer",
        _ => id,
    };

    /// <summary>Resolve the four regulars for <paramref name="bodyId"/> at <paramref name="simTime"/> —
    /// the pure rota (<see cref="PatronRota"/>) turned into deck seats (<see cref="PatronSeats"/>). Which
    /// regulars are present, and which chair each took, is a deterministic function of the station and the
    /// sim-time watch, so the console placement, the droid fill and any interaction gate all agree.</summary>
    /// <param name="churn">#731 · What has happened to the room since the captain walked in — who has stood
    /// up and gone, and who has come out of the back and sat down. Null for the rota's own untouched answer,
    /// which is what every caller that only wants the geometry asks for.</param>
    public static IReadOnlyList<SeatedRegular> ResolveRegulars(
        string bodyId, double simTime, RoomChurn? churn = null)
    {
        var seated = new List<SeatedRegular>(PatronRota.Roster.Count);
        foreach (PatronSeating s in PatronRota.ResolveSeating(bodyId, simTime, PatronSeats.Length))
        {
            // ── #731 · THE ROOM'S OWN EVENING, over the top of the watch's own answer ──
            //
            // Applied HERE and in exactly one place, because three readers ask this question — the [E]
            // consoles, the drawn figures, and the barkeep's line about who is in tonight — and a room that
            // answered two of them would be a chair with a man drawn in it that the key finds nobody at.
            PatronState state = s.State;
            int seat = s.SeatIndex;
            if (churn is { } room)
            {
                if (room.Left.Contains(s.Regular))
                {
                    // He got up and walked out through a leaf that does not open for you. As far as this room
                    // is now concerned he has stepped out, which is the truest of the three states it has.
                    (state, seat) = (PatronState.Gone, -1);
                }
                else if (room.CameIn.TryGetValue(s.Regular, out int took))
                {
                    (state, seat) = (PatronState.AtBar, took);
                }
            }

            bool present = state == PatronState.AtBar && seat >= 0 && seat < PatronSeats.Length;
            (float sx, float sy) = present ? PatronSeats[seat] : default;
            // A seeded base facing per (regular, watch) so a returning captain doesn't find them frozen at
            // the identical angle each visit — small idle life on top of the per-frame thermal jitter.
            ulong seed = RegularSeed(s.Regular, PatronRota.WatchIndex(simTime));
            double facing = -System.Math.PI / 2 + (SpaceSails.Core.ReeverIdle.FacingTwitchAt(seed, 0) * 1.5);
            seated.Add(new SeatedRegular(s.Regular, $"◈ {s.Regular}", ShortNameFor(s.Regular), present, sx, sy, facing, seed, state));
        }
        return seated;
    }

    /// <summary>#731 · WHICH OF THE BAR'S NUMBERED CHAIRS NOBODY IS IN, on this watch as this evening has left
    /// it — in the pool's own order, so a caller allotting one to somebody walking in gets the same chair on
    /// every machine.
    ///
    /// <para>Read off <see cref="ResolveRegulars"/> rather than off the rota, so a chair whose regular has
    /// stood up and gone is free again and a chair somebody has just taken is not: the room's own answer, and
    /// never a second tally of it.</para></summary>
    public static IReadOnlyList<int> FreePatronSeats(string bodyId, double simTime, RoomChurn? churn = null)
    {
        var taken = new HashSet<int>();
        foreach (SeatedRegular r in ResolveRegulars(bodyId, simTime, churn))
        {
            if (r.Present)
            {
                taken.Add(SeatIndexOf(r));
            }
        }

        var free = new List<int>(PatronSeats.Length);
        for (int i = 0; i < PatronSeats.Length; i++)
        {
            if (!taken.Contains(i))
            {
                free.Add(i);
            }
        }
        return free;
    }

    /// <summary>Which numbered chair a present regular is in, by matching their drawn seat back to the pool —
    /// a lookup and not a second geometry. −1 for anybody the room is not seating.</summary>
    private static int SeatIndexOf(SeatedRegular r)
    {
        for (int i = 0; i < PatronSeats.Length; i++)
        {
            if (System.Math.Abs(PatronSeats[i].X - r.X) < 1e-3
                && System.Math.Abs(PatronSeats[i].Y - r.Y) < 1e-3)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>A stable per-regular jitter seed (issue #410 idle life): folds the regular's name and the
    /// watch so their thermal shuffle differs from their neighbours' and from their own last visit.</summary>
    public static ulong RegularSeed(string regular, long watch)
    {
        ulong h = 0x9E3779B97F4A7C15UL;
        foreach (char c in regular)
        {
            h = (h ^ c) * 0x100000001B3UL;
        }
        return h ^ (ulong)watch;
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

    private static DeckPlan BuildComplex(StationSpec spec, IReadOnlyList<DeckWing> activeWings, double simTime,
        bool forceOracle = false, System.Action<DeckPlan.Droid[], int>? fillWalkers = null,
        RoomChurn? churn = null, ArrivalTube.Tier? tier = null)
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
            else if (k == 2) // north edge: the wide door to the bar (gap x BarDoorLeft..BarDoorRight)
            {
                walls.Add(new(a.X, a.Y, BarDoorRight, a.Y, false, true));
                walls.Add(new(BarDoorLeft, b.Y, b.X, b.Y, false, true));
                doors.Add(new(BarDoorLeft, a.Y, BarDoorRight, a.Y)); // wide auto door
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
        // #380 item 10 — the officer at that gate is not mute any more. He stands at CustomsDesk and the
        // console that gives him his sentence goes on the same square, below, where the concourse's other
        // fixtures are hung. See ArrivalTube.CustomsLine for what he says and why it is per-tier.
        //
        // A big lobby welcome poster so you know at a glance which port you're standing in.
        labels.Add((HallCenterX, HallCenterY + 8, $"★  WELCOME TO {spec.Name}  ★"));
        labels.Add((HallCenterX, HallCenterY + 3, $"⚓ {spec.Authority} ORBIT"));

        // The bar, off the hall's north door.
        walls.Add(new(BarLeft, HallTopY, BarDoorLeft, HallTopY, false, true));   // bar floor wall, port of the door
        walls.Add(new(BarDoorRight, HallTopY, BarRight, HallTopY, false, true)); // bar floor wall, starboard of the door
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
        BarDesk desk = BarDesks.For(spec.BodyId) ?? DefaultBarDesk(spec.BodyId);
        float serviceX = desk.ServiceX;
        float serviceY = HallTopY + desk.ServiceYOffset;   // mid-depth on the desk, clear of the window
        float counterY = serviceY + 1f;                     // the counter wall, one du behind the service line
        walls.Add(new(serviceX - desk.CounterHalfWidth, counterY, serviceX + desk.CounterHalfWidth, counterY, false, false)); // waist-high bar counter, on the pictured desk
        Barkeep? keep = Barkeeps.For(spec.BodyId);
        string keepLabel = keep is { } bk ? $"🍺 BARKEEP · {bk.Name}" : "🍺 BARKEEP";

        // Two locked back-room hatches off the bar — more of the place you can't get into (yet), and since
        // #973 L0 the two leaves people come OUT of. The records are BarBackRoomLeaves' and not typed again
        // here: a walker's plate and the plate the captain is refused at are one string, by construction.
        UndergroundComplex.LockedDoor[] backRooms = BarBackRoomLeaves(spec.Authority[0]);
        foreach (UndergroundComplex.LockedDoor leaf in backRooms)
        {
            doors.Add(new((float)leaf.X1, (float)leaf.Y1, (float)leaf.X2, (float)leaf.Y2, Locked: true));
            // The knockable panel sits two du INTO the room from its leaf, on whichever side wall it hangs on.
            float inward = leaf.X1 <= BarLeft ? 2f : -2f;
            hatches.Add(new(DeckPlan.ConsoleKind.Hatch,
                (float)leaf.X1 + inward, (float)((leaf.Y1 + leaf.Y2) / 2), leaf.Sign));
        }

        // The bar's regulars (issue #410): no longer four names nailed to four fixed chairs in every bar.
        // The rota (ResolveRegulars → PatronRota) decides, for THIS station and THIS docking watch, which
        // of the four are drinking here and which chair each took — so a present regular gets a BarPatron
        // console at their seeded seat, and an absent one leaves an empty chair (no console: E finds
        // nothing, they've drifted off — opportunity/dread, not a bug). Contacts stay keyed by the ◈ label
        // id, never by seat, so the drink/rumor/pick systems work whichever chair fills. Drop the ship's ⚓.
        // …and #731's churn over the top of it: a regular who stood up and walked out of the cellar door has
        // no console at his chair any more, and one who came out of it and sat down has one at his. Asked
        // once, here, so the consoles, the droids and the barkeep's line cannot come to three views.
        IReadOnlyList<SeatedRegular> regulars = ResolveRegulars(spec.BodyId, simTime, churn);
        var consoles = new List<DeckPlan.ConsoleSpot>(ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock));
        foreach (SeatedRegular r in regulars)
        {
            if (r.Present)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.BarPatron, (float)r.X, (float)r.Y, r.Label));
            }
        }

        // The station oracle (issue #425), if she's tuned to this bar this watch. A BarPatron console in
        // the port-back corner; the client's E-router matches her by name (OracleRant.Nickname) and hands
        // off to the oracle flow, never the generic quest-giver path. Absent watches leave the stool empty.
        bool oracleHere = OraclePresent(spec.BodyId, simTime, forceOracle);
        if (oracleHere)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.BarPatron, OracleCorner.X, OracleCorner.Y,
                SpaceSails.Core.OracleRant.ConsoleLabel));
        }
        consoles.AddRange(new DeckPlan.ConsoleSpot[]
        {
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
            // The second PIRATE INSURANCE poster, in the BAR wing (#380 item 1 — the pair banked for this
            // lane). Where a spacer nurses a drink and does the grim arithmetic, Nebula Mutual pitches the
            // hard sell: "DIED BROKE? WALK IT OFF." On the starboard wall, clear of Coil's stool (x14, +6)
            // and the back-room hatch. [E] pops the poster + the sales-voice caption. Grok-generated art.
            new(DeckPlan.ConsoleKind.ViewObject, BarRight - 2.5f, HallTopY + 14, "📋 PIRATE INSURANCE",
                "art/poster-pirate-insurance-2.jpg",
                "“DIED BROKE? WALK IT OFF.” Nebula Mutual covers the clinic bill so the void doesn't keep "
                + "you — one premium, and a shot nerve or a Reever's hand is just a bad night, not the last "
                + "one. The hoards you buried outlive the hull; the policy outlives the captain. Underwritten "
                + "by Nebula Mutual — “We Bring You Back Meaner.”"),
        });
        // 📸 THE SELFIE SPOT (issue #400, owner's cruise 2026-07-20: "the awesome-view places … should
        // have a photo spot … the frame should place the CAPTAIN in the awesome view"). The scenic outer
        // havens (Red Eye storm gallery, Ringside's ring-lip, Selene's Earthrise, The Deep's edge) each get
        // a console at the bar's spinward window — walk up, press E, and the captain poses into the vista
        // with a boastful house-voice caption, filed into the legend ledger. Reuses the ViewObject/plaque
        // console idiom (#392); a dedicated kind routes E to the capture instead of the passive viewer.
        // Placed at the starboard end of the big window — clear of the barkeep desk (down the left), the
        // gift-shop consoles (x 6/9.5, +3), the STOREROOM hatch (BarRight−2, +11), and the second insurance
        // poster (BarRight−2.5, +14) — so [E] never grabs the wrong console whichever chair the rota fills.
        if (SpaceSails.Core.SelfieSpots.For(spec.BodyId) is { } selfieSpot)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.SelfieSpot, BarRight - 5, BarTopY - 2,
                selfieSpot.ConsoleLabel, selfieSpot.VistaArt));
        }

        consoles.AddRange(hatches); // the ring departments + bar back-rooms, as knockable locked hatches

        // The station's DEDICATION PLAQUE (owner's cruise ruling, 2026-07-19, photographing their ship's
        // Aker Finnyards builder's plate: "We could gen-AI the ships and docks some space-dock plaques …
        // add some depth to the world (worldbuilding)"). One addition here seeds every port — walk off
        // the tube, and it stands in the concourse on your port side, clear of the tube path (x 1..4), the
        // immigration desk, and every ring hatch. [E] pops the plate + its dedication in the house voice
        // (Core Plaques). Selene / Red Eye / Deep carry Grok plate art; the rest fall back to the text
        // alone until their easel is painted (the souvenir onerror-hide fallback idiom).
        if (Plaques.For(spec.BodyId) is { } plaque)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, HallCenterX - 6, HallCenterY - 5,
                plaque.ConsoleLabel, plaque.ArtUrl, plaque.Lore));
        }

        // The LIFEBOAT STATION (owner worldbuilding addendum, 2026-07-19: "Safety equipment is also cool.
        // Lifeboats at station maybe."). A battered muster point across the concourse from the plaque, on
        // the starboard side — clear of the tube path (x 1..4), the immigration desk, the plaque, and every
        // ring hatch. A wall label marks the muster; [E] pops the muster card (per-port stale inspection
        // date, and an asterisk that does the work). Text-only for now — the art easel is a follow-up.
        labels.Add((HallCenterX + 9, HallCenterY - 6.5f, Plaques.LifeboatLabel));
        consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, HallCenterX + 9, HallCenterY - 5,
            Plaques.LifeboatLabel, null, Plaques.LifeboatMuster(spec.BodyId)));

        // ── #380 item 10 · THE CUSTOMS DESK SAYS WHAT THE GATE IS FOR ───────────────────────────────────
        //
        // The audit's last open complaint, and it was about a PROMISE: a counter, a gate, a signed authority
        // and an officer standing at it set an expectation of being CHECKED, and the captain walked through
        // carrying whatever he liked, every time, for ever. The rule was already written down (the arrival
        // plate's ArrivalTube.WalkLine) and the sweep was already built (#537/#538) — aboard somebody else's
        // hull, never at a port's own gate. What was missing was the officer's own sentence.
        //
        // It is a ViewObject card in the plaque/lifeboat idiom, on the OFFICER'S OWN SQUARE — CustomsDesk,
        // the same constant FillComplexDroids stands him on, so the man and the card can never drift a du
        // apart. The words come from Core, per tier, out of the same switch WalkLine lives in: the desk and
        // the plate he read ninety seconds ago are one reading of one berth. No line at an outpost means no
        // console at an outpost — there is no queue and no officer there to have an opinion.
        //
        // Clearance: over an interact radius from the plaque (−3.5, 35), the lifeboat (11.5, 35), the poster,
        // the three ad plates and every ring hatch — the same rule the whole concourse is placed by, and
        // asserted at each fixture's own square in TheWallsAreHungAndReadTests.
        if (tier is { } berth && ArrivalTube.CustomsLine(berth) is { } stamped)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, CustomsDesk.X, CustomsDesk.Y,
                ArrivalTube.CustomsLabel, null, stamped));
        }

        // PIRATE INSURANCE — the Gen-AI dock poster (#380 item 1: pre-seed the brain-backup / Pirate
        // Insurance premise with port advertising, so a new player meets the fiction BEFORE the death card,
        // not on it; owner 2026-07-19: "we should explain Pirate insurance … advertisements about it as Gen
        // AI at every dockable port"). One addition here seeds all eight ports (the shared hall build, the
        // ViewObject console idiom the plaque/souvenirs use). Port-side of the concourse, above the plaque,
        // clear of the tube path (x 1..4), the immigration desk, the plaque, the lifeboat, and every ring
        // hatch. [E] pops the poster ("OUR RATES ARE A STEAL") + the sales-voice caption. Art is Grok-made.
        consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, HallCenterX - 11, HallCenterY + 6,
            "📋 PIRATE INSURANCE", "art/poster-pirate-insurance-1.jpg",
            "“OUR RATES ARE A STEAL.” Pirate Insurance from Nebula Mutual: brain-backup rebirth, a rustbucket "
            + "gassed and waiting, no awkward questions at the clinic. Die uninsured and you still wake — just "
            + "meaner and broker. Ask your dockmaster before the collectors ask about you. Underwritten by "
            + "Nebula Mutual — “We Bring You Back Meaner.”"));

        // #973 L4 · THE THREE SMALL PLATES, hung round the same concourse the poster hangs in. A text plate
        // in the poster's own idiom — no canvas, exactly as the lifeboat muster above carries none: three
        // more paintings for three one-line ads would be a pool of art bought to say very little.
        //
        // The captain reads the WHOLE of each one walking past (the label IS the advertising), and [E] gives
        // it back on a card so the words can be read twice — which matters, because the third one read is
        // the one that finishes a memory (`StationAds`). Detected by the ad's own text, so this file never
        // learns what any of them is FOR.
        //
        // NO CAPTION, and that came out of looking at the card in a browser: a caption repeating the title
        // word for word read as a stutter — the surface saying one thing twice and meaning it once. The
        // plate is one sentence; the card is that sentence held closer, and there is nothing under it.
        //
        // Placed on the northern half of the concourse, where nothing else stands: the poster and the plaque
        // are port-side and low, the lifeboat is starboard and low, the tube path is x 1..4 and southern.
        // Every one is at least 5 du from every other console on this deck, so [E] can never grab the wrong
        // fixture — the same clearance rule the second poster and the selfie spot are placed by.
        (float X, float Y)[] adSites =
        [
            (HallCenterX + 8.5f, HallCenterY + 4),
            (HallCenterX + 3, HallCenterY + 9),
            (HallCenterX - 4, HallCenterY + 8),
        ];
        for (int adIdx = 0; adIdx < adSites.Length && adIdx < SpaceSails.Core.StationAds.Ads.Count; adIdx++)
        {
            SpaceSails.Core.StationAds.Ad ad = SpaceSails.Core.StationAds.Ads[adIdx];
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, adSites[adIdx].X, adSites[adIdx].Y, ad.Label));
        }

        // Seven tables spread across the big room — the rota seats present regulars at some of them this
        // watch, the rest stand open (an empty chair = someone's drifted off) — plus the ship's cantina.
        var tables = new List<DeckPlan.TableTop>(ship.Tables);
        foreach ((float X, float Y) top in BarTops)
        {
            tables.Add(new(top.X, top.Y));
        }

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

        // ── #973 L5b · A TOP THE CAPTAIN CAN TAKE ───────────────────────────────────────────────────────
        //
        // #973 L0 found the gap and wrote it down: every one of the seven ways to open a sitting in this game
        // was gated on a SurfaceExcursion, a berth has none, and so "the bar's seven tops are drawn dressing
        // with no chairs and no console" — [E] at one answered nothing, which is an absence rather than a
        // refusal and is the one kind of no a player cannot read (#757's own lesson, in the other room).
        //
        // A console goes on every top the room has not already given to somebody: the regulars the rota
        // seated this watch, the Magpie at their stop, the oracle in her corner. Asked of the console list
        // ITSELF, after everything else is in it, so the answer cannot drift from the room — a second table
        // of who is sitting where would be this file's oldest bug class with a stranger in the captain's
        // chair. Within an interact radius of an existing console is "somebody's", because that is exactly
        // the distance at which [E] would grab the wrong one.
        foreach ((float X, float Y) top in BarTops)
        {
            bool somebodysAlready = false;
            foreach (DeckPlan.ConsoleSpot spot in consoles)
            {
                double dx = spot.X - top.X;
                double dy = spot.Y - top.Y;
                if ((dx * dx) + (dy * dy) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
                {
                    somebodysAlready = true;
                    break;
                }
            }

            if (!somebodysAlready)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.BarTop, top.X, top.Y, BarTopLabel));
            }
        }

        return new DeckPlan(walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(),
            spawnX: 2.5, spawnY: 6, // aboard, in the airlock corridor, facing up the tube
            // #973 L0 · …and the WALKER BAND after the room's own seated figures, when somebody is walking this
            // deck. The offset is stated once (SeatedFigureCount) and the width once (Egress.BandSlots); the
            // two times this game threw IndexOutOfRangeException at the renderer, it was because a band's
            // width and a buffer's length were two opinions about one number.
            droidCount: SeatedFigureCount + (fillWalkers is null ? 0 : Egress.BandSlots),
            fillDroids: (simTime, buffer) =>
            {
                FillComplexDroids(simTime, buffer, backRoomOpen, serviceX, serviceY, regulars, oracleHere);
                fillWalkers?.Invoke(buffer, SeatedFigureCount);
            },
            location: (x, y) => x < -14.5 && y is > 15 and < 37 ? "BONDED STORES · BACK ROOM"
                              : y > HallTopY ? spec.BarName
                              : y > HallBottomY ? $"{spec.Authority} IMMIGRATION"
                              : y > ShipHatchY ? "GANGWAY"
                              : DeckPlan.Ship.Location(x, y),
            doors: doors.ToArray(), shipFixtures: true, followCam: true, tables: tables.ToArray(),
            // #1040 · …AND THE SHIP'S OWN COUNTER TRAVELS WITH HER. A docked complex is her plan with a
            // station welded onto it, and her walls, doors, consoles, labels, backdrops and tops are all
            // seeded from it above. Her stool row and her counter's fill were the two she would have arrived
            // without — so the moment she clamped on, the seats [E] still answers at would have stopped
            // being drawn: the walked room and the drawn room disagreeing, which is this repository's third
            // named bug class with a bar stool under it.
            stools: ship.Stools, furniture: ship.Furniture);
    }

    // Ship's three droids, the immigration officer, the four seated bar regulars (issue #410, roved by the
    // rota — each at their seeded seat this watch, or parked off-frame when they've drifted off), and —
    // index 8 — the roaming Magpie, placed by their sim-time rota. Shared across every station (one
    // geometry); deterministic in sim time, stateless. The <paramref name="regulars"/> seating is captured
    // at build time (fixed for the visit), so the droids sit exactly where their consoles do; only the
    // thermal jitter and the Magpie/barkeep pace read the live clock.
    private static void FillComplexDroids(double simTime, DeckPlan.Droid[] buffer, bool backRoomOpen,
        double barkeepX, double barkeepServiceY, IReadOnlyList<SeatedRegular> regulars, bool oracleHere)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // fills [0..3)
        double sway = 0.05 * System.Math.Sin(simTime * 0.0009);
        buffer[3] = new DeckPlan.Droid(CustomsDesk.X, CustomsDesk.Y, -System.Math.PI / 2, "Customs"); // officer beside the gate

        // The four regulars sit at [4..8). A present one gets a tiny seeded thermal shuffle around their
        // seated anchor + a look-around facing twitch (ReeverIdle, #390) so they read alive, not carved;
        // an away one is parked far off-frame (their chair is simply empty this watch). Roster order is
        // stable, so index 4+i is the i-th regular whether or not they're here.
        for (int i = 0; i < 4; i++)
        {
            int slot = 4 + i;
            if (i < regulars.Count && regulars[i].Present)
            {
                SeatedRegular r = regulars[i];
                (double jx, double jy) = SpaceSails.Core.ReeverIdle.JitterAt(r.Seed, simTime);
                double face = r.Facing + SpaceSails.Core.ReeverIdle.FacingTwitchAt(r.Seed, simTime);
                buffer[slot] = new DeckPlan.Droid(r.X + jx, r.Y + jy, face, r.ShortName);
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, i < regulars.Count ? regulars[i].ShortName : "Regular");
            }
        }

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

        // #425 — the station oracle, hunched over her corner drink when the rota has her here this watch.
        // A seeded thermal shuffle + facing twitch (ReeverIdle) so she reads alive, muttering at the wall;
        // parked far off-frame on the watches she's drifted off (her stool simply empty, no console). Index
        // 10, the buffer's last complex slot (droidCount 11).
        if (oracleHere)
        {
            ulong oseed = RegularSeed("STATION-ORACLE", PatronRota.WatchIndex(simTime));
            (double ojx, double ojy) = SpaceSails.Core.ReeverIdle.JitterAt(oseed, simTime);
            double oface = -System.Math.PI / 2 + SpaceSails.Core.ReeverIdle.FacingTwitchAt(oseed, simTime);
            buffer[10] = new DeckPlan.Droid(OracleCorner.X + ojx, OracleCorner.Y + ojy, oface, "Oracle");
        }
        else
        {
            buffer[10] = new DeckPlan.Droid(-9999, -9999, 0, "Oracle");
        }
    }
}
