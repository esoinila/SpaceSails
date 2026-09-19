namespace SpaceSails.Core;

/// <summary>
/// <b>WHAT A COIN MACHINE SAYS WHEN THE PURSE IS EMPTY.</b> One sentence, shared by every slot in the game.
///
/// <para>The words are not new. They have been the souvenir kiosk's refusal since #379 — written inline in
/// the client, where the kiosk composed them with the item and the price — and #1199's gallery needed the
/// same refusal for a pair of coin binoculars and a vending machine. Two machines refusing in two different
/// registers would be two houses; a second sentence written to say what this one already says is the thing
/// §13.15 is about. So the clause is lifted here, the kiosk reads it, and the gallery's fixtures read it,
/// and what the kiosk prints is byte-for-byte what it printed before.</para>
///
/// <para>It is deliberately the MACHINE's voice and not a person's. A barkeep refuses you by name
/// (<c>Interior.Barkeep.BuyDrink</c>); a slot has no idea who you are, blinks something nobody has read in
/// years, and keeps your attention for exactly as long as it takes to look away. That is also why this is
/// said OFF-PLATE — a pulse and never a card. A full-screen picture for "you have no money" is the house
/// making a ceremony out of a refusal, and a refusal is not a moment.</para>
/// </summary>
public static class CoinSlot
{
    /// <summary>The refusal itself — the kiosk's own words since #379, now said by every slot.</summary>
    public const string ShortLine =
        "The slot blinks INSUFFICIENT FUNDS in a dead language. Empty pockets, captain.";
}
