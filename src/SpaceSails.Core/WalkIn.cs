using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #973 L5b · THE WALK-IN — the stunning lady with a sad little story, and the only reason in this game that
/// is not money.
///
/// <para><b>Owner, 2026-08-23 (Addendum 3):</b> <i>"in the old Bogart pictures the stunning lady walks into
/// the office with a sad little story and a job, and the detective ends up doing something dangerous because
/// of her. Our gumshoe has no office — but it gives a motive other than money for a short-distance quest. A
/// note with our own 'we'll always have…' line. And it is the perfect excuse to put NPC walking (A*) on show:
/// that story is never found sitting down — she makes an entrance at some classy place (a jazz bar), crosses
/// the room, and comes to our table when we are alone, and asks."</i></para>
///
/// <h3>What is in this file, and what deliberately is not</h3>
///
/// <para>IN: <b>who she is, what she says, what the job is worth, and whether she is lying</b> — all of it
/// pure, all of it a function of the thread's seed and the room. Every sentence is Fable's and is written down
/// once, here, so the card, the ledger row, the note in the book and the plate over her head cannot come to
/// four different views of one woman.</para>
///
/// <para>NOT IN: the WALK. She comes out of a back-room leaf and crosses the floor on
/// <c>Map.BarWalkers.ApproachTheTable</c> — #973 L0's one hook — and sits down through <c>TakeThisSeat</c>
/// like the other seven sittings in the game. This file knows nothing about doors, lattices or chairs, which
/// is why it can be asked the same questions from a test with no room in it.</para>
///
/// <h3>The two of her</h3>
///
/// <para>By seed: <b>Ilse Marrow</b> when the thread cast her and posted her at THIS great port (L5a's
/// claims desk — she knew the old face, and the note she leaves names a page the service filed against you),
/// or <b>Nadia Kell</b>, a stranger the bar has never seen, which is the pure Bogart walk-in. The owner's
/// ruling 16 is <i>yes, by seed</i>, and the seed decides nothing else about her: her lines, her job and her
/// note are the ones a captain gets.</para>
///
/// <h3>Femme fatale by rule (ruling 17)</h3>
///
/// <para><b>One walk-in in three is a setup.</b> Not a twist somebody wrote: a roll on the shared dice, made
/// once per thread per woman, so the same universe tells the same story twice. The card says NOTHING about it
/// until the SPREAD has actually found it out (<see cref="SetupCardLine"/> answers null until then) — a grey
/// line that appeared for free would be the game warning the player about a scene the game is supposed to let
/// them walk into. Completing a setup job is allowed and dangerous, as designed.</para>
/// </summary>
public static class WalkIn
{
    /// <summary>Which of the two crossed the floor. The fling knew the old face; the stranger has never seen
    /// it, which is the difference the whole scene turns on.</summary>
    public enum Who
    {
        /// <summary>L5a's fling — Ilse Marrow, on a Nebula Mutual claims desk at a great port.</summary>
        Ilse = 0,

        /// <summary>The pure walk-in: a woman with a name, a brother and nothing else.</summary>
        Nadia = 1,
    }

    /// <summary>How rare a walk-in is: one docked visit in this many is even ELIGIBLE for one, before the
    /// once-per-subject latch and the seated-and-alone gate have said anything. Owner's cadence word for the
    /// beat is "rare", and #664 keeps the rest of it.</summary>
    public const int VisitsBetweenWalkIns = 7;

    /// <summary>One walk-in in this many is a setup. The owner's own fraction (ruling 17).</summary>
    public const int SetupInThree = 3;

    /// <summary>#973 L5b · The stranger's contact id. Ilse's is <see cref="OldCrew.FlingId"/>'s ledger id —
    /// she is an old shipmate first and a walk-in second, and two books on one woman is this repo's first
    /// named bug class with a face.</summary>
    public const string NadiaContactId = "walkin-nadia-kell";

    /// <summary>The plate the deck draws over her while she crosses the floor. The same shape the salesman's
    /// is (<see cref="NebulaRep.Plate"/>), because a plate is what the room calls somebody and the room does
    /// not know she is a story beat.</summary>
    public static string Plate(Who who) => "◈ " + Name(who).ToUpperInvariant();

