using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #746 · <b>THE GUARD STOP IS AN ENCOUNTER — the named remainder, and the claim #748 made cashed.</b>
///
/// <para>#748 shipped the machine with a sentence in its own docblock: <i>"the guard stop is not a second
/// system, it is an encounter whose setting walked up to you"</i>, and discharged it with a SAMPLE
/// checkpoint that type-checked and was never played. These guards are about the real one, and the thing
/// every single one of them is watching for is the same: <b>a second answer to a question the game already
/// has one answer to.</b></para>
///
/// <para>Every guard below was watched RED by reverting the thing it guards; the revert is named on it.</para>
/// </summary>
public sealed class TheGuardStopIsAnEncounterTests
{
    private const string Site = "luna";
    private const string Plate = "◈ PATROL 1";

    /// <summary>A floor a department owns, on the shipped ground this file uses — asked of the generator
    /// rather than typed, because a test that names a floor number is a test that goes quietly green the day
    /// the plate stock cycles differently.</summary>
    private static int ADepartmentsFloorOn(string bodyId)
    {
        for (int level = -1; level >= -20; level--)
        {
            if (PatrolBeat.IsPatrolled(bodyId, level) && !PatrolBeat.GeneralHandsBelongOn(bodyId, level))
            {
                return level;
            }
        }
        throw new InvalidOperationException($"{bodyId} has no department floor with a round on it.");
    }

    /// <summary>…and one a general hand belongs on, the same way.</summary>
    private static int AHandsFloorOn(string bodyId)
    {
        for (int level = -1; level >= -20; level--)
        {
            if (PatrolBeat.IsPatrolled(bodyId, level) && PatrolBeat.GeneralHandsBelongOn(bodyId, level))
            {
                return level;
            }
        }
        throw new InvalidOperationException($"{bodyId} has no hand's floor with a round on it.");
    }

    // ── THE SCENE IS CONTENT, ON THE SHIPPED TYPE ───────────────────────────────────────────────────────

    /// <summary>
    /// <b>IT IS A <see cref="Encounter.Scene"/> AND NOTHING ELSE.</b> Four moves, one of them the scene's
    /// own way out, on the type a canteen table is written on — which is the whole of #746's claim and the
    /// only way to hold it: if anybody ever makes <c>Encounter</c> canteen-shaped, this stops compiling.
    ///
    /// <para>The labels and the opening are Fable's and are pinned VERBATIM, because a line improved by
    /// somebody passing through is an improvement nobody is ever shown.</para>
    ///
    /// <para><b>RED</b> by retyping the opening as <c>"Pass?"</c>: <i>Assert.Equal() Failure: Strings
    /// differ</i>. <b>RED</b> by giving the exit move an id of its own (<c>"stop:nothing"</c>):
    /// <i>Encounter.CanAlwaysLeave — Expected True, Actual False</i>.</para>
    /// </summary>
    [Fact]
    public void TheStopIsAScene_WithAnOpeningAWayOutAndFourMoves()
    {
        Encounter.Scene scene = GuardStop.SceneFor(Plate);

        Assert.Equal(Plate, scene.Counterpart);
        Assert.Equal("a checkpoint that walked up to you", scene.Setting);
        Assert.Equal("Pass.", scene.Opening);
        Assert.Equal(4, scene.Moves.Count);

        Assert.Equal(
            new[] { "SHOW THE PASS", "\"I'M NEW. WHICH WAY IS THE MESS?\"", "TALK ABOUT THE WORK", "SAY NOTHING" },
            scene.Moves.Select(m => m.Label).ToArray());

        // The way out is the framework's own id, which is what makes the card's ✕ a DECISION rather than a
        // dodge — and it is free to make, as every scene's exit must be.
        Assert.True(Encounter.CanAlwaysLeave(scene));
        Assert.Equal(Encounter.Leave, GuardStop.Nothing);

        // Nothing in this scene is a door the framework cannot see: every move that exists is drawn.
        Assert.Equal(scene.Moves.Count, Encounter.OnTheTable(scene, []).Count);

        // Two are rolled and two are fixed, and that is a design statement rather than an unfinished roll:
        // what a man makes of a pass is a LADDER, and standing there saying nothing is not a check.
        Assert.Equal(
            new[] { GuardStop.Mess, GuardStop.Work },
            scene.Moves.Where(m => m.Rolled).Select(m => m.Id).ToArray());
    }

