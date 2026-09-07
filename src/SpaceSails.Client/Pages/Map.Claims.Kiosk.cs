using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Claims.Kiosk — #1151 · DEATH IS AUTO-PROCESSED; EVERYTHING ELSE IS A CLAIM.
//
// Owner ruling, 2026-09-06 on #525: "insurance does not know automatically unless we die — deaths are
// registered and auto-processed, everything else is a claim to the rep, gamified: automatic kiosks
// scattered through the system, contactable from the captain's remote in range."
//
// ── WHAT DOES NOT HAPPEN HERE ──────────────────────────────────────────────────────────────────────────
//
// A death still pays exactly as it paid yesterday: `BustedResurrect` asks `InsuranceRule.ApplyToRebirth`
// and nothing in this file is in that path or ever will be. The castaway ending is likewise untouched —
// `SheGoesWithoutHim` still hands over the hull nobody else wanted from the same rule, still empties the
// hold, still charges the nerve. What a non-fatal loss does NOT do is pay the captain a credit, and it never
// did; what is new is that there is now a way for it to, and that way is a process he has to walk to.
//
// ── THE THREE PRESSES, AND WHY THE PRESS VALIDATES ─────────────────────────────────────────────────────
//
// The desk offers rows and the press CHECKS them, rather than the desk only offering rows that would be
// accepted. That is #763's ruling about the kit's own button, applied to a counter: "a press that is going
// to be refused still has to be pressable, because the refusal is the thing worth learning." A lapsed policy
// is on the counter and is refused. Every name the hull has ever answered to is on the counter and only one
// of them is hers. And a wire entry that is not a receipt for a lost hull is refused even if something else
// put it in front of him.
//
// ── AND THE THIRD PRESS IS WHY #535 STILL WORKS ────────────────────────────────────────────────────────
//
// The black-ops key's whole design is that presenting it pushes NOTHING onto the wire — "the scrub is not a
// deletion after the fact: nothing is ever pushed, so there is nothing on the wire to find." A claim needs a
// wire entry. So the key unhappens the claim too, and no line in this file knows the key exists.
public sealed partial class Map
{
    /// <summary>#1151 · How many claims this captain has lodged. The unease is measured on it, and it is not
    /// put back by a rebirth: a file does not forget you died.</summary>
    private int _claimsLodged;

    /// <summary>#1151 · The claim that is lodged and not yet paid, or null. A representative will find you.</summary>
    private LodgedClaimRecord? _claimOwed;

    /// <summary>#1151 · The counter as it stands while the captain is at it — how many of the three presses
    /// have landed, and what the machine took. Null while nobody is at a kiosk.</summary>
    private ClaimDesk? _claimDesk;

    /// <summary>What the machine has taken so far. A record rather than three loose fields, so closing the
    /// card is one assignment and a half-finished claim cannot outlive it.</summary>
    /// <param name="Presses">How many of <see cref="NebulaClaims.Presses"/> have landed.</param>
    /// <param name="Tier">The tier on the policy that was accepted.</param>
    /// <param name="HullName">The name that was accepted for the hull.</param>
    /// <param name="Receipt">The wire entry's subject — what the machine filed the loss against.</param>
    /// <param name="Host">#1151 slice 2 · Which surface the counter is standing on. The state is the PAGE's
    /// and never the fixture's, so a form begun at a machine on a concourse is the form the man at the table
    /// picks up, and the other way about.</param>
    private sealed record ClaimDesk(
        int Presses, InsuranceTier Tier, string HullName, string Receipt, ClaimHost Host);

    /// <summary>
    /// #1151 slice 2 · <b>THE TWO HOSTS OF ONE COUNTER.</b> The rep's own line is the argument for this
    /// enum being the only difference between them: <i>"the machine and I file the same form"</i>. Everything
    /// downstream of a press — the order, the validation, the counter on the vault, the flashback, the
    /// payout — is written once and reads this for nothing except where to draw the rows.
    /// </summary>
    private enum ClaimHost
    {
        /// <summary>A <see cref="NebulaClaims.KioskPlate"/> console on a concourse, or the same machine
        /// raised over the handset's beam.</summary>
        Kiosk,

        /// <summary>A man at your table who has offered to take it, and who is watching your face.</summary>
        Rep,
    }

