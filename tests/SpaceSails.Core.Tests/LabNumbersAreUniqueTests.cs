using System.Text.RegularExpressions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #664 · A LAB NUMBER IS A NAME, AND TWO THINGS CANNOT HAVE IT.
///
/// <para>The fork cost this one twice. `main` and `our-own-ship-has-compartments` ran apart for a day and
/// each numbered its next two labs 43 and 44; the reunification merge (#633) kept all four on purpose —
/// deleting a lab is paying for a measurement twice — and left a note at the top of the ladder asking people
/// to cite them by <b>directory</b> instead. That note is the tell: some forty shipping comments say "Lab 43"
/// or "Lab 44", and for a fortnight every one of them pointed at two different measurements. A citation that
/// resolves to two answers is worse than no citation, because it still reads like one.</para>
///
/// <para>So the numbers were split by chronology (`43-the-sharpest-point` and `44-knock-on-the-hull` came
/// first and kept theirs; the other two became 47 and 48), and this is the guard that stops it happening
/// again — including the way it happened, which was <b>nobody looking at the folder</b>. It reads the real
/// directory rather than the ladder in `labs/README.md`, because a README is a claim and a folder is a fact.</para>
///
/// <para>Three claims, because the number lives in three places and all three have drifted here before: the
/// directory name, the project file, and the root namespace. A lab whose folder says 47 and whose csproj says
/// <c>Lab43</c> is the same ambiguity one layer down, and it is the layer a grep for "Lab 43" finds.</para>
/// </summary>
public sealed class LabNumbersAreUniqueTests
{
    /// <summary>Walk up until the labs folder appears — cheaper than threading a path through MSBuild, and it
    /// fails with a legible message rather than an empty sweep (which would pass every claim below).</summary>
    private static string LabsDirectory()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "labs");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "README.md")))
            {
                return candidate;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find labs/ above {AppContext.BaseDirectory}");
    }

    /// <summary>Every numbered lab directory, as (number, directory name). Unnumbered folders
    /// (<c>SpaceSails.LabViz</c>) are not labs and are not swept.</summary>
    private static List<(int Number, string Dir)> NumberedLabs()
    {
        var numbered = new Regex(@"^(\d{2})-", RegexOptions.Compiled);
        List<(int, string)> found = [];

        foreach (string path in Directory.EnumerateDirectories(LabsDirectory()))
        {
            string dir = Path.GetFileName(path);
            Match m = numbered.Match(dir);
            if (m.Success)
            {
                found.Add((int.Parse(m.Groups[1].Value), dir));
            }
        }

        return found;
    }

    /// <summary>A sweep that finds nothing passes every uniqueness claim ever written. Pin the floor first —
    /// this repo has had forty-plus labs since #932 and the count only goes up.</summary>
    [Fact]
    public void TheSweepActuallySeesTheLabs()
    {
        List<(int Number, string Dir)> labs = NumberedLabs();

        Assert.True(labs.Count >= 40, $"only {labs.Count} numbered lab directories found — the sweep is looking " +
                                      "in the wrong place, and an empty sweep proves uniqueness trivially.");
        Assert.Contains(labs, l => l.Dir == "01-falling-is-orbiting");
        Assert.Contains(labs, l => l.Dir == "43-the-sharpest-point");
        Assert.Contains(labs, l => l.Dir == "44-knock-on-the-hull");
    }

    /// <summary>THE LAW. One number, one lab.</summary>
    [Fact]
    public void NoTwoLabsShareANumber()
    {
        List<string> clashes = [.. NumberedLabs()
            .GroupBy(l => l.Number)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key)
            .Select(g => $"Lab {g.Key} is claimed by {string.Join(" and ", g.Select(l => l.Dir).Order())}")];

        Assert.True(clashes.Count == 0,
            "two labs share a number, so every comment citing it points at two different measurements — " +
            "renumber the LATER one (first commit on its README decides) to the next free number and rewrite " +
            $"its citations: {string.Join("; ", clashes)}");
    }

    /// <summary>…and the number is the same one in all three places it is written down.</summary>
    [Fact]
    public void EachLabsProjectAndNamespaceCarryItsOwnNumber()
    {
        List<string> wrong = [];

        foreach ((int number, string dir) in NumberedLabs())
        {
            string full = Path.Combine(LabsDirectory(), dir);
            string[] projects = Directory.GetFiles(full, "*.csproj");
            if (projects.Length == 0)
            {
                continue;   // a lab that is prose and pictures only — several of the earliest are
            }

            string expected = $"Lab{number:00}";
            foreach (string project in projects)
            {
                if (!Path.GetFileNameWithoutExtension(project).Equals(expected, StringComparison.Ordinal))
                {
                    wrong.Add($"{dir}/{Path.GetFileName(project)} should be {expected}.csproj");
                }

                string text = File.ReadAllText(project);
                Match ns = Regex.Match(text, @"<RootNamespace>([^<]+)</RootNamespace>");
                if (ns.Success && !ns.Groups[1].Value.EndsWith(expected, StringComparison.Ordinal))
                {
                    wrong.Add($"{dir} has <RootNamespace>{ns.Groups[1].Value}</RootNamespace>, not …{expected}");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            "a lab's number disagrees with itself between its folder, its project file and its namespace — " +
            "which is where a renumbering goes half-done and a grep for the old number still finds it: " +
            $"{string.Join("; ", wrong)}");
    }
}
