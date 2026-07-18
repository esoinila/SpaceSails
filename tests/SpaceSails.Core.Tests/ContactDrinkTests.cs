namespace SpaceSails.Core.Tests;

/// <summary>
/// #306 — the drink as a two-edged trust maneuver. Covers the salted-2D6 resolver (determinism, faces
/// in range, the outcome bands, the named modifiers), the goodwill ordering the whole mechanic rests on
/// (a shared drink warms more than a round, refusal debits), the KnownTells leak record, and that the
/// leak survives the Vault round-trip losslessly.
/// </summary>
public class ContactDrinkTests
{
    // ── The goodwill ordering: drink (+3) > round (+1, #283) > 0 > refusal (−2) ──

    [Fact]
    public void SharedDrink_WarmsMoreThanARound_AndRefusalIsADebit()
    {
        Assert.True(ContactDrink.GoodwillPerDrink > 1); // a drink WITH a contact beats a round for the house (#283 +1)
        Assert.True(ContactDrink.RefusalDebit > 0);     // refusing has a real cost
        // The full ordering the mechanic promises: drink > round > refusal-debit.
        Assert.True(ContactDrink.GoodwillPerDrink > 1 && 1 > -ContactDrink.RefusalDebit);
    }

    [Fact]
    public void EveryOutcome_BooksTheSameShared_DrinkGoodwill()
    {
        // Trust rises whichever edge the dice cut — you drank together. Sweep enough seeds to hit
        // every band and assert the booked goodwill never wavers from the shared-drink value.
        for (ulong s = 0; s < 500; s++)
        {
            DrinkParley p = ContactDrink.Roll(s, currentGoodwill: 0, holdingSecret: false);
            Assert.Equal(ContactDrink.GoodwillPerDrink, p.GoodwillDelta);
        }
    }

    // ── Seeded determinism (Core law) ──

    [Fact]
    public void Roll_IsFullyDeterministic_ForTheSameInputs()
    {
        ulong seed = DiceRule.Seed("drink:ONE-EYE SILAS", 4242);
        DrinkParley a = ContactDrink.Roll(seed, currentGoodwill: 4, holdingSecret: true);
        DrinkParley b = ContactDrink.Roll(seed, currentGoodwill: 4, holdingSecret: true);

        Assert.Equal(a.Face1, b.Face1);
        Assert.Equal(a.Face2, b.Face2);
        Assert.Equal(a.Total, b.Total);
        Assert.Equal(a.Outcome, b.Outcome);
    }

    [Fact]
    public void Faces_AreAlwaysTwoHonestD6()
    {
        for (ulong s = 0; s < 1000; s++)
        {
            DrinkParley p = ContactDrink.Roll(s, currentGoodwill: 0, holdingSecret: false);
            Assert.InRange(p.Face1, 1, 6);
            Assert.InRange(p.Face2, 1, 6);
            Assert.InRange(p.Pips, 2, 12);
        }
    }

    // ── The outcome bands ──

    [Theory]
    [InlineData(2, DrinkOutcome.Slip)]
    [InlineData(5, DrinkOutcome.Slip)]
    [InlineData(6, DrinkOutcome.Warm)]
    [InlineData(9, DrinkOutcome.Warm)]
    [InlineData(10, DrinkOutcome.OpensUp)]  // cold contact: a good roll gives intel, not business
    [InlineData(12, DrinkOutcome.OpensUp)]
    public void OutcomeFor_BandsByTotal_WhenTrustIsShallow(int total, DrinkOutcome expected) =>
        Assert.Equal(expected, ContactDrink.OutcomeFor(total, currentGoodwill: 0));

    [Fact]
    public void OutcomeFor_HighRoll_UnlocksBusiness_OnlyOnceTrustIsDeep()
    {
        // Below the business threshold a good roll opens them up (intel); at/above it, business.
        Assert.Equal(DrinkOutcome.OpensUp, ContactDrink.OutcomeFor(11, ContactDrink.TrustForBusiness - 1));
        Assert.Equal(DrinkOutcome.BusinessUnlock, ContactDrink.OutcomeFor(11, ContactDrink.TrustForBusiness));
    }

    [Fact]
    public void Roll_Outcome_AlwaysAgreesWithTheBandOfItsTotal()
    {
        for (ulong s = 0; s < 2000; s++)
        {
            DrinkParley cold = ContactDrink.Roll(s, currentGoodwill: 0, holdingSecret: false);
            Assert.Equal(ContactDrink.OutcomeFor(cold.Total, 0), cold.Outcome);

            DrinkParley trusted = ContactDrink.Roll(s, ContactDrink.TrustForBusiness, holdingSecret: false);
            Assert.Equal(ContactDrink.OutcomeFor(trusted.Total, ContactDrink.TrustForBusiness), trusted.Outcome);
        }
    }

