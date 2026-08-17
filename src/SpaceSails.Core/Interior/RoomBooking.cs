using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #770 · BOOK A ROOM AT THE COUNTER — the grammar the ring already had the rooms for.
///
/// <para>Owner's idea, filed on the issue: the block rents its park-facing suites by the watch, and the deal
/// that gets done in one of them is done ACROSS A TABLE with somebody sitting opposite. The rooms shipped
/// with #816 (<c>NEGOTIATION ROOM · BOOK AT THE COUNTER</c> is one of the block's own park-view plates), the
/// seats with #817 and the long table with #868/#881. <b>Every noun was already on the floor and there was no
/// verb.</b> A plate that names a counter you cannot ask is the building advertising a service the game does
/// not implement, which is one step worse than not having the room at all.</para>
///
/// <h3>What this file is, and what it deliberately is not</h3>
///
/// <para>It is a PRICE, a PICK, a PLATE, a DOOR LAW, a NOTE and a SCENE. It is not an economy: nothing here
/// signs a contract, moves cargo or changes a standing (#760's "relationships negotiated in person" is v2 and
/// is named as such in the issue). The one deal move puts the captain's own papers on the table over #746's
/// grammar, and what comes of that is the field book's business and the delegation's ledger entry.</para>
///
/// <para><b>No state lives here and none lives on the page.</b> A booking is an ENTRY — #715's per-entity
/// ledger, written under <see cref="LedgerId"/> in the machine-readable shape <see cref="TheKeep.AskedEntry"/>
/// established (<see cref="BookedEntry"/> / <see cref="TryReadBooking"/>). That is not a trick to dodge
/// #905's frame ledger: it is the honest model. The counter's book is the thing that remembers who had which
/// room on which watch, the game already has a book of exactly that shape, and a booking that lived in a
/// field on the component would be a fact about the world kept somewhere the world cannot read.</para>
///
/// <para>Pure and deterministic: no clock, no <c>Random</c>, no world. The caller freezes the watch (#709)
/// and hands over the ring it drew and the rota it drew it with.</para>
///
/// <para>§13.8 holds. Every line below says what a ROOM is — a booking, a price, a window — and not one of
/// them says what the facility is for. The delegation is somebody with a boring reason to be in the building
/// who has agreed to sit down with you for twenty minutes, and the horror is a thing the game never
/// states.</para>
/// </summary>
public static class RoomBooking
{
    /// <summary>The glyph a booking wears — a window, because the window is what the price is for.</summary>
    public const string Glyph = "🪟";

    /// <summary>Who the counter keeps its booking book under. <see cref="TheKeep.LedgerId"/>'s own idiom: a
    /// LEDGER key and never a display name, so the entity survives the keep going off shift — the book is the
    /// counter's, not his.</summary>
    public const string LedgerId = "THE COUNTER BOOK";

    /// <summary>…and what the ledger calls it in a list.</summary>
    public const string LedgerName = "the counter's booking book";

    // ── WHAT IT COSTS ─────────────────────────────────────────────────────────────────────────────────
    //
    // TWO PRICES, BESIDE THE ROUND'S. CounterService already carries the two rates this counter charges (a
    // glass and a round for the room); a room for the watch is the third thing the same counter sells, and it
    // is priced here rather than typed into a button so the label, the enabled-ness and the debit are one
    // number. Both FLAGGED for the owner's tuning.

    /// <summary>What a negotiation room costs for one watch. Deliberately more than a round for the room
    /// (<see cref="CounterService.RoundRate"/>) and less than an evening of them: it is a thing a captain can
    /// decide to do rather than a thing they save up for.</summary>
    public const int Price = 24;

    /// <summary>…and what the BIG one costs. The status marker, priced as one — #775's amenity gradient
    /// arriving at the till. Nothing anywhere says why it is worth it.</summary>
    public const int BigRoomPrice = 40;

