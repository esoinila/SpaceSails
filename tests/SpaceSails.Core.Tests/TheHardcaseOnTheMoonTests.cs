using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1061 beat 2 · <b>THE HARDCASE ON THE MOON</b> — every law Core can be asked about Brem Kolt.
///
/// <para>The laws, in the order they matter: <b>WHERE</b> he can be found (never a berth, never a hull,
/// never a poured floor), <b>HOW OFTEN</b> (one ground in three, and never a third ground in a universe),
/// <b>WHAT HE SAYS</b> (three authored sentences and no fourth), <b>HOW HE RUNS</b> (the pathfinder, which
/// the thing he runs from is refused at the door), and <b>WHAT HE DROPS</b> (one sheet, titled and read as
/// it was written, filed under the company and the ground).</para>
///
/// <para>Every guard here was watched go RED against the behaviour it forbids before it was kept — the
/// house rule, and the reason none of them is a test that asserts nothing.</para>
/// </summary>
public sealed class TheHardcaseOnTheMoonTests
{
    // ── WHERE ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HE IS A MAN ON A MOON.</b> A docked berth is Fess's beat, a derelict is nobody's, and a poured
    /// facility under the regolith is the canteen floor where beat 1's round already lives.
    ///
    /// <para>The fourth case is the one that makes this a world that can tell pass from fail: a landed
    /// captain on floor 0 of something that is not a wreck IS his ground, so the predicate is not merely
    /// saying no to everything.</para>
    /// </summary>
    [Theory]
    [InlineData(false, false, 0, false)]   // a docked berth — no excursion at all
    [InlineData(true, true, 0, false)]     // a derelict alongside
    [InlineData(true, false, -1, false)]   // B1, under the regolith
    [InlineData(true, false, -3, false)]   // deeper still
    [InlineData(true, false, 0, true)]     // the regolith itself, which is the whole of his beat
    public void HeIsOnlyEverFoundOnTheOpenRegolith(bool landed, bool wreck, int floor, bool his) =>
        Assert.Equal(his, HardcaseRep.GroundLikeThis(landed, wreck, floor));

    // ── HOW OFTEN ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NEVER A THIRD GROUND.</b> Two in the book and the answer is no for every ground there is, whatever
    /// the seed says.
    ///
    /// <para>And the world can tell: the SAME grounds, asked with an empty book, say yes to some of them.
    /// A guard that only proved "no" against a rota nobody had shown could say "yes" would be this
    /// repository's fifth named bug class — a threshold that selects everything.</para>
    /// </summary>
    [Fact]
    public void HeIsNeverFoundOnAThirdGround()
    {
        string[] full = ["luna:0", "ganymede:1"];
        var grounds = ManyGrounds().ToList();

        Assert.Contains(grounds, g =>
            HardcaseRep.WorksThisGround(Thread, g.Body, g.Site, []));

        foreach ((string body, int site) in grounds)
        {
            string key = HardcaseRep.GroundKey(body, site);
            if (full.Contains(key, StringComparer.Ordinal))
            {
                // One of HIS two. He is still there, which is the clause above this one.
                Assert.True(HardcaseRep.WorksThisGround(Thread, body, site, full));
                continue;
            }

            Assert.False(
                HardcaseRep.WorksThisGround(Thread, body, site, full),
                $"the book is full and he turned up on a third ground, {key}");
        }
    }

    /// <summary>A ground already in the book is still his — asked FIRST, so a captain who lifts off and sets
    /// down again finds the same man rather than rolling him away, and a revisit cannot spend one of the
    /// two.</summary>
    [Fact]
    public void AGroundHeHasAlreadyWorkedIsStillHis()
    {
        // Deliberately a ground the ROTA refuses, so the clause being proved is the book's and not the
        // roll's — a world where both answers are yes could not tell them apart.
        (string body, int site) = ManyGrounds().First(g =>
            !HardcaseRep.WorksThisGround(Thread, g.Body, g.Site, []));

        string key = HardcaseRep.GroundKey(body, site);
        Assert.True(HardcaseRep.WorksThisGround(Thread, body, site, [key]));
    }

    /// <summary>The cap is written by the same function that enforces it: two entries at most, in the order
    /// they happened, and never one ground twice.</summary>
    [Fact]
    public void TheBookIsWrittenWhereTheCapIsKept()
    {
        IReadOnlyList<string> book = [];
        foreach ((string body, int site) in ManyGrounds())
        {
            book = HardcaseRep.WithGroundWorked(book, HardcaseRep.GroundKey(body, site));
        }

        Assert.Equal(HardcaseRep.GroundsAtMost, book.Count);
        Assert.Equal(book.Count, book.Distinct(StringComparer.Ordinal).Count());

        // …and re-writing a ground already in it changes nothing at all.
        Assert.Equal(book, HardcaseRep.WithGroundWorked(book, book[0]));
    }

