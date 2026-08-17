using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #758 · THE CURTAIN AND THE DOOR — measured against what the seed actually produces.
///
/// <para>Owner, 2026-08-06: <i>"the cabinets could have some kind of privacy curtain effect ... something
/// that prevents the sound or sight catching what happens there too easily but is not a real door."</i></para>
///
/// <h3>Why every number below is COUNTED and not read</h3>
///
/// <para>This repository's fifth named bug class is a guard handed a world that cannot tell pass from fail:
/// a threshold that selects everything, or nothing, asserted against a die nobody rolled. A leak law is
/// exactly that shape of thing — a probability is trivially assertable and almost as trivially wrong — so
/// nothing here compares one arithmetic expression with a second copy of itself. Every band is SWEPT: the
/// seed is asked for thousands of real answers, the leaks are counted, and the count is what the assertions
/// are about. The law is then checked against the count, which is the only direction that can catch a
/// <see cref="CabinetPrivacy.Leaks"/> that ignores the density it was handed.</para>
/// </summary>
public sealed class TheCurtainAndTheDoorTests
{
    /// <summary>The sites the sweep asks. Three, because a law that is only true on Luna is a law about
    /// Luna — the seed folds the body id in and a guard that never varied it could not see that.</summary>
    private static readonly string[] Sites = ["luna", "phobos", "titan"];

    /// <summary>How many full days of watches the sweep walks. Each one visits all six bands once, so the
    /// per-band sample is <c>Days × Sites × cabinets × beats</c> = 40 × 3 × 3 × 12 = 4,320 real rolls.</summary>
    private const int Days = 40;

    /// <summary>How many sensitive beats a sitting is swept for. Twelve is far more paper than anybody puts
    /// on one table; the point is that the beat index genuinely moves the answer.</summary>
    private const int Beats = 12;

    /// <summary>How far a measured rate may sit from the law before the seed is not implementing the law.
    /// Two and a half points, against bands that are 3.6 points apart at their closest — so it is loose
    /// enough not to be a sampling test and tight enough that a flattened chance cannot hide inside it.</summary>
    private const double Tolerance = 0.025;

    /// <summary>Every watch band the rota has, in the order <see cref="CanteenRegulars.WatchFill"/> lists
    /// them.</summary>
    private static IEnumerable<int> EveryBand() => Enumerable.Range(0, CanteenRegulars.WatchFill.Count);

    /// <summary>Sweep one band and count what came out. The sample is real: every roll is
    /// <see cref="CabinetPrivacy.Leaks"/> answering for a site, a cabinet, a watch and a beat that the game
    /// can actually be in.</summary>
    private static (int Leaks, int Total) Sweep(int band, CabinetPrivacy.Stage stage)
    {
        int leaks = 0, total = 0;
        for (int day = 0; day < Days; day++)
        {
            long watch = band + (CanteenRegulars.WatchFill.Count * (long)day);
            foreach (string site in Sites)
            {
                for (int cabinet = 1; cabinet <= UndergroundComplex.CabinetsPerHall; cabinet++)
                {
                    for (int beat = 0; beat < Beats; beat++)
                    {
                        total++;
                        if (CabinetPrivacy.Leaks(site, cabinet, watch, beat, stage))
                        {
                            leaks++;
                        }
                    }
                }
            }
        }
        return (leaks, total);
    }