    // ── THE FIXTURE ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1151 · [E] landed on a console and the label is the kiosk's. Opens the counter, or shows it again if
    /// the captain is already partway through — walking away and coming back does not lose two presses,
    /// because the machine has them and a machine does not forget.
    ///
    /// <para>Returns false for every other label so the ordinary console dispatch carries on, exactly as
    /// <c>TryHeadOfficeConsole</c> and <c>TryTheDroppedSchedule</c> do.</para>
    /// </summary>
    private bool TryTheClaimsKiosk(string label)
    {
        if (!string.Equals(label, NebulaClaims.KioskPlate, StringComparison.Ordinal))
        {
            return false;
        }

        OpenTheCounter(ClaimHost.Kiosk);
        RaiseTheClaimsCard();
        return true;
    }

    /// <summary>Stand at the counter. A claim part-way through is picked up where it was left — walking away
    /// and coming back does not lose two presses, because the machine has them and a machine does not forget
    /// — but a claim that has already been LODGED is finished business, and a captain standing here again
    /// with a second hull to mourn starts a fresh one.
    ///
    /// <para>#1151 slice 2 · A half-filled form is RE-HOSTED rather than restarted: the same two presses,
    /// now in front of whoever the captain is standing at. That is the one implementation the slice is built
    /// around — the man and the machine file the same form, so they cannot hold two of them.</para></summary>
    private void OpenTheCounter(ClaimHost host)
    {
        _claimDesk = _claimDesk is { } open && !NebulaClaims.IsLodged(open.Presses)
            ? open with { Host = host }
            : new ClaimDesk(0, InsuranceTier.None, "", "", host);
    }

