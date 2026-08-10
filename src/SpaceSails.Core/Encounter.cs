using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #746 · THE ENCOUNTER — the game's one social-scene machine.
///
/// <para>Owner, 2026-08-06 (work-break brief): <i>"sit-down and drink-offer — in both directions — are how
/// contact with new people begins"</i>, and, in the same breath, the sentence that decides the SHAPE of this
/// file: <i>"guard stops (show ID, explain yourself) should use the same choose-your-action mechanic."</i></para>
///
/// <h3>Why this is a type and not a canteen feature</h3>
///
/// <para>A counterpart, a setting, an opening line and a set of MOVES. That is the whole of it, and it is
/// deliberately the whole of it: <b>the guard stop is not a second system, it is an encounter whose setting
/// walked up to you.</b> Building the table teaches the game to build the checkpoint, and the test of that
/// claim is not an argument — it is that a guard stop can be written as a <see cref="Scene"/> with no new
/// code at all. A guard test does exactly that with a sample content entry, which is why one exists.</para>
///
/// <h3>The roll — Fail Forward, from the owner's own TTRPG</h3>
///
/// <para>Canonical mechanics source: the owner's Ropecon system (Fail Forward / sanity —
/// github.com/esoinila/LatexHahmolomakkeet). Three bands, and the third one is the whole design:</para>
///
/// <list type="bullet">
/// <item><b>YES</b> — it lands.</item>
/// <item><b>YES, BUT</b> — it lands and costs something real: a nerve pip, a favour owed, your name in a
/// book under a name that isn't yours.</item>
/// <item><b>NO — AND THE SCENE MOVES.</b> Never a dead wall. A refusal changes state: a disposition shifts,
/// another door opens in the conversation, somebody stands up and leaves.</item>
/// </list>
///
/// <para>Seeded through <see cref="DiceRule"/> like every other roll in this game — keyed on (site, floor,
/// counterpart, move, attempt) — so the same ask at the same table on the same watch is the same answer, and
/// a test can pin every band by choosing an attempt index rather than by mocking a die.</para>
///
/// <h3>Modifiers are situational, never a stat sheet</h3>
///
/// <para>Five of them, all of them facts about the last ten minutes rather than numbers on a character: you
/// bought the round, you put the right paper on the table, you learned the house's ways first, your hands
/// are shaking (nerve is marked), you already fumbled an ask at this table. <b>NERVE is the social resource
/// too</b> — the same gauge the ground bleeds, never a parallel meter, which is the sanity system's own
/// lineage saying that a scene going wrong down there is genuinely frightening.</para>
///
/// <para>Pure and deterministic, like everything else in Core: no clock, no <c>Random</c>, no world.</para>
/// </summary>
public static class Encounter
{
    /// <summary>The three answers. Ordered worst-first so a band can be compared, and named for what the
    /// player is actually told rather than for a pass/fail the game does not have.</summary>
    public enum Band
    {
        /// <summary>No — <b>and the scene moves.</b> Never a dead wall.</summary>
        NoAnd,

        /// <summary>Yes, but it costs something real.</summary>
        YesBut,

        /// <summary>Yes. It lands.</summary>
        Yes,
    }

    /// <summary>What a move costs before it can even be offered. Not a difficulty — a DOOR: a move whose
    /// requirement is unmet is not shown greyed with a shrug, it is a move this captain has not earned the
    /// right to make yet, and the panel says which.</summary>
    public enum Requirement
    {
        /// <summary>Always available. Small talk, and — by law — taking your leave.</summary>
        Free,

        /// <summary>Costs coin. Buying the round.</summary>
        Credits,

        /// <summary>Needs something in the satchel, put on the table where everyone can see it.</summary>
        SatchelItem,

        /// <summary>Needs a move already made at this table this watch — the house's ways learned before the
        /// ask, the second line of somebody's story after the first.</summary>
        PriorMoveThisWatch,