    /// <summary>#973 L5b · Her portrait — the painting her card draws and the canvas the beat names. One per
    /// woman: a shared picture would be one woman with two names, which is the opposite of the point.</summary>
    public static string PortraitArt(Who who) =>
        who == Who.Ilse ? "art/walk-in-ilse-marrow.jpg" : "art/walk-in-nadia-kell.jpg";

    /// <summary>…and the same, off the SUBJECT the beat was raised with, for the seam. An unrecognised
    /// subject falls to the stranger rather than to an empty string: a card with no picture on it is a worse
    /// answer than a card with the wrong one, and every raise in the game passes a real subject.</summary>
    public static string PortraitArt(string? subject) =>
        PortraitArt(string.Equals(subject, Subject(Who.Ilse), StringComparison.Ordinal) ? Who.Ilse : Who.Nadia);

    /// <summary>Both canvases, for the manifest sweep — the one that is not on the screen is still a file
    /// somebody has to paint.</summary>
    public static IReadOnlyList<string> AllPortraits => [PortraitArt(Who.Ilse), PortraitArt(Who.Nadia)];

    /// <summary>Her name, as it goes in the book from the moment she sits.</summary>
    public static string Name(Who who) => who == Who.Ilse ? "Ilse Marrow" : "Nadia Kell";

    /// <summary>Which book she is written in. Ilse's is the old crew's, already open.</summary>
    public static string ContactId(Who who) => who == Who.Ilse ? OldCrew.LedgerId(OldCrew.FlingId) : NadiaContactId;

    /// <summary>#973 L5b · The subject the beat is filed under — HER, because the cadence is once per subject
    /// and the subject is the woman. Two walk-ins by two different women in one universe are two moments; the
    /// same woman twice is not.</summary>
    public static string Subject(Who who) => who == Who.Ilse ? OldCrew.FlingId : "nadia";

    /// <summary>#973 L5b · The subject the unfinished line is filed under. One id for both women: the sentence
    /// is the captain's and not hers, and a captain does not get to feel it twice.</summary>
    public const string SinceSubject = "since";

    // ── THE ROOM NOTICES ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Said once, at her entrance, before the captain has looked up. Fable's line, verbatim.</summary>
    public const string TheRoomLooks = "The room looks at the door before you do.";

    // ── WHAT SHE SAYS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Her first line at the table — the one said before the captain can stand up.</summary>
    public static string AtTheTable(Who who) => who == Who.Ilse
        ? "Don't get up. I won't stay. — I need something found, and I can't be the one who asks for it."
        : "You're the one they said would be alone. — I have a small thing and nobody to take it to.";

    /// <summary>The sad little story, which is also the whole of the brief.</summary>
    public static string TheStory(Who who) => who == Who.Ilse
        ? "A berth listing under a name that isn't hers any more. The REACH — they renamed her. I want to "
          + "know where she's tied up and who holds the paper. Not for the company. For me."
        : "My brother filed a claim and then stopped writing. The desk says the file is in order. I want "
          + "somebody to find the man, not the file.";

    /// <summary>The captain's two buttons. Two, and never three: there is nothing to bargain about.</summary>
    public const string Yes = "Yes — I'll find it";

    /// <summary>…and the other one.</summary>
    public const string No = "No";

    /// <summary>What she says when the answer is no. She leaves; there is no note, no job, and she does not
    /// ask again this visit.</summary>
    public static string IfNo(Who who) => who == Who.Ilse
        ? "Then I was wrong about the walk."
        : "No. Of course. — Sorry to have sat.";

    // ── THE NOTE SHE LEAVES ──────────────────────────────────────────────────────────────────────────────