    // ── THE FIXED MOVES ARE THE LADDER, AND THERE IS ONE TRUTH ──────────────────────────────────────────

    /// <summary>
    /// <b>SHOWING THE PASS IS <see cref="PatrolBeat.TheGuardReads"/>, TO THE BYTE.</b>
    ///
    /// <para>Every rung of the shipped ladder, walked with a real wallet on a real floor: the answer this
    /// lane composes carries the SAME sentence, the SAME label, the SAME card and the SAME consequence as
    /// the read the challenge has raised since #804 — on every arm where cover blows. The only difference
    /// anywhere is the arm where it holds, where the encounter adds its own resolution under the read, and
    /// this guard pins that the read itself is still untouched under it.</para>
    ///
    /// <para><b>RED</b> by composing the wrong-department arm here instead of handing it the ladder
    /// (<c>new PatrolBeat.Read(false, PatrolBeat.ReadsItTwiceLine, …)</c> with no consequence): <i>the
    /// refused arm dropped the escort</i>. <b>RED</b> by making the satisfied arm say only the new line:
    /// <i>the held arm no longer contains "Right. Keep to the lit side."</i></para>
    /// </summary>
    [Fact]
    public void ShowingThePassIsTheShippedLadderAndNeverASecondOpinion()
    {
        int department = ADepartmentsFloorOn(Site);
        int hands = AHandsFloorOn(Site);
        string tier = ChamberFitting.DepartmentOn(Site, department)!;

        (string What, int Level, Satchel.Item? Shown)[] rungs =
        [
            ("nothing in the wallet", department, null),
            ("this site's general pass on a department floor", department, PatrolBeat.Badge(Site)),
            ("this site's general pass on a hand's floor", hands, PatrolBeat.Badge(Site)),
            ("this site's department pass on its own floor", department, PatrolBeat.Badge(Site, tier)),
            ("another site's pass", department, PatrolBeat.Badge("titan")),
            ("the cage chit", department, CanteenTable.Chit(underAnotherName: false)),
        ];

        var blew = 0;
        var held = 0;
        foreach ((string what, int level, Satchel.Item? shown) in rungs)
        {
            PatrolBeat.Read ladder = PatrolBeat.TheGuardReads(
                Site, level, 3L, Plate, shown, inspectionRunning: false);
            GuardStop.Answer answer = GuardStop.ThePaperGoesIntoHisHand(ladder);

            // The verdict is the ladder's, always: one predicate partitions both.
            Assert.Equal(ladder.Satisfied, answer.Read.Satisfied);
            Assert.Equal(ladder.Line, answer.Read.Line);
            Assert.Equal(ladder.Label, answer.Read.Label);
            Assert.Equal(ladder.Card, answer.Read.Card);
            Assert.False(answer.NameGoesInTheBook, $"{what} cost a rung of heat and nothing said so.");

            if (ladder.Satisfied)
            {
                held++;
                Assert.Equal(GuardStop.Ending.HeWalksOn, answer.How);

                // The read is STILL the read — the canon sentence is in it — and the new line is the thing
                // he does after it, never instead of it.
                Assert.Contains(ladder.Line, answer.Read.Told, StringComparison.Ordinal);
                Assert.Contains(GuardStop.StepsAsideLine, answer.Read.Told, StringComparison.Ordinal);
            }
            else
            {
                blew++;
                Assert.Equal(GuardStop.Ending.HeWalksYouOut, answer.How);

                // BYTE FOR BYTE on every arm that costs anything, consequence included.
                Assert.Equal(ladder.Consequence, answer.Read.Consequence);
                Assert.Equal(ladder.Told, answer.Read.Told);
                Assert.Equal(PatrolBeat.EscortLine, answer.Read.Consequence);
            }
        }

        // …and the world this walked can tell a pass from a refusal, which is what keeps the sweep above
        // from being green on a world where everything lands the same way.
        Assert.True(held >= 2, $"only {held} of the six rungs held — this sweep proved nothing.");
        Assert.True(blew >= 3, $"only {blew} of the six rungs blew — this sweep proved nothing.");
    }

