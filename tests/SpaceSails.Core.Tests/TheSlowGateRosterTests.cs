using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #251 (item 4) · THE SLOW ROSTER — every <see cref="SlowGateAttribute"/> in this assembly is
/// written down here with the number that earned it, and the tag and the list may not drift apart.
///
/// <para><b>What this guard is for.</b> The fast run (<c>--filter "speed!=slow"</c>) is only worth
/// having if a reader can see what it does not run. A tag is invisible in a diff unless somebody
/// happens to scroll to the top of the file it landed on; a ROW is visible, because adding a tag
/// without a row turns this red and adding a row without a tag turns it red the other way. So the
/// roster is not documentation about the tags — it is the second half of the tag, and review sees
/// both halves or neither.</para>
///
/// <para><b>What this guard deliberately does NOT do.</b> It does not re-measure. A guard that
/// asserted "every tagged class really does exceed ten seconds" would be asserting a property of
/// the machine it happens to be running on — a loaded dev box, a cold CI runner, a laptop on
/// battery — and would go red for reasons that have nothing to do with the code. The numbers below
/// are a measurement, dated and quoted; they are evidence for the roster, not a live threshold.
/// The one thing that IS re-checked every run is that the tag and the roster agree, because that is
/// a property of the source and cannot drift with the weather.</para>
///
/// <para><b>The cut: ten seconds of CLASS total.</b> Measured on 2026-09-02 at e7c1915 over the
/// whole solution — 5,759 tests, 3,552 s of test time, 551 test classes:</para>
///
/// <list type="bullet">
/// <item>2,886 tests (50.1%) finish in under a millisecond and hold 0.0% of the clock.</item>
/// <item>5,467 tests (94.9%) finish under a second and hold 5.8% of it.</item>
/// <item>292 tests (5.1%) take a second or more and hold 94.2% of it.</item>
/// <item>42 tests (0.7%) take fifteen seconds or more and hold 61.9% of it.</item>
/// </list>
///
/// <para>The unit of the cut is the CLASS, not the test, because the class is the unit xUnit
/// schedules: it parallelises across test classes and serialises within one. That is not theory —
/// in the baseline run each assembly's wall clock WAS its slowest single class. Core ran 5 m 51 s
/// and <c>ZubrinTrafficTests</c> alone is 349 s; Client ran 5 m 1 s and <c>EveryDeskBootsTests</c>
/// alone is 300 s. Tagging half of a slow class would leave the other half holding the floor.</para>
///
/// <para><b>Why ten and not another number.</b> Honestly: because it is a budget, not a boundary.
/// The class-total distribution here is a continuum with no natural gap — the nearest class above
/// the line is 10.5 s and the nearest below it is 9.8 s. Ten seconds buys 93.0% of the suite's
/// measured time for 12.7% of its tests, and it puts the fast run's own floor (the slowest class it
/// still runs) at ten seconds, which is about as low as it can usefully go before the test host's
/// own start-up dominates. Raising the line to 20 s would save only 601 tests' worth of coverage
/// instead of 732 while giving back a fifth of the win; dropping it to 5 s would cost 155 more
/// tests to buy 2.2% more time. This guard does not enforce the number — see above — so a future
/// crew that wants a different line moves rows and edits this docblock, and the diff says so.</para>
///
/// <para><b>Adding or removing a tag.</b> Tag the class, add its row here with the seconds you
/// measured, and quote the measurement in the PR. Untag a class and delete its row in the same
/// commit. Never edit one half alone; that is exactly what this is here to catch.</para>
/// </summary>
public sealed class TheSlowGateRosterTests
{
    /// <summary>
    /// The documented cut, in seconds of class total. Stated here so the roster's numbers can be
    /// read against something; NOT re-measured at run time (see the class docblock).
    /// </summary>
    private const int TheCutInSeconds = 10;

