using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 L5a · THE OLD CREW, AS LAWS.
///
/// <para>Five things in this lane are rules rather than habits, and every one of them is the sort that rots
/// quietly: the signer being in every seeding, the history table being rolled BEFORE anybody is posted, the
/// classic landing often enough to be the classic, the one page a rebirth cannot take, and the arc's law
/// that no surface ever names the thing.</para>
/// </summary>
public sealed class TheOldCrewTests
{
    private const double Day = 86400.0;

    /// <summary>A world of berths with a real spread of tiers — built here rather than taken off a scenario,
    /// so a test of the postings is a test of the postings and not of whatever Saturn's tonnage is today.</summary>
    private static readonly OldCrew.Berth[] Berths =
    [
        new("ringside", ArrivalTube.Tier.GreatPort),
        new("red-eye", ArrivalTube.Tier.GreatPort),
        new("selene-gate", ArrivalTube.Tier.WorkingBerth),
        new("rusty-roadstead", ArrivalTube.Tier.WorkingBerth),
        new("cinder-roost", ArrivalTube.Tier.Outpost),
    ];

    private static IEnumerable<string> ManyThreads(int count) =>
        Enumerable.Range(0, count).Select(i => $"thread-{i}");

    // ── THE CAST ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Owner ruling §8: FOUR shipmates per thread, and one of them is always the one who signed.
    /// A seeding without him is a seeding with no arc in it.</summary>
    [Fact]
    public void EverySeedingCastsFourAndAlwaysTheSigner()
    {
        foreach (string thread in ManyThreads(400))
        {
            IReadOnlyList<OldCrew.Seeded> seeded = OldCrew.Seed(thread, Berths);

            Assert.Equal(OldCrew.SeededPerThread, seeded.Count);
            Assert.Contains(seeded, s => s.Id == OldCrew.SignerId);
            Assert.Equal(seeded.Count, seeded.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count());
        }
    }

    /// <summary>The dead man is never a contact. He is a face on the photograph and a name in the rep's
    /// file, and a seeding that cast him would be the arc handing the player a witness it does not have.</summary>
    [Fact]
    public void TheDeadManIsNeverSeeded()
    {
        foreach (string thread in ManyThreads(400))
        {
            Assert.DoesNotContain(OldCrew.Seed(thread, Berths), s => s.Id == OldCrew.DeadId);
        }
    }

    /// <summary>Determinism is law in Core: the same universe seeds the same four people, bound the same
    /// way, posted at the same berths, every time it is asked.</summary>
    [Fact]
    public void TheSameThreadAlwaysSeedsTheSameCrew()
    {
        foreach (string thread in ManyThreads(50))
        {
            Assert.Equal(OldCrew.Seed(thread, Berths), OldCrew.Seed(thread, Berths));
        }
    }

    /// <summary>…and two universes are two universes: over four hundred threads the seeding is not one
    /// answer wearing four hundred names. A constant cast would pass every other test in this file.</summary>
    [Fact]
    public void DifferentThreadsSeedDifferentCrews()
    {
        int distinct = ManyThreads(400)
            .Select(t => string.Join(",", OldCrew.Seed(t, Berths).Select(s => s.Id)))
            .Distinct(StringComparer.Ordinal)
            .Count();

        Assert.True(distinct > 3, $"the cast barely varies — {distinct} distinct castings over 400 threads");
    }

    // ── THE CLASSIC ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE OWNER'S CONSTRAINT, MEASURED (addendum 2, ruling §10): the fling and the best friend are bound to
    /// each other in <b>at least one seeding in three</b>. Proved statistically over a large sweep rather
    /// than asserted on one lucky seed, and bounded above as well — a table that produced the classic every
    /// time would have stopped being a table.
    /// </summary>
    [Fact]
    public void TheFlingAndTheBestFriendLandTogetherInAboutATirdOfSeedings()
    {
        const int seeds = 600;
        int classic = ManyThreads(seeds).Count(t => OldCrew.TheClassicHappened(OldCrew.Bonds(t)));

        double rate = classic / (double)seeds;
        Assert.InRange(rate, 0.30, 0.45);
    }

    /// <summary>When the classic lands, BOTH ends of it say so — the book must read the same whichever door
    /// the captain knocks on first.</summary>
    [Fact]
    public void TheClassicIsWrittenOnBothEnds()
    {
        int seen = 0;
        foreach (string thread in ManyThreads(400))
        {
            IReadOnlyList<OldCrew.Seeded> seeded = OldCrew.Bonds(thread);
            if (!OldCrew.TheClassicHappened(seeded))
            {
                continue;
            }

            seen++;
            OldCrew.Seeded friend = OldCrew.Find(seeded, OldCrew.BestFriendId)!.Value;
            OldCrew.Seeded fling = OldCrew.Find(seeded, OldCrew.FlingId)!.Value;
            Assert.True(friend.IsTheClassic);
            Assert.True(fling.IsTheClassic);
            Assert.Equal(OldCrew.FlingId, friend.BondToId);
            Assert.Equal(OldCrew.BestFriendId, fling.BondToId);
        }

        Assert.True(seen > 0, "the sweep never produced the classic at all");
    }

