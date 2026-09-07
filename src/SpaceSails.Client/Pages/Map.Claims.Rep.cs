using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Claims.Rep — #1151 slice 2 · LODGING THE CLAIM WITH THE REP, IN PERSON.
//
// Canon pass, 2026-09-06, on #1151: "At Fess's or Kolt's table, with a loss on the wire and no claim lodged,
// the rep offers it before the pitch… The three presses are the same three (policy, name, wire entry) on the
// rep's card instead of the kiosk's; the counter and the flashback are shared; the payout is the same number,
// on the same next meeting."
//
// ── WHAT THIS FILE IS, AND WHAT IT DELIBERATELY IS NOT ──────────────────────────────────────────────────
//
// It is a HOST and a latch, and nothing else. There is no second copy of the three presses here, no second
// opinion about what a claim is worth, no second counter and no second flashback: `TheClaimAsks`,
// `PressTheClaim`, `LodgeTheClaim` and `PayWhatTheClaimIsWorth` are the ones Map.Claims.Kiosk.cs already
// wrote, and this file's whole contribution is to say WHERE the rows are drawn. That is the point of the one
// line the slice authored — "the machine and I file the same form" — turned into an arrangement of code: if
// a captain could get a different number, a different order or a different memory for walking to a table
// instead of a wall, the sentence would be a lie the game tells about itself.
//
// ── WHY THE OFFER GOES BEFORE THE PITCH ────────────────────────────────────────────────────────────────
//
// Because it is the only thing at that table the captain actually came for. #973's salesman opens with a
// tier line and #1061's underwriter opens with two sentences about hazard; both of them are asking for
// money. A man who has your lost hull on his wire and leads with a premium is a man who has not read his own
// file — and, mechanically, an offer under the pitch would be an offer under the BUTTONS that dismiss the
// card, which is #761's telling law failing quietly: the thing that changed is not where the captain is
// looking. So it goes above his own first sentence, and his answer to a claim — the payout line — still goes
// under it, because an answer is a different register from an approach.
//
// ── AND IT IS SAID ONCE PER LOSS ───────────────────────────────────────────────────────────────────────
//
// `_lodgingOfferedFor` is the latch, and it is spent by EITHER host: he says the line, or a claim is lodged
// against that loss at a machine. It rides the vault for `ClaimOwed`'s own reason — a latch a reload forgot
// would be a salesman offering to file a hull that has already been filed and paid for, which is a purse
// that fills by closing a browser tab.
public sealed partial class Map
{
    /// <summary>#1151 slice 2 · The loss the rep's offer has been spent on — the wire entry's subject — or
    /// null while he has said nothing and nothing has been lodged. Persisted: see the file note.</summary>
    private string? _lodgingOfferedFor;

    /// <summary>#1151 slice 2 · Whether the man at the table is making the offer on the card that is up
    /// right now. Decided once, when the card is built, and not re-asked at render: a line that appeared
    /// and vanished under the captain's eyes as the wire aged would be a man changing his mind.</summary>
    private bool _repOffersToLodge;

    // ── THE OFFER ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1151 slice 2 · <b>BEFORE THE PITCH.</b> Called by Harlan Fess's seam and by Brem Kolt's, both of them
    /// before the card they are about to build — the two of them are the same firm, and a rule about who may
    /// take a form would be the mirrored constant said about a person.
    ///
    /// <para>It runs ahead of <see cref="PayWhatTheClaimIsWorth"/> on purpose, and the ordering is load
    /// bearing: at the meeting where a claim is PAID the offer must not stand, because the firm cannot be
    /// asking to file a hull it is handing over the money for in the same breath. Asking first is what makes
    /// that true — the payout clears <c>_claimOwed</c>, and the offer reads it.</para>
    /// </summary>
    private void OfferToLodgeIt()
    {
        string? loss = NebulaClaims.TheLossOnTheWire(_newsEvents);
        _repOffersToLodge = NebulaClaims.TheOfferStands(loss, _claimOwed is not null, _lodgingOfferedFor);
        if (_repOffersToLodge)
        {
            // Said, and therefore spent. He does not ask twice about one hull.
            _lodgingOfferedFor = loss;
            RequestVaultSave();
        }
    }

    /// <summary>Whether the offer is on the card in front of the captain. The markup's one question, and it
    /// goes down the moment he accepts, because the counter that replaces it is the answer.</summary>
    private bool TheLodgingOfferIsUp => _repOffersToLodge && _claimDesk is not { Host: ClaimHost.Rep };

    /// <summary>
    /// #1151 slice 2 · <b>THE CAPTAIN TAKES HIM UP ON IT.</b> The same counter, opened with a man for a host
    /// instead of a wall — <see cref="OpenTheCounter"/> re-hosts a half-filled form rather than starting a
    /// new one, so a claim two presses in at a kiosk is the claim he is now watching you finish.
    /// </summary>
    private void LodgeItWithHim()
    {
        if (!TheLodgingOfferIsUp)
        {
            return;
        }

        OpenTheCounter(ClaimHost.Rep);
        StateHasChanged();
    }

    /// <summary>
    /// What the counter is saying on the rep's card, or null when it is not on it. One string rather than a
    /// gate and a getter, so the markup cannot draw the rows without the sentence that explains them —
    /// #761: the card is the telling, and a bare list of names is not a telling.
    ///
    /// <para>The words are the machine's own, unchanged: its standing demand until the third press lands and
    /// its receipt after. He has no line of his own here and this lane does not write him one.</para>
    /// </summary>
    private string? TheDeskSays =>
        _claimDesk is { Host: ClaimHost.Rep } desk ? NebulaClaims.DeskLine(desk.Presses) : null;

    /// <summary>
    /// #1151 slice 2 · The conversation is over — from either card's close, either rep's. The offer goes with
    /// it (an offer is a thing said at a meeting), and a FINISHED form goes with it too, because a lodged
    /// claim is business the firm has closed. A form part-way through stays exactly where it is: the machine
    /// has those presses and so does he, and picking it up at the next wall or the next table is the whole
    /// of what "the same form" means.
    /// </summary>
    private void TheRepsCounterCloses()
    {
        _repOffersToLodge = false;
        if (_claimDesk is { Host: ClaimHost.Rep } desk && NebulaClaims.IsLodged(desk.Presses))
        {
            _claimDesk = null;
        }
    }
}