    // ── (a) THE LEAK LAW ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #758 · SOME AND NOT ALL, AT EVERY DENSITY, AND HEAVIER ROOMS LEAK MORE.
    ///
    /// <para>Three assertions over one sweep, and the first is the one that keeps the other two honest: at
    /// every band the curtain both LEAKS and HOLDS. A law that never leaked would make the second assertion
    /// vacuously ordered and the third trivially inside tolerance, which is the fifth bug class wearing a
    /// statistics hat.</para>
    ///
    /// <para><b>The RED case.</b> Flatten <see cref="CabinetPrivacy.LeakChance"/> to a constant — return
    /// <see cref="CabinetPrivacy.QuietestChance"/> whatever the fill is — and the monotone assertion fires on
    /// the second band with the measured rates all sitting on 7%.</para>
    /// </summary>
    [Fact]
    public void THE_CURTAIN_LeaksSomeAndNotAll_AndMoreInAFullerRoom()
    {
        var measured = new List<(double Fill, double Rate, int Leaks, int Total)>();

        foreach (int band in EveryBand())
        {
            double fill = SittingAlone.Fill(band);
            (int leaks, int total) = Sweep(band, CabinetPrivacy.Stage.Curtain);
            double rate = (double)leaks / total;

            Assert.True(leaks > 0,
                $"the curtain never leaks at fill {fill:F2} over {total} real rolls — a stage-one cabinet " +
                "that cannot be overheard is a dogged door with extra words, and every guard below would " +
                "be measuring a constant.");
            Assert.True(leaks < total,
                $"the curtain leaks on EVERY one of {total} beats at fill {fill:F2} — a threshold that " +
                "selects everything is the fifth bug class, and there would be no privacy behind cloth.");

            // The seed must be implementing THIS law and not some other one that happens to be between 0
            // and 1. This is the assertion that catches a Leaks() which never reads the density it is given.
            Assert.True(Math.Abs(rate - CabinetPrivacy.LeakChance(fill)) < Tolerance,
                $"at fill {fill:F2} the law says {CabinetPrivacy.LeakChance(fill):P2} and the seed " +
                $"produced {rate:P2} over {total} rolls — the die is not rolling the law.");

            measured.Add((fill, rate, leaks, total));
        }

        // MONOTONE IN DENSITY, measured. An echoing hall leaks less than a roaring one — the owner's own
        // "which is its own tactical choice about WHEN to meet" — and it is asserted on the COUNTS rather
        // than on the formula, because the formula being monotone proves nothing about the game.
        var byFill = measured.OrderBy(m => m.Fill).ToList();
        for (int i = 1; i < byFill.Count; i++)
        {
            Assert.True(byFill[i].Rate > byFill[i - 1].Rate,
                $"a fuller hall did not leak more: fill {byFill[i - 1].Fill:F2} produced " +
                $"{byFill[i - 1].Leaks}/{byFill[i - 1].Total} and fill {byFill[i].Fill:F2} produced " +
                $"{byFill[i].Leaks}/{byFill[i].Total}. The whole tactical question of WHEN to take the " +
                "meeting is this ordering.");
        }

        // …and the spread is worth having. A law whose loudest band leaked barely more than its quietest
        // would be monotone and pointless.
        Assert.True(byFill[^1].Rate > byFill[0].Rate * 2.0,
            $"the roaring hall ({byFill[^1].Rate:P2}) is not meaningfully worse than the echoing one " +
            $"({byFill[0].Rate:P2}) — there is no choice in the mechanic.");
    }

    // ── (b) THE DOOR ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #758 · A DOGGED DOOR IS ABSOLUTE — zero leaks, on the same sweep, at every density.
    ///
    /// <para>The card's own sentence, made true: <i>"a door padded like a vault that dogs shut from
    /// inside."</i> Not a very small number — zero, over every seed the curtain was measured on, which is why
    /// <see cref="CabinetPrivacy.Leaks"/> answers the stage before any die exists.</para>
    ///
    /// <para><b>The RED case.</b> Delete the stage early-out and let the door fall through to the roll: this
    /// fires immediately with the same counts the curtain produced.</para>
    /// </summary>
    [Fact]
    public void THE_DOOR_NeverLeaks_OnTheSameSweepTheCurtainDoes()
    {
        int swept = 0;
        foreach (int band in EveryBand())
        {
            (int leaks, int total) = Sweep(band, CabinetPrivacy.Stage.Door);
            swept += total;

            Assert.True(leaks == 0,
                $"a dogged cabinet leaked {leaks} of {total} beats at fill {SittingAlone.Fill(band):F2}. " +
                "Stage two is the card's promise and the whole price of being seen to shut a door; a " +
                "cabinet that leaks anyway is a player paying for nothing.");

            // …and the curtain DID leak on exactly these seeds, which is what stops the assertion above
            // from passing on a build where Leaks() has been broken to return false for everybody.
            (int clothLeaks, _) = Sweep(band, CabinetPrivacy.Stage.Curtain);
            Assert.True(clothLeaks > 0,
                $"the curtain leaked nothing either at fill {SittingAlone.Fill(band):F2} — this guard is " +
                "measuring a dead law rather than a shut door.");
        }

        Assert.True(swept > 20_000,
            $"the door was only asked {swept} times — too small a sweep to be evidence of never.");
    }

