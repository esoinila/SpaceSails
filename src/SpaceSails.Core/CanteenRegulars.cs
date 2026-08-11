using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #709 · THE FIRST PEOPLE IN THE HIVE — and they are all outsiders, exactly like you.
///
/// <para>Owner, 2026-08-05: <i>"we should have people in the bar... we have cover story"</i> and, immediately
/// after, the ruling that shapes the whole thing: <i>"for now let's keep the people in B1."</i></para>
///
/// <h3>Why B1 only, and why that is the design rather than a limitation</h3>
///
/// <para>The Hive's abandoned tone is doing real work — stripped rooms, lights left on, nobody paying — and
/// staff on every floor would spend it. Confining them to the top pressurised floor buys three things at
/// once:</para>
///
/// <list type="bullet">
/// <item>The job-seeker cover (#618) acquires a <b>natural expiry</b>. It holds exactly as far as the floor
/// where an outsider plausibly belongs, which is the world's own shape answering "what blows the cover"
/// instead of a rule we invented.</item>
/// <item><b>Descent becomes the horror gradient.</b> Each floor down is quieter and the last person you saw
/// is further behind you. Corridor length was never going to do that; a population falling to zero does it
/// for free.</item>
/// <item>The empty floors below read as <b>absence</b> rather than as unfinished content. Once a captain has
/// seen this building with people in it, B7 is a floor somebody left.</item>
/// </list>
///
/// <h3>They are in the upper canteen because its own sign says they may be</h3>
///
/// <para>#707 stencilled that room <c>CANTEEN 1 · CARRIERS &amp; CONTRACTORS · NO PASS REQUIRED</c> before
/// anybody had thought about who sits in it, and it turns out to have decided the cast. The people here are
/// hauliers, fitters, agency temps and drivers — <b>outsiders with no more right to be in the building than
/// the captain has.</b> That is precisely why nobody asks anybody for a card at band 0, and it is what makes
/// the cover work: not because it is a good lie, but because <i>everyone else's is equally thin.</i></para>
///
/// <h3>Who is in and where they sit turns over with the SHIFT (#709, owner 2026-08-05)</h3>
///
/// <para>Owner: <i>"let's have some random element of who is in the bar and where they got to sit down."</i>
/// The room was seeded off the site alone, so a moon had the same three people in the same three chairs
/// forever — which reads as furniture rather than as a canteen.</para>
///
/// <para><b>It is a ROTA, not a dice roll</b>, and it reuses the bar's own watch upstairs
/// (<see cref="Interior.PatronRota.WatchSeconds"/> — one answer to "how long is a shift", not a second
/// number that must agree with the first). The board on the wall of this very room says
/// <c>ROTA — WEEK 31</c>; people coming and going with the shift is the most in-fiction randomness
/// available, and it costs one seed component.</para>
///
/// <para><b>Why a watch INDEX and never a raw clock.</b> The caller must freeze which shift it is when the
/// floor is drawn and hand that same number to every later question about the room. Passing a live time
/// would let the deck be built in one shift and the [E] press land in the next — the drawn room and the
/// pressed room disagreeing about who is at which table, which is this project's third named bug class (the
/// sim doing one thing while the picture reports another). A watch that is chosen once cannot drift.</para>
///
/// <para>Deterministic within a shift, so re-entering the same room in the same watch shows the same people
/// in the same chairs, and the guards can still pin it. Randomness across shifts, determinism inside
/// one.</para>
///
/// <h3>The laws</h3>
///
/// <list type="number">
/// <item><b>Top pressurised floor only.</b> Never the staff mess deeper down (that room is pass-only and its
/// people are a different question), never a washroom, never anywhere else. The owner's ruling, enforced here
/// rather than in the renderer.</item>
/// <item><b>Seeded off the site AND the shift</b>, never off the visit. A moon has the people that moon has on
/// that watch — the same room on re-entry, a different room next shift, and no call to any clock in here.</item>
/// <item><b>Nobody explains anything.</b> §13.8 holds hardest in the one room where somebody could talk. The
/// talk is about freight, signatures, shifts, pay and the machines. Not one line says what the facility is
/// for, and the closest any of them comes is a remark about hiring that only becomes horrifying if the player
/// has assembled something the game never states.</item>
/// </list>
///
/// <para>Pure and world-blind: the client asks who is sitting down and draws them, and never keeps an opinion
/// of its own about whether a room has people in it.</para>
/// </summary>
public static class CanteenRegulars
{
    /// <summary>The glyph a regular's plate and their filed line both carry — a person, at console size.</summary>
    public const string Glyph = "◈";