        /// <summary>
        /// #749 · An ANSWER to something they said, earlier in THIS conversation.
        ///
        /// <para>The one requirement that is not a door. The others are things you have not earned yet and
        /// are shown refused, with the reason on them (#603): a price you cannot pay is information. This one
        /// is a <b>sentence nobody has spoken</b>, and a reply to a sentence nobody has spoken is not a
        /// control that is disabled — it is not there. Hence two departures from
        /// <see cref="PriorMoveThisWatch"/>, and both of them are the same fact said twice:</para>
        ///
        /// <list type="number">
        /// <item><b>THE SCENE, not the watch.</b> <see cref="Exists"/> and <see cref="Available"/> read the
        /// moves made in THIS sitting. Standing up ends the conversation; the room remembers that you asked,
        /// and the room is right to, but you cannot walk back and answer an offer that was made to somebody
        /// who has since left the table.</item>
        /// <item><b>Absent, not refused.</b> <see cref="OnTheTable"/> leaves it out of the panel until the
        /// line has been said. Owner, filing #749: <i>"a reply you can give before the sentence exists reads
        /// as menu, not conversation."</i></item>
        /// </list>
        ///
        /// <para>The offer's answers at the fitter's table are this. So is every "…and who might that be?"
        /// a checkpoint will ever grow: content, on the framework's own type, with no new code.</para>
        /// </summary>
        ReplyToPriorMove,
    }

    /// <summary>
    /// One thing the captain can say or do at this scene.
    /// </summary>
    /// <param name="Id">Stable identity — what the client keys "already made this watch" off.</param>
    /// <param name="Label">The button. What you are about to do, in the captain's own register.</param>
    /// <param name="Needs">What it costs to be ALLOWED to make it.</param>
    /// <param name="Credits">Coin, for <see cref="Requirement.Credits"/>. Zero otherwise.</param>
    /// <param name="Item">Which kind of thing must be in the satchel, for
    /// <see cref="Requirement.SatchelItem"/>. Null otherwise.</param>
    /// <param name="After">The move id that must already have been made, for
    /// <see cref="Requirement.PriorMoveThisWatch"/> and <see cref="Requirement.ReplyToPriorMove"/> — for the
    /// second of those it is the move that made them SAY the thing this one answers. Null otherwise.</param>
    /// <param name="Rolled">Whether the outcome is decided by the dice. False means the outcome is FIXED —
    /// and that is a design statement wherever it appears, not an unfinished roll: honest work offered
    /// plainly is not a check, and neither is leaving a table politely.</param>
    /// <param name="Says">The fixed outcome's line, for an unrolled move. Null for a rolled one, whose
    /// content owns a line per band.</param>
    /// <param name="OrAfter">#757 · A SECOND SENTENCE THAT WOULD ALSO DO. Some things a counterpart says
    /// have two answers and both of them count as having heard it — a drink taken and a drink turned down
    /// are one beat, and what comes next comes next either way. Without this a ladder either forces the
    /// polite answer or is not a ladder at all, and "you may only proceed if you accepted" is exactly the
    /// dead wall Fail Forward exists to refuse. Read by <see cref="Exists"/> beside
    /// <see cref="After"/>; null on every move that has one answer.</param>
    /// <param name="Note">#757 · What the FIELD BOOK keeps of a fixed outcome, or null when the moment is
    /// not worth filing. Beside <see cref="Says"/> and for the same reason #749 put <see cref="Says"/> here:
    /// a content file must be able to say "this one goes in the notebook" without a client author
    /// remembering to write its id into a switch. Null on manners — a notebook full of manners is a notebook
    /// nobody reads.</param>
    public readonly record struct Move(
        string Id,
        string Label,
        Requirement Needs = Requirement.Free,
        int Credits = 0,
        Satchel.Kind? Item = null,
        string? After = null,
        bool Rolled = false,
        string? Says = null,
        string? OrAfter = null,
        string? Note = null);