    // ── (c) THE COUNTER'S ONE FACT ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #758 · THE KEEP WRITES IT DOWN WHEN THE DOOR SHUTS, AND ON NO OTHER TRANSITION.
    ///
    /// <para>All four transitions are asked, because "records once" is a claim about a table and not about a
    /// call site. Letting the leaf back open writes nothing — the memorable thing already happened — and a
    /// curtain writes nothing at all, which is the whole reason stage one is worth having.</para>
    ///
    /// <para><b>The RED case.</b> Make <see cref="CabinetPrivacy.TheCounterWritesItDown"/> compare the two
    /// stages for inequality and the door-to-curtain row fires; make it return <c>to == Stage.Door</c> and
    /// the door-to-door row fires.</para>
    /// </summary>
    [Fact]
    public void THE_COUNTER_WritesItDownOnTheWayShut_AndNeverForTheCurtain()
    {
        Assert.True(CabinetPrivacy.TheCounterWritesItDown(
            CabinetPrivacy.Stage.Curtain, CabinetPrivacy.Stage.Door),
            "the hall watched a cabinet door close and the counter did not write anything down — the cost " +
            "of stage two IS the line in the book.");

        Assert.False(CabinetPrivacy.TheCounterWritesItDown(
            CabinetPrivacy.Stage.Door, CabinetPrivacy.Stage.Curtain),
            "letting the leaf back open wrote a second line. A book does not un-write, and it does not " +
            "write the same evening down twice.");

        Assert.False(CabinetPrivacy.TheCounterWritesItDown(
            CabinetPrivacy.Stage.Curtain, CabinetPrivacy.Stage.Curtain),
            "drawing a curtain was written down. Nobody out there can tell it happened, which is the whole " +
            "of stage one.");

        Assert.False(CabinetPrivacy.TheCounterWritesItDown(
            CabinetPrivacy.Stage.Door, CabinetPrivacy.Stage.Door),
            "a door that was already shut was written down again.");

        // The fact itself names the room, on every watch, because that is what makes the bark that knows too
        // much land.
        foreach (int band in EveryBand())
        {
            for (int cabinet = 1; cabinet <= UndergroundComplex.CabinetsPerHall; cabinet++)
            {
                string note = CabinetPrivacy.WhoWasInsideNote(cabinet, band);
                Assert.Contains(
                    cabinet.ToString(CultureInfo.InvariantCulture), note, StringComparison.Ordinal);
                Assert.NotEqual(CabinetPrivacy.WhoWasInsideNote(cabinet + 1, band), note);
            }
        }
    }

