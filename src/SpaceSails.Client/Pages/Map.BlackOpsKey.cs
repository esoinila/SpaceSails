using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #535 · <b>THE BLACK-OPS KEY, CLIENT SIDE: one find, and two ways to spend it.</b>
///
/// <para>Owner: <i>"In Expanse they found black ops code keys … that made the Mars military leave them alone
/// and delete all records of ever encountering them"</i>, and then the better half — <i>"We could use these
/// keys to drop heat at a tight spot 😎"</i>.</para>
///
/// <h3>Three seams, and they are deliberately small</h3>
/// <list type="number">
/// <item><b>The find.</b> A console in the CREW SPACES of a hull that fought, dealt by Core's own salvage
/// roll (<see cref="BlackOpsKey.IsAboard"/>). Taking it raises the object's card and nothing else: #614's
/// idiom, and the card's one line is the whole of what the game ever says about the thing.</item>
/// <item><b>The presentation.</b> The fifth exit on the BUSTED card, in <c>Map.Combat.Busted.cs</c> beside
/// the other four — because that is where the stage machine lives and a fifth exit written anywhere else
/// would be a second answer to "how does a catch end".</item>
/// <item><b>The burn.</b> A verb on the satchel row, which scrubs a band off the meter of whoever's ground
/// the captain is standing on.</item>
/// </list>
///
/// <h3>Two judgement calls, written down rather than left in the code</h3>
///
/// <para><b>The burn is drawn only where it can work.</b> The canon says "from the satchel, any time"; the
/// meter it acts on is per-outfit (#715) and there is no such thing as burning a key at nobody. So the verb
/// follows the panel's own established grammar for exactly this — the loader is drawn on rounds, the
/// shredder on evidence, the scan on the kit, because on anything else <i>it is not a refusal, it is a verb
/// that does not apply</i>. Off an outfit's ground, or on the ground of an outfit with nothing on you, there
/// is no file for a key to close and the control is not drawn. The button appearing the day you walk onto a
/// site that remembers you is in #715's own presentation grammar: one flat signal at their doors, silence at
/// everybody else's, and the company never named.</para>
///
/// <para><b>A hull deals its key once.</b> The roll is seeded off her id, so a captain who spent a key and
/// flew back would otherwise be standing over a fresh one. The hull is struck off in the captain's own
/// register of ground already gone through (#615's <c>_roomsTurnedOver</c>, which rides the vault and keeps
/// keys it does not recognise) rather than in a new section of the save, because a crew space you have been
/// through is exactly what that register is a list of.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>This hull is carrying one, and it is still lying there. Decided once, on boarding, by Core
    /// off the wreck's own id — the deck never rolls.</summary>
    private bool _keyAboard;

    /// <summary>#615 · How a hull whose key has been taken is written down in the captain's register of
    /// ground already gone through. A prefix of its own so it can never collide with a Hive room key, and so
    /// <c>SeedTurnedOverRooms</c> — which only seeds what <c>KeepOrLeave.TryReadKey</c> can parse — walks
    /// straight past it.</summary>
    private static string TheHullWhoseKeyIsGone(string wreckId) => "wreck-key:" + wreckId;

    /// <summary>#535 · Called once per boarding, beside <c>PrepareArchiveNode</c>. Core owns the roll; this
    /// only asks it, and then asks the register whether this captain has already been through her.</summary>
    private void PrepareBlackOpsKey(in Derelict.Wreck wreck) =>
        _keyAboard = BlackOpsKey.IsAboard(wreck.Id, wreck.Cause)
                     && !_roomsTurnedOver.Contains(TheHullWhoseKeyIsGone(wreck.Id));

    /// <summary>#535 · TAKE IT. The card is the receipt — the object's own card, raised on the spot, which is
    /// the same thing the pallet in the halls does and for the same reason: this is the one object a captain
    /// will want to look at again the moment they find it.</summary>
    private void TakeTheBlackOpsKey()
    {
        if (_wreck is not { } w || !_keyAboard)
        {
            return;
        }

        Satchel.Item found = BlackOpsKey.FoundOn(w.Id);
        _satchel = [.. Core.Satchel.Add(_satchel, found)];
        _keyAboard = false;
        _roomsTurnedOver.Add(TheHullWhoseKeyIsGone(w.Id));

        RendererInterop.PlayCue("board");

        // #614 · The card, and it is the whole telling: a caption and one line, and not a word about what to
        // do with it or whose it was.
        CarriedObject.Reveal card = BlackOpsKey.Card;
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            card.Label, card.ArtUrl, card.Story);

        RebuildWreckDeck();
        RequestVaultSave();
        StateHasChanged();
    }

    // ── The burn ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#535 · Is there a key in the pocket at all? What the BUSTED card asks before it draws a fifth
    /// exit — a control offering something the captain is not carrying is a control that has to refuse, and
    /// the whole point of this exit is that it never does.</summary>
    private bool CarryingABlackOpsKey => BlackOpsKey.InThePocket(_satchel) is not null;

    /// <summary>#535 · Is the burn this row's verb, here, now? See the file header for why this is drawn
    /// rather than refused: there is no such thing as burning a key at nobody.</summary>
    private bool BurnIsOffered(Core.Satchel.Item item) =>
        BlackOpsKey.IsTheKey(item) && TheOutfitUnderfoot is not null && HeatHere > 0;

    /// <summary>
    /// #535 · <b>BURN ONE COLD.</b> A band comes off the file of whoever runs the ground underfoot, the key
    /// is gone, and one line is said.
    ///
    /// <para>The band is <see cref="IllegalHeat.Scrub"/>'s and therefore the meter's own step. Nothing here
    /// knows how big a band is, which is the point: a number typed at this seam would be a second opinion
    /// about the rung the round's patience is already divided by.</para>
    /// </summary>
    private void BurnTheBlackOpsKey(Core.Satchel.Item key)
    {
        if (!BlackOpsKey.IsTheKey(key) || TheOutfitUnderfoot is not { } outfit)
        {
            return;
        }

        int erased = IllegalHeat.Scrub(_contacts, outfit, BlackOpsKey.ScrubReason);
        if (erased <= 0)
        {
            return;     // nothing was filed here, so there is nothing for a key to close and none is spent
        }

        _satchel = [.. BlackOpsKey.Spend(_satchel, key)];

        // #761/#736 · Said where the captain is looking, which on this press is the open satchel — and on the
        // pulse, at the rank the canon's one line earns, the moment nothing is up in front of them.
        SayItWhereTheyAreLooking(BlackOpsKey.BurnLine, PulseRank.Climax);

        RequestVaultSave();
        StateHasChanged();
    }
}
