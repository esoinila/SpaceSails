namespace SpaceSails.Core.Tests;

/// <summary>
/// #370 · THE RESEARCH BRIEF and THE REVEAL. Pins the pure Core spine of the owner's cruise-dessert ruling:
/// the sugar-coated charter BRIEF (optimistic corporate copy per site kind), the seeded REVEAL (the bigger
/// picture in the site's own voice, contradicting the brief), the reveal TIMING, the post-reveal table
/// DARKENING, and the "truth is worth more" PAYOUT bonus. Determinism is law in Core, so every string and
/// every roll is reproducible from the folded accept-moment + site seed.
/// </summary>
public class ExpeditionBriefTests
{
    // ── The art routing ─────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ExpeditionSiteKind.MysticalRuins, "brief-henge.jpg")]
    [InlineData(ExpeditionSiteKind.CrashedHull, "brief-wreck.jpg")]
    [InlineData(ExpeditionSiteKind.SealedTunnel, "brief-tunnel.jpg")]
    public void ArtFile_RoutesEachKind_ToItsDeliveredAsset(ExpeditionSiteKind kind, string file) =>
        Assert.Equal(file, ExpeditionBrief.ArtFile(kind));

    // ── The brief copy ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Brief_IsDeterministic_ForTheSameGig()
    {
        string a = ExpeditionBrief.BriefFor(ExpeditionSiteKind.MysticalRuins, 1000, "expedition-site-ruins");
        string b = ExpeditionBrief.BriefFor(ExpeditionSiteKind.MysticalRuins, 1000, "expedition-site-ruins");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Brief_IsNonEmpty_ForEveryKind_AndCarriesTheHedgeVoice()
    {
        foreach (ExpeditionSiteKind kind in Enum.GetValues<ExpeditionSiteKind>())
        {
            // Sweep seeds so every pool entry is exercised; each must be a real, optimistic sentence.
            for (int t = 0; t < 40; t++)
            {
                string brief = ExpeditionBrief.BriefFor(kind, t, ExpeditionSite.BodyIdFor(kind));
                Assert.False(string.IsNullOrWhiteSpace(brief));
                Assert.True(brief.Length > 40, "the brief should be a couple of sentences of sales copy");
            }
        }
    }

    [Fact]
    public void Brief_Titles_AreCharterServiceMastheads()
    {
        Assert.Contains("CHARTER BRIEF", ExpeditionBrief.Title(ExpeditionSiteKind.MysticalRuins));
        Assert.Contains("CHARTER BRIEF", ExpeditionBrief.Title(ExpeditionSiteKind.CrashedHull));
        Assert.Contains("CHARTER BRIEF", ExpeditionBrief.Title(ExpeditionSiteKind.SealedTunnel));
    }

    // ── The reveal timing ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RevealOrdinal_IsDeterministic_AndInsideTheMidGigBand()
    {
        int a = ExpeditionBrief.RevealOrdinal(500, "expedition-site-wreck");
        int b = ExpeditionBrief.RevealOrdinal(500, "expedition-site-wreck");
        Assert.Equal(a, b);
        Assert.InRange(a, ExpeditionBrief.RevealMinOrdinal, ExpeditionBrief.RevealMaxOrdinal);
    }

    [Fact]
    public void RevealOrdinal_CoversTheWholeBand_AcrossGigs()
    {
        var seen = new HashSet<int>();
        for (int t = 0; t < 200; t++)
        {
            seen.Add(ExpeditionBrief.RevealOrdinal(t, "expedition-site-tunnel"));
        }

        for (int o = ExpeditionBrief.RevealMinOrdinal; o <= ExpeditionBrief.RevealMaxOrdinal; o++)
        {
            Assert.Contains(o, seen);
        }
        Assert.DoesNotContain(ExpeditionBrief.RevealMinOrdinal - 1, seen); // never before the band
        Assert.DoesNotContain(ExpeditionBrief.RevealMaxOrdinal + 1, seen); // never after it
    }

    [Fact]
    public void IsRevealBeat_IsTrueOnlyAtTheSeededOrdinal()
    {
        int reveal = ExpeditionBrief.RevealOrdinal(42, "expedition-site-ruins");
        for (int ord = 0; ord < 10; ord++)
        {
            Assert.Equal(ord == reveal, ExpeditionBrief.IsRevealBeat(42, "expedition-site-ruins", ord));
        }
    }

    // ── The reveal copy — the horror that contradicts the brief, in the site's own voice ─────────────

    [Fact]
    public void Reveal_IsDeterministic_ForTheSameGig()
    {
        RevealCopy a = ExpeditionBrief.RevealFor(ExpeditionSiteKind.SealedTunnel, 7, "expedition-site-tunnel");
        RevealCopy b = ExpeditionBrief.RevealFor(ExpeditionSiteKind.SealedTunnel, 7, "expedition-site-tunnel");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Reveal_HasHeadlineAndBody_ForEveryKind()
    {
        foreach (ExpeditionSiteKind kind in Enum.GetValues<ExpeditionSiteKind>())
        {
            for (int t = 0; t < 40; t++)
            {
                RevealCopy r = ExpeditionBrief.RevealFor(kind, t, ExpeditionSite.BodyIdFor(kind));
                Assert.False(string.IsNullOrWhiteSpace(r.Headline));
                Assert.False(string.IsNullOrWhiteSpace(r.Body));
            }
        }
    }

    [Fact]
    public void Reveal_ContradictsTheBrief_InEachSitesVoice()
    {
        // The henge: markers/warnings, not monuments. Sweep seeds so both pool entries are checked.
        for (int t = 0; t < 40; t++)
        {
            RevealCopy henge = ExpeditionBrief.RevealFor(ExpeditionSiteKind.MysticalRuins, t, "expedition-site-ruins");
            Assert.True(
                henge.Headline.Contains("NOT MONUMENTS") || henge.Headline.Contains("CAGE"),
                $"henge reveal should upend the 'charming masonry' brief: {henge.Headline}");

            // The wreck: scuttled from inside, not crashed.
            RevealCopy wreck = ExpeditionBrief.RevealFor(ExpeditionSiteKind.CrashedHull, t, "expedition-site-wreck");
            Assert.True(
                wreck.Headline.Contains("DID NOT CRASH") || wreck.Headline.Contains("SCUTTLED"),
                $"wreck reveal should upend the 'simply parked itself' brief: {wreck.Headline}");

            // The tunnel: built to keep something IN, not robbers out.
            RevealCopy tunnel = ExpeditionBrief.RevealFor(ExpeditionSiteKind.SealedTunnel, t, "expedition-site-tunnel");
            Assert.True(
                tunnel.Headline.Contains("LOCKS FROM THE OUTSIDE") || tunnel.Headline.Contains("NEVER A TOMB"),
                $"tunnel reveal should upend the 'tastefully locked' brief: {tunnel.Headline}");
        }
    }

    [Fact]
    public void RevealShock_IsBigger_ThanTheHorrorBand()
    {
        // The owner's "a bigger picture revealed" is a MAJOR sanity-throw — heavier than the horror band's.
        Assert.True(ExpeditionBrief.RevealShock > NerveModel.MonolithSightShock,
            "the reveal must hit harder than an ordinary on-site horror");
    }

    // ── The post-reveal darkening ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Roll_AfterTheReveal_DarkensTheTable_TowardTheScares()
    {
        // The bounded −1 "the bigger picture presses in" tilts the same seeds toward the nerve-costing bands.
        int calmScares = 0, revealedScares = 0;
        for (int ord = 0; ord < 400; ord++)
        {
            ulong seed = AwayExpeditionEvents.Seed(2026_0719, "expedition-site-ruins", ord);
            if (AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, ord, revealed: false).NerveHit > 0) calmScares++;
            if (AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, ord, revealed: true).NerveHit > 0) revealedScares++;
        }

        Assert.True(revealedScares >= calmScares,
            $"a revealed table ({revealedScares}) should skew at least as scary as an un-revealed one ({calmScares})");
    }

