using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1340 · <b>THE ARCHIVE NODE IS DEALT BY THE SEED, NOT BY THE PROCESS.</b> QA 2026-09-30:
/// <c>/map?wreck=mutiny&amp;land=1</c> drew the node in one boot and not in two others of the same build, because
/// <see cref="ArchiveNode.IsAboard"/> hashed the wreck id with <c>string.GetHashCode</c> — randomised per process
/// in .NET. The same scenario and seed must produce the same world on every machine (§9), so every answer here is
/// PINNED: computed once, written down, and compared in whatever process runs this file. An in-process "ask twice"
/// law could never see the bug (it re-asks the same process); a pinned value can.
/// </summary>
public sealed class TheArchiveNodeIsDealtBySeedTests
{
    /// <summary>
    /// <b>EVERY SCENARIO WRECK CARRIES THE SAME ANSWER IN EVERY PROCESS.</b> The ten <c>?wreck=&lt;cause&gt;</c>
    /// hulls (<see cref="Derelict.SeededWithCause"/>) and the <c>?wreck=1</c> hull (kestrel-3): whether the node is
    /// aboard, and whose it is, pinned to the values the seeded hash deals.
    ///
    /// <para><b>Proven RED</b> by restoring <c>(long)wreckId.GetHashCode(StringComparison.Ordinal)</c> in
    /// <c>ArchiveNode</c>: the pinned rows disagree with a fresh process's dealing (kestrel-3, lost-3 and the
    /// residents), differently on each run.</para>
    /// </summary>
    [Theory]
    [InlineData("DriveFailure", "lost-13", false, ArchiveNode.Resident.Stranger)]
    [InlineData("ReactorCascade", "lost-12", false, ArchiveNode.Resident.Stranger)]
    [InlineData("HullBreach", "lost-2", false, ArchiveNode.Resident.Stranger)]
    [InlineData("LifeSupportFailure", "lost-3", true, ArchiveNode.Resident.Stranger)]
    [InlineData("NavigationalError", "lost-0", false, ArchiveNode.Resident.DelinquentSubscriber)]
    [InlineData("Mutiny", "lost-1", false, ArchiveNode.Resident.DelinquentSubscriber)]
    [InlineData("Piracy", "lost-6", false, ArchiveNode.Resident.YourOwn)]
    [InlineData("Infested", "lost-7", false, ArchiveNode.Resident.Stranger)]
    [InlineData("InsuranceJob", "lost-4", false, ArchiveNode.Resident.Stranger)]
    [InlineData("VentedByOneOfTheirOwn", "lost-5", true, ArchiveNode.Resident.Stranger)]
    public void EveryScenarioWreckIsDealtTheSameNodeInEveryProcess(
        string cause, string id, bool aboard, ArchiveNode.Resident resident)
    {
        Derelict.Wreck w = Derelict.SeededWithCause(Enum.Parse<Derelict.WreckCause>(cause))!.Value;
        Assert.Equal(id, w.Id);
        Assert.Equal(aboard, ArchiveNode.IsAboard(w.Id, w.Cause));
        Assert.Equal(resident, ArchiveNode.ResidentOf(w.Id));
    }

    /// <summary>…and the <c>?wreck=1</c> hull, the one every death row and the salvage run boards.</summary>
    [Fact]
    public void TheWreckCheatsOwnHullIsDealtTheSameNodeInEveryProcess()
    {
        Derelict.Wreck k = Derelict.Seeded("kestrel-3");
        Assert.Equal(Derelict.WreckCause.Mutiny, k.Cause);
        Assert.True(ArchiveNode.IsAboard(k.Id, k.Cause));
        Assert.Equal(ArchiveNode.Resident.DelinquentSubscriber, ArchiveNode.ResidentOf(k.Id));
    }

    /// <summary>
    /// <b>THE SHARED HASH IS FNV-1a, TO THE BIT.</b> The published 64-bit FNV-1a vectors — empty string = the
    /// offset basis, "a" = <c>af63dc4c8601ec8c</c>, "foobar" = <c>85944171f73967e8</c> — so a "cleanup" that
    /// swapped the function for anything else (a per-process hash included) re-deals every wreck and fails here
    /// first, by name.
    /// </summary>
    [Fact]
    public void TheSharedHashIsFnv1aToTheBit()
    {
        Assert.Equal(0xcbf29ce484222325UL, StableHash.Of(""));
        Assert.Equal(0xaf63dc4c8601ec8cUL, StableHash.Of("a"));
        Assert.Equal(0x85944171f73967e8UL, StableHash.Of("foobar"));
        Assert.Equal(unchecked((long)0xaf63dc4c8601ec8cUL), StableHash.Id("a"));
    }

    /// <summary>
    /// <b>NO WORLD DECISION IS SEEDED ON A PER-PROCESS HASH.</b> Swept over every <c>.cs</c> and <c>.razor</c>
    /// under <c>src/</c>: no code line calls <c>.GetHashCode(</c> (comments, and the <c>override int
    /// GetHashCode()</c> declarations a value type needs for its own in-process equality, are not calls).
    /// <c>System.HashCode</c> is not swept: its two uses (<c>SurfaceDeckKey</c>, an in-process cache key; and
    /// <c>DischargePlume</c>'s filament jitter, render-only) decide nothing about the world.
    ///
    /// <para><b>Proven RED</b> by restoring any one of the seven sites #1340 moved — e.g.
    /// <c>Map.Venting.Fire.cs</c>'s <c>(long)wreck.Id.GetHashCode(StringComparison.Ordinal)</c>.</para>
    /// </summary>
    [Fact]
    public void NoWorldDecisionIsSeededOnAPerProcessHash()
    {
        string src = Path.Combine(TestTree.RepoRoot(), "src");
        var call = new Regex(@"\.GetHashCode\s*\(", RegexOptions.CultureInvariant);
        var offenders = Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".razor", StringComparison.Ordinal))
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x =>
            {
                string code = x.line;
                int comment = code.IndexOf("//", StringComparison.Ordinal);
                if (comment >= 0)
                {
                    code = code[..comment];
                }

                return call.IsMatch(code);
            })
            .Select(x => $"{Path.GetRelativePath(src, x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "a per-process hash decides something (use StableHash.Id / StableHash.Of):\n" + string.Join("\n", offenders));
    }
}