    /// <summary>He is NOT on every moon. One ground in <see cref="HardcaseRep.OneGroundIn"/>, which is what
    /// keeps him from being scenery — asked of an empty book so the cap cannot be doing this work.</summary>
    [Fact]
    public void MostGroundsDoNotHaveHimOnThem()
    {
        var grounds = ManyGrounds().ToList();
        int on = grounds.Count(g => HardcaseRep.WorksThisGround(Thread, g.Body, g.Site, []));

        Assert.InRange(on, 1, grounds.Count - 1);
        Assert.True(on * 2 < grounds.Count, $"{on} of {grounds.Count} grounds is not a rota, it is a rule");
    }

    /// <summary>Deterministic in the thread and the ground — no clock, no <c>Random</c> — and two universes
    /// do not find him in the same craters.</summary>
    [Fact]
    public void TheSameUniverseAlwaysMeetsHimInTheSameCrater()
    {
        foreach ((string body, int site) in ManyGrounds())
        {
            Assert.Equal(
                HardcaseRep.WorksThisGround(Thread, body, site, []),
                HardcaseRep.WorksThisGround(Thread, body, site, []));
        }

        Assert.Contains(ManyGrounds(), g =>
            HardcaseRep.WorksThisGround(Thread, g.Body, g.Site, [])
            != HardcaseRep.WorksThisGround("another-universe", g.Body, g.Site, []));
    }

    /// <summary>#320 gives one body two to four grounds and they are genuinely different places to be found
    /// on, so the key names both. A key that was only the body would make a captain who set down twice on
    /// one moon into a captain who had met him on two.</summary>
    [Fact]
    public void AGroundIsABodyAndASite()
    {
        Assert.NotEqual(HardcaseRep.GroundKey("luna", 0), HardcaseRep.GroundKey("luna", 1));
        Assert.NotEqual(HardcaseRep.GroundKey("luna", 0), HardcaseRep.GroundKey("ganymede", 0));
        Assert.Equal(HardcaseRep.GroundKey("luna", 2), HardcaseRep.GroundKey("luna", 2));
    }

    // ── WHAT HE SAYS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The three authored sentences, verbatim, and the fact that there are exactly three of them:
    /// the opener, the pitch, the answer to a refusal. Nothing in the beat may grow a fourth.</summary>
    [Fact]
    public void TheThreeLinesAreTheOnesThatWereWritten()
    {
        Assert.Equal(
            "Captain. Brem Kolt, hazardous accounts. Your policy travelled better than your face did.",
            HardcaseRep.Opener);
        Assert.Equal(
            "Out here the premiums write themselves. Sign, and the company remembers you kindly — somewhere "
            + "it matters.",
            HardcaseRep.Pitch);
        Assert.Equal(
            "They all decline on the first moon. The book says you'll sign on the second.",
            HardcaseRep.OnRefusal);
    }

    /// <summary>Line three is not decoration: it is <see cref="HardcaseRep.GroundsAtMost"/> written as a
    /// sentence. The book expects a signature on the SECOND moon, so there is a second moon and there is no
    /// third — and if anybody ever changes the number, this pairing says the line has to change too.</summary>
    [Fact]
    public void TheCapIsLineThreeReadAsArithmetic()
    {
        Assert.Equal(2, HardcaseRep.GroundsAtMost);
        Assert.Contains("second", HardcaseRep.OnRefusal, StringComparison.Ordinal);
    }

    /// <summary>
    /// His buttons are <see cref="NebulaRep"/>'s — one firm, one policy, one set of prices, so #227's vendor
    /// lane re-prices ONE seam.
    ///
    /// <para>And he is denied Fess's two conversational moves: <i>"I already have a policy"</i> opens a
    /// signing flashback about a FILE he is not holding, and <i>"that's not my name"</i> answers a bleed his
    /// own opener has already said out loud.</para>
    /// </summary>
    [Theory]
    [InlineData(InsuranceTier.None)]
    [InlineData(InsuranceTier.Basic)]
    [InlineData(InsuranceTier.Premium)]
    public void HisButtonsAreTheFirmsOwn(InsuranceTier tier)
    {
        IReadOnlyList<NebulaRep.RepOffer> offers = HardcaseRep.OffersFor(tier);

        Assert.NotEmpty(offers);
        Assert.DoesNotContain(offers, o => o.Move == NebulaRep.RepMove.AlreadyHaveAPolicy);
        Assert.DoesNotContain(offers, o => o.Move == NebulaRep.RepMove.ThatsNotMyName);

        foreach (NebulaRep.RepOffer offer in offers)
        {
            Assert.Equal(NebulaRep.PremiumFor(
                offer.Move switch
                {
                    NebulaRep.RepMove.BuyBasic => InsuranceTier.Basic,
                    NebulaRep.RepMove.BuyPremium => InsuranceTier.Premium,
                    _ => InsuranceTier.None,
                }),
                offer.PriceCr);
            Assert.False(string.IsNullOrWhiteSpace(offer.Label));
        }

        // Every card has a way out that costs nothing — the ruling of 2026-08-24, answered in Core so the
        // page cannot draw a pitch nobody can decline.
        Assert.Contains(offers, o => o.PriceCr == 0);
    }

