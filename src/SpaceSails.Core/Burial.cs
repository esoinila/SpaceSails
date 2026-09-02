using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1063 · THE BURIAL — the first customer of <see cref="DisclosureClock"/>, and the watcher reaction that is
/// not the world declining but PEOPLE hiding the truth themselves.
///
/// <para>Owner (2026-09-01), on the creepiest shape the reaction could take: <i>"suppose the people them
/// selves were told really convincingly that they need to hide the truth… all the people suddenly were told
/// and obeyed like automatons them selves hiding the truth and then just forgot they did it… all that mud."</i>
/// And the register the whole beat is played in: <i>"the cheerful… of course we do a ton of work to raise our
/// streets for no apparent purpose is the cherry on the cake."</i></para>
///
/// <para>Between two visits, a ground the captain opened is GONE the way a street grade goes: filled, floored,
/// resurfaced. The neighbours did it. It was a work order. None of them remember the shovel, and all of them
/// sincerely know the ground was always this height. The procedure is the issue's, implemented in the order it
/// is written: <b>(1) remove the element, (2) remove its marks, (3) the town above keeps living, (4) one
/// specimen is kept.</b></para>
///
/// <para><b>THE SCULLY LAW (#672, binding).</b> Every burial reads as renovation to a reasonable person. No
/// card explains it; no stat is published; no sensor returns anything about it; there is no art of THEM. The
/// three authored lines below are a works notice, a maintenance ledger and a local paper being cheerful about
/// drainage, and each of them is a completely ordinary thing for a moon site to say. The word §8 reserves does
/// not appear in any of them, and a guard sweeps this type's strings for it.</para>
///
/// <para><b>LAW — THE BOOK NEVER LIES.</b> Nothing here removes or rewrites one field-book note, one clipped
/// story, one red thread or one satchel row. The burial takes the GROUND away and leaves the captain's record
/// of it untouched and correct, because without one fixed point the player has no floor to stand the horror
/// on. The book is the only witness, and it stays the only witness. Guarded in
/// <c>TheBurialTests.TheBookNeverLies</c>.</para>
/// </summary>
public static class Burial
{
    // ── THE THRESHOLD, AND ITS REASON WRITTEN BESIDE IT ──────────────────────────────────────────────────
    //
    // DisclosureClock's own docblock says what a customer owes it: "every beat that reads it chooses its own
    // threshold and writes that threshold's reason down beside its own words." These are those words.

    /// <summary>#1063 · How many WHOLE world-side windows must have passed since the ground was opened before
    /// the neighbours can have filled it in. <b>One</b>, and the reason is a work order rather than a number
    /// somebody liked: filling, flooring and resurfacing a set of galleries is a SHIFT of work, and a shift is
    /// the shortest thing the world's own clock can measure. A burial inside the window the captain opened the
    /// ground in would be a crew that was already standing there with the trucks running, which is a fact
    /// about THEM, and the moment the world can only be explained by them the Scully law is spent (#672).
    ///
    /// <para>Read off <see cref="DisclosureClock.WindowsSince(DisclosureClock.Opening, long)"/> and never
    /// re-derived — the window is the monolith's own, asked through the clock, so nothing here owns a second
    /// copy of a length this ground has paid for owning twice before.</para></summary>
    public const long WindowsBeforeFilling = 1;

    /// <summary>#1063 · Is this ground due to be filled in — <b>at this moment, ignoring where the captain is
    /// standing</b>. One whole window, per <see cref="WindowsBeforeFilling"/>.</summary>
    public static bool IsDue(DisclosureClock.Opening opening, long window) =>
        DisclosureClock.WindowsSince(opening, window) >= WindowsBeforeFilling;

    // ── THE REGISTER OF FILLED GROUNDS, AND WHY IT IS AMBIENT ────────────────────────────────────────────
    //
    // A burial changes the SHAPE of a site, and the shape of a site is asked by about thirty callers — the
    // lift panel, the remote, the sounder, the room carver, the sign writer, the audits, the renderer — none
    // of which has any business learning what a burial is. §13.15's second cause is a caller reasoning about
    // the shape of a building it does not own, and thirty callers each taught a new idea is that bug thirty
    // times. So the register is installed ONCE, by the one owner of the save, and consulted at the ONE seam
    // every one of those callers already goes through: UndergroundComplex.HasFoundBand.
    //
    // It is a list of body ids and nothing else. It carries no window, no reason and no time: a filled ground
    // is filled, and a record that kept WHY would be a record with an opinion in it — the same rule
    // DisclosureClock.Opening is written to.

    /// <summary>#1063 · The two registers as ONE object, so a reader can never see half of an install. The
    /// game is single-threaded and the guards are not: what is installed together must be readable together,
    /// and a reference assignment is the only write that is atomic without asking for a lock in a hot path.
    /// </summary>
    private sealed record World(
        IReadOnlyList<string> Filled, IReadOnlyList<DisclosureClock.Opening> Opened);

    private static World _world = new([], []);

