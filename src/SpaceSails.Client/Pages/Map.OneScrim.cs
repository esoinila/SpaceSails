using System;
using System.Collections.Generic;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1052 (L2) · <b>ONE SCRIM AT A TIME.</b>
///
/// <para>#1052's law, verbatim: <i>"Two <c>.view-object-backdrop</c>s must never stack… A second scrim-card
/// raise while one is up is refused or queued behind the first, never stacked."</i></para>
///
/// <h3>The bug it names, which is live today</h3>
///
/// <para>A captain sits down at a top in a docked bar and presses <kbd>6</kbd>: the galley card goes up over
/// a full-viewport dim. Harlan Fess is working this berth, and he crosses the floor on his own legs
/// (<c>Map.BarWalkers.cs</c>) to a captain sitting alone. He arrives, and <c>HeReachesYourTable</c> raises
/// his pitch — a SECOND <c>.view-object-backdrop</c>, drawn over the first. Two dims multiply: the room the
/// #784 seated dock exists to keep visible goes to near-black, and the card underneath is neither readable
/// nor reachable while the one on top is up. Nothing in the client refused it, because until now nothing in
/// the client could see it happening.</para>
///
/// <h3>How it is refused</h3>
///
/// <para>A CENSUS and a QUEUE, and the census is the load-bearing half. Every element in <c>Map.razor</c>
/// that draws a <c>.view-object-backdrop</c> is behind an <c>@if</c>, and
/// <see cref="TheScrimCensus"/> is those gates, each one paired with the condition as it is TYPED in the
/// markup. That pairing is what <c>OnlyOneScrimAtATimeTests</c> reads: it walks the razor, pulls every
/// scrim's own gate expression out of the source, and requires the census to name it. So a new card with a
/// scrim on it joins this law on the day it is typed, with no edit here — and a census that had quietly
/// fallen behind the markup fails loudly instead of arbitrating against half a screen.</para>
///
/// <para>The queue is the small half. A card the WORLD raises — a salesman at your elbow, a woman who walked
/// in — goes through <see cref="RaiseAScrimCard"/>, which puts it up if the glass is clear and holds it if
/// it is not. It is held rather than dropped because the body is already standing at the table: the beat
/// happened, and a beat thrown away because a card was up is the #603 class (a thing that quietly does
/// nothing). <see cref="PumpTheScrimQueue"/> lets it through on the first frame the glass clears.</para>
///
/// <para><b>Why the ARBITER is only on the world-raised cards.</b> Everything else on the census is a card
/// the captain went and GOT, with his own hand, and there is a rule older than this one for those: a press
/// that raises a second card while the first is up is the captain answering the first card by asking for
/// another, and the families that can do it already close each other (the bar's doorway family, the
/// mutually-exclusive deck cards). The stacking this law is about is the involuntary kind — a card that
/// arrives because the world decided it should, over one the captain was reading.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// Every gate in <c>Map.razor</c> that draws a <c>.view-object-backdrop</c>, paired with its condition
    /// AS TYPED in the markup.
    ///
    /// <para>The string is not decoration and it is not a comment: <c>OnlyOneScrimAtATimeTests</c> extracts
    /// the same expressions out of the razor and matches them against these, so the census cannot fall
    /// behind the page. Where the markup writes a scrim in an <c>else</c>, the gate recorded is the
    /// <c>@if</c> it hangs off (there is exactly one, the seated CONVERSATION card, whose scrim is the else
    /// of <c>@if (SeatedIsDocked)</c> — the strip branch draws no scrim at all, which is #784's whole
    /// point).</para>
    /// </summary>
    private IEnumerable<(string Gate, bool Up)> TheScrimCensus()
    {
        yield return ("_showScuttlePanel && _wreck is { } scWreck", _showScuttlePanel && _wreck is not null);
        yield return ("_scuttleEpitaph is { } epitaph", _scuttleEpitaph is not null);
        yield return (
            "_ventReadCard is { } readRoom && _ventReads.TryGetValue(readRoom, out var readShown)",
            _ventReadCard is { } readRoom && _ventReads.ContainsKey(readRoom));
        yield return (
            "_pressureDoor is { } pdName && _ventSpaces.TryGetValue(pdName, out var pdSpace)",
            _pressureDoor is { } pdName && _ventSpaces.ContainsKey(pdName));
        yield return ("_showVentPanel && _wreck is { } ventWreck", _showVentPanel && _wreck is not null);
        yield return ("_archiveCard is { } vision", _archiveCard is not null);
        yield return ("_showShipBoard", _showShipBoard);
        yield return ("_showChargeBoard", _showChargeBoard);
        yield return ("_storyCard is { } told", _storyCard is not null);
        yield return ("_repCard is { } pitch", _repCard is not null);
        yield return ("_hardcaseCard is { } koltOffers", _hardcaseCard is not null);
        yield return ("_walkInCard is { } sheAsks", _walkInCard is not null);
        yield return ("_finderCard is { } finderAsks", _finderCard is not null);
        yield return ("_finderReveal is not null", _finderReveal is not null);
        yield return ("_shipEpitaph is { } castaway", _shipEpitaph is not null);
        yield return ("_showShipScuttlePanel", _showShipScuttlePanel);
        yield return ("_wreckLook is { } look", _wreckLook is not null);
        yield return ("_kioskCard is { } buy", _kioskCard is not null);
        yield return ("_showLiftPanel && _surface is { } liftEx", _showLiftPanel && _surface is not null);
        yield return ("_lockedDoor is { } door", _lockedDoor is not null);
        // The seated CONVERSATION card — the else of this @if. The docked branch is the strip and writes no
        // scrim, so the card is up exactly when somebody came to you at a top you are sitting at.
        yield return ("SeatedIsDocked", SeatedTable is not null && !SeatedIsDocked);
        yield return ("TheStandUpConfirmIsUp", TheStandUpConfirmIsUp);
        yield return ("_showSatchel", _showSatchel);
        yield return ("_galleyCardOpen", _galleyCardOpen);
        yield return ("_navHelpOpen", _navHelpOpen);
        yield return ("_viewObject is { } vo", _viewObject is not null);
        yield return ("_selfieShot is { } shot", _selfieShot is not null);
        yield return ("_showCaptainsRemote", _showCaptainsRemote);
        yield return ("_showDoorBoard", _showDoorBoard);
        yield return ("_showAlarmPanel", _showAlarmPanel);
    }

    /// <summary>Is anything wearing a full-viewport scrim right now? Asked of the census, so it can never
    /// answer for a smaller screen than the page is drawing.
    ///
    /// <para><b>It costs nothing per frame</b>, which is worth saying out loud about a member the walked
    /// frame calls: <see cref="PumpTheScrimQueue"/> returns on its FIRST line when the queue is empty, and
    /// the queue is empty on every frame but the handful after somebody arrives at the table. So this walk
    /// runs when a card is actually waiting and at no other time.</para></summary>
    private bool AScrimIsUp
    {
        get
        {
            foreach ((string _, bool up) in TheScrimCensus())
            {
                if (up)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>The card the world wanted to raise while the glass was busy, waiting its turn — or null.
    /// One slot and not a list: two people cannot both be standing at your elbow (the walker band and the
    /// approach's own <c>stillWanted</c> see to that), and a queue that could grow would be a card arriving
    /// minutes after whatever put it there had stopped being true.</summary>
    private (Action Raise, Func<bool> StillWanted)? _scrimQueued;

    /// <summary>#1052 · <b>THE ARBITER.</b> Raise this card if nothing is wearing a scrim; otherwise hold it
    /// behind the one that is. Never stacks, which is the law, and never silently drops, which is #603 (a
    /// beat thrown away because a card was up is a control that quietly did nothing).</summary>
    /// <param name="raise">What putting this card up actually is — the whole of it, so that anything the
    /// raise COUNTS (the rep's meeting tally, the beat the walk-in spends) is counted when the card really
    /// goes up and not when it was merely wanted.</param>
    /// <param name="stillWanted">Asked again at the far end of the wait. A held card is a card about a
    /// moment, and the moment can end while the captain reads something else — a captain who has stood up
    /// and walked off does not want a pitch delivered to an empty chair.</param>
    /// <returns>Whether the card went up now.</returns>
    private bool RaiseAScrimCard(Action raise, Func<bool> stillWanted)
    {
        ArgumentNullException.ThrowIfNull(raise);
        ArgumentNullException.ThrowIfNull(stillWanted);

        if (AScrimIsUp)
        {
            _scrimQueued = (raise, stillWanted);
            return false;
        }

        _scrimQueued = null;
        raise();
        return true;
    }

    /// <summary>#1052 · The held card's turn. Called once from the walked frame — the only frame in which a
    /// bar or a canteen is running at all — so a card queued behind the galley goes up on the frame after
    /// the captain shuts it, with nobody having to remember to let it through.</summary>
    private void PumpTheScrimQueue()
    {
        if (_scrimQueued is not { } held)
        {
            return;
        }

        if (!held.StillWanted())
        {
            _scrimQueued = null;
            return;
        }

        if (AScrimIsUp)
        {
            return;
        }

        _scrimQueued = null;
        held.Raise();
    }
}
