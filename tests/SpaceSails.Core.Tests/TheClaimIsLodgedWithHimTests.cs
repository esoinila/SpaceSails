namespace SpaceSails.Core.Tests;

/// <summary>
/// #1151 slice 2 · <b>LODGING THE CLAIM WITH THE REP — THE RULE.</b> Canon pass, 2026-09-06 on #1151:
/// <i>"At Fess's or Kolt's table, with a loss on the wire and no claim lodged, the rep offers it before the
/// pitch… The three presses are the same three on the rep's card instead of the kiosk's; the counter and the
/// flashback are shared; the payout is the same number, on the same next meeting."</i>
///
/// <para><b>What is asked here and what is asked next door.</b> This file is the OFFER: when it stands, when
/// it does not, which loss it is about, and the one sentence it says. That the two hosts then run one
/// implementation of the three presses is not a thing Core can see — it is a fact about the page — so it is
/// driven in <c>TheClaimIsWalkedEndToEndTests</c>, where a claim begun at a machine is finished at a table
/// and the counter, the flashback and the payout are compared against a kiosk-only run of the same world.</para>
///
/// <para>Every clause below is denied in a world that could answer the other way: an offer that stood on an
/// empty wire, or on a claim the firm already owes for, or twice on one hull, is each shown refused with the
/// other two clauses satisfied — so no guard here can be green because something else was false.</para>
/// </summary>
public sealed class TheClaimIsLodgedWithHimTests
{
    private const string Loss = "SALT WIDOW";
    private const string OtherLoss = "GRIMHOLD";

    private static NewsWire.NewsEvent Entry(NewsWire.NewsEventKind kind, string subject) =>
        new(kind, 100.0, subject, null);

    // ══ 1 · THE ONE SENTENCE ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE LINE, WORD FOR WORD OFF THE CANON PASS.</b> Retyped here on purpose — this file is the second
    /// copy, and its whole job is to go red the day somebody edits the first one.
    /// </summary>
    [Fact]
    public void THE_LINE_IsWordForWordWhatTheCanonPassAuthored() =>
        Assert.Equal(
            "Lodge it with me, then. The machine and I file the same form; I just get to watch your face "
            + "while you do it.",
            NebulaClaims.LodgeWithMe);

    /// <summary>
    /// <b>AND IT IS THE ONLY STRING THIS SLICE ADDED.</b> The reflection sweep that proves it lives in
    /// <c>TheClaimIsTheSceneTests.THE_RULE_HandsOutNoStringNobodyAuthored</c> — one sweep over one type, not
    /// two that could disagree. What is asked here is the half that sweep cannot ask: that the sentence the
    /// rep says is HIS and not the machine's, so nobody can "reuse" one for the other and lose the joke.
    /// </summary>
    [Fact]
    public void THE_LINE_IsHisAndNotAnythingTheMachineSays()
    {
        Assert.NotEqual(NebulaClaims.OnApproach, NebulaClaims.LodgeWithMe);
        Assert.NotEqual(NebulaClaims.LodgedLine, NebulaClaims.LodgeWithMe);
        Assert.NotEqual(NebulaClaims.RepAtThePayout, NebulaClaims.LodgeWithMe);

        // He offers to take the form, and he says what the difference is: a man watching. If the sentence
        // ever stopped saying that, the whole slice would be a shortcut with no scene on it.
        Assert.Contains("the same form", NebulaClaims.LodgeWithMe, StringComparison.Ordinal);
        Assert.Contains("watch your face", NebulaClaims.LodgeWithMe, StringComparison.Ordinal);
    }

