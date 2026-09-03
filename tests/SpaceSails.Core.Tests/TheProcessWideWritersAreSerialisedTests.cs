using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1108 · THE LAW BEHIND <see cref="StopRegisterCollection"/> — a test class that writes process-wide state
/// runs alone, and this is the thing that says so out loud.
///
/// <para><b>The bug this closes.</b> The registers (<see cref="StopOrder"/>, <see cref="PreservationZone"/>,
/// <see cref="Burial"/>, <see cref="PoliteDecline"/>, <see cref="QuietHands"/>) are ambients the generators
/// read: <c>MoonSurface.SurfaceDeck</c> asks whether the ground is fenced, <c>UndergroundComplex</c> asks
/// whether it is filled, stopped or declined, <c>CanteenRegulars</c> asks two of them. A guard that installs
/// one of these on a REAL body id therefore changes the world under every other class building that body at
/// that moment. That is #1108: about one run in four, with Core and Client sharing a machine,
/// <c>EveryFrameHashesTheSameTests</c> drew 651 marks where 649 were pinned and
/// <c>TheLiftHeadIsJustAnotherHutTests</c> found a lift head with a preservation fence welded to it.</para>
///
/// <para><b>Why a source read and not reflection.</b> The thing being audited is <i>who writes the register
/// at all</i>, and a write is a statement inside a method body — invisible to reflection, and visible in the
/// source exactly where a reviewer would look for it. The same argument the deck audits make: ask the
/// shipping artifact, and where the artifact is text, read the text. Both suites are read from one place
/// because the hole was in the suite that had no collection at all, and a guard that could only see its own
/// assembly would have been blind to precisely the four classes that were costing runs.</para>
///
/// <para><b>Proven able to fail.</b> With the attribute taken off
/// <c>ThePreservedSiteIsStillWalkableTests</c>, the roster test below names it and goes red.</para>
/// </summary>
public sealed class TheProcessWideWritersAreSerialisedTests
{
    /// <summary>Every write that replaces process-wide state for the whole test host. Each is spelled with
    /// its opening bracket so an <c>&lt;see cref&gt;</c> in a docblock is not mistaken for a call.</summary>
    private static readonly string[] Writes =
    [
        "StopOrder.Install(",
        "PreservationZone.Install(",
        "Burial.Install(",
        "PoliteDecline.Install(",
        "QuietHands.Install(",
        // Not a register but the same animal: a process-wide delegate seam that changes the answer
        // Aerobrake.CostOfPass gives every caller in the host for as long as it is set.
        "Aerobrake.DiceEpisodeHook =",
    ];

    /// <summary>The attribute a writer must carry, as it is actually typed.</summary>
    private const string Attribute = "[Collection(StopRegisterCollection.Name)]";

    /// <summary>This file names all six writes in string literals; it performs none of them.</summary>
    private static readonly string[] Exempt = ["TheProcessWideWritersAreSerialisedTests.cs"];

    private static string RepoRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir, "src", "SpaceSails.Core")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new InvalidOperationException("could not find the repo root from the test binary.");
        }
    }

    private static IEnumerable<string> EveryTestSource()
    {
        string tests = Path.Combine(RepoRoot, "tests");
        Assert.True(Directory.Exists(tests), $"no tests directory under {RepoRoot}.");
        return Directory.EnumerateFiles(tests, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal)
                     && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal);
    }

    /// <summary>
    /// #1108 · EVERY SUITE THAT WRITES A PROCESS-WIDE REGISTER IS IN THE SERIALISING COLLECTION — in BOTH
    /// test assemblies, because the registers are one process's and the hole was in the assembly that had
    /// no collection at all.
    /// </summary>
    [Fact]
    public void EverySuiteThatWritesAProcessWideRegisterRunsInTheSerialisingCollection()
    {
        var loose = new List<string>();
        int writers = 0;

        foreach (string path in EveryTestSource())
        {
            string name = Path.GetFileName(path);
            if (Array.Exists(Exempt, e => string.Equals(e, name, StringComparison.Ordinal)))
            {
                continue;
            }

            string source = File.ReadAllText(path);
            string[] found = [.. Writes.Where(w => source.Contains(w, StringComparison.Ordinal))];
            if (found.Length == 0)
            {
                continue;
            }

            writers++;
            if (!source.Contains(Attribute, StringComparison.Ordinal))
            {
                loose.Add($"  {name} writes {string.Join(", ", found)} without {Attribute}");
            }
        }

        // The negative is worth as much as the positive: a guard that found no writers at all would pass
        // for the wrong reason forever after somebody renamed Install.
        Assert.True(writers >= 15,
            $"only {writers} suite(s) look like register writers — the tokens have drifted from the code.");
        Assert.True(loose.Count == 0,
            $"{loose.Count} test suite(s) write process-wide state from the parallel phase, where every " +
            $"other class's world moves under it:\n{string.Join("\n", loose)}");
    }

    /// <summary>
    /// #1108 · …AND THE COLLECTION RUNS ALONE. Sharing a collection serialises the writers against each
    /// other and does nothing about the READERS, which are the rest of the suite. Only
    /// <c>DisableParallelization</c> keeps a fenced ground from appearing under somebody else's deck.
    /// </summary>
    [Fact]
    public void TheSerialisingCollectionRunsAlone()
    {
        CollectionDefinitionAttribute definition = Assert.Single(
            typeof(StopRegisterCollection).GetCustomAttributes<CollectionDefinitionAttribute>(inherit: false));

        Assert.Equal(StopRegisterCollection.Name, definition.Name);
        Assert.True(definition.DisableParallelization,
            "StopRegisterCollection must disable parallelization: serialising the writers against each " +
            "other leaves every reader of the ambient registers racing them.");
    }
}
