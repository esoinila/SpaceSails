using System;
using System.IO;
using System.Threading;
using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #251 · WHERE THE REPOSITORY IS, SAID ONCE — the Core suite's twin of
/// <c>SpaceSails.Client.Tests.TestTree</c>.
///
/// <para>The two test assemblies cannot see each other — Core.Tests references Core, Client.Tests references
/// the Client, and neither references the other — so the walk is declared in both, exactly as
/// <see cref="SlowGateAttribute"/> is. What travels between them is the RULE and not the type: walk up from
/// <see cref="AppContext.BaseDirectory"/> until the source tree is under foot, and throw rather than guess.
/// This one is anchored on <c>src/SpaceSails.Core</c>, which was the anchor all six of the byte-identical
/// copies it replaces used.</para>
///
/// <para>See the Client twin's docblock for the measurement behind this and for what was deliberately NOT
/// folded in.</para>
/// </summary>
internal static class TestTree
{
    /// <summary>The repository root, found by walking up from the test assembly until
    /// <c>src/SpaceSails.Core</c> is under foot.</summary>
    /// <exception cref="DirectoryNotFoundException">There is no checkout above the test binary.</exception>
    internal static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// #251 · THE SHIPPING SKY, PARSED ONCE PER ASSEMBLY — the Core twin of
    /// <c>SpaceSails.Client.Tests.TestTree.Sol</c>, whose docblock carries the immutability argument that
    /// makes ONE shared instance safe across classes xUnit runs in parallel.
    ///
    /// <para>This suite's habit was worse than the Client's, because its shared loader was a METHOD rather
    /// than a <c>Lazy</c>: <c>SimulatorTests.LoadSol()</c> was called from seventy-one places across
    /// forty-two files, several of them per-test helpers called once per <c>[Fact]</c>, and every single
    /// call read <c>scenarios/sol.json</c> off the disk and parsed it again. Six more classes held their own
    /// loader beside it, two of them as <c>=&gt;</c> properties that re-parsed on every access.</para>
    ///
    /// <para>The path is <see cref="AppContext.BaseDirectory"/>, not <see cref="RepoRoot"/>: this project's
    /// <c>.csproj</c> copies <c>scenarios/*.json</c> beside the test binary on every build, and that copy is
    /// what all of those loaders read. Reading the same file by the same path is what keeps this a move.</para>
    /// </summary>
    private static readonly Lazy<ScenarioDefinition> TheShippingSky = new(
        () => ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json")),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary><c>scenarios/sol.json</c>, the sky the game ships, parsed once for the whole assembly.</summary>
    internal static ScenarioDefinition Sol => TheShippingSky.Value;
}
