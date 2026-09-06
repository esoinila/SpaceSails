using System.Reflection;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1151 · <b>THE CLAIM IS THE SCENE — THE RULE.</b> Owner ruling, 2026-09-06 on #525: <i>"insurance does not
/// know automatically unless we die — deaths are registered and auto-processed, everything else is a claim to
/// the rep, gamified: automatic kiosks scattered through the system."</i>
///
/// <para>Every guard here is asked in a world that can answer the other way. The placement sweep runs against
/// the REAL <c>sol.json</c> and asserts the working berths are SPLIT — one with a machine and one without —
/// because a deal that came out all-yes or all-no would make "a dealt share" a sentence in a comment rather
/// than a fact about the game. The payout is asked of a policy that pays and a policy that has lapsed. And
/// the death path is compared against numbers computed WITHOUT this lane's function at all, so a change to
/// the claim cannot quietly re-price a rebirth.</para>
/// </summary>
public sealed class TheClaimIsTheSceneTests
{
    private static ICelestialEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(ScenarioLoader.LoadFile(
            Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json")));

    private static PirateInsurance Basic(double through) => new(InsuranceTier.Basic, through);

    private static PirateInsurance Premium(double through) => new(InsuranceTier.Premium, through);

    // ══ 1 · A DEATH PAYS EXACTLY AS IT PAID YESTERDAY ════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE ONE THING THIS LANE MAY NOT HAVE CHANGED.</b> The rebirth is the auto-processed path and it is
    /// out of scope of the claim entirely, so the numbers are restated here from the tiers' own promises —
    /// Basic halves the clinic bill, Premium waives it and hands a mid-grade hull, uninsured pays the whole
    /// bill — computed WITHOUT <see cref="InsuranceRule.HullClaimPayoutCr"/> anywhere in the arithmetic.
    ///
    /// <para>That independence is the point. A guard that asked the claim function what a death costs would
    /// go green on the day somebody wired the two together, which is precisely the mistake worth catching.</para>
    /// </summary>
    [Fact]
    public void A_DEATH_IsProcessedExactlyAsItWasBeforeTheClaimExisted()
    {
        RebirthOutcome uninsured = InsuranceRule.DefaultRebirth(wakeTankPulses: 500);

        Assert.Equal(InsuranceRule.BaseClinicBillCr, uninsured.ClinicBillCr);
        Assert.Equal(uninsured, InsuranceRule.ApplyToRebirth(PirateInsurance.Uninsured, 1000, uninsured));

        RebirthOutcome basic = InsuranceRule.ApplyToRebirth(Basic(9999), 1000, uninsured);
        Assert.Equal(InsuranceRule.BaseClinicBillCr / 2, basic.ClinicBillCr);
        Assert.Equal(uninsured.Kit, basic.Kit);                       // Basic buys the bill down, not a hull

        RebirthOutcome premium = InsuranceRule.ApplyToRebirth(Premium(9999), 1000, uninsured);
        Assert.Equal(0, premium.ClinicBillCr);
        Assert.Equal(1, premium.Kit.MassLevel);

        // …and a lapsed premium is uninsured at the clinic, which is the sentence the kiosk repeats.
        Assert.Equal(uninsured, InsuranceRule.ApplyToRebirth(Premium(500), 900, uninsured));
    }

    /// <summary>
    /// <b>THE PAYOUT IS WHAT A DEATH WOULD HAVE BEEN FORGIVEN</b>, and it is quoted rather than typed: the
    /// expected value on the right is built out of <see cref="InsuranceRule.ApplyToRebirth"/> here, so if the
    /// two ever stop agreeing this goes red instead of one of them silently becoming the number.
    /// </summary>
    [Theory]
    [InlineData(InsuranceTier.None)]
    [InlineData(InsuranceTier.Basic)]
    [InlineData(InsuranceTier.Premium)]
    public void THE_PAYOUT_IsTheClinicBillTheTierWouldHaveForgiven(InsuranceTier tier)
    {
        var policy = new PirateInsurance(tier, 9999);
        RebirthOutcome uninsured = InsuranceRule.DefaultRebirth(0);
        int forgiven = uninsured.ClinicBillCr - InsuranceRule.ApplyToRebirth(policy, 1000, uninsured).ClinicBillCr;

        Assert.Equal(forgiven, InsuranceRule.HullClaimPayoutCr(policy, 1000));
    }

    /// <summary>The three answers, stated as numbers as well, so the theory above cannot be green because both
    /// sides are zero. Uninsured pays nothing at all; the two tiers pay something and pay differently.</summary>
    [Fact]
    public void THE_PAYOUT_IsNothingUninsuredAndDifferentPerTier()
    {
        Assert.Equal(0, InsuranceRule.HullClaimPayoutCr(PirateInsurance.Uninsured, 1000));
        Assert.Equal(0, InsuranceRule.HullClaimPayoutCr(Premium(500), 900));   // lapsed — no claim
        Assert.Equal(InsuranceRule.BaseClinicBillCr / 2, InsuranceRule.HullClaimPayoutCr(Basic(9999), 1000));
        Assert.Equal(InsuranceRule.BaseClinicBillCr, InsuranceRule.HullClaimPayoutCr(Premium(9999), 1000));
        Assert.True(InsuranceRule.HullClaimPayoutCr(Premium(9999), 1000)
                  > InsuranceRule.HullClaimPayoutCr(Basic(9999), 1000));
    }

    // ══ 2 · THE THREE PRESSES, EACH REFUSING THE WRONG THING ═════════════════════════════════════════════

    /// <summary>THE ORDER IS THE PROCESS. Policy, then hull, then the proof anything happened to it — asked
    /// through one function so the counter cannot grow a second opinion, and lodged only when all three have
    /// landed.</summary>
    [Fact]
    public void THE_PRESSES_ComeInOneOrderAndTheThirdIsTheLodging()
    {
        Assert.Equal(NebulaClaims.Press.Policy, NebulaClaims.NextPress(0));
        Assert.Equal(NebulaClaims.Press.Hull, NebulaClaims.NextPress(1));
        Assert.Equal(NebulaClaims.Press.Wire, NebulaClaims.NextPress(2));
        Assert.Null(NebulaClaims.NextPress(NebulaClaims.Presses));

        Assert.False(NebulaClaims.IsLodged(2));
        Assert.True(NebulaClaims.IsLodged(NebulaClaims.Presses));

        // And the machine says one thing until the third press lands and another after — two sentences, and
        // there is no third for a refusal because nobody authored one.
        Assert.Equal(NebulaClaims.OnApproach, NebulaClaims.DeskLine(0));
        Assert.Equal(NebulaClaims.OnApproach, NebulaClaims.DeskLine(2));
        Assert.Equal(NebulaClaims.LodgedLine, NebulaClaims.DeskLine(NebulaClaims.Presses));
    }

    /// <summary>PRESS ONE. A policy in force is presentable; nothing and a lapsed something are not — and the
    /// lapse is asked at the moment of asking, so the same premium is good on Tuesday and refused on
    /// Thursday.</summary>
    [Fact]
    public void PRESS_ONE_RefusesNoPolicyAndALapsedOneAndTakesAPolicyInForce()
    {
        Assert.False(NebulaClaims.ThePolicyIsPresentable(PirateInsurance.Uninsured, 1000));
        Assert.False(NebulaClaims.ThePolicyIsPresentable(Basic(500), 900));
        Assert.True(NebulaClaims.ThePolicyIsPresentable(Basic(500), 400));
        Assert.True(NebulaClaims.ThePolicyIsPresentable(Premium(9999), 1000));
    }

    /// <summary>A hull the dossier gives a past to — asked of the shipped generator rather than typed, so
    /// the refusals below are about names the game really deals. Asserted to exist.</summary>
    private static ShipHistory AHullWithAPast()
    {
        for (int i = 0; i < 200; i++)
        {
            ShipHistory h = ShipHistories.For($"npc-{i}");
            if (h.BareFormerNames.Count > 1)
            {
                return h;
            }
        }

        throw new InvalidOperationException(
            "the dossier generator deals no hull in two hundred a second name — every refusal below would "
            + "be about an empty list.");
    }

    /// <summary>
    /// PRESS TWO. The name she answers to is taken; every name she used to answer to is refused; and so are
    /// a name off somebody else's hull, an empty press and a near miss in the wrong case — a desk being
    /// helpful about case is a desk that would take <i>aurora queen</i> for her.
    /// </summary>
    [Fact]
    public void PRESS_TWO_TakesHerNameAndRefusesEveryNameSheUsedToHave()
    {
        const string hers = "THIS SHIP";
        IReadOnlyList<string> was = AHullWithAPast().BareFormerNames;

        Assert.True(NebulaClaims.TheNameIsHers(hers, hers));
        Assert.False(NebulaClaims.TheNameIsHers(null, hers));
        Assert.False(NebulaClaims.TheNameIsHers("  ", hers));
        Assert.False(NebulaClaims.TheNameIsHers("this ship", hers));
        Assert.False(NebulaClaims.TheNameIsHers("GRIMHOLD", hers));
        Assert.All(was, name => Assert.False(NebulaClaims.TheNameIsHers(name, hers),
            $"the desk took {name}, which is a name this hull has not answered to for two owners."));
    }

    /// <summary>…and the counter really does put every one of them in front of him, hers first. A rule the
    /// player can only be told about is not a refusal; this is the row he can press.</summary>
    [Fact]
    public void PRESS_TWO_PutsHerNameAndEveryFormerOneOnTheCounter()
    {
        const string hers = "THIS SHIP";
        IReadOnlyList<NebulaClaims.Ask> rows =
            NebulaClaims.TheNamesOnFile(hers, AHullWithAPast().BareFormerNames);

        Assert.All(rows, r => Assert.Equal(NebulaClaims.Press.Hull, r.Press));
        Assert.Equal(hers, rows[0].Offer);
        Assert.All(rows, r => Assert.Equal(r.Offer, r.Label));      // no word of a row is authored
        Assert.True(rows.Count > 1, "a counter with one row on it cannot be got wrong.");
        Assert.Single(rows, r => NebulaClaims.TheNameIsHers(r.Offer, hers));

        // A hull with no past at all still gets her own row and nothing else — the empty case is a real one
        // and it must not throw or offer a blank.
        IReadOnlyList<NebulaClaims.Ask> maiden = NebulaClaims.TheNamesOnFile(hers, null);
        Assert.Single(maiden);
        Assert.Equal(hers, maiden[0].Offer);
    }

    /// <summary>
    /// <b>THE RATCHET CAME OFF, AND HERE IS THE SCENE IT WAS HOLDING.</b> Slice 1 filed
    /// <c>HER_OWN_DOSSIER_HasNoFormerNameYet_WhichIsFiledRatherThanFixed</c> because the counter reads the
    /// captain's own dossier and that dossier dealt him no former name — so press two was ONE row and the
    /// refusal it is built around had nothing in the shipping world to exercise it. Owner ruling,
    /// 2026-09-06: <i>"she gets her glory name."</i>
    ///
    /// <para>So this is the same assertion turned the right way up: on the counter the shipping game puts
    /// in front of the captain, at HER dossier and not a hull picked out of the generator, there are two
    /// rows — the name she answers to, taken, and the name she used to answer to, refused.</para>
    /// </summary>
    [Fact]
    public void HER_OWN_DOSSIER_PutsAGloryNameOnTheCounterForPressTwoToRefuse()
    {
        const string hers = "THIS SHIP";
        Assert.NotNull(ShipHistories.Hers.GloryName);

        IReadOnlyList<NebulaClaims.Ask> rows =
            NebulaClaims.TheNamesOnFile(hers, ShipHistories.Hers.BareFormerNames);

        Assert.Equal(2, rows.Count);
        Assert.Equal(hers, rows[0].Offer);
        Assert.Equal(ShipHistories.Hers.GloryName, rows[1].Offer);

        Assert.True(NebulaClaims.TheNameIsHers(rows[0].Offer, hers));
        Assert.False(
            NebulaClaims.TheNameIsHers(rows[1].Offer, hers),
            "the desk took her glory name, which is a name this hull has not answered to for two owners.");
    }

    /// <summary>
    /// PRESS THREE, AND THE ONE WITH TEETH. Two headlines are a receipt for a lost hull, and every other kind
    /// on the wire is refused — including the ones that are ABOUT the same trouble (a collector dispatched, a
    /// robbery committed), because being about the trouble is not being proof the hull is gone.
    ///
    /// <para>Swept over the whole enum rather than spot-checked, so a headline added tomorrow is refused by
    /// default and somebody has to decide out loud that it is a receipt.</para>
    /// </summary>
    [Fact]
    public void PRESS_THREE_TakesTheTwoLossHeadlinesAndRefusesEveryOtherKind()
    {
        Assert.True(NebulaClaims.IsAReceipt(NewsWire.NewsEventKind.HullLostAtABerth));
        Assert.True(NebulaClaims.IsAReceipt(NewsWire.NewsEventKind.HunterBrokeOff));
        Assert.False(NebulaClaims.IsAReceipt(NewsWire.NewsEventKind.HunterDispatched));
        Assert.False(NebulaClaims.IsAReceipt(NewsWire.NewsEventKind.RobberyCommitted));

        NewsWire.NewsEventKind[] receipts =
            [.. Enum.GetValues<NewsWire.NewsEventKind>().Where(NebulaClaims.IsAReceipt)];
        Assert.Equal(
            [NewsWire.NewsEventKind.HunterBrokeOff, NewsWire.NewsEventKind.HullLostAtABerth],
            receipts.Order().ToArray());
    }

    // ══ 3 · THE COUNTER AND THE DESK THAT COMES BACK ═════════════════════════════════════════════════════

    /// <summary>THE UNEASE HAS A THRESHOLD AND IT IS TWO. The first claim is a thing that happened to a
    /// captain; the second is a pattern, and only a pattern is worth a memory.</summary>
    [Fact]
    public void THE_DESK_ComesBackOnTheSecondClaimAndNotTheFirst()
    {
        Assert.Equal(2, NebulaClaims.FlashbackFromClaim);
        Assert.False(NebulaClaims.TheDeskComesBack(0));
        Assert.False(NebulaClaims.TheDeskComesBack(1));
        Assert.True(NebulaClaims.TheDeskComesBack(2));
        Assert.True(NebulaClaims.TheDeskComesBack(9));
    }

    /// <summary>…and the beat carries the painting and the words the issue authored, adopted through #664's
    /// door rather than retyped — so the card the captain reads and the constant a reviewer reads are one
    /// string.</summary>
    [Fact]
    public void THE_BEAT_WearsTheDeskAndTheDeskIsTheAuthoredPlate()
    {
        Assert.Equal(NebulaClaims.DeskArt, StoryBeats.ArtFile(StoryBeats.Beat.TheClaim));
        Assert.Equal(NebulaClaims.DeskTitle, StoryBeats.Title(StoryBeats.Beat.TheClaim));
        Assert.Equal(NebulaClaims.DeskCaption, StoryBeats.Caption(StoryBeats.Beat.TheClaim));
        Assert.Equal(StoryBeats.Presentation.Card, StoryBeats.PresentationOf(StoryBeats.Beat.TheClaim));
        Assert.Contains(NebulaClaims.DeskArt, StoryBeats.Canvases(StoryBeats.Beat.TheClaim));
    }

    // ══ 4 · WHERE A KIOSK STANDS ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>EVERY GREAT PORT, A DEALT SHARE OF THE WORKING BERTHS, NEVER AN OUTPOST</b> — over the shipping
    /// scenario, and the working berths must come out SPLIT.
    ///
    /// <para>That last clause is the whole guard. "A dealt share" is a claim about the world, and a deal that
    /// answered yes for all three of Sol's working berths (or no for all three) would leave this file green
    /// about a rule the game does not actually have — the fifth named bug class, a world that cannot tell
    /// pass from fail. So the split is asserted, and if the salt is ever changed this is what says so.</para>
    /// </summary>
    [Fact]
    public void THE_KIOSKS_StandAtEveryGreatPortAtSomeWorkingBerthsAndAtNoOutpost()
    {
        ICelestialEphemeris sky = Sol();
        string[] havens = [.. sky.Bodies.Where(b => b.IsHaven).Select(b => b.Id)];
        Assert.NotEmpty(havens);

        List<string> withOne = [];
        List<string> without = [];
        foreach (string id in havens)
        {
            ArrivalTube.Tier tier = ArrivalTube.TierFor(sky, id);
            bool stands = NebulaClaims.AKioskStands(tier, id);

            if (tier == ArrivalTube.Tier.GreatPort)
            {
                Assert.True(stands, $"{id} is a great port and has no claims machine on its concourse.");
            }

            if (tier == ArrivalTube.Tier.Outpost)
            {
                Assert.False(stands, $"{id} is an outpost — there is no concourse there to stand one in.");
            }

            if (tier == ArrivalTube.Tier.WorkingBerth)
            {
                (stands ? withOne : without).Add(id);
            }
        }

        Assert.NotEmpty(withOne);
        Assert.NotEmpty(without);
    }

    /// <summary>The deal is the same berth every time it is asked, on every machine — the repo's own dealer,
    /// not a hash somebody wrote at the call site.</summary>
    [Fact]
    public void THE_DEAL_IsTheSameBerthEveryTimeItIsAsked()
    {
        ICelestialEphemeris sky = Sol();
        foreach (CelestialBody body in sky.Bodies.Where(b => b.IsHaven))
        {
            ArrivalTube.Tier tier = ArrivalTube.TierFor(sky, body.Id);
            bool first = NebulaClaims.AKioskStands(tier, body.Id);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(first, NebulaClaims.AKioskStands(tier, body.Id));
            }
        }
    }

    // ══ 5 · THE PROSE, AND THAT THERE IS NO MORE OF IT ═══════════════════════════════════════════════════

    /// <summary>THE SIX AUTHORED STRINGS, VERBATIM off the issue. Retyped here on purpose: this file is the
    /// second copy, and its whole job is to go red the day somebody edits the first one.</summary>
    [Fact]
    public void THE_CANON_IsWordForWordWhatTheIssueAuthored()
    {
        Assert.Equal("NEBULA MUTUAL · CLAIMS", NebulaClaims.KioskPlate);
        Assert.Equal(
            "Loss of hull, non-fatal. Deaths are processed automatically; everything else is a claim. "
            + "Present the policy.",
            NebulaClaims.OnApproach);
        Assert.Equal("Claim lodged. A representative will find you. They always do.", NebulaClaims.LodgedLine);
        Assert.Equal(
            "Your hull is a line item now. Sign here, and here, and try not to read the third page.",
            NebulaClaims.RepAtThePayout);
        Assert.Equal("WRIT · AWAITING THE MASTER", NebulaClaims.PendingWritPlate);
        Assert.Equal("THE CLAIM", NebulaClaims.DeskTitle);
        Assert.Equal("art/claim-desk.jpg", NebulaClaims.DeskArt);
        Assert.Equal(
            "You remember the desk. You do not remember agreeing to the third page.",
            NebulaClaims.DeskCaption);
    }

    /// <summary>
    /// <b>AND THERE IS NO SEVENTH.</b> Every public string this rule can hand a surface is one of the six,
    /// swept by reflection rather than by reading the file — a constant added tomorrow fails here until
    /// somebody either authors it in the issue or admits it is not canon.
    ///
    /// <para>The rep never names the third page either, which is asserted as a property of the sentence
    /// rather than trusted to a reader: it says the words <i>third page</i> and says nothing after them.</para>
    /// </summary>
    [Fact]
    public void THE_RULE_HandsOutNoStringNobodyAuthored()
    {
        string[] canon =
        [
            NebulaClaims.KioskPlate, NebulaClaims.OnApproach, NebulaClaims.LodgedLine,
            NebulaClaims.RepAtThePayout, NebulaClaims.PendingWritPlate,
            NebulaClaims.DeskTitle, NebulaClaims.DeskArt, NebulaClaims.DeskCaption,
            // #1151 slice 2 · the seventh, and the only string that slice authored. It is asserted word for
            // word in TheClaimIsLodgedWithHimTests; here it is simply admitted to the canon, which is the
            // whole point of this sweep — a constant is either in the issue or it is not shipped.
            NebulaClaims.LodgeWithMe,
        ];

        List<string> loose = [];
        foreach (FieldInfo f in typeof(NebulaClaims).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.GetValue(null) is string s && !canon.Contains(s, StringComparer.Ordinal))
            {
                loose.Add($"{f.Name} = \"{s}\"");
            }
        }

        Assert.True(loose.Count == 0,
            "strings the claim can put on a screen that nobody authored: " + string.Join(", ", loose));

        // The plate is a plate and the two sentences are sentences: a rule that let the plate become prose
        // would let a ledger row start explaining itself.
        Assert.DoesNotContain('.', NebulaClaims.PendingWritPlate);
        Assert.DoesNotContain('.', NebulaClaims.KioskPlate);
        Assert.EndsWith("try not to read the third page.", NebulaClaims.RepAtThePayout, StringComparison.Ordinal);
    }

