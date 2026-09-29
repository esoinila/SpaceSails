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
/// <remarks><para>Split at 1,225 lines (#251) into <c>HavenInterior.Wings</c> and
/// <c>HavenInterior.Build</c>. The cut is not where the concerns are — it is where the STATIC FIELD
/// INITIALIZERS allow. Almost every coordinate in this class is measured off <c>HallTopY</c>, which is a
/// <c>static readonly</c> (it is <c>Math.Cos</c> of the hall's apothem, so it cannot be a <c>const</c>);
/// the bar's tops, the patron seats, the oracle's corner and the Magpie's posts are all written as
/// <c>HallTopY + n</c>. Static initializers of a partial class run in the order the compiler READS THE
/// FILES, not the order a reader sees, so moving those declarations into a partial that sorts before this
/// one initialises them against <c>HallTopY == 0</c> and quietly builds a station whose furniture is
/// stacked on the hall floor. That was measured here, not guessed: splitting this file by concern moved
/// 33 pinned frames and reddened 14 guards. So everything that declares a static field stays in this file,
/// in its original order — the catalogue, the memo, the shape of a station, the bar and its people — and
/// only what declares none was carved off.</para></remarks>
public static partial class HavenInterior
{
    /// <summary>One walkable station: which body, what it's called, and its themed dressing.</summary>
    /// <param name="Lower">#1253 · The level under the concourse, or null at a station that has only the one
    /// floor — which was every haven in the game but Selene Gate until #1332 A gave every hub one. NULLABLE and last, so <see cref="Specs"/>
    /// is untouched for the six that do not have one and a station grows a basement by gaining a field
    /// rather than by anything else in this file learning about floors.</param>
    private sealed record StationSpec(
        string BodyId, string Name, string Authority, string Quip, string BarName,
        string HallArt, string BarArt, string TshirtArt, string MagnetArt, string Gag,
        LowerSpec? Lower = null);

    /// <summary>
    /// #1253 · <b>WHAT A STATION'S LOWER LEVEL IS DRESSED IN.</b> The twin of the four art/name fields above,
    /// for the floor that has one — and deliberately the same SHAPE, because a level is a room with a name, a
    /// plate and a picture, exactly as the hall and the bar are.
    /// </summary>
    /// <param name="Name">What the floor calls itself on the one label it carries.</param>
    /// <param name="Plate">What is painted on every door down there — the inspectorate line, and the only
    /// sentence the building says about the place.</param>
    /// <param name="Art">The backdrop laid across the service corridor, in the concourse's own grammar (a
    /// canvas over a rectangle, at an alpha). ONE picture for the level: the hall gets one and the bar gets
    /// one, and a floor whose whole content is a corridor does not need two.</param>
    /// <param name="Edges">#1332 A · Which three edges of the ring carry the cars, or null for Selene Gate's
    /// proven three (<see cref="CageEdges"/>). Only a station that has already spent one of those edges on
    /// something else names its own — Cinder Roost, whose V-06 is the Bonded Stores hatch the Magpie's back
    /// room grows behind.</param>
    private sealed record LowerSpec(string Name, string Plate, string Art, int[]? Edges = null);

    /// <summary>#1332 A · The one backdrop every lower level is laid under: Selene Gate's service-level art,
    /// reused — no new art this slice. A <c>const</c>, so it is folded rather than initialised and no file order
    /// can reach it early.</summary>
    private const string TheServiceLevelArt = "art/selene-service-level.jpg";

