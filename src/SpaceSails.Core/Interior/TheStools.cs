using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #756 · THE HIGH CHAIRS — the third social posture of the floor.
///
/// <para>Owner, live playtest 2026-08-08: <i>"Also there should be high chairs so sitting at the bar desk is
/// also possible."</i> And, filing the design the same day: <i>"the bar desk gets TALL SEATS — stools at the
/// counter for people who want to socialize with the keep or with the other lonely people there… a table
/// seats a party (#757), a cabinet seats a secret (#758), the STOOL seats a stranger next to whoever fate
/// parked there — you sit at the counter to be talked AT."</i></para>
///
/// <h3>What a stool is, and why it is not a small table</h3>
///
/// <para>A table is a room you hold: you choose it, you face outward, and somebody has to cross the floor to
/// reach you — which is why #757's whole scene is about being FINDABLE and about how long it takes. A stool
/// is the opposite geometry with the same loneliness. You do not choose your company; you take the free seat
/// and inherit whoever is on the next one. Nobody crosses anything. <b>Proximity IS the invitation</b>
/// (owner's own words), so the neighbour never asks to sit: she is already sitting, she turns her head, and
/// the ladder starts at a sentence instead of at a chair.</para>
///
/// <h3>Everything mechanical here is #757's, on purpose</h3>
///
/// <para>The wait beat, the one-approach-per-watch law, the odds read off the hall's own
/// <see cref="CanteenRegulars.WatchFill"/>, and the told silence when nobody speaks — all of it is
/// <see cref="SittingAlone"/>'s, called rather than copied. What is new is the LINES and one law that only a
/// counter can have: <see cref="SomebodyTurns"/> can only be true when somebody is actually on the stool
/// beside you, which is a fact about the room rather than about the dice.</para>
///
/// <para>Pure and deterministic: no clock, no <c>Random</c>, no world. The client owns which stool you are on
/// and how many beats you have sat through; this owns the words, the law and the dice.</para>
///
/// <para>§13.8 holds. Not one line below says what this facility is for, and the neighbour is not the plot —
/// she is a woman at the end of a shift with an ordinary problem, and the horror is a thing the game never
/// states.</para>
/// </summary>
public static class TheStools
{
    /// <summary>The glyph a stool wears. A high chair, at console size.</summary>
    public const string Glyph = "🪑";

    /// <summary>How many stools are bolted down along the counter. The number the picture shows, and the
    /// number the room has: a short row, so a full watch really does leave you standing and a dead one
    /// really does leave you alone with eight empty seats.</summary>
    public const int Count = 8;

    /// <summary>Where you are, while you are on one. <see cref="CanteenTable.Setting"/>'s own room, said the
    /// way you would say it from a stool.</summary>
    public const string Setting = "the counter of the upper canteen";

    /// <summary>What the stool you are on is called on the panel. One-based, the way a seat is numbered
    /// anywhere anybody has ever numbered seats.</summary>
    public static string StoolPlate(int stool) => $"{Glyph} STOOL {stool + 1} · THE COUNTER";

    /// <summary>
    /// #756/#759 · WHAT YOU ARE LOOKING AT ONCE YOU ARE UP ON ONE. Owner, live: <i>"I do not see the park
    /// through the bar windows?"</i>
    ///
    /// <para>Standing at the counter you are looking AT the counter, and the service card wears the desk
    /// (<see cref="CounterService"/>'s <c>DeskArtUrl</c>, which the owner approved shot-for-shot). Sitting on
    /// a stool you are up above the bottles with the window wall in front of you, and what is behind the
    /// glass is the green. So the picture on the card changes with the posture, because the view does — one
    /// fixture, two things to look at, and which one you get is decided by whether you sat down.</para>
    ///
    /// <para>This is the park's first appearance in the game (#759 keeps the rest of it). It is a picture and
    /// nothing else here: no line in this file says what a green is doing under a hundred and fifty metres of
    /// rock, and none ever will — §13.8, and the park is worth more unexplained.</para>
    /// </summary>
    public const string SeatedArtUrl = "art/b1-park-behind-windows.jpg";

    /// <summary>#782 · What is written over the view, at the size a caption is read at: what it IS, and not
    /// one word about why. A caption that explained the window would be the picture's whole effect
    /// spent.</summary>
    public const string SeatedArtCaption =
        "Through the window wall behind the counter: grass, in courses, under a painted sky.";

    // ── TAKING ONE, AND GETTING OFF ───────────────────────────────────────────────────────────────────

