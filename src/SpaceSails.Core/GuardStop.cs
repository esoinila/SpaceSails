using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #746 · <b>THE GUARD STOP, AS CONTENT ON THE ENCOUNTER MACHINE.</b>
///
/// <para>Owner, 2026-08-06 (work-break brief): <i>"guard stops (show ID, explain yourself) should use the
/// same choose-your-action mechanic."</i> <see cref="Encounter"/> shipped in #748 with that sentence as its
/// whole shape — <i>"the guard stop is not a second system, it is an encounter whose setting walked up to
/// you"</i> — and discharged the claim with a SAMPLE checkpoint that type-checked and was never played. This
/// file is the real one, and it is still no new mechanics: a counterpart, a setting, an opening and four
/// moves, on the type the canteen table is written on.</para>
///
/// <h3>The stop that was already here is the fixed core of it</h3>
///
/// <para><see cref="PatrolBeat.TheGuardReads"/> has been the whole of a challenge since #804: a card, an
/// automatic read of one paper, and a four-rung ladder that #1149 and #605 each added a rung to. <b>None of
/// that moves.</b> <see cref="ThePaperGoesIntoHisHand"/> is one move of this scene and its verdict is that
/// call, handed in — the shipped sentences, the shipped escort, the shipped filed line — because a second
/// opinion about what a man makes of a pass is exactly the bug class this feature has paid for twice. What
/// the encounter adds is the other three things a captain can do while his hand is out.</para>
///
/// <h3>The three that are not the wallet</h3>
///
/// <list type="bullet">
/// <item><b>The ask</b> (<see cref="TheWayToTheMess"/>) — the oldest trick there is, and it is ROLLED. A man
/// who believes you points; a man who half believes you points and writes your name down; a man who does not
/// walks you out. It never leaves you standing there, which is Fail Forward's whole rule.</item>
/// <item><b>The work</b> (<see cref="TheWork"/>) — the satchel as a conversational move, one system along
/// from <c>CanteenTable.PutOnTheTable</c>. A file on somebody, or a pass for a department that is not this
/// floor's, is a thing to talk ABOUT rather than a thing to hand over.</item>
/// <item><b>Saying nothing</b> (<see cref="NothingIsSaid"/>) — which is not a dead wall and is not silence
/// either: it is the ladder's own empty-hand rung, byte for byte, because standing there with your hands in
/// your pockets is precisely what that rung has always described.</item>
/// </list>
///
/// <para>Pure and deterministic like everything else in Core: no clock, no <c>Random</c>, no world. The dice
/// are <see cref="Encounter.Roll"/>'s and the bands are <see cref="Encounter.Band"/>'s, so <c>?roll=hi|lo</c>
/// reaches this scene by reaching every scene.</para>
/// </summary>
public static class GuardStop
{
    // ── THE SCENE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where this happens, in the <see cref="Encounter.Scene.Setting"/>'s own register. It is the
    /// sentence #748 wrote for the sample checkpoint and the one the whole machine was shaped around: the
    /// setting is not a room the captain walked into, it is a man who walked up to them.</summary>
    public const string Setting = "a checkpoint that walked up to you";

    /// <summary>
    /// What he says while you decide — <b>one word, Fable-authored, verbatim</b>.
    ///
    /// <para>It is deliberately not the challenge. <see cref="PatrolBeat.ChallengeCard"/> carries the owner's
    /// own three-clause line (<i>"Hold there. Floor's restricted. Show me something."</i>) at the moment he
    /// arrives, and that is a man explaining himself. This is the second thing he says, after a pause, with
    /// the palm still out — and a man who has done this a hundred times does not explain himself twice.</para>
    /// </summary>
    public const string Opening = "Pass.";

    /// <summary>Hand over whatever the approach put in your hand (#836's <c>WalletChoice</c>). The move the
    /// whole shipped feature used to be.</summary>
    public const string Show = "stop:show";

    /// <summary>The ask. Free, rolled, and once per man per watch.</summary>
    public const string Mess = "stop:mess";

    /// <summary>Talk about the work, with something on you that makes that a sentence rather than a bluff.</summary>
    public const string Work = "stop:work";

