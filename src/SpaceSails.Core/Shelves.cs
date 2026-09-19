using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #701 · THE LIBRARY LAYER — the per-occupant shelves, and the half of the odd book that was never built.
///
/// <para>Owner's morning expansion, 2026-08-05: <i>"They have their work books and they have their freetime
/// books there... provide soft clues about what kind of people stay in those rooms."</i> So a room's shelves
/// are seeded from its OCCUPANT: <b>the work shelf says what they did, the freetime shelf says who they
/// were.</b> Three pieces make a person (§12.3) — a shelf is piece-material and never a dossier, and no
/// shelf anywhere names anybody.</para>
///
/// <h3>The engine, which is never on screen</h3>
///
/// <para><see cref="OddBooks"/>' engine, one scale down. The facility runs a books-as-intelligence function:
/// staff who know they are told nothing, reading EVERYTHING, sifting for leaks about the before-worlds. The
/// odd book is what that department left in a room nobody works in any more; <b>this</b> is what the people
/// it employed kept on their own walls. <b>The department read all of it and found nothing, and that fact is
/// nowhere stated and everywhere present.</b></para>
///
/// <h3>THE OCCUPANCY RULE — derived, never stored, never named</h3>
///
/// <para>The repo has no occupant concept and this file does not invent one. There is no name, no record, no
/// roster: there is a question, <see cref="Occupied"/>, and it is answered out of the ground the building
/// already publishes. A room is somebody's when it is <b>a room somebody was given</b>:</para>
///
/// <list type="number">
/// <item><b>A chamber — or the one room the department that reads everything was actually given.</b>
/// <see cref="UndergroundComplex.RoomKind.Chamber"/> is the module the building is made of: the room off a
/// rib, with a door and a plate. A hall is a venue, a cabinet is a booking, a cubicle and a cell are
/// plumbing, a meeting room is a room a DEPARTMENT books, and a ring suite is rank — none of them is a room
/// one person sat in every day. The single exception is <c>PRIVILEGED RECORDS · READING ROOM</c>, a
/// park-view suite in the block's own register (<see cref="UndergroundComplex.ParkViewPlates"/>), which is
/// the audit's answer to the catalog's <i>"if such a room exists"</i>: it does, it is a suite and not a
/// chamber, and it is the one room on this ground whose plate IS the engine behind the feature.</item>
/// <item><b>With a plate on it.</b> A gallery in the band nobody dug carries none (#677) and never did: it
/// was not labelled because nobody ever worked there, and a paperback down there would be the most
/// explaining object in the game — see <see cref="OddBooks.ShelvesStandHere"/>, which this asks rather than
/// re-deciding.</item>
/// <item><b>That is not the empty store.</b> The owner's own escape hatch (<see cref="ChamberFitting.IsEmptyStore"/>):
/// a store that says it is empty is empty, shelves included.</item>
/// <item><b>Whose TRADE is one somebody stands in.</b> Read off the ladder the furniture is already dealt by
/// (<see cref="ChamberFitting.KitFor"/>), so a room's shelves and a room's benches can never disagree about
/// what is done in it. <see cref="ChamberFitting.Kit.Store"/> is stock and <see cref="ChamberFitting.Kit.None"/>
/// is nothing — <b>a storeroom has no occupant and therefore no shelves</b>, which is the rule saying out
/// loud what the owner's brief predicted it would.</item>
/// </list>
///
/// <para>Deterministic per (site, floor, room) with no dice on the occupancy itself: whether a room is
/// somebody's is a fact about the plate and the department, not a roll. The FREETIME shelf is the one seeded
/// thing here, and it is seeded per floor rather than per room — see <see cref="DealOn"/>.</para>
///
/// <h3>Why there is no second reader</h3>
///
/// <para>A shelf is read exactly as the odd book is read: a fixture, an [E], a card in the #528 caption-only
/// idiom, and a gist the casebook learns once per game-thread. The read-list is the odd book's own
/// (<c>Vault.Progress.OddBooksRead</c>) and the ids are namespaced so the two can never collide. A room may
/// hold both — the odd book is one in six of the rooms nobody works in, and these are the rooms somebody
/// did.</para>
/// </summary>
public static class Shelves
{
    /// <summary>The glyph the shelf line and the casebook entry both carry.</summary>
    public const string Glyph = "\U0001F4DA";

