using System;
using System.IO;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #638 · <b>THE VOID FINALLY HAS A LANE.</b> The issue's whole finding was a wiring gap that no Core test
/// could see: <c>grep -rn "DeathCause.Void" src/SpaceSails.Client</c> → <i>no hits</i>, for two years, under
/// a shipped painting and three shipped lines. <see cref="TheLongDarkHasAScheduleTests"/> (Core) proves the
/// LAW; this file proves the WIRING, because a law nothing calls is exactly what #638 was filed about.
///
/// <para><b>Source-shape guards, and why.</b> The claims here are about where a call SITS — the watch inside
/// the whole-day loop, the cause staged on her own deck, the clock written into the vault only while it
/// runs — and none of those can be asserted by looking at a rendered page. Each reads one file and one
/// method's body, and each carries an anti-vacuous half: the file must be the file (a landmark that has
/// nothing to do with the void is found in it first), so a rename or a move reddens these instead of
/// quietly making them pass over nothing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheVoidFinallyHasALaneTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file)
    {
        string path = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file);
        Assert.True(File.Exists(path), $"{file} is not where this guard reads it ({path}).");
        string src = File.ReadAllText(path);
        Assert.True(src.Length > 400, $"{file} is suspiciously empty — this guard would be reading nothing.");
        return src;
    }

    /// <summary>
    /// The dev door stops reporting "no lane" and takes the real one. <c>?death=void</c> shipped in #636 with
    /// an honest refusal — <i>"Void has no lane at all yet"</i> — and that sentence is the issue's own
    /// evidence, so its removal is the thing worth pinning.
    /// </summary>
    [Fact]
    public void TheDevDoorTakesTheRealLaneNow()
    {
        string busted = Pages("Map.Combat.Busted.cs");
        Assert.Contains("private void StageDeathCheat(DeathCause cause)", busted, StringComparison.Ordinal);   // the right file

        Assert.DoesNotContain("Void has no lane at all yet", busted, StringComparison.Ordinal);
        Assert.Contains("case DeathCause.Void:", busted, StringComparison.Ordinal);

        // …and that arm calls the lane, rather than printing a different apology.
        int arm = busted.IndexOf("case DeathCause.Void:", StringComparison.Ordinal);
        int next = busted.IndexOf("default:", arm, StringComparison.Ordinal);
        Assert.True(next > arm, "the void arm no longer sits above the default in StageDeathCheat.");
        Assert.Contains("TheVoidTakesHer();", busted[arm..next], StringComparison.Ordinal);
    }

    /// <summary>The one place the cause is set, and the place it is set in. #636 tightened
    /// <see cref="DeathNarration.CanHappen"/> to <see cref="DeathPlace.OwnShip"/> only; a lane that staged
    /// the void anywhere else would be a client arguing with a Core law.</summary>
    [Fact]
    public void TheVoidIsStagedOnHerOwnDeckAndNowhereElse()
    {
        string lane = Pages("Map.Void.cs");
        Assert.Contains("private void TheVoidTakesHer()", lane, StringComparison.Ordinal);

        Assert.Contains("Cause = DeathCause.Void,", lane, StringComparison.Ordinal);
        Assert.Contains("Place = DeathPlace.OwnShip,", lane, StringComparison.Ordinal);
        Assert.True(DeathNarration.CanHappen(DeathCause.Void, DeathPlace.OwnShip));

        // The death machinery is shared, never duplicated: this must be the SAME BustedEncounter every other
        // death is staged through, on the generic what-happened beat.
        Assert.Contains("_busted = new BustedEncounter", lane, StringComparison.Ordinal);
        Assert.Contains("Phase = BustedEncounter.Stage.SurfaceEnd", lane, StringComparison.Ordinal);
    }

    /// <summary>The watch runs on the whole-day loop, beside the hoard's — the one place in this client where
    /// "a sim-day rolled over" is already resolved skip-proof against a warp jump.</summary>
    [Fact]
    public void TheWatchRunsOnTheWholeDayLoop()
    {
        string combat = Pages("Map.Combat.cs");
        Assert.Contains("RunCacheDiscoveryWatch();", combat, StringComparison.Ordinal);   // the right loop
        Assert.Contains("RunTheVoidWatch();", combat, StringComparison.Ordinal);
    }

    /// <summary>The countdown rides the vault, and is written ONLY while it is running — the whole of the
    /// byte-for-byte save-compat story (proved in bytes next door, in the Core suite).</summary>
    [Fact]
    public void TheCountdownRidesTheVaultAndOnlyWhenItIsRunning()
    {
        string vault = Pages("Map.Vault.cs");
        Assert.Contains("private Vault BuildVault(", vault, StringComparison.Ordinal);   // the right file

        Assert.Contains("Void = _voidDeclaredDay == VoidRule.ClockNotRunning", vault, StringComparison.Ordinal);
        Assert.Contains("? null", vault, StringComparison.Ordinal);
        Assert.Contains("vault.Void?.DeclaredDay ?? VoidRule.ClockNotRunning", vault, StringComparison.Ordinal);
        Assert.Contains("vault.Void?.LastToldDay ?? VoidRule.NothingToldYet", vault, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The ruling's one prohibition: no second reachability oracle.</b> The sweep must borrow the arrive
    /// row's own three calls — project, sweep, judge — and must not re-type a single gate of its own. So the
    /// lane names <c>ProjectAdaptive</c>, <c>ClosestApproach.Passes</c> and Core's fold, and it must NOT
    /// contain any of the threshold names <see cref="ArrivalStepRule"/> exists to own: the moment one of them
    /// appears here, a captain's life is being judged against a number typed twice.
    /// </summary>
    [Fact]
    public void TheSweepBorrowsTheArriveRowsOracleAndInventsNoSecondOne()
    {
        string lane = Pages("Map.Void.cs");
        Assert.Contains("private bool AHavenStillTakesHer()", lane, StringComparison.Ordinal);

        Assert.Contains("ProjectAdaptive(", lane, StringComparison.Ordinal);
        Assert.Contains("ClosestApproach.Passes(", lane, StringComparison.Ordinal);
        Assert.Contains("VoidRule.AHavenStillTakesHer(havens", lane, StringComparison.Ordinal);
        Assert.Contains("ArrivableAs(body", lane, StringComparison.Ordinal);   // the plan's own "where may a plan end"

        foreach (string typedTwice in new[]
                 {
                     "CaptureRange", "MaxRelativeSpeed", "EnvelopeMeters", "MatchSpeed", "DistanceLimit(", "SpeedLimit(",
                 })
        {
            Assert.DoesNotContain(typedTwice, lane, StringComparison.Ordinal);
        }
    }

    /// <summary>All three tellings come out of Core, verbatim, so the banner, the log and the card cannot
    /// drift from the words the ruling authored — and every one of them is actually reached from the lane.</summary>
    [Fact]
    public void TheThreeTellingsAreSpokenFromCoresOwnWords()
    {
        string lane = Pages("Map.Void.cs");
        Assert.Contains("VoidRule.DeclaredLine", lane, StringComparison.Ordinal);
        Assert.Contains("VoidRule.HalfwayLine", lane, StringComparison.Ordinal);
        Assert.Contains("VoidRule.OneDayLeftLine", lane, StringComparison.Ordinal);

        // The banner, the log and the card: one surface each, in that order of escalation.
        Assert.Contains("_shipAlerts.Raise(AlertKind.Void", lane, StringComparison.Ordinal);
        Assert.Contains("LogAutopilotEvent(", lane, StringComparison.Ordinal);
        Assert.Contains("_viewObject = new DeckPlan.ConsoleSpot(", lane, StringComparison.Ordinal);

        // …and none of the three is typed as a literal here, which is how a sentence starts disagreeing with
        // the sim that raised it.
        Assert.DoesNotContain("Nothing answers the helm", lane, StringComparison.Ordinal);
        Assert.DoesNotContain("the line goes nowhere", lane, StringComparison.Ordinal);
        Assert.DoesNotContain("One day remains on it", lane, StringComparison.Ordinal);
    }
}