    /// <summary>#973 L5b · Her note, left on the table when she goes — a held memory marked <i>hers</i> and
    /// tagged <i>love</i>, in her hand. The first sheet in the book that is evidence of ANOTHER PERSON'S
    /// MEMORY OF YOU, which is why the mark matters more here than anywhere else in the arc.
    ///
    /// <para>Ilse's names the object on the summer-party page (the banner, the fleet-day) — which is how the
    /// captain learns that page was FILED, because somebody else can still read it.</para></summary>
    public static string NoteText(Who who) => who == Who.Ilse
        ? "We still have the fleet-day."
        : "You said yes before I finished. Thank you for that. — N.";

    /// <summary>The id her note is filed under in the book. One per woman, so a thread that meets both keeps
    /// two sheets and a thread that meets one twice keeps one.</summary>
    public static string NoteId(Who who) => "walkin-note:" + Subject(who);

    // ── THE UNFINISHED LINE ──────────────────────────────────────────────────────────────────────────────

    /// <summary>#973 L5b · The line that fires at the job's FIRST site and DOES NOT FINISH, because the page
    /// it ends on is grey. Owner's own sentence, and the em-dash is the point of it.</summary>
    public const string Unfinished = "I haven't felt like this since —";

    /// <summary>…and how it ends, once the job is done or her note has been laid beside the right document.
    /// Two endings, one per woman, and neither of them is about the job.</summary>
    public static string Finished(Who who) => who == Who.Ilse
        ? "— since the rail went cold."
        : "— since somebody last asked me for something and meant it.";

    /// <summary>
    /// #973 L5b · <b>DOES THE SENTENCE END YET?</b> — the predicate L3 spends and this file owns.
    ///
    /// <para>Ruling 18: the line does not finish until the job or the note finishes it. Both halves are
    /// arguments rather than fields for the reason the whole arc is built this way — the RECONCILE is L3's
    /// machinery (two sheets laid together on the SPREAD) and the job's state is the quest ledger's, and a
    /// predicate that reached for either would be this file guessing at a surface it does not own.</para>
    /// </summary>
    /// <param name="jobCompleted">Whether the FIND she asked for is done.</param>
    /// <param name="noteReconciled">Whether her note has been laid beside <see cref="ReconcilesAgainst"/> on
    /// the SPREAD and agreed. L3 answers this; until L3 ships it is always false, and the job still
    /// finishes the sentence on its own.</param>
    public static bool SinceFinishes(bool jobCompleted, bool noteReconciled) => jobCompleted || noteReconciled;

    /// <summary>
    /// #973 L5b · <b>WHAT HER NOTE HAS TO BE LAID BESIDE</b> — the sheet id L3's SPREAD reconciles against,
    /// named here so the two lanes cannot come to two answers about one pairing.
    ///
    /// <para>Ilse's note names the fleet-day, so it reconciles against the summer-party page — the one page
    /// the filing line cannot grey, because the service filed it (L5a). Nadia has no shared past at all, so
    /// hers reconciles against the first slip her own job produces.</para>
    /// </summary>
    public static string ReconcilesAgainst(Who who, string jobId) =>
        who == Who.Ilse ? OldCrewScene.SummerPartyId : FirstSlipId(jobId);

    /// <summary>The sheet the walk-in job's first site puts in the book — the paper the captain actually
    /// finds. One per job, so two walk-ins do not overwrite each other's evidence.</summary>
    public static string FirstSlipId(string jobId) => "walkin-slip:" + (jobId ?? "");

    // ── FEMME FATALE BY RULE ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · <b>IS THIS ONE A SETUP?</b> One in three, on the shared dice, seeded on the thread and on
    /// HER — so the same universe tells the same story twice, and the two women can differ.
    /// </summary>
    public static bool IsASetup(string threadId, Who who) =>
        DiceRule.Roll(DiceRule.Seed($"walkin|setup|{threadId ?? ""}|{Subject(who)}"), SetupInThree).Face == 1;

    /// <summary>
    /// #973 L5b · <b>THE GREY LINE ON THE JOB CARD, OR NOTHING AT ALL.</b>
    ///
    /// <para>It says nothing until the SPREAD has found it out. That is the whole of ruling 17's second half:
    /// the player may discover the setup before the end by laying her note beside a money-tagged slip, or may
    /// simply go — and a card that warned them for free would delete the choice the owner asked for.</para>
    /// </summary>
    /// <returns>The line, or null while the job card must stay silent.</returns>
    public static string? SetupCardLine(bool isASetup, bool revealed) =>
        isASetup && revealed ? "a setup — you can still go" : null;