    /// <summary>
    /// WHOSE ROOM THIS IS, said as a trade and never as a person. Six posts, one per authored work shelf.
    ///
    /// <para><see cref="None"/> is not a person with no books: it is a room with nobody in it, and a room
    /// with nobody in it has no shelves at all.</para>
    /// </summary>
    public enum Post
    {
        /// <summary>Nobody. A storeroom, a gallery, the store that says it is empty.</summary>
        None,

        /// <summary>The plant floors and the rooms plated POWER or PLANT — the people who keep it running.
        /// The department livery has called them <i>engineering rust</i> since #605.</summary>
        Engineering,

        /// <summary>ISOLATION, and every room plated in the clinic's own register.</summary>
        Clinic,

        /// <summary>ADMINISTRATION, ARCHIVE, and the clerks of a depot, a transit station and the head
        /// office. The commonest post in the building, which is the honest arithmetic of a place whose
        /// horror is administrative.</summary>
        Records,

        /// <summary>LABORATORIES, and the assay and calibration rooms of the band nobody listed.</summary>
        Lab,

        /// <summary>The rooms whose plate is about WHO COMES THROUGH THE DOOR — the only place in this
        /// building where somebody's job is the door itself. See <see cref="IsAPost"/>.</summary>
        Security,

        /// <summary>The department that reads everything, where the block gave it a room with a view.
        /// <c>PRIVILEGED RECORDS · READING ROOM</c> and nowhere else in the game.</summary>
        ReadingRoom,
    }

    /// <summary>One authored shelf. <see cref="Shelf"/> is what the room shows, <see cref="Card"/> is what
    /// [E] reads, <see cref="Gist"/> is what the casebook keeps.
    ///
    /// <para>All three are AUTHORED TEXT, lifted verbatim from #701's library-layer catalog. Nothing in this
    /// file may reword them; the framing glyph on the shelf line is house prose and the authored fragment
    /// inside it is asserted present character-for-character by the guards.</para>
    ///
    /// <para><see cref="Id"/> is what the read-list stores — short, stable, namespaced against the odd
    /// book's own ids, and never shown. Renaming one re-files a shelf a captain has already read, so they
    /// are as fixed as the prose.</para></summary>
    public readonly record struct Entry(string Id, string Shelf, string Card, string Gist);

    // ── THE WORK SHELVES · what the occupant DID ──────────────────────────────────────────────────────

    /// <summary>THE AUTHORED WORK CATALOG — one per <see cref="Post"/>, verbatim.</summary>
    public static IReadOnlyList<Entry> Work { get; } =
    [
        new Entry("work:engineering",
            "a university standard in its twenty-seventh edition, the spine cracked at the chapter on " +
            "transfer orbits",
            "Twenty-seven editions. The chapter that falls open is the one on getting from one orbit to " +
            "another cheaply. Somebody needed it often.",
            "the engineer's shelf — a twenty-seventh edition, opened always at the same chapter"),

        new Entry("work:clinic",
            "a pharmacopoeia with a hospital's stamp inside the cover, the dosages pencilled over in a " +
            "smaller hand",
            "The stamp is a hospital's, somewhere with weather. The dosages have been changed in pencil, " +
            "all of them downward, in a hand that was sure.",
            "the clinic's shelf — a hospital's book, every dose pencilled down"),

        new Entry("work:records",
            "a binder of filing conventions, three revisions deep, every revision initialled",
            "Three revisions of how to file things, each initialled by the same person. The third one is " +
            "shorter than the first.",
            "the clerk's shelf — three revisions of how to file, the last the shortest"),

        new Entry("work:lab",
            "a bench manual on cold storage, the tables of hold-times worn to grey",
            "A manual on keeping things cold for a long time. The tables are worn where a thumb ran down " +
            "them, looking for one number.",
            "the lab's shelf — a cold-storage manual, thumbed at one column"),

        new Entry("work:security",
            "a patrol manual, unopened, and a paperback under it that has been opened a great deal",
            "The manual has never been read. The paperback under it has been read to pieces.",
            "the guard's shelf — the manual unread, the paperback read to pieces"),

        new Entry("work:reading-room",
            "catalogue cards in a language nobody here was born speaking",
            "Cards, thousands, in a hand-drawn script. Whoever catalogued this did not learn the alphabet " +
            "here.",
            "the reading room — catalogue cards in a borrowed alphabet"),
    ];

