using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1074 beat 2 · <b>THE PRESERVATION ZONE</b> — what the office's stop hardens into when nobody comes back
/// to argue with it, and the cheapest hide anybody has ever built.
///
/// <para>#1074, verbatim: <i>"The cheapest hide is official care: a site fenced, signed, and studied forever
/// — the easiest way to hide a discovery is to build a national park around it."</i></para>
///
/// <para><b>THE EPIGRAPH, and it is never in a string.</b> <i>"The gatekeepers changed. The gate did
/// not."</i> The found band's seal predates the company, predates the Authority and predates everyone
/// currently enforcing it. Each administration inherits the stop the way a town inherits a mound — already
/// standing, already carrying weight — and files its own reason over the same door. A structural review
/// closed this working (beat 1); a study keeps it closed, and a study needs no schedule at all because
/// nothing about a study is ever due. The reasons rotate; the door does not.</para>
///
/// <para><b>WHAT IT DOES TO THE GROUND, in one sentence: nothing.</b> There is no mechanic here beyond
/// persistence. The halls are where the captain left them, the shaft under the listed bottom is still
/// sealed by beat 1's plate, every band predicate answers exactly what it answered yesterday, and the lift
/// still carries him to the bottom the building admits to. What changes is on the SURFACE, in two objects a
/// person walks past: <b>a rail</b> around the working's head, and <b>a sign</b> at the one gap in it.</para>
///
/// <para><b>THE TWO LAWS OF THE #672 DOCTRINE, and where each is kept here:</b></para>
/// <list type="number">
/// <item><b>The enforcer is always an OFFICE, never a name.</b> The sign carries
/// <see cref="StopOrder.Stamp"/> — beat 1's own stamp, not a second one — and no signature, no department,
/// no ministry and no person. A guard sweeps for all four.</item>
/// <item><b>Every stop files a REAL reason (the Scully law, mandatory).</b> Preservation is a real ethic;
/// fencing a working that a structural review closed is the most ordinary thing a public body ever does;
/// and a significance that is <i>under study</i> is a true statement about a site nobody has finished
/// looking at. A reasonable person reads heritage care and is not being fooled — they are reading a true
/// notice. <b>And notice what it produces.</b> The sign says nothing about a date and there is no date on
/// the study.</item>
/// </list>
///
/// <para><b>#761 holds: the fence and the sign ARE the telling.</b> There is no card, no pulse, no beat, no
/// nerve shock, no marker and nothing on the wire. A captain lands, walks up to the shed he has ridden down
/// from before, and finds a rail round it and one notice at the gate. Everything the beat has to say is in
/// those two objects, and §8's reserved word is in neither (<see cref="AllProse"/> is swept for it).</para>
///
/// <para><b>THE GAP IS A LAW AND NOT A DECORATION.</b> The ring has exactly one break in it and the break
/// faces the way home, so a captain who rides the car up into the middle of a fenced site can always walk
/// out of it to his own boat. A fence that could shut a man in beside his own lift would be #602's report
/// wearing a heritage plaque.</para>
/// </summary>
public static class PreservationZone
{
    // ── THE THRESHOLD, AND ITS REASON WRITTEN BESIDE IT ──────────────────────────────────────────────────
    //
    // DisclosureClock's contract: "every beat that reads it chooses its own threshold and writes that
    // threshold's reason down beside its own words." These are those words.

    /// <summary>#1074 · How many WHOLE world-side windows must have passed since the ground was opened
    /// before the site passes into official care. <b>Two</b> — <b>one more than the order took</b>
    /// (<see cref="StopOrder.WindowsBeforeStopping"/>), and the extra shift IS the beat.
    ///
    /// <para>The order closed the working <i>pending structural review</i>, and no schedule for the review
    /// was published. A window later there is still no schedule, because there was never going to be one:
    /// the review that was never scheduled has quietly become a study that never ends, and a study needs a
    /// fence around it. That is the whole of the escalation and it is why the number is not the order's
    /// number — a zone that arrived on the same shift as the order would be one act with two props, and the
    /// point is that nobody decided anything in between. <b>Time passed and the paperwork hardened.</b></para>
    ///
    /// <para>It is the clock's ordinary customer in every other respect: read off the OPENING, never
    /// farmable, and never evaluated with the captain standing on the ground (see <see cref="Note"/>).</para>
    /// </summary>
    public const long WindowsBeforePreserving = 2;

    /// <summary>#1074 · Is this ground due for the fence — <b>at this moment, ignoring where the captain is
    /// standing and ignoring whether the working was ever closed</b>. Two whole windows, per
    /// <see cref="WindowsBeforePreserving"/>.</summary>
    public static bool IsDue(DisclosureClock.Opening opening, long window) =>
        DisclosureClock.WindowsSince(opening, window) >= WindowsBeforePreserving;

