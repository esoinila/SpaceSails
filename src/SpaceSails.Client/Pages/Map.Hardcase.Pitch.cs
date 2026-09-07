using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE PITCH — he is at your elbow, held behind the scrim if something is already in front of the
/// captain (#1052: his legs already brought him here, so the beat happened and must not be dropped, and
/// it is asked again on the way out because a captain who has walked back up the tube in the meantime is
/// a different question).
///
/// <para>Split out of <c>Map.Hardcase.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── The pitch ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>He is at your elbow. Held behind the scrim if something is already in front of the captain,
    /// for #1052's reason — his legs already brought him here, so the beat happened and must not be dropped;
    /// and asked again on the way out, because a captain who has walked back up the tube in the meantime is
    /// not standing there to be sold to.</summary>
    private void HeReachesYou() =>
        RaiseAScrimCard(KoltsPitchGoesUp, () => !CaptainBeyondReach && !_hardcaseFled);

    /// <summary>The pitch itself, once the glass is his — and the moment the ground goes in his book, because
    /// being found on a ground is what spends one of the two.</summary>
    private void KoltsPitchGoesUp()
    {
        _hardcasePitched = true;

        // #1151 slice 2 · The offer goes before the pitch here for the same reason it does at Fess's table,
        // and through the same seam (Map.Claims.Rep.cs): they are one firm, and a rule about WHICH of them
        // may take a form would be the mirrored constant said about a person. He is not paid at — the money
        // arrives when a representative next finds you, and that is Harlan Fess's meeting, not this one.
        OfferToLodgeIt();

        _hardcaseCard = HardcaseRep.OffersFor(_insurance.Tier);
        RememberHeWasFoundHere();

        // He is a relationship like every other name in the book, and the book knows him from the first
        // hello — even though he will not know you at the next one.
        _contacts.AddGoodwill(HardcaseRep.ContactId, HardcaseRep.DisplayName, 0);
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>#1061 · The ground goes in the book that caps him at two, and the vault is asked to keep it.
    /// Written through <see cref="HardcaseRep.WithGroundWorked"/> so the cap is enforced and written by one
    /// function rather than by two that have to agree.</summary>
    private void RememberHeWasFoundHere()
    {
        if (_hardcaseGround is not { } ground)
        {
            return;
        }

        IReadOnlyList<string> book = HardcaseRep.WithGroundWorked(_hardcaseGroundsWorked, ground);
        if (book.Count == _hardcaseGroundsWorked.Count)
        {
            return;
        }

        _hardcaseGroundsWorked.Clear();
        _hardcaseGroundsWorked.AddRange(book);
        RequestVaultSave();
    }

    /// <summary>Take the card down. His body stays wherever it is standing; only the panel goes.</summary>
    private void CloseTheHardcasesCard()
    {
        _hardcaseCard = null;
        TheRepsCounterCloses();   // #1151 slice 2 · the offer and a finished form go with the conversation
        StateHasChanged();
    }

    /// <summary>
    /// THE CAPTAIN ANSWERS. One door for every button, so a move's meaning cannot drift from its words — and
    /// the buttons are <see cref="NebulaRep"/>'s, so a sale signed on a moon is the same sale signed in a
    /// concourse.
    /// </summary>
    private void AnswerTheHardcase(NebulaRep.RepMove move)
    {
        if (_hardcaseCard is null)
        {
            return;
        }

        switch (move)
        {
            case NebulaRep.RepMove.BuyBasic:
                SignWithKolt(InsuranceTier.Basic);
                break;

            case NebulaRep.RepMove.BuyPremium:
                SignWithKolt(InsuranceTier.Premium);
                break;

            default:
                TellKoltNo();
                break;
        }
    }

    /// <summary>
    /// NO — and he says the third line. It is pulsed rather than carded because the card is going: he has
    /// been answered, and the answer is the end of the conversation.
    ///
    /// <para>He does not come back at the captain on this ground. That is line three read literally: the
    /// book's expectation is about the NEXT moon, not about asking again on this one.</para>
    /// </summary>
    private void TellKoltNo()
    {
        _hardcaseRefused = true;
        ShowPulseMessage(HardcaseRep.OnRefusal);
        CloseTheHardcasesCard();
        _hardcaseMoveOnAt = SimTime + HardcaseDwellSeconds;
    }

    /// <summary>A SALE, through the one seam a policy is ever set by.</summary>
    private void SignWithKolt(InsuranceTier tier)
    {
        int price = NebulaRep.PremiumFor(tier);
        if (_credits < price)
        {
            // The page's own voice and not his. He has no line for a captain who cannot pay, and putting
            // words in his mouth here would be the one place in this beat a hardcase got a fourth sentence.
            ShowPulseMessage("Not enough credits for that premium.");
            return;
        }

        _credits -= price;
        _insurance = NebulaRep.PolicyAfterBuying(tier, SimTime);
        _contacts.ApplyCredit(
            HardcaseRep.ContactId, HardcaseRep.DisplayName,
            new CreditTransaction(CreditKind.Premium, 0, SimTime, $"{tier} premium · {price} cr"));
        _contacts.AddGoodwill(HardcaseRep.ContactId, HardcaseRep.DisplayName, 1);
        LogAutopilotEvent(HardcaseRep.SaleLedgerNote(tier, price, ActiveCaptainName));
        RequestVaultSave();

        _hardcaseCard = HardcaseRep.OffersFor(_insurance.Tier);
        StateHasChanged();
    }
}