    /// <summary>…and the book says it in plain words before the captain knocks: <i>now with Ilse</i>, which
    /// is what a person would actually say, rather than the table's own vocabulary.</summary>
    [Fact]
    public void TheBookSaysNowWithHerBeforeYouKnock()
    {
        string thread = ManyThreads(400).First(t => OldCrew.TheClassicHappened(OldCrew.Bonds(t)));
        OldCrew.Seeded friend = OldCrew.Find(OldCrew.Bonds(thread), OldCrew.BestFriendId)!.Value;

        Assert.Contains("now with Ilse", friend.History(), StringComparison.Ordinal);
    }

    /// <summary>Every seeded shipmate carries BOTH bonds — one to the captain (their role) and one to
    /// another shipmate who is actually in this seeding. A bond pointing at somebody the thread did not cast
    /// is a history the player can never walk into.</summary>
    [Fact]
    public void EveryShipmateIsBoundToTheCaptainAndToSomebodyWhoIsHere()
    {
        foreach (string thread in ManyThreads(400))
        {
            IReadOnlyList<OldCrew.Seeded> seeded = OldCrew.Bonds(thread);
            foreach (OldCrew.Seeded s in seeded)
            {
                Assert.Equal(OldCrew.AsBond(OldCrew.ById(s.Id)!.Value.Role), s.ToCaptain);
                Assert.NotEqual(s.Id, s.BondToId);
                Assert.Contains(seeded, other => other.Id == s.BondToId);
                Assert.False(string.IsNullOrWhiteSpace(s.History()));
            }
        }
    }

    // ── BONDS BEFORE POSTINGS ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ORDER IS THE LAW (ruling §10, the Fail Forward table adopted whole): the history is rolled FIRST
    /// and the postings second. Proved by handing the same thread two completely different worlds of
    /// berths: the bonds do not move a millimetre, because <see cref="OldCrew.Bonds"/> is never told where
    /// anybody works.
    ///
    /// <para>This is the guard that fails the day somebody makes a posting influence a role — which is the
    /// natural refactor to reach for the first time a berth runs out of room.</para>
    /// </summary>
    [Fact]
    public void TheBondsAreRolledBeforeAndWithoutTheBerths()
    {
        OldCrew.Berth[] elsewhere =
        [
            new("far-station", ArrivalTube.Tier.GreatPort),
            new("other-port", ArrivalTube.Tier.WorkingBerth),
        ];

        foreach (string thread in ManyThreads(200))
        {
            IReadOnlyList<OldCrew.Seeded> here = OldCrew.Seed(thread, Berths);
            IReadOnlyList<OldCrew.Seeded> there = OldCrew.Seed(thread, elsewhere);

            Assert.Equal(
                here.Select(s => (s.Id, s.ToCaptain, s.BondToId, s.Bond)),
                there.Select(s => (s.Id, s.ToCaptain, s.BondToId, s.Bond)));
        }
    }

    /// <summary>…and the posting really does depend on the ROLE: the claims desk is at a great port and the
    /// working berth's bar is at a working berth, in every universe. A posting that ignored the kind would
    /// make the previous guard vacuous — bonds that cannot affect postings and postings that are not about
    /// anything.</summary>
    [Fact]
    public void ThePostingFollowsTheRolesPlaceKind()
    {
        foreach (string thread in ManyThreads(400))
        {
            foreach (OldCrew.Seeded s in OldCrew.Seed(thread, Berths))
            {
                OldCrew.Shipmate who = OldCrew.ById(s.Id)!.Value;
                OldCrew.Berth berth = Berths.Single(b => b.Id == s.StationId);

                if (who.Posting == OldCrew.PlaceKind.NebulaClaimsDesk)
                {
                    Assert.Equal(ArrivalTube.Tier.GreatPort, berth.Tier);
                }

                if (who.Posting == OldCrew.PlaceKind.WorkingBerthBar)
                {
                    Assert.Equal(ArrivalTube.Tier.WorkingBerth, berth.Tier);
                }
            }
        }
    }

    /// <summary>When the classic lands, she is posted where he is — she moved to be with him. It is what
    /// makes the door the captain stands outside of a door he can actually reach.</summary>
    [Fact]
    public void WhenSheIsWithHimSheIsPostedWhereHeIs()
    {
        int seen = 0;
        foreach (string thread in ManyThreads(400))
        {
            IReadOnlyList<OldCrew.Seeded> seeded = OldCrew.Seed(thread, Berths);
            if (!OldCrew.TheClassicHappened(seeded))
            {
                continue;
            }

            seen++;
            string at = OldCrew.Find(seeded, OldCrew.FlingId)!.Value.StationId;
            Assert.Equal(at, OldCrew.Find(seeded, OldCrew.BestFriendId)!.Value.StationId);
            Assert.True(OldCrew.KnockingCostsNerve(seeded, at));
        }

        Assert.True(seen > 0, "the sweep never produced the classic at all");
    }

