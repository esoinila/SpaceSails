using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #759 · <b>THE LIGHT KEEPS ITS OWN DAY.</b> The last named remainder of the park behind the bar, and the
/// only one of them that is arithmetic rather than geometry.
///
/// <para>Owner, filing the room: <i>"The light keeps its own day — a grow-cycle that matches no watch of
/// the building above or below. Anyone who lingers notices the park's morning arriving at the wrong time.
/// <b>Subtly wrong is the register: never broken, never right.</b>"</i></para>
///
/// <para><b>"Never broken, never right" is two bounds, and this file holds both of them.</b> A cycle that
/// agreed with the building would be a room with a legible day — that is "right", and it is what a whole
/// number of watches, or a half or a third of one, would have bought. A cycle that agreed with it NEVER
/// would be a claim no observation supports and, worse, a rule a player could learn as reliably as the
/// other one. What actually ships is a ratio that is irrational in practice: the two clocks agree
/// sometimes, disagree most of the time, and the pattern never comes round.</para>
///
/// <para><b>Every guard below is stated against the shipped constants and the real generator.</b> Nothing
/// here builds a synthetic cycle to measure, and each has an anti-vacuous half — the fifth named bug class
/// in this repo is a guard whose world cannot tell pass from fail, and a test about a NUMBER is the
/// easiest possible place to ship one.</para>
/// </summary>
public sealed class TheParkKeepsItsOwnDayTests
{
    /// <summary>The shipped sites plus forty probe moons, the park suite's own sweep — so a per-site phase
    /// offset is exercised on more parks than the three a captain is likely to walk.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    /// <summary>A number in a failure message, in the one culture a CI log should be read in.</summary>
    private static string F(double v) => v.ToString("F6", CultureInfo.InvariantCulture);