    // ══ 2 · WHICH LOSS HE IS TALKING ABOUT ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE LOSS ON THE WIRE IS THE FRESHEST RECEIPT AND NOTHING ELSE.</b> The wire is kept newest-first,
    /// so the first entry <see cref="NebulaClaims.IsAReceipt"/> would take is the hull he means — and a
    /// headline that is not a receipt for a lost hull is not a loss he can offer to file, however loudly the
    /// wire wrote it.
    /// </summary>
    [Fact]
    public void THE_LOSS_OnTheWireIsTheFreshestReceiptAndNoOtherHeadline()
    {
        Assert.Null(NebulaClaims.TheLossOnTheWire([]));

        // A wire with plenty on it and no receipt at all: nothing to claim on.
        NewsWire.NewsEvent[] noise =
        [
            Entry(NewsWire.NewsEventKind.RobberyCommitted, "SOMEBODY"),
            Entry(NewsWire.NewsEventKind.HunterDispatched, "SOMEBODY ELSE"),
        ];
        Assert.Null(NebulaClaims.TheLossOnTheWire(noise));

        // …and the same wire with a loss laid on top of it, newest first.
        NewsWire.NewsEvent[] wire =
        [
            Entry(NewsWire.NewsEventKind.HullLostAtABerth, Loss),
            Entry(NewsWire.NewsEventKind.HunterBrokeOff, OtherLoss),
            .. noise,
        ];
        Assert.Equal(Loss, NebulaClaims.TheLossOnTheWire(wire));

        // The freshest, not merely "a" receipt: with the older one first the answer changes, which is what
        // makes the guard above about ORDER rather than about membership.
        Assert.Equal(OtherLoss, NebulaClaims.TheLossOnTheWire(
            [Entry(NewsWire.NewsEventKind.HunterBrokeOff, OtherLoss), .. wire]));

        // And a receipt buried under noise is still found — he reads the file, he does not glance at it.
        Assert.Equal(Loss, NebulaClaims.TheLossOnTheWire([.. noise, Entry(NewsWire.NewsEventKind.HullLostAtABerth, Loss)]));
    }

    // ══ 3 · WHEN HE OFFERS ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE OFFER STANDS ON THREE CLAUSES AND FALLS ON ANY ONE OF THEM.</b> Each row denies exactly one
    /// clause with the other two satisfied, so no refusal below can be green for somebody else's reason —
    /// and the all-satisfied row is in the same table, which is what stops the whole thing being a function
    /// that always says no.
    /// </summary>
    [Theory]
    // the loss, is a claim already owed, spent on, does he offer
    [InlineData(Loss, false, null, true)]         // a hull on the wire, nothing owed, nothing said: he offers
    [InlineData(null, false, null, false)]        // no loss on the wire — there is no claim without a receipt
    [InlineData("", false, null, false)]          // …and an empty subject is no receipt either
    [InlineData(Loss, true, null, false)]         // the firm already owes for a hull: that meeting is the payout
    [InlineData(Loss, false, Loss, false)]        // he has already said it about this hull. Once per loss
    [InlineData(Loss, false, OtherLoss, true)]    // …but a DIFFERENT hull is a different form
    public void THE_OFFER_StandsOnALossNoOneIsOwedForAndUnspoken(
        string? loss, bool owed, string? spentOn, bool offers) =>
        Assert.Equal(offers, NebulaClaims.TheOfferStands(loss, owed, spentOn));

    /// <summary>
    /// <b>ONCE PER LOSS IS ORDINAL AND EXACT.</b> The latch holds the wire entry's own subject, so "spent"
    /// means the same hull and not a hull that reads like it — a rule that matched loosely would let one
    /// callsign silence the offer for another.
    /// </summary>
    [Fact]
    public void THE_LATCH_IsTheLossItselfAndNotSomethingThatLooksLikeIt()
    {
        Assert.False(NebulaClaims.TheOfferStands(Loss, aClaimIsOwed: false, offerSpentOn: Loss));
        Assert.True(NebulaClaims.TheOfferStands(Loss, aClaimIsOwed: false, offerSpentOn: Loss.ToLowerInvariant()));
        Assert.True(NebulaClaims.TheOfferStands(Loss, aClaimIsOwed: false, offerSpentOn: Loss + " II"));
    }

    /// <summary>
    /// <b>AND THE OFFER IS ABOUT A LOSS, NEVER ABOUT A DEATH.</b> A death is auto-processed — the owner's
    /// whole ruling — so no headline about one is a thing a rep can offer to file. Asked through the two
    /// functions in the order the seam asks them, because that is the only place the mistake could be made.
    /// </summary>
    [Fact]
    public void THE_OFFER_IsNeverAboutADeathBecauseADeathIsProcessedForYou()
    {
        foreach (NewsWire.NewsEventKind kind in Enum.GetValues<NewsWire.NewsEventKind>())
        {
            string? loss = NebulaClaims.TheLossOnTheWire([Entry(kind, Loss)]);
            bool offers = NebulaClaims.TheOfferStands(loss, aClaimIsOwed: false, offerSpentOn: null);
            Assert.Equal(NebulaClaims.IsAReceipt(kind), offers);
        }
    }
}