    /// <summary>
    /// <b>SAYING NOTHING IS THE EMPTY-HAND RUNG, TO THE BYTE.</b> It is not a fifth thing that can happen at
    /// a checkpoint: <see cref="PatrolBeat.NothingLine"/> has described a man waiting the entire time you are
    /// looking since #804, and standing there with your hands in your pockets is exactly what it describes.
    ///
    /// <para><b>RED</b> by giving the move a <c>Says</c> of its own and answering off that: <i>the answer's
    /// line is not the ladder's</i>.</para>
    /// </summary>
    [Fact]
    public void SayingNothingIsTheLaddersOwnEmptyHandAndCostsWhatItHasAlwaysCost()
    {
        int level = ADepartmentsFloorOn(Site);
        PatrolBeat.Read ladder = PatrolBeat.TheGuardReads(
            Site, level, 3L, Plate, null, inspectionRunning: false);
        GuardStop.Answer answer = GuardStop.NothingIsSaid(ladder);

        Assert.Equal(ladder, answer.Read);
        Assert.Equal(PatrolBeat.NothingLine, answer.Read.Line);
        Assert.Equal(PatrolBeat.EscortLine, answer.Read.Consequence);
        Assert.Equal(GuardStop.Ending.HeWalksYouOut, answer.How);
        Assert.False(answer.NameGoesInTheBook);

        // And the move carries no sentence of its own for anybody to reach for.
        Encounter.Move exit = GuardStop.SceneFor(Plate).Moves.Single(m => m.Id == GuardStop.Nothing);
        Assert.Null(exit.Says);
        Assert.Null(exit.Note);
    }

    // ── THE THREE BANDS, AND EVERY ONE OF THEM REACHES A SEAM THAT WAS ALREADY HERE ──────────────────────

