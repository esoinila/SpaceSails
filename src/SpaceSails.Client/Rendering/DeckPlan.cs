using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// #251 · WHY THIS FILE WAS CUT, and what "pure motion" is holding across the family.
//
// At 1,394 lines this was the longest hand-written file in `src/` and the file that set the size gate's
// daylight (`NoSourceFileIsTooLongTests`, the line at 1,500) — 106 lines of room for a class that gains a
// console kind, a seat record or a room every few weeks. The next block anybody added here would have had
// to shove an unrelated one out first, which is a file telling you where its seams are.
//
// So it is cut along the seams it already had — one subject per partial, every line moved VERBATIM: no
// rename, no signature change, no member reordered, no statement moved inside a method. A `partial` is the
// same class, so the roster of fields, the constructor's assignments and every caller are untouched by
// construction, and the deck is drawn into every FrameHash in the suite: had one row of one ledger moved,
// the plan would have changed.
//
// THE FAMILY (1,373 body lines → six files, largest 658):
//   · DeckPlan.cs             — what a deck is MADE of and how a body moves through it: the wall, door,
//                               console, backdrop, structure and furniture vocabulary, the reach and
//                               clearance constants, the plan's own arrays and constructor, and the four
//                               questions asked of the stone — Move, Collides, NearestConsoleSpot, CastRay.
//   · DeckPlan.ConsoleKind.cs — one kind per verb: every press the game can offer at a fixture.
//   · DeckPlan.Seating.cs     — the tops, chairs, stools and bench ends, every field handed down.
//   · DeckPlan.Regions.cs     — #371's append, and the one removal that answers it: a plan that GROWS.
//   · DeckPlan.Ship.cs        — her own deck, the one plan in the game that is written out by hand.
//   · DeckPlan.Barriers.cs    — #1099/#442's question: is this doorway walled up, asked of the one list.
//
// WHAT STAYED HERE, and why it is not "the leftovers". Three source guards read THIS PATH and assert a
// literal in it, and the seams were chosen around them so that no test file had to be edited:
//
//   · AJambIsNotASealedDoorTests            — sweeps all of `src/` for `Gait.Person` and requires one of
//                                             the nine claims to be `DeckPlan.cs:`. That is `Move`, the
//                                             one place in the game the captain's body is stepped.
//   · TheDossierCardCarriesItsOwnSayingsTests — `string? Outcome`, on the `ConsoleSpot` record.
//   · TheParkBenchIsAGumshoeMoveTests       — the `Droid` record's whole parameter list, `Held` included.
//
// Those three pin the captain's step, the console record and the figure record to this path, and they are
// the same subject anyway: this file is the language a deck is written in, and the five beside it are
// things written in it.

