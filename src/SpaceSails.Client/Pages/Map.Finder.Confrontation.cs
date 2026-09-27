using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE CONFRONTATION (#417) — the reveal at the berth, the fourth name, settling the case (and the
/// one heat it banks), and closing the reveal.
///
/// <para>Split out of <c>Map.Finder.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public sealed partial class Map
{
    // ── THE CONFRONTATION ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #417 · <b>THE REVEAL, AT THE BERTH.</b> Raised the moment a captain who has walked the whole trail is
    /// tied up at the port the case ends at — which is the one place in this feature where the world comes to
    /// him rather than the other way round.
    ///
    /// <para>Called from the walked frame rather than from the bar's metabolism, because a working berth is
    /// not always a room with a bar in it and the fourth name is tied up outside either way.</para>
    /// </summary>
    private void TheRevealAtTheBerth()
    {
        if (!_deckMode || _surface is not null || _dockedHavenId is not { } berth
            || _finderReveal is not null || !TheTrailIsLive || !_finderProgress.TrailWalked
            || _finderProgress.Revealed || _finderCase is not { } c
            || !string.Equals(berth, c.BerthPortId, StringComparison.Ordinal))
        {
            return;
        }

        RaiseAScrimCard(TheFourthNameGoesUp, () => _deckMode && _dockedHavenId == c.BerthPortId);
    }

    /// <summary>The card itself, once the glass is its. THE CARD IS THE TELLING (#761): it changes what the
    /// captain knows and what he can do, and there is a row in <c>ThePlayerIsToldTests</c> that says so.</summary>
    private void TheFourthNameGoesUp()
    {
        if (_finderCase is not { } c)
        {
            return;
        }

        _finderProgress = _finderProgress with { Revealed = true };
        _finderOutcome = null;
        _finderReveal = c;
        LogAutopilotEvent($"{FinderGlyph} {FinderCase.Reveal}");
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>
    /// #417 · <b>THE TWO VERBS.</b> Both are the finding done, so both pay Varga's fee and Varga's standing;
    /// the fork is what happens on top, and the arithmetic is Core's (<see cref="FinderCase.PayFor"/>) so the
    /// sentence on the card and the numbers in the purse cannot come to two views of one evening.
    ///
    /// <para>The outcome rides the CARD (#736), not the HUD under its own backdrop. The heat, when there is
    /// any, is banked against whoever runs this port through the one call every crossing in the game goes
    /// through — never a number written into the ledger here.</para>
    /// </summary>
    private void SettleTheCase(FinderCase.Outcome outcome)
    {
        if (_finderReveal is not { } c || _finderProgress.Settled != FinderCase.Outcome.Open
            || outcome == FinderCase.Outcome.Open)
        {
            return;
        }

        FinderCase.Payment paid = FinderCase.PayFor(c, outcome);
        _credits += paid.Credits;
        _contacts.AddGoodwill(FinderCase.ContactId, FinderCase.DisplayName, paid.Reputation);
        _finderProgress = _finderProgress with { Settled = outcome };

        if (paid.HeatPoints > 0)
        {
            BankTheCrossing(new UndergroundComplex.HeatCharge(
                SiteOperator.Of(c.BerthPortId).Id, paid.HeatPoints));
        }

        _finderOutcome = FinderCase.OutcomeLine(outcome);
        LogAutopilotEvent($"{FinderGlyph} {_finderOutcome} (+{paid.Credits:N0} cr)");
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>Shut the reveal. The choice, once made, is made — closing it again settles nothing, which is
    /// why the two verbs are gone off the card the moment one of them is pressed.</summary>
    private void CloseTheReveal()
    {
        _finderReveal = null;
        _finderOutcome = null;
        StateHasChanged();
    }
}