    // ── THE FREETIME SHELVES · who the occupant WAS ───────────────────────────────────────────────────

    /// <summary>THE AUTHORED FREETIME CATALOG — seven personas, verbatim, in the catalog's own order.
    ///
    /// <para>The order is part of the contract: it is what <see cref="DealOn"/> permutes and what the
    /// <c>?shelf=</c> cheat names, so reordering this list renames every row in the testing guide.</para></summary>
    public static IReadOnlyList<Entry> Freetime { get; } =
    [
        new Entry("free:ships-with-opinions",
            "far-future paperbacks with cracked spines, the kind where the ships have names and opinions",
            "Ships with names, ships with opinions, ships that outlive everyone aboard. Somebody down here " +
            "read these for comfort.",
            "freetime — paperbacks where the ships have opinions"),

        new Entry("free:chess-problems",
            "a book of chess problems, every one solved in pencil, the last one not",
            "Every problem solved, in pencil, in order. The last one has a single move written and then " +
            "nothing.",
            "freetime — chess problems, the last one unfinished"),

        new Entry("free:bird-guide",
            "a field guide to birds of a coast nobody here has seen",
            "Birds, by colour and call, of a coast with tides. Somebody kept it where they could reach it.",
            "freetime — a bird guide for a coast with tides"),

        new Entry("free:cookbook",
            "a cookbook, and no kitchen on this floor",
            "Recipes for a kitchen that is not on this floor, or on any floor. The pages for bread are the " +
            "dirtiest.",
            "freetime — a cookbook on a floor with no kitchen"),

        new Entry("free:one-poem",
            "poetry in a small edition, one page dog-eared so often it is soft",
            "A small book, one page folded and unfolded until the corner is cloth. It is not a long poem.",
            "freetime — one poem, folded soft"),

        new Entry("free:childs-primer",
            "a child's primer, kept where nobody would have to explain it",
            "Letters and animals. It is on the low shelf, behind the others.",
            "freetime — a child's primer, behind the others"),

        new Entry("free:collision-book",
            "the collision book — a real title on the spine and nobody left to argue with",
            "A book that says the planets used to be somewhere else, and the old stories remember it. It " +
            "has been argued with in the margins, and then the arguing stops.",
            "freetime — the collision book, argued with and then not"),
    ];

    /// <summary>How many personas there are. Named so the deal, the guards and the testing guide all read
    /// one number rather than a 7 typed in four places.</summary>
    public static int Personas => Freetime.Count;

    /// <summary>The work shelf for a post. Throws on <see cref="Post.None"/>, deliberately: a caller asking
    /// what is on nobody's shelf has skipped <see cref="Occupied"/>, and a quiet empty here would be a room
    /// with half a library in it.</summary>
    public static Entry WorkShelf(Post post) => post switch
    {
        Post.Engineering => Work[0],
        Post.Clinic => Work[1],
        Post.Records => Work[2],
        Post.Lab => Work[3],
        Post.Security => Work[4],
        Post.ReadingRoom => Work[5],
        _ => throw new ArgumentOutOfRangeException(
            nameof(post), post, "nobody works in this room, so nothing is on its shelf"),
    };

    // ── WHOSE ROOM IS THIS ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #701 · IS THIS A ROOM WHOSE PLATE IS ABOUT WHO COMES THROUGH THE DOOR?
    ///
    /// <para>The audit's own finding, said in a predicate. <b>The Hive has no SECURITY department and no
    /// guard room</b> — the departments are the eight in <see cref="UndergroundComplex.Departments"/> and
    /// none of them is one, and the twenty-four head-office plates name no post either. What the building
    /// does have, in four of its six door registers, is the plate that ADMITS: <c>DO NOT ADMIT
    /// UNESCORTED</c>, <c>AUDIT — NO ADMITTANCE</c>, <c>CONTINUITY — AUTHORISED ONLY</c>, <c>OUTBOUND —
    /// AUTHORISED ONLY</c>. Those are the only rooms in this building whose sign is about a PERSON standing
    /// at the door rather than about the work behind it, and the guard's shelf is the one that belongs in
    /// them. A patrol manual nobody opened, in the room whose whole plate is a refusal.</para>
    ///
    /// <para>Asked of the plate and never of the department, so it survives the band nobody listed — where
    /// there is no department at all and the plate is the only thing in the building still talking.</para>
    /// </summary>
    public static bool IsAPost(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return plate.Contains("ADMIT", StringComparison.Ordinal)
            || plate.Contains("ADMITTANCE", StringComparison.Ordinal)
            || plate.Contains("AUTHORISED ONLY", StringComparison.Ordinal);
    }