    /// <summary>#1063 · The grounds that have been filled in. Empty in every world where nobody has been past
    /// a seam long enough ago, which is almost every world.</summary>
    public static IReadOnlyList<string> Filled => _world.Filled;

    /// <summary>#1063 · …and the grounds that have been OPENED, which is <see cref="DisclosureClock"/>'s own
    /// register, kept here as well because the works notice and the man on the works both have to know that
    /// the job exists BEFORE it is done. Installed by the same one writer, in the same call, so the two can
    /// never be a window out of step with each other.</summary>
    public static IReadOnlyList<DisclosureClock.Opening> Opened => _world.Opened;

    /// <summary>#1063 · Install the register — the ONE writer, called by whoever owns the save (the client, on
    /// load and on every fill). Null and empty are the same answer: nothing has been opened or buried.
    ///
    /// <para>Tests restore what they installed in a <c>finally</c>. Because the register only ever changes the
    /// answer for the ids IN it, a guard that installs a ground of its own cannot move any other guard's
    /// world — which is what makes an ambient safe here rather than merely convenient.</para></summary>
    public static void Install(
        IReadOnlyList<string>? filled, IReadOnlyList<DisclosureClock.Opening>? opened = null)
        => _world = new(filled ?? [], opened ?? []);

    /// <summary>#1063 · <b>ARE THE WORKS ON, ON THIS GROUND?</b> True from the moment a captain crosses the
    /// seam here, and true ever after.
    ///
    /// <para>This is the gate the notice and the man on the job are hung on, and it says something the game
    /// never states out loud: <b>the work order exists because the captain went down there.</b> A player who
    /// notices that the resurfacing notice was not on the board last time will have worked out the whole
    /// feature from a piece of paper about upper walks, which is the only way this may ever be worked out.
    /// The mundane reading survives intact and is not even strained — a site that has just had somebody go
    /// down a shaft nobody listed is exactly a site that is about to have its lower levels looked at.</para></summary>
    public static bool WorksAreOn(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return DisclosureClock.OpeningOf(_world.Opened, bodyId) is not null;
    }

    /// <summary>#1063 · Is the works notice up? <b>Between the opening and the fill</b>, which is the window
    /// the issue posts it in — the notice comes down when the job is done, the way a notice about a job that
    /// is finished comes down.</summary>
    public static bool NoticeIsUp(string bodyId) => WorksAreOn(bodyId) && !IsFilled(bodyId);