    /// <summary>What sitting down at a counter is. Not a transaction: a posture, and a different one from a
    /// table's — you are facing the bottles, not the room, which is the whole of the difference.</summary>
    public const string TookAStoolLine =
        "You get up onto one of the tall seats and put your elbows where a thousand elbows have been. " +
        "Facing the bottles instead of the room is a decision about what you are here for, and everybody " +
        "who has ever sat down at a counter has made it.";

    /// <summary>Getting off. Free, always, and it never costs a thing.</summary>
    public const string GotDownLine = "You get down off the stool, and it turns very slightly, and stops.";

    /// <summary>What the row refuses with when every seat is taken. A refusal said out loud (#603), never a
    /// button that does nothing.</summary>
    public const string RowIsFullLine =
        "Every stool along the counter has somebody on it, and not one of them looks up.";

    // ── WHO IS ON THE OTHER STOOLS ────────────────────────────────────────────────────────────────────
    //
    // THE ROOM DECIDES, OFF THE FROZEN WATCH, AND IT DECIDES ONCE. The drawn room and the pressed room are
    // one room (this project's third named bug class), so how many stools are taken comes out of the very
    // WatchFill that fills the tables — the counter of a hall that is heaving is a counter with a queue at
    // it, and the counter of a hall that has been emptied has eight empty seats and a fridge.

    /// <summary>How many stools have somebody on them this watch. The hall's own fill, applied to the row —
    /// floored, so the emptiest shift stays the emptiest shift, and capped one short of the row so a captain
    /// is never simply refused a seat by arithmetic on the busiest watch.</summary>
    public static int TakenCount(long watch) =>
        Math.Clamp((int)(Count * SittingAlone.Fill(watch)), 0, Count - 1);

    /// <summary>Is this stool occupied on this watch? Seeded on (site, floor, stool, watch) and nothing
    /// else, so the row a captain sits down at is the row they saw, every time they look.</summary>
    public static bool Taken(string bodyId, int level, int stool, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (stool < 0 || stool >= Count)
        {
            return false;
        }
        // A stable shuffle rather than "the first N are taken": people do not fill a bar left to right, and
        // a row that did would make the free seat predictable and the neighbour law trivial. Each stool
        // draws its own number and the lowest TakenCount of them are the ones with somebody on them.
        int rank = 0;
        ulong mine = DiceRule.Seed($"counter:stool:{bodyId}:{stool}", level, watch);
        for (int s = 0; s < Count; s++)
        {
            if (s == stool)
            {
                continue;
            }
            if (DiceRule.Seed($"counter:stool:{bodyId}:{s}", level, watch) < mine)
            {
                rank++;
            }
        }
        return rank < TakenCount(watch);
    }

    /// <summary>#756 · PICK-OR-DEFAULT: the free stool a captain gets when they take one, or null when the
    /// row is full. Owner's grammar, in one call — <i>"one E, pick-or-default a free stool"</i> — so no
    /// caller anywhere walks the row itself and no two callers can disagree about which seat is free.</summary>
    public static int? FirstFreeStool(string bodyId, int level, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        for (int s = 0; s < Count; s++)
        {
            if (!Taken(bodyId, level, s, watch))
            {
                return s;
            }
        }
        return null;
    }

    /// <summary>Is anybody on a stool immediately beside this one? THE LAW THE COUNTER HAS AND THE TABLES DO
    /// NOT: at a table the room decides whether somebody will walk over, and anybody in it might; here the
    /// only person who can turn to you is the person already within arm's reach. An empty seat either side
    /// is a silence nothing can break, however busy the watch — and that is not a gap in the content, it is
    /// the same one law stated in its other direction (#757's cabinet clause, at a counter).</summary>
    public static bool HasNeighbour(string bodyId, int level, int stool, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return (stool > 0 && Taken(bodyId, level, stool - 1, watch))
            || (stool < Count - 1 && Taken(bodyId, level, stool + 1, watch));
    }

    // ── WHETHER THE NEIGHBOUR SPEAKS ──────────────────────────────────────────────────────────────────
    //
    // FIFTH-BUG-CLASS WARNING, paid up front and inherited: the odds are SittingAlone's own, so the same
    // guard that proves a table's threshold selects neither everything nor nothing proves this one. What is
    // this file's own is the gate in front of the roll, and its two directions are both reachable on watches
    // the game actually has — which a guard measures against real rolls rather than trusting this comment.

