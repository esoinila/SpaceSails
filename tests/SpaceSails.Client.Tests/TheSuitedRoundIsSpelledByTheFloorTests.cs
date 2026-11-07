using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U08-F11 (Fable ruling 2026-11-07) · Nothing in the client says the round's lines by their bare pressurised
/// spelling: every hail, stop, card, escort and pumps line goes through the *For(bool) / (plate, bool) seam, so a
/// vacuum floor is spelled in vacuum. (The behaviour itself is TheRoundIsSuitedInVacuumTests in Core.)
/// </summary>
public sealed class TheSuitedRoundIsSpelledByTheFloorTests
{
    [Fact]
    public void NoPatrolFileSpellsARoundLineWithoutAskingTheFloor()
    {
        string patrol = Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Patrol");
        var bad = Directory.EnumerateFiles(patrol, "*.cs").SelectMany(f =>
                File.ReadAllLines(f).Select((line, i) => (f, i, line)))
            .Where(x => !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .Where(x => Regex.IsMatch(x.line,
                @"PatrolBeat\.(HailLine|PumpsLine|SatisfiedLine|EscortLine|ChallengeArtUrl)\b"
                + @"|PatrolBeat\.ChallengeCard\([^,()]*(\([^()]*\))?[^,()]*\)"))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.True(bad.Count == 0, "a round line is spelled without asking the floor's air:\n" + string.Join("\n", bad));
    }
}
