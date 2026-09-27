using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #251 · THE BOOKING, THE DOOR AND THE COUNTER'S MEMORY — the booking record, the door that honours it,
/// the counter's long memory, and the verb at the counter.
///
/// <para>Split out of <c>RoomBooking.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>.</para>
/// </summary>
public static partial class RoomBooking
{
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
}
