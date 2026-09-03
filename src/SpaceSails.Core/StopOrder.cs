using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1074 · <b>THE STOP ORDER AT THE DIG</b> — the third customer of <see cref="DisclosureClock"/>, and the
/// ENFORCEMENT tier of the #672 doctrine: the watchers hold the lease on the classroom, and the tenants in
/// office know it.
///
/// <para>Owner, 2026-09-02: <i>"it seems almost like that governments are told by some entity to stop letting
/// people explore what is down there… all of a sudden Egypt says national security and pull the plug from the
/// research before they can drill through the roof… Then the government people in power do the
/// enforcing."</i></para>
///
/// <para>Between two visits, a working the intranet scheduled, the ledger funded and the crew rostered is
/// CLOSED. Nothing is filled and nothing is removed — the halls are exactly where the captain left them — and
/// the shaft that reaches them is sealed by a plate with a stamp on it. He comes back down, rides to the
/// bottom the building admits to, and the panel has nothing under it any more.</para>
///
/// <para><b>THE TWO LAWS THE DOCTRINE ADDS, AND WHERE EACH IS KEPT:</b></para>
/// <list type="number">
/// <item><b>The enforcer is always an OFFICE, never a name.</b> <i>"Word came down from the top."</i> The
/// plate carries <see cref="Stamp"/> and no signature; nothing in this file names a person, a department or a
/// ministry, and a guard sweeps every string here for both. The game never mints a villain official — the
/// horror is that any office will do.</item>
/// <item><b>Every stop files a REAL reason (the Scully law, mandatory).</b> The structure genuinely could
/// collapse; a structural review is a real ethic and services really are isolated when a working closes. A
/// reasonable person reads prudence and is not being fooled — they are reading a true document. The mechanism
/// is #1063's, quoted from the owner's own research because it is exactly our idiom: <i>"not outright
/// fabrication, which would be detectable. Selective omission framed in bureaucratic language designed to
/// sound responsible."</i></item>
/// </list>
///
/// <para><b>WHAT IT IS NOT: THE BURIAL.</b> #1063 is the town forgetting; this is the office remembering ON
/// PURPOSE and filing prudence over it. They are siblings on one trigger and they are mutually exclusive by
/// construction — see <see cref="TheOfficeGetsThisOne"/>, which is the whole of the split and is asked by both
/// sides so neither can ever fire on the other's ground.</para>
///
/// <para><b>DEEP TIME, and it is never in a card.</b> <i>"The gatekeepers changed. The gate did not."</i> The
/// found band's seal predates the company, predates the Authority and predates everyone currently enforcing
/// it. Each administration inherits the stop the way a town inherits a mound — already standing, already
/// carrying weight — and files its own reason over the same door. The reasons rotate; the door does not.</para>
///
/// <para><b>CANON.</b> Every player-facing word here is authored in #1074's canon pass of 2026-09-03 and is
/// lifted character-for-character; nothing is composed but the PLATE, which is the canon's own two nouns in
/// the SHOUTED-DEPARTMENT-DASH-STATUS form every other plate in this building already wears (#1063's
/// <c>NoticeHead</c> precedent). The word §8 reserves does not appear, and a guard sweeps
/// <see cref="AllProse"/> for it.</para>
/// </summary>
public static class StopOrder
{
    // ── THE THRESHOLD, AND ITS REASON WRITTEN BESIDE IT ──────────────────────────────────────────────────
    //
    // DisclosureClock's own docblock says what a customer owes it: "every beat that reads it chooses its own
    // threshold and writes that threshold's reason down beside its own words." These are those words.

    /// <summary>#1074 · How many WHOLE world-side windows must have passed since the ground was opened before
    /// the Authority may close the working. <b>One</b> — the burial's number and #1068's number, and
    /// deliberately the same one, because <b>this is the same trigger read a second way</b>: the two outcomes
    /// are two things that can happen to one opened ground on one schedule, and a stop that ran on a clock of
    /// its own would be a second answer to "how long is a shift".
    ///
    /// <para>Never inside the window the captain opened the ground in. An order posted before he had climbed
    /// back out of the seam he had just crossed would be an answer to what he had just done, arriving inside
    /// the hour, from something that was watching him do it — which is the sensor return #672's instrument law
    /// forbids outright. A shift later it is a works decision by an office with a form to fill in, and that is
    /// a thing a reasonable person can hold.</para></summary>
    public const long WindowsBeforeStopping = 1;