    /// <summary>#701 · Is this the one room the department that reads everything was actually given? The
    /// block's own park-view register carries it (<c>PRIVILEGED RECORDS · READING ROOM</c>,
    /// <see cref="UndergroundComplex.ParkViewPlates"/>) and nothing else in the game does — so the reading
    /// room is a rare room with a window, which is exactly the rank #813's gradient gives it.</summary>
    public static bool IsAReadingRoom(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return plate.Contains("READING ROOM", StringComparison.Ordinal);
    }

    /// <summary>
    /// #701 · WHOSE ROOM THIS IS — the plate first, then the trade the furniture is already dealt by.
    ///
    /// <para>The same ladder <see cref="ChamberFitting.KitFor"/> walks, asked one question further along,
    /// and it asks THROUGH that method rather than beside it: a room's shelves and a room's benches read one
    /// sentence about what is done in it, so a floor cannot grow a clinic's books over a laboratory's fume
    /// hood. The two rungs this file adds above it are the two the kit ladder has no opinion about, because
    /// neither changes what furniture a room gets: a reading room is an office with a different library, and
    /// a door post is whatever room it is guarding the door of.</para>
    ///
    /// <para><see cref="ChamberFitting.Kit.Trade"/> — the UNMARKED floors — falls through to the SITE's own
    /// register, because UNMARKED is a floor whose department nobody wrote down and not a floor whose work
    /// nobody does. A laboratory's unmarked floor is still full of laboratory people.</para>
    /// </summary>
    public static Post PostIn(string plate, string? department, UndergroundComplex.Kind kind)
    {
        ArgumentNullException.ThrowIfNull(plate);

        if (plate.Length == 0 || ChamberFitting.IsEmptyStore(plate))
        {
            return Post.None;
        }
        if (IsAReadingRoom(plate))
        {
            return Post.ReadingRoom;
        }
        if (IsAPost(plate))
        {
            return Post.Security;
        }

        return ChamberFitting.KitFor(plate, department, kind) switch
        {
            ChamberFitting.Kit.Laboratory => Post.Lab,
            ChamberFitting.Kit.Clinic => Post.Clinic,
            ChamberFitting.Kit.Office => Post.Records,
            ChamberFitting.Kit.Plant => Post.Engineering,
            ChamberFitting.Kit.Trade => TradeOf(kind),
            // Store and None: stock and nothing. Nobody was given this room.
            _ => Post.None,
        };
    }

    /// <summary>The site's own trade, for a room on a floor whose department nobody wrote down. A transit
    /// station's people file manifests, a depot's grade people on paper and the head office is paper all the
    /// way down — all three are clerks, and saying so is more honest than inventing a fourth answer.</summary>
    private static Post TradeOf(UndergroundComplex.Kind kind) => kind switch
    {
        UndergroundComplex.Kind.Laboratory => Post.Lab,
        UndergroundComplex.Kind.BlackClinic => Post.Clinic,
        _ => Post.Records,
    };

    /// <summary>
    /// #701 · IS THIS ROOM SOMEBODY'S? The occupancy rule, in one call, so the placer, the renderer and
    /// every guard read one sentence. See the class summary for the four clauses and why each is there.
    /// </summary>
    public static bool Occupied(
        in UndergroundComplex.Room room, string? department, UndergroundComplex.Kind kind) =>
        (room.Kind == UndergroundComplex.RoomKind.Chamber || IsAReadingRoom(room.Plate))
        && PostIn(room.Plate, department, kind) != Post.None;

    // ── WHICH FREETIME SHELF · dealt per floor, never rolled per room ─────────────────────────────────

