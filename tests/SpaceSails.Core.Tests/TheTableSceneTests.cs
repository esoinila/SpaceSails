using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #746 · THE TABLE SCENE — the machine, the dice and what the three bands actually hand you.
///
/// <para>Owner, 2026-08-06: <i>"asking to sit is missing; tables should seat 2/4/more, not all pairs...
/// guard stops should use the same choose-your-action mechanic... with all the charm we can muster, get the
/// job that takes us downstairs — and politely dodge the jobs that don't."</i></para>
///
/// <para>Every guard here was watched RED before it was watched green — the thresholds against a real
/// <see cref="DiceRule"/> sweep rather than against the arithmetic in a comment, and the Hand's three
/// outcomes by forcing each band in turn. A band whose granted state nobody pinned is a band that will
/// quietly stop granting it.</para>
/// </summary>
public sealed class TheTableSceneTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    // ── SEATS ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryTableSeatsTwoFourOrSixAndSaysTheSameNumberTwice()
    {
        // The owner asked for 2/4/more rather than "all pairs". A seat count that could come back 3, or 0, or
        // a different number on the second look, is a number nothing may be built on.
        int tops = 0;
        var spread = new HashSet<int>();

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.Amenity a in
                    UndergroundComplex.Build(body, level, Field).Amenities)
                {
                    for (int i = 0; i < a.Tables.Count; i++)
                    {
                        int seats = CanteenRegulars.SeatsAt(body, a, i);
                        Assert.Contains(seats, CanteenRegulars.SeatCounts);
                        Assert.Equal(seats, CanteenRegulars.SeatsAt(body, a, i));
                        spread.Add(seats);
                        tops++;
                    }
                }
            }
        }

        Assert.True(tops > 100, $"only {tops} round tops swept — this guard is not walking the world.");

        // …and all three counts actually occur. A seeded pick that landed on one value forever would satisfy
        // every assertion above and would be exactly the "all pairs" the owner filed the issue about.
        Assert.Equal(CanteenRegulars.SeatCounts.Count, spread.Count);
    }

    [Fact]
    public void TheSeatCountIsFurnitureAndNeverTurnsOverWithTheSHIFT()
    {
        // A room that re-furnished itself every watch would be the picture and the sim disagreeing about a
        // thing the player can literally count.
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }
            UndergroundComplex.Amenity canteen = UndergroundComplex.Build(body, level, Field).Amenities
                .Single(a => a.Use == UndergroundComplex.Comfort.UpperCanteen);

            for (int i = 0; i < canteen.Tables.Count; i++)
            {
                int seats = CanteenRegulars.SeatsAt(body, canteen, i);
                for (long watch = 0; watch < 8; watch++)
                {
                    Assert.Equal(seats, CanteenRegulars.Tables(body, level, canteen, watch)[i].Seats);
                }
            }
        }
    }

    [Fact]
    public void ONESOURCE_TheTablesAndTheSeatedAgreeOnEveryFloorOfEverySite()
    {
        // THE ONE-SOURCE GUARD. Tables() and Sitting() are two readings of one rota, and the day they part
        // company the drawn room and the pressed room stop being one room — #709's own warning, and this
        // repo's most expensive bug class.
        int checkedTops = 0, occupied = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.Amenity a in
                    UndergroundComplex.Build(body, level, Field).Amenities)
                {
                    for (long watch = 0; watch < 4; watch++)
                    {
                        var tops = CanteenRegulars.Tables(body, level, a, watch);
                        var sat = CanteenRegulars.Sitting(body, level, a, watch);

                        // #751 · …plus the cabinets, which are tops in this room like any other and hang off
                        // the HALL rather than off its floor: their chairs are extra and are deliberately
                        // not in the hall's own eighty (TheCantinaHallTests measures that).
                        Assert.Equal(a.Tables.Count + (a.Hall?.Cabinets.Count ?? 0), tops.Count);

                        // #751 · …and the NAMED cast, which is all Sitting() has ever answered about. The
                        // hall's background patrons are a third tier on the same list and are Sitting()'s
                        // business no more than the furniture is.
                        Assert.Equal(sat.Count, tops.Count(t => t.Taken && !t.Stranger));
                        occupied += sat.Count;
                        checkedTops += tops.Count;

                        foreach (CanteenRegulars.Seated s in sat)
                        {
                            CanteenRegulars.TableSeat top = tops.Single(
                                t => t.X == s.X && t.Y == s.Y);
                            Assert.Equal(s.Plate, top.Plate);
                            Assert.Equal(s.Line, top.Line);
                        }

                        foreach (CanteenRegulars.TableSeat t in tops)
                        {
                            // #823 · The chairs left are the seats less the PARTY, and the party is what it
                            // says it is. This read `t.Taken ? 1 : 2` while every occupied top was assumed
                            // to hold one person — the assumption the owner counted his way out of.
                            Assert.Equal(t.Seats - t.Heads, t.Free);
                            Assert.InRange(t.Free, 0, t.Seats);
                            Assert.InRange(t.Heads, t.Taken ? 1 : 0, t.Taken ? t.Seats : 0);
                        }
                    }
                }
            }
        }

        Assert.True(checkedTops > 100, $"only {checkedTops} tops swept.");
        Assert.True(occupied > 20, $"only {occupied} people seated across the sweep — nobody is in the bar.");
    }

    // ── THE DICE ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void THRESHOLDS_EveryBandIsReachableWithNoModifiersAtAll()
    {
        // FIFTH BUG CLASS, paid up front. The thresholds are constants; what matters is whether the DIE can
        // reach them. This measures the bands against what DiceRule actually produces — a threshold no real
        // roll can hit is a guard that asserts nothing, and this project has shipped exactly that.
        var seen = new Dictionary<Encounter.Band, int>
        {
            [Encounter.Band.Yes] = 0, [Encounter.Band.YesBut] = 0, [Encounter.Band.NoAnd] = 0,
        };

        for (int attempt = 0; attempt < 400; attempt++)
        {
            DiceRoll roll = Encounter.Roll("luna", -1, "Hand", "work", attempt, new Encounter.Situation());
            Assert.Empty(roll.Modifiers);                       // the naked case, on purpose
            Assert.InRange(roll.Face, 1, DiceRule.D20);
            seen[Encounter.BandOf(roll.Total)]++;
        }

        foreach ((Encounter.Band band, int count) in seen)
        {
            Assert.True(count > 0, $"400 unmodified rolls never produced {band} — the threshold is unreachable.");
        }

        // And neither threshold selects EVERYTHING: the middle band has to be a band and not a rounding
        // error, which is the other half of the same bug class.
        Assert.True(seen[Encounter.Band.YesBut] > 40,
            $"YES-BUT came up only {seen[Encounter.Band.YesBut]} times in 400 — the middle band is vestigial.");
        Assert.InRange(Encounter.YesButAt, 2, Encounter.YesAt - 1);
        Assert.InRange(Encounter.YesAt, Encounter.YesButAt + 1, DiceRule.D20);
    }

    [Fact]
    public void TheRollIsDeterministicAndKeyedOnEveryPartOfTheAsk()
    {
        var s = new Encounter.Situation();

        // Same ask, same answer. A captain may not re-press their way to a better one.
        Assert.Equal(
            Encounter.Roll("luna", -1, "Hand", "work", 0, s).Face,
            Encounter.Roll("luna", -1, "Hand", "work", 0, s).Face);

        // …and every key component actually moves the seed. A key that ignored one of its parts would make
        // two different tables one table.
        ulong baseline = Encounter.Roll("luna", -1, "Hand", "work", 0, s).Seed;
        Assert.NotEqual(baseline, Encounter.Roll("phobos", -1, "Hand", "work", 0, s).Seed);
        Assert.NotEqual(baseline, Encounter.Roll("luna", -2, "Hand", "work", 0, s).Seed);
        Assert.NotEqual(baseline, Encounter.Roll("luna", -1, "Fitter", "work", 0, s).Seed);
        Assert.NotEqual(baseline, Encounter.Roll("luna", -1, "Hand", "smalltalk", 0, s).Seed);
        Assert.NotEqual(baseline, Encounter.Roll("luna", -1, "Hand", "work", 1, s).Seed);
    }

    [Fact]
    public void TheModifiersAreNamedSituationsAndOnlyTheOnesThatHold()
    {
        Assert.Empty(Encounter.Modifiers(new Encounter.Situation()));

        var all = Encounter.Modifiers(new Encounter.Situation(true, true, true, true, true));
        Assert.Equal(5, all.Count);
        Assert.Equal(+1, all.Sum(m => m.Value));   // +3 and −2, the whole spread of the stack

        // The labels are sentences about the scene, and the UI shows them — an unnamed modifier is a number
        // the player is asked to trust.
        Assert.All(all, m => Assert.False(string.IsNullOrWhiteSpace(m.Label)));

        var oneGood = Encounter.Modifiers(new Encounter.Situation(RoundBought: true));
        Assert.Single(oneGood);
        Assert.Equal(Encounter.RoundLabel, oneGood[0].Label);
        Assert.Equal(+Encounter.ModifierStep, oneGood[0].Value);

        var oneBad = Encounter.Modifiers(new Encounter.Situation(Fumbled: true));
        Assert.Single(oneBad);
        Assert.Equal(-Encounter.ModifierStep, oneBad[0].Value);
    }

    [Fact]
    public void ABoughtRoundActuallyMovesTheOddsAcrossARealSweep()
    {
        // The owner's ask in one sentence: "offer-a-drink needs to matter." A +1 that never changed a band
        // over four hundred rolls would be a receipt rather than a mechanic.
        int naked = 0, warmed = 0;
        for (int attempt = 0; attempt < 400; attempt++)
        {
            if (Encounter.BandOf(
                    Encounter.Roll("luna", -1, "Hand", "work", attempt, new Encounter.Situation()).Total)
                == Encounter.Band.Yes)
            {
                naked++;
            }
            if (Encounter.BandOf(
                    Encounter.Roll("luna", -1, "Hand", "work", attempt,
                        new Encounter.Situation(RoundBought: true, HouseWaysLearned: true)).Total)
                == Encounter.Band.Yes)
            {
                warmed++;
            }
        }
        Assert.True(warmed > naked,
            $"a bought round and the house's ways landed {warmed} yeses against {naked} cold — the " +
            "modifiers do not reach the bands.");
    }

    [Fact]
    public void NerveIsTheSocialResourceAndOnlyTheTwoBandsThatHurtSpendIt()
    {
        Assert.Equal(0, Encounter.NervePipsFor(Encounter.Band.Yes));
        Assert.True(Encounter.NervePipsFor(Encounter.Band.YesBut) > 0);
        Assert.True(Encounter.NervePipsFor(Encounter.Band.NoAnd) > 0);

        // "−1 nerve is marked" is read off the gauge's OWN rungs, so the modifier and the readout the player
        // is looking at can never disagree.
        Assert.False(Encounter.NerveReadsAcrossATable(NerveModel.Steady));
        Assert.True(Encounter.NerveReadsAcrossATable(NervePips.FromPips(4)));
        Assert.True(Encounter.NerveReadsAcrossATable(NervePips.FromPips(0)));
    }

    // ── THE HAND'S THREE BANDS ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void THE_THREE_BANDS_EachOneGrantsItsOwnState()
    {
        // Forced, one band at a time — the only way to prove a band grants what it says without waiting for
        // a die to come up. A band that stopped granting would otherwise be invisible until a playtest.

        CanteenTable.Answer yes = CanteenTable.HandAsksAboutWork(Encounter.Band.Yes);
        Assert.Equal(CanteenTable.HandWorkYes, yes.Line);
        Assert.True(yes.GrantsChit);
        Assert.False(yes.UnderAnotherName);
        Assert.True(yes.ClosesTheAsk);
        Assert.Equal(CanteenTable.ChitGist, yes.Note);
        Assert.False(yes.OpensFitter);

        CanteenTable.Answer but = CanteenTable.HandAsksAboutWork(Encounter.Band.YesBut);
        Assert.Equal(CanteenTable.HandWorkYesBut, but.Line);
        Assert.True(but.GrantsChit);
        Assert.True(but.UnderAnotherName);          // the thread #718 will pull
        Assert.Equal(CanteenTable.ChitUnderAnotherNameGist, but.Note);
        Assert.True(but.NervePips > 0);             // it costs something REAL

        CanteenTable.Answer no = CanteenTable.HandAsksAboutWork(Encounter.Band.NoAnd);
        Assert.Equal(CanteenTable.HandWorkNoAnd, no.Line);
        Assert.False(no.GrantsChit);
        // …AND THE SCENE MOVES. All three of them, or it is a dead wall with a sentence on it.
        Assert.True(no.OpensFitter);
        Assert.True(no.HardensTable);
        Assert.True(no.ArmsTheTemp);
    }

    [Fact]
    public void TheChitIsAWalletCardAndTheNameInTheBookRidesItsOwnIdentity()
    {
        Assert.Equal(Satchel.Compartment.Wallet, Satchel.CompartmentOf(Satchel.Kind.Chit));

        // A wallet never fills — the owner's #688 heartbreak, applied to the paper that opens the cage.
        var stuffed = new List<Satchel.Item>();
        for (int i = 0; i < Satchel.PocketCapacity + Satchel.SleeveCapacity + 4; i++)
        {
            stuffed.Add(new Satchel.Item(Satchel.Kind.Paper, $"paper-{i}"));
            stuffed.Add(new Satchel.Item(Satchel.Kind.Relic, $"relic-{i}"));
        }
        Assert.True(Satchel.CanTake(stuffed, CanteenTable.Chit(false)));

        IReadOnlyList<Satchel.Item> plain = Satchel.Add([], CanteenTable.Chit(false));
        IReadOnlyList<Satchel.Item> booked = Satchel.Add([], CanteenTable.Chit(true));

        Assert.True(CanteenTable.Cover.Held(plain));
        Assert.False(CanteenTable.Cover.UnderAnotherName(plain));
        Assert.True(CanteenTable.Cover.Held(booked));
        Assert.True(CanteenTable.Cover.UnderAnotherName(booked));
        Assert.False(CanteenTable.Cover.Held([]));

        // …and it survives a save, because the satchel's own round trip is the only persistence it has.
        Assert.True(Satchel.Item.TryParse(booked[0].Stored, out Satchel.Item back));
        Assert.Equal(booked[0], back);
        Assert.True(CanteenTable.Cover.UnderAnotherName([back]));
    }

    // ── THE DODGE, AND THE LOUD FILE ──────────────────────────────────────────────────────────────────

    [Fact]
    public void THE_DODGE_IS_FREE_AndChangesNothingAtAll()
    {
        // The owner's smoke test, as an assertion: take the downstairs job, politely dodge the rest. Every
        // field but the line is at its default, and it is spelled out field by field on purpose — a dodge
        // that quietly hardened a table would pass any assertion written as "the answer is not null".
        //
        // #749: asked THROUGH THE MOVE, which is the path the panel takes. A guard that asked a function
        // nobody presses would have gone on passing while the press said nothing at all — which is exactly
        // what it did.
        Encounter.Move dodgeMove = CanteenTable.SceneFor(CanteenTable.Who.Fitter).Moves
            .Single(m => m.Id == CanteenTable.DodgeScaffold);
        CanteenTable.Answer dodge = CanteenTable.SaidPlainly(dodgeMove);
        Assert.Equal(CanteenTable.ScaffoldDodgedLine, dodge.Line);
        Assert.False(dodge.GrantsChit);
        Assert.False(dodge.UnderAnotherName);
        Assert.False(dodge.OpensFitter);
        Assert.False(dodge.HardensTable);
        Assert.False(dodge.ArmsTheTemp);
        Assert.False(dodge.ClosesTheAsk);
        Assert.False(dodge.TeachesTheHouse);
        Assert.Equal(0, dodge.NervePips);
        Assert.Null(dodge.Note);

        // Leaving is the same law one size up, and the framework says it about ANY scene.
        foreach (CanteenTable.Who who in
            new[] { CanteenTable.Who.Hand, CanteenTable.Who.Fitter, CanteenTable.Who.Temp })
        {
            Assert.True(Encounter.CanAlwaysLeave(CanteenTable.SceneFor(who)),
                $"the {who}'s table has grown a price on standing up.");
        }
    }

    /// <summary>
    /// #749 · THE OFFER'S ANSWERS DO NOT EXIST UNTIL THE OFFER HAS BEEN SPOKEN — this sitting.
    ///
    /// <para>Owner, having played it: <i>"a reply you can give before the sentence exists reads as menu, not
    /// conversation."</i> Both halves are asserted, because both halves were wrong: the answers were DRAWN
    /// before the fitter had said anything (greyed, with "Not yet." on them), and the gate they were on was
    /// the WATCH, so sitting down a second time handed you two live buttons answering a sentence the man had
    /// last said to somebody who then stood up and left.</para>
    /// </summary>
    [Fact]
    public void THE_OFFERS_ANSWERS_AreNotOnTheTableUntilHeHasMadeTheOfferThisSitting()
    {
        Encounter.Scene fitter = CanteenTable.SceneFor(CanteenTable.Who.Fitter);
        string[] answers = [CanteenTable.TakeScaffold, CanteenTable.DodgeScaffold];

        IReadOnlyList<Encounter.Move> before = Encounter.OnTheTable(fitter, []);
        foreach (string id in answers)
        {
            Assert.DoesNotContain(before, m => m.Id == id);
        }

        // …and NOTHING ELSE went missing with them. A requirement you have not met is a door, and a door is
        // drawn and refused out loud (#603) — hiding those would trade one silent panel for another.
        foreach (Encounter.Move m in fitter.Moves)
        {
            if (Array.IndexOf(answers, m.Id) < 0)
            {
                Assert.Contains(before, shown => shown.Id == m.Id);
            }
        }

        IReadOnlyList<Encounter.Move> after = Encounter.OnTheTable(fitter, [CanteenTable.Work]);
        foreach (string id in answers)
        {
            Assert.Contains(after, m => m.Id == id);
            Encounter.Move move = fitter.Moves.Single(m => m.Id == id);

            // THIS SITTING and never the watch. The room remembering that somebody once asked about work at
            // this table is not the same fact as a man having just made you an offer, and the whole bug was
            // one being read for the other: a purse full of credits and a watch full of history still leave
            // an unspoken sentence unanswerable.
            Assert.False(Encounter.Available(move, 9_999, null, madeThisWatch: [CanteenTable.Work],
                madeThisScene: []),
                $"{id} can be pressed on the strength of an ask made in an earlier sitting.");
            Assert.True(Encounter.Available(move, 0, null, madeThisWatch: [], madeThisScene: [CanteenTable.Work]));
        }
    }

    /// <summary>#749 · Every answer at this table CARRIES its line or has a resolver that does — because the
    /// panel presses moves, not names. The dodge is the one that proved it: it spoke only because a client
    /// author had written its id a case, and the next free move anybody adds would have been mute.</summary>
    [Fact]
    public void A_FIXED_OUTCOME_CARRIES_ITS_OWN_LINE_SoAnyPanelCanSayItWithoutKnowingTheMove()
    {
        Encounter.Move dodge = CanteenTable.SceneFor(CanteenTable.Who.Fitter).Moves
            .Single(m => m.Id == CanteenTable.DodgeScaffold);
        Assert.False(dodge.Rolled);
        Assert.Equal(CanteenTable.ScaffoldDodgedLine, dodge.Says);
        Assert.Equal(CanteenTable.ScaffoldDodgedLine, CanteenTable.SaidPlainly(dodge).Line);

        // And the framework's own free exit, on every scene this file builds — the other fixed outcome, and
        // the one that would be noticed in an afternoon if it ever went quiet.
        foreach (CanteenTable.Who who in
            new[] { CanteenTable.Who.Hand, CanteenTable.Who.Fitter, CanteenTable.Who.Temp })
        {
            Encounter.Move leave = CanteenTable.SceneFor(who).Moves
                .Single(m => m.Id == Encounter.Leave);
            Assert.Equal(CanteenTable.LeaveLine, CanteenTable.SaidPlainly(leave).Line);
        }
    }

    [Fact]
    public void THE_LOUD_FILE_ShutsTheAskAndTheBookRemembersTheSlip()
    {
        var dirt = new Satchel.Item(Satchel.Kind.Dirt, "dirt:somebody");
        foreach (CanteenTable.Who who in
            new[] { CanteenTable.Who.Hand, CanteenTable.Who.Fitter, CanteenTable.Who.Temp })
        {
            CanteenTable.Answer said = CanteenTable.PutOnTheTable(dirt, who);
            Assert.Equal(CanteenTable.DirtLine, said.Line);
            Assert.True(said.ClosesTheAsk, $"a file on somebody left the {who}'s ask open.");
            Assert.False(string.IsNullOrEmpty(said.Note));   // the field book keeps the slip
        }
    }

    [Fact]
    public void THE_DEEP_CARD_QuietsTheHandAndNothingElseOnTheTableDoes()
    {
        var deep = new Satchel.Item(
            Satchel.Kind.Authority,
            new UndergroundComplex.AuthorityCard("luna", CanteenTable.OverqualifiedBand).Id);
        var shallow = new Satchel.Item(
            Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard("luna", 0).Id);
        var manifest = new Satchel.Item(Satchel.Kind.Paper, "paper:manifest");

        Assert.Equal(CanteenTable.DeepCardLine, CanteenTable.PutOnTheTable(deep, CanteenTable.Who.Hand).Line);
        Assert.True(CanteenTable.ForcesTheYesBut(deep, CanteenTable.Who.Hand));

        // Shallow paper is weather on another moon, and the card only means anything to the one person at
        // this table who knows what it is worth.
        Assert.Equal(CanteenTable.OtherPaperLine,
            CanteenTable.PutOnTheTable(shallow, CanteenTable.Who.Hand).Line);
        Assert.Equal(CanteenTable.OtherPaperLine,
            CanteenTable.PutOnTheTable(manifest, CanteenTable.Who.Hand).Line);
        Assert.Equal(CanteenTable.OtherPaperLine,
            CanteenTable.PutOnTheTable(deep, CanteenTable.Who.Fitter).Line);
        Assert.False(CanteenTable.ForcesTheYesBut(deep, CanteenTable.Who.Temp));

        // …and the band it forces is the middle one. Fear, not friendship.
        Assert.Equal(Encounter.Band.YesBut,
            CanteenTable.PutOnTheTable(deep, CanteenTable.Who.Hand).Band);
    }

    // ── THE CONTENT AND THE CAST ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void EachOfTheThreeNamesExactlyOneOfTheHivesOwnRegulars()
    {
        // The plates are matched, not re-authored. If somebody edits a plate in CanteenRegulars this must go
        // red rather than the canteen quietly losing its Hand.
        var plates = CanteenRegulars.AllProse().Where((_, i) => i % 2 == 0).ToList();
        foreach (CanteenTable.Who who in
            new[] { CanteenTable.Who.Hand, CanteenTable.Who.Fitter, CanteenTable.Who.Temp })
        {
            Assert.Single(plates, p => CanteenTable.WhoIs(p) == who);
            Assert.Equal(who, CanteenTable.WhoIs(CanteenRegulars.Glyph + " " + CanteenTable.PlateOf(who)));
        }

        // Everybody else keeps their one breath and is not a scene.
        Assert.True(plates.Count(p => CanteenTable.WhoIs(p) == CanteenTable.Who.None) >= 5);
        Assert.Equal(CanteenTable.Who.None, CanteenTable.WhoIs(null));
    }

    [Fact]
    public void EveryCounterpartWavesYouInAndHasSomethingToSay()
    {
        foreach (CanteenTable.Who who in
            new[] { CanteenTable.Who.Hand, CanteenTable.Who.Fitter, CanteenTable.Who.Temp })
        {
            Encounter.Scene scene = CanteenTable.SceneFor(who);
            Assert.False(string.IsNullOrWhiteSpace(scene.Opening));
            Assert.Equal(CanteenTable.WaveIn(who), scene.Opening);
            Assert.False(string.IsNullOrWhiteSpace(CanteenTable.SmallTalkFirst(who)));
            Assert.False(string.IsNullOrWhiteSpace(CanteenTable.SmallTalkSecond(who)));
        }

        // Only one line in this room teaches the house's ways, and it is the one the Temp does not know is
        // worth anything. A second one would make the +1 unearnable-by-accident rather than earned.
        Assert.True(CanteenTable.TeachesTheHouse(CanteenTable.Who.Temp, second: true));
        Assert.False(CanteenTable.TeachesTheHouse(CanteenTable.Who.Temp, second: false));
        Assert.False(CanteenTable.TeachesTheHouse(CanteenTable.Who.Hand, second: true));
    }

    [Fact]
    public void THE_TEMPS_SECOND_LINE_IsBoughtWithARoundUntilTheSceneMovesForYou()
    {
        Encounter.Move gated = CanteenTable.SceneFor(CanteenTable.Who.Temp).Moves
            .Single(m => m.Id == CanteenTable.SmallTalkAgain);
        Assert.Equal(Encounter.Requirement.PriorMoveThisWatch, gated.Needs);
        Assert.Equal(CanteenTable.Round, gated.After);

        // …and after the Hand waves you off in front of them, it is the ordinary next thing they say. The
        // NO-AND's third effect, expressed as the requirement moving rather than as a second gate.
        Encounter.Move opened = CanteenTable.SceneFor(CanteenTable.Who.Temp, overheard: true).Moves
            .Single(m => m.Id == CanteenTable.SmallTalkAgain);
        Assert.Equal(CanteenTable.SmallTalk, opened.After);

        // The Hand and the Fitter simply keep talking if you stay.
        Assert.Equal(CanteenTable.SmallTalk, CanteenTable.SceneFor(CanteenTable.Who.Hand).Moves
            .Single(m => m.Id == CanteenTable.SmallTalkAgain).After);
    }

    [Fact]
    public void ONLY_THE_HAND_ROLLS()
    {
        Assert.True(CanteenTable.SceneFor(CanteenTable.Who.Hand).Moves
            .Single(m => m.Id == CanteenTable.Work).Rolled);

        // Honest work offered plainly is not a check — and the Temp has no job at all, which is who they are
        // rather than a gap in the content.
        Encounter.Move fitter = CanteenTable.SceneFor(CanteenTable.Who.Fitter).Moves
            .Single(m => m.Id == CanteenTable.Work);
        Assert.False(fitter.Rolled);
        Assert.Equal(CanteenTable.FitterWorkLine, fitter.Says);
        Assert.DoesNotContain(CanteenTable.SceneFor(CanteenTable.Who.Temp).Moves,
            m => m.Id == CanteenTable.Work);
    }

    [Fact]
    public void RequirementsAreCheckedTheWayThePanelWillCheckThem()
    {
        Encounter.Scene hand = CanteenTable.SceneFor(CanteenTable.Who.Hand);
        Encounter.Move round = hand.Moves.Single(m => m.Id == CanteenTable.Round);
        Encounter.Move show = hand.Moves.Single(m => m.Id == CanteenTable.Show);
        Encounter.Move again = hand.Moves.Single(m => m.Id == CanteenTable.SmallTalkAgain);

        Assert.False(Encounter.Available(round, CanteenTable.RoundPrice - 1, null, null));
        Assert.True(Encounter.Available(round, CanteenTable.RoundPrice, null, null));

        Assert.False(Encounter.Available(show, 0, [], null));
        Assert.True(Encounter.Available(show, 0, [new Satchel.Item(Satchel.Kind.Paper, "p")], null));

        Assert.False(Encounter.Available(again, 0, null, []));
        Assert.True(Encounter.Available(again, 0, null, [CanteenTable.SmallTalk]));
    }

    // ── THE CLAIM THE WHOLE FRAMEWORK RESTS ON ────────────────────────────────────────────────────────

    [Fact]
    public void A_GUARD_STOP_IS_CONTENT_AndTypeChecksOnTheSameEncounterType()
    {
        // #746's design constraint, discharged. No guards exist yet and none are wired — this is a SAMPLE
        // content entry, written here and nowhere else, whose whole job is to prove that "show ID, explain
        // yourself" needs no new mechanics: a counterpart, a setting, an opening, and moves with the same
        // four requirement kinds and the same rolled/fixed split the canteen uses.
        //
        // If somebody one day makes the encounter type canteen-shaped, THIS is what stops compiling.
        var checkpoint = new Encounter.Scene(
            "guard:checkpoint:sample",
            "A CONTRACT GUARD, BORED AND THOROUGH",
            "a checkpoint that walked up to you",
            "Hold on. Let's see it.",
            [
                new Encounter.Move("show-card", "Show a card",
                    Encounter.Requirement.SatchelItem, Item: Satchel.Kind.Authority),
                new Encounter.Move("explain", "Explain yourself", Rolled: true),
                new Encounter.Move("bribe", "Make it worth the walk back",
                    Encounter.Requirement.Credits, Credits: 40),
                new Encounter.Move("name-drop", "Say who sent you",
                    Encounter.Requirement.PriorMoveThisWatch, After: "explain"),
                new Encounter.Move(Encounter.Leave, "Back off", Says: "You take a step back."),
            ]);

        Assert.True(Encounter.CanAlwaysLeave(checkpoint));
        Assert.True(Encounter.Available(
            checkpoint.Moves[0], 0, [new Satchel.Item(Satchel.Kind.Authority, "luna#1")], null));
        Assert.False(Encounter.Available(checkpoint.Moves[2], 39, null, null));

        // …and its rolled move settles through the same three bands as the Hand's ask, on the same dice.
        var bands = new HashSet<Encounter.Band>();
        for (int attempt = 0; attempt < 200; attempt++)
        {
            bands.Add(Encounter.BandOf(Encounter.Roll(
                "luna", -6, checkpoint.Counterpart, "explain", attempt,
                new Encounter.Situation(NerveMarked: true)).Total));
        }
        Assert.Equal(3, bands.Count);
    }

    [Fact]
    public void TheQaBandOverrideChangesTheBandAndNeverTheRoll()
    {
        DiceRoll roll = Encounter.Roll("luna", -1, "Hand", "work", 7, new Encounter.Situation());
        Assert.Equal(Encounter.BandOf(roll.Total), Encounter.Settle(roll));
        Assert.Equal(Encounter.Band.Yes, Encounter.Settle(roll, Encounter.Band.Yes));
        Assert.Equal(Encounter.Band.NoAnd, Encounter.Settle(roll, Encounter.Band.NoAnd));

        // The roll itself is untouched — the math the player watches add up is the real math.
        Assert.Equal(roll.Face, Encounter.Roll("luna", -1, "Hand", "work", 7, new Encounter.Situation()).Face);
    }

    [Fact]
    public void NothingInThisRoomExplainsWhatTheBuildingIsFor()
    {
        // §13.8, on the file where it holds hardest: the one room in the Hive where somebody could talk.
        string[] forbidden =
        [
            "old one", "reever", "ancient", "alien", "experiment", "specimen",
            "kaamos", "restore", "backup", "vantar", "monolith",
        ];

        foreach (string line in new[]
        {
            CanteenTable.WaveIn(CanteenTable.Who.Hand), CanteenTable.WaveIn(CanteenTable.Who.Fitter),
            CanteenTable.WaveIn(CanteenTable.Who.Temp),
            CanteenTable.SmallTalkFirst(CanteenTable.Who.Hand),
            CanteenTable.SmallTalkFirst(CanteenTable.Who.Fitter),
            CanteenTable.SmallTalkFirst(CanteenTable.Who.Temp),
            CanteenTable.SmallTalkSecond(CanteenTable.Who.Hand),
            CanteenTable.SmallTalkSecond(CanteenTable.Who.Fitter),
            CanteenTable.SmallTalkSecond(CanteenTable.Who.Temp),
            CanteenTable.RoundLine, CanteenTable.DirtLine, CanteenTable.DeepCardLine,
            CanteenTable.OtherPaperLine, CanteenTable.HandWorkYes, CanteenTable.HandWorkYesBut,
            CanteenTable.HandWorkNoAnd, CanteenTable.FitterWorkLine, CanteenTable.ScaffoldDodgedLine,
            CanteenTable.ScaffoldTakenNote, CanteenTable.LeaveLine, CanteenTable.ChitGist,
            CanteenTable.ChitUnderAnotherNameGist, CanteenTable.ChitTitle, CanteenTable.MessBeatLine,
            // #842 · the sentence a top with no chair left answers [E] with. It says nothing about the
            // building either, and it is swept with everything else that can reach a screen from here.
            CanteenTable.TableIsFullLine,
        })
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