    // ── The named modifiers: warmth helps, a second reality hurts ──

    [Fact]
    public void HoldingASecret_AddsTheTwoRealitiesPenalty_DroppingTheTotalBy2()
    {
        ulong seed = DiceRule.Seed("drink:THE FIXER", 99);
        DrinkParley steady = ContactDrink.Roll(seed, currentGoodwill: 0, holdingSecret: false);
        DrinkParley hiding = ContactDrink.Roll(seed, currentGoodwill: 0, holdingSecret: true);

        Assert.Equal(steady.Pips, hiding.Pips);            // same dice
        Assert.Equal(steady.Total - 2, hiding.Total);      // but two realities cost 2
        Assert.Contains(hiding.Modifiers, m => m.Label == "two realities to hold" && m.Value == -2);
        Assert.DoesNotContain(steady.Modifiers, m => m.Label == "two realities to hold");
    }

    [Fact]
    public void OldFriends_BonusAppears_OnlyOnceWarm()
    {
        ulong seed = DiceRule.Seed("drink:MADAM COIL", 7);
        DrinkParley stranger = ContactDrink.Roll(seed, ContactDrink.WarmThreshold - 1, holdingSecret: false);
        DrinkParley friend = ContactDrink.Roll(seed, ContactDrink.WarmThreshold, holdingSecret: false);

        Assert.DoesNotContain(stranger.Modifiers, m => m.Label == "old friends");
        Assert.Contains(friend.Modifiers, m => m.Label == "old friends" && m.Value == +1);
        Assert.Equal(stranger.Pips + 1, friend.Total);
    }

    [Fact]
    public void Describe_ShowsTheDice_ForTheReceipt()
    {
        ulong seed = DiceRule.Seed("drink:GILT-EYE", 3);
        DrinkParley p = ContactDrink.Roll(seed, currentGoodwill: 0, holdingSecret: true);

        string line = p.Describe();
        Assert.StartsWith("2d6:", line);
        Assert.Contains("two realities to hold", line);
        Assert.Contains("→", line); // the outcome is spelled out for the reveal
    }

    // ── The blade's far edge: the ledger records what a contact now knows ──

    [Fact]
    public void RecordKnownTell_BooksALeak_CreatingTheContactOnFirstSlip()
    {
        var ledger = new ContactLedger();

        ContactHistory after = ledger.RecordKnownTell("gilt-eye", "Gilt-Eye", "the hot ORE in your hold");
        Assert.Contains("the hot ORE in your hold", after.KnownTells);
        Assert.True(after.HasHistory); // a slipped tell is history, even with no job done

        // Books no coin and no goodwill — the honest cost twin of AddGoodwill.
        Assert.Equal(0, after.Goodwill);
        Assert.Equal(0, after.CreditBalance);
    }

    [Fact]
    public void KnownTells_AreDeduped_ASecondSlipOfTheSameFactIsStillOneFact()
    {
        var ledger = new ContactLedger();
        ledger.RecordKnownTell("the-fixer", "The Fixer", "where you're bound — Titan");
        ContactHistory after = ledger.RecordKnownTell("the-fixer", "The Fixer", "where you're bound — Titan");

        Assert.Single(after.KnownTells);
    }

    [Fact]
    public void KnownTells_SurviveTheVaultRoundTrip_AlongsideGoodwill()
    {
        var ledger = new ContactLedger();
        ledger.AddGoodwill("madam-coil", "Madam Coil", ContactDrink.GoodwillPerDrink);
        ledger.RecordKnownTell("madam-coil", "Madam Coil", "that you're running heat (level 2)");

        ContactsSection section = VaultMapper.ToSection(ledger);
        var restored = new ContactLedger();
        VaultMapper.Apply(section, restored);

        ContactHistory back = restored.For("madam-coil");
        Assert.Equal(ContactDrink.GoodwillPerDrink, back.Goodwill);
        Assert.Contains("that you're running heat (level 2)", back.KnownTells);
    }