    /// <summary>…and it costs nothing to knock anywhere else, or on any other door. The pip is for one
    /// moment, not for a mood.</summary>
    [Fact]
    public void KnockingCostsNothingWhenSheIsNotWithHim()
    {
        string thread = ManyThreads(400).First(t => !OldCrew.TheClassicHappened(OldCrew.Bonds(t)));
        IReadOnlyList<OldCrew.Seeded> seeded = OldCrew.Seed(thread, Berths);

        foreach (OldCrew.Berth b in Berths)
        {
            Assert.False(OldCrew.KnockingCostsNerve(seeded, b.Id));
        }

        Assert.False(OldCrew.KnockingCostsNerve(seeded, null));
    }

    // ── THE ROW THE VAULT CARRIES ────────────────────────────────────────────────────────────────────

    /// <summary>A seeding round-trips through the file without losing a bond or a berth.</summary>
    [Fact]
    public void ASeedingRoundTripsThroughTheVaultRow()
    {
        foreach (string thread in ManyThreads(200))
        {
            foreach (OldCrew.Seeded s in OldCrew.Seed(thread, Berths))
            {
                Assert.True(OldCrew.TryParse(OldCrew.Stored(s), out OldCrew.Seeded back));
                Assert.Equal(s, back);
            }
        }
    }

    /// <summary>A row this build cannot read is dropped rather than thrown over — the tolerance every other
    /// book in the vault gets.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("nobody|0|teo|1|ringside")]
    [InlineData("teo|99|ilse|1|ringside")]
    [InlineData("teo|0|ilse")]
    public void AnUnreadableRowIsDroppedRatherThanThrownOver(string stored)
    {
        Assert.False(OldCrew.TryParse(stored, out _));
    }

    // ── THE PAGE THE LINE CANNOT TAKE ────────────────────────────────────────────────────────────────

    /// <summary>
    /// OWNER RULING §13, AS A LAW. A full uninsured rebirth — the line at negative infinity, the whole book
    /// grey — greys every page in the ledger EXCEPT the one the service filed. That page is the summer
    /// party, preserved perfectly by the people who wrote it up against him, and it is the joke the ruling
    /// kept.
    /// </summary>
    [Fact]
    public void AnUninsuredRebirthGreysEveryPageExceptTheOneTheServiceFiled()
    {
        LedgerPage[] book =
        [
            new(OldCrewScene.SummerPartyId, -12 * Day, OldCrewScene.SummerPartyTitle,
                [OldCrewScene.SummerPartyPage], OldCrewScene.SummerPartyProvenance, Filed: true),
            new("autopilot:1", 1 * Day, "🛰 Autopilot", ["stood down"], "logged 1d ago"),
            new("plunder:2", 2 * Day, "🏴 Plunder", ["took 4 units"], "taken 2d ago"),
            new("field:3", 3 * Day, "🥾 Cinder Roost", ["a bootprint"], "found on the ground · day 3"),
        ];

        double line = FilingLine.At(PirateInsurance.Uninsured);
        IReadOnlyList<FilingLine.Page> marked = FilingLine.MarkTheBook([], book, line);

        Assert.True(double.IsNegativeInfinity(line));
        Assert.Equal(
            FilingLine.PageState.Remembered,
            FilingLine.Standing(marked, OldCrewScene.SummerPartyId).State);
        Assert.All(
            marked.Where(p => p.EntryId != OldCrewScene.SummerPartyId),
            p => Assert.Equal(FilingLine.PageState.Unremembered, p.State));
    }

    /// <summary>…and the exemption is about the SERVICE having filed it, not about its date: the same page
    /// dated after the line, unfiled, greys like anything else. Without this the previous guard would pass
    /// on a rule that merely exempted old pages.</summary>
    [Fact]
    public void ThePageOnlySurvivesBecauseItWasFiled()
    {
        var filed = new LedgerPage(OldCrewScene.SummerPartyId, 9 * Day, OldCrewScene.SummerPartyTitle,
            [OldCrewScene.SummerPartyPage], OldCrewScene.SummerPartyProvenance, Filed: true);
        LedgerPage unfiled = filed with { Filed = false };

        Assert.False(FilingLine.Greys(double.NegativeInfinity, filed));
        Assert.True(FilingLine.Greys(double.NegativeInfinity, unfiled));
        Assert.True(FilingLine.Greys(2 * Day, unfiled));
        Assert.False(FilingLine.Greys(2 * Day, filed));
    }

    /// <summary>An insured captain keeps the filed page too — the rule is not a consolation prize for the
    /// broke.</summary>
    [Fact]
    public void TheFiledPageSurvivesAnInsuredRebirthAsWell()
    {
        var policy = new PirateInsurance(InsuranceTier.Basic, 2 * Day);
        LedgerPage[] book =
        [
            new(OldCrewScene.SummerPartyId, 9 * Day, OldCrewScene.SummerPartyTitle,
                [OldCrewScene.SummerPartyPage], OldCrewScene.SummerPartyProvenance, Filed: true),
            new("autopilot:1", 9 * Day, "🛰 Autopilot", ["stood down"], "logged just now"),
        ];

        IReadOnlyList<FilingLine.Page> marked = FilingLine.MarkTheBook([], book, FilingLine.At(policy));

        Assert.Equal(
            FilingLine.PageState.Remembered,
            FilingLine.Standing(marked, OldCrewScene.SummerPartyId).State);
        Assert.Equal(FilingLine.PageState.Unremembered, FilingLine.Standing(marked, "autopilot:1").State);
    }

