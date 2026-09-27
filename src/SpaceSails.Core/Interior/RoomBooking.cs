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
public static partial class RoomBooking
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
}
