using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #711 slice 1 · <b>WHERE A CAPTAIN GETS ONE, AND WHICH UNIVERSE THIS IS.</b>
///
/// <para>Canon design (head coder, 2026-08-09): <i>"a captain skimming a little — an unlisted parcel, a bent
/// filing. TRUE, and deliberately discoverable: petty crime is the expected secret of a small
/// hauler."</i></para>
///
/// <h3>Why the dark-web desk and not the cargo market</h3>
///
/// <para>The trade panel sells CARGO, which is the thing a manifest is a list of. Anything bought there is
/// on the list by construction, and a parcel that arrived through a priced market row would be a parcel with
/// a receipt — which is precisely the opposite of the object. The desk beside it is where this game already
/// keeps the handovers that leave no paper: the chip's buyer (#233), the inspector's credentials (#1149),
/// the fence's key (#535 slice 2). A fourth row there is the honest seam, and it is the only one in the game
/// where "an unlisted parcel" is a true description of what just happened.</para>
///
/// <h3>What it costs, which is nothing and is not free</h3>
///
/// <para>No coin moves. Nobody pays a hauler up front for a box they are not listing, and the row would have
/// had to invent a price this economy makes no statement about. What the captain actually pays is a POCKET —
/// #603's twelve-slot cap, which is the pressure the whole satchel is built on — and, one afternoon, a fine
/// off <see cref="BustedRule.BribeDemand"/> at whatever this outfit's meter says he is worth. The row's
/// silence about that is deliberate: the desk does not explain the mechanic and neither does the card that
/// closes it.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// #711 · <b>WHICH UNIVERSE THIS CAPTAIN IS IN, as a seed.</b>
    ///
    /// <para>The active game thread's own id, folded through <see cref="DiceRule.Seed(ulong, string)"/>.
    /// A thread IS a universe in this build — a new voyage clears the contacts book, the caches and the
    /// KAAMOS assembly (<c>ResetLiveStateForNewGame</c>) — so it is the only honest answer to "what world is
    /// this", and it is durable across a reload because the thread id is.</para>
    ///
    /// <para>Nothing but <see cref="UnlistedParcel.TheOneWhoKeepsLooking"/> reads it today. It lives here
    /// rather than in the patrol because the thread is Map's fact, and it is handed down through
    /// <c>IPatrolHost</c> rather than re-derived on the floor, so two readers can never disagree about which
    /// world they are standing in.</para>
    /// </summary>
    public ulong WorldSeed => DiceRule.Seed(0UL, _activeThreadId ?? string.Empty);

    /// <summary>
    /// #711 · <b>COIN OUT FOR A FINE PAID ON SOMEBODY'S FLOOR.</b> The amount is Core's — the BUSTED card's
    /// own <c>BustedRule.BribeDemand</c>, called by <see cref="UnlistedParcel.TheFine"/> — and this page
    /// does no arithmetic about it beyond refusing to go below nothing.
    ///
    /// <para>Floored at zero and not at <c>BustedRule.MinBerthFeeCr</c>: that floor is the CONFISCATION's
    /// mercy law, and it exists so a busted captain is not stranded penniless at a dock by the collectors
    /// taking everything. A fine is a form, not a seizure — a man writing a number down does not check
    /// whether you can still afford a berth — so borrowing that constant here would be one law quoted in a
    /// place it was not written for, which is how a mirrored constant starts.</para>
    /// </summary>
    public void PayTheFine(int credits)
    {
        if (credits <= 0)
        {
            return;
        }

        _credits = System.Math.Max(0, _credits - credits);
    }

    /// <summary>#711 · Is there a parcel to be had across this desk? Drawn where it applies rather than
    /// shown and denied (#212): the desk has to be open, the captain must not already be carrying one (there
    /// is one per hull at a time), and the pocket has to have room for it.
    ///
    /// <para>#711 slice 2 · …and nobody has anything for a hull somebody's box was just taken off
    /// (<see cref="TheDeskHasNothingForThisHull"/>). It is the same shape as the three clauses above it, and
    /// that is the point: an ABSENCE, in the row's own vocabulary, with no sentence attached to it.</para></summary>
    private bool ParcelOnOffer() =>
        DarkWebCanTrade()
        && !UnlistedParcel.Held(_satchel)
        && !TheDeskHasNothingForThisHull()
        && TheParcelOnThisDesk() is { } parcel
        && Core.Satchel.CanTake(_satchel, parcel);

    /// <summary>#711 · The parcel this desk is handing over, or null when there is no desk open. Its id is
    /// the haven and the WATCH — <c>Interior.PatronRota.WatchIndex</c>, the four-sim-hour shift the seated
    /// regulars, the escort's patience and the fence's stock all turn on — so two windows are two parcels
    /// and one window is one parcel however many times the row is looked at.</summary>
    private Core.Satchel.Item? TheParcelOnThisDesk() =>
        DarkWebCurrentBody()?.Id is { Length: > 0 } haven
            ? UnlistedParcel.FromTheDesk(haven, PatronRota.WatchIndex(SimTime))
            : null;

    /// <summary>
    /// #711 · <b>TAKE IT.</b> Into the pocket, and NOTHING IS SAID — there is no line authored for this
    /// counter and none is wanted: the parcel's own look card is what the captain reads about it, when he
    /// opens the satchel and looks, which is #614's idiom and the register the object was written in.
    ///
    /// <para>No coin moves and no heat is banked. Taking a box off a stranger is nobody's business but the
    /// stranger's — the same call the inspector's card makes one row up — and a desk that reported its own
    /// customers would not have any.</para>
    /// </summary>
    private void TakeTheUnlistedParcel()
    {
        if (!ParcelOnOffer() || TheParcelOnThisDesk() is not { } parcel)
        {
            return;
        }

        _satchel = [.. Core.Satchel.Add(_satchel, parcel)];
        RendererInterop.PlayCue("board");
        FileNote(UnlistedParcel.LookCardLine, UnlistedParcel.Glyph);
        RequestVaultSave();
        StateHasChanged();
    }
}
