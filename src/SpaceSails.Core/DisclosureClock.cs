using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #677 · THE DISCLOSURE CLOCK — the second half of the found halls, and the half that is a CLOCK rather
/// than a place.
///
/// <para>Owner ruling 2026-08-04, recorded in <c>docs/worldbuilding-notes.md</c> §10: <i>"The horror is the
/// disclosure schedule. As the day of decision approaches, the fourth is LET to know more and more about
/// what is under its feet."</i> The issue states the mechanic in one sentence and this file is that sentence
/// and nothing else: <b>what a captain is shown rides slow world-side windows, not search effort — what you
/// find is a fact about WHEN YOU WENT. Later windows may show more. It is never a progress bar and never
/// announced.</b></para>
///
/// <para>v1 of the halls (#716) shipped the BAND and filed this deliberately unbuilt. This is the clock, and
/// only the clock: it starts when a captain crosses the seam, it reads in the same slow windows the monolith's
/// foot-offerings use, and <b>nothing in the game reads it yet</b>. That is not an oversight, it is the
/// sequencing every downstream issue asked for in the same words — #1063's burial needs <i>"a place worth
/// burying"</i>, #1068's channels wait for <i>"#677 giving the thresholds something to guard"</i>, and #1074's
/// stop orders wait for <i>"#677 giving the world a threshold worth enforcing"</i>. The threshold is here.
/// The consequences are theirs, and a feature that ships its own second half unasked is a feature nobody
/// reviewed (§13.19's judgement call, made twice now on this ground).</para>
///
/// <para><b>THE FIVE LAWS, each guarded in <c>TheDisclosureClockTests</c> and each proved able to fail:</b></para>
/// <list type="number">
/// <item><b>ONE CLOCK.</b> The window is <see cref="Monolith.EpochAt"/>'s, asked and never re-derived. The
/// issue names that clock by name — <i>"the monolith foot-offering clock"</i> — and two copies of a window
/// length is the mirrored-constant bug this ground keeps a table of. <see cref="WindowSeconds"/> exists so a
/// caller can say the number without owning it.</item>
/// <item><b>ONLY THE HALLS START IT.</b> <see cref="OpensOn"/> is <see cref="UndergroundComplex.IsFound"/> and
/// nothing else — not the band nobody listed (#592: human all the way down, and its own secret), not the head
/// office, not a hidden lab, not the slab. A clock that any deep floor could start would be a clock about
/// digging, and this one is about what was already there.</item>
/// <item><b>IT IS NEVER ANNOUNCED, AND THE PROOF IS THAT THERE IS NOTHING TO SAY.</b> This type publishes no
/// prose at all: no label, no line, no percentage, no title, not one string. That is the strongest available
/// form of the never-announced law — a beat that wanted to speak would have to author its words somewhere a
/// reviewer can see them, which is exactly where canon belongs. The guard sweeps this type by reflection.</item>
/// <item><b>IT IS NOT FARMABLE.</b> Nothing in any signature here is effort: not a visit count, not rooms
/// searched, not floors walked, not a die. A captain who rides down a hundred times inside one window reads
/// exactly what a captain who rode down once reads, which is the owner's own object-persistence law
/// (<see cref="Monolith.EpochSeconds"/>) said about knowledge instead of about objects.</item>
/// <item><b>IT NEVER RUNS BACKWARDS.</b> <see cref="Note"/> keeps the FIRST crossing — a site is opened once
/// and re-entering it is not a fresh discovery — and <see cref="WindowsSince(Opening, long)"/> is monotone
/// non-decreasing in the window and floored at zero, so no reading can ever un-show what a later window
/// showed. <i>Later windows may show more</i> is a promise about direction, and a clock that could tick down
/// would break it silently.</item>
/// </list>
///
/// <para><b>WHAT IT DELIBERATELY IS NOT: A STAGE, A LEVEL, OR A BAR.</b> The obvious shape here is a small
/// integer with a cap on it — stage 0, 1, 2, 3 — and it was written that way first and taken out. Two reasons,
/// and both are canon rather than taste. A capped counter needs a cadence (how many windows per stage) and a
/// ceiling (how many stages), and neither number is derivable from anything in the world — they would be two
/// invented facts about the schedule of a decision the game may never state, authored by an implementer, in
/// code, where no canon review looks. And a capped counter is a progress bar with the drawing left off:
/// something a beat could show as three pips, which the issue forbids in the sentence it introduces the
/// mechanic in. So what is published is the raw reading — how many world windows have passed since this ground
/// was opened — and every beat that reads it chooses its own threshold and writes that threshold's reason down
/// beside its own words.</para>
///
/// <para><b>CANON.</b> Nothing here names a builder, an age or a purpose, and it could not: there is no prose
/// in this file. Both readings of §10 survive untouched, because a clock that says only <i>how long</i> says
/// nothing whatever about <i>why</i> — the mundane reading (a resurvey team came back with a better sounder,
/// and time is simply what surveys take) and the other one (this was always here, and the time to be shown it
/// has come) are the same number. The word §8 reserves does not appear.</para>
/// </summary>
public static class DisclosureClock
{
    /// <summary>How long one world-side window lasts in sim-seconds. <b>The monolith's own</b>, and it is
    /// exposed here as a delegation rather than copied as a number, so that the day somebody re-times the
    /// foot-offerings the halls are re-timed with them. The issue names that clock explicitly; two of them
    /// would be the mirrored constant this ground has paid for repeatedly.</summary>
    public static double WindowSeconds => Monolith.EpochSeconds;

