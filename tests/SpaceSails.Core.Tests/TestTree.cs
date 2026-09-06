using System;
using System.IO;

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
}