    /// <summary>The price of the room you are actually getting.</summary>
    public static int PriceOf(bool big) => big ? BigRoomPrice : Price;

    // ── WHICH ROOMS THESE ARE ─────────────────────────────────────────────────────────────────────────

    /// <summary>Is this one of the block's negotiation rooms? Off the PLATE, which is the only thing that
    /// says what a room is for — <see cref="RingOffice.IsWashroom"/>'s own one question, asked in one place so
    /// a guard, the counter and the deck cannot each answer it their own way.</summary>
    public static bool IsANegotiationRoom(in UndergroundComplex.RingRoom room) =>
        room.Plate.Contains("NEGOTIATION", StringComparison.Ordinal);

    /// <summary>
    /// #770 · IS THIS THE GOOD ONE — the wide suite with the service strip behind it?
    ///
    /// <para><b>This is where the issue's own draft was wrong, and the code said so.</b> The spec asked for
    /// the PARK WINDOW to be the status marker and for the counter to offer "the glass room at a higher price
    /// when one is free". There is no such fork in this building: the block only ever stencils
    /// <c>NEGOTIATION ROOM</c> on a room that has the view (<c>UndergroundComplex.RingBox</c> reaches for
    /// <see cref="UndergroundComplex.ParkViewPlates"/> only when <c>view &amp;&amp; side != Far</c>), so
    /// "with glass" would have selected every negotiation room in the game and "without" none of them — a
    /// threshold that selects EVERYTHING, which is this repository's fifth named bug class and the exact
    /// shape of a guard that cannot tell pass from fail.</para>
    ///
    /// <para>So the marker is the one the building already draws the gradient with: <b>frontage</b>.
    /// <see cref="RingOffice.IsBigSuite"/> is what earns a room its kitchenette, its two staff WCs and its
    /// privacy booths (#817/#821) — the ring's 40 du bands do and its 19 du end blocks do not. Both sides of
    /// that fork exist on floors the game actually generates, which a guard measures rather than trusts.</para>
    /// </summary>
    public static bool IsTheBigRoom(in UndergroundComplex.RingRoom room) => RingOffice.IsBigSuite(in room);

    /// <summary>
    /// WHICH ROOM THE COUNTER GIVES YOU — of the kind you asked for, free, and nothing else.
    ///
    /// <para>STRICT, and that is the honest version: a button that says 24 cr must not hand over the 40 cr
    /// room and charge for it, and a button that says 40 must not quietly sell you the small one. A kind that
    /// is not free on this floor comes back null and the counter refuses OUT LOUD (#603), which is how a
    /// captain learns what this building has.</para>
    ///
    /// <para>A LOOKUP AND NOT A DECISION. The ring is the one the deck was drawn from, walked in its own laid
    /// order (near, far, west, east — <see cref="UndergroundComplex.RingRoom.Number"/>), so the room the
    /// counter names is a room the captain can walk to and read the plate of. Nothing here measures a
    /// rectangle: §13.15, and this project has set a captain down inside a wall twice by letting a caller do
    /// arithmetic about a room it did not carve.</para>
    /// </summary>
    /// <param name="ring">The park's own frontage — <see cref="UndergroundComplex.Park.Frontage"/>.</param>
    /// <param name="wantBig">Whether the captain asked for the wide one.</param>
    /// <param name="taken">Room numbers already booked on this floor this watch. Empty on every floor of
    /// every site today, and a parameter all the same, because "a free one" is the sentence the verb is
    /// written on and a law about a free room cannot be written against a set nobody keeps.</param>
    public static int? RoomYouGet(
        IReadOnlyList<UndergroundComplex.RingRoom> ring, bool wantBig,
        IReadOnlyCollection<int>? taken = null)
    {
        ArgumentNullException.ThrowIfNull(ring);

        foreach (UndergroundComplex.RingRoom room in ring)
        {
            if (IsANegotiationRoom(in room) && IsTheBigRoom(in room) == wantBig
                && (taken is null || !taken.Contains(room.Number)))
            {
                return room.Number;
            }
        }

        return null;
    }