    // ── #1332 A · EVERY HUB IS A LOBBY ────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-09-29: "The big round immigration points already look like elevator lobbies, so we might as
    // well have those hubs have elevators that take to apartment-hotel-like, usually locked, spaces down
    // below." Every station below carries a LowerSpec now, in Selene Gate's proven shape — the same cabin
    // block, the same corridor, the same three cars — and differs only in the one plate its floor carries
    // (Fable canon, in HavenLevels) and, at Cinder Roost, in one car's edge.
    //
    // The grey-market docks with walkable interiors, each themed to its world (vision par. 8). Gag =
    // the T-shirt one-liner (owner's "every place has a gift shop" joke).
    private static readonly StationSpec[] Specs =
    [
        new("the-space-bar", "THE RUSTY ROADSTEAD", "MARS", "most guests stay two weeks", "THE ROADSTEAD BAR",
            "art/the-rusty-roadstead-lobby.jpg", "art/the-roadstead-bar.jpg",
            "art/souvenir-roadstead-tshirt.jpg", "art/souvenir-roadstead-magnet.jpg",
            "“I visited Mars and all I got was this rusty T-shirt.”",
            new LowerSpec(HavenLevels.SpaceBarPlate, HavenLevels.SpaceBarPlate, TheServiceLevelArt)),
        new("cinder-roost", "CINDER ROOST", "VENUS", "mind the sulphur, spacer", "THE CINDER LOUNGE",
            "art/cinder-roost-hall.jpg", "art/cinder-roost-bar.jpg",
            "art/souvenir-cinder-tshirt.jpg", "art/souvenir-cinder-magnet.jpg",
            "“I visited Venus and all I got was this lousy T-shirt.”",
            new LowerSpec(HavenLevels.CinderRoostPlate, HavenLevels.CinderRoostPlate, TheServiceLevelArt, Edges: [CageEdgeNorthEast, CageEdgeWest, CageEdgeEast])),
        new("ringside-exchange", "RINGSIDE EXCHANGE", "SATURN", "trade fast — the rings don't wait", "THE RINGSIDE BAR",
            "art/ringside-hall.jpg", "art/ringside-bar.jpg",
            "art/souvenir-ringside-tshirt.jpg", "art/souvenir-ringside-magnet.jpg",
            "“I went all the way to Saturn and all I got was this T-shirt.”",
            new LowerSpec(HavenLevels.RingsidePlate, HavenLevels.RingsidePlate, TheServiceLevelArt)),
        new("the-tilt", "THE TILT", "URANUS", "everything's sideways out here", "THE TILT BAR",
            "art/the-tilt-hall.jpg", "art/the-tilt-bar.jpg",
            "art/souvenir-tilt-tshirt.jpg", "art/souvenir-tilt-magnet.jpg",
            "“I went to Uranus for the proctologist — they were fully booked.”",
            new LowerSpec(HavenLevels.TiltPlate, HavenLevels.TiltPlate, TheServiceLevelArt)),
        // Selene Gate — the oldest port in the system, in orbit off Luna (#352, owner playtest 2026-07-18:
        // docked but "there is nothing here to walk to"). The immigration authority is LUNA (→ hatch ids
        // L-05 …), the deadpan quip customs' been-there tone, the bar the EARTHRISE off its home-in-the-
        // window backdrop. Scene art (hall + bar) and now the dedicated souvenir tee/magnet are all
        // Grok-generated — the outer havens no longer reuse their backdrops as gift-shop postcards
        // (owner 2026-07-19, browsing The Red Eye: "The eye bar has two T-shirts and no magnets :-D").
        //
        // #1253 · …and the oldest port in the system is the one with a FLOOR UNDER IT. Owner, 2026-09-20:
        // "could we add a basement level to the observation deck station, so the tailing task could start
        // from the basement cabin and end at the observation deck?" Selene Gate is the station with the most
        // built on it — the walk, the T, the tables, the tail — so it is the one that gets the basement, and
        // the other six are untouched by construction: they carry no LowerSpec, so nothing below asks them
        // anything. What is down there is a service corridor, a row of crew cabins whose doors do not open
        // for a captain, and three cages up to the hall.
        //
        // #1332 A · …and since 2026-09-29 the other six have the same floor in the same shape (see the note
        // above the catalogue): Selene Gate keeps its LOWER CONCOURSE and its SERVICE LEVEL — NO PUBLIC
        // ACCESS, and its concourse frame did not move.
        new("selene-gate", "SELENE GATE", "LUNA", "oldest gate in the system — customs has seen it all", "THE EARTHRISE BAR",
            "art/selene-gate-hall.jpg", "art/selene-gate-bar.jpg",
            "art/souvenir-selene-tshirt.jpg", "art/souvenir-selene-magnet.jpg",
            "“I visited Luna, the oldest port in the system, and all I got was this regolith-grey T-shirt.”",
            new LowerSpec(
                HavenLevels.LowerConcoursePlate, HavenLevels.NoPublicAccessPlate,
                TheServiceLevelArt)),
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
            "“I made the pilgrimage to the Great Red Spot and all I got was this T-shirt.”",
            new LowerSpec(HavenLevels.RedEyePlate, HavenLevels.RedEyePlate, TheServiceLevelArt)),
        // The Deep — the farthest port in the system, in orbit off Neptune (#352 follow-through, night
        // shift 2026-07-18→19). Cold, half-empty, frost on the pipes, icicles down the dome: the end of
        // every road. Immigration authority NEPTUNE (→ hatch ids N-05 …), the quip the last stamp before
        // the dark, the bar THE DEEP END off its Neptune-in-the-window backdrop. Grok-generated scene art
        // (hall + bar), and now a dedicated Grok souvenir tee/magnet — no more backdrop-as-postcard reuse
        // (owner 2026-07-19, on The Red Eye: "The eye bar has two T-shirts and no magnets :-D").
        new("the-deep", "THE DEEP", "NEPTUNE", "last port before the dark — dress warm", "THE DEEP END",
            "art/the-deep-hall.jpg", "art/the-deep-bar.jpg",
            "art/souvenir-deep-tshirt.jpg", "art/souvenir-deep-magnet.jpg",
            "“I reached the end of the system at Neptune and all I got was this frost-bitten T-shirt.”",
            new LowerSpec(HavenLevels.DeepPlate, HavenLevels.DeepPlate, TheServiceLevelArt)),
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

