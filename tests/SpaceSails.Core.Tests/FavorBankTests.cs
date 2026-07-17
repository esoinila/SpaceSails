namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-WIRE — the favor bank (FridaySecondPlan §0/PR-WIRE, rulings 5 &amp; 6). The book foots (signed
/// balance = Σ transactions); interest is a calm-weather reward only; fencing while heated always
/// undercuts the collector; channels and trust gate who banks how; and a favor debt raises one quiet
/// delivery in the contact's voice.
/// </summary>
public class FavorBankTests
{
    // ---- The ledger's signed credit balance round-trips ----

    [Fact]
    public void CreditBalance_Sums_EveryTransaction_AndSurvivesDeposit_Withdraw()
    {
        var ledger = new ContactLedger();

        ContactHistory afterDeposit = ledger.ApplyCredit("MADAM COIL", "Madam Coil",
            FavorBank.DepositTxn(1000, simTime: 100, "parked 1,000 cr"));
        Assert.Equal(1000, afterDeposit.CreditBalance);        // + = they hold OUR coin

        ContactHistory afterInterest = ledger.ApplyCredit("MADAM COIL", "Madam Coil",
            FavorBank.InterestTxn(50, simTime: 200, "interest"));
        Assert.Equal(1050, afterInterest.CreditBalance);

        ContactHistory afterDraw = ledger.ApplyCredit("MADAM COIL", "Madam Coil",
            FavorBank.WithdrawalTxn(600, simTime: 300, "drew 600 cr"));
        Assert.Equal(450, afterDraw.CreditBalance);

        // The book foots: balance is exactly the sum of every posted amount.
        long sum = 0;
        foreach (CreditTransaction t in afterDraw.Transactions)
        {
            sum += t.Amount;
        }
        Assert.Equal(afterDraw.CreditBalance, sum);
        Assert.Equal(3, afterDraw.Transactions.Length);
    }

    [Fact]
    public void Borrow_MakesBalanceNegative_WeOweThem()
    {
        var ledger = new ContactLedger();
        ContactHistory owed = ledger.ApplyCredit("THE FIXER", "The Fixer",
            FavorBank.BorrowTxn(FavorBank.InterestDebtTotal(300), simTime: 10, "wired 300 cr gas money"));

        Assert.Equal(-360, owed.CreditBalance);   // 300 + 20% premium, and we owe it (negative)
        Assert.True(owed.HasHistory);             // coin in the air is a history
    }

    [Fact]
    public void FavorDebt_RepaidByDelivery_ReturnsToZero()
    {
        var ledger = new ContactLedger();
        ledger.ApplyCredit("MADAM COIL", "Madam Coil",
            FavorBank.BorrowTxn(300, simTime: 10, "favor wire")); // favor debt: principal only, no interest
        ContactHistory settled = ledger.ApplyCredit("MADAM COIL", "Madam Coil",
            FavorBank.RepaymentTxn(300, simTime: 500, "quiet delivery worked off the favor"));

        Assert.Equal(0, settled.CreditBalance);   // the delivery IS the repayment — debt cleared
    }

    [Fact]
    public void PreExistingHistory_ReadsCleanCreditBook_AdditiveSeam()
    {
        // A contact seeded the old way (a mission), never touched by the bank, reads zero balance and an
        // empty passbook — the credit fields are additive, disturbing nothing.
        var ledger = new ContactLedger();
        ContactHistory h = ledger.RecordCompletion("GILT-EYE", "Gilt-Eye", 500, simTime: 5);
        Assert.Equal(0, h.CreditBalance);
        Assert.Empty(h.Transactions);

        // And a default struct is safe — the accessor coalesces the uninitialized array to empty.
        ContactHistory blank = default;
        Assert.Empty(blank.Transactions);
        Assert.Equal(0, blank.CreditBalance);
    }

    // ---- Interest accrues ONLY while calm (heat 0) ----

    [Fact]
    public void Interest_Accrues_WhenCalm()
    {
        // 1,000 cr over a 20-day lay-low at 0.25%/day ≈ 50 cr (the documented number).
        long interest = FavorBank.AccrueInterest(1000, days: 20, heatLevel: 0);
        Assert.Equal(50, interest);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Interest_IsZero_WhileHeated(int heat)
    {
        Assert.Equal(0, FavorBank.AccrueInterest(1000, days: 20, heatLevel: heat));
    }

    [Fact]
    public void Interest_IsZero_ForNonPositiveBalanceOrSpan()
    {
        Assert.Equal(0, FavorBank.AccrueInterest(0, 20, 0));
        Assert.Equal(0, FavorBank.AccrueInterest(-500, 20, 0)); // a debt earns us nothing
        Assert.Equal(0, FavorBank.AccrueInterest(1000, 0, 0));
    }

    // ---- Fencing while heated: the cut is ALWAYS strictly less than confiscation ----

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FenceCut_IsStrictlyLessThanConfiscation_AtEveryHeat_ForEveryRoll(int heat)
    {
        double confiscation = FavorBank.ConfiscationShare(heat);
        // Sweep the whole dice range, endpoints included.
        for (int i = 0; i <= 100; i++)
        {
            double roll = i / 100.0;
            double fence = FavorBank.FenceCutFraction(heat, roll);
            Assert.True(fence < confiscation,
                $"heat {heat}, roll {roll}: fence {fence} must be < confiscation {confiscation}");
            Assert.True(fence >= FavorBank.MinFenceFraction(heat)); // and never below the floor
        }
    }

    [Fact]
    public void FenceCut_IsZero_WhenCalm()
    {
        Assert.Equal(0.0, FavorBank.FenceCutFraction(0, roll: 0.9));
        Assert.Equal(0, FavorBank.FenceCut(1000, heatLevel: 0, roll: 0.9));
    }

    [Fact]
    public void PriceDeposit_Calm_LandsInFull_Heated_LosesTheCut()
    {
        FavorBank.DepositQuote calm = FavorBank.PriceDeposit(1000, heatLevel: 0, roll: 0.5);
        Assert.Equal(1000, calm.Gross);
        Assert.Equal(0, calm.Cut);
        Assert.Equal(1000, calm.Credited);

        FavorBank.DepositQuote hot = FavorBank.PriceDeposit(1000, heatLevel: 3, roll: 0.5);
        Assert.True(hot.Cut > 0);
        Assert.Equal(hot.Gross - hot.Cut, hot.Credited);
        // Still cheaper than the collector's 50% at heat 3.
        Assert.True(hot.Cut < FavorBank.ConfiscationShare(3) * hot.Gross);
    }

    // ---- Channel gating (ruling 6): in person vs the wire ----

    [Fact]
    public void Channel_GatesRemoteBanking_ButNotInPerson()
    {
        ContactSheet coil = ContactSheets.For("MADAM COIL");   // dark-web native
        ContactSheet magpie = ContactSheets.For("THE MAGPIE"); // in person only (the hermit's channel)

        Assert.Equal(BankingChannel.DarkWebWire, coil.Channel);
        Assert.Equal(BankingChannel.InPersonOnly, magpie.Channel);

        Assert.True(FavorBank.CanBankRemotely(coil));   // Coil wires anywhere
        Assert.False(FavorBank.CanBankRemotely(magpie)); // the Magpie won't
        Assert.True(FavorBank.CanBankInPerson(coil));    // both bank across a table
        Assert.True(FavorBank.CanBankInPerson(magpie));
    }

    [Fact]
    public void UnknownContact_DefaultsToInPersonOnly_TheHermitsSettings()
    {
        ContactSheet hermit = ContactSheets.For("SOME ASTEROID HERMIT");
        Assert.Equal(BankingChannel.InPersonOnly, hermit.Channel);
        Assert.False(hermit.CanWire);
    }

    // ---- Trust tier derives from missions; borrowing needs trust AND the wire ----

    [Theory]
    [InlineData(0, TrustTier.Stranger)]
    [InlineData(1, TrustTier.Acquaintance)]
    [InlineData(2, TrustTier.Acquaintance)]
    [InlineData(3, TrustTier.Trusted)]
    [InlineData(5, TrustTier.Trusted)]
    [InlineData(6, TrustTier.Confidant)]
    public void TrustTier_DerivesFromMissions(int missions, TrustTier expected)
    {
        Assert.Equal(expected, ContactSheets.TrustFor(missions));
    }

    [Fact]
    public void CanWireLoan_NeedsTrusted_AndAWireCapableContact()
    {
        ContactSheet coil = ContactSheets.For("MADAM COIL");   // wire-capable
        ContactSheet magpie = ContactSheets.For("THE MAGPIE"); // in person only

        Assert.False(FavorBank.CanWireLoan(coil, missionsCompleted: 2)); // not yet trusted
        Assert.True(FavorBank.CanWireLoan(coil, missionsCompleted: 3));  // trusted + wire → wires you gas
        Assert.False(FavorBank.CanWireLoan(magpie, missionsCompleted: 6)); // trusted, but can't wire
    }

    // ---- The favor-debt obligation: one quiet delivery, in the contact's voice ----

    [Fact]
    public void FavorObligation_IsRaised_InTheContactsVoice()
    {
        ContactSheet coil = ContactSheets.For("MADAM COIL");
        FavorObligation debt = FavorObligation.ForLoan(coil, principal: 300, simTime: 42);

        Assert.Equal("MADAM COIL", debt.ContactId);
        Assert.Equal("Madam Coil", debt.DisplayName);
        Assert.Equal(300, debt.PrincipalCredits);
        Assert.Equal(42, debt.IncurredSimTime);
        Assert.False(string.IsNullOrWhiteSpace(debt.VoiceLine));

        // Different characters call the favor in with different words.
        ContactSheet fixer = ContactSheets.For("THE FIXER");
        Assert.NotEqual(FavorObligation.ObligationVoice(coil), FavorObligation.ObligationVoice(fixer));
    }

    [Fact]
    public void InterestDebtTotal_AddsThePremium()
    {
        Assert.Equal(360, FavorBank.InterestDebtTotal(300)); // 300 + 20%
        Assert.Equal(0, FavorBank.InterestDebtTotal(0));
    }

    [Fact]
    public void Roll_IsDeterministic_AndInRange()
    {
        double a = FavorBank.Roll("MADAM COIL|deposit|1200");
        double b = FavorBank.Roll("MADAM COIL|deposit|1200");
        Assert.Equal(a, b);                       // same seed, same roll — determinism is law
        Assert.InRange(a, 0.0, 1.0);
        Assert.NotEqual(a, FavorBank.Roll("MADAM COIL|deposit|1201")); // different seed, different roll
    }
}