    /// <summary>Is the room you got the big one? Asked of the ring rather than remembered, so the plate, the
    /// price and the wall are one answer.</summary>
    public static bool BigOn(IReadOnlyList<UndergroundComplex.RingRoom> ring, int number)
    {
        ArgumentNullException.ThrowIfNull(ring);
        foreach (UndergroundComplex.RingRoom room in ring)
        {
            if (room.Number == number)
            {
                return IsTheBigRoom(in room);
            }
        }
        return false;
    }

    // ── THE PLATE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// WHAT THE DOOR SAYS ONCE IT IS YOURS. The block's own register (§13.8) — it names the room, the state
    /// and the shift, and nothing about what the facility is for.
    ///
    /// <para>The tier is on it because the plate is where the status marker LANDS: a captain walking the
    /// frontage can read which of the two prices somebody paid, and nothing in the game ever remarks on that.
    /// #775's amenity gradient, one more time, as stencil.</para>
    /// </summary>
    public static string BookedPlate(long watch, bool big) =>
        big
            ? $"{PlateHead}{watch}{LongRoomTail}"
            : $"{PlateHead}{watch}";

    /// <summary>Everything a booked plate starts with. One string, so the stencil and the parser cannot be
    /// edited apart.</summary>
    public const string PlateHead = "NEGOTIATION ROOM · BOOKED · WATCH ";

    /// <summary>…and what the wide one adds.</summary>
    public const string LongRoomTail = " · LONG ROOM";