    // ── THE CROSSING ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>The captain's answer goes into the crossings ledger with the answer he gave and the person
    /// he gave it to — §3's hinge, filled by construction.</summary>
    [Theory]
    [InlineData(OldCrewScene.Answer.TheTruth)]
    [InlineData(OldCrewScene.Answer.ThePolicyLine)]
    [InlineData(OldCrewScene.Answer.ALie)]
    public void TheCrossingIsWrittenWithTheAnswerAndTheWitness(OldCrewScene.Answer answer)
    {
        IReadOnlyList<CaptainCrossings.Crossing> ledger =
            CaptainCrossings.Add([], CaptainCrossings.OwnFace(answer, "Ilse Marrow", 4 * Day));

        CaptainCrossings.Crossing only = Assert.Single(ledger);
        Assert.Equal(CaptainCrossings.Kind.OwnFaceExplained, only.Kind);
        Assert.Equal(answer, CaptainCrossings.AnswerOf(only));
        Assert.Equal("Ilse Marrow", only.Witness);
        Assert.Equal(4 * Day, only.SimTime);
        Assert.Contains("Ilse Marrow", CaptainCrossings.Row(only), StringComparison.Ordinal);
        Assert.Contains(OldCrewScene.Button(answer), CaptainCrossings.Row(only), StringComparison.Ordinal);
    }