    /// <summary>
    /// One scene: who you are talking to, where, what they open with, and what you may do about it.
    ///
    /// <para>This is the type a guard stop is written on. Nothing in it knows about canteens.</para>
    /// </summary>
    /// <param name="Id">Stable identity for the scene's own state (which moves have been made).</param>
    /// <param name="Counterpart">Who — as they read at a glance, before anybody speaks.</param>
    /// <param name="Setting">Where. A canteen table, a bar table, a checkpoint at a lift head.</param>
    /// <param name="Opening">What they say as the scene opens. For a table it is the wave-in; for a guard
    /// stop it is the thing that stopped you.</param>
    /// <param name="Moves">What you may do. <see cref="Leave"/> must be among them — see its docs.</param>
    public readonly record struct Scene(
        string Id,
        string Counterpart,
        string Setting,
        string Opening,
        IReadOnlyList<Move> Moves);

    /// <summary>
    /// The last ten minutes, as the dice see it. Every field is a FACT the player could have narrated
    /// themselves — which is what keeps this from turning into a character sheet.
    /// </summary>
    /// <param name="RoundBought">A round was bought at this table this watch.</param>
    /// <param name="PaperShown">Something relevant is on the table.</param>
    /// <param name="HouseWaysLearned">You learned how things work here before you asked for anything.</param>
    /// <param name="NerveMarked">Your nerve is marked — shaking hands read across a table.</param>
    /// <param name="Fumbled">You already fumbled an ask at this table this watch.</param>
    public readonly record struct Situation(
        bool RoundBought = false,
        bool PaperShown = false,
        bool HouseWaysLearned = false,
        bool NerveMarked = false,
        bool Fumbled = false);

    // ── The named modifier stack ──────────────────────────────────────────────────────────────────────
    //
    // DiceRule keeps the LIST verbatim so the UI can show the math (§5.0: "the player watches the numbers
    // add up"). These are the labels they read; they are sentences about the scene, not stat names.

    /// <summary>+1 · you stood the table a round this watch.</summary>
    public const string RoundLabel = "you bought the round";

    /// <summary>+1 · the right piece of paper is on the table.</summary>
    public const string PaperLabel = "the paper on the table";

    /// <summary>+1 · you learned the house's ways before you asked for anything.</summary>
    public const string HouseWaysLabel = "you know how it works here";

    /// <summary>−1 · your hands are not steady, and it reads across a table.</summary>
    public const string NerveLabel = "your hands are not steady";

    /// <summary>−1 · you already fumbled an ask at this table this watch.</summary>
    public const string FumbledLabel = "you already asked once";

    /// <summary>What one situational fact is worth. One, every time, in either direction — the owner's
    /// standing "dice modifiers, never OP" rule, and the reason five of them still cannot swamp a d20.</summary>
    public const int ModifierStep = 1;

    /// <summary>The situation as a named modifier stack, in a fixed order so the on-screen math reads the
    /// same way twice. Only the facts that HOLD appear: a stack listing "+0 (you did not buy a round)" is a
    /// receipt for things that did not happen.</summary>
    public static IReadOnlyList<DiceModifier> Modifiers(Situation s)
    {
        var stack = new List<DiceModifier>(5);
        if (s.RoundBought)
        {
            stack.Add(new DiceModifier(RoundLabel, +ModifierStep));
        }
        if (s.PaperShown)
        {
            stack.Add(new DiceModifier(PaperLabel, +ModifierStep));
        }
        if (s.HouseWaysLearned)
        {
            stack.Add(new DiceModifier(HouseWaysLabel, +ModifierStep));
        }
        if (s.NerveMarked)
        {
            stack.Add(new DiceModifier(NerveLabel, -ModifierStep));
        }
        if (s.Fumbled)
        {
            stack.Add(new DiceModifier(FumbledLabel, -ModifierStep));
        }
        return stack;
    }

