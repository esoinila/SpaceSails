using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 (item 4) · THE SLOW ROSTER, Client side — every <see cref="SlowGateAttribute"/> in this
/// assembly is written down here with the number that earned it, and the tag and the list may not
/// drift apart.
///
/// <para>The reasoning, the cut and the measured distribution are all written out once, in
/// <c>SpaceSails.Core.Tests.TheSlowGateRosterTests</c>; read that first. This is its twin, and it
/// exists as a separate class for one reason: the two test assemblies cannot see each other, so a
/// guard in Core.Tests cannot reflect over a Client.Tests type. Same three laws, same anti-vacuous
/// half, a different roster.</para>
///
/// <para><b>This assembly is where the slow mass really lives.</b> Client.Tests is 1,581 tests
/// against Core.Tests' 4,178, and it holds 2,149 s of the baseline's 3,552 s — because almost
/// everything in it BOOTS the shipping page (the deck sweeps, the pop-up register, the boot
/// fingerprints, the A* walkability audits) rather than checking a rule. 42 of its classes are over
/// the cut, holding 2,033 s across 451 tests.</para>
///
/// <para><b>Adding or removing a tag.</b> Tag the class, add its row here with the seconds you
/// measured, quote the measurement in the PR; untag and delete the row in the same commit. See
/// <c>docs/testing-guide.md</c>, Appendix C.</para>
/// </summary>
public sealed class TheSlowGateRosterTests
{
    /// <summary>
    /// The documented cut, in seconds of class total. Stated so the roster's numbers can be read
    /// against something; NOT re-measured at run time — see the Core twin's docblock for why a
    /// guard that re-times its own subjects is a guard that reddens on a busy afternoon.
    /// </summary>
    private const int TheCutInSeconds = 10;

    /// <summary>
    /// #251 · THE ROSTER — class name → seconds that class cost the suite in the 2026-09-02 baseline
    /// run at e7c1915. 42 classes, 451 tests, 2,033 s of this assembly's 2,149 s.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, int> TheRoster =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            // ── The boot sweeps: every dev-start URL, booted for real ──
            { nameof(EveryDeskBootsTests), 300 },
            { nameof(EveryPopUpCanBeDismissedTests), 152 },
            { nameof(TheBootBuildsTheSameWorldTests), 148 },
            { nameof(TheBootStopsWhenYouLeaveTests), 14 },

            // ── The walkability audits: A* over every square of a floor ──
            { nameof(TheParkTakesAClickTests), 96 },
            { nameof(TheLandingPutsYouSomewhereYouCanWALKTests), 90 },
            { nameof(TheHallIsWalkableTests), 63 },
            { nameof(TheRoundIsWalkableTests), 33 },
            { nameof(TheEscortIsAWalkTests), 26 },
            { nameof(AStandingGuardIsStandingAtSomethingTests), 25 },
            { nameof(TheFarGatesLeadSomewhereTests), 20 },
            { nameof(YouCanWalkTheHiveTests), 19 },
            { nameof(ClickToWalkIsStillWalkingTests), 19 },
            { nameof(TheCirculationIsWalkableTests), 14 },
            { nameof(NothingLocksTheUiWhileAnNpcWalksTests), 12 },

            // ── The shell / host censuses: a booted page, swept member by member ──
            { nameof(TheShellOwnsTheBeatCardsTests), 80 },
            { nameof(TheClickMenusTakeTheShellAndTheContractTakesItsFootTests), 71 },
            { nameof(TheDossiersFileScrollsUnderItsHeadAndTheDeckCardsTakeTheShellTests), 55 },
            { nameof(TheShellOwnsTheDeathRowsAndTheShuttleHatchTests), 53 },
            { nameof(TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests), 50 },
            { nameof(ThePatrolKeepsItsOwnStateTests), 47 },
            { nameof(TheSeatKeepsItsOwnStateTests), 31 },
            { nameof(TheShellOwnsTheStartPickerFamilyTests), 30 },
            { nameof(TheChecklistAndTheTrayTakeTheShellAndEscapeClosesTheMenusTests), 26 },
            { nameof(ThePocketIsNotACardTests), 24 },
            { nameof(TheShipLanesAreArchivedTests), 22 },
            { nameof(ThePeekIsAModeYouCanSeeYourWayOutOfTests), 15 },
            { nameof(TheShellOwnsTheVoidAndTheWalkInTests), 15 },
            { nameof(TheHelpCardTeachesTodaysPanelTests), 11 },