/// <summary>
/// A walkable interior — the single source of truth for every interior view (the top-down deck
/// plan, and any future isometric mode). Deck units (du), origin midships, +X bow, +Y port.
///
/// Once a hardcoded static ship singleton; **now a selectable plan** (go-ashore, 2026-07-07):
/// the renderer and the avatar loop take a <see cref="DeckPlan"/> by reference, so the
/// ship is one plan (<see cref="Ship"/>) and a haven interior (see <c>HavenInterior</c>) is
/// another. Everything downstream — collision, console interaction, the room labels — works
/// unchanged against whichever plan is active.
///
/// The ship, bow to stern: bridge (helm + nav post) → cantina with a panoramic hull window
/// (port) / three cabins + a space HEAD 🚽 (starboard) → midship corridor → shuttle bay (port,
/// where the boarding-droid infantry is stationed) / cargo hold (starboard) → engine room.
/// </summary>
public sealed partial class DeckPlan
{
    /// <summary>A wall segment. <paramref name="IsHull"/> draws it as pressure hull — bright and thick,
    /// the readable boundary of a made thing; anything else draws as a dimmer inner line.
    ///
    /// <para>#563 · <paramref name="Unseen"/> is a wall the eye NEVER sees: it collides exactly like every
    /// other wall, but nothing is drawn for it. Owner, 2026-07-31: <i>"The space ships come with outside
    /// borders but the landing site out-doors should not... at least not obviously so with square area"</i>,
    /// and <i>"if our space has limits for some technical reasons then let's not advertise it, more like
    /// hide that fact."</i> A hull IS a real boundary and drawing it as one is right — that treatment stays
    /// on ships. The open regolith has no such object, so the field's envelope must not borrow the ship's
    /// ink. An airless moon has no atmosphere to scatter light: ground the lamp never reaches is simply
    /// black, and the field does not END so much as stop being visible.</para>
    ///
    /// <para>The deeper reason to hide it (owner, same session): <i>"the reevers and supply line are kind
    /// of the invisible tether to players distance"</i>. The real edge of a landing site is the point where
    /// the magazine and the pack behind you say turn around — the #453 law that how deep you dare go is
    /// priced by sentries and nerve, NOT by geometry. A drawn rectangle competes with that tether and wins,
    /// announcing the wrong limit before the honest one can be felt.</para></summary>
    /// <param name="IsStone">#563 · Solid mass that is NOT a made pressure boundary — a monolith, a
    /// plinth, a mass-driver muzzle, an ancient spur. It draws heavy like hull, because it IS solid, but in
    /// rock rather than metal.
    ///
    /// <para>Owner's complaint was that a landing site looks artificial, and removing the rim fence only got
    /// half of it: the body's own geography flags its solid objects with <c>IsHull</c>, and on Miranda's
    /// Ridge Camp that is 16 of 25 segments drawn in the ship's cold pressure-hull stroke. The flag was
    /// never wrong — a monolith IS solid and a fallen span is not — it was the INK that was wrong. Same
    /// distinction, different material.</para></param>
    /// <param name="IsSeamless">#677 · THE THIRD IDIOM. Not a made pressure boundary and not a body's stone
    /// — a continuous, unbroken surface that carries no texture, no joints and no palette at all.
    ///
    /// <para>The game has exactly two wall materials and both of them say who built the thing: hull ink is
    /// poured, welded, bolted and paid for; stone ink is the body's own rock in the body's own colour. The
    /// found halls (#677) are neither, so they are drawn as neither. <b>The absence of texture IS the
    /// style</b> — owner's ruling: <i>"it is just built into the smooth monolith style walls"</i> — which on
    /// a crude top-down grid reads exactly as wrong as it should, because every other wall in the game
    /// belongs to somebody.</para>
    ///
    /// <para>It takes NO ink from <see cref="DeckPlan.HullInk"/> or <see cref="DeckPlan.StoneInk"/>, and that
    /// refusal is the load-bearing half: a palette is a fact about a moon or a department, and either one
    /// applied here would quietly answer the question the whole feature exists to leave open. The precedent
    /// is #649's slab, which says <i>no seam</i> by having no interior line-work rather than by being
    /// labelled.</para></param>
    public readonly record struct Wall(
        float X1, float Y1, float X2, float Y2, bool IsWindow, bool IsHull, bool Unseen = false,
        bool IsStone = false, bool IsSeamless = false);

    /// <summary>An airlock door across a passage. An automatic door slides open as the avatar nears
    /// (a top-down flourish; it never blocks — the passage is always walkable). A <c>Locked</c> door
    /// stays shut and is drawn cold — it marks another berth's sealed hatch, decoration only, and is
    /// backed by a real wall so you can't pass.
    ///
    /// <para>#462 · <paramref name="Interlock"/> groups doors into an AIRLOCK. Owner, 2026-07-27: "The idea
    /// is that only one door in a tube is open at a time… think of airlock" — "both doors being open at the
    /// same time defeats the purpose (an abstraction though at this case)". Doors sharing a non-zero group
    /// take turns: only the one nearest the captain may stand open, so the far end is ALWAYS drawn shut.
    /// That is what finally gives the Old Ones a visible thing to stop at (#462) — the outer door closing
    /// behind you as the inner opens — and it is why a tailgater ends up shut in the tube with the built-in
    /// gun (#461) rather than following you aboard. 0 = an ordinary door with no partner.</para></summary>
    /// <summary>#465 · How near the captain must be for an auto-door to retract. Lives HERE, not in the
    /// renderer, because the same number now decides two things that must never disagree: what is DRAWN open
    /// and what is transparent to sight and gunfire. A door the player sees shut must stop a round.</summary>
    public const double DoorOpenRadius = 4.0;

    /// <param name="Imported">#592 · This door was not made here. Owner: <i>"some special color not
    /// distinctive to the site could then used to draw our attention to a place (like expensive door made
    /// with far away imported materials)."</i> Every ordinary hatch is drawn in its world's own stone, so the
    /// one that is not becomes a sentence — somebody shipped materials across the system to seal this, and
    /// nobody does that for a store cupboard.</param>
    /// <param name="Machined">#606 · Not merely off-palette — a different KIND of object. Owner, on hiding
    /// the lift head in an ordinary hut: <i>"The expensive doors would be the clue."</i>
    ///
    /// <para>Colour alone had already failed once (#585): violet marks shelters, about one ruin hatch in
    /// seven, and the way down, so it identified nothing. A tell that has to survive being one of three
    /// things has to be readable as SHAPE. This one is drawn heavy, with a second inner rail and its frame
    /// picked out at the jambs — a machined pressure door in a wall of piled regolith, next to hatches that
    /// are a single thin stroke. It still opens: sealed is what it looks like, not what it does.</para></param>
    public readonly record struct Door(
        float X1, float Y1, float X2, float Y2, bool Locked = false, int Interlock = 0,
        bool Imported = false, bool Machined = false);