    /// <summary>The most who will ever be in the room at once. Three is a canteen with people in it; six is a
    /// crowd, and a crowd in a clandestine basement is a different building than the one we are describing.
    /// It is additionally clamped by how many tables the room actually has (#707 places those).</summary>
    public const int MostAtOnce = 3;

    /// <summary>One authored regular. <see cref="Plate"/> is what stands over them in the room;
    /// <see cref="Line"/> is what they say when a captain stops at the table.</summary>
    /// <param name="Plate">Who they read as, at a glance, before anybody speaks.</param>
    /// <param name="Line">What they actually say. One breath, because a stranger in a canteen gets one.</param>
    public readonly record struct Character(string Plate, string Line);

    /// <summary>Somebody sitting at a table, placed.</summary>
    /// <param name="X">The table's centre, in the surface's own coordinates.</param>
    /// <param name="Y">The table's centre.</param>
    /// <param name="Plate">Who they read as.</param>
    /// <param name="Line">What they say.</param>
    public readonly record struct Seated(double X, double Y, string Plate, string Line);

    /// <summary>
    /// The authored cast. Every one of them is somebody with a boring, verifiable, entirely legitimate reason
    /// to be in this building — which is the whole point of the room and the whole point of the cover.
    ///
    /// <para><b>The register test</b>, inherited from #701: nobody here is interesting. They are tired, owed
    /// money, waiting on somebody else's paperwork, or eating. A regular who was mysterious would be a
    /// quest-giver with a hat on, and the moment one of them is worth talking to for plot reasons the room
    /// stops being cover and becomes a corridor with clues in it.</para>
    /// </summary>
    private static readonly Character[] Cast =
    [
        new("◈ A CARRIER, WAITING ON A SIGNATURE",
            "Third day sat here. They'll sign it when the man who signs it gets back from wherever he is."),

        new("◈ A FITTER, OFF A MAINTENANCE CONTRACT",
            "Number two pump's been singing since spring. I've written it up four times. It's still singing."),

        new("◈ AN AGENCY TEMP, FIRST WEEK",
            "They took my name at the door and put a different one on the rota. Said it's easier that way."),

        new("◈ A DRIVER, NOT SAYING WHO FOR",
            "I bring it to the hut and I put it down. What it is after that is somebody else's job."),

        new("◈ SOMEBODY EATING, SHIFT ENDED",
            "Don't take the stew. Take anything else."),

        new("◈ A WOMAN DOING INVOICES AT A TABLE",
            "Everything down here was bought as something else. My pallet jack is a soil sampler."),

        new("◈ A HAND WHO HAS BEEN HERE LONGER THAN THE CONTRACT SAID",
            "Six weeks, it said. That was a while ago now. The money still comes, so."),

        new("◈ A MAN AT THE MACHINES, HAVING NO LUCK",
            "It takes the card and it thinks about it and then it gives you the card back. Every time."),

        new("◈ A CONTRACTOR NOBODY HAS COME FOR",
            "They're always hiring. Nobody ever says what for, and the pay clears, so nobody asks."),

        new("◈ A QUIET ONE, FACING THE DOOR",
            "..."),
    ];

    /// <summary>How many authored regulars exist. Public so a guard can pin the catalog's size without
    /// reaching into it.</summary>
    public static int CastSize => Cast.Length;