    /// <summary>
    /// #917 · THE LINE IS WRITTEN ON BOTH WATCHES, AND ONLY ONE OF THEM HAS A MAN IN IT.
    ///
    /// <para>The keep tends this bar on the living watches only (<see cref="Interior.TheKeep.KeptWatch"/>),
    /// so the sentence that had him looking up named somebody who is not there on two watches in six — a
    /// sentence and a sim disagreeing about a PERSON, which is the third named bug class one room over from
    /// where this feature already found it about a leaf.</para>
    ///
    /// <para><b>The fact does not fork; only the words do.</b> A door coming out of a wall goes in the
    /// counter's book whether or not anybody was standing at the till, and the dead-watch line says exactly
    /// that and declines to say who wrote it. Both readings name the cabinet, so the bark that knows too much
    /// still lands either way.</para>
    ///
    /// <para><b>Anti-vacuous:</b> both watches are required to be REACHED over the rota the game has — a
    /// building where the keep was always there, or never, would make this guard a test of one string.</para>
    ///
    /// <para><b>The RED case.</b> Take the <c>Interior.TheKeep.KeptWatch(watch)</c> fork out of
    /// <see cref="CabinetPrivacy.WhoWasInsideNote"/> and return the kept line always: the dead watches fail
    /// on the man who is not behind the counter, and the two-readings assertion fails with one.</para>
    /// </summary>
    [Fact]
    public void THE_COUNTERS_LINE_NamesTheKeepOnlyOnTheWatchesHeIsBehindIt()
    {
        var living = new HashSet<string>(StringComparer.Ordinal);
        var dead = new HashSet<string>(StringComparer.Ordinal);

        foreach (int band in EveryBand())
        {
            string note = CabinetPrivacy.WhoWasInsideNote(1, band);

            // The fact is filed on every watch there is. It is a ledger, not a witness statement.
            Assert.StartsWith("Dogged the door of cabinet 1 from inside.", note, StringComparison.Ordinal);

            if (Interior.TheKeep.KeptWatch(band))
            {
                living.Add(note);
                Assert.Contains("The keep behind the counter looked up", note, StringComparison.Ordinal);
            }
            else
            {
                dead.Add(note);
                Assert.DoesNotContain("The keep behind the counter", note, StringComparison.Ordinal);
                Assert.Contains("nobody behind the counter to look up", note, StringComparison.Ordinal);
                Assert.Contains("It was written down anyway.", note, StringComparison.Ordinal);
            }
        }

        Assert.True(living.Count == 1 && dead.Count == 1,
            $"the counter's line has {living.Count} living reading(s) and {dead.Count} dead one(s) across "
            + "the rota — both must be REACHED, and each must be one sentence, or this guard is measuring a "
            + "string that never varies (or one that varies for the wrong reason).");

        // …and the whole of what changed is who is described. The room is named identically either way, so
        // a captain re-reading the book on any watch can still tell which door it was.
        Assert.NotEqual(living.Single(), dead.Single());
        for (int cabinet = 1; cabinet <= UndergroundComplex.CabinetsPerHall; cabinet++)
        {
            foreach (int band in EveryBand())
            {
                Assert.Contains(
                    $"cabinet {cabinet.ToString(CultureInfo.InvariantCulture)} from inside",
                    CabinetPrivacy.WhoWasInsideNote(cabinet, band), StringComparison.Ordinal);
            }
        }

        // The rota really does have both kinds of watch — asked of the hall's own numbers rather than of
        // this file's opinion of them.
        int kept = EveryBand().Count(b => Interior.TheKeep.KeptWatch(b));
        Assert.True(kept > 0 && kept < CanteenRegulars.WatchFill.Count,
            $"{kept} of {CanteenRegulars.WatchFill.Count} watches are kept — one of the two readings above "
            + "is unreachable and the assertions about it are asserting nothing.");
    }

    // ── (e) WHO BRINGS YOU, AND WHAT THEY DO WITH THE DOOR ────────────────────────────────────────────

    /// <summary>
    /// #758 · THE ESCORT'S CHOICE IS A FACT ABOUT THE PERSON, AND BOTH ANSWERS EXIST.
    ///
    /// <para>Owner's design: <i>"NPCs who bring you choose their own stage, and WHICH they choose
    /// characterizes them for free."</i> So: the same person always decides the same way (it is who they
    /// are, not what evening it is), different people decide differently, and across the cast the game
    /// actually has, BOTH stages occur. A chooser that answered one way for everybody would be
    /// characterisation that characterises nothing.</para>
    ///
    /// <para><b>The RED case.</b> Return <c>Stage.Door</c> unconditionally and the both-occur assertion
    /// fires; seed it on a caller-supplied watch instead of the plate and the stability assertion fires.</para>
    /// </summary>
    [Fact]
    public void THE_ESCORT_ChoosesTheSameWayEveryTime_AndNotEverybodyChoosesTheSame()
    {
        var cast = new List<string>(CanteenRegulars.StrangerPlates)
        {
            CanteenTable.HandPlate,
            CanteenTable.FitterPlate,
            CanteenTable.TempPlate,
        };

        var seen = new HashSet<CabinetPrivacy.Stage>();
        foreach (string who in cast)
        {
            CabinetPrivacy.Stage first = CabinetPrivacy.EscortsStage(who);
            seen.Add(first);

            // Stable, forever. The fitter who dogs the door dogs it every single time, which is the whole
            // of what makes it characterisation rather than a coin flip with a face on it.
            for (int again = 0; again < 8; again++)
            {
                Assert.Equal(first, CabinetPrivacy.EscortsStage(who));
            }
        }

        Assert.True(seen.Count == 2,
            "every person in the building makes the same choice about the door — the one thing this was " +
            "for was telling them apart. Saw: " + string.Join(", ", seen));

        // …and it is genuinely per-person and not per-first-letter or some other accident: at least two of
        // the cast go each way.
        int door = cast.Count(w => CabinetPrivacy.EscortsStage(w) == CabinetPrivacy.Stage.Door);
        Assert.True(door >= 2 && door <= cast.Count - 2,
            $"{door} of {cast.Count} escorts dog the door — a split that lopsided is one answer with an " +
            "exception rather than two kinds of person.");
    }

