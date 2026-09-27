using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE SCENE AND THE ANSWER — the encounter a table is dealt as, the stranger's version of it, and
/// every answer a move at the table gets back.
///
/// <para>Split out of <c>CanteenTable.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class CanteenTable
{
    // ── THE SCENE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the setting is called, for <see cref="Encounter.Scene.Setting"/>. The room's own sign
    /// (#707) says what it is; this says where in it you are.</summary>
    public const string Setting = "a table in the upper canteen";

    /// <summary>
    /// #746 · The table, as an <see cref="Encounter.Scene"/>.
    ///
    /// <para>This is the file's real claim: the proto is CONTENT. Everything the panel needs — which moves
    /// exist, what each costs, which are rolled — is data on the framework's own types, and a guard stop will
    /// be another function exactly like this one.</para>
    /// </summary>
    /// <param name="who">Which regular.</param>
    /// <param name="overheard">Whether the Temp overheard the Hand wave you off — the NO-AND's third effect.
    /// It relaxes their second line's requirement rather than granting it, because the line is still
    /// something you have to sit down and ask for.</param>
    public static Encounter.Scene SceneFor(Who who, bool overheard = false)
    {
        var moves = new List<Encounter.Move>
        {
            new(SmallTalk, LabelOf(SmallTalk)),
        };

        // The second line. For the Hand and the Fitter it is simply the next thing they say if you stay; for
        // the Temp it is the house's ways, and it is bought with a round — unless the scene already moved.
        moves.Add(SecondLineIsEarned(who) && !overheard
            ? new(SmallTalkAgain, LabelOf(SmallTalkAgain), Encounter.Requirement.PriorMoveThisWatch, After: Round)
            : new(SmallTalkAgain, LabelOf(SmallTalkAgain), Encounter.Requirement.PriorMoveThisWatch, After: SmallTalk));

        moves.Add(new(Round, LabelOf(Round), Encounter.Requirement.Credits, RoundPrice));

        // No KIND named: the gesture is "put something on the table", and which something is the next press.
        moves.Add(new(Show, LabelOf(Show), Encounter.Requirement.SatchelItem));

        // The Hand's is the only rolled move at this table. The Fitter's is honest work offered plainly, and
        // the Temp has none at all — which is not a gap, it is who they are in their first week.
        if (who == Who.Hand)
        {
            moves.Add(new(Work, LabelOf(Work), Rolled: true));
        }
        else if (who == Who.Fitter)
        {
            // #749 · THE ANSWERS ARE ANSWERS. Both of these are replies to one sentence — "South face
            // scaffold, four watches, pay at the end of each" — and until he has said it there is nothing to
            // reply to, so they are not in the panel at all. They used to be shown greyed with "Not yet." on
            // them, which is a menu with two items you may not order (found by playing it, #749).
            moves.Add(new(Work, LabelOf(Work), Says: FitterWorkLine));
            moves.Add(new(TakeScaffold, LabelOf(TakeScaffold),
                Encounter.Requirement.ReplyToPriorMove, After: Work));
            moves.Add(new(DodgeScaffold, LabelOf(DodgeScaffold),
                Encounter.Requirement.ReplyToPriorMove, After: Work, Says: ScaffoldDodgedLine));
        }

        // Last, and free, and on every scene this file will ever build.
        moves.Add(new(Leave, LabelOf(Leave), Says: LeaveLine));

        return new Encounter.Scene(
            $"canteen:table:{who}".ToLowerInvariant(),
            PlateOf(who),
            Setting,
            WaveIn(who),
            moves);
    }

    /// <summary>
    /// #751 · A STRANGER'S TABLE — the thin scene, on the very same machine.
    ///
    /// <para>Three moves and no fourth. Small talk (their bark, drawn by <see cref="CanteenRegulars"/> per
    /// patron per watch), buy the round (the +1 applies exactly as it does anywhere — a stranger's table is
    /// where you warm up cheap), and take your leave. <b>Ask-about-work is not on it</b>, and that is the
    /// design rather than an omission: a hall where every one of eighty faces has a job to hand out is a
    /// quest hub, and the room's whole job is to be a room.</para>
    ///
    /// <para>Everything about it is <see cref="Encounter"/>'s — which is the claim this file has been making
    /// since #746, now tested by a second kind of counterpart existing at all.</para>
    /// </summary>
    /// <param name="plate">Who they read as, as the deck drew them.</param>
    public static Encounter.Scene StrangerScene(string plate) => new(
        $"canteen:table:{Who.Stranger}".ToLowerInvariant(),
        plate ?? "",
        Setting,
        WaveIn(Who.Stranger),
        [
            new(SmallTalk, LabelOf(SmallTalk)),
            new(Round, LabelOf(Round), Encounter.Requirement.Credits, RoundPrice),
            new(Leave, LabelOf(Leave), Says: LeaveLine),
        ]);

    /// <summary>#751 · One line of a stranger's day: the bark they were dealt this watch, and nothing
    /// else — no state, no modifier, no memory. It is a room being a room.</summary>
    public static Answer StrangerSaid(string bark) => new(bark ?? "");

    /// <summary>The plate this counterpart reads as, without the glyph.</summary>
    public static string PlateOf(Who who) => who switch
    {
        Who.Hand => HandPlate,
        Who.Fitter => FitterPlate,
        Who.Temp => TempPlate,
        _ => "",
    };

    // ── THE ANSWER ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// What one move did: the words, and every consequence, as data. The client applies it; nothing about
    /// what a band GRANTS is decided in a razor file, so a guard can force a band and pin the state.
    /// </summary>
    /// <param name="Line">What is said. Rendered inside the panel (#680), never only pulsed.</param>
    /// <param name="Band">Which band it settled in. <see cref="Encounter.Band.Yes"/> for unrolled moves.</param>
    /// <param name="GrantsChit">The day-labour chit goes in the wallet.</param>
    /// <param name="UnderAnotherName">…and the name in the book is not one you gave.</param>
    /// <param name="OpensFitter">The fitter's own ask about work is now on offer.</param>
    /// <param name="HardensTable">This table is harder for the rest of the watch (−1 on further asks).</param>
    /// <param name="ArmsTheTemp">The temp overheard it, and their second line is available without a round.</param>
    /// <param name="ClosesTheAsk">Ask-about-work is shut at this table for this watch.</param>
    /// <param name="TeachesTheHouse">The house's ways are learned (+1 on the Hand's ask).</param>
    /// <param name="NervePips">Whole pips spent, through <see cref="NervePips"/> and nothing else.</param>
    /// <param name="Note">What the field book keeps, or null when the moment is not worth filing.</param>
    public readonly record struct Answer(
        string Line,
        Encounter.Band Band = Encounter.Band.Yes,
        bool GrantsChit = false,
        bool UnderAnotherName = false,
        bool OpensFitter = false,
        bool HardensTable = false,
        bool ArmsTheTemp = false,
        bool ClosesTheAsk = false,
        bool TeachesTheHouse = false,
        int NervePips = 0,
        string? Note = null);

    /// <summary>
    /// #746 · THE HAND'S ASK, resolved. Three bands, three granted states, and the scene moves in all three.
    ///
    /// <para>The nerve cost is <see cref="Encounter.NervePipsFor"/> — the framework's, not a number invented
    /// here — so a table and a checkpoint charge fear the same way.</para>
    /// </summary>
    public static Answer HandAsksAboutWork(Encounter.Band band) => band switch
    {
        Encounter.Band.Yes => new(
            HandWorkYes, band,
            GrantsChit: true,
            ClosesTheAsk: true,
            NervePips: Encounter.NervePipsFor(band),
            Note: ChitGist),

        Encounter.Band.YesBut => new(
            HandWorkYesBut, band,
            GrantsChit: true,
            UnderAnotherName: true,
            ClosesTheAsk: true,
            NervePips: Encounter.NervePipsFor(band),
            Note: ChitUnderAnotherNameGist),

        // NO — AND. The refusal is the busiest outcome in the file, which is exactly what "the scene moves"
        // has to mean if it means anything: a door shuts, a different door opens, and a third person in the
        // room now has something to say that they did not have a minute ago.
        _ => new(
            HandWorkNoAnd, band,
            OpensFitter: true,
            HardensTable: true,
            ArmsTheTemp: true,
            NervePips: Encounter.NervePipsFor(band),
            Note: null),
    };

    /// <summary>The Fitter's ask. Never rolled, and it is not a refusal to leave it on the table.</summary>
    public static Answer FitterAsksAboutWork() => new(FitterWorkLine);

    /// <summary>Taking the scaffold job.</summary>
    public static Answer ScaffoldTaken() => new(FitterWorkLine, Note: ScaffoldTakenNote);

    /// <summary>
    /// #749 · THE ANSWER TO A MOVE WHOSE OUTCOME IS FIXED: what it says, and nothing else.
    ///
    /// <para>The framework's <see cref="Encounter.Move.Says"/> — "the outcome is FIXED, and here is the line
    /// it is fixed to" — made into this file's <see cref="Answer"/>. It exists so a panel can hand back a
    /// reply it has never heard of: the client that presses a move looks at what the move CARRIES rather than
    /// at a list of ids somebody remembered to write down, which is how the polite dodge came to be the only
    /// free move in the game that spoke.</para>
    ///
    /// <para><b>THE POLITE DODGE IS THIS, and the law it carries is the default of every other field:
    /// it costs nothing and changes nothing.</b> The owner smoke-tests it by hand ("politely dodge the jobs
    /// that don't"), and a dodge that quietly spent a pip or hardened a table would fail that test without
    /// anybody noticing for a month — so there is nowhere for a cost to be added except by hand, here, to
    /// every fixed outcome in the game at once.</para>
    /// </summary>
    /// <para>#757 · …and whatever the move says the BOOK should keep, by the same law and for the same
    /// reason: a content file names what is worth writing down, and no client author has to remember to.
    /// The dodge's is null, which is the nothing it has always cost.</para>
    public static Answer SaidPlainly(Encounter.Move move) => new(move.Says ?? "", Note: move.Note);

    /// <summary>Standing up. Free, and it files nothing.</summary>
    public static Answer TookTheirLeave() => new(LeaveLine);

    /// <summary>Buying the round.</summary>
    public static Answer BoughtTheRound() => new(RoundLine);

    /// <summary>One line of somebody's day. The only one that changes anything is the Temp's second, and what
    /// it changes is what YOU know — which is the modifier the Hand's ask reads.</summary>
    public static Answer MadeSmallTalk(Who who, bool second) =>
        new(second ? SmallTalkSecond(who) : SmallTalkFirst(who),
            TeachesTheHouse: TeachesTheHouse(who, second));

    /// <summary>
    /// Putting something on the table.
    ///
    /// <para>Three readings, in the order a room would have them: the file on somebody is LOUD and shuts the
    /// ask; the deep card silences the Hand and turns their ask into fear rather than friendship; everything
    /// else made of paper is weather on another moon.</para>
    /// </summary>
    /// <param name="item">What went on the table.</param>
    /// <param name="who">Who is sitting at it — the deep card only lands on the Hand, because they are the
    /// only one at this table who knows what it is worth.</param>
    /// <param name="quiet">#751 · Whether this table is in a CABINET. The LOUD reading of a file is not a
    /// fact about the paper, it is a fact about the ROOM — <i>"the counter has eyes"</i> — so in a room with
    /// no line of sight to the counter it does not apply. See <see cref="QuietLine"/>.</param>
    public static Answer PutOnTheTable(Satchel.Item item, Who who, bool quiet = false)
    {
        if (item.Kind == Satchel.Kind.Dirt)
        {
            // ── #751 · THE QUIET RULE ────────────────────────────────────────────────────────────────
            //
            // Owner: "have cabinet-spaces for sensitive negotiations." This is what makes one — not a
            // label on a door, but the one mechanic in the game that a room can switch off.
            //
            // #746's LOUD closure has always been about the counter rather than about the slip: "not
            // because I care — because the counter has eyes and you just taught it your face." A cabinet
            // is a room the counter cannot see, so the sentence does not apply in it, and NOTHING here
            // closes. The player is never told; they put a file down in a cabinet one day and the ask is
            // still there afterwards, and the card they read on the way in already said why.
            if (quiet)
            {
                return new(QuietLine, Note: CabinetLeverageNote);
            }

            // The line IS the note. Inventing a second sentence about the same slip would be two voices
            // describing one event, and the one the captain heard is the one worth keeping.
            return new(DirtLine, ClosesTheAsk: true, Note: DirtLine);
        }

        if (who == Who.Hand && IsOverqualifying(item))
        {
            // Fear, not friendship. Nothing is granted HERE — the card does not hand you a chit, it settles
            // the ask you have not made yet (see ForcesTheYesBut), which is why this answer changes no state
            // and only speaks. A move that both spoke and granted would be two beats on one press.
            return new(DeepCardLine, Encounter.Band.YesBut);
        }

        return new(OtherPaperLine);
    }

    /// <summary>Does what is on the table read as relevant — the +1 in <see cref="Encounter.Situation"/>?
    /// Only the card the Hand cannot look away from. A shipping manifest is paper; it is not leverage.</summary>
    public static bool CountsAsPaperOnTheTable(Satchel.Item item, Who who) =>
        who == Who.Hand && IsOverqualifying(item);

    /// <summary>#746 · Does putting THIS card down settle the Hand's ask without a roll? The card does not
    /// persuade them; it frightens them into writing you down. Auto-resolved as YES-BUT, because being
    /// afraid of you is exactly a yes that costs something.</summary>
    public static bool ForcesTheYesBut(Satchel.Item item, Who who) => CountsAsPaperOnTheTable(item, who);
}