    // ── The bands ─────────────────────────────────────────────────────────────────────────────────────
    //
    // FIFTH-BUG-CLASS WARNING, paid up front: a threshold no real roll can reach is a guard that asserts
    // nothing. The die is DiceRule's own d20 (a uniform 1..20) and the stack is at most +3 / −2, so the
    // total lives in [-1, 23]. Both thresholds are chosen INSIDE the unmodified 1..20 window on purpose —
    // every band is reachable with no modifiers at all, and the modifiers move the odds rather than
    // unlocking a band that was otherwise impossible. A guard measures this against real rolls rather than
    // trusting the arithmetic in this comment.

    /// <summary>At or above this total, it lands. Seven faces of twenty naked — a stranger's ask in a
    /// canteen where you are nobody. FLAGGED for the owner's tuning.</summary>
    public const int YesAt = 14;

    /// <summary>At or above this total (and below <see cref="YesAt"/>), it lands and costs something.
    /// Six faces of twenty naked, which makes the MIDDLE band the most likely single outcome of an
    /// unprepared ask — exactly what a Fail Forward system should feel like. FLAGGED for tuning.</summary>
    public const int YesButAt = 8;

    /// <summary>Which band a settled total falls in.</summary>
    public static Band BandOf(int total) =>
        total >= YesAt ? Band.Yes : total >= YesButAt ? Band.YesBut : Band.NoAnd;