    /// <summary>
    /// Say nothing — and it is <see cref="Encounter.Leave"/>'s own id, which is a design statement rather
    /// than a saving.
    ///
    /// <para>Every scene has to offer a way OUT of itself, and at a checkpoint the way out is not a door: you
    /// cannot politely dodge a man with his hand out, and a stop you could close with a ✕ would be the one
    /// pop-up in the game that pays a captain for ignoring it. So the scene's exit move is this one — free to
    /// make, always available, never a dead wall — and the card's ✕ presses it, exactly the way the find
    /// card's ✕ presses LEAVE (#615). What it COSTS is what a refusal has always cost; what it cannot do is
    /// leave the captain standing in front of a card with nothing to press.</para>
    /// </summary>
    public const string Nothing = Encounter.Leave;

    /// <summary>The four buttons, Fable-authored and verbatim. Three are things you DO and one is a thing you
    /// SAY, which is why exactly one of them is in quotation marks.</summary>
    public const string ShowLabel = "SHOW THE PASS";

    /// <inheritdoc cref="ShowLabel"/>
    public const string MessLabel = "\"I'M NEW. WHICH WAY IS THE MESS?\"";

    /// <inheritdoc cref="ShowLabel"/>
    public const string WorkLabel = "TALK ABOUT THE WORK";

    /// <inheritdoc cref="ShowLabel"/>
    public const string NothingLabel = "SAY NOTHING";

    /// <summary>
    /// THE SCENE, off one man's plate. Content and nothing else — no new type, no new requirement, no new
    /// band.
    /// </summary>
    /// <param name="plate">Who stopped you, as the deck draws him (<see cref="PatrolBeat.PlateOf"/>).</param>
    public static Encounter.Scene SceneFor(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return new Encounter.Scene(
            $"guard:stop:{plate}",
            plate,
            Setting,
            Opening,
            [
                // SatchelItem with no KIND named is the framework's own "a gesture, and which something is
                // the next press" (Encounter.Available). At a stop the something was already chosen, during
                // the walk-up, by #836's fan — so the requirement here is only that there is a paper at all,
                // and WHICH paper is not this move's question.
                new Encounter.Move(Show, ShowLabel, Encounter.Requirement.SatchelItem),
                new Encounter.Move(Mess, MessLabel, Rolled: true),
                new Encounter.Move(Work, WorkLabel, Encounter.Requirement.SatchelItem, Rolled: true),
                new Encounter.Move(Nothing, NothingLabel),
            ]);
    }

    // ── WHAT A MOVE NEEDS, AND WHY IT IS REFUSED ──────────────────────────────────────────────────────
    //
    // Two of the four have a door on them, and BOTH doors are questions about the wallet that
    // Encounter.Requirement cannot ask on its own — the same shape the table keeps (Core's requirement, plus
    // the one fact about the room). They are answered HERE rather than in the client for the table's own
    // stated reason: the panel asks questions, it does not compute answers.

    /// <summary>Is there anything in the wallet a palm is for? <see cref="WalletChoice.Fan"/>'s own
    /// question, so the move and the chooser that filled the hand can never disagree about whether the
    /// captain had a paper.</summary>
    public static bool APaperToShow(string bodyId, IReadOnlyList<Satchel.Item>? carried) =>
        WalletChoice.Fan(bodyId, carried).Count > 0;

