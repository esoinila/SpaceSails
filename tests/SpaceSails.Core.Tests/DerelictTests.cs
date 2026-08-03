namespace SpaceSails.Core.Tests;

/// <summary>#488 · The wreck: a lost ship, why she was lost, and the choice of what to do about her.
/// Owner: <i>"the accident investigation might be something we could even get paid from … say 10% of the
/// value or we might want to loot it all and never tell anyone. That option might be fun to have."</i>
/// These pin the economics of that fork, and the physics of why she stayed lost.</summary>
public class DerelictTests
{
    private static Derelict.Wreck Wreck(
        Derelict.WreckCause cause, int value = 100_000, double years = 12) =>
        new("wreck-test", "Anna Vale", cause, value, years);

    // ── Why she is hard to find ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheHaystackGrowsWithTheCalendar()
    {
        // A ship that dies under way keeps her velocity. Every year since widens the cone of places she
        // could be — that, and not a dice roll, is why nobody has collected the cargo.
        double young = Derelict.SearchConeRadiusMeters(2);
        double old = Derelict.SearchConeRadiusMeters(40);

        Assert.True(old > young);
        Assert.Equal(20 * young, old, 3);        // linear in the years
        Assert.Equal(0.0, Derelict.SearchConeRadiusMeters(0));
        Assert.Equal(0.0, Derelict.SearchConeRadiusMeters(-5)); // never negative
    }

    [Fact]
    public void ASeededWreckCarriesItsOwnSearchCone()
    {
        Derelict.Wreck w = Derelict.Seeded("wreck-alpha");
        Assert.Equal(Derelict.SearchConeRadiusMeters(w.YearsAdrift), w.SearchConeMeters, 3);
        Assert.True(w.SearchConeMeters > 0, "she has been out here long enough to be genuinely lost");
    }

    // ── The fork ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void StrippingHerPaysEverythingToday_AndItIsAllHot()
    {
        Derelict.Wreck w = Wreck(Derelict.WreckCause.DriveFailure, value: 100_000);

        Derelict.SalvageOutcome o = Derelict.Resolve(w, Derelict.SalvageChoice.StripAndSayNothing, null);

        Assert.Equal(100_000, o.CreditsNow);          // the whole assessed value, now
        Assert.True(o.CargoIsHot);                    // it is somebody's insured cargo
        Assert.Equal(Derelict.QuietSalvageHeat, o.HeatGained);
        Assert.False(o.ContactEarned);                // a ship that was never found makes no friends
    }

    [Fact]
    public void FilingHonestlyPaysTheTenPercentFee_PlusABonusForReadingHerRight()
    {
        Derelict.Wreck w = Wreck(Derelict.WreckCause.HullBreach, value: 100_000);

        Derelict.SalvageOutcome o = Derelict.Resolve(
            w, Derelict.SalvageChoice.FileTheReport, Derelict.WreckCause.HullBreach);

        // #553 · BOTH HALVES ARRIVE NET OF cl. 14(b). Filing is legal work, so it attracts the surcharge — the
        // line item nobody can explain, off the back of an incident nobody will describe. The captain is paying,
        // in credits, for the regulation that drove the research into mountains in the first place.
        int fee = ComplianceSurcharge.Deduct((int)(100_000 * Derelict.ReportFeeFraction));
        int bonus = ComplianceSurcharge.Deduct((int)(100_000 * Derelict.CorrectCauseBonusFraction));
        Assert.Equal(fee + bonus, o.CreditsNow);
        Assert.True(o.ContactEarned);                                   // the part that outlives the payout
        Assert.Equal(0, o.HeatGained);
        Assert.False(o.CargoIsHot);
    }

    [Fact]
    public void AWrongFindingStillPaysTheFee_ButCostsYouTheContact()
    {
        // You did find her, so the finder's fee stands. But a bad report helps nobody, and they remember.
        Derelict.Wreck w = Wreck(Derelict.WreckCause.LifeSupportFailure, value: 100_000);

        Derelict.SalvageOutcome o = Derelict.Resolve(
            w, Derelict.SalvageChoice.FileTheReport, Derelict.WreckCause.Mutiny);

        // …and a wrong finding still pays the surcharge. The clause does not care whether you were right; it
        // cares that you filed.
        Assert.Equal(ComplianceSurcharge.Deduct((int)(100_000 * Derelict.ReportFeeFraction)), o.CreditsNow);
        Assert.False(o.ContactEarned);
        Assert.Contains("did not survive review", o.Line);
    }

    [Fact]
    public void CatchingAStagedLossIsTheBigOne()
    {
        Derelict.Wreck w = Wreck(Derelict.WreckCause.InsuranceJob, value: 100_000);

        Derelict.SalvageOutcome caught = Derelict.Resolve(
            w, Derelict.SalvageChoice.FileTheReport, Derelict.WreckCause.InsuranceJob);
        Derelict.SalvageOutcome ordinary = Derelict.Resolve(
            Wreck(Derelict.WreckCause.HullBreach, 100_000),
            Derelict.SalvageChoice.FileTheReport, Derelict.WreckCause.HullBreach);

        Assert.True(caught.CreditsNow > ordinary.CreditsNow,
            "an underwriter about to pay out on a fraud pays more than a routine finding");
        Assert.True(caught.ContactEarned);
        Assert.Contains("staged", caught.Line);
    }