    /// <summary>Every authored plate and line, for the canon grep. Nothing in this list may explain the Old
    /// Ones, and the guard that checks it walks THIS, so a line added tomorrow is checked tomorrow.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (Character c in Cast)
        {
            yield return c.Plate;
            yield return c.Line;
        }
    }

    /// <summary>
    /// Who is sitting in this amenity, if anybody is.
    ///
    /// <para>Empty for every room that is not the upper canteen on the site's top pressurised floor — which
    /// is the owner's B1 ruling, living in Core where a test can reach it rather than as an <c>if</c> in a
    /// renderer.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor being built.</param>
    /// <param name="amenity">The room, as Core carved it (#707) — its tables are the seats.</param>
    /// <param name="watch">Which shift this is — <see cref="Interior.PatronRota.WatchIndex"/> of the sim
    /// clock, and deliberately a WATCH rather than a raw time. See the class docs.</param>
    public static IReadOnlyList<Seated> Sitting(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch = 0)
    {
        var sat = new List<Seated>();
        foreach ((int table, int who) in Seating(bodyId, level, amenity, watch))
        {
            (double tx, double ty) = amenity.Tables[table];
            sat.Add(new Seated(tx, ty, Cast[who].Plate, Cast[who].Line));
        }

        return sat;
    }

    // ── #746 · THE TABLES HAVE SEATS, AND THE SAME LAW SAYS WHO IS IN THEM ────────────────────────────────
    //
    // Owner, 2026-08-06: "tables should seat 2/4/more, not all pairs" — and, one sentence later, the reason
    // it matters mechanically rather than decoratively: "asking to sit is missing."
    //
    // A seat count is only worth having if somebody can ask whether one is free, and the moment two callers
    // can answer that question this repo has its most expensive bug class back (two sources for one fact —
    // the drawn room and the pressed room disagreeing, #709's own warning). So the renderer does not walk
    // Amenity.Tables and separately ask who is Sitting: it asks THIS, once, and gets the tops, their seat
    // counts and their occupancy in the same list, off the same frozen watch.

    /// <summary>How many a round top seats. Two, four or six — the owner's own three (#746), stated as a
    /// list so a guard can pin them without knowing the arithmetic that picks one.</summary>
    public static readonly IReadOnlyList<int> SeatCounts = [2, 4, 6];

    /// <summary>One round top in an amenity: where it is, how many it seats, and who — if anybody — is in
    /// one of those seats this watch.</summary>
    /// <param name="Index">Its ordinal in the room's own table list, so a caller can key state off it.
    /// Cabinet tops carry on from the hall floor's, so one ordinal names one top in one room.</param>
    /// <param name="X">Centre, in the surface's own coordinates.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="Seats">2, 4 or 6 — furniture, decided by the building and never by the shift.</param>
    /// <param name="Plate">Who is at it, or null for an empty table.</param>
    /// <param name="Line">What they say, or null.</param>
    /// <param name="Stranger">#751 · Whether the person at it is a BACKGROUND PATRON — one of the crowd
    /// that fills a hall — rather than one of the ten named regulars. A stranger's table is a thin scene
    /// (small talk, the round, your leave) and the depth stays with the named cast.</param>
    /// <param name="Cabinet">#751 · Which cabinet this top is in, or 0 for the hall floor. It is the fact
    /// the QUIET RULE reads: the counter has eyes everywhere except in here.</param>
    /// <param name="Talking">#792 · Are the people at this top IN A CONVERSATION WITH EACH OTHER? See
    /// <see cref="StrangerTalks"/> for why this is authored rather than counted.</param>
    /// <param name="Heads">#823 · HOW MANY PEOPLE ARE AT IT — 0 for an empty top, 1 for any of the ten named
    /// regulars, and the crowd's own authored headcount (<see cref="StrangerHeads"/>) for a background
    /// patron. Owner, playtest 2026-08-11: <i>"The number of people sitting at a table should match the
    /// amount of seats that are taken."</i></param>
    public readonly record struct TableSeat(
        int Index, double X, double Y, int Seats, string? Plate, string? Line,
        bool Stranger = false, int Cabinet = 0, bool Talking = false, int Heads = 0)
    {
        /// <summary>Somebody is at this table.</summary>
        public bool Taken => Plate is not null;

        /// <summary>
        /// Chairs nobody is in — the seat count less the party's <see cref="Heads"/>, and the number "ask to
        /// join" reads.
        ///
        /// <para>#823 · It was <c>Seats - (Taken ? 1 : 0)</c> until the owner counted the chairs: <i>"it says
        /// there are two haulers eating at a table that seats four, yet there is visual indication of only one
        /// seat out of four being taken? Does the other try sit in the others lap?"</i> A bool cannot seat a
        /// crew, and the plates have held crews since #792. Clamped at zero because a table may be FULL, and
        /// a full table refusing to offer a chair is the honest answer rather than a negative one.</para>
        /// </summary>
        public int Free => Math.Max(0, Seats - Heads);

        /// <summary>#751 · Is this a table the counter cannot see? The one source for the quiet rule.</summary>
        public bool Quiet => Cabinet > 0;

        /// <summary>
        /// #792 · SOMEBODY SITTING ON THEIR OWN — the other half of <see cref="Talking"/>, named so that a
        /// caller asking the approachable question does not have to spell it as a negation.
        ///
        /// <para>Owner, playtest 2026-08-08: <i>"it determines whether there is anything to overhear as
        /// discussion goes."</i> A lone sitter is #757's ask-to-join; a conversation already going is a
        /// different affordance entirely, and a captain looking for one or the other must be able to tell
        /// them apart across a room.</para>
        /// </summary>
        public bool Alone => Taken && !Talking;
    }

    /// <summary>
    /// #746/#751 · HOW MANY EACH ROUND TOP IN THIS ROOM SEATS — the one place the question is answered.
    ///
    /// <para>Two rooms, two laws, and they meet here so that nothing downstream has to know which it is
    /// looking at:</para>
    ///
    /// <list type="bullet">
    /// <item><b>An ordinary three-top canteen</b> rolls each top, seeded off the SITE, the room's use and
    /// the top's ordinal — and deliberately NOT off the watch. A canteen does not re-furnish itself every
    /// shift, and a table that seated six at breakfast and two at supper would be the picture and the sim
    /// disagreeing about a thing the player can count.</item>
    /// <item><b>A hall</b> reads <see cref="UndergroundComplex.HallSeatBill"/>, because a hall has a SEAT
    /// TARGET to hit and twenty independent rolls miss it by seven on average (see that method's docs). The
    /// caterer's stock is designed; only its arrangement is seeded.</item>
    /// </list>
    /// </summary>
    public static IReadOnlyList<int> SeatBill(string bodyId, UndergroundComplex.Amenity amenity)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (amenity.Hall is { } hall)
        {
            return UndergroundComplex.HallSeatBill(bodyId, amenity.Use, hall.SeatTarget);
        }

        var rolled = new List<int>(amenity.Tables.Count);
        for (int i = 0; i < amenity.Tables.Count; i++)
        {
            ulong seed = DiceRule.Seed(
                $"hive:canteen:seats:{bodyId}:{(int)amenity.Use}:{i}", 0);
            rolled.Add(SeatCounts[DiceRule.Roll(seed, SeatCounts.Count).Face - 1]);
        }
        return rolled;
    }

    /// <summary>#746 · How many a given round top seats. <see cref="SeatBill"/>'s entry for it, and kept as
    /// its own call because that is how the rest of the game asks.</summary>
    public static int SeatsAt(string bodyId, UndergroundComplex.Amenity amenity, int tableIndex)
    {
        IReadOnlyList<int> bill = SeatBill(bodyId, amenity);
        return tableIndex >= 0 && tableIndex < bill.Count ? bill[tableIndex] : SeatCounts[0];
    }

    /// <summary>
    /// #746/#751 · EVERY ROUND TOP IN THE ROOM, with its seats and its occupancy — the one fact the
    /// renderer draws and the one fact the [E] press asks. Same frozen watch (#709), so the chair on the
    /// screen and the chair the game offers you are the same chair.
    ///
    /// <para>#751 · Three tiers come out of this one call, in one list, because they are one question:
    /// the ten NAMED REGULARS keep their tables and their whole scene; BACKGROUND PATRONS fill whatever the
    /// watch says the hall is holding and are pure data (a plate, a bark, a chair — no pathing, nothing per
    /// frame); and the CABINET tops come last, empty, because #731's walkers are the ones who will sit in
    /// them.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="amenity">The room, as Core carved it (#707/#751).</param>
    /// <param name="watch">The shift, frozen when the floor was drawn.</param>
    public static IReadOnlyList<TableSeat> Tables(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch = 0)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var who = new Dictionary<int, int>();
        foreach ((int table, int cast) in Seating(bodyId, level, amenity, watch))
        {
            who[table] = cast;
        }

        IReadOnlyList<int> bill = SeatBill(bodyId, amenity);
        Dictionary<int, int> crowd = Crowd(bodyId, level, amenity, watch, who.Keys, bill);

        var tops = new List<TableSeat>(amenity.Tables.Count + CabinetRoom(amenity));
        for (int i = 0; i < amenity.Tables.Count; i++)
        {
            (double tx, double ty) = amenity.Tables[i];
            int seats = i < bill.Count ? bill[i] : SeatCounts[0];

            if (who.TryGetValue(i, out int cast))
            {
                // #792 · A named regular is ONE PERSON at a top, every one of the ten, which is the whole
                // premise of #757's ask-to-join: there is a chair, and somebody to ask. So they are never
                // Talking, and the deck may draw them as the approachable thing they are.
                tops.Add(new TableSeat(
                    i, tx, ty, seats, Cast[cast].Plate, Cast[cast].Line, Heads: RegularHeads));
            }
            else if (crowd.TryGetValue(i, out int face))
            {
                tops.Add(new TableSeat(
                    i, tx, ty, seats,
                    StrangerPlates[face % StrangerPlates.Count],
                    Barks[BarkIndex(bodyId, i, watch)],
                    Stranger: true,
                    Talking: StrangerTalks(face % StrangerPlates.Count),
                    // #823 · …and the party's size, off the same one authored row. Crowd() has already
                    // refused to deal a face this top cannot seat, so this arrives fitting.
                    Heads: StrangerHeads(face % StrangerPlates.Count)));
            }
            else
            {
                tops.Add(new TableSeat(i, tx, ty, seats, null, null));
            }
        }

        // …and the cabinets, which are tops in this room like any other and are simply nobody's this watch.
        if (amenity.Hall is { } hall)
        {
            foreach (UndergroundComplex.Cabinet cabinet in hall.Cabinets)
            {
                tops.Add(new TableSeat(
                    tops.Count, cabinet.Table.X, cabinet.Table.Y, UndergroundComplex.CabinetSeats,
                    null, null, Cabinet: cabinet.Number));
            }
        }

        return tops;
    }

    /// <summary>How many cabinet tops this room adds, for the list's capacity.</summary>
    private static int CabinetRoom(UndergroundComplex.Amenity amenity) =>
        amenity.Hall?.Cabinets.Count ?? 0;

    /// <summary>
    /// #709/#757 · IS THIS THE ROOM PEOPLE ARE IN? The owner's B1 ruling, as a question anybody may ask.
    ///
    /// <para>The upper canteen, on the top pressurised floor, and nowhere else. It was already the first two
    /// clauses of <see cref="Seating"/> and of <see cref="Crowd"/> in longhand; #757 needed a THIRD caller,
    /// because an empty top may only offer <i>take this table</i> in a room outsiders are admitted to — the
    /// pass-only staff mess is hall-class as well, full of tops, and its whole identity is that the shift
    /// has not come and nobody is ever in it. A third private copy of two clauses is how one rule stops
    /// being one rule, so the copies became this.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="amenity">The room, as Core carved it (#707).</param>
    public static bool PeopleSitHere(string bodyId, int level, UndergroundComplex.Amenity amenity)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return amenity.Use == UndergroundComplex.Comfort.UpperCanteen
            && UndergroundComplex.TopPressurisedFloor(bodyId) == level;
    }

    /// <summary>WHO IS AT WHICH TABLE, as indices — the one rota, called by <see cref="Sitting"/> and by
    /// <see cref="Tables"/>. It was inlined in Sitting until #746 needed the table's ORDINAL as well as its
    /// coordinates; matching a person back to a top by comparing two doubles would have been a second answer
    /// to "who is sitting where", which is the thing this class's own docs warn about hardest.</summary>
    private static List<(int Table, int Who)> Seating(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var seating = new List<(int Table, int Who)>();

        // The washroom and the deep staff mess get nobody. The mess is pass-only and the people who would be
        // in it are #618's question, not this one; the washroom is the one amenity nobody sits down in. And
        // only on the floor the owner put them on — #757 lifted both clauses into PeopleSitHere so that the
        // third caller could not become a second opinion.
        if (!PeopleSitHere(bodyId, level, amenity))
        {
            return seating;
        }

        int seats = Math.Min(amenity.Tables.Count, MostAtOnce);
        if (seats <= 0)
        {
            return seating;
        }

        // How many turned up THIS SHIFT. At least one — the owner asked for people in the bar, and an empty
        // canteen is a thing this building already has twenty floors of.
        ulong seed = DiceRule.Seed($"hive:canteen:{bodyId}", watch);
        int here = DiceRule.Roll(seed, seats).Face;

        var usedTables = new List<int>(here);
        var usedCast = new List<int>(here);

        for (int i = 0; i < here; i++)
        {
            int table = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:canteen:table:{bodyId}:{i}", watch), amenity.Tables.Count).Face - 1,
                amenity.Tables.Count, usedTables);
            int cast = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:canteen:who:{bodyId}:{i}", watch), Cast.Length).Face - 1,
                Cast.Length, usedCast);

            seating.Add((table, cast));
        }

        return seating;
    }

    // ── #751 · THE CROWD, WHICH IS THE COVER ─────────────────────────────────────────────────────────────
    //
    // Owner: "It needs to house like 80 customers… I am thinking like Mos Eisley Space port size bar."
    //
    // Eighty carriers eating on the company's coin, none of whom ask what the cage carries, is #707's lie
    // rendered as a crowd — and one nearly-empty night watch of the same hall is free horror. Nothing ever
    // announces which watch you have walked into; the seeding just differs, and the room tells you.
    //
    // THEY ARE DATA. A background patron is a plate, a bark and a chair. No pathing, no schedule, no
    // per-frame anything — the renderer draws a console at a coordinate Core already placed, exactly as it
    // does for the ten named regulars, and the whole crowd costs one pass over the room's own table list at
    // deck-build time. WASM perf is a law here and this is how it is kept: by there being nothing to run.
    //
    // AND THEY ARE ONLY EVER IN A HALL. An ordinary three-top canteen keeps #709's room exactly as it was —
    // three tables, up to three regulars — because a "crowd" of two strangers in a nook is not a crowd, it
    // is the named cast with the names taken off.

    /// <summary>
    /// #751 · HOW FULL THE HALL IS, WATCH BY WATCH — the fraction of its tops that have somebody at them.
    ///
    /// <para>Six entries, because a watch is four sim-hours (<see cref="Interior.PatronRota.WatchSeconds"/>)
    /// and six of those are a day. Read as a day: the hall heaves through the middle of it and empties out
    /// to a handful of tables on the small watches. <b>Nothing says so out loud</b> — there is no sign, no
    /// caption and no line about it anywhere in the game, and a captain who walks in twice at different
    /// hours simply finds two different rooms.</para>
    ///
    /// <para>FLAGGED for the owner's tuning: these six numbers are the entire mood of the room.</para>
    /// </summary>
    public static readonly IReadOnlyList<double> WatchFill = [0.30, 0.85, 0.95, 0.70, 0.45, 0.15];

    /// <summary>
    /// #792 · ONE BACKGROUND PATRON — the plate, and WHETHER THERE IS ANYTHING TO OVERHEAR AT IT.
    /// </summary>
    /// <param name="Plate">What stands over them, before anybody speaks.</param>
    /// <param name="Talking">Are they in a conversation with each other?</param>
    /// <param name="Heads">#823 · HOW MANY OF THEM THERE ARE. Authored beside the sentence for the same
    /// reason <paramref name="Talking"/> is: it is a fact about the party, not a property of the string.</param>
    private readonly record struct Face(string Plate, bool Talking, int Heads);

    /// <summary>
    /// #792 · THE CROWD, WITH THE ONE FACT A HUNGRY TRAVELLER READS OFF A ROOM SECOND.
    ///
    /// <para>Owner, playtest 2026-08-08: <i>"people looking to sit down look at those like hungry wild
    /// beasts look at their prey"</i>, and then the second glance — <i>"does a table hold somebody alone
    /// … or a conversation already going … it determines whether there is anything to overhear."</i></para>
    ///
    /// <para><b>Why it is authored and not counted.</b> The obvious implementation reads the plate and
    /// counts the people in it: TWO HAULIERS is two, so they are talking. It is wrong on the last line of
    /// this list, and wrong in the direction that matters — <i>A COUPLE NOT TALKING</i> is two people with
    /// nothing to overhear, and the plate says so in words. A renderer, or a Core helper, deriving the
    /// conversation from the headcount would draw a speech mark over the one table in the room whose whole
    /// character is silence. So the fact lives HERE, beside the sentence it belongs to, where an author
    /// writing the eleventh plate has to decide it, and a guard proves it is not the plural rule wearing a
    /// field name.</para>
    ///
    /// <para>#823 · HOW MANY OF THEM THERE ARE is the second fact, authored on the same line and for exactly
    /// the same reason. It shipped as a bool — every occupied top took one chair — until the owner sat down
    /// and counted: <i>"two haulers eating at a table that seats four, yet… only one seat out of four being
    /// taken."</i> A crew is not a person, and the number of chairs a party takes is the kind of thing an
    /// author decides when they write the sentence, never something a downstream caller reads out of it.</para>
    /// </summary>
    private static readonly Face[] Faces =
    [
        new("◈ CAGE CREW, OFF SHIFT", true, 3),
        new("◈ TWO HAULIERS, EATING", true, 2),
        new("◈ A LOADER WITH A BAD WRIST", false, 1),
        new("◈ SOMEBODY'S WHOLE CREW AT ONE TABLE", true, 4),
        new("◈ A DRIVER READING A DOCKET", false, 1),
        new("◈ A YARD HAND, WAITING ON A CAR", false, 1),
        new("◈ A PAIR FROM THE SCAFFOLD GANG", true, 2),
        new("◈ A WEIGHBRIDGE CLERK ON A BREAK", false, 1),
        new("◈ THREE ON THE SAME CONTRACT", true, 3),
        // …and the one that makes this a fact rather than a word count.
        new("◈ A COUPLE NOT TALKING", false, 2),
    ];

    /// <summary>#823 · ONE NAMED REGULAR IS ONE PERSON — every one of the ten, which is #757's whole premise:
    /// there is a chair, and somebody to ask. Named so that the deal site states it rather than typing a
    /// bare 1 that a reader has to take on trust.</summary>
    public const int RegularHeads = 1;

    /// <summary>The plates, in the crowd's own order — <see cref="StrangerPlates"/>'s backing, so the list
    /// every existing caller reads and the list the conversation fact is authored in cannot drift apart by
    /// one entry.</summary>
    private static string[] PlatesOf(Face[] faces)
    {
        var plates = new string[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            plates[i] = faces[i].Plate;
        }
        return plates;
    }

    /// <summary>
    /// #751 · The plates the crowd wears. Ten of them, and every one is duller than the last — the register
    /// test (#701) applies twice as hard here, because these are the people the cover is MADE of. A
    /// background patron who read as interesting would turn a room of eighty strangers into eighty leads.
    ///
    /// <para>Deliberately distinct strings from the named cast's, so <see cref="CanteenTable.WhoIs"/> can
    /// never match one of them and hand a stranger the Hand's conversation.</para>
    ///
    /// <para>#792 · Projected from <see cref="Faces"/> rather than typed a second time. Declared BELOW it
    /// on purpose: a static field initialiser runs in the order it is written, and this list read from an
    /// array that had not been built yet would be ten nulls at every call site in the game.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> StrangerPlates = PlatesOf(Faces);

    /// <summary>#792 · Is the crowd's <paramref name="face"/>th patron a CONVERSATION rather than somebody
    /// on their own? The one source; <see cref="TableSeat.Talking"/> is this answer carried to wherever the
    /// top is drawn or pressed.</summary>
    public static bool StrangerTalks(int face) =>
        face >= 0 && face < Faces.Length && Faces[face].Talking;

    /// <summary>#823 · How many people the crowd's <paramref name="face"/>th patron IS. The one source, in the
    /// same shape as <see cref="StrangerTalks"/>; <see cref="TableSeat.Heads"/> is this answer carried to
    /// wherever the top is drawn or pressed, and <see cref="TableSeat.Free"/> is the chairs it leaves.</summary>
    public static int StrangerHeads(int face) =>
        face >= 0 && face < Faces.Length ? Faces[face].Heads : 0;

    /// <summary>
    /// #751 · WHAT A STRANGER SAYS. Fourteen barks, drawn seeded per patron per watch — so the same table on
    /// the same shift always says the same thing, and the hall as a whole says a different fourteen things
    /// every shift.
    ///
    /// <para>Two registers, deliberately braided. Twelve are the working day of people paid not to ask
    /// (#751's own pool); the last two are the FANCY register — #601's funding trail overheard rather than
    /// stated, because a suspiciously nice canteen on a nowhere rock is money that does not mind being seen
    /// feeding contractors, only being asked.</para>
    ///
    /// <para>Not one of them explains anything, and the guard that greps them walks THIS list.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> Barks =
    [
        "Two more runs and I'm wintering somewhere with weather.",
        "They pay on the nail here. You don't ask what the nail's in.",
        "Cage crew again? Wear the good gloves.",
        "I had a mate went down-contract. Sends money home regular. Never writes.",
        "The coffee's the same on every rock. That's either comforting or it isn't.",
        "Don't sit near the board on rota day unless you want work.",
        "Somebody asked the counter what's below. Funniest thing — nobody remembers who.",
        "Freight doesn't weigh what the manifest says. Freight never weighs what the manifest says.",
        "First week? It shows. Sit down, it wears off.",
        "The lift's polite. That's more than I can say for the last three sites.",
        "You get used to the hum. Then one day it stops and you find out you liked it.",
        "Keep your name simple here. They'll shorten it anyway.",
        "Real coffee. On a rock like this. Somebody's writing it off against something.",
        "Table linen on a freight stop. I stopped asking the questions I like the answers to.",
    ];

    /// <summary>#751 · Every stranger plate and every bark, for the canon grep — the same discipline the
    /// named cast's <see cref="AllProse"/> keeps, and a separate call so neither list's guard can be
    /// silently made vacuous by the other growing.</summary>
    public static IEnumerable<string> AllStrangerProse()
    {
        foreach (string p in StrangerPlates)
        {
            yield return p;
        }
        foreach (string b in Barks)
        {
            yield return b;
        }
    }

    /// <summary>#751 · Which bark this patron has this watch. Seeded on (site, top, watch) and nothing else,
    /// so it is stable while you stand there, different next shift, and a guard can measure that every one
    /// of the fourteen is actually reachable rather than assuming it.</summary>
    public static int BarkIndex(string bodyId, int tableIndex, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return DiceRule.Roll(
            DiceRule.Seed($"hive:hall:bark:{bodyId}:{tableIndex}", watch), Barks.Count).Face - 1;
    }

    /// <summary>#751 · How many of the hall's tops have somebody at them this watch — the named regulars
    /// included. The number the room's whole mood comes out of.</summary>
    public static int OccupiedTops(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch = 0)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int taken = 0;
        foreach (TableSeat top in Tables(bodyId, level, amenity, watch))
        {
            if (top.Taken)
            {
                taken++;
            }
        }
        return taken;
    }

    /// <summary>WHICH TOPS THE CROWD IS AT, and which face each of them wears. Keyed by top ordinal, exactly
    /// like <see cref="Seating"/>, and it skips whatever the named regulars already have.</summary>
    /// <param name="bill">#823 · What each top SEATS, so the deal can respect the furniture. A whole crew of
    /// four cannot be dealt to a two-top: the party would be sitting in each other's laps, which is the exact
    /// picture the owner sent back from the canteen.</param>
    private static Dictionary<int, int> Crowd(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch,
        IEnumerable<int> takenByTheCast, IReadOnlyList<int> bill)
    {
        var crowd = new Dictionary<int, int>();

        // Halls only, upper canteen only, top floor only. The middle clause is the one with teeth: the
        // STAFF MESS is hall-class too (#751's second customer) and it must stay empty on every watch
        // forever — its whole identity is #743's sentence at architectural scale, "the shift has not come".
        if (amenity.Hall is null || !PeopleSitHere(bodyId, level, amenity))
        {
            return crowd;
        }

        int tops = amenity.Tables.Count;
        if (tops <= 0)
        {
            return crowd;
        }

        double fill = WatchFill[(int)(((watch % WatchFill.Count) + WatchFill.Count) % WatchFill.Count)];
        int want = (int)Math.Round(tops * fill, MidpointRounding.AwayFromZero);

        var used = new List<int>(takenByTheCast);
        int seatedByCast = used.Count;
        want = Math.Clamp(want - seatedByCast, 0, tops - seatedByCast);

        for (int i = 0; i < want; i++)
        {
            int table = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:hall:top:{bodyId}:{i}", watch), tops).Face - 1,
                tops, used);
            int face = DiceRule.Roll(
                DiceRule.Seed($"hive:hall:face:{bodyId}:{table}", watch), StrangerPlates.Count).Face - 1;

            // #823 · THE FURNITURE HAS A VETO. The die names a face; the top says whether it will take it.
            if (FaceThatFits(face, table < bill.Count ? bill[table] : SeatCounts[0]) is { } fits)
            {
                crowd[table] = fits;
            }
        }

        return crowd;
    }

    /// <summary>
    /// #823 · THE FIRST FACE AT OR AFTER THE ROLLED ONE THAT THIS TOP CAN SEAT, or null if none of them can.
    ///
    /// <para>Same skip-forward discipline as <see cref="PickUnused"/> and the hall's seat bill, and for the
    /// same reason: a re-roll loop on a seeded die is how a generator stops being deterministic. A hall's
    /// twos take the ones and the pairs; the fours and sixes take the crews. Null is the honest answer for a
    /// top nobody could sit at — the caller leaves it empty rather than seating four people on two chairs —
    /// and no top the game builds is that small today (SeatCounts starts at 2 and half the faces are one
    /// person), which is a thing the guard measures rather than a thing this comment promises.</para>
    /// </summary>
    private static int? FaceThatFits(int wanted, int seats)
    {
        for (int step = 0; step < Faces.Length; step++)
        {
            int candidate = ((wanted + step) % Faces.Length + Faces.Length) % Faces.Length;
            if (Faces[candidate].Heads <= seats)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Take the rolled index, or the next free one after it. Two people on one chair and one person
    /// said twice are the same bug wearing different clothes, and a re-roll loop on a seeded die is how a
    /// generator stops being deterministic.</summary>
    private static int PickUnused(int wanted, int count, List<int> used)
    {
        for (int step = 0; step < count; step++)
        {
            int candidate = (wanted + step) % count;
            if (!used.Contains(candidate))
            {
                used.Add(candidate);
                return candidate;
            }
        }

        used.Add(wanted);
        return wanted;
    }
}