    /// <summary>#1074 · Is this ground due for the office's attention — <b>at this moment, ignoring where the
    /// captain is standing</b>. One whole window, per <see cref="WindowsBeforeStopping"/>.</summary>
    public static bool IsDue(DisclosureClock.Opening opening, long window) =>
        DisclosureClock.WindowsSince(opening, window) >= WindowsBeforeStopping;

    // ── THE SPLIT: ONE REGISTER, TWO OUTCOMES ────────────────────────────────────────────────────────────
    //
    // #1063 and this issue are siblings on ONE trigger — an opened found band, one whole world window, the
    // captain off the body — and a ground gets ONE of the two. The split is therefore not two conditions
    // that happen to be disjoint (which is a thing that stays true until somebody edits one of them); it is
    // ONE function, asked by both sides, whose two answers are the two outcomes. Burial.Fill skips the
    // grounds this hands to the office; Note below skips the ones it hands to the neighbours. Neither knows
    // anything about the other's register.
    //
    // WHY IT IS SEEDED ON THE OPENING'S OWN WINDOW AND NOT ON THE CURRENT ONE. The outcome has to be a FACT
    // ABOUT THE GROUND rather than about the moment the trucks happen to arrive: a coin flipped against
    // "now" would give one site a stop this afternoon and a burial tomorrow morning, so which of the two
    // happened would depend on when the captain flew home. That is the clock's own law four (not farmable)
    // said about an outcome instead of about a reading — what you find is a fact about WHEN YOU WENT, and
    // the window you went in is exactly what the opening record keeps.
    //
    // ROUGHLY HALF EACH, and it is a fair coin rather than a weighting, because there is no fact in the
    // world that would justify a number: the neighbours and the office are two tenants of one landlord and
    // nothing in #672 says either is the commoner. The measured share is asserted rather than assumed
    // (TheStopOrderAtTheDigTests sweeps the seeding and reads the rate off the sweep).

    /// <summary>#1074 · <b>WHOSE GROUND IS THIS?</b> True where the Authority gets there first and the working
    /// is closed; false where the neighbours get there first and it is filled in (#1063).
    ///
    /// <para>Pure, and a fact about the ground and the window it was opened in — nothing else. It is asked by
    /// <see cref="Note"/> and by <see cref="Burial.Fill"/>, which is what makes "a stopped ground is never
    /// also a buried one" true by construction rather than by two conditions somebody has to keep
    /// agreeing.</para></summary>
    public static bool TheOfficeGetsThisOne(DisclosureClock.Opening opening) =>
        DiceRule.Roll(DiceRule.Seed($"stop:office:{opening.BodyId}", opening.Window), 2).Face == 1;

    // ── THE REGISTER OF STOPPED GROUNDS ──────────────────────────────────────────────────────────────────
    //
    // A list of body ids and nothing else, exactly as Burial's is and for its reason: a closed working is
    // closed, and a record that kept WHY would be a record with an opinion in it. Nothing downstream needs a
    // window — the seal stands in the one pocket the building has to spare and the plate says one sentence,
    // so there is nothing here to choose and therefore nothing to be stable against (which is the one thing
    // #1068's register does need, and why that one keeps a number and this one does not).
    //
    // It is ambient for the reason Burial's is: the shape of a site is asked by about thirty callers and
    // none of them has any business learning what a stop order is. It is consulted at the seams those
    // callers already go through — the lift panel's gate row, the floor build, the haul table, the board.

    private static IReadOnlyList<string> _stopped = [];

    /// <summary>#1074 · The grounds whose deep working has been closed. Empty in every world where nobody has
    /// been past a seam long enough ago, which is almost every world.</summary>
    public static IReadOnlyList<string> Stopped => _stopped;