    /// <summary>
    /// #756 · DOES THE PERSON ON THE NEXT STOOL TURN TO YOU THIS BEAT?
    ///
    /// <para>Two things have to be true and they are different KINDS of true. There has to be somebody
    /// beside you (the room), and this has to be the beat they say something (the dice). The odds are the
    /// hall's own — <see cref="SittingAlone.FacesThatBringSomebody"/>, unchanged and uncopied, because "does
    /// this room have anything for you tonight" is one question however you are sitting. The DIE is this
    /// counter's own, seeded on the stool rather than the top, so sitting at a table and sitting at the bar
    /// are two evenings and not one answer read twice.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="stool">Which seat — the row's own ordinal, never a pair of doubles.</param>
    /// <param name="watch">The shift, frozen when the floor was drawn (#709).</param>
    /// <param name="beat">How many times you have waited on this stool this sitting, from zero.</param>
    public static bool SomebodyTurns(string bodyId, int level, int stool, long watch, int beat)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!HasNeighbour(bodyId, level, stool, watch))
        {
            return false;
        }
        DiceRoll roll = DiceRule.Roll(
            DiceRule.Seed($"counter:turns:{bodyId}:{stool}", level, watch, beat), SittingAlone.Faces);
        return roll.Face <= SittingAlone.FacesThatBringSomebody(watch);
    }

    // ── AND WHEN NOBODY DOES ──────────────────────────────────────────────────────────────────────────
    //
    // Three silences, because a counter has three of them and they are not the same sentence. The seat beside
    // you is EMPTY (nobody could have spoken). It is TAKEN and they do not (somebody could, and did not,
    // which is worse). Or the whole room is empty and the counter is serving nobody at all. Nothing anywhere
    // announces which of the three you are in; the words do it.

    /// <summary>Nobody is on either side of you, and the row is a row of empty stools.</summary>
    public static readonly IReadOnlyList<string> NobodyBesideYou =
    [
        "The stools either side of you are empty and stay empty. The reader on the counter wakes when you " +
        "move, decides you have not asked it anything, and goes back to sleep.",
        "A while goes by. You have a whole run of tall seats to yourself, which is a thing you only ever " +
        "get in a room nobody wants to be in.",
        "Nothing comes over. The rail under your boots has been polished down to the brass by other " +
        "people's feet, and none of them are here.",
    ];

    /// <summary>Somebody is right there, and does not turn. The busiest silence in the building.</summary>
    public static readonly IReadOnlyList<string> NeighbourSaysNothing =
    [
        "The one on the next stool turns a cup a quarter turn and does not turn with it.",
        "A while goes by, close enough to hear the other person breathe, and neither of you says anything. " +
        "That is allowed here. It seems to be usual.",
        "Your neighbour reads the same three lines on their own reader again, then puts it face down, then " +
        "picks it back up.",
        "Somebody two stools along laughs at something on a screen. Nobody nearer than that makes a sound.",
    ];

    /// <summary>And a counter in a hall that has been emptied. Not one of these says why.</summary>
    public static readonly IReadOnlyList<string> CounterIsQuiet =
    [
        "A while goes by. The machine behind the counter finishes a cycle nobody asked for and starts " +
        "warming a plate for nobody.",
        "You wait. Eight stools, and the glasses on the shelf behind the bottles have been washed and " +
        "stacked by somebody who had the time.",
    ];

    /// <summary>
    /// #756 · WHAT NOTHING HAPPENING SOUNDS LIKE AT A COUNTER — the told outcome, so a wait that produced
    /// nobody reads as an answer and not as a control that did not respond (#757's law, #603's fear).
    /// </summary>
    /// <param name="watch">The shift, which decides whether this is a busy silence or an emptied one.</param>
    /// <param name="beat">Which wait this was, so a captain who sits through four is told four different
    /// things and the counter does not loop.</param>
    /// <param name="neighbour">Whether somebody is actually on the next stool. The difference between the
    /// two loud silences, and the only thing this needs to know that the watch cannot tell it.</param>
    public static string NobodySpoke(long watch, int beat, bool neighbour)
    {
        IReadOnlyList<string> pool = neighbour
            ? NeighbourSaysNothing
            : SittingAlone.Fill(watch) >= SittingAlone.BusyAt ? NobodyBesideYou : CounterIsQuiet;
        return pool[(int)(((beat % pool.Count) + pool.Count) % pool.Count)];
    }

    // ── THE ONE ON THE NEXT STOOL ─────────────────────────────────────────────────────────────────────
    //
    // She is a SCAFFOLDER four days off a shift rotation, which is the register test (#701) again: not
    // mysterious, not interesting, not the plot. She has an ordinary reason to be sitting here and every
    // word she says is about her own week and her own money. #709's economics, in one fixture — she is paid
    // by a company that has stopped writing the reason on the slip, and she has noticed, and she has decided
    // it is fine. What is horrifying about that is a thing the game never states.
    //
    // AND SHE POINTS AT WHAT IS ALREADY BUILT. The rest days that are not leave, the slip that comes with no
    // line on it, the office upstairs that files everything (#411) — the captain can go and look at all of
    // it today. Nothing here promises a scene the game cannot already play.

    /// <summary>Who she reads as, at a glance, before she says anything.</summary>
    public const string NeighbourPlate = "◈ A SCAFFOLDER WITH FOUR DAYS OFF";

    /// <summary>#756 · SHE DOES NOT ASK TO SIT. Owner's law, said as a sentence instead of as a rule: she is
    /// already there, so the ladder starts one rung higher than a table's — at a remark, thrown sideways, to
    /// nobody in particular, which is how everybody has always opened a conversation at a bar.</summary>
    public const string NeighbourOpening =
        "The one on the next stool speaks without turning her head. \"You're the one off the boat. " +
        "Everybody's decided that already, so you might as well hear it from somebody sitting down.\"";

    /// <summary>The first rung: you say something back. Free, and it is the whole invitation — you did not
    /// pull a chair out, you just answered.</summary>
    public const string AnsweredHerLine =
        "She turns about a third of the way round, which from somebody four days into their rest is most of " +
        "the way. \"Scaffolder. Four days off. Third lot of four days off this quarter, which is generous " +
        "of them, and I've stopped asking.\"";

    /// <summary>…or you do not, and it costs nothing. She takes it exactly as well as she took the last
    /// person who did.</summary>
    public const string LetItLieLine =
        "\"Fair enough.\" She goes back to her cup, and the next time you look she has been served something " +
        "she did not order and is drinking it anyway.";

    /// <summary>The second rung: you stand HER one. At a table the visitor buys; at a counter the drinks are
    /// eighteen inches away and there is nothing to carry, so the offer runs the other way and is the
    /// cheapest thing in this building.</summary>
    public const string StoodHerOneLine =
        "The counter puts it in front of her without being told which one, which she notices and you notice " +
        "her noticing. \"Right,\" she says. \"Now I owe you a sentence.\"";

    /// <summary>…or you keep your money and she carries on regardless, because she was going to.</summary>
    public const string KeptYourCoinLine =
        "\"No, quite right, don't.\" She says it approvingly, and carries on as if you had.";

    /// <summary>
    /// THE THIRD RUNG — what she has actually been sitting there thinking about, said flatly, the way people
    /// say the thing that has been bothering them for a month.
    ///
    /// <para>#761 · Told clearly. There is nothing to infer about what she is telling you; the inference is
    /// somewhere else entirely, and she does not know she is sitting next to it.</para>
    /// </summary>
    public const string HerWeekLine =
        "\"Here's the thing I can't put down. The slip used to say what the days off were FOR — weather, " +
        "waiting on steel, whatever. Since spring it just says the number and the money's the same, and " +
        "everybody I've asked says take the money. So I take the money. Only there's a hand upstairs who " +
        "files every slip in this building, and he's never once looked me in the face while he did it.\"";

    /// <summary>What the field book keeps of it. The book records what she said and what it points at, and
    /// never what it might mean.</summary>
    public const string HerWeekNote =
        "A scaffolder at the counter is on her third block of rest days this quarter. The slips stopped " +
        "saying what the days off are for in the spring; the money did not change. The hand upstairs files " +
        "every slip and does not look up.";

    /// <summary>The move ids. Named here so no panel invents its own vocabulary for a move the design
    /// named — the same discipline <see cref="SittingAlone.LabelOf"/> keeps.</summary>
    public const string Wait = SittingAlone.Wait;

    /// <summary>Say something back — the rung a table spends a chair on.</summary>
    public const string AnswerHer = "answer-her";

    /// <summary>…or do not.</summary>
    public const string LetItLie = "let-it-lie";

    /// <summary>Stand her one, off a counter eighteen inches away.</summary>
    public const string StandHerOne = "stand-her-one";

    /// <summary>…or keep your coin, which is also an answer.</summary>
    public const string KeepYourCoin = "keep-your-coin";

    /// <summary>Ask what has been bothering her.</summary>
    public const string AskHerWeek = "ask-her-week";

    /// <summary>Get down off the stool. <see cref="Encounter.Leave"/>, so the framework's free-exit law
    /// covers this scene without this file restating it.</summary>
    public const string GetDown = Encounter.Leave;

    /// <summary>What a round costs her — the counter's own rate for its own pour, asked of
    /// <see cref="CounterService.HouseRate"/> rather than typed, so the button, the debit and the card under
    /// the glass are one number.</summary>
    public const int StandHerOnePrice = CounterService.HouseRate;

    /// <summary>The button labels, beside the ids.</summary>
    public static string LabelOf(string moveId) => moveId switch
    {
        Wait => "Wait",
        AnswerHer => "Say something back",
        LetItLie => "Let it lie",
        StandHerOne => $"Stand her one · {StandHerOnePrice} cr",
        KeepYourCoin => "Keep your coin",
        AskHerWeek => "What's the thing, then?",
        _ => "Get down off the stool",
    };

    /// <summary>
    /// #756 · YOUR OWN STOOL, as an <see cref="Encounter.Scene"/> — two moves and no third.
    ///
    /// <para>Wait, and get down. Deliberately nothing else: ordering is not a move here because the menu is
    /// already on this card and always has been (#756's one seam), which is exactly what <i>"the keep serves
    /// you seated"</i> had to mean if it was to mean anything at all.</para>
    /// </summary>
    public static Encounter.Scene TheStool(int stool) => new(
        "counter:stool:alone",
        StoolPlate(stool),
        Setting,
        TookAStoolLine,
        [
            new(Wait, LabelOf(Wait)),
            new(GetDown, LabelOf(GetDown), Says: GotDownLine),
        ]);

    /// <summary>
    /// #756 · THE NEIGHBOUR, as an <see cref="Encounter.Scene"/> — a three-rung ladder that starts one rung
    /// higher than #757's, because the chair was never in question.
    ///
    /// <para>Every rung is <see cref="Encounter.Requirement.ReplyToPriorMove"/>, so nothing is on the panel
    /// before the sentence it answers has been spoken (#749). The drink is a REPLY to her having said who
    /// she is, and the ask needs the drink ANSWERED either way — turning a round down is still having heard
    /// yourself decline it.</para>
    /// </summary>
    public static Encounter.Scene TheNeighbour() => new(
        "counter:stool:neighbour",
        NeighbourPlate,
        Setting,
        NeighbourOpening,
        [
            new(AnswerHer, LabelOf(AnswerHer), Says: AnsweredHerLine),
            new(LetItLie, LabelOf(LetItLie), Says: LetItLieLine),

            new(StandHerOne, LabelOf(StandHerOne),
                Encounter.Requirement.ReplyToPriorMove, After: AnswerHer,
                Credits: StandHerOnePrice, Says: StoodHerOneLine),
            new(KeepYourCoin, LabelOf(KeepYourCoin),
                Encounter.Requirement.ReplyToPriorMove, After: AnswerHer, Says: KeptYourCoinLine),

            // …and it is the ONE rung the field book keeps. The note rides the move (#757's addition to
            // Encounter.Move), so no client author has to remember which sentence was worth writing down.
            new(AskHerWeek, LabelOf(AskHerWeek),
                Encounter.Requirement.ReplyToPriorMove, After: StandHerOne, OrAfter: KeepYourCoin,
                Says: HerWeekLine, Note: HerWeekNote),

            new(GetDown, LabelOf(GetDown), Says: GotDownLine),
        ]);

    /// <summary>Every line this file can put on a screen, for the canon grep. The guard walks THIS, so a
    /// line added tomorrow is checked tomorrow — <see cref="SittingAlone.AllProse"/>'s own idiom.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string s in NobodyBesideYou)
        {
            yield return s;
        }
        foreach (string s in NeighbourSaysNothing)
        {
            yield return s;
        }
        foreach (string s in CounterIsQuiet)
        {
            yield return s;
        }
        yield return TookAStoolLine;
        yield return GotDownLine;
        yield return RowIsFullLine;
        yield return NeighbourOpening;
        yield return AnsweredHerLine;
        yield return LetItLieLine;
        yield return StoodHerOneLine;
        yield return KeptYourCoinLine;
        yield return HerWeekLine;
        yield return HerWeekNote;
        yield return NeighbourPlate;
        yield return Setting;
        yield return SeatedArtCaption;
    }
}