    /// <summary>An interaction point on the deck. A <see cref="ConsoleKind.ViewObject"/> spot also
    /// carries an <paramref name="ImageUrl"/> and <paramref name="Caption"/> — press E and the game
    /// pops up that Gen-AI image (a souvenir, a lore prop) with its caption.</summary>
    /// <param name="Outcome">#774 · What the act that raised this card actually FOUND — the reveal card's
    /// own field (#736), grown here because the surface's full-screen cards are these ones. The caption is
    /// the fiction and this is the record: the name that came out of the kit, the family who are waiting,
    /// the moon somebody named. It is a REGION and not a slot, which is the whole of #774 — an event with
    /// four things to say in one breath says all four, instead of writing three of them into a backdrop
    /// nobody can read and letting append order pick the survivor (the contract #693 killed).</param>
    /// <param name="Run">#791 · THE FIXTURE'S REACH ALONG ITSELF, as the SEGMENT it actually is — so the
    /// interaction point is a line rather than a dot, and <see cref="NearestConsoleSpot"/> measures to the
    /// nearest point on it.
    ///
    /// <para>Owner, live at the B1 bar: <i>"The Bar desk is really long now, but there is only one spot to
    /// get service on it… we would need an E-bus of the bar desk length instead of one bar keep cashier at a
    /// single spot."</i> The desk is eighty-odd deck units and the press reached six of them.</para>
    ///
    /// <para><b>Null everywhere else, which is every console in the game but this one.</b> A helm is a chair
    /// and a valve is a wheel; they are points and they stay points — a fixture with no run is its own
    /// endpoint, so the distance below is the distance it always was. Nothing about any other deck changes,
    /// and no caller has to learn a new idea to keep working.</para>
    ///
    /// <para>#827 · <b>It is the segment and not a half-span off (X, Y), because the two are not concentric
    /// any more.</b> A counter's PLATE stands in front of the desk, on the square a body can stand on and
    /// every walkability audit walks to; the desk's own front FACE is a step behind it, and the face is what
    /// you order over. While the run was a span about the plate, the deck drew a cyan service rail two du
    /// clear of the bar's own photograph and the owner read the picture as the counter — because the picture
    /// WAS the counter.</para>
    ///
    /// <para>The segment is HANDED DOWN from whoever carved the fixture (for the counter: the hall's own
    /// <c>Service</c> run, which is its desk's front face). A renderer that worked out how long a bar is
    /// would be doing geometry about a room it did not carve, which is §13.15 and the reason this project
    /// has twice put a captain in a wall.</para></param>
    public readonly record struct ConsoleSpot(ConsoleKind Kind, float X, float Y, string Label,
        string? ImageUrl = null, string? Caption = null, string? Outcome = null,
        (float X0, float Y0, float X1, float Y1)? Run = null)
    {
        /// <summary>#791 · Is this fixture a RUN rather than a point? Asked, rather than compared against
        /// zero at four call sites.</summary>
        public bool IsRun => Run.HasValue;

        /// <summary>#791 · One end of the run — (X, Y) itself where the fixture is a point.</summary>
        public (float X, float Y) End0 => Run is { } r ? (r.X0, r.Y0) : (X, Y);

        /// <summary>#791 · The other end.</summary>
        public (float X, float Y) End1 => Run is { } r ? (r.X1, r.Y1) : (X, Y);

        /// <summary>#791 · How far this spot is from the fixture — to the nearest point ON it, which for a
        /// point console is the point itself. <see cref="SurfaceCollision.DistanceToSegment"/>, so the reach
        /// the key uses, the reach the pen draws and the reach a guard measures are one function.</summary>
        public double DistanceFrom(double x, double y)
        {
            (float ax, float ay) = End0;
            (float bx, float by) = End1;
            return SurfaceCollision.DistanceToSegment(x, y, ax, ay, bx, by);
        }

        /// <summary>#791 · The point on the fixture nearest a captain — where the [E] prompt is drawn, so a
        /// captain at the far end of a long desk sees the offer beside THEM rather than forty du away at the
        /// plate. Clamped to the segment, which for a point console is the point.</summary>
        public (float X, float Y) NearestPointTo(double x, double y)
        {
            if (Run is not { } r)
            {
                return (X, Y);
            }
            double ex = r.X1 - r.X0, ey = r.Y1 - r.Y0;
            double len2 = (ex * ex) + (ey * ey);
            if (len2 <= 0)
            {
                return (r.X0, r.Y0);
            }
            double t = Math.Clamp((((x - r.X0) * ex) + ((y - r.Y0) * ey)) / len2, 0.0, 1.0);
            return ((float)(r.X0 + (t * ex)), (float)(r.Y0 + (t * ey)));
        }
    }

