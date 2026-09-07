using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #677 · THE DISCLOSURE CLOCK — the found halls' second half, and the one that is a clock rather than a
/// place. Owner ruling 2026-08-04 (<c>worldbuilding-notes.md</c> §10): <i>"the horror is the disclosure
/// schedule … the fourth is LET to know more and more about what is under its feet."</i> The issue's own
/// sentence is the whole spec: <b>what a captain is shown rides slow world-side windows, not search effort —
/// what you find is a fact about WHEN YOU WENT; later windows may show more; it is never a progress bar and
/// never announced.</b>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names — the reverts are
/// listed on each one, in the shape this ground has used since #587's lesson: a guard that has never failed
/// is a guard nobody has checked.</para>
///
/// <para>The vacuity pairs matter more here than usual, because the feature's ordinary answer is NOTHING.
/// A clock that never started would pass a careless sweep of "no ground is announced" perfectly. So every
/// negative law is shipped beside its positive twin, and the population the positives run over is DERIVED —
/// swept out of the generator and named in the failure message — rather than typed.</para>
/// </summary>
public sealed class TheDisclosureClockTests
{
    /// <summary>How many generated rocks the sweeps walk. The band is about one site in fifty, so a ten-site
    /// sample tells you nothing about it — the same reasoning, and the same number, as
    /// <c>TheFoundBandTests</c>'s own sweep.</summary>
    private const int Probes = 4000;