    /// <summary>
    /// #251 · THE ROSTER — class name → seconds that class cost the suite in the 2026-09-02 baseline
    /// run at e7c1915. 21 classes, 281 tests, 1,271 s of the Core assembly's 1,403 s.
    ///
    /// <para><b>Every row here has a matching <c>[SlowGate]</c> on the class, and every
    /// <c>[SlowGate]</c> in this assembly has a row here.</b> The two laws below say so out loud.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, int> TheRoster =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            // ── The traffic and surface generators: deterministic sweeps over whole scenarios ──
            { nameof(ZubrinTrafficTests), 349 },
            { nameof(EncounterRuleTests), 151 },
            { nameof(OneCounterAndOnlyOneTests), 132 },
            { nameof(SurfaceStructureTests), 104 },
            { nameof(TrafficAndPredictionTests), 35 },
            { nameof(OuterReachesTests), 33 },
            { nameof(ArchiveNodeTests), 23 },
            { nameof(TheRefugesUndergroundTests), 11 },

            // ── The N-body / long-flight gates: real arcs integrated to a real arrival ──
            { nameof(Lab20LongGoodbyeTests), 49 },
            { nameof(SimulatorTests), 28 },
            { nameof(TheCyclerArrivalIsAKeptCoOrbitalTests), 28 },
            { nameof(TheParkedShipIsNotRunDownByTheMoonTests), 25 },
            { nameof(EveryLaneItLaysHashesTheSameTests), 18 },
            { nameof(TheAutopilotFliesAtATenthTests), 14 },
            { nameof(LongHaulTests), 12 },