    /// <summary>
    /// #746 · <b>A RELEVANT PAPER — the modifier's first live customer, and the ask's own door.</b>
    ///
    /// <para>Two things make talking about the work a sentence rather than a bluff, and neither of them is a
    /// thing you would hand a guard:</para>
    ///
    /// <list type="bullet">
    /// <item><b>A file on somebody</b> (<see cref="Satchel.Kind.Dirt"/>) — a name. It is LOUD at a canteen
    /// table (<c>CanteenTable.DirtLine</c>) and it is the same loudness here, pointed the other way: you are
    /// not showing it to him, you are knowing a name he has heard.</item>
    /// <item><b>A pass for ANOTHER department of this building.</b> Not this floor's — that paper is the one
    /// you SHOW, and a move that quietly accepted it would be two moves doing one job. A pass printed
    /// <c>PLANT</c> on a laboratory floor is worth talking about precisely because it is not worth
    /// showing.</item>
    /// </list>
    ///
    /// <para>Another site's pass is not relevant and never was: a laminate from a building four moons away
    /// says nothing about the work on this one.</para>
    /// </summary>
    public static bool ARelevantPaper(string bodyId, int level, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        string? floorIs = ChamberFitting.DepartmentOn(bodyId, level);
        foreach (Satchel.Item item in carried ?? [])
        {
            if (item.Kind == Satchel.Kind.Dirt)
            {
                return true;
            }

            if (item.Kind != Satchel.Kind.Badge
                || !string.Equals(PatrolBeat.SiteOfBadge(item.Id), bodyId, StringComparison.Ordinal))
            {
                continue;
            }

            string tier = PatrolBeat.TierOfBadge(item.Id);
            if (PatrolBeat.IsADepartmentOf(bodyId, tier)
                && !string.Equals(tier, floorIs, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Is this move on offer right now? The one call the panel asks, so the button that is drawn and
    /// the branch that runs cannot come to two answers.</summary>
    /// <param name="moveId">Which move.</param>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="carried">The wallet.</param>
    /// <param name="askedAlready">Whether this man has already been asked the way this watch.</param>
    public static bool OnOffer(
        string moveId, string bodyId, int level, IReadOnlyList<Satchel.Item>? carried, bool askedAlready) =>
        moveId switch
        {
            Show => APaperToShow(bodyId, carried),
            Mess => !askedAlready,
            Work => ARelevantPaper(bodyId, level, carried),
            _ => true,
        };

    /// <summary>Why not, said out loud on the disabled control — #603's founding law: a control that does
    /// nothing and says nothing is indistinguishable from a bug.</summary>
    public static string WhyNot(string moveId) => moveId switch
    {
        Show => NoPaperLine,
        Mess => AlreadyAskedLine,
        Work => NothingToTalkAboutLine,
        _ => AlreadyAskedLine,
    };

    /// <summary>The refusal on SHOW THE PASS with an empty wallet. It says what is true about the POCKET and
    /// draws no conclusion about the man — the register <c>PatrolBeat.NothingLine</c> keeps one beat
    /// later.</summary>
    public const string NoPaperLine = "There is nothing in your wallet a palm is for.";

    /// <summary>The refusal on the ask, second time. A man remembers being asked the way five minutes
    /// ago.</summary>
    public const string AlreadyAskedLine = "You have asked him that once already.";

    /// <summary>The refusal on the work. Nothing on you is a name, and a bluff with nothing behind it is not
    /// a move this scene offers.</summary>
    public const string NothingToTalkAboutLine = "You have nothing on you that is worth his time.";

    // ── WHAT THE BANDS SAY ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Cover held.</b> Fable-authored, verbatim, and it is the RESOLUTION rather than the read — the read
    /// itself is <see cref="PatrolBeat.SatisfiedLine"/> and has been since #804's canon pass.
    ///
    /// <para>Five words, and they are short on purpose: cover is a state and silence is the reward (#605).
    /// Nothing is explained, nothing is granted, and nobody says well done. He steps aside because the
    /// paperwork balanced, and the loudest thing in the sentence is the full stop.</para></summary>
    public const string StepsAsideLine = "He steps aside. Nothing is said.";

    /// <summary><b>The ask, landed.</b> Fable-authored, verbatim — and the second clause is the whole of what
    /// it buys. He believes you, he is helpful about it, and the way he points is not the way you came, which
    /// means the building is bigger than the corridor you know and a man on the rota has just told you
    /// so.</summary>
    public const string PointsTheWayLine = "He points. It is not the way you came.";

    /// <summary><b>The work, landed.</b> Fable-authored, verbatim. He has heard the name — which is the whole
    /// value of a file on somebody — and then he does the thing everybody on a rota does with a problem that
    /// is not theirs.</summary>
    public const string NotHisProblemLine = "He has heard the name. He decides it is not his problem.";

    // ── WHAT AN ANSWER IS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>How a move ENDS, as a fact rather than as a sentence. Three, and every one of them reaches
    /// machinery that was already in the game — there is no new punishment on this lane, which is #605's own
    /// constraint one file along.</summary>
    public enum Ending
    {
        /// <summary>He goes back to his round and nothing happens to you.</summary>
        HeWalksOn,

        /// <summary>The escort, as a refusal — the pip, the line in a book, the walk to the car, the heat.
        /// Exactly what a refused read has cost since #804, reached through exactly the same seam.</summary>
        HeWalksYouOut,

        /// <summary>#746 · The same walk, <b>helpfully</b>. He is taking you where you said you were going,
        /// which on a restricted floor is the lift and nowhere else — the same legs, the same route, the same
        /// pace ahead of him — and it costs nothing at all: no pip, no heat, no line in any book. The escort
        /// machinery is a man walking you somewhere, and this lane is the first thing in the game to notice
        /// that being walked somewhere is not intrinsically a punishment.</summary>
        HeWalksYouToTheHalls,
    }

    /// <summary>One move's whole answer.</summary>
    /// <param name="Read">What the card says — <see cref="PatrolBeat.Read"/>, the same shape the shipped stop
    /// has always raised, so there is one card and never a second one.</param>
    /// <param name="How">What happens next.</param>
    /// <param name="NameGoesInTheBook">Whether the YES-BUT's cost applies: one rung of #715's heat at this
    /// outfit, and <b>nothing is said about it</b>. A sentence would be the building announcing that it has
    /// started remembering you, which is the one thing that feature may never do (§13.8).</param>
    public readonly record struct Answer(PatrolBeat.Read Read, Ending How, bool NameGoesInTheBook = false);

    /// <summary>
    /// <b>MOVE 1 · SHOW THE PASS — and the ladder is the whole of it.</b>
    ///
    /// <para>The verdict is handed in rather than re-derived: <see cref="PatrolBeat.TheGuardReads"/> already
    /// walks #683's four rungs plus #1149's two and #605's one, already quotes the four owner-authored lines
    /// and Fable's fifth, and already names the escort as the consequence. A second judgement here would be
    /// two answers to one question about the one system whose entire register is procedure.</para>
    ///
    /// <para>The ONE thing this move adds is on the arm that holds: the read is what he SAYS, and
    /// <see cref="StepsAsideLine"/> is what he then DOES. The refused arms are byte-identical to the shipped
    /// card, down to the consequence.</para>
    /// </summary>
    /// <param name="ladder">The read, off the paper that was actually handed over.</param>
    public static Answer ThePaperGoesIntoHisHand(PatrolBeat.Read ladder) =>
        ladder.Satisfied
            ? new Answer(ladder with { Consequence = StepsAsideLine }, Ending.HeWalksOn)
            : new Answer(ladder, Ending.HeWalksYouOut);

    /// <summary>
    /// <b>MOVE 4 · SAY NOTHING — and it is the ladder too.</b>
    ///
    /// <para>Standing there with your hands in your pockets is not a fifth thing that can happen at a
    /// checkpoint; it is the rung the read has had since #804, and <see cref="PatrolBeat.NothingLine"/> is
    /// already written about a man waiting the entire time you are looking. So the move hands back that read
    /// unaltered, and the fixed outcome is byte-identical to the verdict the same wallet would have earned by
    /// being empty.</para>
    /// </summary>
    /// <param name="ladderWithAnEmptyHand">The read composed with nothing handed over.</param>
    public static Answer NothingIsSaid(PatrolBeat.Read ladderWithAnEmptyHand) =>
        new(ladderWithAnEmptyHand, Ending.HeWalksYouOut);

    /// <summary>
    /// <b>MOVE 2 · THE ASK.</b> Free, rolled, once per man per watch.
    ///
    /// <para>YES he points and walks you to the halls for nothing. YES-BUT he does the same and your name
    /// goes in a book. NO-AND is the escort as a refusal, in the shipped words — which is the scene MOVING
    /// rather than a wall: you asked a man for directions and he decided you were somebody who should not be
    /// on this floor, and that is a different afternoon from having shown him a bad pass.</para>
    /// </summary>
    public static Answer TheWayToTheMess(Encounter.Band band, string plate) => band switch
    {
        Encounter.Band.Yes => new(Said(PointsTheWayLine, plate), Ending.HeWalksYouToTheHalls),
        Encounter.Band.YesBut =>
            new(Said(PointsTheWayLine, plate), Ending.HeWalksYouToTheHalls, NameGoesInTheBook: true),
        _ => Refused(plate),
    };

    /// <summary>
    /// <b>MOVE 3 · TALK ABOUT THE WORK.</b> Rolled, and it needs a name on you
    /// (<see cref="ARelevantPaper"/>).
    ///
    /// <para>YES you are passed this once — he walks on and nothing about your cover changed, because nothing
    /// about your cover was ever in question: he simply decided not to make this his afternoon. YES-BUT is
    /// the same and your name goes in a book. NO-AND is the escort.</para>
    /// </summary>
    public static Answer TheWork(Encounter.Band band, string plate) => band switch
    {
        Encounter.Band.Yes => new(Said(NotHisProblemLine, plate), Ending.HeWalksOn),
        Encounter.Band.YesBut =>
            new(Said(NotHisProblemLine, plate), Ending.HeWalksOn, NameGoesInTheBook: true),
        _ => Refused(plate),
    };

    /// <summary>The card an unrolled, unrefused answer is told on: the round's own label, the round's own
    /// picture, the round's own body, and one line. Nothing about the CARD forks per move — #804's law about
    /// the plate applies to the whole scene, because the man in the picture has not decided anything
    /// yet.</summary>
    private static PatrolBeat.Read Said(string line, string plate) =>
        new(true, line, PatrolBeat.ChallengeLabel, PatrolBeat.ChallengeCard(plate));

    /// <summary>
    /// NO — AND THE SCENE MOVES. The band that is the whole design, and at a checkpoint it needs no new
    /// sentence at all: <see cref="PatrolBeat.EscortLine"/> opens with the word <i>"No?"</i> and is an answer
    /// to an ask in the owner's own canon copy. A refused ask IS that sentence.
    /// </summary>
    private static Answer Refused(string plate) =>
        new(
            new PatrolBeat.Read(
                false, PatrolBeat.EscortLine, PatrolBeat.ChallengeLabel, PatrolBeat.ChallengeCard(plate)),
            Ending.HeWalksYouOut);

    // ── THE DICE ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The last ten minutes at a checkpoint, as facts. Three of <see cref="Encounter.Situation"/>'s five
    /// apply and two cannot: you did not buy this man a round, and there is no house whose ways you learned
    /// first — a corridor is not a canteen, and a modifier that could never be true would be a receipt for
    /// something that did not happen.
    /// </summary>
    /// <param name="relevantPaper">+1 · something on you is a name (<see cref="ARelevantPaper"/>).</param>
    /// <param name="nerve">The gauge, read through <see cref="Encounter.NerveReadsAcrossATable"/> — the same
    /// rungs the player is looking at, so the −1 and the readout can never disagree.</param>
    /// <param name="fumbled">−1 · you already fumbled an ask at this stop this watch.</param>
    public static Encounter.Situation SituationAt(bool relevantPaper, double nerve, bool fumbled) => new(
        PaperShown: relevantPaper,
        NerveMarked: Encounter.NerveReadsAcrossATable(nerve),
        Fumbled: fumbled);

    /// <summary>Every authored sentence this file owns, for the canon grep — the discipline
    /// <see cref="PatrolBeat.AllProse"/> and <see cref="WalletChoice.AllProse"/> keep, so a line added
    /// tomorrow is swept tomorrow.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Opening;
        yield return Setting;
        yield return ShowLabel;
        yield return MessLabel;
        yield return WorkLabel;
        yield return NothingLabel;
        yield return StepsAsideLine;
        yield return PointsTheWayLine;
        yield return NotHisProblemLine;
        yield return NoPaperLine;
        yield return AlreadyAskedLine;
        yield return NothingToTalkAboutLine;
    }
}