    /// <summary>The card the counter is looking at right now. The label is the plate on the machine, the
    /// caption is the machine's own standing demand until the third press lands and its receipt after, and
    /// the outcome region is what it has taken — facts, in the order it took them, never a sentence.</summary>
    private void RaiseTheClaimsCard()
    {
        // #1151 slice 2 · Only the MACHINE has a card of its own to raise. When the host is a man at a table
        // his card is already on the screen and the rows are drawn on it — putting a ViewObject up over him
        // would be the stacked card #777 named, and would take the pitch off the glass mid-sentence.
        if (_claimDesk is not { Host: ClaimHost.Kiosk } desk)
        {
            return;
        }

        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            NebulaClaims.KioskPlate, null, NebulaClaims.DeskLine(desk.Presses), WhatTheMachineHas(desk));
    }

    /// <summary>What is on the counter, joined the way the dossier joins a hull's former names — a fact
    /// list, and empty (so the card renders no outcome region at all) before the first press.</summary>
    private static string? WhatTheMachineHas(ClaimDesk desk)
    {
        List<string> taken = [];
        if (desk.Presses >= 1)
        {
            taken.Add(desk.Tier.ToString());
        }

        if (desk.Presses >= 2)
        {
            taken.Add(desk.HullName);
        }

        if (desk.Presses >= 3)
        {
            taken.Add(desk.Receipt);
        }

        return taken.Count == 0 ? null : string.Join(" · ", taken);
    }

    /// <summary>Is the claims counter the surface the captain is looking at? The card's own gate, so the
    /// press rows appear on this card and on no other <c>ViewObject</c> in the game — and, since slice 2,
    /// only while the MACHINE is the host: the same three rows on a rep's card are his card's business.</summary>
    private bool TheClaimDeskIsUp =>
        _claimDesk is { Host: ClaimHost.Kiosk }
        && _viewObject is { Label: { } label }
        && string.Equals(label, NebulaClaims.KioskPlate, StringComparison.Ordinal);

    // ── WHAT THE COUNTER OFFERS ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1151 · The rows for the press the machine is on. Recomputed from the world every render rather than
    /// stored — a wire entry that has aged off the ledger between two presses is a receipt the captain no
    /// longer has, and a stored row would let him hand over a headline that is not there any more.
    ///
    /// <para>Not one word of any row is authored here: a policy row wears its tier's own name, a hull row
    /// wears a name off the dossier, and a wire row wears <see cref="NewsWire.Headline"/> — the sentence the
    /// wire already wrote when the hull was lost.</para>
    /// </summary>
    private IReadOnlyList<NebulaClaims.Ask> TheClaimAsks()
    {
        if (_claimDesk is not { } desk || NebulaClaims.NextPress(desk.Presses) is not { } press)
        {
            return [];
        }

        switch (press)
        {
            case NebulaClaims.Press.Policy:
                // What is in the wallet, whether or not it is still good. An uninsured captain has nothing to
                // put down and the machine goes on asking, which is the whole of what being uninsured is.
                return _insurance.Tier == InsuranceTier.None
                    ? []
                    : [new NebulaClaims.Ask(press, _insurance.Tier.ToString(), _insurance.Tier.ToString())];

            case NebulaClaims.Press.Hull:
                return NebulaClaims.TheNamesOnFile(ShipNameNow(), ShipHistories.Hers.BareFormerNames);

            default:
                List<NebulaClaims.Ask> wire = [];
                foreach (NewsWire.NewsEvent entry in _newsEvents)
                {
                    if (NebulaClaims.IsAReceipt(entry.Kind))
                    {
                        wire.Add(new NebulaClaims.Ask(press, entry.Subject, NewsWire.Headline(entry)));
                    }
                }

                return wire;
        }
    }

    /// <summary>
    /// #1151 · <b>A PRESS.</b> Accepted or refused, and refused is a real outcome: the press does not land,
    /// the counter does not advance, and the machine's standing line — <i>"Present the policy"</i> — is still
    /// on the card saying what it wants. Nobody authored a refusal sentence and this lane does not invent
    /// one.
    ///
    /// <para>Every arm asks Core, never itself. The policy is asked through
    /// <see cref="NebulaClaims.ThePolicyIsPresentable"/> so a lapsed premium reads as no policy exactly as it
    /// does at the clinic; the hull through <see cref="NebulaClaims.TheNameIsHers"/>; and the wire against
    /// the LIVE ledger, so an entry that is not a receipt is refused even when something else put it in front
    /// of the captain.</para>
    /// </summary>
    private void PressTheClaim(NebulaClaims.Ask ask)
    {
        if (_claimDesk is not { } desk || NebulaClaims.NextPress(desk.Presses) is not { } press
            || ask.Press != press)
        {
            return;
        }

        switch (press)
        {
            case NebulaClaims.Press.Policy:
                if (!NebulaClaims.ThePolicyIsPresentable(_insurance, SimTime))
                {
                    break;
                }

                _claimDesk = desk with { Presses = 1, Tier = _insurance.Tier };
                break;

            case NebulaClaims.Press.Hull:
                if (!NebulaClaims.TheNameIsHers(ask.Offer, ShipNameNow()))
                {
                    break;
                }

                _claimDesk = desk with { Presses = 2, HullName = ask.Offer };
                break;

            default:
                if (TheReceiptOnTheWire(ask.Offer) is not { } receipt)
                {
                    break;
                }

                _claimDesk = desk with { Presses = 3, Receipt = receipt };
                LodgeTheClaim();
                break;
        }

        RaiseTheClaimsCard();
        StateHasChanged();
    }

    /// <summary>The wire entry the captain is pointing at, if it is on the wire NOW and if it is a receipt
    /// for a lost hull. Answers the entry's subject rather than a bool so the counter files what it actually
    /// took.</summary>
    private string? TheReceiptOnTheWire(string subject)
    {
        foreach (NewsWire.NewsEvent entry in _newsEvents)
        {
            if (NebulaClaims.IsAReceipt(entry.Kind) && string.Equals(entry.Subject, subject, StringComparison.Ordinal))
            {
                return entry.Subject;
            }
        }

        return null;
    }

    // ── LODGED ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1151 · <b>THE THIRD PRESS LANDED.</b> The claim goes on the file at the amount
    /// <see cref="InsuranceRule.HullClaimPayoutCr"/> quotes off the policy he is holding TODAY, the counter
    /// the unease is measured on goes up, and nothing is paid — the payout arrives when a representative
    /// next finds him, which is the clinic bill's idiom run backwards.
    ///
    /// <para>And from the second claim onward the desk comes back. The card is raised AFTER the machine's
    /// own receipt line is on the screen, so the order the captain reads them in is the order they happened
    /// in: the machine says a representative will find you, and then he is somewhere else for a moment.</para>
    /// </summary>
    private void LodgeTheClaim()
    {
        if (_claimDesk is not { } desk || !NebulaClaims.IsLodged(desk.Presses))
        {
            return;
        }

        _claimsLodged++;
        _claimOwed ??= new LodgedClaimRecord(
            desk.HullName, InsuranceRule.HullClaimPayoutCr(_insurance, SimTime), SimTime);

        // #1151 slice 2 · …and the rep's offer is spent on this loss, whichever host took the third press.
        // Shared by construction rather than by two seams agreeing: a salesman who offered to file a hull
        // the captain had already filed at a machine would be the firm not knowing its own paperwork.
        _lodgingOfferedFor = desk.Receipt;

        RequestVaultSave();

        if (NebulaClaims.TheDeskComesBack(_claimsLodged))
        {
            RaiseStoryBeat(StoryBeats.Beat.TheClaim);
        }
    }

    // ── THE PAYOUT ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1151 · <b>A REPRESENTATIVE FOUND YOU.</b> Called on the frame Harlan Fess's pitch goes up, which is
    /// the game's own definition of his next meeting — the same statement his bleed counter is spent on.
    ///
    /// <para>The money is the amount the counter quoted on the day, off the record rather than re-asked: a
    /// premium that lapsed between the kiosk and the salesman does not un-lodge a claim, and re-pricing here
    /// would be a second opinion about what a tier is worth. His line goes UNDER the pitch he is already
    /// making — the card is the telling, and it is the one place the third page is ever mentioned.</para>
    /// </summary>
    private void PayWhatTheClaimIsWorth()
    {
        if (_claimOwed is not { } owed)
        {
            return;
        }

        _claimOwed = null;
        _credits += owed.PayoutCr;
        _contacts.ApplyCredit(
            NebulaRep.ContactId, NebulaRep.DisplayName,
            new CreditTransaction(CreditKind.Premium, owed.PayoutCr, SimTime, NebulaClaims.KioskPlate));
        _repSaid = NebulaClaims.RepAtThePayout;
        RequestVaultSave();
    }

    // ── AND FROM THE HANDSET ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1151 · <b>CONTACTABLE FROM THE CAPTAIN'S REMOTE IN RANGE</b> — the owner's own clause, and the range
    /// is not a number invented for it.
    ///
    /// <para>The handset had no reach of its own to borrow: its five switches talk to machines the captain
    /// set down himself or to a boat in the next bay, and none of them is a link to a company. So the link
    /// this uses is the one the game already models for talking to ONE named thing without shouting at the
    /// whole sky — <see cref="ActiveSensors.TightBeamMaxRangeMeters"/>, a directed point-to-point link, far
    /// shorter than the telescope's passive reach. It is measured from the ship, because the ship is what
    /// carries the set. <b>RULED, 2026-09-06:</b> <i>"a claims call from the captain's remote costs the same
    /// exposure a laser ping does — the tight-beam is the tight-beam, whoever is on the other end"</i> — so
    /// raising the counter pays through <see cref="TheBeamIsKeyed"/>, the same till the laser pays at.</para>
    ///
    /// <para>Null when no kiosk is inside it, so the switch is simply not on the handset — unlike SEND
    /// STANDING, which is live in order to refuse, because a refusal there teaches the captain what would fix
    /// it and a kiosk four AU away has nothing to teach.</para>
    /// </summary>
    private string? TheKioskTheRemoteReaches()
    {
        if (_ephemeris is not { } sky)
        {
            return null;
        }

        string? nearest = null;
        double best = double.PositiveInfinity;

        foreach (CelestialBody body in sky.Bodies)
        {
            if (!HavenInterior.HasInterior(body.Id)
                || !NebulaClaims.AKioskStands(ArrivalTube.TierFor(sky, body.Id), body.Id))
            {
                continue;
            }

            double range = (sky.Position(body.Id, SimTime) - _ship.Position).Length;
            if (range <= ActiveSensors.TightBeamMaxRangeMeters && range < best)
            {
                best = range;
                nearest = body.Id;
            }
        }

        return nearest;
    }

    /// <summary>Whether the handset shows the switch at all — the panel's own question, kept beside the
    /// answer so the markup asks one thing.</summary>
    private bool TheRemoteReachesAKiosk() => TheKioskTheRemoteReaches() is not null;

    /// <summary>
    /// #1151 · The switch: the same counter, raised over the beam instead of walked to. The claim state is
    /// the page's and not the fixture's, so a claim begun at a machine on a concourse is the claim the
    /// handset picks up.
    ///
    /// <para><b>And it costs.</b> Owner ruling, 2026-09-06: the beam is the beam whoever is on the other end,
    /// so keying it at a company's machine is paid for at the same till a laser ping pays at
    /// (<see cref="TheBeamIsKeyed"/>) — the far end is the port whose kiosk took the call, and it now knows
    /// where the ship was when the captain lodged. Charged where the beam is actually keyed rather than at
    /// <see cref="TheRemoteReachesAKiosk"/>, which is only the handset asking itself whether to draw a
    /// switch; a captain is not lit up by looking at his own remote.</para>
    /// </summary>
    private void RaiseTheClaimsDesk()
    {
        if (TheKioskTheRemoteReaches() is not { } farEnd)
        {
            return;
        }

        TheBeamIsKeyed(ActiveSensors.Ping(farEnd, _ship.Position, SimTime));
        OpenTheCounter(ClaimHost.Kiosk);
        RaiseTheClaimsCard();
        StateHasChanged();
    }
}
