using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1151 slice 2 · <b>THE INTERVIEW — ON THE PAGE.</b> The booked captain presses the console once more on her watch and
/// the one card sits him at her desk: three questions, each answered by showing a paper (or, for the ship, the policy —
/// the wallet card the collectors' law names), and then the outcome. Every word, every die and every number is Core's
/// (<see cref="ClaimInterview"/>); this file opens the card, presses the rows, and applies what Core settled.
///
/// <para><b>One card, never two.</b> The interview is a <c>ViewObject</c> card in the kiosk's idiom: the ask is its
/// caption, the last answer its outcome region, the rows its own subtree. Pressing a row REPLACES the answer on the
/// same card. It closes by deciding — ✕ at any stage is "get up and leave": nothing is lost (the form is consumed only
/// when the third question is answered), and the next press starts the interview again from the seating, the same
/// rolls, because they are salted from the booking.</para>
///
/// <para><b>The eighth shape's care.</b> The outcome is applied the moment the last question is answered — the credits,
/// the paper swap, the closed row, the stamp — and the card then only READS it. Nothing waits on a close.</para>
/// </summary>
public partial class Map
{
    /// <summary>The interview as it stands while the card is up — which loss, which question, what was last said, and
    /// (once the third question has been answered) what was settled. Null while nobody is seated.</summary>
    private sealed record ClaimInterviewState(
        HullClaim.Loss Loss, ClaimInterview.Stage Stage, string Said, ClaimInterview.Settlement? Settled);

    private ClaimInterviewState? _interview;

    /// <summary>The first filled form the satchel holds whose booked row is still open, or null.</summary>
    private HullClaim.Loss? TheBookedFormHeld()
    {
        foreach (Satchel.Item held in _satchel)
        {
            if (held.Kind == Satchel.Kind.Paper && HullClaim.TryReadFilled(held.Id, out HullClaim.Loss loss)
                && ClaimInterview.IsOpen(_roomsTurnedOver, loss.DoneAt))
            {
                return loss;
            }
        }

        return null;
    }

    /// <summary>#1151 slice 2 · The press: she reads the form, the card raises. Question one stands on it.</summary>
    private void OpenTheInterview(HullClaim.Loss loss)
    {
        _interview = new ClaimInterviewState(loss, ClaimInterview.Stage.Loss, ClaimInterview.SeatingLine, null);
        RaiseTheInterviewCard();
        StateHasChanged();
    }

    /// <summary>The card as it stands: titled with the room's plate (NEBULA MUTUAL is who speaks), her ask as the
    /// caption until the outcome is told, and what was last said under it.</summary>
    private void RaiseTheInterviewCard()
    {
        if (_interview is not { } iv)
        {
            return;
        }

        string caption = iv.Settled is { } s ? s.Told : ClaimInterview.AskOf(iv.Stage);
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            AdjustersRoom.DoorPlate, null, caption, iv.Said);
    }

    /// <summary>Is the interview the surface the captain is looking at? The card's own gate, so the rows are drawn on
    /// this card and on no other <c>ViewObject</c>.</summary>
    private bool TheInterviewIsUp =>
        _interview is { Settled: null }
        && _viewObject is { Label: { } label }
        && string.Equals(label, AdjustersRoom.DoorPlate, StringComparison.Ordinal);

    /// <summary>What the captain can show right now: every paper in the sleeve, the policy on the wallet's face, and —
    /// on the last question only — an explicit nothing. Recomputed every render; empty once nobody is asking.</summary>
    private IReadOnlyList<ClaimInterview.Show> TheInterviewShows()
    {
        if (!TheInterviewIsUp || _interview is not { } iv)
        {
            return [];
        }

        List<ClaimInterview.Show> rows = [];
        foreach (Satchel.Item held in _satchel)
        {
            if (held.Kind == Satchel.Kind.Paper)
            {
                rows.Add(new ClaimInterview.Show(ClaimInterview.ShowKind.Paper, held.Id, FieldClue.Title(held.Id)));
            }
        }

        if (_insurance.Tier != InsuranceTier.None)
        {
            rows.Add(new ClaimInterview.Show(
                ClaimInterview.ShowKind.Policy, _insurance.Tier.ToString(), _insurance.Tier.ToString()));
        }

        if (iv.Stage == ClaimInterview.Stage.Fault)
        {
            rows.Add(new ClaimInterview.Show(ClaimInterview.ShowKind.Nothing, "", ClaimInterview.NothingLabel));
        }

        return rows;
    }

    /// <summary>#1151 slice 2 · <b>A SHOW.</b> Core answers (a wrong paper leaves the question standing and costs
    /// nothing); the third question's answer settles the claim on the spot.</summary>
    private void ShowThePaper(ClaimInterview.Show shown)
    {
        if (!TheInterviewIsUp || _interview is not { } iv)
        {
            return;
        }

        ClaimInterview.Reply reply = ClaimInterview.Step(
            iv.Stage, shown, iv.Loss, NebulaClaims.ThePolicyIsPresentable(_insurance, SimTime),
            _insurance.Tier != InsuranceTier.None);
        _interview = iv with { Stage = reply.Next, Said = reply.Said };

        if (reply.Next == ClaimInterview.Stage.Settled)
        {
            _interview = _interview with { Settled = ClaimInterview.Settle(iv.Loss, reply.Q3Accepted) };
            SettleTheClaim(iv.Loss, _interview.Settled!.Value);
        }

        RaiseTheInterviewCard();
        StateHasChanged();
    }

    /// <summary>
    /// The outcome, applied: the filled form is consumed by EVERY outcome (a declined one comes back as the returned
    /// form, a paper in its own right), the credits move only on PAID and ADJUSTED through the purse's normal path, a
    /// DECLINED outcome stamps the book's loss line, and the booked row closes.
    /// </summary>
    private void SettleTheClaim(HullClaim.Loss loss, ClaimInterview.Settlement settled)
    {
        IReadOnlyList<Satchel.Item> without = Satchel.Remove(_satchel, Satchel.Kind.Paper, HullClaim.FilledId(loss));
        _satchel = [.. Satchel.Add(without, new Satchel.Item(Satchel.Kind.Paper, settled.PaperId))];

        if (settled.Outcome != ClaimInterview.Outcome.Declined)
        {
            _credits += settled.Credits;
        }
        else
        {
            _fieldNotes = [.. ClaimInterview.Stamp(_fieldNotes, loss)];
        }

        _roomsTurnedOver.Add(ClaimInterview.SettledTag(loss.DoneAt));
        TheUneaseComes(loss, settled);
        RequestVaultSave();
    }
}