    /// <summary>A room backdrop image: top-left at (X, Y) in deck units, W×H deck units, drawn
    /// under the vector overlay. The top-down renderer walks these.</summary>
    public readonly record struct Backdrop(string Url, float X, float Y, float W, float H, float Alpha);

    /// <summary>
    /// #537 · A RECTANGLE OF SOLID SHIP. Shielding bands, bulkhead runs, machinery spaces — anything that is
    /// STRUCTURE rather than room, drawn filled so it cannot be mistaken for somewhere you could be.
    ///
    /// <para>Owner: <i>"if we can see into them from the hall then they don't hide anything."</i> A narrow gap
    /// left black reads as a space, and a hiding place drawn as a space is not a hiding place — the whole
    /// knock-on-the-walls search was readable off the map until these were filled.</para>
    /// </summary>
    public readonly record struct Structure(float X0, float Y0, float X1, float Y1);

    /// <summary>
    /// #868 · ONE PIECE OF FURNITURE, AS THE RECTANGLE IT STANDS ON — filled, so a captain can see it.
    ///
    /// <para>Owner, reading a back-of-house room off the plan on 2026-08-13: <i>"The graphics kind of does
    /// not show there being a table"</i> · <i>"The bench is a line"</i> · and, as the positive control three
    /// paces away, <i>"The Shelving is clear as furniture goes."</i> His own fix, quoted: <i>"could the table
    /// just be a different color rectangle in front of the chair, so arms (and papers) could rest on
    /// it?"</i>, sealed with <i>"I think table should be similar just say table."</i></para>
    ///
    /// <para><b>Why the deck needed a new primitive at all.</b> Until this record, every fixture down here
    /// reached the screen as its own SOLIDS — the wall segments Core lays a box out of — so a rectangle drew
    /// as four strokes and a bench, which is one degenerate box, drew as one. That is honest about the
    /// collision field and useless as furniture: an outline reads as a space you could stand in, which is
    /// exactly the complaint #537 answered for structure with a fill. This is the same answer for the things
    /// standing IN a room rather than the things a room is made of, and it is drawn from the box Core
    /// published rather than measured off the wall list, so the picture and the plan cannot drift.</para>
    /// </summary>
    /// <param name="X0">Left edge, in deck units.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Tone">What the piece IS, never a colour — <see cref="BigLabels"/>'s own rule, because
    /// the plan is Core-shaped data and the ink lives in the renderer. 0 = a surface you work at (a desk
    /// bank, a table, a counter, a worktop), 1 = something you keep things in (shelving, a kitchenette),
    /// 2 = something you sit on (a bench).</param>
    public readonly record struct FurnitureSpot(float X0, float Y0, float X1, float Y1, int Tone)
    {
        /// <summary>#868 · Which tone a published fitting draws in. Asked of the KIND and never of the
        /// plate's wording, so a fixture renamed tomorrow keeps its ink — and asked here, once, so the pen
        /// and any guard about the pen read one answer.</summary>
        public static int ToneOf(SpaceSails.Core.RingOffice.Fitting kind) => kind switch
        {
            SpaceSails.Core.RingOffice.Fitting.Shelving
                or SpaceSails.Core.RingOffice.Fitting.Racking
                or SpaceSails.Core.RingOffice.Fitting.FilingCabinet
                or SpaceSails.Core.RingOffice.Fitting.Kitchenette
                // #828 · The secure disposal is a MACHINE you stand at rather than a surface you work on,
                // so it takes the kitchenette's ink and nobody reads it as a spare desk.
                or SpaceSails.Core.RingOffice.Fitting.SecureDisposal => 1,
            SpaceSails.Core.RingOffice.Fitting.Bench => 2,
            _ => 0,
        };

        /// <summary>#868 · Is this the kind of fitting that gets FILLED at all? A cubicle and a booth are
        /// little ROOMS — a captain steps inside one and shuts the door — so filling them would draw the one
        /// hiding place in the building as a solid block. A partition is a screen, which IS a line and is
        /// honestly drawn as one.</summary>
        public static bool IsFurniture(SpaceSails.Core.RingOffice.Fitting kind) =>
            kind is not (SpaceSails.Core.RingOffice.Fitting.Cubicle
                or SpaceSails.Core.RingOffice.Fitting.Booth
                or SpaceSails.Core.RingOffice.Fitting.Partition);
    }

    public const double InteractRadius = 3.0;
    public const double AvatarRadius = 0.7;