    /// <summary>What the SPREAD says when her note and the desk's paper turn out to be one hand. L3 renders
    /// it; it is written here because it is a sentence about HER.</summary>
    public const string SameHandLine = "Her hand and the desk's hand are the same hand.";

    // ── WHO CROSSES THE FLOOR ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 L5b · <b>WHO IT IS, BY SEED.</b> Ilse when the thread cast her AND posted her at this very berth
    /// (L5a's <c>OldCrew.FlingIsAt</c>); otherwise the stranger.
    ///
    /// <para>Deliberately NOT a roll. The strongest version of this scene is the one where the woman crossing
    /// the room already knew the face, and that is a fact about the world the thread has already decided — a
    /// die thrown on top of it could only ever take the better story away.</para>
    /// </summary>
    public static Who Cast(bool flingIsPostedHere) => flingIsPostedHere ? Who.Ilse : Who.Nadia;

    /// <summary>
    /// #973 L5b · <b>IS A WALK-IN EVEN POSSIBLE THIS VISIT?</b> — the rarity half, and the only half this
    /// file decides. Whether the captain is SEATED and ALONE, and whether the venue is classy, are questions
    /// about a room and are asked by the room.
    /// </summary>
    /// <param name="threadId">The game thread's id — the per-universe seed.</param>
    /// <param name="stationId">The berth being visited.</param>
    /// <param name="visitIndex">How many dock visits this thread has made, counting from zero.</param>
    public static bool CouldWalkInThisVisit(string threadId, string stationId, int visitIndex)
    {
        if (visitIndex < 0)
        {
            return false;
        }

        int slot = (int)(DiceRule.Seed(0UL, $"walkin|rota|{threadId ?? ""}|{stationId ?? ""}")
            % (ulong)VisitsBetweenWalkIns);
        int since = visitIndex - slot;
        int r = since % VisitsBetweenWalkIns;
        return (r < 0 ? r + VisitsBetweenWalkIns : r) == 0;
    }

    // ── THE JOB ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The job's title on the offer card and in the ledger. FABLE-authored, per woman.</summary>
    // FABLE: line needed — the job card's TITLE for each woman, in the ledger's own register (the issue gives
    // her spoken lines and the note, not the row the book files them under). Placeholders below are plain and
    // deliberately flat so nobody mistakes them for authored copy.
    public static string JobTitle(Who who) => who == Who.Ilse
        ? "A berth listing"          // FABLE: placeholder
        : "A claimant";              // FABLE: placeholder

    /// <summary>What the captain is being asked to find, named. Ilse's is the ship she will not say the name
    /// of out loud; Nadia's is her brother.</summary>
    public static string TargetName(Who who) => who == Who.Ilse
        ? OldCrew.TheDecentShip
        // FABLE: line needed — the brother's NAME. The issue says "a named claimant" and does not name him.
        : "the claimant";            // FABLE: placeholder

    /// <summary>#973 L5b · The ledger's own theory tag for this job. <b>Love, by construction</b> — the first
    /// job in the game that is, and the reason the owner asked for the axis at all.</summary>
    public const HeldMemory.Theory Theory = HeldMemory.Theory.Love;

    /// <summary>Every line this file can say, for the guard that asserts none of them says "copy" and none of
    /// them names what is in the pods.</summary>
    public static IEnumerable<string> EveryLine()
    {
        yield return TheRoomLooks;
        yield return Yes;
        yield return No;
        yield return Unfinished;
        yield return SameHandLine;
        yield return SetupCardLine(true, true) ?? "";
        foreach (Who who in new[] { Who.Ilse, Who.Nadia })
        {
            yield return Name(who);
            yield return Plate(who);
            yield return AtTheTable(who);
            yield return TheStory(who);
            yield return IfNo(who);
            yield return NoteText(who);
            yield return Finished(who);
            yield return JobTitle(who);
            yield return TargetName(who);
        }
    }
}