    /// <summary>
    /// <b>EACH BAND GRANTS ITS OWN STATE, AND NO BAND INVENTS A PUNISHMENT.</b>
    ///
    /// <para>The whole surface of what a rolled move can do to a captain, enumerated: three endings, two
    /// sentences, one rung of heat. Every one of them is machinery that was in the game before this lane —
    /// the escort (#833/#804), #715's meter, and nothing at all — which is #605's own constraint one file
    /// along: <i>what a failed conversation costs is what a refusal already cost.</i></para>
    ///
    /// <para>YES and YES-BUT are the same sentence and differ in exactly one field, which is what makes the
    /// middle band a COST rather than a different scene; NO-AND is the escort in the owner's own canon copy,
    /// which opens with the word <i>"No?"</i> and is therefore already an answer to an ask.</para>
    ///
    /// <para><b>RED</b> by dropping <c>NameGoesInTheBook</c> from the YES-BUT arm of the ask: <i>YES and
    /// YES-BUT of stop:mess are the same answer — the middle band costs nothing</i>. <b>RED</b> by giving
    /// NO-AND a consequence of its own (a second pip): <i>a band reached a consequence that is not the
    /// escort</i>.</para>
    /// </summary>
    [Fact]
    public void TheThreeBandsEachGrantTheirOwnStateAndNoneOfThemIsNewPunishment()
    {
        foreach (string moveId in new[] { GuardStop.Mess, GuardStop.Work })
        {
            var byBand = new Dictionary<Encounter.Band, GuardStop.Answer>();
            foreach (Encounter.Band band in Enum.GetValues<Encounter.Band>())
            {
                byBand[band] = moveId == GuardStop.Mess
                    ? GuardStop.TheWayToTheMess(band, Plate)
                    : GuardStop.TheWork(band, Plate);
            }

            GuardStop.Answer yes = byBand[Encounter.Band.Yes];
            GuardStop.Answer but = byBand[Encounter.Band.YesBut];
            GuardStop.Answer no = byBand[Encounter.Band.NoAnd];

            // It LANDS on the two upper bands, and the same way — the sentence is the move's, not the band's.
            Assert.Equal(yes.Read, but.Read);
            Assert.Equal(yes.How, but.How);
            Assert.NotEqual(yes, but);
            Assert.False(yes.NameGoesInTheBook);
            Assert.True(
                but.NameGoesInTheBook,
                $"YES and YES-BUT of {moveId} are the same answer — the middle band costs nothing.");

            // …AND THE SCENE MOVES. Never a dead wall: the refusal ends in the walk, in the shipped words.
            Assert.Equal(GuardStop.Ending.HeWalksYouOut, no.How);
            Assert.Equal(PatrolBeat.EscortLine, no.Read.Line);
            Assert.False(no.Read.Satisfied);
            Assert.False(no.NameGoesInTheBook);

            // Every card any band raises is the round's own: one label, one painting, one body, whatever
            // just happened (#804's plate rule — the man in the picture has not decided anything).
            foreach (GuardStop.Answer a in byBand.Values)
            {
                Assert.Equal(PatrolBeat.ChallengeLabel, a.Read.Label);
                Assert.Equal(PatrolBeat.ChallengeCard(Plate), a.Read.Card);
            }
        }

        // THE WHOLE SURFACE, and it is bounded. Every ending and every consequence any move of this scene
        // can produce, gathered — no fourth ending, and no consequence that is not the shipped escort.
        var endings = new HashSet<GuardStop.Ending>();
        var consequences = new HashSet<string>(StringComparer.Ordinal);
        int level = ADepartmentsFloorOn(Site);
        foreach (Encounter.Band band in Enum.GetValues<Encounter.Band>())
        {
            foreach (GuardStop.Answer a in new[]
                     {
                         GuardStop.TheWayToTheMess(band, Plate),
                         GuardStop.TheWork(band, Plate),
                     })
            {
                endings.Add(a.How);
                consequences.Add(a.Read.Consequence ?? "<nothing>");
            }
        }
        foreach ((int floor, Satchel.Item? shown) in new (int, Satchel.Item?)[]
                 {
                     (level, null),                                    // an empty hand: the escort
                     (level, PatrolBeat.Badge(Site)),                  // a hand on a department floor: the escort
                     (AHandsFloorOn(Site), PatrolBeat.Badge(Site)),    // …and where it holds
                 })
        {
            GuardStop.Answer a = GuardStop.ThePaperGoesIntoHisHand(
                PatrolBeat.TheGuardReads(Site, floor, 3L, Plate, shown, inspectionRunning: false));
            endings.Add(a.How);
            consequences.Add(a.Read.Consequence ?? "<nothing>");
        }

        Assert.Equal(
            new HashSet<GuardStop.Ending>(Enum.GetValues<GuardStop.Ending>()), endings);
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "<nothing>", PatrolBeat.EscortLine, GuardStop.StepsAsideLine,
            },
            consequences);
    }

    /// <summary>
    /// <b>ONE RUNG, AND IT IS THE CHEAPEST ONE IN THE GAME.</b> The YES-BUT's cost is #715's own meter
    /// through #715's own seam, weighted off the number the gate has published since #929 rather than a
    /// second copy of it — so one of these can never move a band on its own and the day the cheapest rung is
    /// tuned there is one number to change.
    ///
    /// <para><b>RED</b> by weighting it like the escort (<c>=&gt; 2</c>): <i>a name in a book costs more
    /// than a parcel found aboard</i>.</para>
    /// </summary>
    [Fact]
    public void TheNameInTheBookIsTheCheapestRungAndIsOwedToTheOutfit()
    {
        Assert.Equal(
            UndergroundComplex.RefusedCardHeat,
            IllegalHeat.WeightOf(IllegalHeat.Crossing.YourNameInTheirBook));

        // Cheaper than the walk it is an alternative to, which is the whole of what the middle band means.
        Assert.True(
            IllegalHeat.WeightOf(IllegalHeat.Crossing.YourNameInTheirBook)
            < IllegalHeat.WeightOf(IllegalHeat.Crossing.TheEscort));

        // …and one of them can never move a band on its own.
        Assert.True(IllegalHeat.WeightOf(IllegalHeat.Crossing.YourNameInTheirBook) < IllegalHeat.HeatPerRung);

        // Owed to the OUTFIT and never to the moon, like every other crossing.
        UndergroundComplex.HeatCharge charge =
            IllegalHeat.Charge(Site, IllegalHeat.Crossing.YourNameInTheirBook);
        Assert.Equal(SiteOperator.Of(Site).Id, charge.OperatorId);
        Assert.NotEqual(Site, charge.OperatorId);
    }

    // ── THE DOORS ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE ASK IS ONCE PER MAN PER WATCH, AND THE WORK NEEDS A NAME ON YOU.</b>
    ///
    /// <para>A relevant paper is a file on somebody, or a pass for a department that is not the one painted
    /// on the wall behind you. Neither the floor's own department pass nor another site's counts, and the
    /// two exclusions are different mistakes: the first is the paper you would SHOW (two moves doing one
    /// job), and the second says nothing about the work on this moon.</para>
    ///
    /// <para><b>RED</b> by dropping the <c>!string.Equals(tier, floorIs)</c> clause: <i>the floor's own
    /// department pass was worth talking about</i>. <b>RED</b> by returning <c>true</c> from the ask's arm
    /// of <c>OnOffer</c>: <i>the way could be asked twice of one man</i>.</para>
    /// </summary>
    [Fact]
    public void TheDoorsAreTheWalletAndTheWatch()
    {
        int level = ADepartmentsFloorOn(Site);
        string floorIs = ChamberFitting.DepartmentOn(Site, level)!;
        string other = UndergroundComplex.DepartmentsFor(Site)
            .First(d => !string.Equals(d, floorIs, StringComparison.Ordinal));

        // SHOW THE PASS wants something a palm is for, and the chit counts — it is a rung of the ladder.
        Assert.False(GuardStop.OnOffer(GuardStop.Show, Site, level, [], false));
        Assert.False(
            GuardStop.OnOffer(
                GuardStop.Show, Site, level, [new Satchel.Item(Satchel.Kind.Paper, "a leaflet")], false));
        Assert.True(GuardStop.OnOffer(GuardStop.Show, Site, level, [CanteenTable.Chit(underAnotherName: false)], false));
        Assert.True(GuardStop.OnOffer(GuardStop.Show, Site, level, [PatrolBeat.Badge(Site)], false));

        // THE ASK: free, and once.
        Assert.True(GuardStop.OnOffer(GuardStop.Mess, Site, level, [], false));
        Assert.False(GuardStop.OnOffer(GuardStop.Mess, Site, level, [], true));

        // SAY NOTHING is always there. A stop you could be stuck in is the one pop-up this game may not have.
        Assert.True(GuardStop.OnOffer(GuardStop.Nothing, Site, level, [], true));

        // THE WORK: a name on you, and only a name.
        Assert.False(GuardStop.OnOffer(GuardStop.Work, Site, level, [], false));
        Assert.False(
            GuardStop.OnOffer(GuardStop.Work, Site, level, [PatrolBeat.Badge(Site)], false),
            "the general pass was worth talking about — it is not a department.");
        Assert.False(
            GuardStop.OnOffer(GuardStop.Work, Site, level, [PatrolBeat.Badge(Site, floorIs)], false),
            "the floor's own department pass was worth talking about — that is the paper you SHOW.");
        Assert.False(
            GuardStop.OnOffer(GuardStop.Work, Site, level, [PatrolBeat.Badge("titan", floorIs)], false),
            "a pass from another moon was worth talking about on this one.");
        Assert.True(
            GuardStop.OnOffer(GuardStop.Work, Site, level, [PatrolBeat.Badge(Site, other)], false));
        Assert.True(
            GuardStop.OnOffer(
                GuardStop.Work, Site, level, [new Satchel.Item(Satchel.Kind.Dirt, "a file on somebody")],
                false));

        // A refused control says why (#603), and none of the four is silent about it.
        foreach (Encounter.Move m in GuardStop.SceneFor(Plate).Moves)
        {
            Assert.False(string.IsNullOrWhiteSpace(GuardStop.WhyNot(m.Id)));
        }
    }

    // ── THE DICE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY BAND IS REACHABLE ON REAL DICE AT A CHECKPOINT, AND THE MODIFIERS MOVE THE ODDS.</b> The
    /// fifth bug class, paid up front: a threshold no real roll can reach is a guard that asserts nothing,
    /// and a stack that could never be true is a receipt for something that did not happen.
    ///
    /// <para>400 real <see cref="Encounter.Roll"/>s per move, over the seed this lane actually uses —
    /// (site, floor, plate, move, attempt) — with the naked situation and again with the paper. The middle
    /// band must not be vestigial, and the paper must actually help.</para>
    ///
    /// <para><b>RED</b> by seeding every stop on the same plate (dropping the counterpart from the seed):
    /// <i>400 rolls produced 1 distinct total</i>.</para>
    /// </summary>
    [Fact]
    public void EveryBandComesUpOnRealDiceAndThePaperMovesTheOdds()
    {
        foreach (string moveId in new[] { GuardStop.Mess, GuardStop.Work })
        {
            var naked = new Dictionary<Encounter.Band, int>();
            var withPaper = new Dictionary<Encounter.Band, int>();

            for (int i = 0; i < 400; i++)
            {
                string plate = $"◈ PATROL {i}";
                foreach ((Dictionary<Encounter.Band, int> tally, bool paper) in
                         new[] { (naked, false), (withPaper, true) })
                {
                    Encounter.Situation s = GuardStop.SituationAt(paper, nerveMarked: false, fumbled: false);
                    Encounter.Band band = Encounter.BandOf(
                        Encounter.Roll(Site, -2, plate, moveId, 0, s).Total);
                    tally[band] = tally.GetValueOrDefault(band) + 1;
                }
            }

            foreach (Encounter.Band band in Enum.GetValues<Encounter.Band>())
            {
                Assert.True(
                    naked.GetValueOrDefault(band) > 20,
                    $"{moveId}: {band} came up {naked.GetValueOrDefault(band)} times in 400 unmodified "
                    + "rolls — the band is vestigial or unreachable.");
            }

            Assert.True(
                withPaper[Encounter.Band.Yes] > naked[Encounter.Band.Yes],
                $"{moveId}: the paper on you did not move the odds ("
                + $"{withPaper[Encounter.Band.Yes]} vs {naked[Encounter.Band.Yes]} landings in 400).");
        }

        // …and the modifier stack a checkpoint builds is the named one, with the two that could never be
        // true left OUT rather than passed as false into a receipt.
        IReadOnlyList<DiceModifier> stack = Encounter.Modifiers(
            GuardStop.SituationAt(relevantPaper: true, nerveMarked: true, fumbled: true));
        Assert.Equal(
            new[] { Encounter.PaperLabel, Encounter.NerveLabel, Encounter.FumbledLabel },
            stack.Select(m => m.Label).ToArray());
        Assert.Empty(Encounter.Modifiers(GuardStop.SituationAt(false, false, false)));
    }

    /// <summary>
    /// <b><c>?roll=</c> REACHES THIS SCENE, AND IT OVERRIDES THE BAND AND NEVER THE ROLL.</b> A scene nobody
    /// can reach on demand is a scene that ships broken (#693's rule, which #746 wrote into its own issue as
    /// <i>testing is a feature</i>) — so all four outcomes of every rolled move have to be reachable by
    /// forcing, with the dice still cast and the math still truthful.
    ///
    /// <para><b>RED</b> by having <c>Settle</c> ignore the forced band.</para>
    /// </summary>
    [Fact]
    public void ForcingTheBandReachesEveryOutcomeOfEveryRolledMove()
    {
        DiceRoll cast = Encounter.Roll(
            Site, -2, Plate, GuardStop.Mess, 0, GuardStop.SituationAt(false, false, false));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Encounter.Band forced in Enum.GetValues<Encounter.Band>())
        {
            Assert.Equal(forced, Encounter.Settle(cast, forced));
            seen.Add($"{GuardStop.TheWayToTheMess(forced, Plate)}");
            seen.Add($"{GuardStop.TheWork(forced, Plate)}");
        }

        // FIVE distinct outcomes across the two rolled moves on one cast die, and the fifth is missing on
        // purpose: the two asks say different things when they land and the SAME thing when they do not,
        // because a refusal at a checkpoint is the escort and there is only one of those. Two refusal
        // sentences would be this lane writing prose it was not allowed to write.
        Assert.Equal(5, seen.Count);
        Assert.Equal(
            GuardStop.TheWayToTheMess(Encounter.Band.NoAnd, Plate),
            GuardStop.TheWork(Encounter.Band.NoAnd, Plate));

        // The roll itself is untouched by the cheat: the same die, the same total, the same math on screen.
        Assert.Equal(cast.Total, Encounter.Roll(
            Site, -2, Plate, GuardStop.Mess, 0, GuardStop.SituationAt(false, false, false)).Total);
    }

    // ── THE PROSE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE CANON SWEEP, EXTENDED TO THIS SCENE.</b> <see cref="GuardStop.AllProse"/> walks itself, so a
    /// line added tomorrow is swept tomorrow — and the sweep is the round's own: nothing here describes an
    /// injury (owner: <i>"just no damage by default :-D"</i>), nothing is loud, and nothing names the
    /// reserved thing.
    ///
    /// <para><b>RED</b> by dropping <see cref="GuardStop.StepsAsideLine"/> out of the catalog: <i>a sentence
    /// this file owns is not in AllProse</i>.</para>
    /// </summary>
    [Fact]
    public void NothingThisSceneSaysIsLoudInjuriousOrReserved()
    {
        var prose = GuardStop.AllProse().ToList();
        Assert.NotEmpty(prose);

        string[] injuries =
        [
            "blood", "bleed", "bruis", "broke", "broken", "punch", "hit you", "struck", "baton",
            "cuff", "wound", "hurt",
        ];
        foreach (string line in prose)
        {
            foreach (string word in injuries)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
            foreach (string loud in new[] { "klaxon", "siren", "alarm", "!" })
            {
                Assert.DoesNotContain(loud, line, StringComparison.OrdinalIgnoreCase);
            }
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("reever", line, StringComparison.OrdinalIgnoreCase);
        }

        // The catalog is not a list somebody remembers to update: every authored sentence and label this
        // scene can put in front of a captain is in it.
        foreach (string said in new[]
                 {
                     GuardStop.Opening, GuardStop.StepsAsideLine, GuardStop.PointsTheWayLine,
                     GuardStop.NotHisProblemLine, GuardStop.NoPaperLine, GuardStop.AlreadyAskedLine,
                     GuardStop.NothingToTalkAboutLine,
                 })
        {
            Assert.Contains(said, prose);
        }
        foreach (Encounter.Move m in GuardStop.SceneFor(Plate).Moves)
        {
            Assert.Contains(m.Label, prose);
        }

        // And the three lines a band can say are SAID BY THE SIM, never only by a constant — the standard
        // PatrolBeat.AuthoredLines is held to.
        Assert.Contains(
            GuardStop.PointsTheWayLine,
            GuardStop.TheWayToTheMess(Encounter.Band.Yes, Plate).Read.Told,
            StringComparison.Ordinal);
        Assert.Contains(
            GuardStop.NotHisProblemLine,
            GuardStop.TheWork(Encounter.Band.Yes, Plate).Read.Told,
            StringComparison.Ordinal);
        Assert.Contains(
            GuardStop.StepsAsideLine,
            GuardStop.ThePaperGoesIntoHisHand(
                PatrolBeat.TheGuardReads(
                    Site, AHandsFloorOn(Site), 3L, Plate, PatrolBeat.Badge(Site), false)).Read.Told,
            StringComparison.Ordinal);
    }
}
