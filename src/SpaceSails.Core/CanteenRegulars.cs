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
///
/// <para><b>#251 · THIS FILE IS THE REGISTERS; THE REST OF THE CLASS IS FIVE PARTIALS.</b> What is dealt
/// FROM here — the cast and its plates, the seat counts, the watch's fill, the strangers' faces and their
/// barks — stays in this file; what is decided WITH it moved out by concern:
/// <c>CanteenRegulars.Sitting.cs</c> (who is at which bench this watch), <c>CanteenRegulars.Tops.cs</c>
/// (what a top is and where its chairs are), <c>CanteenRegulars.Tables.cs</c> (how many tops a room gets
/// and where they stand), <c>CanteenRegulars.Rota.cs</c> (the shift turning over) and
/// <c>CanteenRegulars.Crowd.cs</c> (dealing the cover crowd).</para>
///
/// <para>The cut is where the initializers allow rather than where the concerns are, and that is #1163's
/// static-class law: static field initializers of a partial class run in the order the compiler reads the
/// FILES, not the order a reader sees. <see cref="StrangerPlates"/> is literally <c>PlatesOf(Faces)</c> —
/// put those two in different files and the glob can hand one to the compiler before the other, with a
/// clean build and no warning. So every <c>static readonly</c> in this class — <c>Cast</c>,
/// <see cref="SeatCounts"/>, <see cref="WatchFill"/>, <c>Faces</c>, <see cref="StrangerPlates"/>,
/// <see cref="Barks"/> — is declared HERE, in its original order, and no partial of it declares one at
/// all.</para>
/// </summary>
public static partial class CanteenRegulars
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

        // ── #1063 · AND THE ONE WHO IS NOT ALWAYS HERE ───────────────────────────────────────────────────
        //
        // THE MASON ON THE JOB. Last in this array and dealt out of a pool that stops one short of him, so on
        // every ground nobody has been past a seam of — which is every ground in almost every world — this
        // cast is the ten it has always been, dealt by the same dice against the same length, and not one
        // person in the game moves seat.
        //
        // He is the dullest man in the room and he passes the register test hardest: he is a tradesman
        // reciting what he wrote on a form. He is not mysterious, he is not hiding anything, and he is not
        // lying — HE MEANS IT, AND HE FILED IT, AND THAT IS THE WHOLE TESTIMONY (#1063). The horror is
        // entirely in the fact that a filing phrase is all there is, and he is the only man alive who was
        // standing in front of the thing.
        new(Burial.MasonPlate, Burial.MasonLine),

        // ── #1074 beat 4 · …AND THE TWO WHO ARE ONLY HERE WHERE THE OFFICE HAS BEEN ──────────────────────
        //
        // CAREER-COST NPCs, on the mason's own law one rung further along the array: the ordinary deal stops
        // THREE short of the end now rather than one, so a ground nobody has stopped seats the same ten
        // people, dealt by the same dice against the same length, and the room does not move by one
        // character. Their order here is the order they take chairs on a stopped ground (see Seating).
        //
        // They pass the register test as hard as he does, and neither is a quest-giver in a hat. One is a man
        // who believes what he was told and is telling a stranger the truth as he has it; the other is a
        // woman who has not moved a mug. Neither is mysterious, neither is hiding anything, and neither
        // knows one thing the captain does not — which is the whole of it, because a working closed, a name
        // went into a register, and this is the entire remainder.
        new(CareerCost.ColleaguePlate, CareerCost.ColleagueLine),
        new(CareerCost.MugPlate, CareerCost.MugLine),
    ];

    /// <summary>How many authored regulars exist. Public so a guard can pin the catalog's size without
    /// reaching into it.</summary>
    public static int CastSize => Cast.Length;

    /// <summary>#1063/#1074 · How many of the cast the ORDINARY seeded rota may deal — everybody but the
    /// mason and the two career-cost regulars, which is why a site nobody has opened or stopped seats exactly
    /// the people it always seated. <b>This number must never change</b>: it is the length the dice are
    /// rolled against, and moving it re-deals every canteen in every world. Public so a guard can pin it
    /// without reaching into the array (the incidental "cast and catalogue are the same length" it replaces
    /// stopped being true the day a regular arrived who pins no paper).</summary>
    public static int OrdinaryCastSize => OrdinaryCast;

    /// <summary>#1063 · Which of the cast is the mason — the first of the ones the ordinary deal stops short
    /// of. Found by his plate rather than written as an index, for <c>CanteenBoard.RosterNotice</c>'s reason:
    /// a beat pointed at a row that does not exist should throw at the first call rather than go quietly
    /// missing on some worlds forever.</summary>
    private static int Mason => IndexOfPlate(Burial.MasonPlate);

    /// <summary>#1074 · …the colleague who was asked, and</summary>
    private static int Colleague => IndexOfPlate(CareerCost.ColleaguePlate);

    /// <summary>#1074 · …the one who keeps the mug. Same lookup, same reason.</summary>
    private static int MugKeeper => IndexOfPlate(CareerCost.MugPlate);

    /// <summary>#1063/#1074 · …and how many of them the ordinary seeded rota may deal. Everybody but the
    /// mason and the two of #1074's, which is why a site nobody has opened seats exactly the people it always
    /// seated.</summary>
    private static int OrdinaryCast => Cast.Length - 3;

    /// <summary>Which of the cast wears this plate. Throws rather than answering -1: every caller is a beat
    /// that has just named somebody it must be able to seat.</summary>
    private static int IndexOfPlate(string plate)
    {
        for (int i = 0; i < Cast.Length; i++)
        {
            if (string.Equals(Cast[i].Plate, plate, StringComparison.Ordinal))
            {
                return i;
            }
        }
        throw new InvalidOperationException($"the canteen has nobody plated \"{plate}\"");
    }

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
        // #783/#941 · This one said "Table linen on a freight stop"; the owner's no-tablecloths ruling on
        // #759 is an ART spec — the hall's tables are bare steel — so the fancy register points at what is
        // actually in the picture, the brass, and the prose follows the picture. Owner-authored, verbatim.
        "Brass pillars on a freight stop. I stopped asking the questions I like the answers to.",
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
}