    /// <summary>EVERY SITE IN THE SWEEP THAT ACTUALLY HAS HALLS, derived and never typed, plus the rock the
    /// cheat parks — without which four of the sweeps below would audit a universe with no galleries in it
    /// and pass for the wrong reason (the fifth named bug class).
    ///
    /// <para>Asserted non-empty with a real floor rather than merely non-empty: "the 1-in-50 must be provably
    /// nonempty across the seeded sites", and a population of one is not a population.</para></summary>
    private static List<string> GroundsWithHalls()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"probe-moon-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        found.Add(UndergroundComplex.FoundBandCheatSiteId);
        Assert.True(found.Count > 40,
            $"only {found.Count} of {Probes} generated sites had halls — this proves little.");
        return found;
    }

    /// <summary>…and every site in the same sweep that has NONE, which is what almost every site is. The
    /// negative population for the vacuity pairs.</summary>
    private static List<string> GroundsWithout()
    {
        var plain = new List<string>();
        for (int i = 0; i < Probes && plain.Count < 400; i++)
        {
            string body = $"probe-moon-{i}";
            if (!UndergroundComplex.HasFoundBand(body))
            {
                plain.Add(body);
            }
        }
        Assert.True(plain.Count > 200, $"only {plain.Count} sites had no halls — the sweep is not a sweep.");
        return plain;
    }

    /// <summary>Every floor a site actually has, plus the surface and a floor below its own bottom — so the
    /// sweeps ask about ground the building does not have as well as ground it does.</summary>
    private static IEnumerable<int> FloorsAround(string body) =>
        UndergroundComplex.FloorsOf(body).Concat([0, 1, UndergroundComplex.DeepestPossibleFloor - 1]);

    // ── LAW 1 · ONE CLOCK, AND IT IS THE MONOLITH'S ──────────────────────────────────────────────────────

    /// <summary>
    /// THE WINDOW IS THE FOOT-OFFERINGS' OWN, ASKED AND NEVER RE-DERIVED.
    ///
    /// <para>The issue names that clock by name. Two copies of a window length is the mirrored-constant bug
    /// this ground keeps a table of — and it would be invisible, because both clocks would tick and only the
    /// arithmetic BETWEEN them would be wrong.</para>
    ///
    /// <para>RED against: <c>WindowSeconds =&gt; 4_000.0</c> (the same number, typed) — the equality on the
    /// constant still passes, and the sweep below fails the moment the monolith's own number is changed,
    /// which is the case a hard-coded copy exists to survive.</para>
    /// </summary>
    [Fact]
    public void TheWindowIsTheMonolithsOwnAndIsNeverReDerived()
    {
        Assert.Equal(Monolith.EpochSeconds, DisclosureClock.WindowSeconds);

        int distinct = 0;
        long? last = null;
        for (double t = -500; t < Monolith.EpochSeconds * 12; t += Monolith.EpochSeconds / 7.0)
        {
            long window = DisclosureClock.WindowAt(t);
            Assert.Equal(Monolith.EpochAt(t), window);
            if (last != window)
            {
                distinct++;
                last = window;
            }
        }

        // Coverage floor: the sweep has to have crossed real window boundaries, or it proved only that two
        // functions agree about zero.
        Assert.True(distinct >= 12, $"the sweep only saw {distinct} window(s) — it never crossed a boundary.");
        Assert.Equal(0, DisclosureClock.WindowAt(-1_000_000));   // before the world started is window zero
    }

    // ── LAW 2 · ONLY THE BAND NOBODY DUG STARTS IT ───────────────────────────────────────────────────────

    /// <summary>
    /// A CLOCK STARTS PAST THE SEAM AND NOWHERE ELSE IN THE GAME — and the halls are the ONLY floors of the
    /// only sites that can start one.
    ///
    /// <para>The band nobody listed (#592) deliberately does not: it is human all the way down, poured and
    /// invoiced and hidden from the staff who paid for it, so a captain who reaches one has found somebody's
    /// SECRET and not somebody's SCHEDULE. Neither does the listed building, nor the surface, nor a floor the
    /// site does not have.</para>
    ///
    /// <para>The positive half names the grounds it ran over, derived from the sweep, so a future change that
    /// quietly emptied the population fails here instead of passing silently.</para>
    ///
    /// <para>RED against: <c>OpensOn =&gt; level &lt; 0</c> (any deep floor starts it) — the negative sweep
    /// fails on the first ordinary rock; and <c>OpensOn =&gt; false</c> — the positive half fails naming zero
    /// galleries.</para>
    /// </summary>
    [Fact]
    public void OnlyTheBandNobodyDugStartsAClock()
    {
        List<string> withHalls = GroundsWithHalls();
        var galleries = new List<string>();

        foreach (string body in withHalls.Take(60))
        {
            foreach (int level in FloorsAround(body))
            {
                bool isHall = UndergroundComplex.IsFound(body, level);
                Assert.Equal(isHall, DisclosureClock.OpensOn(body, level));

                // …and the two floors that are most tempting to confuse with a gallery answer NO.
                if (UndergroundComplex.IsUnlisted(body, level) || level >= 0)
                {
                    Assert.False(DisclosureClock.OpensOn(body, level),
                        $"{body} B{-level} started a clock and it is not a gallery.");
                }

                if (isHall)
                {
                    galleries.Add($"{body} B{-level}");
                }
            }
        }

        Assert.True(galleries.Count >= 60,
            $"only {galleries.Count} gallery floor(s) in the sample opened a clock — nothing was proved. " +
            $"The grounds that carry the band here are: {string.Join(", ", withHalls.Take(8))}…");
    }

    /// <summary>
    /// AND A SITE WITHOUT THE BAND SHOWS NOTHING OF IT — no floor opens, and <c>Open</c> hands back null on
    /// every floor the site has, on the surface, and under its own bottom.
    ///
    /// <para>This is the vacuity twin of the law above and it is the one that would rot: a clock wired to a
    /// looser predicate would still pass "galleries open a clock" and would quietly start one on every deep
    /// site in the universe.</para>
    ///
    /// <para>RED against: <c>OpensOn =&gt; UndergroundComplex.HasUnlistedBand(bodyId) &amp;&amp; level &lt;
    /// 0</c>.</para>
    /// </summary>
    [Fact]
    public void ASiteWithoutTheBandShowsNothingOfTheClock()
    {
        List<string> plain = GroundsWithout();
        int asked = 0;

        foreach (string body in plain)
        {
            Assert.False(UndergroundComplex.HasFoundBand(body));
            foreach (int level in FloorsAround(body))
            {
                Assert.False(DisclosureClock.OpensOn(body, level),
                    $"{body} B{-level} opened a clock on a site with no halls at all.");
                Assert.Null(DisclosureClock.Open(body, level, 12_345.0));
                asked++;
            }
        }

        Assert.True(asked > 2_000, $"the negative sweep only asked {asked} floor(s).");
    }

    // ── LAW 3 · IT IS NEVER ANNOUNCED, AND THERE IS NOTHING TO SAY ───────────────────────────────────────

    /// <summary>
    /// THE CLOCK PUBLISHES NO PROSE AT ALL — no label, no line, no title, no percentage, not one string.
    ///
    /// <para>This is the never-announced law in its strongest available form. A beat that wanted to speak
    /// about the schedule would have to author its words somewhere a canon reviewer can see them, which is
    /// the whole of the standing rule that lore work is not written by an implementer — and it also settles
    /// §8 for free, because a type with no strings in it cannot contain the reserved word.</para>
    ///
    /// <para>Coverage floor included: the sweep asserts the type actually has a public surface, so a rename
    /// that emptied it could not turn this guard green by having nothing left to check.</para>
    ///
    /// <para>RED against: adding <c>public const string Label = "◷ DISCLOSURE";</c> to the type.</para>
    /// </summary>
    [Fact]
    public void TheClockPublishesNoProseAtAll()
    {
        Type clock = typeof(DisclosureClock);
        var offenders = new List<string>();
        int surface = 0;

        const BindingFlags Public = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        foreach (FieldInfo f in clock.GetFields(Public))
        {
            surface++;
            if (f.FieldType == typeof(string))
            {
                offenders.Add($"field {f.Name}");
            }
        }
        foreach (PropertyInfo p in clock.GetProperties(Public))
        {
            surface++;
            if (p.PropertyType == typeof(string))
            {
                offenders.Add($"property {p.Name}");
            }
        }
        foreach (MethodInfo m in clock.GetMethods(Public))
        {
            surface++;
            if (m.ReturnType == typeof(string) && !m.IsSpecialName && m.DeclaringType == clock)
            {
                offenders.Add($"method {m.Name}");
            }
        }

        Assert.True(surface >= 8, $"the clock's public surface is only {surface} member(s) — nothing swept.");
        Assert.True(offenders.Count == 0,
            "the disclosure clock is never announced, so it publishes no prose. Found: "
            + string.Join(", ", offenders));
    }

    // ── LAW 4 · IT IS NOT FARMABLE ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// WHAT YOU FIND IS A FACT ABOUT WHEN YOU WENT, NOT ABOUT HOW HARD YOU LOOKED.
    ///
    /// <para>Two halves. Inside ONE window the reading does not move however many times a captain goes down
    /// — the owner's object-persistence law said about knowledge instead of about objects. And the register
    /// keeps the FIRST crossing, so going back to a familiar ground can never hand the captain a fresh clock:
    /// a register that took the latest crossing would make revisiting the farm.</para>
    ///
    /// <para>RED against: <c>Note</c> replacing the stored row with the new one (the hundred re-entries read
    /// 0 instead of 3); and <c>WindowsSince</c> taking <c>register.Count</c> into account at all.</para>
    /// </summary>
    [Fact]
    public void GoingBackAgainAndAgainMovesNothing()
    {
        string body = UndergroundComplex.FoundBandCheatSiteId;
        int gallery = UndergroundComplex.FloorsOf(body).First(l => UndergroundComplex.IsFound(body, l));

        double opening = Monolith.EpochSeconds * 4.0;
        IReadOnlyList<DisclosureClock.Opening> register =
            DisclosureClock.Note([], DisclosureClock.Open(body, gallery, opening)!.Value);

        // A hundred more crossings, spread over the next three windows.
        IReadOnlyList<DisclosureClock.Opening> farmed = register;
        for (int i = 1; i <= 100; i++)
        {
            double later = opening + (Monolith.EpochSeconds * 3.0 * i / 100.0);
            farmed = DisclosureClock.Note(farmed, DisclosureClock.Open(body, gallery, later)!.Value);
        }

        Assert.Single(farmed);
        Assert.Equal(register[0], farmed[0]);

        // …and at the same moment, both registers read the same. Effort bought nothing.
        double now = opening + (Monolith.EpochSeconds * 3.0);
        Assert.Equal(
            DisclosureClock.WindowsSince(register, body, now),
            DisclosureClock.WindowsSince(farmed, body, now));
        Assert.Equal(3, DisclosureClock.WindowsSince(farmed, body, now));

        // And inside ONE window, every moment reads the same: eight visits at eight times, one answer.
        var insideOneWindow = new HashSet<long?>();
        for (int i = 0; i < 8; i++)
        {
            insideOneWindow.Add(DisclosureClock.WindowsSince(
                farmed, body, opening + (Monolith.EpochSeconds * i / 8.0)));
        }
        Assert.Single(insideOneWindow);
    }

    // ── LAW 5 · IT NEVER RUNS BACKWARDS ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// LATER WINDOWS MAY SHOW MORE — WHICH IS A PROMISE ABOUT DIRECTION, so the reading is monotone
    /// non-decreasing in sim time and floored at zero before the ground was opened.
    ///
    /// <para>A clock that could tick down would break that promise silently: a beat gated on "at least N
    /// windows" would fire and then un-fire, and nothing on screen would ever say why.</para>
    ///
    /// <para>RED against: dropping the <c>Math.Max(0, …)</c> floor (the pre-opening half goes negative, and a
    /// threshold written as <c>&gt;= 0</c> would be true forever); and against a <c>WindowsSince</c> that
    /// subtracted the other way round.</para>
    /// </summary>
    [Fact]
    public void TheReadingOnlyEverGrows()
    {
        var opening = new DisclosureClock.Opening("probe-moon-1", 6);

        long last = long.MinValue;
        int grew = 0;
        for (long window = 0; window <= 40; window++)
        {
            long reading = DisclosureClock.WindowsSince(opening, window);
            Assert.True(reading >= 0, $"window {window} read {reading}, which is behind the opening.");
            Assert.True(reading >= last, $"window {window} read {reading} after {last}.");
            if (reading > last)
            {
                grew++;
            }
            last = reading;
        }

        Assert.Equal(0, DisclosureClock.WindowsSince(opening, 0));   // before it was opened: nothing, not less
        Assert.Equal(0, DisclosureClock.WindowsSince(opening, 6));   // the window it was opened in
        Assert.Equal(34, DisclosureClock.WindowsSince(opening, 40));
        Assert.True(grew >= 34, $"the reading only moved {grew} time(s) over forty windows.");
    }

    /// <summary>
    /// A GROUND NEVER OPENED READS NOTHING, WHICH IS NOT ZERO.
    ///
    /// <para>The distinction is the whole point and it is exactly the shape that rots into a bug: a site
    /// opened in this very window reads zero windows since, and a site nobody has ever been under reads
    /// null. A beat that treated the two the same would fire on every moon in the universe on its first
    /// tick.</para>
    ///
    /// <para>RED against: <c>WindowsSince(register, …)</c> returning <c>0</c> in place of null.</para>
    /// </summary>
    [Fact]
    public void AGroundNeverOpenedReadsNothingAndNotZero()
    {
        string body = UndergroundComplex.FoundBandCheatSiteId;
        int gallery = UndergroundComplex.FloorsOf(body).First(l => UndergroundComplex.IsFound(body, l));

        Assert.Null(DisclosureClock.WindowsSince(null, body, 50_000.0));
        Assert.Null(DisclosureClock.WindowsSince([], body, 50_000.0));
        Assert.Null(DisclosureClock.OpeningOf([], body));

        IReadOnlyList<DisclosureClock.Opening> register =
            DisclosureClock.Note([], DisclosureClock.Open(body, gallery, 50_000.0)!.Value);

        Assert.Equal(0, DisclosureClock.WindowsSince(register, body, 50_000.0));   // opened, this window
        Assert.Null(DisclosureClock.WindowsSince(register, "some-other-rock", 50_000.0));   // never opened
    }

    /// <summary>
    /// THE REGISTER KEEPS ONE ROW PER GROUND AND CARRIES EACH GROUND'S OWN WINDOW.
    ///
    /// <para>Two grounds opened in two different windows must read two different numbers at the same moment,
    /// or the clock is a fact about the captain rather than about the place — and the whole arc it gates
    /// (#1063's burial, #1074's stop orders) is about what happens to ONE ground.</para>
    ///
    /// <para>RED against: a register keyed on nothing (one global window) — the two readings collapse.</para>
    /// </summary>
    [Fact]
    public void EachGroundCarriesItsOwnWindow()
    {
        List<string> withHalls = GroundsWithHalls();
        string first = withHalls[0], second = withHalls[1];
        Assert.NotEqual(first, second);

        int a = UndergroundComplex.FloorsOf(first).First(l => UndergroundComplex.IsFound(first, l));
        int b = UndergroundComplex.FloorsOf(second).First(l => UndergroundComplex.IsFound(second, l));

        IReadOnlyList<DisclosureClock.Opening> register = DisclosureClock.Note(
            [], DisclosureClock.Open(first, a, Monolith.EpochSeconds * 2.0)!.Value);
        register = DisclosureClock.Note(
            register, DisclosureClock.Open(second, b, Monolith.EpochSeconds * 9.0)!.Value);

        Assert.Equal(2, register.Count);

        double now = Monolith.EpochSeconds * 11.0;
        Assert.Equal(9, DisclosureClock.WindowsSince(register, first, now));
        Assert.Equal(2, DisclosureClock.WindowsSince(register, second, now));
    }

    /// <summary>
    /// NOTHING TO ADD MEANS NOTHING CHANGES — the register comes back by reference, so a caller can compare
    /// and only then ask for a save.
    ///
    /// <para>Not a micro-optimisation: a save requested on every arrival past a seam is a save requested on
    /// every ride in a familiar building, and the honest signal for "something happened" is that the register
    /// is a different object.</para>
    ///
    /// <para>RED against: <c>Note</c> always allocating a new list.</para>
    /// </summary>
    [Fact]
    public void AnAlreadyOpenGroundHandsTheSameRegisterBack()
    {
        string body = UndergroundComplex.FoundBandCheatSiteId;
        int gallery = UndergroundComplex.FloorsOf(body).First(l => UndergroundComplex.IsFound(body, l));
        var opening = new DisclosureClock.Opening(body, 3);

        IReadOnlyList<DisclosureClock.Opening> first = DisclosureClock.Note([], opening);
        IReadOnlyList<DisclosureClock.Opening> again = DisclosureClock.Note(first, opening);
        Assert.Same(first, again);

        IReadOnlyList<DisclosureClock.Opening> laterVisit = DisclosureClock.Note(
            first, DisclosureClock.Open(body, gallery, Monolith.EpochSeconds * 20.0)!.Value);
        Assert.Same(first, laterVisit);   // the same ground, a later window: still nothing to add
    }

    // ── The chain, end to end ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FINDING THE BAND STARTS SOMETHING, AND ONLY FINDING THE BAND DOES.
    ///
    /// <para>The whole of v1 in one walk: take a ground the generator gives halls to, ride every floor it
    /// has in order, and watch the clock start on exactly the floors past the seam and on no other. This is
    /// the guard the downstream arc leans on — #1068 and #1074 both wait for "#677 giving the thresholds
    /// something to guard", and this is the threshold.</para>
    ///
    /// <para>RED against every revert above; it is the integration twin of law 2.</para>
    /// </summary>
    [Fact]
    public void RidingTheWholeBuildingStartsTheClockAtTheSeamAndNowhereElse()
    {
        string body = UndergroundComplex.FoundBandCheatSiteId;
        Assert.True(UndergroundComplex.HasFoundBand(body));

        IReadOnlyList<DisclosureClock.Opening> register = [];
        int started = 0, floors = 0;
        double t = 0;

        foreach (int level in UndergroundComplex.FloorsOf(body))
        {
            floors++;
            t += Monolith.EpochSeconds / 3.0;   // a long descent; the windows turn under it
            IReadOnlyList<DisclosureClock.Opening> before = register;
            if (DisclosureClock.Open(body, level, t) is { } opening)
            {
                register = DisclosureClock.Note(register, opening);
            }

            bool ticked = !ReferenceEquals(before, register);
            Assert.Equal(UndergroundComplex.IsFound(body, level) && started == 0, ticked);
            if (ticked)
            {
                started++;
            }
        }

        Assert.True(floors > 12, $"{body} only has {floors} floors — the walk proved little.");
        Assert.Equal(1, started);   // one ground, one clock, however many galleries it has

        // …and it started on the TOP gallery, which is the floor the seam is crossed onto.
        int top = UndergroundComplex.FloorsOf(body).First(l => UndergroundComplex.IsFound(body, l));
        Assert.True(UndergroundComplex.IsFound(body, top));
        Assert.Single(register);
        Assert.Equal(body, register[0].BodyId);
    }
}