    /// <summary>A captain who is already on Premium is sold nothing, because there is nothing left to
    /// sell — and the way out is <see cref="NebulaRep.GoodDayLabel"/>, the constant BOTH salesmen now
    /// print.</summary>
    [Fact]
    public void ThePremiumCaptainIsSoldNothing()
    {
        IReadOnlyList<NebulaRep.RepOffer> offers = HardcaseRep.OffersFor(InsuranceTier.Premium);

        Assert.DoesNotContain(offers, o => o.PriceCr > 0);
        Assert.Equal(NebulaRep.GoodDayLabel, Assert.Single(offers).Label);
        Assert.Contains(NebulaRep.PitchFor(InsuranceTier.Premium, "Captain Vane").Offers,
            o => o.Label == NebulaRep.GoodDayLabel);
    }

    // ── HOW HE RUNS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE FLEEING MAN USES THE PATHFINDER AND THE THING HE FLEES MAY NOT.</b> That asymmetry is the
    /// scene, and it is not a convention anybody has to remember: <see cref="NpcWalk.Plan"/> throws on any
    /// gait but a person's, so a lane that tried to give an Old One a route would not compile a world.
    ///
    /// <para>The route is asserted to be a REAL ROUTE round a real obstruction — more than the two endpoints
    /// — because "it planned something" is satisfied by a straight line through stone.</para>
    /// </summary>
    [Fact]
    public void HeRunsOnTheLatticeAndTheOldOnesNeverDo()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = AWallWithAWayRound();
        var from = new DeckReachability.Point(-6, 0);
        var to = new DeckReachability.Point(6, 0);

        NpcWalk? run = NpcWalk.Plan(
            HardcaseRep.Plate, new NpcWalk.Bound("", to.X, to.Y), from, walls,
            ABodyRadius, SurfaceCollision.Gait.Person,
            HardcaseRep.DespairPaceDu, NpcWalk.NoPersonalSpace);

        Assert.NotNull(run);
        Assert.True(run!.Route.Count > 2, "a run straight through the slab is not a route");
        Assert.Equal(HardcaseRep.DespairPaceDu, run.Pace);