    // ══ 5 · #1151 SLICE 4 · THE TERMS THE WRIT WAS WRITTEN ON ═══════════════════════════════════════════

    /// <summary>
    /// <b>A WRIT THAT WAITED IS SERVED ON THE TERMS IT HAD ON THE DAY</b>, so the two things a boarding
    /// demand is cut from ride the FILE and not the moment of service — and a file is only worth writing if
    /// it comes back. The heat the contract was worth and the pursuer's own id survive the vault's own
    /// serializer, through the envelope a reload actually reads, checksum and all.
    ///
    /// <para>Asked with values nothing else in the record could supply: a heat of 3 against a callsign, a
    /// haven and a moment that are all of other types, so a round trip that dropped the terms and answered
    /// with a zero would go red rather than agree with a default.</para>
    /// </summary>
    [Fact]
    public void THE_WRITS_TermsSurviveTheReloadThatHasToServeIt()
    {
        var filed = new PendingWritRecord(
            "GRIMHOLD", "selene-gate", 1234.5, HeatWhenFiled: 3, HunterId: "hunter-7");
        var saved = new Vault { Progress = new ProgressSection { WritPending = filed } };

        Vault reread = VaultSerializer.Load(VaultSerializer.Save(saved));

        Assert.False(reread.Tampered);
        PendingWritRecord back = reread.Progress?.WritPending
            ?? throw new InvalidOperationException("the writ did not survive its own serializer.");
        Assert.Equal(filed, back);
        Assert.Equal(3, back.HeatWhenFiled);
        Assert.Equal("hunter-7", back.HunterId);
    }

