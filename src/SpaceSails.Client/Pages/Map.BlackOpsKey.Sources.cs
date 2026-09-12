using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #535 slice 2 · <b>THE KEY'S OTHER TWO SOURCES, CLIENT SIDE: A FAVOUR AND A FENCE.</b>
///
/// <para>Slice 1 gave the key one road into the pocket — lying in the crew spaces of a hull that fought —
/// which made it a thing that happens TO a captain and never a thing a captain can go and get. The issue
/// names the other two: <i>"the key's other sources (a fence, a favour)"</i>. Core owns both laws
/// (<see cref="BlackOpsKey"/>); this file is the two presses and nothing else.</para>
///
/// <h3>Both roads run through ONE strike-off</h3>
///
/// <para><b>A port deals one key a watch, whichever way it reaches you.</b> The favour at the bar and the
/// fence at the desk write and read the same tag —
/// <see cref="BlackOpsKey.ThePortHasDealtOne"/> in the captain's register of ground already gone through
/// (#615's <c>_roomsTurnedOver</c>, the same durable set slice 1 strikes a spent hull off in) — so a captain
/// cannot take the favour and then walk across the concourse for the second one. Two sources with two
/// registers would have been the two-meters-that-must-agree bug with a consumable on the end of it.</para>
///
/// <h3>Neither verb is ever drawn to be refused</h3>
///
/// <para>The satchel panel's own grammar, which slice 1's burn already follows: the offer is drawn where it
/// applies and absent where it does not. A greyed <i>Take the favour</i> under somebody's name would teach
/// the player that this person has something, which is the announcement the whole object is written against.
/// The one exception is the fence's <c>disabled</c> on insufficient credits — the desk's own existing
/// refusal, shared with the chip's row and the inspector's card, because a price you cannot meet is a price
/// you are allowed to see.</para>
/// </summary>
public sealed partial class Map
{
    // ── THE FAVOUR ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#535 slice 2 · The port the captain is standing in, for the one-key-per-port-per-window
    /// strike-off. At the bar that is the haven at the clamp; at the desk it is the body the dark web is
    /// being reached through. Both are ephemeris body ids, which is what lets the two share one tag.</summary>
    private bool ThisPortHasAlreadyDealtAKey(string? portId) =>
        portId is null
        || _roomsTurnedOver.Contains(BlackOpsKey.ThePortHasDealtOne(portId, BlackOpsKey.FenceWindow(SimTime)));

    /// <summary>#535 slice 2 · Strike this port off for this watch. Called by BOTH sources, so neither can
    /// forget what the other did.</summary>
    private void ThisPortHasNowDealtAKey(string portId)
    {
        _roomsTurnedOver.Add(BlackOpsKey.ThePortHasDealtOne(portId, BlackOpsKey.FenceWindow(SimTime)));
    }

    /// <summary>
    /// #535 slice 2 · <b>IS THE FAVOUR ON THIS PERSON'S CARD?</b> Core answers the relationship half
    /// (<see cref="BlackOpsKey.FavourIsOnTheTable"/> — top band, no lie on the book, not already spent); this
    /// adds only the two things Core cannot see: the captain is at a port that has not dealt one this watch,
    /// and there is room in the satchel for what he would be handed.
    ///
    /// <para>The capacity read is deliberate and is #615's own lesson: a verb that hands over an object the
    /// pocket cannot hold is a verb that destroys the object. It is asked again at the press, because the
    /// captain may open the satchel and make room between the two.</para>
    /// </summary>
    private bool TheFavourIsOnTheTable(string giver) =>
        _dockedHavenId is { } port
        && !ThisPortHasAlreadyDealtAKey(port)
        && BlackOpsKey.FavourIsOnTheTable(_contacts.For(giver))
        && Core.Satchel.CanTake(_satchel, BlackOpsKey.FavourFrom(giver));