        Assert.Throws<ArgumentException>(() => NpcWalk.Plan(
            "Reever", new NpcWalk.Bound("", to.X, to.Y), from, walls,
            ABodyRadius, SurfaceCollision.Gait.Stagger, HardcaseRep.DespairPaceDu));
    }

    /// <summary>The despair gait is a RUN and not an amble: legibly faster than the walk the same body was
    /// doing thirty seconds earlier, which is what makes the break readable from across a field. Quoted from
    /// the one other beat in the game where a walk turns into something else.</summary>
    [Fact]
    public void TheDespairGaitIsARun()
    {
        Assert.True(HardcaseRep.DespairPaceDu > NpcWalk.PaceDu * 2,
            "a man who breaks and runs at a walking pace is a man taking a slightly brisk walk");
        Assert.Equal(PatrolBeat.AfterYouSpeed, HardcaseRep.DespairPaceDu);
    }

    /// <summary>How far he can see one is Core's own ruling about when an Old One stops being scenery, and
    /// not a number of this file's own — two opinions about one distance is how the nerve and the eye come
    /// to disagree.</summary>
    [Fact]
    public void HisEyesAreCoresOwnRange() =>
        Assert.Equal(NerveModel.DreadRangeDeckUnits, HardcaseRep.SeesOneAtDu);

    // ── WHAT HE DROPS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SHEET READS AS A MENTION, NOT A POSITION.</b> Everything the pocket says about a paper is
    /// seeded off its id, and this id seeds <see cref="FieldClue.Certainty.Vague"/> — <i>"a place mentioned
    /// the way you mention a place you have never had to find"</i>, which is exactly what a schedule of
    /// rates by site is.
    ///
    /// <para>Pinned so a rename cannot quietly promote a price list into a position and have the tracker
    /// paint a dot for it.</para>
    /// </summary>
    [Fact]
    public void TheScheduleIsAMentionAndNeverAPosition()
    {
        Assert.Equal(FieldClue.Certainty.Vague, FieldClue.CertaintyOf(HardcaseRep.ScheduleFindId));
        Assert.False(FieldClue.IsExact(FieldClue.CertaintyOf(HardcaseRep.ScheduleFindId)));
    }

    /// <summary>The one authored sheet in the game is titled and read as it was written — in the sleeve, on
    /// the card and in the gist filed when it is put down, all through the same two functions every seeded
    /// paper goes through. And the seeded papers are untouched: a branch that had swallowed them would be a
    /// guard with nothing behind it.</summary>
    [Fact]
    public void TheScheduleIsTitledAndReadAsItWasAuthored()
    {
        Assert.Equal(HardcaseRep.ScheduleLabel, FieldClue.Title(HardcaseRep.ScheduleFindId));
        Assert.Equal(HardcaseRep.ScheduleBody, FieldClue.Document(HardcaseRep.ScheduleFindId));

        CarriedObject.Reveal card = CarriedObject.PaperReveal(HardcaseRep.ScheduleFindId);
        Assert.Equal(HardcaseRep.ScheduleLabel, card.Label);
        Assert.Equal(HardcaseRep.ScheduleBody, card.Story);
        Assert.Equal("", card.ArtUrl);   // #528's caption-only idiom: nothing here is a picture of him

        // …and an ordinary find is still an ordinary find.
        Assert.NotEqual(HardcaseRep.ScheduleLabel, FieldClue.Title("hive:luna:-3:7"));
        Assert.NotEqual(HardcaseRep.ScheduleBody, FieldClue.Document("hive:luna:-3:7"));
    }

    /// <summary>It prices what it never names. The body of the sheet says nothing about what happens on the
    /// grounds it prices — no Old One, no incident, no origin — which is the inference-horror law written as
    /// an assertion about a string.</summary>
    [Fact]
    public void TheSheetNamesNothingItIsAboutIndeed()
    {
        foreach (string forbidden in new[] { "Reever", "reever", "Old One", "died", "killed", "attack" })
        {
            Assert.DoesNotContain(forbidden, HardcaseRep.ScheduleBody, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <b>THE SHEET JOINS THE COMPANY'S THREAD.</b> Two subjects, both already printed for the captain to
    /// read: the letterhead on his card and the site name on the plate the shuttle sets you down under.
    ///
    /// <para>The OFFICE is the point of it. A second entry naming Nebula Mutual is what turns a loose note
    /// into a stack on the threads page (#741/#898) — proved by filing one beside another and watching the
    /// heading appear, rather than by asserting that a string contains a string.</para>
    /// </summary>
    [Fact]
    public void ClippingItInJoinsTheCompanysThread()
    {
        string subjects = HardcaseRep.ScheduleSubjects("The Crater Shelf");
        IReadOnlyList<CaseSubjects.Subject> on = CaseSubjects.On(
            new FieldNote(HardcaseRep.ScheduleBody, 1, "Luna", HardcaseRep.ScheduleGlyph, subjects));

        Assert.Contains(on, s => s.Of == CaseSubjects.Kind.Office && s.Name == HardcaseRep.Company);
        Assert.Contains(on, s => s.Of == CaseSubjects.Kind.Place && s.Name == "The Crater Shelf");
        Assert.DoesNotContain(on, s => s.Of == CaseSubjects.Kind.Person);

        // …and it STACKS with anything else the book has written about the same letterhead.
        List<FieldNote> book =
        [
            new("A poster promising to bring you back meaner.", 1, "The Tilt", "📄",
                CaseSubjects.Line(CaseSubjects.Office(HardcaseRep.Company))),
            new(HardcaseRep.ScheduleBody, 2, "Luna", HardcaseRep.ScheduleGlyph, subjects),
        ];

        CaseSubjects.SubjectThread thread = Assert.Single(
            CaseSubjects.ThreadsOf(book),
            t => t.Subject.Of == CaseSubjects.Kind.Office && t.Subject.Name == HardcaseRep.Company);
        Assert.Equal(2, thread.Entries.Count);
    }

    /// <summary>One document and not one per ground: it is the company's rate book, the same rate book on
    /// both moons, and a captain holding two of them would be holding a bug. The sleeve agrees — a second
    /// copy does not go in.</summary>
    [Fact]
    public void ThereIsOnlyOneOfIt()
    {
        var sheet = new Satchel.Item(Satchel.Kind.Paper, HardcaseRep.ScheduleFindId);
        IReadOnlyList<Satchel.Item> sleeve = Satchel.Add(Satchel.Add([], sheet), sheet);

        Assert.Single(sleeve);
        Assert.True(HardcaseRep.IsTheSchedule(sleeve[0].Id));
        Assert.False(HardcaseRep.IsTheSchedule("hive:luna:-3:7"));
        Assert.False(HardcaseRep.IsTheSchedule(null));
    }

    /// <summary>It rides in the SLEEVE, which is where paper rides — so a captain carrying it is carrying a
    /// document and not a piece of kit, and #678's full-pocket refusal is the one they can actually hit.</summary>
    [Fact]
    public void ItRidesWhereverPaperRides() =>
        Assert.Equal(
            Satchel.Compartment.Sleeve,
            Satchel.CompartmentOf(Satchel.Kind.Paper));

    // ── THE VAULT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A pre-#1061 file round-trips BYTE FOR BYTE. The digest is taken over the payload, so a section that
    /// wrote <c>"hardcase": []</c> on every save would change the checksum of every vault ever written and
    /// hang the 📛 tampered marker on an honest voyage.
    /// </summary>
    [Fact]
    public void ALegacyVaultRoundTripsByteForByteAcrossTheHardcase()
    {
        string legacy = LegacyWeatherVault.ReplaceLineEndings();

        Vault loaded = VaultSerializer.Load(legacy);

        Assert.False(loaded.Tampered);
        Assert.NotNull(loaded.InsuranceWeather);
        Assert.Null(loaded.InsuranceWeather!.Hardcase);        // a universe that never met him
        Assert.Single(loaded.InsuranceWeather.Heard);          // …and the rest of the section survived

        string rewritten = VaultSerializer.Save(loaded);
        Assert.Equal(legacy, rewritten);
        Assert.DoesNotContain("hardcase", rewritten, StringComparison.Ordinal);
    }

    /// <summary>…and the other half, so the rows are not merely unwritten but unwritten FOR A REASON: a
    /// universe that HAS met him carries them, and they come back the same.</summary>
    [Fact]
    public void TheGroundsHeWasFoundOnRideTheVault()
    {
        Vault legacy = VaultSerializer.Load(LegacyWeatherVault.ReplaceLineEndings());
        var before = new Vault
        {
            Version = legacy.Version,
            SavedSimTime = legacy.SavedSimTime,
            InsuranceWeather = legacy.InsuranceWeather! with
            {
                Hardcase = ["luna:0", "ganymede:2"],
            },
        };

        Vault after = VaultSerializer.Load(VaultSerializer.Save(before));

        Assert.False(after.Tampered);
        Assert.Equal(["luna:0", "ganymede:2"], after.InsuranceWeather!.Hardcase);
    }

    // ── The bench ──────────────────────────────────────────────────────────────────────────────────────

    private const string Thread = "hardcase-tests";

    /// <summary>A body's width, which is the captain's — <c>DeckPlan.AvatarRadius</c>, quoted as a number
    /// because Core cannot see the client's deck and the whole point of the guard below is that the route is
    /// planned for a person-sized body.</summary>
    private const double ABodyRadius = 0.7;

    /// <summary>Enough real grounds that a rota of one in three has both answers in it — the world this
    /// file's presence guards are asked about, and the reason none of them is a threshold that selects
    /// everything.</summary>
    private static IEnumerable<(string Body, int Site)> ManyGrounds()
    {
        foreach (string body in new[] { "luna", "miranda", "ganymede", "europa", "titan", "phobos" })
        {
            for (int site = 0; site < LandingSites.MaxSites; site++)
            {
                yield return (body, site);
            }
        }
    }

    /// <summary>A slab with open ground round both ends of it: a straight line from one side to the other is
    /// blocked, so a plan that comes back with two waypoints did not plan anything.</summary>
    private static IReadOnlyList<SurfaceCollision.Segment> AWallWithAWayRound() =>
    [
        new(0, -4, 0, 4),
    ];

    /// <summary>Captured from a build before this lane: the insurance weather with one line heard and one
    /// station stood in, and no hardcase rows. Verbatim — the whole point is the bytes.</summary>
    private const string LegacyWeatherVault = """
        {
          "version": 1,
          "savedSimTime": 90000,
          "sections": {
            "insuranceweather": {
              "heard": [
                "cover-lapses|2"
              ],
              "stations": [
                "the-tilt|3|1"
              ]
            }
          },
          "checksum": "7344320ca4eb047497d664c4c1f2c4dee71570abf35671b439755865bb1c717e"
        }
        """;
}