    // ── THE REGISTER OF PRESERVED SITES ──────────────────────────────────────────────────────────────────
    //
    // A list of body ids and nothing else, exactly as StopOrder's is and for its reason: there is nothing
    // about a preserved site to CHOOSE, so there is nothing here for a number to keep stable. It is ambient
    // for StopOrder's reason too — the surface deck is built by a function with eight callers and none of
    // them has any business learning what a preservation zone is.
    //
    // AND IT NEVER REVERTS. Nothing in this file removes an id. A site that has been taken into care is in
    // care for the rest of the voyage, which is the one mechanical fact the beat has: the study does not
    // end.

    private static IReadOnlyList<string> _preserved = [];

    /// <summary>#1074 · The grounds fenced, signed and under study. Empty in every world where nobody has
    /// been past a seam long enough ago, which is almost every world.</summary>
    public static IReadOnlyList<string> Preserved => _preserved;

    /// <summary>#1074 · Install the register — the ONE writer, called by whoever owns the save, exactly as
    /// <see cref="StopOrder.Install"/> and <see cref="Burial.Install"/> are. Null and empty are the same
    /// answer: nothing is under study.
    ///
    /// <para>Tests restore what they installed in a <c>finally</c>. Because the register only ever changes
    /// the answer for the ids IN it, a guard that installs a ground of its OWN cannot move any other guard's
    /// world — and the emphasis on "its own" is paid for: xUnit runs test classes in parallel, and #1068's
    /// first full run reddened two audits that had nothing to do with it.</para></summary>
    public static void Install(IReadOnlyList<string>? preserved) => _preserved = preserved ?? [];

