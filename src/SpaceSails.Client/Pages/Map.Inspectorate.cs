using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1149 slice 2 · <b>THE OTHER ROAD TO THE INSPECTOR'S CARD: THE FENCE.</b>
///
/// <para>Design pass (Fable, 2026-09-06): <i>"the fence at the DarkWeb desk sells one at a price derived from
/// the fence markup on the highest-tier pass (never typed), and a bought card is a BET: sites keep an
/// inspection roster, and presenting the card where no inspection is due is
/// <c>WalletChoice.Outcome.WrongSite</c>'s cousin."</i></para>
///
/// <para>#804's <c>FoundPass</c> file says, in as many words, that a false ID <i>is never bought — there is no
/// vendor, no fence and no price</i>, and that stands: a SITE PASS is a real person's real paper that ended
/// up somewhere it should not have, and there is no market in those. This is a different object with a
/// different fiction. The INSPECTORATE issues above the sites, its credentials are the one kind of paper
/// down here that is worth something to somebody who is not standing in the building, and the card the
/// captain buys has had its photograph replaced once already — which is the look card saying, flatly, that
/// it has been through a desk like this one before.</para>
///
/// <para><b>What it does NOT buy.</b> Not a way in: it is a bet on a roster the captain cannot read, and on
/// most sites, on most watches, presenting it away from a refuge floor ends in the walk back to the car. The
/// only sites where it is always good are the ones with an open inspection on them
/// (<see cref="Inspectorate.InspectionIsDue"/>), and the captain has no way of knowing which those are until
/// he has been down there.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>#1149 · What the desk wants for one, or null when there is nowhere to buy it or nothing to
    /// buy. The price is <see cref="Inspectorate.FencePrice"/> — derived, never typed, and it reprices itself
    /// with the intel market it is derived from.
    ///
    /// <para>Null once the card is in the wallet, because there is exactly one of these and a second row for
    /// a thing you are carrying is an affordance with nothing behind it (#212). It is drawn where it applies
    /// rather than shown and denied — the same call the chip's own row makes.</para></summary>
    private int? InspectorCardPrice() =>
        DarkWebCanTrade() && !Inspectorate.Held(_satchel) ? Inspectorate.FencePrice : null;

    /// <summary>#1149 · Buy it. Coin out, laminate in, and NOTHING IS SAID — there is no line authored for
    /// this counter and none is wanted: the card's own look card is what the captain reads about it, when he
    /// opens the wallet and looks, which is #614's whole idiom and the register the object was written in.
    ///
    /// <para>No heat is banked. The chip's sale puts a band on the book of whoever runs the ground it was
    /// sold from (#715's step) because what was sold was evidence about THEIR site; buying a document off a
    /// stranger is nobody's business but the stranger's, and a fence who reported his own customers would not
    /// have any.</para></summary>
    private void BuyTheInspectorCard()
    {
        if (InspectorCardPrice() is not { } price || _credits < price
            || !Core.Satchel.CanTake(_satchel, Inspectorate.Card))
        {
            return;
        }

        _credits -= price;
        _satchel = [.. Core.Satchel.Add(_satchel, Inspectorate.Card)];
        RendererInterop.PlayCue("board");
        FileNote(FoundPass.Plate(Inspectorate.Card), PatrolBeat.BadgeGlyph);
        RequestVaultSave();
        StateHasChanged();
    }
}
