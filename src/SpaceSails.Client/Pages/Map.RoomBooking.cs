using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #770 · BOOK A ROOM AT THE COUNTER — the client half.
///
/// <para>Owner's idea: the block's park-view suites are rented by the watch, and the deal that gets done in
/// one is done across a table with somebody sitting opposite. The rooms, the seats and the long table all
/// shipped (#816/#817/#868); what was missing was the VERB, and the plate on the door has been advertising it
/// since #816 — <c>NEGOTIATION ROOM · BOOK AT THE COUNTER</c>.</para>
///
/// <h3>Where the booking lives, and why it is not a field</h3>
///
/// <para><b>In the counter's own book</b> — <c>_contacts</c>, #715's per-entity ledger, under
/// <see cref="RoomBooking.LedgerId"/>, in the machine-readable shape <see cref="TheKeep.AskedEntry"/>
/// established for the keep's asks. That is the honest model (the thing that remembers who had which room on
/// which watch IS the book on the counter) and it has two consequences worth naming: the booking round-trips
/// through the vault for free, and <b>#905's frame ledger stays pinned</b> — no new field on
/// <see cref="Map"/>, none on <c>SurfaceExcursion</c>, so not one of the thirty committed fingerprints
/// moves.</para>
///
/// <para>A booking is never deleted. The book does not un-write: <see cref="RoomBooking.HonoursTheDoor"/> is
/// what makes exactly one of them true right now, and the watch turning is what takes the room back.</para>
///
/// <h3>The refusals are said, never hidden</h3>
///
/// <para>Both verbs are drawn at this counter whatever the floor has on it (#212: an affordance never hides)
/// and every way they can fail answers OUT LOUD in the card's own notice slot (#603): no room of that kind
/// free, nothing bookable here at all, the purse short, or one already yours. That is also why nothing here
/// asks <c>UndergroundComplex.Build</c> at render time — the floor is laid on the PRESS, once, and never on a
/// frame.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// #770 · THE ROOM THE CAPTAIN IS HOLDING RIGHT NOW, or null.
    ///
    /// <para>Read off the counter's book against the FROZEN watch (#709) and the floor underfoot, so the
    /// plate on the plan, the leaf under it and the scene at the table are one answer. The book holds every
    /// booking ever made; at most one of them is this floor on this shift, which is the whole of what
    /// "until the watch turns" means.</para>
    /// </summary>
    private RoomBooking.Booking? TheRoomYouHold(SurfaceExcursion ex)
    {
        foreach (string entry in _contacts.For(RoomBooking.LedgerId).KnownTells)
        {
            if (RoomBooking.TryReadBooking(entry, RoomBooking.TheCaptain, out RoomBooking.Booking held)
                && RoomBooking.HeldOn(in held, ex.Floor, ex.CanteenWatch))
            {
                return held;
            }
        }
        return null;
    }

    /// <summary>#770 · Is the captain standing at a counter that rents rooms? The Hive's own counter and
    /// nowhere else — a haven bar is a haven bar, and the fork is about ONE fixture (the law
    /// <see cref="CounterService.OnWatch"/> keeps one file over).</summary>
    private bool TheCounterRentsRooms =>
        _surface is { Floor: < 0 } ex && _barMenu is { } keep
        && CounterService.For(ex.Stop.Body.Id, UndergroundComplex.Comfort.UpperCanteen) is { } house
        && (keep == house || CounterService.ServedByTheKeep(keep));

    /// <summary>What the booking button says on the way past — two counters, two sentences, because the
    /// difference between them is whether there is anybody to ask (#781).</summary>
    private string BookingHint => RoomBooking.BookHint(_barMenu?.SelfService ?? true);

    /// <summary>
    /// #770 · BOOK ONE. The whole verb, in one press.
    ///
    /// <para>The order is the order it happens at a counter: are you already holding one, is there one of
    /// that kind free, can you pay for it, and only then does the coin move. The debit is BEFORE the answer
    /// for the reason the round's is — the answer is the room being yours.</para>
    ///
    /// <para>Two books grow, and they are different books. The captain's field book keeps a sentence the
    /// captain wrote (<see cref="RoomBooking.WhoBookedNote"/>, forked on whether anybody was behind the
    /// counter to write it); the COUNTER's book keeps five integers and nothing a panel could print.</para>
    /// </summary>
    /// <param name="wantBig">Whether the captain pressed the long room's button.</param>
    private void BookARoom(bool wantBig)
    {
        if (_surface is not { } ex || ex.Floor >= 0 || _barMenu is null || !TheCounterRentsRooms)
        {
            return;
        }

        if (TheRoomYouHold(ex) is { } already)
        {
            SayAtTheCounter(RoomBooking.AlreadyHeldLine(already.Room));
            return;
        }

        // THE FLOOR IS LAID ON THE PRESS. Core's own frontage (Park.Frontage) — the very list the deck was
        // drawn from — walked by Core's own picker. Nothing here measures a room (§13.15).
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        if (floor.Park is not { } green || green.Frontage.Count == 0)
        {
            SayAtTheCounter(RoomBooking.NothingToBookLine);
            return;
        }

        if (RoomBooking.RoomYouGet(green.Frontage, wantBig) is not { } room)
        {
            // Which of the two refusals it is comes off whether this floor has ANY negotiation room at all:
            // "no long one free" is a different fact from "this block does not rent rooms", and a captain
            // told the wrong one learns the wrong thing about the building.
            SayAtTheCounter(
                RoomBooking.RoomYouGet(green.Frontage, !wantBig) is null
                    ? RoomBooking.NothingToBookLine
                    : RoomBooking.NoRoomOfThatKindLine(wantBig));
            return;
        }

        // The PRICE FOLLOWS THE ROOM and never the button: BigOn is asked of the ring rather than of the
        // press, so the label, the debit and the plate cannot come to three answers.
        bool big = RoomBooking.BigOn(green.Frontage, room);
        int price = RoomBooking.PriceOf(big);
        if (_credits < price)
        {
            SayAtTheCounter(RoomBooking.ShortLine(price));
            return;
        }

        _credits -= price;

        var booking = new RoomBooking.Booking(ex.Floor, room, big, ex.CanteenWatch, RoomBooking.TheCaptain);
        _contacts.RecordKnownTell(
            RoomBooking.LedgerId, RoomBooking.LedgerName, RoomBooking.BookedEntry(in booking));
        FileNote(RoomBooking.WhoBookedNote(room, ex.CanteenWatch), RoomBooking.Glyph);

        SayAtTheCounter($"{RoomBooking.TookItLine(room, big)} (−{price:N0} cr)");

        // The plate and the leaf change on the plan, so the plan is laid again — the same one rebuild every
        // other durable change down here goes through.
        RebuildSurfaceDeck();
        RequestVaultSave();   // #225: the purse moved and two books grew
    }

    /// <summary>One place a counter sentence lands, so the notice slot and the pulse can never carry two
    /// different words for one press (#736's fight, kept won).</summary>
    private void SayAtTheCounter(string said)
    {
        _barNotice = said;
        ShowPulseMessage(said);
    }
}
