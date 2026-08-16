using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #784 · POSTURE IS STATE — what sitting down stops you doing, and what it is the only way to do.
///
/// <para>Owner, live 2026-08-08, over the #778 table: <i>"before moving I have to stand up… so if I try to
/// move when sitting down it should ask with a pop-up whether I want to stand up again."</i> And, the same
/// minutes, the law that gives the chair a job: <i>"writing things down requires sitting down to be properly
/// done."</i></para>
///
/// <h3>Two rules, one fact</h3>
///
/// <list type="number">
/// <item><b>Sitting constrains movement honestly.</b> WASD in a chair does not slide the chair around the
/// hall. It asks. One press stands you up and the table is gone; the cancel keeps your seat. This is not
/// friction for its own sake — a seat that could be nudged away by a stray key would throw away the watch
/// you spent being findable (#757) and, since #784, the rest you spent being sat down
/// (<see cref="ShortRest"/>).</item>
/// <item><b>The field book has two registers and posture is the gate between them.</b> STANDING, the book
/// takes what it has always taken: the automatic gist-once jot the game files when something is found — a
/// scrawl on a moving knee, which is #696's own idiom and is deliberately untouched by this file.
/// SEATED AT A TABLE is where writing is properly done, and a deliberate entry attempted on your feet is
/// REFUSED OUT LOUD rather than quietly missing (#603).</item>
/// </list>
///
/// <para>Pure: a predicate and the words. Which client state means "seated" is the client's business, and
/// this file must never learn what a table is.</para>
/// </summary>
public static class SeatedPosture
{
    // ── STANDING UP IS A DECISION ─────────────────────────────────────────────────────────────────────

    /// <summary>What the confirm asks, in the plainest words there are. #782: a label is text that must
    /// READ, and #783's own ruling one lane over — a verb that needs the design doc to parse is wrong. It
    /// names the price in the same breath as the question, because the price is the entire reason the
    /// question exists.</summary>
    public const string StandUpAsk = "Stand up? You will lose the table.";

    /// <summary>…and what a captain who is mid-rest is told underneath it. Only shown when there is
    /// something to lose, so it never reads as boilerplate.</summary>
    public const string StandUpCostsTheRest = "You are getting your breath back sitting there.";

    /// <summary>The one press that does it.</summary>
    public const string StandUpYes = "Stand up";

    /// <summary>…and the one that does not. Esc says the same thing with a key.</summary>
    public const string StandUpNo = "Stay where you are";

    /// <summary>What is said once the chair goes back. <see cref="SittingAlone.StoodUpLine"/> is the table
    /// scene's own goodbye and this is deliberately not a second copy of it — the client says that one.
    /// This is the line for a captain who stood up in order to WALK, which is a different sentence.</summary>
    public const string StoodUpToWalkLine = "You stand, and the chair goes back under the table. Off you go.";

    // ── #865 · THE SIT BEAT ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #865 · HOW LONG THE CAPTAIN IS OWED THE SIGHT OF THEMSELVES TAKING THE CHAIR, in real seconds.
    ///
    /// <para>Owner, 2026-08-13, at a canteen table on B1: <i>"I sat down the table but the pop ups blocked
    /// my view of my avatar sitting down"</i> — and on the retry, when nothing covered it: <i>"Oh now it
    /// picked the chair and it worked."</i> #820/#846's snap onto the seat is a tiny animation the player is
    /// MEANT TO WATCH; it is the whole payoff of both issues, and one modal raised over it un-ships them.</para>
    ///
    /// <para>A number and not a frame count, because frames are whatever the tab is giving today and the
    /// promise is about a MOMENT the player gets to see. "A beat or two" in the owner's words, in the room's
    /// own tempo. FLAGGED for the owner's tuning.</para>
    ///
    /// <para>Nothing is DROPPED for it. Every card this holds off is latched by its own poll or held in the
    /// story seam's queue, so the beat delays and never swallows — which is what makes the rule safe to make
    /// unconditional.</para>
    /// </summary>
    public const double SitBeatSeconds = 1.2;

    // ── THE FIELD BOOK'S TWO REGISTERS ────────────────────────────────────────────────────────────────

    /// <summary>Whether deliberate authorship is allowed right now. One predicate, and it is this trivial on
    /// purpose: the day the answer grows a second clause, every caller gets the new clause for free.</summary>
    public static bool CanWriteProperly(bool seated) => seated;

    /// <summary>
    /// #784 · WHY NOT, said out loud on the control that refused. Owner's law in the game's own mouth: the
    /// standing register exists and is not being taken away — it is just not WRITING, and the sentence says
    /// which is which so a captain knows what to do about it.
    /// </summary>
    public const string NotOnYourFeetLine =
        "Not on your feet. Standing up you can photograph a thing and leave it, and that is all the book " +
        "gets — a scrawl on a moving knee. Sit down at a table and it can be written properly.";

    /// <summary>The refusal, or null when the captain is sitting and the answer is yes. Callers ask for the
    /// refusal rather than for permission, so a control that means to explain itself cannot forget to.</summary>
    public static string? RefusalIfStanding(bool seated) =>
        CanWriteProperly(seated) ? null : NotOnYourFeetLine;

    /// <summary>What the deliberate control is called where it is drawn. A pen, and never the camera
    /// (<see cref="Processing.Glyph"/>) — the standing register is the photograph and this is the other
    /// thing, and a glyph that said one while the sim did the other is the class of lie #562 was filed
    /// on.</summary>
    public const string WriteGlyph = "✍";

    /// <summary>The hint on that control while it is live.</summary>
    public const string WriteHint = "Write it up properly — the book keeps it and you keep the paper";

    /// <summary>What a properly written entry reads like when it lands in the book. The gist is the same
    /// gist the standing register files (<see cref="LeftBehind.GistOf"/>) — what a table buys is not a
    /// better fact, it is the sheet still being in your pocket afterwards.</summary>
    public static string WrittenUpLine(string what, string gist)
    {
        System.ArgumentNullException.ThrowIfNull(what);
        System.ArgumentNullException.ThrowIfNull(gist);
        return $"{WriteGlyph} You spread {what} out on the table and copy it into the book in your own " +
            $"hand. The sheet goes back in the sleeve. {gist}";
    }

    /// <summary>And what it is once the book already has it. Doing it twice is not a cost and not a bug; it
    /// is simply nothing, and being told so beats a control that answers nothing (#603).</summary>
    public const string AlreadyWrittenLine =
        "You have already written that one up. The book has it in your own hand and the sheet is still " +
        "yours.";

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return StandUpAsk;
        yield return StandUpCostsTheRest;
        yield return StandUpYes;
        yield return StandUpNo;
        yield return StoodUpToWalkLine;
        yield return NotOnYourFeetLine;
        yield return WriteHint;
        yield return AlreadyWrittenLine;
    }
}