    [Fact]
    public void Roll_DefaultsToUnrevealed_SoExistingCallersAreUnchanged()
    {
        ulong seed = AwayExpeditionEvents.Seed(1, "expedition-site", 3);
        Assert.Equal(
            AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, 3, revealed: false).Outcome,
            AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, 3).Outcome);
    }

    // ── The "truth is worth more" payout bonus ──────────────────────────────────────────────────────

    [Fact]
    public void TruthBonus_AddsOnTopOfTheGig_WhenWitnessed()
    {
        double r = HaulReward.AstronomicalUnitMeters * 1.5;
        int without = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, discoveryBonus: 0, scientistsLost: 0);
        int witnessed = ExpeditionReward.Total(
            ExpeditionReward.BaseFee, r, r, discoveryBonus: 0, scientistsLost: 0, truthBonus: ExpeditionReward.TruthBonus);

        Assert.Equal(without + ExpeditionReward.TruthBonus, witnessed);
        Assert.True(ExpeditionReward.TruthBonus > 0, "the truth must actually be worth more");
    }

    [Fact]
    public void TruthBonus_ComposesWithDiscoveries_AndOmittedByDefault()
    {
        double r = HaulReward.AstronomicalUnitMeters * 1.5;
        int baseline = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, 0, 0);
        int full = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, discoveryBonus: 800, scientistsLost: 0, truthBonus: 2000);
        Assert.Equal(baseline + 800 + 2000, full);

        // The optional param defaults to no bonus — the old 5-arg callers are unaffected.
        Assert.Equal(baseline, ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, 0, 0));
    }
}
