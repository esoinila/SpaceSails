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
    /// <param name="Index">Its ordinal in the amenity's own table list, so a caller can key state off it.</param>
    /// <param name="X">Centre, in the surface's own coordinates.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="Seats">2, 4 or 6 — furniture, seeded off the building and never off the shift.</param>
    /// <param name="Plate">Who is at it, or null for an empty table.</param>
    /// <param name="Line">What they say, or null.</param>
    public readonly record struct TableSeat(
        int Index, double X, double Y, int Seats, string? Plate, string? Line)
    {
        /// <summary>Somebody is at this table.</summary>
        public bool Taken => Plate is not null;

        /// <summary>Chairs nobody is in. One regular per top today (<see cref="Sitting"/> never doubles
        /// up), so this is the seat count less at most one — and it is the number "ask to join" reads.</summary>
        public int Free => Seats - (Taken ? 1 : 0);
    }

    /// <summary>
    /// #746 · How many a given round top seats.
    ///
    /// <para>Seeded off the SITE, the room's use and the top's ordinal — and deliberately NOT off the watch.
    /// A canteen does not re-furnish itself every shift, and a table that seated six at breakfast and two at
    /// supper would be the picture and the sim disagreeing about a thing the player can count.</para>
    /// </summary>
    public static int SeatsAt(string bodyId, UndergroundComplex.Amenity amenity, int tableIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ulong seed = DiceRule.Seed(
            $"hive:canteen:seats:{bodyId}:{(int)amenity.Use}:{tableIndex}", 0);
        return SeatCounts[DiceRule.Roll(seed, SeatCounts.Count).Face - 1];
    }

    /// <summary>
    /// #746 · EVERY ROUND TOP IN THE ROOM, with its seats and its occupancy — the one fact the renderer
    /// draws and the one fact the [E] press asks. Same frozen watch (#709), so the chair on the screen and
    /// the chair the game offers you are the same chair.
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="amenity">The room, as Core carved it (#707).</param>
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

        var tops = new List<TableSeat>(amenity.Tables.Count);
        for (int i = 0; i < amenity.Tables.Count; i++)
        {
            (double tx, double ty) = amenity.Tables[i];
            bool sat = who.TryGetValue(i, out int cast);
            tops.Add(new TableSeat(
                i, tx, ty, SeatsAt(bodyId, amenity, i),
                sat ? Cast[cast].Plate : null,
                sat ? Cast[cast].Line : null));
        }

        return tops;
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
        // in it are #618's question, not this one; the washroom is the one amenity nobody sits down in.
        if (amenity.Use != UndergroundComplex.Comfort.UpperCanteen)
        {
            return seating;
        }

        // And only on the floor the owner put them on. UpperCanteen is only ever carved on the top
        // pressurised floor today, so this is belt and braces — deliberately, because the day somebody
        // carves a second canteen the B1 ruling must not quietly stop being true.
        if (UndergroundComplex.TopPressurisedFloor(bodyId) != level)
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