    /// <summary>
    /// #770 · READING THE PLATE BACK OFF THE PLAN.
    ///
    /// <para>Published because the SEAT asks it. A captain sitting down at the long table has to know whether
    /// this is the room they paid for, and the honest place to ask is the plan the room was drawn from —
    /// <c>CabinetStage</c>'s own discipline one room along (<i>"asked of the one set the DECK is drawn from
    /// and never of a flag captured when the captain sat down"</i>). The stencil beside the door IS the fact,
    /// so the drawn room and the pressed room cannot be two rooms — which is this project's third named bug
    /// class, closed by construction rather than by a comment.</para>
    /// </summary>
    public static bool TryReadPlate(string? plate, out long watch, out bool big)
    {
        (watch, big) = (0L, false);
        if (plate is null || !plate.StartsWith(PlateHead, StringComparison.Ordinal))
        {
            return false;
        }

        string rest = plate[PlateHead.Length..];
        if (rest.EndsWith(LongRoomTail, StringComparison.Ordinal))
        {
            (rest, big) = (rest[..^LongRoomTail.Length], true);
        }
        return long.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out watch);
    }

    // ── THE BOOKING ITSELF ────────────────────────────────────────────────────────────────────────────

    /// <summary>One room, held for one watch, by one person.</summary>
    /// <param name="Level">The floor it is on, so a booking on B1 is not a booking on B6.</param>
    /// <param name="Room">Its <see cref="UndergroundComplex.RingRoom.Number"/>.</param>
    /// <param name="Big">Whether it is the wide one with the service strip — and therefore which price was
    /// paid. See <see cref="IsTheBigRoom"/>.</param>
    /// <param name="Watch">The frozen shift (#709) it was booked FOR. It expires by this and by nothing
    /// else: see <see cref="HonoursTheDoor"/>.</param>
    /// <param name="Booker">Who holds it. One string, because there is exactly one person in this game who
    /// can walk up to a counter — and it is a parameter rather than an assumption so the door law can be
    /// asked about somebody else and answer no.</param>
    public readonly record struct Booking(int Level, int Room, bool Big, long Watch, string Booker)
    {
        /// <summary>What is stencilled beside its door while it is held.</summary>
        public string Plate => BookedPlate(Watch, Big);

        /// <summary>What it cost.</summary>
        public int Paid => PriceOf(Big);
    }

    /// <summary>Who books rooms. The captain, and — today — nobody else in the building has a purse the game
    /// models. Named so the door law reads as a comparison rather than as a constant true.</summary>
    public const string TheCaptain = "the captain";

    /// <summary>
    /// #770 · THE DOOR HONOURS THE BOOKER, AND ONLY UNTIL THE WATCH TURNS.
    ///
    /// <para>Two clauses and they fail in opposite directions, which is why they are one expression rather
    /// than two calls a caller could get half right. A door that honoured anybody is a door that was never
    /// booked; a door that honoured the booker forever is a room the building has given away. The building
    /// rents by the watch and takes it back without saying anything.</para>
    /// </summary>
    /// <param name="booking">What the counter wrote down.</param>
    /// <param name="comer">Whoever is at the leaf.</param>
    /// <param name="watch">The shift it is now, frozen (#709) and never a clock.</param>
    public static bool HonoursTheDoor(in Booking booking, string comer, long watch) =>
        watch == booking.Watch
        && string.Equals(comer, booking.Booker, StringComparison.Ordinal);

    /// <summary>Is this booking live on this floor on this shift? The question the COUNTER asks of its own
    /// book, which keeps every booking ever made and needs to know which one is now.</summary>
    public static bool HeldOn(in Booking booking, int level, long watch) =>
        booking.Level == level && booking.Watch == watch;

    /// <summary>Is this ROOM the one held on this watch? The question the DECK asks, once per rebuild, so the
    /// plate on the plan and the leaf under it cannot come to two answers.</summary>
    public static bool HoldsThisRoom(in Booking booking, int level, int room, long watch) =>
        HeldOn(in booking, level, watch) && booking.Room == room;

    // ── THE DOOR, AS #758 ALREADY KNOWS HOW TO DRAW ONE ───────────────────────────────────────────────
    //
    // A booked room is a CABINET-CLASS QUIET SPACE (the issue's fifth clause), so the curtain/door law it
    // already has is the one it gets — the same set of dogged leaves, the same one button on the strip, the
    // same leak die behind the cloth. What it needs is an ORDINAL of its own: CabinetPrivacy keys a leaf on
    // (floor, number), the hall's cabinets are numbered from one, and a booked ring room dealt the same small
    // integer would dog a cabinet on the far side of the building. RingOffice.ApproachOrdinalBase's own
    // reason, one system along.

    /// <summary>Where a booked room's leaf ordinals start, past anything the hall's cabinet row will ever
    /// reach. Far enough that no block will grow into the gap.</summary>
    public const int LeafOrdinalBase = 400;

    /// <summary>This room's leaf, as <see cref="CabinetPrivacy.Key"/> counts leaves.</summary>
    public static int LeafOrdinal(int room) => LeafOrdinalBase + room;

    /// <summary>…and reading one back. False for a hall cabinet, which is the whole point of the base.</summary>
    public static bool IsABookedLeaf(int leaf) => leaf > LeafOrdinalBase;

    /// <summary>Which room a booked leaf belongs to.</summary>
    public static int RoomOfLeaf(int leaf) => leaf - LeafOrdinalBase;

    /// <summary>#770/#758 · …and what comes back later, in somebody else's mouth, from somebody with no way
    /// of knowing it. <see cref="CabinetPrivacy.BarkThatKnows"/>'s own beat, naming a ROOM rather than a
    /// cabinet — a leak that named the wrong fixture would be the bark and the building disagreeing.</summary>
    public static string BarkThatKnows(int room) =>
        $"Room {room} on the green, was it? No — none of my business. It never is.";

    /// <summary>
    /// #770/#758 · WHAT THE COUNTER WRITES DOWN WHEN YOU SHUT THE DOOR OF A ROOM YOU PAID FOR.
    ///
    /// <para><see cref="CabinetPrivacy.WhoWasInsideNote"/> word for word, with the room named as what it is.
    /// A booked suite is not cabinet three off the back of the hall, and a book that said it was would be
    /// this repo's oldest fault — the sentence reporting a different world than the sim.</para>
    /// </summary>
    public static string DoggedTheDoorNote(int room, long watch) =>
        TheKeep.KeptWatch(watch)
            ? $"Dogged the door of negotiation room {room} from inside. Behind the counter the keep looked " +
                "up at the sound and wrote something down without hurrying."
            : $"Dogged the door of negotiation room {room} from inside. There was nobody behind the counter " +
                "to look up. It was written down anyway.";

    // ── THE COUNTER'S LONG MEMORY ─────────────────────────────────────────────────────────────────────
    //
    // #715's per-entity ledger is where a booking LIVES — TheKeep.AskedEntry's own shape, all integers, so
    // nothing a person typed can ever end up inside a field the parser splits on. The book keeps every
    // booking ever made; HonoursTheDoor is what makes only one of them true right now.

    /// <summary>The tag every booking entry starts with.</summary>
    public const string BookTag = "room-booked";

    /// <summary>One line of the counter's book, in the shape a machine wrote it.</summary>
    public static string BookedEntry(in Booking booking) =>
        $"{BookTag}:{booking.Level}:{booking.Room}:{(booking.Big ? 1 : 0)}:{booking.Watch}";

    /// <summary>…and reading one back. False for anything that is not one of ours, so the same ledger can
    /// hold the keep's asks and the counter's bookings without either learning about the other.</summary>
    public static bool TryReadBooking(string? entry, string booker, out Booking booking)
    {
        booking = default;
        if (entry is null || booker is null)
        {
            return false;
        }

        string[] parts = entry.Split(':');
        if (parts.Length != 5 || !string.Equals(parts[0], BookTag, StringComparison.Ordinal))
        {
            return false;
        }
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int level)
            || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int room)
            || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int big)
            || !long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out long watch))
        {
            return false;
        }

        booking = new Booking(level, room, big != 0, watch, booker);
        return true;
    }

    /// <summary>
    /// #770/#758/#781 · WHAT THE COUNTER WRITES DOWN — and who, if anybody, was behind it to write it.
    ///
    /// <para><see cref="CabinetPrivacy.WhoWasInsideNote"/>'s own idiom, one fixture along: <b>the FACT is
    /// filed on both watches and only the SENTENCE forks.</b> The keep works the living watches (#781); on
    /// the dead ones the counter serves itself, which on this rock is somehow worse — and the self-service
    /// version of being written down is that you write it yourself, in a book left out on the counter, and
    /// nobody checks.</para>
    /// </summary>
    public static string WhoBookedNote(int room, long watch) =>
        TheKeep.KeptWatch(watch)
            ? $"Took negotiation room {room} for the watch. It was the keep who looked it up; he wrote a " +
                "name against it and did not ask what it was for."
            : $"Took negotiation room {room} for the watch. There was nobody behind the counter to look " +
                "up. The book is out on the counter — you wrote your own name in it.";

    // ── THE VERB AT THE COUNTER ───────────────────────────────────────────────────────────────────────

    /// <summary>What the counter's button says. The price is IN the label for the reason the round's is: a
    /// captain must know what a press costs before pressing it.</summary>
    public static string BookLabel(bool big) =>
        big
            ? $"{Glyph} Book the long room · {BigRoomPrice} cr"
            : $"{Glyph} Book a negotiation room · {Price} cr";

    /// <summary>…and what it says on the way past. Two counters, two sentences, and the difference is
    /// whether there is anybody to ask (#781).</summary>
    public static string BookHint(bool selfService) =>
        selfService
            ? "The book is out on the counter. Write your own name in it — nobody is going to."
            : "Ask for a room for the watch. He will not ask what it is for.";

    /// <summary>What the counter says back when it cannot. Said out loud (#603), never a control that does
    /// nothing.</summary>
    public const string NothingToBookLine =
        "There is no room on this floor to hold. The book on the counter has nothing in it that would be a "
        + "room, which is either an oversight or the point.";

    /// <summary>…and when the purse is short. The counter's own flat register.</summary>
    public static string ShortLine(int price) =>
        $"A room for the watch is {price} cr, and the purse will not cover it.";

    /// <summary>…and when you already have one. A second room is not a thing the counter will sell you, and
    /// saying so is more useful than a button that quietly does nothing.</summary>
    public static string AlreadyHeldLine(int room) =>
        $"Room {room} is already yours for this watch. The book will not write you a second one.";

    /// <summary>What the counter says when it hands the key over.</summary>
    public static string TookItLine(int room, bool big) =>
        big
            ? $"Room {room} is yours until the watch turns. It is one of the long ones down the green side, "
                + "which costs what it costs and is not remarked upon."
            : $"Room {room} is yours until the watch turns. A table, six chairs, and a door that shuts.";

    /// <summary>…and when the kind you asked for is not free on this floor. Said out loud (#603), and it
    /// names the kind rather than shrugging — a captain who is told nothing learns nothing about the
    /// building.</summary>
    public static string NoRoomOfThatKindLine(bool big) =>
        big
            ? "There is no long room free on this floor this watch. The book has the small ones and that is "
                + "all it has."
            : "There is no small room free on this floor this watch. What is left is the long one, at the "
                + "long one's price.";

    // ── WHO IS SITTING OPPOSITE ───────────────────────────────────────────────────────────────────────
    //
    // A DELEGATION IS SOMEBODY WHO WAS ALREADY IN THE BUILDING. Owner's grammar for the room: you book it and
    // you put something to somebody across a table. So the party opposite is drawn from the hall's own rota
    // (CanteenRegulars) off the watch you booked ON — a room booked on a shift with nobody in the canteen is
    // a room with nobody in it, and being told that in an eighty-seat building IS the event (#757's law).
    //
    // Deterministic and re-derivable: the pick is a pure function of (site, floor, watch) and the rota the
    // deck was drawn from, so nothing about who is opposite has to be remembered anywhere. That is what lets
    // the booking be five integers in a book.

    /// <summary>Which of the people in this hall walked over. Null when the hall is empty this watch, which
    /// is a reachable and deliberate outcome — see <see cref="RoomIsYoursAndEmptyLine"/>.</summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="watch">The shift, frozen (#709).</param>
    /// <param name="rota">Who is in this hall on it — <see cref="CanteenRegulars.Sitting"/>'s own list,
    /// handed over rather than re-derived, so the person across the table is a person the captain could have
    /// walked up to instead.</param>
    public static CanteenRegulars.Seated? TheDelegation(
        string bodyId, int level, long watch, IReadOnlyList<CanteenRegulars.Seated>? rota)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (rota is null || rota.Count == 0)
        {
            return null;
        }

        // TWO THINGS HAVE TO BE TRUE AND THEY ARE DIFFERENT KINDS OF TRUE — the counter's own stool law
        // (TheStools.SomebodyTurns) said back at a table. Somebody has to be in this hall on this shift (the
        // room), and they have to have actually come up (the dice). THE ODDS ARE THE HALL'S OWN —
        // SittingAlone.FacesThatBringSomebody, unchanged and uncopied, because "does this room have anybody
        // for you tonight" is one question however you asked it — and the DIE is the booking's, so a room
        // taken on a busy shift is likelier to have somebody across the table and a dead one likelier not.
        //
        // Both answers are the feature. A room you paid for with nobody in it is #757's own law arriving
        // where it costs the most: nothing happening is an OUTCOME here (RoomIsYoursAndEmptyLine), and being
        // told so after handing over the coin is the whole of what this building is like.
        DiceRoll came = DiceRule.Roll(
            DiceRule.Seed($"hive:booking:came:{bodyId}", level, watch), SittingAlone.Faces);
        if (came.Face > SittingAlone.FacesThatBringSomebody(watch))
        {
            return null;
        }

        ulong seed = DiceRule.Seed($"hive:booking:delegation:{bodyId}", level, watch);
        return rota[(int)(seed % (ulong)rota.Count)];
    }

    // ── WHAT IS SAID ACROSS IT ────────────────────────────────────────────────────────────────────────

    /// <summary>Where you are, while you are in one.</summary>
    public const string Setting = "the long table in a booked negotiation room";

    /// <summary>Who the captain reads as on the strip when the room is theirs and empty.</summary>
    public const string EmptyRoomPlate = "🪟 A ROOM YOU PAID FOR";

    /// <summary>
    /// SITTING DOWN WITH NOBODY OPPOSITE. #783's law kept — the first clause confirms the state change — and
    /// the whole of what the room is worth when the rota was against you.
    ///
    /// <para><b>NEW PROSE, flagged for the owner to bless.</b></para>
    /// </summary>
    public const string RoomIsYoursAndEmptyLine =
        "You take the head of the long table in a room you have paid for, and nobody comes. The chairs down "
        + "both sides are pushed in square, the lamps over the green are still four in the afternoon, and "
        + "the whole of what you bought was twenty minutes of nobody being able to walk in on you.";

    /// <summary>…and sitting down with somebody across it. The plate is the room's own — one of the ten who
    /// were in the hall — and the sentence says nothing about why they agreed.</summary>
    /// <param name="plate">Who is opposite, at plate size.</param>
    public static string TheyAreSeatedOppositeLine(string plate) =>
        $"You take one side of the long table and {Downcased(plate)} takes the other, with the chair "
            + "pulled out and put back the way somebody does when they have done this before. Nobody says "
            + "what the twenty minutes are for.";

    /// <summary>A plate, said mid-sentence. The stencil is shouted because a stencil is; a sentence about a
    /// person is not, and the glyph is not a word.</summary>
    private static string Downcased(string plate)
    {
        string words = plate.Replace(CanteenRegulars.Glyph, "", StringComparison.Ordinal).Trim();
        return words.Length == 0 ? "somebody" : words.ToLowerInvariant();
    }

    /// <summary>The one deal move's id. Named here so no panel invents its own vocabulary for a move the
    /// design named — <see cref="TheStools.LabelOf"/>'s own discipline.</summary>
    public const string PutItToThem = "put-it-to-them";

    /// <summary>What the button says.</summary>
    public const string PutItToThemLabel = "Put it to them";

    /// <summary>
    /// WHAT PUTTING IT TO THEM IS. #746's papers-on-the-table grammar, in a room with a door on it: you have
    /// the spread out in front of you and you turn it round.
    ///
    /// <para>Nothing is granted and nothing is signed — v1 is deliberately the GESTURE (#760's standing and
    /// contract negotiation is v2, and is named as such on the issue). What it actually changes is that the
    /// dig at this table files under the person opposite (<see cref="DugAgainstThem"/>), which is #715's
    /// per-entity memory arriving as a consequence rather than as a meter.</para>
    ///
    /// <para><b>NEW PROSE, flagged for the owner to bless.</b></para>
    /// </summary>
    public const string PutItToThemLine =
        "You turn the papers round on the table and leave them there. Whoever is opposite reads the top "
        + "sheet the whole way down without touching it, and then reads it again, and does not put a hand "
        + "out. \"I'll take that as said,\" they say, which is not the same as taking it.";

    /// <summary>The scene the strip is holding when somebody is opposite. Asked by NAME rather than by
    /// counting buttons, so a client can tell the two rooms apart without a field of its own.</summary>
    public const string DelegationSceneId = "ring:booked:delegation";

    /// <summary>…and the same room with nobody in it.</summary>
    public const string EmptySceneId = "ring:booked:empty";

    /// <summary>Is this sitting one of the two the booked room opens?</summary>
    public static bool IsABookedSitting(string? sceneId) =>
        string.Equals(sceneId, DelegationSceneId, StringComparison.Ordinal)
        || string.Equals(sceneId, EmptySceneId, StringComparison.Ordinal);

    /// <summary>What the counterpart's book keeps of a dig done in front of them — the machine-readable half,
    /// under their own entity. A note about a case, filed under the person it was put to.</summary>
    public static string DugAgainstThem(int room, long watch) =>
        $"{BookTag}-worked:{room}:{watch}";

    /// <summary>
    /// #770 · THE BOOKED TABLE WITH SOMEBODY AT IT — an <see cref="Encounter.Scene"/> and therefore the SAME
    /// docked strip every other seat in this game runs on (#865).
    ///
    /// <para>The three social moves are <see cref="CanteenTable"/>'s own ids and not new ones: a delegation
    /// is people at a table, and small talk at a table is small talk at a table however much the room cost.
    /// What is this room's is the FOURTH button, and the fact that there is a door.</para>
    /// </summary>
    /// <param name="plate">Who is opposite.</param>
    public static Encounter.Scene TheDelegationTable(string plate) => new(
        DelegationSceneId,
        plate,
        Setting,
        TheyAreSeatedOppositeLine(plate),
        [
            new(CanteenTable.SmallTalk, CanteenTable.LabelOf(CanteenTable.SmallTalk)),
            new(CanteenTable.Round, CanteenTable.LabelOf(CanteenTable.Round),
                Encounter.Requirement.Credits, CanteenTable.RoundPrice),
            new(PutItToThem, PutItToThemLabel, Says: PutItToThemLine),
            new(CanteenTable.Leave, CanteenTable.LabelOf(CanteenTable.Leave), Says: CanteenTable.LeaveLine),
        ]);

    /// <summary>…and the same table with nobody across it. Wait and stand, the two moves every lone seat in
    /// the game has, on the same ids (<see cref="SittingAlone.Wait"/>/<see cref="SittingAlone.Stand"/>) so a
    /// saved game and a guard both keep working.</summary>
    public static Encounter.Scene TheEmptyRoom() => new(
        EmptySceneId,
        EmptyRoomPlate,
        Setting,
        RoomIsYoursAndEmptyLine,
        [
            new(SittingAlone.Wait, RingOffice.WaitLabel),
            new(SittingAlone.Stand, RingOffice.StandLabel, Says: RingOffice.StoodUpLine),
        ]);

    /// <summary>Every sentence this file can put on a screen, for the canon sweep. It walks EVERY watch, so
    /// both halves of the counter's forked note are seen — <see cref="CabinetPrivacy.AllProse"/>'s own
    /// anti-vacuous idiom.</summary>
    public static IEnumerable<string> AllProse()
    {
        for (long watch = 0; watch < CanteenRegulars.WatchFill.Count; watch++)
        {
            yield return WhoBookedNote(1, watch);
            yield return DoggedTheDoorNote(1, watch);
            yield return BookedPlate(watch, true);
            yield return BookedPlate(watch, false);
        }
        yield return BookLabel(true);
        yield return BookLabel(false);
        yield return BookHint(true);
        yield return BookHint(false);
        yield return NothingToBookLine;
        yield return NoRoomOfThatKindLine(true);
        yield return NoRoomOfThatKindLine(false);
        yield return ShortLine(Price);
        yield return AlreadyHeldLine(1);
        yield return TookItLine(1, true);
        yield return TookItLine(1, false);
        yield return Setting;
        yield return EmptyRoomPlate;
        yield return RoomIsYoursAndEmptyLine;
        yield return TheyAreSeatedOppositeLine("◈ A CARRIER, WAITING ON A SIGNATURE");
        yield return PutItToThemLabel;
        yield return PutItToThemLine;
        yield return BarkThatKnows(1);
    }
}