            // ── The snapshot fingerprints: worlds, frames, seats and rounds, pinned ──
            { nameof(EveryFrameLeavesTheSameFingerprintTests), 36 },
            { nameof(EveryRoundFingerprintsTheSameTests), 29 },
            { nameof(EverySeatTheCaptainTakesFingerprintsTheSameTests), 25 },

            // ── The played-through flows: a scene driven beat by beat ──
            { nameof(TheGalleyIsACardNotADeskTests), 71 },
            { nameof(TheBarsTalkAboutTheInsuranceManTests), 60 },
            { nameof(TheRepCrossesTheFloorTests), 43 },
            { nameof(TheArrivalIsArmedThenNotOnlyNowTests), 42 },
            { nameof(TheNewsIsASeatVerbTests), 42 },
            { nameof(TheCastOffIsAStepTests), 39 },
            { nameof(NearestHoldsTheNeighbourhoodTests), 34 },
            { nameof(TheSalesmanWorksTheRoomTests), 23 },
            { nameof(TheTenderLeadsTheGalleyCardTests), 15 },
            { nameof(TheDirectNavActionsAreEmergenciesTests), 12 },
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
    /// #251 · A class may not carry <c>[SlowGate]</c> without a row on the roster. A tag is a test
    /// the fast run stops running, and the fast run's whole value is that a reader can see what it
    /// skips.
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
    /// #251 · The other direction: a row naming a class that is not there, or that no longer carries
    /// the mark, must be deleted — or the roster rots into a list of things that used to be slow.
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
    /// filter sees is the TRAIT the discoverer emits — not the attribute. Laws 1 and 2 would stay
    /// green forever with a discoverer emitting the wrong key while <c>--filter "speed!=slow"</c>
    /// quietly ran the whole suite. So the discoverer is asked, directly, what it emits.
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

        // And the attribute is wired to THIS discoverer, in THIS assembly. Both test assemblies
        // declare a type with this exact name; a [TraitDiscoverer] copied across naming the other
        // one would silently emit nothing at all. Read through CustomAttributeData because
        // TraitDiscovererAttribute keeps its two arguments and exposes neither.
        System.Reflection.CustomAttributeData wiring = typeof(SlowGateAttribute).CustomAttributes
            .Single(a => a.AttributeType == typeof(Xunit.Sdk.TraitDiscovererAttribute));
        Assert.Equal(typeof(SlowGateDiscoverer).FullName, wiring.ConstructorArguments[0].Value);
        Assert.Equal(typeof(SlowGateDiscoverer).Assembly.GetName().Name, wiring.ConstructorArguments[1].Value);
    }

    // ── THE GATE ITSELF ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 · The world this guard is stated against can tell pass from fail. The sweep must
    /// actually find this assembly's test classes; the roster must be non-empty; the tagged set must
    /// be a strict minority (a mark on everything would make the fast run empty and still pass both
    /// laws above); and every row's measured seconds must be at or above the documented cut, so the
    /// roster can never be padded with harmless names.
    /// </summary>
    [Fact]
    public void THE_GATE_CanTellPassFromFail()
    {
        IReadOnlyList<Type> all = EveryTestClass();
        IReadOnlyList<string> tagged = EveryTaggedClass();

        Assert.True(all.Count > 100, $"only {all.Count} test class(es) found in this assembly — the sweep is blind.");
        Assert.Contains(all, t => t.Name == nameof(EveryDeskBootsTests));
        Assert.Contains(all, t => t.Name == nameof(ConsoleCrowdingTests));

        Assert.NotEmpty(TheRoster);
        Assert.Equal(TheRoster.Count, tagged.Count);
        Assert.True(tagged.Count * 3 < all.Count,
            $"{tagged.Count} of {all.Count} classes are tagged slow — that is no longer a minority, and a " +
            "fast run that skips most of the suite is not a fast run, it is a missing one.");

        foreach ((string name, int seconds) in TheRoster)
        {
            Assert.True(seconds >= TheCutInSeconds,
                $"{name}'s row says {seconds} s, which is under the documented cut of {TheCutInSeconds} s — " +
                "either it does not belong on the roster, or the docblock's cut is out of date.");
        }
    }
}