    /// <summary>
    /// Roll one move of one encounter.
    ///
    /// <para>The seed is the whole determinism story: (site, floor, counterpart, move, attempt). Same table,
    /// same watch, same ask — same answer, so a captain cannot re-press their way to a better one, and a
    /// test walks attempt indices to reach a band instead of mocking a die.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="counterpart">Who is being asked.</param>
    /// <param name="move">Which move.</param>
    /// <param name="attempt">Which attempt this is — the ordinal that makes a second ask a second roll.</param>
    /// <param name="situation">The last ten minutes.</param>
    public static DiceRoll Roll(
        string bodyId, int level, string counterpart, string move, int attempt, Situation situation)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(counterpart);
        ArgumentNullException.ThrowIfNull(move);
        return DiceRule.Roll(
            DiceRule.Seed($"encounter:{counterpart}:{move}:{bodyId}", level, attempt),
            DiceRule.D20,
            Modifiers(situation));
    }

    /// <summary>The band a roll settles into — or the one QA asked for.
    ///
    /// <para><paramref name="forced"/> is <c>?roll=hi|lo</c> and nothing else. It overrides the BAND and
    /// never the roll: the dice still cast, the math still reads truthfully on screen, and what the tester
    /// sees played out is the same scene a captain would get. A cheat that showed a different scene would be
    /// worse than no cheat at all.</para></summary>
    public static Band Settle(DiceRoll roll, Band? forced = null) => forced ?? BandOf(roll.Total);

    /// <summary>
    /// What a band costs in NERVE — through <see cref="NervePips"/>, never a meter of its own.
    ///
    /// <para>One pip for the two bands that hurt, zero for the one that does not. The owner's sanity-system
    /// lineage says a social scene going wrong down here is genuinely frightening; it does not say it is
    /// worse than being grabbed (<see cref="NervePips.TouchPips"/> is also one). FLAGGED for tuning.</para>
    /// </summary>
    public static int NervePipsFor(Band band) => band switch
    {
        Band.Yes => 0,
        _ => 1,
    };

    /// <summary>
    /// Is the captain's nerve MARKED — visibly, to somebody sitting across a table from them?
    ///
    /// <para>Asked of <see cref="NerveModel.BandFor"/> rather than of a threshold typed here. The rungs
    /// already say when the hands stop being steady, the gauge already draws that rung, and the flavor line
    /// already reads it out loud — so anything below Steady is the −1, and there is no second number that
    /// could ever disagree with the one the player is looking at.</para>
    /// </summary>
    public static bool NerveReadsAcrossATable(double nerve) =>
        NerveModel.BandFor(nerve) != NerveModel.NerveBand.Steady;

    // ── Requirements ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The id every scene's "walk away" move carries. <b>Leaving is free and never penalised</b>
    /// (owner: <i>"politely dodge the jobs that don't"</i>) — the id is public so a guard can assert that law
    /// on any scene, including one nobody has written yet.</summary>
    public const string Leave = "leave";

    /// <summary>
    /// #749 · Does this move EXIST yet — is it a thing on the table to be pressed at all?
    ///
    /// <para>Asked of every move before the panel draws it. Only <see cref="Requirement.ReplyToPriorMove"/>
    /// can answer no: everything else is a door, and a door you cannot open is still a door you can see.</para>
    /// </summary>
    /// <param name="move">The move.</param>
    /// <param name="madeThisScene">Move ids already made in THIS sitting — the conversation, not the watch.
    /// A reply is scoped to the sentence it answers, and the sentence was said to whoever was in the chair.</param>
    public static bool Exists(Move move, IReadOnlyCollection<string>? madeThisScene) =>
        move.Needs != Requirement.ReplyToPriorMove
        || (madeThisScene is not null
            && ((move.After is { } after && madeThisScene.Contains(after))
                // #757 · …or the OTHER thing they might have been answering. Two replies to one sentence are
                // one beat; what follows follows either way.
                || (move.OrAfter is { } orAfter && madeThisScene.Contains(orAfter))));

    /// <summary>#749 · The moves that are ON THE TABLE right now, in the scene's own order. What a panel
    /// draws — and it is a call rather than a rule the markup applies, so the guard stop and the canteen
    /// cannot end up with two different ideas of when an answer exists.</summary>
    public static IReadOnlyList<Move> OnTheTable(Scene scene, IReadOnlyCollection<string>? madeThisScene)
    {
        var shown = new List<Move>(scene.Moves?.Count ?? 0);
        foreach (Move m in scene.Moves ?? [])
        {
            if (Exists(m, madeThisScene))
            {
                shown.Add(m);
            }
        }
        return shown;
    }

    /// <summary>Can this move be made right now?</summary>
    /// <param name="move">The move.</param>
    /// <param name="credits">The purse.</param>
    /// <param name="carried">The satchel.</param>
    /// <param name="madeThisWatch">Move ids already made at this scene this watch.</param>
    /// <param name="madeThisScene">Move ids already made in THIS sitting. A reply reads this and never the
    /// watch: the room remembering that you once asked is not the same as somebody having just spoken.</param>
    public static bool Available(
        Move move, int credits, IReadOnlyList<Satchel.Item>? carried, IReadOnlyCollection<string>? madeThisWatch,
        IReadOnlyCollection<string>? madeThisScene = null)
        => move.Needs switch
        {
            Requirement.Credits => credits >= move.Credits,
            // A move that names a KIND wants that kind; a move that names none wants anything at all —
            // "put something on the table" is a gesture, and which something is the next press, not this one.
            Requirement.SatchelItem => move.Item is { } kind
                ? Satchel.CountOf(carried, kind) > 0
                : (carried?.Count ?? 0) > 0,
            Requirement.PriorMoveThisWatch =>
                move.After is { } after && madeThisWatch is not null && madeThisWatch.Contains(after),
            // One law, one place: a reply that exists is a reply you may make. Answering costs nothing —
            // it is the sentence being on the table at all that was ever in question.
            Requirement.ReplyToPriorMove => Exists(move, madeThisScene),
            _ => true,
        };

    /// <summary>Every scene must offer a way out, and it must be free. Stated here rather than trusted,
    /// because "the polite dodge costs nothing" is a design law the owner smoke-tests by hand — take the
    /// downstairs job, politely dodge the rest — and a scene that grew a price on leaving would fail that
    /// test silently.</summary>
    public static bool CanAlwaysLeave(Scene scene)
    {
        foreach (Move m in scene.Moves ?? [])
        {
            if (string.Equals(m.Id, Leave, StringComparison.Ordinal))
            {
                return m.Needs == Requirement.Free && m.Credits == 0 && !m.Rolled;
            }
        }
        return false;
    }
}