    /// <summary>
    /// #535 slice 2 · <b>TAKE IT.</b> The key is in the pocket, the band that qualified the favour comes off
    /// their standing, and the book marks it so it can never be asked for twice.
    ///
    /// <para>The ledger is the one record that it happened, and it is written through
    /// <see cref="ContactLedger.RecordFavourGiven"/> — ONE call, which re-asks the law of the book itself
    /// before it writes. A press that raced the state (a lie told in another card, a favour already taken at
    /// the counter's doorway while this table's copy was on screen) gets null back and nothing moves; the
    /// alternative is a debit with no key or a key with no debit.</para>
    ///
    /// <para><b>Nothing is said here.</b> The canon line is the CONTACT speaking, and it is printed on the
    /// card the verb sits on — where they are looking, in their own voice, before the press rather than after
    /// it. A pulse line narrating the hand-over would be the game reporting a thing the player just watched.</para>
    /// </summary>
    private void TakeTheFavour(string giver)
    {
        if (!TheFavourIsOnTheTable(giver) || _dockedHavenId is not { } port)
        {
            return;
        }

        if (_contacts.RecordFavourGiven(giver, giver) is null)
        {
            return;     // the book refused it — nothing is debited and nothing is handed over
        }

        _satchel = [.. Core.Satchel.Add(_satchel, BlackOpsKey.FavourFrom(giver))];
        ThisPortHasNowDealtAKey(port);

        RendererInterop.PlayCue("board");
        RequestVaultSave();
        StateHasChanged();
    }

    // ── THE FENCE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#535 slice 2 · The port the fence is trading from, or null when there is no desk open. The
    /// dark web's own body (<c>DarkWebCurrentBody</c>), so the fence's stock rotates per PLACE and the row
    /// cannot follow a captain across the system on one watch.</summary>
    private string? TheFencesPort() => DarkWebCanTrade() ? DarkWebCurrentBody()?.Id : null;

    /// <summary>
    /// #535 slice 2 · <b>WHAT THE FENCE WANTS FOR ONE, OR NULL WHEN THERE IS NOTHING TO BUY.</b> The price is
    /// <see cref="BlackOpsKey.FencePrice"/> — three times the BUSTED card's own
    /// <c>BustedRule.BribeDemand</c>, called rather than restated, off the captain's own heat and the port's
    /// own watch seed. This file does no arithmetic about it.
    ///
    /// <para>Null once this port has dealt its one key this watch, and null when the pocket has no room: the
    /// chip's row and the inspector's card are both drawn where they apply rather than shown and denied
    /// (#212), and a row that took the coin and dropped the key would be worse than either.</para>
    /// </summary>
    private int? TheFencesKeyPrice()
    {
        if (TheFencesPort() is not { } port || ThisPortHasAlreadyDealtAKey(port))
        {
            return null;
        }

        long window = BlackOpsKey.FenceWindow(SimTime);
        return Core.Satchel.CanTake(_satchel, BlackOpsKey.FromTheFence(port, window))
            ? BlackOpsKey.FencePrice(_heat.Level, BlackOpsKey.FenceSeed(port, window))
            : null;
    }

    /// <summary>#535 slice 2 · The price the row prints, in the desk's own credit typography — the same
    /// <c>N0</c>/invariant formatting the chip's row and the inspector's card are quoted in, composed here so
    /// the three rows on one desk cannot come to three ways of writing a number.</summary>
    private string TheFencesKeyPriceText() =>
        TheFencesKeyPrice() is { } price ? price.ToString("N0", CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>
    /// #535 slice 2 · <b>BUY IT.</b> Coin out, key in, and the port is struck off for the watch.
    ///
    /// <para><b>Cannot pay is the desk's existing refusal</b> — the button is <c>disabled</c> below the price,
    /// exactly as the inspector's card is, and this guard is the second half of that so a press that raced
    /// the purse cannot overdraw it.</para>
    ///
    /// <para>No heat is banked and no line is said. The chip's sale puts a band on the book of whoever runs
    /// the ground because what was sold was evidence about THEIR site; buying a code off a stranger is
    /// nobody's business but the stranger's, and the fence's own row has already said the only thing this
    /// beat says.</para>
    /// </summary>
    private void BuyTheKeyFromTheFence()
    {
        if (TheFencesPort() is not { } port || TheFencesKeyPrice() is not { } price || _credits < price)
        {
            return;
        }

        Satchel.Item key = BlackOpsKey.FromTheFence(port, BlackOpsKey.FenceWindow(SimTime));
        if (!Core.Satchel.CanTake(_satchel, key))
        {
            return;
        }

        _credits -= price;
        _satchel = [.. Core.Satchel.Add(_satchel, key)];
        ThisPortHasNowDealtAKey(port);

        RendererInterop.PlayCue("board");
        RequestVaultSave();
        StateHasChanged();
    }
}
