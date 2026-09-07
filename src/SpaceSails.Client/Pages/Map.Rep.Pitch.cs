using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE PITCH — he is at your elbow, and he is delighted. The card, the four answers, the tier bought, the
/// name that is not yours, and the line he says when he is only passing.
///
/// <para>Split out of <c>Map.Rep.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── The pitch ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// He is at your elbow, and he is delighted.
    ///
    /// <para>#1052 · …UNLESS SOMETHING IS ALREADY WEARING THE SCRIM, in which case he waits. This is the
    /// live collision the one-scrim law was written for: a captain reading the galley card at a bar top
    /// while the salesman crosses the floor got two full-viewport dims on top of each other and a room the
    /// #784 dock exists to keep visible went black. He is HELD rather than turned away — his legs already
    /// brought him here and he is standing at the table — and <c>PumpTheScrimQueue</c> lets him speak on
    /// the first frame the glass is clear. The meeting is still counted where it always was, inside the
    /// raise, because the bleed's cadence counts MEETINGS and a pitch that never landed was not one.</para>
    /// </summary>
    private void HeReachesYourTable() => RaiseAScrimCard(HisPitchGoesUp, () => CaptainIsSeated);

    /// <summary>The pitch itself, once the glass is his. Everything this method does is what "he is at your
    /// elbow" means, which is why the arbiter holds the WHOLE of it rather than the card alone.</summary>
    private void HisPitchGoesUp()
    {
        _repMeetings++;
        _repBleeding = NebulaRep.BleedsThePreviousName(
            _activeThreadId ?? "", _repMeetings, RetiredCaptainCount);
        _repNameOnFile = RepNameOnFile(_repBleeding);
        _repSaid = null;

        // #1151 slice 2 · …AND IF THERE IS A LOSS ON THE WIRE HE HAS NOT ALREADY OFFERED ON, HE OFFERS TO
        // TAKE IT — before the pitch, which is where it goes on the card as well (Map.Claims.Rep.cs). Ahead
        // of the payout below, and the order is load bearing: at the meeting a claim is PAID the offer must
        // not stand, and the payout is what clears the claim the offer reads.
        OfferToLodgeIt();

        _repCard = NebulaRep.PitchFor(_insurance.Tier, _repNameOnFile, _repBleeding);

        // #1151 · …AND IF THERE IS A CLAIM ON THE FILE, THIS IS THE MEETING IT IS PAID AT. The owner's
        // ruling: everything that is not a death is a claim, and the claim's money arrives when a
        // representative next finds you. Here, after the pitch is built and not before — the payout writes
        // `_repSaid`, which the line above clears, so the two statements in the other order would pay the
        // captain and say nothing. This is the clinic bill's idiom run backwards: a bill turns up at the
        // wake-up you did not ask for, and a payout turns up in a bar you did not go to for it.
        PayWhatTheClaimIsWorth();

        // He is a relationship, not a vending machine: the book knows him from the first hello.
        _contacts.AddGoodwill(NebulaRep.ContactId, NebulaRep.DisplayName, 0);
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>Take him off the table and off the card. The body stays on the floor for whatever errand
    /// put it there; only the panel goes.</summary>
    private void CloseTheRepsCard()
    {
        _repCard = null;
        _repSaid = null;
        _repBleeding = false;
        TheRepsCounterCloses();   // #1151 slice 2 · the offer and a finished form go with the conversation
        StateHasChanged();
    }

    /// <summary>
    /// THE CAPTAIN ANSWERS. One door for every button on his card, so a move's meaning cannot drift from
    /// the words on it.
    /// </summary>
    private void AnswerTheRep(NebulaRep.RepMove move)
    {
        if (_repCard is null)
        {
            return;
        }

        switch (move)
        {
            case NebulaRep.RepMove.BuyBasic:
                BuyFromTheRep(InsuranceTier.Basic);
                break;

            case NebulaRep.RepMove.BuyPremium:
                BuyFromTheRep(InsuranceTier.Premium);
                break;

            case NebulaRep.RepMove.AlreadyHaveAPolicy:
                TellHimYouAlreadyHaveOne();
                break;

            case NebulaRep.RepMove.ThatsNotMyName:
                TellHimThatIsNotYourName();
                break;

            case NebulaRep.RepMove.NotToday:
            case NebulaRep.RepMove.GoodDay:
                SendHimToTheBar(move == NebulaRep.RepMove.NotToday);
                break;
        }
    }

    /// <summary>
    /// #973 L2 · THE SIGNING COMES BACK. The captain's line is available whether or not it is true, and it
    /// costs nothing and buys nothing — except that saying it out loud to a man holding the file puts you
    /// back at the counter where you signed.
    ///
    /// <para>The plate is HOSTED on his card (#777's shape, second case): the seam spends the cadence,
    /// files the seen-set and writes the words into the ledger, and the panel already on the screen carries
    /// the picture. The subject stamps the captain's LIFE, which is how "once per subject" becomes "once
    /// per life" with no rebirth hook for anybody to forget.</para>
    /// </summary>
    private void TellHimYouAlreadyHaveOne()
    {
        _repSaid = NebulaRep.PolicyClaimReply(RetiredCaptainCount);

        if (_repSigningToldInLife != CaptainsLife)
        {
            _repSigningToldInLife = CaptainsLife;
            RaiseStoryBeat(StoryBeats.Beat.Flashback, NebulaRep.SigningMemoryId);

            // #973 L3 · …and the afternoon goes into the BLACK BOOK. The plate is what was in the room; the
            // paragraph is a held memory, marked MINE and tagged money, and after a rebirth it GROWS the one
            // line about the hand. Filed on the same edge as the plate and behind the same once-per-life
            // latch — this lane adds no second condition for anybody to keep in step with the first.
            FileTheSigningSheet();
        }

        StateHasChanged();
    }

    /// <summary>He read the wrong line off the file, and the captain says so. He does not explain, and he
    /// never will — but the ship's ledger keeps the one line, so the black book can find it later.</summary>
    private void TellHimThatIsNotYourName()
    {
        _repSaid = NebulaRep.BleedApology;
        LogAutopilotEvent(NebulaRep.BleedLedgerNote(_repNameOnFile));
        _repBleeding = false;
        _repCard = NebulaRep.PitchFor(_insurance.Tier, ActiveCaptainName, bleeding: false);
        StateHasChanged();
    }

    /// <summary>
    /// A SALE. The policy is set the one way a policy is ever set — <see cref="NebulaRep.PolicyAfterBuying"/>
    /// — so #227's vendor lane re-prices one function rather than hunting for a second seam.
    /// </summary>
    private void BuyFromTheRep(InsuranceTier tier)
    {
        int price = NebulaRep.PremiumFor(tier);
        if (_credits < price)
        {
            // The page's own voice and not his: he has no line for a captain who cannot pay, and putting
            // words in his mouth here would be the one place in this feature the salesman got a new script.
            ShowPulseMessage("Not enough credits for that premium.");
            return;
        }

        _credits -= price;
        _insurance = NebulaRep.PolicyAfterBuying(tier, SimTime);
        _contacts.ApplyCredit(
            NebulaRep.ContactId, NebulaRep.DisplayName,
            new CreditTransaction(CreditKind.Premium, 0, SimTime, $"{tier} premium · {price} cr"));
        _contacts.AddGoodwill(NebulaRep.ContactId, NebulaRep.DisplayName, 1);
        LogAutopilotEvent(NebulaRep.SaleLedgerNote(tier, price, ActiveCaptainName));
        RequestVaultSave();

        // He says nothing new about a sale, and that is the character: the card simply refreshes to the
        // tier you now hold and he is already talking about the next one up. His Premium line — "Nothing to
        // sell you, then… It is a comfort, isn't it, a file in order" — is the reward for going all the way.
        _repSaid = null;
        _repCard = NebulaRep.PitchFor(_insurance.Tier, _repNameOnFile, bleeding: false);
        StateHasChanged();
    }

    /// <summary>No. He withdraws to the counter and does not come back this visit — and next visit he
    /// starts over, which is the joke.</summary>
    private void SendHimToTheBar(bool told)
    {
        if (told)
        {
            _repMemory = _repMemory.AtVisit(_repVisitIndex).WithNo();
            ShowPulseMessage(NebulaRep.WithdrawLine);
        }

        CloseTheRepsCard();

        // #973 L0 · Whichever room he is standing in. The walker list is the truth about who is afoot and
        // there are two of them now — a withdrawal that only knew about the Hive would leave a salesman
        // standing at a bar table with no card in his hand, which is the state #731's escort branch refuses.
        if (_surface is { } ex && TheRepAfoot(ex.Walkers) is { } underground)
        {
            // #1061 · …and he walks on from where his feet actually are, which is your elbow. The round is not
            // over: there are other tables in this room, and the man who has just been told no goes to them.
            HeIsStandingHere(underground);
            ex.Walkers.Remove(underground);
        }

        if (TheRepAfoot(_barAfoot) is { } ashore)
        {
            HeIsStandingHere(ashore);
            _barAfoot.Remove(ashore);
        }

        _repMoveOnAt = SimTime + RepDwellSeconds;
    }

    /// <summary>…and if the captain walks past him afterwards, he says the only thing he has left. Once a
    /// visit: a man repeating it every time you cross the room is a different, worse joke.</summary>
    private void MaybeSayHeIsOnlyPassing(IReadOnlyList<Walker> afoot)
    {
        if (_repSaidPassing || _repMemory.MayApproach(_repVisitIndex) || TheRepAfoot(afoot) is not { } who)
        {
            return;
        }

        double dx = who.Walk.X - _avatarX;
        double dy = who.Walk.Y - _avatarY;
        if ((dx * dx) + (dy * dy) > RepPassingReachDu * RepPassingReachDu)
        {
            return;
        }

        _repSaidPassing = true;
        ShowPulseMessage(NebulaRep.PassingLine);
    }
}