            // ── The A* walkability audits: every square of a floor, proved reachable ──
            { nameof(TheExitIsTheFullStopTests), 39 },
            { nameof(StationWreckTests), 29 },
            { nameof(FollowMeIntoTheCabinetTests), 26 },
            { nameof(TheEmptyRoomThatHoldsOneBookTests), 12 },
            { nameof(TheFreightSideIsNobodysRoundTests), 11 },
        };

    /// <summary>
    /// Every class in this assembly that holds at least one <c>[Fact]</c> or <c>[Theory]</c> — the
    /// world this guard is stated against.
    /// </summary>
    private static IReadOnlyList<Type> EveryTestClass() =>
        typeof(TheSlowGateRosterTests).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Any(m => m.GetCustomAttributes(inherit: false)
                                    .Any(a => a is FactAttribute)))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>The classes actually wearing the mark, by name.</summary>
    private static IReadOnlyList<string> EveryTaggedClass() =>
        EveryTestClass()
            .Where(t => t.GetCustomAttribute<SlowGateAttribute>(inherit: false) is not null)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

    // ── LAW 1 · NO UNWRITTEN TAG ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 · A class may not carry <c>[SlowGate]</c> without a row on the roster. This is the law
    /// that matters: a tag is a test the fast run stops running, and the fast run's whole value is
    /// that a reader can see what it skips.
    /// </summary>
    [Fact]
    public void NoClassWearsTheMarkWithoutARowOnTheRoster()
    {
        List<string> unwritten = EveryTaggedClass()
            .Where(n => !TheRoster.ContainsKey(n))
            .Select(n => $"  {n} carries [SlowGate] but is not on the roster.")
            .ToList();

        Assert.True(unwritten.Count == 0,
            $"#251 · {unwritten.Count} class(es) are tagged slow but written down nowhere:\n" +
            string.Join("\n", unwritten) + "\n\n" +
            "Add a row to TheRoster in TheSlowGateRosterTests.cs with the seconds you MEASURED (run\n" +
            "the suite with --logger trx and read the class total; see docs/testing-guide.md,\n" +
            "Appendix C). A tag nobody wrote down is a test the fast run silently stops running.");
    }

    // ── LAW 2 · NO STALE ROW ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 · The other direction: a row that names a class which is not there, or which no longer
    /// carries the mark, must be deleted. Without this the roster rots into a list of things that
    /// used to be slow, and then it stops being read at all.
    /// </summary>
    [Fact]
    public void NoRowOnTheRosterNamesAClassThatIsNotTagged()
    {
        HashSet<string> present = EveryTestClass().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        HashSet<string> tagged = EveryTaggedClass().ToHashSet(StringComparer.Ordinal);

        List<string> stale = TheRoster.Keys
            .OrderBy(n => n, StringComparer.Ordinal)
            .Select(n => !present.Contains(n)
                ? $"  {n} is not a test class in this assembly at all (its row says {TheRoster[n]} s) — REMOVE ME."
                : !tagged.Contains(n)
                    ? $"  {n} is on the roster ({TheRoster[n]} s) but carries no [SlowGate] — REMOVE ME, or re-tag it."
                    : null)
            .OfType<string>()
            .ToList();

        Assert.True(stale.Count == 0,
            $"#251 · {stale.Count} roster row(s) are STALE:\n" +
            string.Join("\n", stale) + "\n\n" +
            "Delete the row(s) named above from TheRoster in TheSlowGateRosterTests.cs, in the SAME\n" +
            "commit that removed or renamed the tag. A roster of things that used to be slow is a\n" +
            "roster nobody reads.");
    }

    // ── LAW 3 · THE MARK REACHES THE RUNNER ───────────────────────────────────────────────────────

    /// <summary>
    /// #251 · The tag is only worth anything if <c>dotnet test --filter</c> can see it, and what the
    /// filter sees is the TRAIT the discoverer emits — not the attribute. An attribute whose
    /// discoverer emitted the wrong key would leave laws 1 and 2 perfectly green while
    /// <c>--filter "speed!=slow"</c> ran the whole suite and nobody noticed for a month. So the
    /// discoverer is asked, directly, what it emits.
    /// </summary>
    [Fact]
    public void TheMarkEmitsTheTraitTheFilterAsksFor()
    {
        List<KeyValuePair<string, string>> traits = new SlowGateDiscoverer()
            .GetTraits(traitAttribute: null!)
            .ToList();

        KeyValuePair<string, string> only = Assert.Single(traits);
        Assert.Equal("speed", only.Key);
        Assert.Equal("slow", only.Value);

        // And the attribute is wired to THIS discoverer, in THIS assembly — the two test assemblies
        // declare the same attribute name, and a [TraitDiscoverer] copied across naming the OTHER
        // assembly's type would silently emit nothing at all. Read through CustomAttributeData
        // because TraitDiscovererAttribute keeps its two arguments and exposes neither.
        System.Reflection.CustomAttributeData wiring = typeof(SlowGateAttribute).CustomAttributes
            .Single(a => a.AttributeType == typeof(Xunit.Sdk.TraitDiscovererAttribute));
        Assert.Equal(typeof(SlowGateDiscoverer).FullName, wiring.ConstructorArguments[0].Value);
        Assert.Equal(typeof(SlowGateDiscoverer).Assembly.GetName().Name, wiring.ConstructorArguments[1].Value);
    }

    // ── THE GATE ITSELF ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 · The world this guard is stated against can tell pass from fail — the fifth bug class
    /// is a guard whose world is empty or whose threshold selects everything. So: the sweep must
    /// actually find this assembly's test classes (hundreds, not zero); the roster must be
    /// non-empty; the tagged set must be a strict, small MINORITY of the classes (a mark on
    /// everything would make the fast run empty and still pass both laws above); and every row's
    /// measured seconds must be at or above the documented cut, so the roster can never be padded
    /// with harmless names.
    /// </summary>
    [Fact]
    public void THE_GATE_CanTellPassFromFail()
    {
        IReadOnlyList<Type> all = EveryTestClass();
        IReadOnlyList<string> tagged = EveryTaggedClass();

        // A sweep that found nothing would pass laws 1 and 2 forever.
        Assert.True(all.Count > 200, $"only {all.Count} test class(es) found in this assembly — the sweep is blind.");
        Assert.Contains(all, t => t.Name == nameof(ZubrinTrafficTests));
        Assert.Contains(all, t => t.Name == nameof(NoSourceFileIsTooLongTests));

        // The roster is real, and the mark is a minority: the fast run must still run most of the
        // suite, or "fast" is just "empty".
        Assert.NotEmpty(TheRoster);
        Assert.Equal(TheRoster.Count, tagged.Count);
        Assert.True(tagged.Count * 10 < all.Count,
            $"{tagged.Count} of {all.Count} classes are tagged slow — that is no longer a minority, and a " +
            "fast run that skips most of the suite is not a fast run, it is a missing one.");

        // No harmless names on the list: every row is a class the cut genuinely selected.
        foreach ((string name, int seconds) in TheRoster)
        {
            Assert.True(seconds >= TheCutInSeconds,
                $"{name}'s row says {seconds} s, which is under the documented cut of {TheCutInSeconds} s — " +
                "either it does not belong on the roster, or the docblock's cut is out of date.");
        }
    }
}