    /// <summary>Which world-side window a moment falls in. Pure — no clock is read in Core, the caller passes
    /// sim time, exactly as <see cref="Monolith.EpochAt"/> requires.</summary>
    public static long WindowAt(double simTime) => Monolith.EpochAt(simTime);

    /// <summary>#677 · CAN STANDING HERE START THE CLOCK? Only in the band nobody dug.
    ///
    /// <para>One predicate, delegating to the one that already answers it, for the reason
    /// <see cref="Monolith.StandsOn"/> exists: four different callers deciding for themselves what counts as
    /// "the halls" is how a marquee beat came to fire on a broken machine on the wrong moon (#574). The
    /// unlisted band deliberately does NOT open one — it is human all the way down, poured and invoiced and
    /// hidden from the staff who paid for it, and a captain who finds one has found somebody's secret, not
    /// somebody's schedule.</para></summary>
    public static bool OpensOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UndergroundComplex.IsFound(bodyId, level);
    }

    /// <summary>#677 · One ground, opened. The site, and the window the seam was first crossed in — which is
    /// the whole of what the clock knows and, deliberately, less than the game knows: not who, not how deep,
    /// not what they carried out. A record that kept more would be a record with an opinion in it.</summary>
    /// <param name="BodyId">The site whose halls were entered.</param>
    /// <param name="Window">The world-side window it happened in (<see cref="WindowAt"/>).</param>
    public readonly record struct Opening(string BodyId, long Window);

    /// <summary>#677 · What a crossing at this moment opens, or null where this floor cannot open anything.
    ///
    /// <para>Null is the ordinary answer everywhere in the game and is the reason this returns an option
    /// rather than a bool and an out: the caller wiring the seam beat should be handed the record or handed
    /// nothing, never handed a record it has to remember to check a flag beside.</para></summary>
    public static Opening? Open(string bodyId, int level, double simTime)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return OpensOn(bodyId, level) ? new Opening(bodyId, WindowAt(simTime)) : null;
    }

    /// <summary>#677 · Fold a crossing into the register this thread keeps, and <b>the first one wins</b>.
    ///
    /// <para>A site is opened ONCE. Re-entering halls a captain has already stood in is not a discovery and
    /// must not move the clock forward: a register that took the latest crossing would hand a captain who
    /// revisits a familiar site a reading of zero forever, which is precisely the farmable shape law four
    /// forbids — going back would keep the ground new. It also never moves BACKWARDS, which matters for a
    /// loaded save whose sim time starts wherever the file left it.</para>
    ///
    /// <para>Returns the register unchanged, by reference, when there is nothing to add — so a caller may
    /// compare and only then ask for a save.</para></summary>
    public static IReadOnlyList<Opening> Note(IReadOnlyList<Opening>? register, Opening opening)
    {
        IReadOnlyList<Opening> had = register ?? [];
        foreach (Opening o in had)
        {
            if (string.Equals(o.BodyId, opening.BodyId, StringComparison.Ordinal))
            {
                return had;   // already opened, and the window it was opened in is the one that counts
            }
        }

        var next = new List<Opening>(had) { opening };
        return next;
    }

    /// <summary>#677 · When this ground was opened, or null if this captain has never been past its seam.
    ///
    /// <para>Null and zero are different answers and the distinction is the whole point: a site opened in
    /// this very window reads zero windows since, and a site never opened reads NOTHING. A beat that treated
    /// the two the same would fire on every moon in the universe.</para></summary>
    public static Opening? OpeningOf(IReadOnlyList<Opening>? register, string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (Opening o in register ?? [])
        {
            if (string.Equals(o.BodyId, bodyId, StringComparison.Ordinal))
            {
                return o;
            }
        }
        return null;
    }

    /// <summary>#677 · <b>THE READING.</b> How many world-side windows have passed since this ground was
    /// opened.
    ///
    /// <para>Floored at zero rather than allowed negative, because a window before the opening is not a
    /// smaller reading, it is a moment the ground had not been found in — and a negative number would sort
    /// below zero in exactly the comparison a threshold is written as.</para>
    ///
    /// <para>This is the only quantity this file publishes and it has no maximum on purpose. See the type's
    /// own remarks: a capped stage would be a progress bar with the drawing left off, and its cadence and
    /// ceiling would be two invented facts about a schedule no card may state.</para></summary>
    public static long WindowsSince(Opening opening, long window) =>
        Math.Max(0, window - opening.Window);

    /// <summary>#677 · The same reading, asked of the register a caller actually keeps, at a moment in sim
    /// time. Null where this ground has never been opened — see <see cref="OpeningOf"/> for why that is not
    /// zero.</summary>
    public static long? WindowsSince(IReadOnlyList<Opening>? register, string bodyId, double simTime)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return OpeningOf(register, bodyId) is { } opening
            ? WindowsSince(opening, WindowAt(simTime))
            : null;
    }
}