    /// <summary>Two answers to two people are two rows. Nothing is summed and nothing is deduped — it is a
    /// map, not a meter, and the absence of a score is the feature.</summary>
    [Fact]
    public void TheLedgerAppendsAndNeverScores()
    {
        IReadOnlyList<CaptainCrossings.Crossing> ledger = CaptainCrossings.Add(
            CaptainCrossings.Add([], CaptainCrossings.OwnFace(OldCrewScene.Answer.ALie, "Teodor \"Teo\" Brask", Day)),
            CaptainCrossings.OwnFace(OldCrewScene.Answer.TheTruth, "Corwin Sallis", 2 * Day));

        Assert.Equal(2, ledger.Count);

        // …and there is NOTHING on the type that returns a number about the captain. The absence is the
        // feature (§1: a meter invites grinding), and it is asserted rather than trusted, because "add a
        // score" is the first thing anybody reaches for the day a UI wants a summary line.
        Assert.Empty(typeof(CaptainCrossings)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => m.ReturnType == typeof(int) || m.ReturnType == typeof(double))
            .Select(m => m.Name));
    }

    /// <summary>A crossing round-trips through the file.</summary>
    [Fact]
    public void ACrossingRoundTripsThroughTheVaultRow()
    {
        var crossing = CaptainCrossings.OwnFace(OldCrewScene.Answer.ThePolicyLine, "Pell | Andrade", 7 * Day);

        Assert.True(CaptainCrossings.Crossing.TryParse(crossing.Stored, out CaptainCrossings.Crossing back));
        Assert.Equal(crossing, back);
    }

    // ── THE DRINK ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The three new modifiers are NAMED and they are APPLIED — the whole point of the dice homage
    /// is that the player watches the numbers add up, and a modifier that moved the total without printing
    /// itself would be the house lying to the table.</summary>
    [Fact]
    public void TheThreeNewModifiersAreNamedAndApplied()
    {
        ulong seed = DiceRule.Seed("drink:crew", 1);
        DrinkParley plain = ContactDrink.Roll(seed, currentGoodwill: 0, holdingSecret: false);
        DrinkParley crewed = ContactDrink.Roll(
            seed, currentGoodwill: 0, holdingSecret: false, offeringFavorite: false,
            room: new ContactDrink.TheRoom(SharedHistory: true, SignerPresent: true, FlingPresent: true));

        Assert.Equal(plain.Pips, crewed.Pips);
        Assert.Equal(
            plain.ModifierTotal
                + ContactDrink.SharedHistoryBonus
                + ContactDrink.SignerInTheRoomPenalty
                + ContactDrink.FlingInTheRoomPenalty,
            crewed.ModifierTotal);

        foreach (string label in new[]
        {
            ContactDrink.SharedHistoryLabel, ContactDrink.SignerInTheRoomLabel, ContactDrink.FlingInTheRoomLabel,
        })
        {
            Assert.Contains(crewed.Modifiers, m => m.Label == label);
            Assert.Contains(label, crewed.Describe(), StringComparison.Ordinal);
        }
    }

    /// <summary>Each of the three moves the total on its own, and in the direction the bible gives it.</summary>
    [Fact]
    public void SharedHistoryHelpsAndTheTwoOfThemHurt()
    {
        ulong seed = DiceRule.Seed("drink:crew", 2);
        int plain = ContactDrink.Roll(seed, 0, false).Total;

        Assert.Equal(plain + 1, ContactDrink.Roll(seed, 0, false, false, new(true, false, false)).Total);
        Assert.Equal(plain - 1, ContactDrink.Roll(seed, 0, false, false, new(false, true, false)).Total);
        Assert.Equal(plain - 1, ContactDrink.Roll(seed, 0, false, false, new(false, false, true)).Total);
    }

    /// <summary>An ordinary room rolls exactly the dice every caller written before this lane rolled. A
    /// default that quietly added a modifier would have re-tuned every drink in the game.</summary>
    [Fact]
    public void AnOrdinaryRoomChangesNothingAtAll()
    {
        ulong seed = DiceRule.Seed("drink:crew", 3);

        // Compared through Describe rather than by record equality: the modifier stack is a List, so two
        // identical rolls are two different references and == would pass on anything.
        Assert.Equal(
            ContactDrink.Roll(seed, 4, true, true).Describe(),
            ContactDrink.Roll(seed, 4, true, true, room: default).Describe());
        Assert.Equal(
            ContactDrink.OfferDrink(seed, 4, true, true).Describe(),
            ContactDrink.OfferDrink(seed, 4, true, true, ContactDrink.TheRoom.Ordinary).Describe());
        Assert.False(ContactDrink.TheRoom.Ordinary.Matters);
    }

    /// <summary>The offer takes the same room as the drink — whether a shipmate takes the glass at all is
    /// coloured by who else is standing about, and the two rolls can never be handed different rooms.</summary>
    [Fact]
    public void TheOfferReadsTheSameRoom()
    {
        ulong seed = DiceRule.Seed("drink:crew", 4);
        DrinkOfferResult crewed = ContactDrink.OfferDrink(seed, 0, false, false, new(true, true, false));

        Assert.Contains(crewed.Modifiers, m => m.Label == ContactDrink.SharedHistoryLabel);
        Assert.Contains(crewed.Modifiers, m => m.Label == ContactDrink.SignerInTheRoomLabel);
    }

    // ── THE SIGNER REPORTS ───────────────────────────────────────────────────────────────────────────

    /// <summary>He does not denounce you: he files. One unit, owed to the authority of the station he works
    /// at, banked through #715's own API and against nobody else's book.</summary>
    [Fact]
    public void TheSignerReportsOneUnitToHisOwnPortAndToNobodyElse()
    {
        var book = new ContactLedger();
        UndergroundComplex.HeatCharge charge = OldCrew.SignerReport("ringside");

        Assert.Equal(SiteOperator.Of("ringside").Id, charge.OperatorId);
        Assert.Equal(OldCrew.SignerReportUnits, charge.Points);

        IllegalHeat.Bank(book, charge, 3 * Day);

        Assert.Equal(OldCrew.SignerReportUnits, IllegalHeat.HeatAt(book, charge.OperatorId));
        foreach (SiteOperator.Operator other in SiteOperator.All.Where(o => o.Id != charge.OperatorId))
        {
            Assert.Equal(0, IllegalHeat.HeatAt(book, other.Id));
        }
    }

    /// <summary>Banking the same visit's report twice is what the once-per-visit latch exists to stop, and
    /// the meter would happily take it — which is why the latch is a fact the client keeps and this test
    /// pins the cost of getting it wrong.</summary>
    [Fact]
    public void TwoReportsWouldCostTwice()
    {
        var book = new ContactLedger();
        UndergroundComplex.HeatCharge charge = OldCrew.SignerReport("ringside");

        IllegalHeat.Bank(book, charge, 3 * Day);
        IllegalHeat.Bank(book, charge, 3 * Day);

        Assert.Equal(2 * OldCrew.SignerReportUnits, IllegalHeat.HeatAt(book, charge.OperatorId));
    }

    // ── THE BOOK ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>An old shipmate's row is seeded with the small warmth their role starts with, the flag that
    /// says they knew the face, and — crucially — the warmth is applied ONCE. A reload that warmed them
    /// again would make the ledger a function of how often the game was saved.</summary>
    [Fact]
    public void SeedingAShipmateIsWarmOnceAndMarkedForever()
    {
        var book = new ContactLedger();
        OldCrew.Shipmate ilse = OldCrew.ById(OldCrew.FlingId)!.Value;
        string id = OldCrew.LedgerId(ilse.Id);

        book.SeedOldShipmate(id, ilse.Name, ilse.Warmth);
        book.SeedOldShipmate(id, ilse.Name, ilse.Warmth);
        book.SeedOldShipmate(id, ilse.Name, ilse.Warmth);

        Assert.Equal(ilse.Warmth, book.For(id).Goodwill);
        Assert.True(book.For(id).KnewTheOldFace);
        Assert.True(book.For(id).HasHistory);
        Assert.True(OldCrew.IsAnOldShipmate(id));
        Assert.False(OldCrew.IsAnOldShipmate(IllegalHeat.LedgerId("meridian")));
    }

    /// <summary>THE BOOK MARKS THE LIE, and only the lie.</summary>
    [Fact]
    public void OnlyTheLieIsMarked()
    {
        var book = new ContactLedger();
        string id = OldCrew.LedgerId(OldCrew.FlingId);
        book.SeedOldShipmate(id, "Ilse Marrow", 3);

        Assert.False(book.For(id).WasLiedTo);

        book.RecordLie(id, "Ilse Marrow");
        book.RecordLie(id, "Ilse Marrow");

        Assert.True(book.For(id).WasLiedTo);
        Assert.Equal(3, book.For(id).Goodwill);   // the lie is not goodwill with a minus sign
        Assert.Equal(0, book.For(id).HeatOwed);   // …nor heat
    }

    /// <summary>The old crew are the only contacts who knew the face. The rep and the strangers are not,
    /// and a flag that defaulted true would fire the whole scene at a barkeep.</summary>
    [Fact]
    public void NobodyElseKnewTheOldFace()
    {
        var book = new ContactLedger();
        book.AddGoodwill("MADAM COIL", "Madam Coil", 4);
        book.RecordCompletion("THE FIXER", "The Fixer", 100, Day);

        Assert.False(book.For("MADAM COIL").KnewTheOldFace);
        Assert.False(book.For("THE FIXER").KnewTheOldFace);
        Assert.False(ContactHistory.New("x", "x").KnewTheOldFace);
    }

    /// <summary>A held-memory sheet round-trips with its mark, its tag, its threads and its filed stamp —
    /// including text with the row format's own separator in it.</summary>
    [Fact]
    public void AHeldMemoryRoundTrips()
    {
        var sheet = new HeldMemory.Sheet(
            HeldMemory.PhotographId, HeldMemory.Mark.His, HeldMemory.Theory.Love,
            "a | pipe and a ␟ unit separator", ["Ilse Marrow", "Corwin Sallis"], 5 * Day, Filed: true);

        Assert.True(HeldMemory.Sheet.TryParse(sheet.Stored, out HeldMemory.Sheet back));
        Assert.Equal(sheet.Mark, back.Mark);
        Assert.Equal(sheet.Tag, back.Tag);
        Assert.Equal(sheet.Text, back.Text);
        Assert.Equal(sheet.Threads, back.Threads);
        Assert.Equal(sheet.SimTime, back.SimTime);
        Assert.True(back.Filed);
        Assert.Contains("his", back.Byline, StringComparison.Ordinal);
        Assert.Contains("love", back.Byline, StringComparison.Ordinal);
    }

    /// <summary>Putting the same photograph in twice is one photograph.</summary>
    [Fact]
    public void ASheetPutTwiceIsOneSheet()
    {
        var sheet = new HeldMemory.Sheet(HeldMemory.PhotographId, HeldMemory.Mark.His,
            HeldMemory.Theory.Love, OldCrewScene.Photograph, ["a"], Day);

        IReadOnlyList<HeldMemory.Sheet> book = HeldMemory.Put(HeldMemory.Put([], sheet), sheet);

        Assert.Single(book);
        Assert.NotNull(HeldMemory.Find(book, HeldMemory.PhotographId));
    }

    // ── THE PHOTOGRAPH ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Four faces, always — the picture is of four people on a boat deck and it does not shrink
    /// because a seeding went one way.</summary>
    [Fact]
    public void ThePhotographAlwaysHasFourFaces()
    {
        foreach (string thread in ManyThreads(400))
        {
            IReadOnlyList<OldCrew.Seeded> seeded = OldCrew.Seed(thread, Berths);
            IReadOnlyList<string> faces = OldCrewScene.PhotographFaces(thread, seeded);

            Assert.Equal(OldCrew.SeededPerThread, faces.Count);
            Assert.All(faces, f => Assert.False(string.IsNullOrWhiteSpace(f)));
            Assert.Contains(OldCrew.ById(OldCrew.SignerId)!.Value.Name, faces);
        }
    }

    /// <summary>…and in about one seeding in two the dead man is one of them. Bounded on both sides: never
    /// would lose the clue, always would stop it being one.</summary>
    [Fact]
    public void TheDeadManIsAFaceInAboutHalfOfSeedings()
    {
        const int seeds = 600;
        string hollis = OldCrew.ById(OldCrew.DeadId)!.Value.Name;
        int with = ManyThreads(seeds)
            .Count(t => OldCrewScene.PhotographFaces(t, OldCrew.Seed(t, Berths)).Contains(hollis));

        Assert.InRange(with / (double)seeds, 0.40, 0.60);
    }

    /// <summary>Somebody in every seeding is holding it — a photograph nobody can hand over is a beat that
    /// never fires.</summary>
    [Fact]
    public void SomebodyAlwaysHoldsThePhotograph()
    {
        foreach (string thread in ManyThreads(400))
        {
            IReadOnlyList<OldCrew.Seeded> seeded = OldCrew.Seed(thread, Berths);
            string holder = OldCrewScene.PhotographHeldBy(seeded);

            Assert.NotEqual("", holder);
            Assert.NotNull(OldCrew.Find(seeded, holder));
        }
    }

    // ── THE WORDS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every living name has an opening line of their own, and no two of them are the same
    /// sentence — the whole value of the old crew is that they sound like people somebody knew.</summary>
    [Fact]
    public void EveryLivingShipmateOpensInTheirOwnVoice()
    {
        string[] openings = [.. OldCrew.Pool.Where(p => p.Living).Select(p => OldCrewScene.Opening(p.Id))];

        Assert.Equal(6, openings.Length);
        Assert.All(openings, o => Assert.False(string.IsNullOrWhiteSpace(o)));
        Assert.Equal(openings.Length, openings.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>The three buttons are three different buttons and the captain says three different things.</summary>
    [Fact]
    public void TheThreeAnswersAreThree()
    {
        OldCrewScene.Answer[] all = Enum.GetValues<OldCrewScene.Answer>();

        Assert.Equal(3, all.Length);
        Assert.Equal(3, all.Select(OldCrewScene.Button).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3, all.Select(OldCrewScene.Said).Distinct(StringComparer.Ordinal).Count());
        Assert.All(all, a => Assert.False(string.IsNullOrWhiteSpace(OldCrewScene.Reply(OldCrew.SignerId, a))));
    }

    /// <summary>
    /// #973 L3 · <b>NOT ONE SLOT IS STANDING IN ANY MORE.</b> L5a shipped with eleven of the eighteen replies
    /// wearing the best friend's line, and this guard counted them so the PR body could not lie about it. The
    /// words exist now, so the guard is INVERTED rather than deleted: zero standing-ins, and a name added to
    /// the pool without a voice of its own fails here the day it is added.
    /// </summary>
    [Fact]
    public void NoReplySlotIsStandingInAnyMore()
    {
        var standingIn = new List<string>();
        foreach (OldCrew.Shipmate who in OldCrew.Pool.Where(p => p.Living))
        {
            foreach (OldCrewScene.Answer answer in Enum.GetValues<OldCrewScene.Answer>())
            {
                if (OldCrewScene.ReplyIsStandingIn(who.Id, answer))
                {
                    standingIn.Add($"{who.Name} · {OldCrewScene.Button(answer)}");
                }
            }
        }

        Assert.Empty(standingIn);
    }

    /// <summary>…and every one of the eighteen is a DIFFERENT sentence — six people by three answers, all
    /// distinct. The whole value of the old crew, stated as an assertion: they sound like people somebody
    /// knew, and two of them saying the identical thing would be the wiring showing through.</summary>
    [Fact]
    public void EveryReplyIsThatPersonsOwnSentence()
    {
        var replies = new List<string>();
        foreach (OldCrew.Shipmate who in OldCrew.Pool.Where(p => p.Living))
        {
            foreach (OldCrewScene.Answer answer in Enum.GetValues<OldCrewScene.Answer>())
            {
                string said = OldCrewScene.Reply(who.Id, answer);
                Assert.False(string.IsNullOrWhiteSpace(said), $"{who.Name} has nothing to say to {answer}");
                replies.Add(said);
            }
        }

        Assert.Equal(18, replies.Count);
        Assert.Equal(replies.Count, replies.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>All six slips are that person's own piece of paper, and no two of them are the same one. The
    /// count lives with the code rather than in a paragraph somebody has to remember to edit.</summary>
    [Fact]
    public void AllSixSlipsAreWrittenAndNoneRepeats()
    {
        string[] slips = [.. OldCrew.Pool.Where(p => p.Living).Select(p => OldCrewScene.Slip(p.Id))];

        Assert.Equal(6, slips.Length);
        Assert.DoesNotContain(OldCrew.Pool.Where(p => p.Living), p => OldCrewScene.SlipIsPlaceholder(p.Id));
        Assert.Equal(slips.Length, slips.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// #973 L3 · <b>THE ONE WHO SIGNED IS ONE MAN.</b> The shipmate-to-shipmate bond names what the OTHER
    /// person is to this one (<c>Seeded.History</c> reads <i>the fling · the best friend: Teo</i>), so
    /// <c>Signed</c> is a sentence about the person it points AT — and there is exactly one man in this world
    /// it can truthfully point at. Swept over a long run of threads, and asked of the bag itself as well, so
    /// the law is proved at the DRAW rather than sampled at the result.
    /// </summary>
    [Fact]
    public void OnlyCorwinEverWearsTheBondOfTheOneWhoSigned()
    {
        foreach (string thread in ManyThreads(300))
        {
            foreach (OldCrew.Seeded s in OldCrew.Seed(thread, Berths))
            {
                if (s.Bond == OldCrew.BondKind.Signed)
                {
                    Assert.Equal(OldCrew.SignerId, s.BondToId);
                }
            }
        }

        Assert.Contains(OldCrew.BondKind.Signed, OldCrew.BondsAvailableAbout(OldCrew.SignerId));
        foreach (OldCrew.Shipmate other in OldCrew.Pool.Where(p => p.Id != OldCrew.SignerId))
        {
            Assert.DoesNotContain(OldCrew.BondKind.Signed, OldCrew.BondsAvailableAbout(other.Id));
        }

        // …and every OTHER kind is still legal about anybody, so the reservation cost the table one cell and
        // not a column: a bag that had quietly lost two kinds would flatten the history between four people.
        foreach (OldCrew.BondKind kind in Enum.GetValues<OldCrew.BondKind>())
        {
            if (kind != OldCrew.BondKind.Signed)
            {
                Assert.Contains(kind, OldCrew.BondsAvailableAbout("maren"));
            }
        }
    }

    /// <summary>The tags are the bible's: the fling and the best friend are LOVE, everybody else is MONEY.</summary>
    [Fact]
    public void TheTagsFollowTheRoles()
    {
        Assert.Equal(HeldMemory.Theory.Love, OldCrewScene.SlipTag(OldCrew.FlingId));
        Assert.Equal(HeldMemory.Theory.Love, OldCrewScene.SlipTag(OldCrew.BestFriendId));
        foreach (string id in new[] { OldCrew.SignerId, "maren", "pell", "dagny" })
        {
            Assert.Equal(HeldMemory.Theory.Money, OldCrewScene.SlipTag(id));
        }
    }

    /// <summary>
    /// THE ARC'S LAW, SWEPT OVER EVERY SURFACE THIS LANE CAN PUT IN FRONT OF A PLAYER. The word for what the
    /// clinic does is never printed, and neither is what the decent ship was carrying — that is the writers'
    /// bible and it stays there. Both halves, because the one that leaks will be the one somebody adds in a
    /// hurry.
    /// </summary>
    [Fact]
    public void NoGameTextInThisLaneNamesTheThing()
    {
        List<string> everySurface =
        [
            OldCrewScene.Photograph,
            OldCrewScene.SummerPartyPage,
            OldCrewScene.SummerPartyTitle,
            OldCrewScene.SummerPartyProvenance,
            OldCrewScene.AtTheRegistrarsDoor,
            OldCrewScene.KnockNerveLabel,
            CaptainCrossings.Heading,
            CaptainCrossings.Line(CaptainCrossings.Kind.OwnFaceExplained),
            ContactDrink.SharedHistoryLabel,
            ContactDrink.SignerInTheRoomLabel,
            ContactDrink.FlingInTheRoomLabel,
            .. Enum.GetValues<HeldMemory.Mark>().Select(HeldMemory.Label),
            .. Enum.GetValues<HeldMemory.Theory>().Select(HeldMemory.Label),
            .. Enum.GetValues<OldCrew.BondKind>().Select(OldCrew.Name),
            .. OldCrew.Pool.Select(p => p.Name),
            .. OldCrew.Pool.Where(p => p.Living).Select(p => OldCrewScene.Opening(p.Id)),
            .. OldCrew.Pool.Where(p => p.Living).Select(p => OldCrewScene.Slip(p.Id)),
            .. Enum.GetValues<OldCrewScene.Answer>().Select(OldCrewScene.Button),
            .. Enum.GetValues<OldCrewScene.Answer>().Select(OldCrewScene.Said),
            .. OldCrew.Pool.Where(p => p.Living)
                .SelectMany(p => Enum.GetValues<OldCrewScene.Answer>().Select(a => OldCrewScene.Reply(p.Id, a))),
            .. ManyThreads(20).SelectMany(t => OldCrew.Seed(t, Berths).Select(s => s.History())),
        ];

        Assert.All(everySurface, line => Assert.False(string.IsNullOrWhiteSpace(line)));

        // The word the arc never says.
        Assert.All(everySurface, line =>
            Assert.DoesNotContain("copy", line, StringComparison.OrdinalIgnoreCase));

        // …and WHAT WAS IN THE PODS. Never named — which is the law, and it is about the CONTENTS.
        //
        // #973 L3 narrowed this list, deliberately and with the canon in hand. L5a wrote it before the slips
        // existed and reached for the nearest nouns as a proxy; Fable's own text for the signer's slip is
        // "a customs receipt for sealed reefer pods, medical, and a counter-signature you would know
        // anywhere", which is the single strongest clue in the arc and says nothing whatever about what was
        // inside. A crate is not its contents. What must never appear is the WORD FOR THE THING — and every
        // one of those is still swept below, alongside the manifest's own lie ("medical") going on being
        // allowed to stand, because the lie is the point.
        foreach (string forbidden in new[] { "restore", "clone", "backup", "cadaver", "corpse", "body double" })
        {
            Assert.All(everySurface, line =>
                Assert.DoesNotContain(forbidden, line, StringComparison.OrdinalIgnoreCase));
        }

        // …and the receipt is real: the one line that DOES name the crates is the signer's slip, and nobody
        // else's surface has picked the noun up.
        Assert.Contains("reefer pods", OldCrewScene.Slip(OldCrew.SignerId), StringComparison.Ordinal);
        Assert.Equal(
            1,
            everySurface.Count(line => line.Contains("reefer", StringComparison.OrdinalIgnoreCase)));
    }
}