    /// <summary>
    /// <b>AND A WRIT FILED BEFORE THE TERMS EXISTED IS STILL A WRIT.</b> Slice 1 shipped this record with
    /// three fields, and a voyage saved between then and now can have one on the file right this minute. It
    /// loads as what it is — the callsign, the berth and the moment — with no terms, and the demand it opens
    /// falls back on the floor every demand in the game already has rather than on a file that says nothing.
    ///
    /// <para>Read through the wire's own naming, because a naming policy is exactly the kind of thing that
    /// makes a "forward compatible" record quietly deserialize into nothing at all.</para>
    /// </summary>
    [Fact]
    public void A_WRIT_FiledBeforeTheTermsExistedStillLoadsAsAWrit()
    {
        const string asSliceOneWroteIt =
            """{"callsign":"GRIMHOLD","havenId":"selene-gate","filedAtSimTime":1234.5}""";

        PendingWritRecord old = System.Text.Json.JsonSerializer.Deserialize<PendingWritRecord>(
            asSliceOneWroteIt,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("a slice-1 writ no longer reads as a writ at all.");

        Assert.Equal("GRIMHOLD", old.Callsign);
        Assert.Equal("selene-gate", old.HavenId);
        Assert.Equal(1234.5, old.FiledAtSimTime);
        // …and no terms on it. What a writ with no terms is SERVED at is next door in the Client's own
        // A_WRIT_FiledBeforeTheTermsExistedIsServedAtTheDemandsOwnFloor, where a live demand is opened on
        // one — restating the floor here would only be this file agreeing with its own arithmetic.
        Assert.Equal(0, old.HeatWhenFiled);
        Assert.Null(old.HunterId);
    }

    /// <summary>
    /// THE RESERVED WORD IS ABSENT (docs/worldbuilding-notes.md §8: <i>"there is one monolith … the word is
    /// reserved"</i>). Swept over the whole of this lane's authored prose rather than eyeballed, because the
    /// point of a reserved word is that it is never borrowed by accident.
    /// </summary>
    [Fact]
    public void THE_RESERVED_WORD_IsNowhereInThisLanesProse()
    {
        string[] prose =
        [
            NebulaClaims.KioskPlate, NebulaClaims.OnApproach, NebulaClaims.LodgedLine,
            NebulaClaims.RepAtThePayout, NebulaClaims.PendingWritPlate,
            NebulaClaims.DeskTitle, NebulaClaims.DeskCaption, NebulaClaims.LodgeWithMe,
            StoryBeats.Title(StoryBeats.Beat.TheClaim), StoryBeats.Caption(StoryBeats.Beat.TheClaim),
        ];

        Assert.All(prose, line =>
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase));
    }
}