    [Fact]
    public void DefaultContactHistory_HasNoHistory_SoTheDrinkOptionStaysHiddenForStrangers()
    {
        // The client gates the "buy a drink" row on HasHistory (a KNOWN contact). A never-met contact
        // reads as no-history, so the option — and any effect — never shows without a relationship.
        Assert.False(default(ContactHistory).HasHistory);
        Assert.False(ContactHistory.New("stranger", "Stranger").HasHistory);
        Assert.True(default(ContactHistory).KnownTells.IsEmpty); // default-safe accessor never throws
    }

    // ── #347 The OFFER, resolved before a credit moves: the contact may refuse ──

    [Fact]
    public void OfferDrink_IsFullyDeterministic_ForTheSameInputs()
    {
        ulong seed = DiceRule.Seed("drink-offer:MADAM COIL", 8080);
        DrinkOfferResult a = ContactDrink.OfferDrink(seed, currentGoodwill: 2, holdingSecret: true);
        DrinkOfferResult b = ContactDrink.OfferDrink(seed, currentGoodwill: 2, holdingSecret: true);

        Assert.Equal(a.Face1, b.Face1);
        Assert.Equal(a.Face2, b.Face2);
        Assert.Equal(a.Total, b.Total);
        Assert.Equal(a.Accepted, b.Accepted);
    }

    [Fact]
    public void OfferDrink_Faces_AreAlwaysTwoHonestD6()
    {
        for (ulong s = 0; s < 1000; s++)
        {
            DrinkOfferResult o = ContactDrink.OfferDrink(s, currentGoodwill: 0, holdingSecret: false);
            Assert.InRange(o.Face1, 1, 6);
            Assert.InRange(o.Face2, 1, 6);
            Assert.InRange(o.Pips, 2, 12);
        }
    }

    [Theory]
    [InlineData(ContactDrink.AcceptThreshold - 1, false)] // just under: waved off
    [InlineData(ContactDrink.AcceptThreshold, true)]      // on the line: taken
    [InlineData(12, true)]
    [InlineData(2, false)]
    public void Accepts_IsTheSharedBoundary(int total, bool expected) =>
        Assert.Equal(expected, ContactDrink.Accepts(total));

    [Fact]
    public void OfferResult_AcceptedAlways_AgreesWithItsTotalAgainstTheThreshold()
    {
        for (ulong s = 0; s < 2000; s++)
        {
            DrinkOfferResult o = ContactDrink.OfferDrink(s, currentGoodwill: 1, holdingSecret: false);
            Assert.Equal(ContactDrink.Accepts(o.Total), o.Accepted);
        }
    }

    [Fact]
    public void KnownContact_TakesTheGlassEasier_AndAShiftyReadMakesThemWary()
    {
        // Same dice, three reads of the captain. Warmth lifts the total (+2), a secret drops it (−2), so
        // the SAME faces can be a refusal when you're heated and an accept when a warm contact trusts you.
        ulong seed = DiceRule.Seed("drink-offer:ONE-EYE SILAS", 55);
        DrinkOfferResult cold = ContactDrink.OfferDrink(seed, ContactDrink.WarmThreshold - 1, holdingSecret: false);
        DrinkOfferResult warm = ContactDrink.OfferDrink(seed, ContactDrink.WarmThreshold, holdingSecret: false);
        DrinkOfferResult shifty = ContactDrink.OfferDrink(seed, ContactDrink.WarmThreshold - 1, holdingSecret: true);

        Assert.Equal(cold.Pips, warm.Pips); // same faces throughout
        Assert.Equal(cold.Pips + 2, warm.Total);
        Assert.Contains(warm.Modifiers, m => m.Label == "they know you" && m.Value == +2);
        Assert.DoesNotContain(cold.Modifiers, m => m.Label == "they know you");

        Assert.Equal(cold.Pips - 2, shifty.Total);
        Assert.Contains(shifty.Modifiers, m => m.Label == "you read as shifty" && m.Value == -2);

        // Acceptance never falls as the contact warms and never rises as you turn shifty.
        Assert.True(warm.Total >= cold.Total && cold.Total >= shifty.Total);
    }

    [Fact]
    public void OfferDescribe_ShowsTheDice_AndTheVerdict()
    {
        ulong seed = DiceRule.Seed("drink-offer:THE FIXER", 3);
        DrinkOfferResult o = ContactDrink.OfferDrink(seed, currentGoodwill: 0, holdingSecret: true);

        string line = o.Describe();
        Assert.StartsWith("2d6:", line);
        Assert.Contains("you read as shifty", line);
        Assert.Contains(o.Accepted ? "take the glass" : "wave it off", line);
    }
}