    // ── THE GLYPH, THE PLATE, AND THE PROSE ───────────────────────────────────────────────────────────

    /// <summary>
    /// #758 · THE PLAN CAN TELL THE TWO APART, AND NEITHER OF THEM EXPLAINS ITSELF.
    ///
    /// <para>A glyph and never a sentence — the owner's own rule for the deck. The cabinet's own plate is
    /// carried through untouched, because the room did not change its name when somebody pulled a curtain,
    /// and the two marks must actually differ or the plan is publishing one state twice.</para>
    /// </summary>
    [Fact]
    public void THE_PLATE_CarriesOneGlyphThatSaysWhichStage()
    {
        Assert.NotEqual(
            CabinetPrivacy.GlyphFor(CabinetPrivacy.Stage.Curtain),
            CabinetPrivacy.GlyphFor(CabinetPrivacy.Stage.Door));

        for (int number = 1; number <= UndergroundComplex.CabinetsPerHall; number++)
        {
            string bare = UndergroundComplex.CabinetPlate(number);
            string cloth = CabinetPrivacy.PlateFor(bare, CabinetPrivacy.Stage.Curtain);
            string leaf = CabinetPrivacy.PlateFor(bare, CabinetPrivacy.Stage.Door);

            Assert.StartsWith(bare, cloth, StringComparison.Ordinal);
            Assert.StartsWith(bare, leaf, StringComparison.Ordinal);
            Assert.NotEqual(cloth, leaf);

            // A GLYPH AND NOT PROSE: whatever the stage added is one mark long, so a row of doors reads at
            // a glance rather than becoming three sentences down a wall.
            Assert.True(cloth.Length - bare.Length <= 2,
                $"the curtain added prose to a deck plate: \"{cloth}\".");
            Assert.True(leaf.Length - bare.Length <= 2,
                $"the door added prose to a deck plate: \"{leaf}\".");
        }
    }

    /// <summary>#758 · Every sentence this feature can put on a screen, held to §13.8: nothing down here
    /// says what the facility is for, and nothing states the mechanic it is a consequence of.</summary>
    [Fact]
    public void NOTHING_THE_CURTAIN_SAYS_ExplainsAnything()
    {
        string[] forbidden =
        [
            "chance", "probability", "roll", "odds", "percent", "%",
            "leak", "overheard", "eavesdrop", "detected", "stealth",
        ];

        var prose = CabinetPrivacy.AllProse().ToList();
        Assert.True(prose.Count >= 8, "the prose sweep walks fewer lines than the feature has.");

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line), "an empty line reached the sweep.");
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // The two strip labels are the verbs, and they are distinct — one button that said the same thing
        // in both stages would be a button that never tells you what it will do.
        Assert.NotEqual(
            CabinetPrivacy.LabelFor(CabinetPrivacy.Stage.Curtain),
            CabinetPrivacy.LabelFor(CabinetPrivacy.Stage.Door));
        Assert.NotEqual(
            CabinetPrivacy.HintFor(CabinetPrivacy.Stage.Curtain),
            CabinetPrivacy.HintFor(CabinetPrivacy.Stage.Door));
        Assert.NotEqual(
            CabinetPrivacy.SaidOn(CabinetPrivacy.Stage.Curtain),
            CabinetPrivacy.SaidOn(CabinetPrivacy.Stage.Door));
    }

    /// <summary>#758 · Two floors of one building may never share a leaf, and neither may two cabinets on
    /// one floor. The same lesson <c>HiveInterior.CubicleKey</c> is keyed on, asked here because the set the
    /// deck is drawn from is keyed with this.</summary>
    [Fact]
    public void EVERY_CABINET_HasItsOwnKey()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int level = -1; level >= -20; level--)
        {
            for (int cabinet = 1; cabinet <= UndergroundComplex.CabinetsPerHall; cabinet++)
            {
                Assert.True(keys.Add(CabinetPrivacy.Key(level, cabinet)),
                    $"B{-level} cabinet {cabinet} shares a key with another leaf in this building.");
            }
        }
    }
}