    /// <summary>
    /// #701 · THE DEAL — which persona each occupied room on this floor gets.
    ///
    /// <para>The catalog asks for <i>never two alike on one floor</i>, and a floor has far more than seven
    /// occupied rooms in it, so the literal form of that is arithmetically impossible. The honest form is a
    /// DEAL: the seven are shuffled and handed out in order, and only when all seven are gone are they
    /// shuffled again. So no persona is ever repeated until every one of them has been seen, and no two
    /// rooms in a row ever share one — which is the thing the ask was protecting, because two identical
    /// shelves you can see from one another is the moment the layer stops saying anything about people.</para>
    ///
    /// <para>Seeded per (site, floor, cycle), so a floor deals the same hand on every visit. The first card
    /// of a cycle is swapped with the second where it would repeat the last card of the cycle before it —
    /// deterministically, and it is the only fixup in this file.</para>
    /// </summary>
    /// <param name="bodyId">The moon this building is under.</param>
    /// <param name="level">Which floor.</param>
    /// <param name="cycle">Which pass through the seven this is — the occupied room's ordinal divided by
    /// <see cref="Personas"/>.</param>
    public static IReadOnlyList<int> DealOn(string bodyId, int level, int cycle)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        int[] order = Shuffle(bodyId, level, cycle);
        if (cycle > 0 && order.Length > 1)
        {
            int last = Shuffle(bodyId, level, cycle - 1)[^1];
            if (order[0] == last)
            {
                (order[0], order[1]) = (order[1], order[0]);
            }
        }
        return order;
    }

    private static int[] Shuffle(string bodyId, int level, int cycle)
    {
        var order = new int[Personas];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = DiceRule.Roll(
                DiceRule.Seed($"hive:shelf-deal:{bodyId}:{level}:{cycle}:{i}"), i + 1).Face - 1;
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    /// <summary>#701 · Which persona the occupied room at this ORDINAL gets — the deal, read at one
    /// position. The ordinal is the room's place among the floor's occupied rooms and never its published
    /// index: a floor whose third chamber is a storeroom must not leave a gap in the hand.</summary>
    public static Entry FreetimeShelf(string bodyId, int level, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        return Freetime[DealOn(bodyId, level, ordinal / Personas)[ordinal % Personas]];
    }

    // ── THE FIXTURE ITSELF ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #701 · ONE SHELF, ON ONE WALL — where the generator stood it, and the copy that never moves.
    /// </summary>
    /// <param name="X">Where it stands, in the surface's own coordinates: on the chamber's own measured wall
    /// line, in a stretch nothing else claimed.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Room">Which published room of the floor it is in, so a guard and a press can name it
    /// without measuring anything.</param>
    /// <param name="Of">The authored shelf on it.</param>
    /// <param name="IsFreetime">Which of the room's two this is — what they did, or who they were.</param>
    public readonly record struct Shelf(double X, double Y, int Room, Entry Of, bool IsFreetime)
    {
        /// <summary>What is drawn on the deck beside it, and what the look-card is titled. One string for
        /// both, exactly as the odd book's card wears its own shelf line: a captain reads the same words on
        /// the card that the room is showing them.</summary>
        public string Plate => $"{Glyph} {Of.Shelf}";

        /// <summary>What [E] puts on the card.</summary>
        public string Card => Of.Card;

        /// <summary>What the casebook keeps, once per thread.</summary>
        public string Gist => Of.Gist;

        /// <summary>What the read-list stores.</summary>
        public string Id => Of.Id;
    }

    /// <summary>How far apart a room's two shelves stand, at least. <see cref="ChamberFitting.MinFittingDu"/>
    /// — the shortest stretch of wall this building thinks is worth standing anything against, which is also
    /// the shortest gap at which two plates on one wall read as two objects.</summary>
    public static double ApartDu => ChamberFitting.MinFittingDu;

    /// <summary>How far out of a fitting's own face a shelf's reading spot stands.
    /// <see cref="ChamberFitting.SeatSetbackDu"/> — the number the furnisher already stands a STOOL off a
    /// worktop with, and therefore the one square in this room that is known to hold a body: the seam the
    /// bins' own stand-offs are measured the same way (<c>CarveBins</c>).</summary>
    public static double StandOffDu => ChamberFitting.SeatSetbackDu;

    /// <summary>How far in from the end of a free stretch a shelf stands, where a room falls back on bare
    /// wall. The incident board's own hand's breadth (<see cref="IncidentBoard.EndInsetDu"/>) and not a
    /// second number.</summary>
    public static double EndInsetDu => IncidentBoard.EndInsetDu;

    /// <summary>How much floor a shelf on BARE WALL keeps between itself and the nearest piece of furniture.
    /// The incident board's own clearance, for its reason: a board behind a fume hood is a board nobody ever
    /// reads.</summary>
    public static double ClearOfFurnitureDu => IncidentBoard.ClearOfFurnitureDu;

    /// <summary>
    /// #701 · EVERY SHELF ON THIS FLOOR — two per occupied room, none anywhere else.
    ///
    /// <para><b>A shelf is hung over the room's OWN FURNITURE.</b> That is the finding rather than the first
    /// guess: the first cut stood shelves on bare wall the way the incident board hangs, and <b>only three
    /// occupied rooms in five had bare wall left</b> to stand two things on — a chamber is 15 × 12 du, its
    /// two fire-code doorways claim four du either side of themselves, and #818's furniture is standing
    /// against what is left. The rooms that failed were not unusual rooms. They were the ordinary ones, with
    /// two ways out and two fittings.</para>
    ///
    /// <para>Which is the right answer anyway, and the catalog said so: a shelf belongs in <i>the room's
    /// existing furniture idiom</i>. Books live on the shelving over the bench, not on the one metre of
    /// plaster nobody put anything against. So the candidates are the faces of the room's own fittings —
    /// taken in the order the furnisher dealt them, at the middle of each face and then out toward its ends — and
    /// BARE WALL IS THE FALLBACK, for the handful of rooms whose furniture would not seat two.</para>
    ///
    /// <para>Every candidate is then CHECKED, and nothing here is a coordinate somebody liked: clear of
    /// every published opening, clear of the square the A* audit stands a body on, clear of every OTHER
    /// fitting in the room, clear of the incident board where the floor carries one, and
    /// <see cref="ApartDu"/> from the room's other shelf. §13.15's rule — a fixture that cannot find floor
    /// is not fitted, and the room says so by having none.</para>
    ///
    /// <para>Depth ZERO, like the board: a shelf is fixed to a wall or to the thing standing against it and
    /// claims none of the room's floor, so it lays <b>no solid</b>, moves no wall, and changes nothing the
    /// walkability audits or the sightline cost measure. The building's collision field is byte-for-byte
    /// what it was.</para>
    ///
    /// <para><b>BOTH OR NEITHER.</b> A room that cannot seat two gets none — a room with a work shelf and no
    /// freetime shelf would be the layer saying half a thing about somebody, which is worse than saying
    /// nothing. The guards measure how often that happens rather than assuming it never does.</para>
    /// </summary>
    /// <param name="bodyId">The moon this building is under.</param>
    /// <param name="level">Which floor.</param>
    /// <param name="rooms">The floor as carved AND FURNISHED — the published list, after
    /// <see cref="ChamberFitting.Fit"/> has run, because how much wall is spare is a fact about what is
    /// already standing against it.</param>
    /// <param name="openings">Every hole on the floor a body can pass. A caller that hands over more loses
    /// nothing.</param>
    /// <param name="board">The incident board, where the floor carries one, so a shelf is never stood in
    /// front of it.</param>
    public static IReadOnlyList<Shelf> On(
        string bodyId, int level,
        IReadOnlyList<UndergroundComplex.Room> rooms,
        IReadOnlyList<SurfaceLayout.Doorway> openings,
        IncidentBoard.Board? board = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(openings);

        var shelves = new List<Shelf>();

        // #677 · A SHELF IS A FACILITY OBJECT, and the band nobody dug is not a facility. One call, the odd
        // book's own, so the halls cannot be reached by either feature and there is one place to argue.
        if (!OddBooks.ShelvesStandHere(bodyId, level))
        {
            return shelves;
        }

        string? department = ChamberFitting.DepartmentOn(bodyId, level);
        UndergroundComplex.Kind kind = UndergroundComplex.KindOn(bodyId, level);

        // The ordinal is the room's place among the floor's OCCUPIED rooms — see FreetimeShelf.
        int ordinal = 0;
        for (int i = 0; i < rooms.Count; i++)
        {
            UndergroundComplex.Room room = rooms[i];
            if (!Occupied(in room, department, kind))
            {
                continue;
            }

            Post post = PostIn(room.Plate, department, kind);
            var pair = new List<Shelf>(2);
            foreach ((double x, double y) in Spots(room, openings, board))
            {
                if (pair.Count == 1 && Near(pair[0].X, pair[0].Y, x, y, ApartDu))
                {
                    continue;
                }

                pair.Add(new Shelf(
                    x, y, i,
                    pair.Count == 0 ? WorkShelf(post) : FreetimeShelf(bodyId, level, ordinal),
                    IsFreetime: pair.Count == 1));
                if (pair.Count == 2)
                {
                    break;
                }
            }

            // Both or neither. The ordinal only advances where a hand was actually dealt, so a room the
            // walls refused does not put a gap in the floor's deal.
            if (pair.Count == 2)
            {
                shelves.AddRange(pair);
                ordinal++;
            }
        }

        return shelves;
    }

    /// <summary>How finely a free stretch of BARE wall is walked, in the fallback. A hand's breadth
    /// (<see cref="EndInsetDu"/>): fine enough that the foot of wall beyond a bench's end is still offered,
    /// coarse enough that no room is walked more than a few dozen times.</summary>
    public static double StepDu => EndInsetDu;

    /// <summary>
    /// Every square in this room a shelf could be read at, in the order the room offers them.
    ///
    /// <para><b>The furniture first</b>, in the order the furnisher dealt it: out of each fitting's own face
    /// toward the room, at the middle of the face and then out toward its ends. That is where the books are, and
    /// it is the one square per fitting this building already proves a body fits on — it is the stool's own
    /// setback (<see cref="StandOffDu"/>).</para>
    ///
    /// <para><b>Then the bare wall</b>, longest free stretch first
    /// (<see cref="ChamberFitting.FreeWallRuns"/> sorts them), walked end to end at <see cref="StepDu"/>.
    /// For a room whose furniture cannot seat two, and for the rare room the furnisher left empty.</para>
    /// </summary>
    private static IEnumerable<(double X, double Y)> Spots(
        UndergroundComplex.Room room, IReadOnlyList<SurfaceLayout.Doorway> openings,
        IncidentBoard.Board? board)
    {
        var found = new List<(double X, double Y)>();
        bool boardIsHere = board is { } b && room.Contains(b.X, b.Y);

        // ── THE ROOM'S OWN FITTINGS ──────────────────────────────────────────────────────────────────
        foreach (RingOffice.Fixture kit in room.Furniture)
        {
            // Which way is OUT of this fitting into the room. A fitting is laid along a wall, so its short
            // axis is the one that points at the room — and a fitting with no depth at all (a run of
            // racking, a filing bank) is a LINE, whose short axis is zero long and whose direction is
            // therefore read off the room's own centre. Both cases, one sentence.
            double cx = (kit.X0 + kit.X1) / 2.0, cy = (kit.Y0 + kit.Y1) / 2.0;
            bool along = (kit.X1 - kit.X0) >= (kit.Y1 - kit.Y0);
            double away = along
                ? (room.Y >= cy ? +1.0 : -1.0)
                : (room.X >= cx ? +1.0 : -1.0);
            double face = along
                ? (away > 0 ? kit.Y1 : kit.Y0) + (away * StandOffDu)
                : (away > 0 ? kit.X1 : kit.X0) + (away * StandOffDu);

            double lo = along ? kit.X0 : kit.Y0, hi = along ? kit.X1 : kit.Y1;
            foreach (double t in new[] { 0.5, 1.0 / 6.0, 5.0 / 6.0, 1.0 / 3.0, 2.0 / 3.0 })
            {
                double at = lo + ((hi - lo) * t);
                Offer(along ? at : face, along ? face : at, kit);
            }
        }

        // ── …AND THEN WHATEVER WALL IS LEFT BARE ─────────────────────────────────────────────────────
        foreach (ChamberFitting.WallRun wall in ChamberFitting.FreeWallRuns(in room, openings))
        {
            double lo = wall.Lo + EndInsetDu, hi = wall.Hi - EndInsetDu;
            if (hi < lo)
            {
                continue;
            }

            int steps = (int)Math.Floor((hi - lo) / StepDu);
            for (int s = 0; s <= steps; s++)
            {
                (double x, double y) = wall.At(lo + (s * StepDu));
                Offer(x, y, null);
            }
        }

        return found;

        // One gate, so a spot at a fitting and a spot on bare wall are held to the same three tests and
        // neither list can quietly relax one of them.
        void Offer(double x, double y, RingOffice.Fixture? mine)
        {
            if (!room.Contains(x, y))
            {
                return;   // a face that points out of the room is not a face anybody stands at
            }
            foreach (SurfaceLayout.Doorway hole in openings)
            {
                if (ChamberFitting.BoxToPoint(
                        Math.Min(hole.X1, hole.X2), Math.Min(hole.Y1, hole.Y2),
                        Math.Max(hole.X1, hole.X2), Math.Max(hole.Y1, hole.Y2), x, y)
                    < ChamberFitting.OpeningClearDu)
                {
                    return;
                }
            }
            if (Near(x, y, room.X, room.Y, ChamberFitting.CentreClearDu))
            {
                return;   // the square the A* audit stands a body on stays standable
            }
            foreach (RingOffice.Fixture other in room.Furniture)
            {
                if (mine is { } own && other.X0 == own.X0 && other.Y0 == own.Y0
                    && other.X1 == own.X1 && other.Y1 == own.Y1)
                {
                    continue;   // the fitting this spot is the face OF
                }
                if (ChamberFitting.BoxToPoint(other.X0, other.Y0, other.X1, other.Y1, x, y)
                    < ClearOfFurnitureDu)
                {
                    return;
                }
            }
            if (boardIsHere && board is { } hung && Near(hung.X, hung.Y, x, y, ClearOfFurnitureDu))
            {
                return;   // never in front of the board: the room's library would hide its own gag
            }
            found.Add((x, y));
        }
    }

    private static bool Near(double ax, double ay, double bx, double by, double du)
    {
        double dx = ax - bx, dy = ay - by;
        return ((dx * dx) + (dy * dy)) < du * du;
    }

    // ── READING ONE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Everything one read of one shelf answers, in one pure call.
    ///
    /// <para><see cref="Gist"/> is null when this thread has already filed this shelf — that is the whole of
    /// the once-per-shelf law, and it lives here rather than in an <c>if</c> in a partial class so a test can
    /// walk it. <see cref="Filed"/> is the new read-list either way, so the caller never has to
    /// decide.</para></summary>
    public readonly record struct Reading(
        Shelf Of, string Title, string Card, string? Gist, IReadOnlyList<string> Filed);

    /// <summary>
    /// #701 · READ A SHELF WHERE IT STANDS. The odd book's own law, said about a fixture that is always
    /// there: looking is free and the card comes up every time; the casebook learns the gist once per shelf
    /// per game-thread (#603).
    ///
    /// <para>Once per SHELF and not per room, exactly as the odd book files per book and not per room: the
    /// clerk's shelf is the clerk's shelf in every records annex in the system, and a book that filed it
    /// eleven times would be a book keeping a tally of how many corridors a captain has walked.</para>
    ///
    /// <para><paramref name="filed"/> is the ids this game-thread has already put in the casebook — the odd
    /// book's own list (<c>Vault.Progress.OddBooksRead</c>), which these ids are namespaced against so the
    /// two features share one store and can never collide.</para></summary>
    public static Reading Read(Shelf shelf, IReadOnlyList<string>? filed)
    {
        var read = new List<string>(filed ?? []);
        bool first = !read.Contains(shelf.Id, StringComparer.Ordinal);
        if (first)
        {
            read.Add(shelf.Id);
        }
        return new Reading(shelf, shelf.Plate, shelf.Card, first ? shelf.Gist : null, read);
    }

    /// <summary>#741 · WHAT THE ENTRY IS ABOUT, declared by the author and never read back out of its words.
    ///
    /// <para>The subject is the PLACE. A shelf says what somebody did and who they were and it names nobody
    /// — there is no person here for the book to be about, and minting one would be the game detecting
    /// (§12.4, and #741's own refusal). What a captain will want the stack of, standing in a corridor two
    /// floors down, is <i>this building</i>: every shelf they have read in it, under one heading.</para></summary>
    public static string SubjectsFor(string place) =>
        CaseSubjects.Line(CaseSubjects.Place(place ?? string.Empty));

    /// <summary>Every sentence this file can put on a screen, for the canon sweep. The shelf line, the card
    /// and the gist of all thirteen, and the plate the room shows built out of them.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (Entry entry in Work)
        {
            yield return entry.Shelf;
            yield return entry.Card;
            yield return entry.Gist;
            yield return $"{Glyph} {entry.Shelf}";
        }
        foreach (Entry entry in Freetime)
        {
            yield return entry.Shelf;
            yield return entry.Card;
            yield return entry.Gist;
            yield return $"{Glyph} {entry.Shelf}";
        }
    }
}