    /// <summary>#1063 · Has this ground been filled in?</summary>
    public static bool IsFilled(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // READ THE REFERENCE ONCE. The game is single-threaded and the guards are not: xUnit runs test
        // classes in parallel, and a walk that read `_filled.Count` and then `_filled[i]` took its length
        // from one register and its rows from the next one somebody installed — an IndexOutOfRange thrown out
        // of the site generator, from a guard that had nothing to do with any of this. Reference assignment
        // is atomic, so one local is the whole fix, and it is the honest shape anyway: a walk over the
        // register as it was when the question was asked.
        IReadOnlyList<string> filled = _world.Filled;

        // Deliberately a walk and not a set: the register is at most a handful of grounds in the longest
        // voyage anybody will ever play, and this is asked from inside the site generator's hot path, where
        // an empty list costs one length check and a hash set costs an allocation nobody needed.
        for (int i = 0; i < filled.Count; i++)
        {
            if (string.Equals(filled[i], bodyId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>#1063 · <b>THE EVENT.</b> Which of the grounds this captain has opened the neighbours have
    /// filled in by now — folded into the register, and the register handed back <b>by reference</b> when
    /// there is nothing to add, so a caller can compare and only then ask for a save.
    ///
    /// <para><b>Two conditions, and the second is the whole of the horror's manners:</b></para>
    /// <list type="number">
    /// <item>A whole window has passed since the opening (<see cref="WindowsBeforeFilling"/>) — a work order
    /// takes a shift.</item>
    /// <item><b>The captain is not on that body.</b> The neighbours do not fill a hall while the captain is
    /// standing in it: a crew that walked past him with the trucks would be a thing that happened TO him and
    /// therefore a thing he could describe, and the beat is that it happened while nobody was looking and
    /// nobody afterwards remembers doing it. It is also the only version that is fair — a ground that closed
    /// under a captain eleven floors down would be a death by book-keeping.</item>
    /// </list>
    ///
    /// <para>Nothing here is effort, a die, or a visit count: it is a fact about WHEN the captain went and
    /// where he is now, which is the clock's own law four (not farmable) carried into its first customer.</para>
    /// </summary>
    /// <param name="register">The disclosure clock's register of opened grounds.</param>
    /// <param name="filled">What has been filled in already.</param>
    /// <param name="standingOn">The body the captain is on right now, or null when he is on none.</param>
    /// <param name="simTime">Sim seconds — no clock is read in Core.</param>
    public static IReadOnlyList<string> Fill(
        IReadOnlyList<DisclosureClock.Opening>? register,
        IReadOnlyList<string>? filled,
        string? standingOn,
        double simTime)
    {
        IReadOnlyList<string> had = filled ?? [];
        if (register is not { Count: > 0 })
        {
            return had;
        }

        long window = DisclosureClock.WindowAt(simTime);
        List<string>? next = null;
        foreach (DisclosureClock.Opening opening in register)
        {
            if (!IsDue(opening, window))
            {
                continue;
            }
            if (string.Equals(opening.BodyId, standingOn, StringComparison.Ordinal))
            {
                continue;   // not while he is standing in it
            }
            if (Contains(had, opening.BodyId) || (next is not null && Contains(next, opening.BodyId)))
            {
                continue;   // already filled, and a ground is filled once
            }
            next ??= [.. had];
            next.Add(opening.BodyId);
        }
        return next ?? had;

        static bool Contains(IReadOnlyList<string> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }

    // ── THE EVIDENCE LIFECYCLE — THREE AUTHORED LINES, VERBATIM FROM #1063 ───────────────────────────────
    //
    // The issue authored every player-facing word of this beat and they are lifted character-for-character.
    // The way this feature dies is one helpful sentence written to fill a gap (§13.20's own lesson), and the
    // gap here is enormous: nobody in the world may ever say what happened, so every temptation to explain
    // has to be answered with a line that was already written.
    //
    // Read them in order and the whole event is there, told entirely by ordinary paperwork: a works notice
    // goes up; the ledger records the job in three lines and cites no instruction; the local paper is pleased
    // about the drainage. A Scully reads three documents about a resurfacing. Nothing else is ever said.

    /// <summary>#1063 · <b>BEFORE</b> — the notice, posted in the window between the ground being opened and
    /// the ground being filled. It is the most ordinary sentence a facility ever writes, and it is the only
    /// warning the game gives. Authored, verbatim.</summary>
    public const string NoticeLine =
        "Resurfacing of the lower galleries begins Monday. Please use the upper walks.";

    /// <summary>#1063 · <b>DURING</b> — the entry in an otherwise meticulous maintenance ledger, and
    /// <b>the anomaly IS the brevity</b>: the ledger cites an instruction number for everything else it has
    /// ever recorded, and this job has none. Authored, verbatim; the absence of a number is the evidence, so
    /// nothing may ever be appended to it.</summary>
    public const string LedgerLine =
        "Sub-level access no longer required. Filled and remediated per instruction.";

    /// <summary>#1063 · <b>AFTER</b> — the rag, cheerful, once. The owner's own cherry on the cake: <i>"of
    /// course we do a ton of work to raise our streets for no apparent purpose"</i>, reported as good news.
    /// Last year's street standing in this year's foundation, in one sentence, by a paper that thinks it is
    /// writing about kerbstones. Authored, verbatim.</summary>
    public const string RagLine =
        "The concourse reopens a full meter higher, and the drainage is much improved. "
        + "The old kerbs make a handsome course of masonry in the new wall.";

    /// <summary>#1063 · And if pressed, any mason on the job. <b>He means it, and he files it, and that is the
    /// whole testimony</b> — which is why it is a filing phrase and not a sentence: a man who was lied to
    /// would sound like a man who was lied to, and this one sounds like a man doing his job. Authored,
    /// verbatim.</summary>
    public const string MasonLine = "pre-existing masonry, origin undetermined";

    /// <summary>#1063 · Who that is, at a glance, before he says it. <b>Not new prose</b>: it is the issue's
    /// own noun phrase for him — <i>"any mason on the job"</i> — set in the plate idiom every other regular in
    /// that room already wears, so the room's canon grep sees a workman and not a character.</summary>
    public const string MasonPlate = "◈ A MASON ON THE JOB";

    /// <summary>#1063 · The heading the works notice reads across the room. It is not a line of prose and it
    /// is not authored copy: it is the issue's own two nouns — <i>resurfacing</i>, <i>lower galleries</i> —
    /// in the board's own SHOUTED-DEPARTMENT-DASH-STATUS form, which every other notice on it wears. The
    /// sentence underneath is the authored one and it is untouched.</summary>
    public const string NoticeHead = "LOWER GALLERIES — RESURFACING";

    /// <summary>#1063 · Which office filed the rag's line, for the ✂ CLIP heading (#1052). <b>Derived and
    /// never authored</b>: it is the site's own operator, asked of the register that already names it, so the
    /// clipping stacks under the same office heading every other paper from that site does — and no new
    /// company is invented to have done the work.</summary>
    public static string RagOffice(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return SiteOperator.Of(bodyId).Name;
    }

    /// <summary>#1063 · Every player-facing string this beat publishes, for the canon grep — the same
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps. §8's reserved word is swept out of
    /// this list by <c>TheBurialTests</c>, and so is every other word that would settle which reading of §10
    /// is true.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return NoticeHead;
        yield return NoticeLine;
        yield return LedgerLine;
        yield return RagLine;
        yield return MasonPlate;
        yield return MasonLine;
    }
}