    /// <summary>#1074 · <b>Is this ground fenced, signed and under study?</b> False everywhere in a world
    /// where nobody has been past a seam long enough ago, which is almost every world.</summary>
    public static bool On(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // READ THE REFERENCE ONCE — Burial.IsFilled's own lesson, paid for there with an IndexOutOfRange
        // thrown out of the site generator by a guard that had nothing to do with any of it.
        IReadOnlyList<string> preserved = _preserved;

        // A walk and not a set, for Burial.IsFilled's reason: the register is at most a handful of grounds
        // in the longest voyage anybody will play, and this is asked from inside the deck builder.
        for (int i = 0; i < preserved.Count; i++)
        {
            if (string.Equals(preserved[i], bodyId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>#1074 · <b>THE EVENT.</b> Which of the closed workings have hardened into official care by
    /// now — folded into the register, and the register handed back <b>by reference</b> when there is
    /// nothing to add, so a caller can compare and only then ask for a save.
    ///
    /// <para><b>Four conditions:</b> two whole windows have passed since the opening
    /// (<see cref="WindowsBeforePreserving"/>); <b>the working is already closed</b>, which is the whole of
    /// "never on an unstopped ground" and, by construction, the whole of "never on a buried one" as well —
    /// a ground is stopped or buried and never both (<see cref="StopOrder.TheOfficeGetsThisOne"/>), so
    /// membership in the stop register is the one question that has to be asked and the split is not asked
    /// a second time here; <b>the captain is not on that body</b>, because a fence that went up around a man
    /// standing inside it would be a thing that happened TO him and therefore a thing he could describe; and
    /// it is not already in care, because care does not begin twice.</para>
    ///
    /// <para>Nothing here is effort, a die, or a visit count: it is a fact about WHEN the captain went, what
    /// the office already did, and where he is now.</para></summary>
    /// <param name="register">The disclosure clock's register of opened grounds.</param>
    /// <param name="stopped">The workings the Authority has already closed (<see cref="StopOrder"/>).</param>
    /// <param name="preserved">What is already under study.</param>
    /// <param name="standingOn">The body the captain is on right now, or null when he is on none.</param>
    /// <param name="simTime">Sim seconds — no clock is read in Core.</param>
    public static IReadOnlyList<string> Note(
        IReadOnlyList<DisclosureClock.Opening>? register,
        IReadOnlyList<string>? stopped,
        IReadOnlyList<string>? preserved,
        string? standingOn,
        double simTime)
    {
        IReadOnlyList<string> had = preserved ?? [];
        if (register is not { Count: > 0 } || stopped is not { Count: > 0 })
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
            if (!Contains(stopped, opening.BodyId))
            {
                continue;   // nobody closed this working, so there is nothing here to take into care
            }
            if (string.Equals(opening.BodyId, standingOn, StringComparison.Ordinal))
            {
                continue;   // not while he is standing on it
            }
            if (Contains(had, opening.BodyId) || (next is not null && Contains(next, opening.BodyId)))
            {
                continue;   // already in care, and care begins once
            }
            next ??= [.. had];
            next.Add(opening.BodyId);
        }
        return next ?? had;
    }

    private static bool Contains(IReadOnlyList<string> list, string id)
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

    // ── THE ONE THING THAT IS WRITTEN DOWN ───────────────────────────────────────────────────────────────

    /// <summary>#1074 · <b>THE SIGN.</b> Authored in #1074, verbatim, and the only new player-facing string
    /// this beat adds to the game.
    ///
    /// <para>Read it as a reasonable person reads it and it is a heritage notice on a working that a
    /// structural review closed: true, prudent, and the most ordinary thing a public body ever posts.
    /// <b>And notice what it does not say.</b> It carries no date, names no department, cites no instruction
    /// and gives the study neither a beginning nor an end — and a significance that is permanently under
    /// study is a significance nobody will ever be asked to publish. No sentence anywhere points that
    /// out.</para></summary>
    public const string Sign = "THIS SITE IS PRESERVED. Its significance is under study.";

    /// <summary>#1074 · What is actually posted at the gap, which is the sign under an office's stamp.
    ///
    /// <para><b>Not new prose.</b> The stamp is <see cref="StopOrder.Stamp"/> — beat 1's own constant, so
    /// the notice at the gate and the plate at the seal are stamped by the same office rather than by two
    /// that happen to be spelled alike — and the sentence is the canon's, character for character. What
    /// joins them is the SHOUTED-STAMP-DASH form every plate in this game already wears
    /// (<c>AUTHORITY — WORKING CLOSED</c>, <c>POWER — LOCKED OUT</c>, <c>RECORDS — SEALED</c>); the shape of
    /// a heading is the world's and only the sentence under it is authored, which is #1063's
    /// <c>NoticeHead</c> ruling and beat 1's <see cref="StopOrder.Plate"/> doing exactly this.</para>
    ///
    /// <para>There is a stamp and there is no signature, which is the doctrine's first law said in one
    /// word.</para></summary>
    public const string Notice = StopOrder.Stamp + " — " + Sign;

    /// <summary>#1074 · Is this the notice? Asked of the string itself, for the reason
    /// <see cref="StopOrder.IsPlate"/> is asked that way: the client meets these as text on a deck and must
    /// never be the place that decides what one of them IS.</summary>
    public static bool IsNotice(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return string.Equals(text, Notice, StringComparison.Ordinal);
    }

    /// <summary>#1074 · Every player-facing string this beat publishes — one — for the canon grep, the same
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps.
    ///
    /// <para>The notice is here and the bare sign is not, because the sign is a substring of the notice: a
    /// list that yielded both would report one sentence twice and tell a reviewer nothing new. That is
    /// beat 1's own arrangement with its stamp and its plate.</para></summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Notice;
    }

    // ── THE RAIL ─────────────────────────────────────────────────────────────────────────────────────────
    //
    // WHAT IT IS NOT. It is not the rim fence #565 took off the landing site — four bright hull lines around
    // a rectangle, which announced a boundary that was not the real one and made a moon look like graph
    // paper — and it is not a tile boundary either; the ground is an unbounded lattice and nothing may draw
    // its seams. It is a small closed ring around ONE building, in the ordinary inner-line ink every fallen
    // span and low ruin wall on every landing site is already drawn in: not hull (this is not a pressure
    // boundary), not stone (a rail is not mass), not seamless (that ink belongs to the halls), not unseen
    // (being seen is the entire job). A person walking up to it reads a fence, because that is what it is.

    /// <summary>#1074 · How many sides the ring is drawn with. Enough that it reads as a ring rather than as
    /// a box on a crude grid, few enough that each side is a real run of rail a body-width guard would
    /// accept rather than a scatter of stubs (<c>SurfaceStructure.MinThickness</c>'s own argument: a segment
    /// shorter than the captain is wide reads as an invisible wall).</summary>
    public const int RingSides = 16;

    /// <summary>#1074 · The swept ground between the shed's furthest corner and the rail.
    ///
    /// <para><b>It is under the berth the lift head already reserves</b> (<c>SecretLab</c> places the head
    /// at least its own reach plus four clear of every shelter on the site), so the rail stands on ground
    /// that was already kept empty for the shed and cannot be laid through a neighbour's wall or across its
    /// door. That is the whole reason the number is this number and not a prettier one: a fence is only
    /// allowed to occupy ground somebody has already promised is free.</para></summary>
    public const double RailBerth = 3.0;

    /// <summary>#1074 · How far outside the rail the notice is posted — a pace back from the gate, on the
    /// approach side, where a person reads a sign before walking through rather than after.</summary>
    public const double SignStandoff = 3.0;

    /// <summary>#1074 · A fenced site: the rails, the one gap, and where the notice is posted.
    ///
    /// <para>The gap is published as the SEGMENT across it rather than as a point, for
    /// <c>SurfaceStructure.Doorway</c>'s reason — a caller that wants to know whether the way through is
    /// wide enough for a body needs to know which way the opening runs, and a point cannot say.</para></summary>
    public readonly record struct Fence(
        IReadOnlyList<SurfaceLayout.Wall> Rails,
        double GapX1, double GapY1, double GapX2, double GapY2,
        double SignX, double SignY,
        double CentreX, double CentreY, double Radius)
    {
        /// <summary>The middle of the gap — the spot a captain actually walks through.</summary>
        public double GapCentreX => (GapX1 + GapX2) / 2;

        /// <summary>The middle of the gap — the spot a captain actually walks through.</summary>
        public double GapCentreY => (GapY1 + GapY2) / 2;

        /// <summary>How wide the way through is, corner to corner.</summary>
        public double GapWidth =>
            Math.Sqrt(((GapX2 - GapX1) * (GapX2 - GapX1)) + ((GapY2 - GapY1) * (GapY2 - GapY1)));
    }

    /// <summary>#1074 · <b>FENCE THIS BUILDING.</b> A closed ring of rail around the working's head, with
    /// exactly one gap in it, and the gap turned to face the way home.
    ///
    /// <para><b>Why the gap faces the tube and not a seeded bearing.</b> The captain rides the car up into
    /// the middle of this ring. A gap anywhere else would be correct exactly as often as a coin, and on the
    /// other half of the seeds it would put a fence between a man and his own boat — walkable, yes, but a
    /// walk around a heritage rail is not a thing this game is about, and on a bad seed the long way round
    /// is most of a tank. Facing the gate at the tube is also what an office would do: the gate goes where
    /// the road is.</para>
    ///
    /// <para><b>Exactly one gap</b>, because a ring with two ways through is a pair of hurdles rather than an
    /// enclosure, and because the sign has to be posted somewhere a person cannot miss it: at the one
    /// gate.</para>
    ///
    /// <para>Pure and deterministic — the same hut and the same field always give the same rails. There is
    /// no die here at all: everything is derived from a building that was already placed.</para></summary>
    /// <param name="hut">The working's head — the shed the lift comes up in.</param>
    /// <param name="field">The shared field envelope, which is where the way home is
    /// (<see cref="SurfaceLayout.Field.HomeX"/> and <see cref="SurfaceLayout.Field.TopY"/> — the tube
    /// mouth). Read from the field rather than typed here, for the reason the field exists at all: a
    /// hand-copied client constant in Core is the drift this ground keeps paying for.</param>
    public static Fence FenceAround(in SurfaceStructure.Spec hut, in SurfaceLayout.Field field)
    {
        double cx = hut.CentreX, cy = hut.CentreY;
        double radius = SurfaceStructure.EnvelopeOf(hut).Reach + RailBerth;

        // The bearing to the way home. The ring is laid so that ONE SIDE is centred on exactly this bearing,
        // and then that side is the side that is never drawn — which is what makes "the gap faces the tube"
        // a fact about the construction rather than a thing a search happened to find.
        double home = Math.Atan2(field.TopY - cy, field.HomeX - cx);
        double step = Math.Tau / RingSides;

        (double X, double Y) Corner(int k)
        {
            double a = home + ((k - 0.5) * step);
            return (cx + (radius * Math.Cos(a)), cy + (radius * Math.Sin(a)));
        }

        // Side k runs from corner k to corner k+1. Side 0 straddles the home bearing, so it is the gap;
        // every other side is rail. Fifteen segments, one break, and the loop closes on itself.
        var rails = new List<SurfaceLayout.Wall>(RingSides - 1);
        for (int k = 1; k < RingSides; k++)
        {
            (double ax, double ay) = Corner(k);
            (double bx, double by) = Corner(k + 1);
            rails.Add(new SurfaceLayout.Wall(ax, ay, bx, by, IsHull: false));
        }

        (double g1x, double g1y) = Corner(0);
        (double g2x, double g2y) = Corner(1);

        // The notice stands outside the gate on the approach, facing whoever is walking up to it.
        double signR = radius + SignStandoff;
        return new Fence(
            rails,
            g1x, g1y, g2x, g2y,
            cx + (signR * Math.Cos(home)), cy + (signR * Math.Sin(home)),
            cx, cy, radius);
    }
}