    // ── #1199 · THE OBSERVATION WALK ─────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-09-13: "even better if we preclude the cliché hidden door by having that place be like an
    // observation tube (a Grand Canyon walk on top of the cliff with a transparent floor) in some space
    // station, with only one entry / exit."
    //
    // A straight tube out from the concourse, due west, over the drop. WHICH haven has it is Core's
    // (ObservationWalk.HavenId) and how long it is is Core's (ObservationWalk.LengthDu) — this file owns only
    // the room's place on the ring, which is a fact about this hall's geometry and about nothing else.
    //
    // WHY EDGE 5. Edge k faces (30 + 30k)°, so 5 is due west: the only free compass point left after the
    // tube took south (8) and the bar took north (2), and the one that gives a tube whose walls are axis
    // aligned — a straight walk, drawn straight, with a rail square across the blind end. It costs the
    // station one sealed department panel (the ring deals MEDBAY to this edge), and the sealed-edge counter
    // is still stepped over it below so that every OTHER edge on every station keeps the tag and the hatch id
    // it already had. The oldest port in the system gave up a department to put a window where its people
    // could look at home; that is the sort of thing a station does and the sort of thing it never mentions.

    /// <summary>#1199 · Which edge of the hall ring the walk opens off. Edge <c>k</c> faces
    /// <c>(30 + 30k)°</c>; 5 is due west.</summary>
    private const int ObservationWalkEdge = 5;

    /// <summary>#1199 · The walk's own numbers, worked out ONCE off the hall ring: the two jambs of its one
    /// doorway and the x of its blind end. The walls the renderer draws, the plate the map layer reads, the
    /// floor the art is laid on and the band a walker is plotted down all come from here, so the room the
    /// captain walks and the room he is looking at cannot become two rooms — this repository's third named
    /// bug class, which on this deck would be a body strolling through a rail.</summary>
    private static readonly (float MouthX, float NorthJambY, float SouthJambY, float BlindX) TheWalk =
        MeasureTheObservationWalk();

    // ── #1199 (2026-09-18) · THE GALLERY — THE CROSSBAR ──────────────────────────────────────────────────
    //
    // Owner, live: "a wider place at the far end of the observation place… a tube, then an area to view…
    // like the letter T — now we have the foot of the letter ready."
    //
    // The stem is the tube above, unchanged. The crossbar is measured off the SAME numbers: the tube's own
    // blind x becomes the gallery's back wall, and the room grows due west from it, square across the tube's
    // axis and centred on it. HOW WIDE and HOW DEEP are Core's (ObservationWalk.GalleryWidthDu /
    // GalleryDepthDu); WHERE is this file's, because it is a fact about this hall's geometry and nothing
    // else. Nothing here is typed in — unaudited client geometry literals are this project's oldest and
    // most reliably wrong bug class, and a room measured twice is a room that will eventually be two rooms.

    /// <summary>#1199 · The gallery's own box — <c>(west, south, east, north)</c>. The east face is the
    /// tube's blind x, which is why the crossbar and the stem cannot drift apart; the other three are
    /// Core's two dimensions laid off it and the axis.</summary>
    private static readonly (float WestX, float SouthY, float EastX, float NorthY) TheGallery =
        MeasureTheGallery();

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

    // --- THE STATION ORACLE (issue #425): Solenne "Static" Marsh, the ranting-drunk oracle. A bar fixture
    // present SOME watches and a drifted-off empty stool others (OracleRant.PresentAt, the #414 patron/rota
    // idiom), planted in the port-back corner of the bar — clear of every other console: > InteractRadius
    // (3 du) from the nearest patron stool (−9,+16 → 3.6 du), the barkeep desk (down the left, mid-depth),
    // the cellar hatch (−12,+11), the bar back-room door (x −14) and the spinward window (BarTopY). So E at
    // her corner never grabs the wrong console whoever else the rota seats. She wears a BarPatron console
    // (the client routes E on it to the oracle flow by matching OracleRant.Nickname, exactly as the Magpie
    // is matched) and a deck droid so she reads as a hunched figure nursing a wrong-frequency drink.
    private static readonly (float X, float Y) OracleCorner = (-11f, HallTopY + 19f);
}