    /// <summary>
    /// HOW FAR APART TWO CONSOLES MUST STAND FOR THEIR LABELS TO BE SEPARATELY READABLE.
    ///
    /// <para>The deck audit's own law — <c>ConsoleCrowdingTests.NoTwoConsolesShareADoorstep</c>, which the
    /// owner enforced by eye twice before it went into CI (<i>"see the two crowded consoles at the back of
    /// our ship... add a CI check to catch all such cases"</i>). Well under twice the interact radius on
    /// purpose: a dense bridge (helm, nav post, scope, three desks) is a deliberate design and must stay
    /// legal; what must not happen is two dots on top of each other.</para>
    ///
    /// <para>#1016 · IT IS A CONSTANT HERE RATHER THAN IN THE TEST because a plan now has to be able to obey
    /// it while it is being BUILT: the ship's cantina publishes a takeable-top console on every drawn top
    /// that is far enough from the room's existing fixtures, and one of the three is not. A threshold typed
    /// into the builder beside the one in the audit is two numbers for one law, which is §13.15 with a
    /// console on it.</para>
    /// </summary>
    public const double LabelClearance = 2.0;

    /// <summary>
    /// #1040 · <b>MAY A NEW CONSOLE STAND HERE?</b> — <see cref="LabelClearance"/>'s own question, asked as a
    /// function instead of being spelled out inside whichever builder needed it.
    ///
    /// <para>#1016 wrote this loop inline in <c>BuildShip</c> to decide which of her cantina tops could carry
    /// a seat, and its guard proved the law was not selecting everything by relying on the room happening to
    /// have one crowded top. That is an anti-vacuity proof made of an accident: re-plan the room and the
    /// guard silently stops proving anything, which is this repository's <i>green test that asserts nothing</i>
    /// class. Named here, the law can be driven both ways by a test on any room at all.</para>
    /// </summary>
    /// <param name="consoles">Everything already on the deck — asked of the list ITSELF, after everything
    /// else is in it, so a fixture that moves tomorrow is re-judged tomorrow.</param>
    public static bool ALabelFitsAt(IEnumerable<ConsoleSpot> consoles, double x, double y)
    {
        foreach (ConsoleSpot spot in consoles)
        {
            double dx = spot.X - x, dy = spot.Y - y;
            if ((dx * dx) + (dy * dy) < LabelClearance * LabelClearance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The SHUTTLE-BAY HATCH on the ship's bottom hull (#295): the wild-side threshold the
    /// down-tube mates to, mirroring the top airlock hatch that mates the station tube. The bare ship
    /// seals it; a surface excursion (see <c>MoonSurface</c>) carves it open and grows the tube below.
    /// The crew-only-door law lives here: Reevers may chase to this line but never cross it.</summary>
    public const float ShuttleHatchY = -10f;
    /// <summary>
    /// #537 · HER OWN SHIELDING, at the derelicts' spec. Owner: <i>"Our own ship should match these specs
    /// also. 👍😎"</i> Read from <c>WreckLayout</c> rather than re-typed, so the ship you own and the ships you
    /// rob can never drift apart on a number that is supposed to be the same number.
    /// </summary>
    public const float ShieldingDepth = SpaceSails.Core.WreckLayout.ShieldingDepth;

    public const float ShuttleHatchX1 = -9f;
    public const float ShuttleHatchX2 = -5f;

    /// <summary>Upper bound on droids in any one plan — the render buffers size to this. Bumped to 9
    /// for the docked complex's roaming NPC (PR-F: a station patron on a sim-time rota, index 8), then
    /// to 10 for the bar's barkeep pacing behind the counter (#247, index 9). Lane-1 (owner, 2026-07-18):
    /// the surface tide needs room for the 3 crew + the engine ceiling on live Reevers (24), so the
    /// buffer grows to 27 — only the surface plan ever fills that far; the ship/complex still fill ≤10.
    /// #583: and four more for a repo crew that lands on the same ground (CollectorLanding.PartySize is
    /// clamped to 4), which is a DIFFERENT kind of figure sharing the same buffer — 31.
    /// #538: and three more for the black-ops sweep team, which is a third kind — 34.
    ///
    /// <para>#633 · The two branches each raised this for their own band and each was left short by the
    /// other's: 31 here, 30 on `main`. Sized now for ALL FOUR bands (3 + 24 + 4 + 3), which is what
    /// <c>Map.Surface.SurfaceDroidCount</c> computes and what <c>FillSurfaceDroids</c> writes. A buffer that
    /// is one band short does not throw — it silently draws nobody, which is how this class of bug survives
    /// a merge.</para>
    ///
    /// <para>#804 · And two more for the ROUNDS on the Hive's restricted floors — a fifth kind of figure,
    /// and the first the underground has ever had. All FIVE bands: 3 + 24 + 4 + 3 + 2 = 36.</para>
    ///
    /// <para>#731 · And two more again for the WALKERS — the people who are not on anybody's payroll: a
    /// regular finishing a drink and going, and the one who comes out of a door to sit at your table
    /// (<c>Egress.MostAtOnce</c>). SIX bands: 3 + 24 + 4 + 3 + 2 + 2 = 38.</para>
    ///
    /// <para><b>And this time it DID throw</b>, which is worth writing down beside the paragraph above that
    /// says it would not. <c>Map.Surface.SurfaceDroidCount</c> grew to 38 and this stayed at 36, and
    /// <c>DeckView.DrawTheFigures</c> walked <c>plan.DroidCount</c> over a 36-long buffer: fifteen frame
    /// fingerprints went red with an <c>IndexOutOfRangeException</c> on the regolith, in a chair and on a Hive
    /// floor alike. The mirrored constant is the same bug it has always been; what changed since #633 is that
    /// the renderer now trusts the plan's own count instead of its buffer's length, so the silent version of
    /// it has become the loud one.</para>
    ///
    /// <para><b>#973 L2 · AND IT THREW AGAIN, THE SAME WAY, FOR THE SAME REASON.</b> The walker band grew by
    /// one — the Nebula rep needs a slot of his own, because <c>Egress.MostAtOnce</c> is a law about the
    /// room's REGULARS and he is not one of them — so <c>SurfaceDroidCount</c> went to 39 while this stayed
    /// at 38, and the same fifteen fingerprints came back with the same <c>IndexOutOfRangeException</c> on
    /// the regolith, in a chair and on a Hive floor. Seven bands now: 3 + 24 + 4 + 3 + 2 + 3 = 39. The
    /// paragraph above is the whole lesson and it earned itself twice; anyone widening a band again should
    /// expect to be back here.</para></summary>
    public const int MaxDroids = 39;

    /// <summary>One figure on the deck that is not the captain.
    ///
    /// <para>#793 · <paramref name="Held"/> — this one has STOPPED BECAUSE THE CAPTAIN SAT DOWN, which is the
    /// answer a park bench is for (<see cref="SpaceSails.Core.FootTail.MustHold"/>). It is handed down from
    /// the sim per frame like every other field on this record: the pen has never heard of a tail and must
    /// not work one out, which is #788's one-reach lesson pointed at somebody else's feet.</para>
    ///
    /// <para>#832 · <paramref name="Smeared"/> — this one is at the far end of what the captain's eye can
    /// do, and is drawn as a DISTANT FIGURE: the silhouette, softened, with no name written over it. The
    /// tier is the sim's own answer (<see cref="SpaceSails.Core.PatrolBeat.SightingFor"/>), handed down for
    /// the same reason every other field here is — the alternative is a renderer with an opinion about how
    /// far a person is visible, which is a second answer to a question Core already owns.</para></summary>
    public readonly record struct Droid(
        double X, double Y, double FacingRad, string Name, bool Held = false, bool Smeared = false);

    public Wall[] Walls { get; private set; }

    /// <summary>PR-324 · The walls as bare collidable/opaque segments — the single source the captain's
    /// avatar, the surface Reevers (<c>ReeverChase</c>), and the crude line-of-sight check all share, so
    /// the maze is law for everyone. Built once with the plan; both movers obey the same lines.
    /// #371 Phase 3: an <see cref="AppendRegion"/> grows this array by ONLY the appended walls' segments —
    /// the existing entries keep their indices and values, so a live append never disturbs the geometry
    /// already on the ground.</summary>
    public SurfaceCollision.Segment[] CollisionSegments { get; private set; }

    /// <summary>#448 · the SAME walls, filed into a coarse grid (<see cref="SurfaceCollision.WallIndex"/>).
    /// Reads as an ordinary segment list and answers exactly what <see cref="CollisionSegments"/> answers —
    /// it just gets there without measuring the whole ground for every step, sightline and shot. This is
    /// what the per-frame surface step hands the collision primitives, so the frame's cost stops scaling
    /// with how much stone a landing site happened to seed (owner, live 2026-07-26: the shuttle ride timing
    /// out twice). Rebuilt whenever the walls change — a fresh plan, or an <see cref="AppendRegion"/>.</summary>
    public SurfaceCollision.WallIndex CollisionField { get; private set; }

    public ConsoleSpot[] Consoles { get; private set; }
    public (float X, float Y, string Text)[] RoomLabels { get; private set; }

    /// <summary>#600 · Signage PAINTED ON THE STRUCTURE — drawn several times the size of a room
    /// label, the way a real facility marks a stairwell or a car-park level.
    ///
    /// <para>Owner, riding between floors that are built from the same bones: <i>"something different
    /// in every floor so we visually spot some difference when we go to different floors"</i> —
    /// and, for what it should say, <i>"we can use seriously large numbers there :-D"</i> ...
    /// <i>"or depths (in meters)"</i>.</para>
    ///
    /// <para>#612 · <c>Tone</c> is what the sign MEANS, never a colour — the plan is Core-shaped data and
    /// the ink lives in the renderer. 0 = painted signage (the depth, the department), 1 = you can breathe
    /// here (a floor that holds pressure, or a #608 refuge cut into one that does not), 2 = you cannot and
    /// your tank is running. Owner, on the first cut of the plate: <i>"they are kind of hidden now"</i> /
    /// <i>"the meters and the floor name could be yellow here"</i> — which the renderer answers with a
    /// backing plate as well as brighter ink, because text on a busy deck needs a background and not merely
    /// a louder colour.</para></summary>
    public (float X, float Y, string Text, float Px, int Tone)[] BigLabels { get; private set; } = [];
    public Backdrop[] Backdrops { get; private set; }

    /// <summary>Filled structure — see <see cref="Structure"/>. Drawn under everything else, because it is what
    /// the ship is made of rather than something in her.</summary>
    public Structure[] Structures { get; private set; }

    /// <summary>#868 · The FURNITURE, as filled rectangles — see <see cref="FurnitureSpot"/>. Drawn over the
    /// floor and under the walls, because it is what is standing IN a room rather than what the room is made
    /// of. Empty everywhere Core furnishes nothing, which is every deck in the game but the Hive's.</summary>
    public FurnitureSpot[] Furniture { get; private set; }
    /// <summary>#563 slice 2 · Settable for the same reason <see cref="Walls"/> is: a live plan GROWS. A
    /// tile welded on at a crossing brings its own buildings, and a building without its doorway is the
    /// #573 report ("shelter like spaces that were just missing the services and the doors") re-shipped one
    /// tile out.</summary>
    public Door[] Doors { get; private set; }

    /// <summary>#563 · TERRAIN — drawn, never collided. Kept in its own array rather than as a flag on
    /// <see cref="Wall"/> ON PURPOSE: <see cref="CollisionSegments"/> is derived from <c>Walls</c> in the
    /// constructor, so a decorative "wall" would obstruct the captain the moment any caller forgot to
    /// filter it. Scenery cannot be given substance by an oversight, because the movement code is never
    /// handed it at all.</summary>
    public SpaceSails.Core.SurfaceScenery.Mark[] Scenery { get; private set; }

    /// <summary>#589 · The ink this ground's in-situ stonework is drawn in. Owner, touring the rebuilt
    /// sites: <i>"the in-situ construction materials of the walls might be planet specific ... red for mars
    /// etc theming"</i> / <i>"gray for Moon"</i> / <i>"something to spot where we are visually"</i>.
    ///
    /// <para>It is the plan's property rather than the renderer's constant because it is a fact about a
    /// WORLD — you build out of what is under your boots — and the renderer only happens to draw it. Null
    /// on the ship and the stations, which are made of steel like everything else in the fleet.</para></summary>
    /// <summary>#605 · The ink this deck's MADE structure draws in — poured, welded, bolted things, as
    /// opposed to a body's stonework (<see cref="StoneInk"/>). Null everywhere it has always been null: the
    /// ship, the stations and the wrecks are steel and keep the standard hull line.
    ///
    /// <para>The Hive uses it to carry a floor's DEPARTMENT LIVERY, so two floors cut from identical bones
    /// are told apart at a glance by what they were for.</para></summary>
    public SpaceSails.Core.BodyPalette.Ink? HullInk { get; private set; }

    public SpaceSails.Core.BodyPalette.Ink? StoneInk { get; private set; }

    /// <summary>#592 · The ink an ORDINARY door is drawn in on this ground — the local stone, brightened.
    /// Null on the ship and the stations, which are steel and keep the amber airlock look.</summary>
    public SpaceSails.Core.BodyPalette.Ink? DoorInk { get; private set; }

    /// <summary>#371 Phase 3 · how many regions have been appended to this live plan (0 on a freshly-built
    /// plan). A cheap handle for the perf test — "segment count grows only by the region's walls" — and for
    /// any caller that wants to know the world has grown.</summary>
    public int AppendedRegionCount { get; private set; }

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
        TableTop[]? tables = null,
        SpaceSails.Core.SurfaceScenery.Mark[]? scenery = null,
        SpaceSails.Core.BodyPalette.Ink? stoneInk = null,
        SpaceSails.Core.BodyPalette.Ink? doorInk = null,
        (float X, float Y, string Text, float Px, int Tone)[]? bigLabels = null,
        SpaceSails.Core.BodyPalette.Ink? hullInk = null,
        Structure[]? structures = null,
        StoolSpot[]? stools = null,
        BenchSpot[]? benchSeats = null,
        FurnitureSpot[]? furniture = null)
    {
        Structures = structures ?? [];
        Furniture = furniture ?? [];
        Walls = walls;
        CollisionSegments = new SurfaceCollision.Segment[walls.Length];
        for (int i = 0; i < walls.Length; i++)
        {
            CollisionSegments[i] = new SurfaceCollision.Segment(walls[i].X1, walls[i].Y1, walls[i].X2, walls[i].Y2);
        }
        CollisionField = SurfaceCollision.WallIndex.Build(CollisionSegments);
        Consoles = consoles;
        RoomLabels = roomLabels;
        BigLabels = bigLabels ?? [];
        HullInk = hullInk;
        Backdrops = backdrops;
        Doors = doors ?? [];
        Tables = tables ?? [];
        Stools = stools ?? [];
        BenchSeats = benchSeats ?? [];
        Scenery = scenery ?? [];
        StoneInk = stoneInk;
        DoorInk = doorInk;
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

    /// <summary>The room label for a deck position — the name a room says when you are asked where
    /// you are (the ashore/boot guards read it, and the generators hand it down per wing).</summary>
    public string Location(double x, double y) => _location(x, y);

    // --- Collision ---

    /// <summary>The captain's step, and it is now literally the Core one.
    ///
    /// <para>#724 · This method used to RE-TYPE <see cref="SurfaceCollision.Slide"/> line for line — the same
    /// axis-separated bump-and-slide, spelled out a second time in the client. Two copies of a law are one
    /// law and one bug waiting: the boots and the Old Ones would have parted company the moment either copy
    /// was touched, and the doorway fix is exactly such a touch. So the duplicate is gone and the avatar
    /// walks by the same primitive every other mover on this ground already obeyed.</para>
    ///
    /// <para>#724 · <see cref="SurfaceCollision.Gait.Person"/> is stated here, ONCE, and it is the only
    /// place in the game the captain's body is stepped. Everything they do on foot arrives through this
    /// method — the WASD walk and (#738) every sub-step of a clicked AutoWalk
    /// route — so the funnel is theirs on all three without any of the three having to know it exists. The
    /// Old Ones step through the same primitive and hand it <see cref="SurfaceCollision.Gait.Stagger"/>, per
    /// the owner's ruling. The difference between the boots and the shamble is this one word.</para>
    /// </summary>
    public (double X, double Y) Move(double x, double y, double dx, double dy) =>
        SurfaceCollision.Slide(x, y, dx, dy, AvatarRadius, CollisionField, SurfaceCollision.Gait.Person);

    /// <summary>PR-324 · The avatar's own collision is the shared Core check (<see cref="SurfaceCollision"/>),
    /// the very same one the surface Reevers obey — one wall law for everyone on the walked ground. Public
    /// since #724 folded <see cref="Move"/> into Core: this is the only thing left in the client that names
    /// the captain's body against the deck's stone, and the deck audit asks it by name.</summary>
    public bool Collides(double x, double y) => SurfaceCollision.Blocked(x, y, AvatarRadius, CollisionField);

    public static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2) =>
        SurfaceCollision.DistanceToSegment(px, py, x1, y1, x2, y2);

    public ConsoleKind NearestConsole(double x, double y) => NearestConsoleSpot(x, y)?.Kind ?? ConsoleKind.None;

    /// <summary>
    /// The nearest interactable console within reach, or null — lets a caller read the specific spot's
    /// label (e.g. which bar patron you walked up to), not just its kind.
    ///
    /// <para>IT NOW ACTUALLY MEANS NEAREST. For as long as this existed it returned the FIRST console in
    /// array order within the interact radius, which is a different function with the same name — and the
    /// reason the owner could not open her atmosphere board: <i>"those panels won't open there"</i>. Her
    /// valves and her bridge repeater are APPENDED to the console list (they are built per-compartment,
    /// after the hand-written spots), while the charge dump and the COMMS SEAT sit early in it. So every
    /// point from which the valves were reachable also reached an earlier console, and the earlier console
    /// took the key. Both of her boards were unpressable, on a deck where both are drawn.</para>
    ///
    /// <para>Array order is a build detail. It must never decide what the captain is standing at — with the
    /// true nearest, a console is reached by walking to it, which is the only rule a player can see. Ties
    /// keep array order so the answer stays deterministic (and <c>ConsoleCrowdingTests</c> proves no console
    /// is left with nowhere to be reached from).</para>
    /// </summary>
    public ConsoleSpot? NearestConsoleSpot(double x, double y)
    {
        ConsoleSpot? best = null;
        double bestDistance = double.MaxValue;

        foreach (ConsoleSpot c in Consoles)
        {
            // #791 · TO THE NEAREST POINT ON THE FIXTURE, which for every point console in the game is the
            // point and therefore is the very number this line computed before. What changed is that a
            // fixture may now BE eighty deck units long (the B1 bar desk), and a captain standing at one
            // end of it is standing at it.
            double d = c.DistanceFrom(x, y);
            if (d <= InteractRadius && d < bestDistance)
            {
                bestDistance = d;
                best = c;
            }
        }

        return best;
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

}