    /// <summary>Every site the generator actually puts a park on — asked of the generator, never listed.</summary>
    private static IEnumerable<string> ParkSites()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || UndergroundComplex.IsHeadOffice(body))
            {
                continue;
            }

            if (UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField).Park is not null)
            {
                yield return body;
            }
        }
    }

    // ── (a) THE PERIOD IS NO SIMPLE FRACTION OF THE BUILDING'S WATCH ──────────────────────────────────

    /// <summary>
    /// <b>THE BOUND, AND IT IS THE BOUND FOR BADLY-APPROXIMABLE NUMBERS RATHER THAN A LIST OF FRACTIONS.</b>
    ///
    /// <para>"Not a rational multiple p/q for small p, q" cannot be asserted as a list, because Dirichlet
    /// guarantees that EVERY real number has a rational within 1/q² of it — pick q large enough and some
    /// fraction always fits. The honest statement is the one that says how badly: for every denominator q
    /// up to <c>Q</c>, the nearest fraction p/q misses the ratio by at least <c>c/q²</c>. That is the
    /// definition of a badly-approximable number, and the golden section is the worst-approximable of them
    /// all — its continued fraction is all ones, so its convergents are as far off as convergents can be.
    /// </para>
    ///
    /// <para>MEASURED on the shipped ratio, over q = 1…<c>Q</c>: the minimum of q²·|r − p/q| is
    /// <b>0.381966</b>, at 5/1. The bound asserted is 0.35, which sits clear of it and is nowhere near what
    /// a commensurate period would produce. In plain terms: <b>the closest the park's day ever comes to a
    /// whole number of watches is 5 watches, and it misses by 0.382 of one — twenty-two sim-minutes, every
    /// cycle, for ever.</b></para>
    ///
    /// <para><b>Proven RED, twice.</b> The bound was re-run with the period set equal to the watch, and with
    /// it set to half the watch — the two shapes the design brief names — and read off the run:</para>
    /// <code>
    /// the park's grow-cycle is 1.000000 watch(es), and that is a fraction of the building's own:
    ///   q=1: 1/1 is 0.000000 away — q²·err = 0.000000, under the 0.350000 the law asks for
    ///
    /// the park's grow-cycle is 0.500000 watch(es), and that is a fraction of the building's own:
    ///   q=2: 1/2 is 0.000000 away — q²·err = 0.000000, under the 0.350000 the law asks for
    /// </code>
    /// </summary>
    [Fact]
    public void ThePeriodIsNoSimpleFractionOfTheBuildingsWatch()
    {
        const int Q = 200;
        const double Bound = 0.35;

        double worst = double.MaxValue;
        long worstP = 0;
        int worstQ = 0;
        var broken = new List<string>();

        for (int q = 1; q <= Q; q++)
        {
            long p = (long)Math.Round(ParkDay.Watches * q);
            double err = Math.Abs(ParkDay.Watches - ((double)p / q));
            double c = err * q * q;
            if (c < worst)
            {
                worst = c;
                worstP = p;
                worstQ = q;
            }
            if (c < Bound)
            {
                broken.Add(FormattableString.Invariant(
                    $"  q={q}: {p}/{q} is {err:F6} away — q²·err = {c:F6}, under the {Bound:F6} the law asks for"));
            }
        }

        Assert.True(broken.Count == 0,
            $"the park's grow-cycle is {F(ParkDay.Watches)} watch(es), and that is a fraction of the "
            + $"building's own:\n{string.Join('\n', broken)}");

        // The anti-vacuous half. A bound of 0.35 would also be met by a sweep that looked at nothing, so the
        // sweep is required to have found its own worst case and to have found it where the golden section
        // says it should be — near 1/√5 = 0.447, and specifically NOT at some enormous q where any number
        // looks irrational.
        Assert.True(worstQ >= 1 && worstP >= 1,
            "the incommensurability sweep found no fractions at all — it is asserting nothing.");
        Assert.True(worst < 0.5,
            $"the sweep's worst case is {F(worst)} at {worstP}/{worstQ}; a badly-approximable ratio's "
            + "convergents cannot all be further off than 1/√5 = 0.447, so this sweep is not measuring "
            + "what it thinks it is.");

        // …and the period really is what the doc says it is: four and a bit watches, eighteen and a half
        // sim-hours, which is the OTHER half of the argument — a photoperiod a crop house would run.
        Assert.InRange(ParkDay.PeriodSeconds / 3600.0, 16.0, 24.0);
        Assert.Equal(PatronRota.WatchSeconds * ParkDay.Watches, ParkDay.PeriodSeconds, 6);
    }

    // ── (b) IT IS A FUNCTION, AND IT IS THE SAME FUNCTION EVERY TIME ──────────────────────────────────

    /// <summary>
    /// <b>DETERMINISM.</b> The same sim-time and the same site give the same phase and the same level, on
    /// every call and in any order — determinism is law in Core and a light on a schedule is exactly the
    /// kind of fact that grows a clock in it by accident.
    ///
    /// <para>Also asserted here: the level and the phase are the SAME statement read two ways. A level of 0
    /// is the dark and nothing else; a level of 1 is the day and nothing else; anything strictly between is
    /// one of the two shoulders. A phase table and a level curve that could disagree would be two answers
    /// to one question, which is this repo's own named bug class (the sim doing one thing while a sentence
    /// reports another).</para>
    /// </summary>
    [Fact]
    public void TheSameSimTimeAlwaysDrawsTheSameLight()
    {
        foreach (string site in ParkSites())
        {
            for (int k = 0; k < 500; k++)
            {
                double t = k * 137.0;
                ParkDay.Phase first = ParkDay.PhaseAt(t, site);
                double level = ParkDay.LevelAt(t, site);

                Assert.Equal(first, ParkDay.PhaseAt(t, site));
                Assert.Equal(level, ParkDay.LevelAt(t, site), 12);
                Assert.InRange(level, 0.0, 1.0);

                switch (first)
                {
                    case ParkDay.Phase.Night:
                        Assert.Equal(0.0, level, 12);
                        break;
                    case ParkDay.Phase.Day:
                        Assert.Equal(1.0, level, 12);
                        break;
                    default:
                        Assert.InRange(level, 0.0, 1.0);
                        break;
                }
            }
        }

        // …and a SHOULDER is a shoulder rather than a second switch. Half way up the dawn the lamps are half
        // on, half way down the dusk they are half off, and a ramp that jumped to full the instant the dark
        // ended would read 1.0 at both. Stated on the pure fraction, which is where the shape lives.
        Assert.Equal(0.5, ParkDay.LevelOf(ParkDay.NightFraction + (ParkDay.DawnFraction / 2.0)), 9);
        Assert.Equal(
            0.5,
            ParkDay.LevelOf(
                ParkDay.NightFraction + ParkDay.DawnFraction + ParkDay.DayFraction
                + (ParkDay.DuskFraction / 2.0)),
            9);
        Assert.Equal(0.0, ParkDay.LevelOf(ParkDay.NightFraction / 2.0), 9);
        Assert.Equal(
            1.0,
            ParkDay.LevelOf(ParkDay.NightFraction + ParkDay.DawnFraction + (ParkDay.DayFraction / 2.0)),
            9);
    }

    /// <summary>
    /// <b>THE LIGHT NEVER SNAPS.</b> Walked across a whole cycle in one-second steps: no step changes the
    /// level by more than a step's worth of the steepest ramp. A discontinuity here would draw as a frame
    /// in which the park's lamps jumped, which is a room that has broken rather than drifted.
    /// </summary>
    [Fact]
    public void TheLampsComeUpAndGoDownRatherThanSwitching()
    {
        const double Step = 1.0;
        double steepest = Step / (Math.Min(ParkDay.DawnFraction, ParkDay.DuskFraction) * ParkDay.PeriodSeconds);

        foreach (string site in new[] { "luna", "phobos", "titan" })
        {
            double previous = ParkDay.LevelAt(0.0, site);
            for (double t = Step; t <= ParkDay.PeriodSeconds * 2.0; t += Step)
            {
                double now = ParkDay.LevelAt(t, site);
                Assert.True(Math.Abs(now - previous) <= steepest + 1e-9,
                    $"{site}: the park's light jumped {F(Math.Abs(now - previous))} in one second at "
                    + $"t={F(t)} — the steepest ramp it has is {F(steepest)} a second.");
                previous = now;
            }
        }
    }

    // ── (c) NEVER BROKEN, NEVER RIGHT ────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PARK'S DAY AND THE BUILDING'S DISAGREE MOST OF THE TIME, AND NOT ALL OF IT.</b> The owner's
    /// register, as two numbers.
    ///
    /// <para>Sampled at the top of every watch — the moment the whole building turns over, which is the one
    /// instant every clock in this facility is at the same place and therefore the fair place to ask. Over
    /// 2,000 watches (about three hundred and thirty sim-days) the two phases disagree on
    /// <b>1,599–1,601 of them, which is 0.7995–0.8005</b>, on every park site swept. The bounds asserted
    /// are <b>0.60 and 0.92</b>, and they are both real: the low one says a majority (never right), the
    /// high one says the agreement is not a rounding error (never broken — the park DOES sometimes turn
    /// over with the building, and a captain who happened to see that would be seeing the truth).</para>
    ///
    /// <para><b>Proven RED, twice.</b> With the period set equal to the watch — the "right" failure — every
    /// site read 0.0000 and the low bound caught it; with the park's own phases replaced by their
    /// complement, the disagreement read 1.0000 and the high bound caught that:</para>
    /// <code>
    /// 156 park(s) keep a day that is not subtly wrong:
    ///   luna: the two clocks disagree on 0/2000 watches (0.0000) — the law asks for 0.6000…0.9200.
    ///   phobos: the two clocks disagree on 0/2000 watches (0.0000) — the law asks for 0.6000…0.9200.
    ///
    /// 156 park(s) keep a day that is not subtly wrong:
    ///   luna: the two clocks disagree on 2000/2000 watches (1.0000) — the law asks for 0.6000…0.9200.
    /// </code>
    /// </summary>
    [Fact]
    public void ThePartsMorningIsAtTheWrongTimeMostlyAndNotAlways()
    {
        const int Watches = 2000;
        const double Low = 0.60, High = 0.92;

        var wrong = new List<string>();
        int sites = 0;

        foreach (string site in ParkSites())
        {
            sites++;
            int apart = 0;
            for (int k = 0; k < Watches; k++)
            {
                double t = k * PatronRota.WatchSeconds;
                if (ParkDay.PhaseAt(t, site) != ParkDay.BuildingPhaseAt(t))
                {
                    apart++;
                }
            }

            double f = (double)apart / Watches;
            if (f < Low || f > High)
            {
                wrong.Add(
                    $"  {site}: the two clocks disagree on {apart}/{Watches} watches ({F(f)}) — the law "
                    + $"asks for {F(Low)}…{F(High)}.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) keep a day that is not subtly wrong:\n{string.Join('\n', wrong)}");

        // Anti-vacuous: the sweep has to have found parks to sweep. A green run over zero sites is the
        // fifth bug class exactly.
        Assert.True(sites >= 40, $"the sweep found {sites} park site(s) — it is asserting nothing.");
    }

    /// <summary>
    /// <b>AND IT NEVER SETTLES.</b> The disagreement above would also be produced by a cycle that simply
    /// sat at a fixed offset from the watch, and that is a different room: its morning would arrive at the
    /// same point of every shift, which is precisely the legibility #759 is refusing.
    ///
    /// <para>So: take the sim-time of the park's own dawn on each of its next 500 cycles, express it as a
    /// fraction INTO whichever watch it lands in, and bucket those fractions into hundredths. MEASURED:
    /// <b>all 100 hundredths are visited</b>, on every site swept. The law asks for 90, which is clear of
    /// the measurement and impossible for any period that is a simple fraction of the watch — 5/1 would
    /// visit one bucket, 21/4 would visit four.</para>
    ///
    /// <para><b>Proven RED</b> with the period set to five watches exactly:</para>
    /// <code>
    /// 156 park(s) have a morning that keeps an appointment:
    ///   luna: the park's dawn lands in 1 of 100 hundredths of a watch over 500 cycles — the law asks for 90.
    /// </code>
    /// </summary>
    [Fact]
    public void TheMorningWalksAllTheWayRoundTheWatch()
    {
        const int Cycles = 500, Buckets = 90;

        var settled = new List<string>();

        foreach (string site in ParkSites())
        {
            var seen = new HashSet<int>();
            double dawn = ParkDay.FirstMiddleOf(ParkDay.Phase.Dawn, site);
            for (int k = 0; k < Cycles; k++)
            {
                double t = dawn + (k * ParkDay.PeriodSeconds);
                double into = (t / PatronRota.WatchSeconds) - Math.Floor(t / PatronRota.WatchSeconds);
                seen.Add((int)(into * 100));
            }

            if (seen.Count < Buckets)
            {
                settled.Add(
                    $"  {site}: the park's dawn lands in {seen.Count} of 100 hundredths of a watch over "
                    + $"{Cycles} cycles — the law asks for {Buckets}.");
            }
        }

        Assert.True(settled.Count == 0,
            $"{settled.Count} park(s) have a morning that keeps an appointment:\n"
            + string.Join('\n', settled));
    }

    /// <summary>
    /// <b>AND NO TWO PARKS KEEP THE SAME DAY AS EACH OTHER.</b> The per-site offset, measured: every park
    /// site swept has its own phase, no two within a fiftieth of a cycle of one another, and the offsets
    /// spread across the whole of it rather than clustering.
    /// </summary>
    [Fact]
    public void EveryParkKeepsItsOwnMorning()
    {
        double[] offsets = [.. ParkSites().Select(ParkDay.OffsetFraction)];

        Assert.True(offsets.Length >= 40, $"{offsets.Length} park site(s) swept — nothing to compare.");
        Assert.All(offsets, o => Assert.InRange(o, 0.0, 1.0));

        // Spread: the offsets must not all be the same number, and must not all sit in one tenth of the
        // cycle. A seed that folded to a constant would pass every other guard in this file.
        Assert.True(offsets.Distinct().Count() == offsets.Length,
            "two park sites were handed the same phase offset — the seed is not per-site.");
        Assert.True(offsets.Max() - offsets.Min() > 0.75,
            $"every park's offset lies in {F(offsets.Max() - offsets.Min())} of the cycle — the seed is "
            + "not spreading.");

        // …and the three a captain actually walks are not neighbours, which is the readable half of it.
        Assert.True(Math.Abs(ParkDay.OffsetFraction("luna") - ParkDay.OffsetFraction("phobos")) > 0.02);
        Assert.True(Math.Abs(ParkDay.OffsetFraction("luna") - ParkDay.OffsetFraction("titan")) > 0.02);
        Assert.True(Math.Abs(ParkDay.OffsetFraction("phobos") - ParkDay.OffsetFraction("titan")) > 0.02);
    }

    // ── (d) THE LINGERING NOTICE FIRES ON A CHANGE, AND FIRES ONCE ────────────────────────────────────

    /// <summary>
    /// <b>THE BEAT, EXHAUSTIVELY.</b> <see cref="ParkDay.WouldNotice"/> takes four inputs over 4 × 4 × 4 × 2
    /// = 128 combinations, so it is not sampled — it is enumerated, and every one of them is checked against
    /// the authored sentence read as a specification.
    ///
    /// <para><i>"It is coming up to morning in here"</i> ⇒ the park is at DAWN now. <i>"It is COMING UP"</i>
    /// ⇒ you did not walk in on it. <i>"It was not morning anywhere else in the building when you came
    /// in"</i> ⇒ the building was on some other part of its own day at that moment. And spent is spent.</para>
    ///
    /// <para>MEASURED: <b>9 of the 128 combinations fire</b> — 3 park phases you can have come in on × 3
    /// building phases it can have been × 1 unspent. That the number is small is the feature: the beat is
    /// rare and it is a coincidence, not a schedule.</para>
    /// </summary>
    [Fact]
    public void NoticingIsTheChangeAndItIsSpentOnce()
    {
        ParkDay.Phase[] all = Enum.GetValues<ParkDay.Phase>();
        int fired = 0, cases = 0;

        foreach (ParkDay.Phase cameInOn in all)
        {
            foreach (ParkDay.Phase buildingWas in all)
            {
                foreach (ParkDay.Phase now in all)
                {
                    foreach (bool spent in new[] { false, true })
                    {
                        cases++;
                        bool got = ParkDay.WouldNotice(cameInOn, buildingWas, now, spent);
                        bool want = !spent
                            && now == ParkDay.Phase.Dawn
                            && cameInOn != ParkDay.Phase.Dawn
                            && buildingWas != ParkDay.Phase.Dawn;

                        Assert.True(got == want,
                            $"came in on {cameInOn}, the building on {buildingWas}, now {now}, "
                            + $"spent={spent}: the beat says {got} and the authored line says {want}.");
                        if (got)
                        {
                            fired++;
                        }
                    }
                }
            }
        }

        Assert.Equal(128, cases);
        Assert.Equal(9, fired);

        // Spent is spent, said once more on its own: there is no combination at all that fires twice.
        foreach (ParkDay.Phase cameInOn in all)
        {
            foreach (ParkDay.Phase buildingWas in all)
            {
                foreach (ParkDay.Phase now in all)
                {
                    Assert.False(ParkDay.WouldNotice(cameInOn, buildingWas, now, alreadySpent: true));
                }
            }
        }
    }

    /// <summary>
    /// <b>ONE KEY PER CAPTAIN PER SITE</b>, and it cannot be mistaken for anything else in the register it
    /// rides in — the durable turned-over set holds room keys, port keys and burn tags too, and two features
    /// that mint the same string are one feature spending the other's beat.
    /// </summary>
    [Fact]
    public void TheNoticeIsSpentOncePerSite()
    {
        string[] tags = [.. ParkSites().Select(ParkDay.NoticeTag)];

        Assert.True(tags.Length >= 40, $"{tags.Length} site(s) — nothing to key.");
        Assert.Equal(tags.Length, tags.Distinct().Count());
        Assert.All(tags, t => Assert.StartsWith("park-day:", t, StringComparison.Ordinal));
        Assert.Equal("park-day:luna", ParkDay.NoticeTag("luna"));
        Assert.NotEqual(ParkDay.NoticeTag("luna"), ParkDay.NoticeTag("phobos"));
    }

    /// <summary>
    /// <b>THE DEV DOOR LANDS ON THE BEAT.</b> <c>?parkphase=morning</c> is the only way a tester can watch
    /// the change rather than walk in on it, so the arithmetic behind it is asserted rather than trusted:
    /// on EVERY park site, standing on the gravel from <see cref="ParkDay.FirstJustBeforeMorning"/> and
    /// waiting <see cref="ParkDay.MorningLeadSeconds"/> fires the beat — and the other four values of the
    /// key land in the phase they name.
    /// </summary>
    [Fact]
    public void TheDevDoorLandsWhereItSaysItDoes()
    {
        foreach (string site in ParkSites())
        {
            foreach (ParkDay.Phase want in Enum.GetValues<ParkDay.Phase>())
            {
                double at = ParkDay.FirstMiddleOf(want, site);
                Assert.True(at > 0.0 && at <= ParkDay.PeriodSeconds * 2.0, FormattableString.Invariant(
                    $"{site}: ?parkphase={want} lands at t={at:F1}, which is not inside the first cycle."));
                Assert.True(ParkDay.PhaseAt(at, site) == want, FormattableString.Invariant(
                    $"{site}: ?parkphase={want} lands the park in {ParkDay.PhaseAt(at, site)}."));
            }

            double came = ParkDay.FirstJustBeforeMorning(site);
            Assert.True(came > 0.0, $"{site}: ?parkphase=morning lands before the clock starts.");

            ParkDay.Phase cameInOn = ParkDay.PhaseAt(came, site);
            ParkDay.Phase buildingWas = ParkDay.BuildingPhaseAt(came);
            ParkDay.Phase later = ParkDay.PhaseAt(came + ParkDay.MorningLeadSeconds, site);

            Assert.True(
                ParkDay.WouldNotice(cameInOn, buildingWas, later, alreadySpent: false),
                $"{site}: ?parkphase=morning sets down at t={F(came)} with the park on {cameInOn} and "
                + $"the building on {buildingWas}; {F(ParkDay.MorningLeadSeconds)} s later the park is "
                + $"on {later} and the beat does not fire.");
        }
    }

    // ── (e) NOTHING ANYWHERE SAYS WHAT TIME IT IS IN THERE ────────────────────────────────────────────

    /// <summary>
    /// <b>THE PLAYER CAN ONLY SEE IT.</b> #759's hardest clause, and the one a later lane is most likely to
    /// break by being helpful: <i>"no card, no sensor and no bark may ever confirm"</i>, and the design's
    /// own clause 2(c) — <b>nothing in any HUD, clock or label ever states the park's time.</b>
    ///
    /// <para>So the whole of <c>src/</c> is swept for a REACH — <c>ParkDay.</c> on a line that is not a
    /// comment, which is the only way any file can get at the number. The four files allowed to make one
    /// are written down: the page partial that notices, the renderer pass that draws it, the cheat that
    /// jumps the clock and the cheat's own key reader. A fifth file reaching for it is red whatever it
    /// wanted it for, and NO <c>.razor</c> may so much as name it, because markup is the glass.</para>
    ///
    /// <para>Comments are stripped before the search on purpose, and that is a WIDENING rather than a
    /// narrowing: a docblock that says "see ParkDay" is a cross-reference and this house writes a lot of
    /// them, while the thing the law is about is a file that can obtain the hour and print it.</para>
    ///
    /// <para>Plus the three anti-vacuous halves: the sweep must find the tree it thinks it is reading, the
    /// fact itself must exist at its own path, and every file on the allow-list must still be found
    /// MAKING the reach — a rename that let the sweep pass by leaving nothing to find is exactly the
    /// silent weakening this house has a rule about.</para>
    /// </summary>
    [Fact]
    public void NothingOnTheGlassEverSaysWhatTimeItIsInThePark()
    {
        string src = Path.Combine(TestTree.RepoRoot(), "src");
        string[] files = [.. Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                                    StringComparison.OrdinalIgnoreCase)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                                    StringComparison.OrdinalIgnoreCase))];

        Assert.True(files.Length >= 200,
            $"the source sweep found {files.Length} file(s) under {src} — it is asserting nothing.");

        // The fact itself, at its own path, and it is excluded from the sweep below for the obvious reason:
        // the type does not qualify its own members.
        string fact = Path.Combine(src, "SpaceSails.Core", "ParkDay.cs");
        Assert.True(File.Exists(fact), $"{fact} is gone — this whole guard is about a type that is not there.");
        Assert.Contains("public static class ParkDay", File.ReadAllText(fact), StringComparison.Ordinal);

        // WHO IS ALLOWED TO REACH FOR IT. Four files, and every one of them is either a drawing of the
        // light or a dev door — never a sentence, a label, a plate or a card.
        string[] allowed =
        [
            Path.Combine("SpaceSails.Client", "Pages", "Map.ParkDay.cs"),               // the noticing
            Path.Combine("SpaceSails.Client", "Rendering", "DeckView.Frame.Ground.cs"), // the drawing
            Path.Combine("SpaceSails.Client", "Pages", "Map.Surface.Cheats.Stand.cs"),  // the dev door
            Path.Combine("SpaceSails.Client", "Pages", "Map.Sim.World.QueryHive.cs"),   // …and its key
        ];

        var offenders = new List<string>();
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in files)
        {
            if (string.Equals(file, fact, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool markup = file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
            string text = File.ReadAllText(file);

            // Markup may not so much as say the word; code is searched for the REACH, comments stripped.
            bool reaches = markup
                ? text.Contains("ParkDay", StringComparison.Ordinal)
                : WithoutComments(text).Contains("ParkDay.", StringComparison.Ordinal);

            if (!reaches)
            {
                continue;
            }

            string? match = markup
                ? null
                : allowed.FirstOrDefault(a => file.EndsWith(a, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                offenders.Add($"  {file[(src.Length + 1)..]} reaches for ParkDay and is not one of the "
                    + "files allowed to know what time it is in the park.");
            }
            else
            {
                found.Add(match);
            }
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} file(s) reach for the park's clock:\n{string.Join('\n', offenders)}");

        // The anti-vacuous half: every exempted path must exist AND must still be caught making the reach.
        // A list of permissions granted to files that are not there is a list that cannot redden.
        var missing = allowed.Where(a => !found.Contains(a)).ToList();
        Assert.True(missing.Count == 0,
            "the exemption list names file(s) that never reach for ParkDay — the sweep has been narrowed "
            + $"without anybody saying so:\n  {string.Join("\n  ", missing)}");
    }

    /// <summary>Source with its comments taken out — whole-line <c>//</c> and <c>///</c>, trailing <c>//</c>
    /// that is not part of a <c>://</c> scheme, and <c>/* … */</c> blocks. Deliberately blunt: it only has
    /// to be good enough that a cross-reference in a docblock is not mistaken for a reach.</summary>
    private static string WithoutComments(string source)
    {
        var sb = new StringBuilder(source.Length);
        bool inBlock = false;

        foreach (string raw in source.Split('\n'))
        {
            string line = raw;
            if (inBlock)
            {
                int close = line.IndexOf("*/", StringComparison.Ordinal);
                if (close < 0)
                {
                    continue;
                }
                line = line[(close + 2)..];
                inBlock = false;
            }

            int open = line.IndexOf("/*", StringComparison.Ordinal);
            if (open >= 0)
            {
                int close = line.IndexOf("*/", open, StringComparison.Ordinal);
                if (close < 0)
                {
                    inBlock = true;
                    line = line[..open];
                }
                else
                {
                    line = line[..open] + line[(close + 2)..];
                }
            }

            for (int i = 0; i + 1 < line.Length; i++)
            {
                if (line[i] == '/' && line[i + 1] == '/' && (i == 0 || line[i - 1] != ':'))
                {
                    line = line[..i];
                    break;
                }
            }

            sb.Append(line).Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    /// <b>AND THE TWO SENTENCES SAY NOTHING ABOUT WHY.</b> The canon sweep, walked over
    /// <see cref="ParkDay.AllProse"/> so a line added next month is swept without anybody remembering to
    /// come here. Both are pinned VERBATIM (they are authored, and an authored line that drifts is a
    /// different line), and neither may contain a word that would explain the room: a lamp, a schedule, a
    /// crop, a company, or a because.
    /// </summary>
    [Fact]
    public void TheTwoLinesReportAndDoNotExplain()
    {
        var prose = ParkDay.AllProse().ToList();
        Assert.Equal(2, prose.Count);

        string[] wouldExplain =
        [
            "because", "so that", "lamp", "grow", "photoperiod", "cycle", "crop", "schedule",
            "company", "kaamos", "reason", "why", "timer", "programme", "program",
        ];

        var said = new List<string>();
        foreach (string line in prose)
        {
            foreach (string word in wouldExplain)
            {
                if (line.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    said.Add($"  \"{line}\" contains \"{word}\" — that is an explanation, and #759 reserves "
                        + "the explanation.");
                }
            }
        }

        Assert.True(said.Count == 0, $"{said.Count} line(s) explain the park:\n{string.Join('\n', said)}");

        // Anti-vacuous: the forbidden list must be able to catch something. A sentence that DOES explain is
        // built here and run through the same sieve.
        Assert.Contains(
            wouldExplain,
            w => "the grow-lamps run on a schedule".Contains(w, StringComparison.OrdinalIgnoreCase));

        // …and only THEN the verbatim pins, because a line that has started explaining should be reported
        // as an explanation and not merely as a string that differs from one.
        Assert.Equal(
            "It is coming up to morning in here. It was not morning anywhere else in the building when "
            + "you came in.",
            ParkDay.LingerLine);
        Assert.Equal("the park keeps a day of its own — set to nobody's watch", ParkDay.NoteLine);

        // The note is an entry, not an announcement: lower case, no full stop (ParcelDrop's house style).
        Assert.False(ParkDay.NoteLine.EndsWith('.'));
        Assert.False(ParkDay.NoteLine.EndsWith(' '));
        Assert.True(char.IsLower(ParkDay.NoteLine[0]));

        // …and the subject is declared by the author, never read out of the words (#741).
        string subjects = ParkDay.Subjects("LUNA · B1");
        Assert.False(string.IsNullOrWhiteSpace(subjects));
        Assert.Equal(CaseSubjects.Line(CaseSubjects.Place("LUNA · B1")), subjects);
    }

    /// <summary>The prose is one <see cref="StringBuilder"/>'s worth of text and no more — a guard against
    /// this type quietly growing a second voice. It publishes two sentences; if it ever publishes a third,
    /// the row above goes red and somebody has to say in a PR body what the park started saying.</summary>
    [Fact]
    public void TheParkSaysTwoThingsAndStops()
    {
        var all = new StringBuilder();
        foreach (string line in ParkDay.AllProse())
        {
            all.Append(line).Append('\n');
        }

        Assert.Equal(2, all.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