    [Fact]
    public void HonestyIsNeverThePurelyRicherChoice_SoTheForkIsARealDecision()
    {
        // The whole point of the owner's fork: looting is the FAST win. Filing must not simply dominate it,
        // or there is no decision — the payoff for honesty is the contact and the clean hold, not the cash.
        Derelict.Wreck w = Wreck(Derelict.WreckCause.DriveFailure, value: 100_000);

        int filed = Derelict.Resolve(w, Derelict.SalvageChoice.FileTheReport, w.Cause).CreditsNow;
        int stripped = Derelict.Resolve(w, Derelict.SalvageChoice.StripAndSayNothing, null).CreditsNow;

        Assert.True(stripped > filed, "stripping her must always pay more TODAY");
    }

    [Fact]
    public void EvenTheFraudBountyStaysUnderTheFullValue()
    {
        // Catching a staged loss should be the best FILING, not better than simply taking everything —
        // otherwise the "loot it and say nothing" road stops being tempting and the fork collapses.
        Derelict.Wreck w = Wreck(Derelict.WreckCause.InsuranceJob, value: 100_000);

        int filed = Derelict.Resolve(w, Derelict.SalvageChoice.FileTheReport, w.Cause).CreditsNow;
        int stripped = Derelict.Resolve(w, Derelict.SalvageChoice.StripAndSayNothing, null).CreditsNow;

        Assert.True(filed < stripped);
    }

    // ── The wrecks themselves ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryCauseHasAHeadlineAndPhysicalEvidence()
    {
        // The investigation is read off what is bolted to the deck, not off a tooltip. A cause with no
        // evidence is a cause the away team cannot solve.
        foreach (Derelict.WreckCause c in System.Enum.GetValues<Derelict.WreckCause>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Derelict.CauseHeadline(c)), $"{c} has no headline");
            Assert.False(string.IsNullOrWhiteSpace(Derelict.Evidence(c)), $"{c} leaves nothing to find");
        }
    }

    [Fact]
    public void SomeWrecksLie_AndAStagedLossIsDressedAsAnOrdinaryFailure()
    {
        // If every wreck read cleanly, naming the cause would be a lookup rather than a skill.
        Assert.Equal(Derelict.WreckCause.DriveFailure, Derelict.MisreadsAs(Derelict.WreckCause.InsuranceJob));
        Assert.NotNull(Derelict.MisreadsAs(Derelict.WreckCause.ReactorCascade));

        // …and a misread is never the truth itself.
        foreach (Derelict.WreckCause c in System.Enum.GetValues<Derelict.WreckCause>())
        {
            Assert.NotEqual(c, Derelict.MisreadsAs(c));
        }
    }

    [Fact]
    public void ASeededWreckIsAlwaysTheSameWreck()
    {
        // A rumour that names her must be trustworthy: the same id is the same ship, cargo and cause.
        Derelict.Wreck a = Derelict.Seeded("wreck-kestrel");
        Derelict.Wreck b = Derelict.Seeded("wreck-kestrel");

        Assert.Equal(a, b);
        Assert.False(string.IsNullOrWhiteSpace(a.ShipName));
        Assert.InRange(a.AssessedValueCr, 40_000, 320_000);
    }

    [Fact]
    public void DifferentWrecksAreDifferentShips()
    {
        var causes = new System.Collections.Generic.HashSet<Derelict.WreckCause>();
        var names = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 40; i++)
        {
            Derelict.Wreck w = Derelict.Seeded($"wreck-{i}");
            causes.Add(w.Cause);
            names.Add(w.ShipName);
        }

        Assert.True(causes.Count > 3, "the taxonomy should actually get used");
        Assert.True(names.Count > 3, "and they should not all be the same ship");
    }

    [Fact]
    public void BothRoadsQuoteRealNumbersOnTheChoiceCard()
    {
        // The captain must never be guessing what they are choosing between.
        Derelict.Wreck w = Wreck(Derelict.WreckCause.Piracy, value: 200_000);

        string file = Derelict.DescribeChoice(w, Derelict.SalvageChoice.FileTheReport);
        string strip = Derelict.DescribeChoice(w, Derelict.SalvageChoice.StripAndSayNothing);

        Assert.Contains("20,000", file);    // the 10% fee, spelled out
        Assert.Contains("200,000", strip);  // the whole value, spelled out
        Assert.Contains(w.ShipName, strip);
    }

    // ── Routing (the client branches on this alone) ───────────────────────────────────────────────────

    [Fact]
    public void AWreckBodyIdRoundTrips()
    {
        string bodyId = Derelict.BodyIdFor("kestrel-3");

        Assert.True(Derelict.TryParseWreckId(bodyId, out string back));
        Assert.Equal("kestrel-3", back);
    }

    [Theory]
    [InlineData("miranda")]
    [InlineData("expedition-site-ruins")]
    [InlineData("wreck-")]      // the prefix alone names no wreck
    [InlineData("")]
    [InlineData(null)]
    public void OrdinaryBodiesAreNotWrecks(string? bodyId)
    {
        Assert.False(Derelict.TryParseWreckId(bodyId, out string id));
        Assert.Equal(string.Empty, id);
    }

    [Fact]
    public void AWorthlessWreckNeverPaysNegative()
    {
        Derelict.Wreck w = Wreck(Derelict.WreckCause.Mutiny, value: 0);

        Assert.Equal(0, Derelict.Resolve(w, Derelict.SalvageChoice.FileTheReport, w.Cause).CreditsNow);
        Assert.Equal(0, Derelict.Resolve(w, Derelict.SalvageChoice.StripAndSayNothing, null).CreditsNow);
    }
}