    /// <summary>#1074 · Install the register — the ONE writer, called by whoever owns the save (the client, on
    /// load and on every descent), exactly as <see cref="Burial.Install"/> and
    /// <see cref="PoliteDecline.Install"/> are. Null and empty are the same answer: nothing has been stopped.
    ///
    /// <para>Tests restore what they installed in a <c>finally</c>. Because the register only ever changes the
    /// answer for the ids IN it, a guard that installs a ground of its OWN cannot move any other guard's
    /// world — and the emphasis on "its own" is paid for: xUnit runs test classes in parallel, and #1068's
    /// first full run reddened two audits that had nothing to do with it because its guards had declined on a
    /// site the shipped sweeps walk.</para></summary>
    public static void Install(IReadOnlyList<string>? stopped) => _stopped = stopped ?? [];

    /// <summary>#1074 · <b>Is this ground's deep working closed?</b> False everywhere in a world where nobody
    /// has been past a seam long enough ago, which is almost every world.</summary>
    public static bool On(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // READ THE REFERENCE ONCE — Burial.IsFilled's own lesson, paid for there with an IndexOutOfRange
        // thrown out of the site generator by a guard that had nothing to do with any of it.
        IReadOnlyList<string> stopped = _stopped;

        // A walk and not a set, for Burial.IsFilled's reason: the register is at most a handful of grounds in
        // the longest voyage anybody will play, and this is asked from inside the generator's hot path.
        for (int i = 0; i < stopped.Count; i++)
        {
            if (string.Equals(stopped[i], bodyId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>#1074 · <b>THE EVENT.</b> Which of the grounds this captain has opened the Authority has
    /// closed by now — folded into the register, and the register handed back <b>by reference</b> when there
    /// is nothing to add, so a caller can compare and only then ask for a save.
    ///
    /// <para><b>Three conditions, and only the third is new:</b> a whole window has passed since the opening
    /// (<see cref="WindowsBeforeStopping"/>); <b>the captain is not on that body</b>, because an order posted
    /// while he stood on the floor would be a thing that happened TO him and therefore a thing he could
    /// describe, and the beat is that he comes back to a door that was closed while nobody was looking; and
    /// <b>this ground is the office's</b> (<see cref="TheOfficeGetsThisOne"/>) rather than the neighbours'.
    /// </para>
    ///
    /// <para>Nothing here is effort, a die, or a visit count: it is a fact about WHEN the captain went and
    /// where he is now.</para></summary>
    /// <param name="register">The disclosure clock's register of opened grounds.</param>
    /// <param name="stopped">What has been closed already.</param>
    /// <param name="standingOn">The body the captain is on right now, or null when he is on none.</param>
    /// <param name="simTime">Sim seconds — no clock is read in Core.</param>
    public static IReadOnlyList<string> Note(
        IReadOnlyList<DisclosureClock.Opening>? register,
        IReadOnlyList<string>? stopped,
        string? standingOn,
        double simTime)
    {
        IReadOnlyList<string> had = stopped ?? [];
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
            if (!TheOfficeGetsThisOne(opening))
            {
                continue;   // the neighbours' ground, and they fill it in (#1063)
            }
            if (string.Equals(opening.BodyId, standingOn, StringComparison.Ordinal))
            {
                continue;   // not while he is standing on it
            }
            if (Contains(had, opening.BodyId) || (next is not null && Contains(next, opening.BodyId)))
            {
                continue;   // already closed, and a working closes once
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

    // ── THE AUTHORED WORDS, VERBATIM FROM #1074's CANON PASS ─────────────────────────────────────────────
    //
    // Read them in order and the whole event is there, told entirely by ordinary paperwork: an order posted
    // at a seal, a valve-book that goes terse for one line, and a roster that still lists the shift. A
    // Scully reads a structural review and a plant book. Nothing else is ever said, by anybody, anywhere.

    /// <summary>#1074 · <b>THE STAMP.</b> The enforcer, and the whole of the enforcer: an OFFICE. There is no
    /// signature under the order and there is no name anywhere on it, which is the doctrine's first law said
    /// in one word — the game never mints a villain official, because the horror is that any office will do.
    /// </summary>
    public const string Stamp = "AUTHORITY";

    /// <summary>#1074 · <b>THE ORDER, POSTED AT THE SEAL.</b> Authored (canon pass, 2026-09-03), verbatim.
    ///
    /// <para>Every word of it is true and every word of it is prudent, which is the Scully law working
    /// exactly as it is meant to: a structural review is a real ethic, a working that might come down is a
    /// real danger, and a review with no published schedule is the most ordinary thing a public body ever
    /// writes. <b>And notice the result it produces.</b></para></summary>
    public const string OrderLine =
        "By order of the Authority this working is closed pending structural review. "
        + "No schedule for the review is published.";

    /// <summary>#1074 · What is stencilled on the plate itself, which is what the corridor reads at a glance.
    ///
    /// <para><b>Not new prose</b>: it is the canon's own two nouns — the <i>Authority</i>, the <i>working
    /// closed</i> — set in the SHOUTED-NOUN-DASH-STATUS form every other plate in this building already wears
    /// (<c>POWER — LOCKED OUT</c>, <c>RECORDS — SEALED</c>, <c>CUSTOMS — SEALED</c>). #1063's
    /// <c>NoticeHead</c> is the same move for the same reason: the shape of a heading is the board's, and
    /// only the sentence under it is authored.</para>
    ///
    /// <para>It is deliberately NOT one of the door vocabularies (<c>UndergroundComplex.SignsFor</c>), so
    /// <c>UndergroundComplex.IsDoorSign</c> answers no and nothing in the game can mistake this for a room
    /// somebody shut — which is also what keeps a captain from taking a sentry to it (see
    /// <see cref="ShootTheLock.Judge"/>).</para></summary>
    public const string Plate = Stamp + " — WORKING CLOSED";

    /// <summary>#1074 · Is this sign the order's plate? Asked of the sign itself, for the reason
    /// <c>UndergroundComplex.IsSealedWay</c> is asked that way: the client meets these as strings on a console
    /// and must never be the place that decides what one of them IS.</summary>
    public static bool IsPlate(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return string.Equals(sign, Plate, StringComparison.Ordinal);
    }

    // ── THE VALVE-BOOK: THREE ENTRIES, ONE UNNUMBERED ────────────────────────────────────────────────────
    //
    // #1063's law, one rung along and in a different book: THE ANOMALY IS THE BREVITY. The plant book cites
    // an instruction number for everything it has ever recorded — 2231, then 2233 — and the entry between
    // them cites an ORDER and no number at all. The book's own arithmetic therefore says an instruction 2232
    // was issued, and the one line that would cite it is the one line that cites nothing.
    //
    // The second tell is the preposition. Every other line in this book says PER INSTRUCTION; this one says
    // PER ORDER, which is a different kind of authority and a different piece of paper, and no sentence
    // anywhere points that out.
    //
    // NOTHING IS FAKE. The packing really was renewed, the valves really were exercised, the services really
    // are isolated, and the review really is pending. A Scully reads three lines of a plant book.

    /// <summary>#1074 · <b>The entry ABOVE</b>, citing <b>2231/M</b>. Authored, verbatim. It exists to
    /// establish the house style the next entry breaks: riser, work, hours, hands, instruction, department.
    /// Nothing about it is interesting, which is the entire job it does.</summary>
    public const string ValveBookBefore =
        "Riser B: packing renewed on valves fourteen and fifteen, two hours, one hand. "
        + "Per instruction 2231/M, Plant.";

    /// <summary>#1074 · <b>The entry itself</b> — the one line in an otherwise meticulous book that cites no
    /// number and says <i>per order</i> where every other line says <i>per instruction</i>. Authored,
    /// verbatim; the absence of the number is the evidence, so nothing may ever be appended to it.</summary>
    public const string ValveBookLine = "Working closed. Services isolated per order. Review pending.";

    /// <summary>#1074 · <b>The entry BELOW</b>, citing <b>2233/M</b>. Authored, verbatim. Between them the two
    /// cited numbers do the arithmetic no sentence on the paper is allowed to do.</summary>
    public const string ValveBookAfter =
        "Riser C: valves exercised and logged, one hour, one hand. Per instruction 2233/M, Plant.";

    /// <summary>#1074 · Every player-facing string this beat publishes, for the canon grep — the same
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps. §8's reserved word is swept out of
    /// this list, and so is every other word that would settle which reading of §10 is true.
    ///
    /// <para>The plate is here and the stamp is not, because the stamp is a substring of the plate: a list
    /// that yielded both would report one word twice and tell a reviewer nothing new.</para></summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Plate;
        yield return OrderLine;
        yield return ValveBookBefore;
        yield return ValveBookLine;
        yield return ValveBookAfter;
    }
}
