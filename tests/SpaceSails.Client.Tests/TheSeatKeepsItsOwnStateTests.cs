using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6a · THE SEAT KEEPS ITS OWN STATE.
///
/// <para>The seat family is seven partials of <see cref="Pages.Map"/> — <c>Map.Seated.cs</c>,
/// <c>Map.Stool.cs</c>, <c>Map.Table.cs</c>, <c>Map.Bench.cs</c>, <c>Map.OfficeChair.cs</c>,
/// <c>Map.Cubicle.cs</c>, <c>Map.SitStandDesk.cs</c> — and between them they hold FIVE fields: the open
/// table, the stool under the captain, the stand-up confirm, the sit beat still owed, and which cubicle has
/// its catch over. Because they are partials of one big page, every other partial in the client could reach
/// straight past the family's own vocabulary and read those fields raw, and four issues in four days
/// (#847, #865, #868, #881) all landed in that family because of it.</para>
///
/// <h3>What this guard actually asserts</h3>
///
/// <para>That the five names appear NOWHERE in <c>src/SpaceSails.Client</c> outside those seven files — not
/// in a <c>.cs</c> partial, not in the <c>.razor</c> markup, and not in a comment either, because a comment
/// that has to name another family's private field is the same coupling wearing a different hat. Everyone
/// outside asks the family a QUESTION instead: <c>CaptainIsSeated</c>, <c>CaptainIsOnAStool</c>,
/// <c>SeatedTable</c>, <c>SeatedStoolPlate</c>, <c>TheStandUpConfirmIsUp</c> — or tells it to do something:
/// <c>OweTheSitBeat</c>, <c>LeaveTheStoolBehind</c>. Add a member to the family rather than a raw read out
/// here; that is the whole point, and it is what makes #870's 6b (the five fields become ONE object) a
/// change to seven files and not to the whole client.</para>
///
/// <h3>Why the second fact is here</h3>
///
/// <para>A guard that only looks for absence passes gloriously on a tree where the fields have simply been
/// renamed — the world could no longer tell pass from fail, which is this repo's fifth named bug class. So
/// the anti-vacuous half asserts the five names are still IN the family, and that all seven family files
/// still exist at the paths this guard exempts. Both halves have to be satisfiable at once for the first
/// fact to mean anything.</para>
///
/// <para>It was proven RED before it was trusted: on the tree this lane started from it named eleven lines
/// across five files — <c>Map.Bin.cs</c>, <c>Map.Quests.Bar.cs</c>, <c>Map.Sim.Cancel.cs</c>,
/// <c>Map.Surface.Frame.cs</c> and <c>Map.razor</c>.</para>
/// </summary>
public sealed class TheSeatKeepsItsOwnStateTests
{
    /// <summary>The five. Raw field names, exactly as the family declares them.</summary>
    private static readonly string[] TheFiveFields =
    [
        "_table",
        "_stool",
        "_standUpAsk",
        "_sitBeatOwedSeconds",
        "_cubicleFloorKey",
    ];

    /// <summary>The seven partials that ARE the seat family, relative to <c>src/SpaceSails.Client</c>. A
    /// file added to this list must exist (asserted below), so the exemption can never be a typo that
    /// quietly excuses nothing — or, worse, excuses everything.</summary>
    private static readonly string[] TheFamily =
    [
        "Pages/Map.Seated.cs",
        "Pages/Map.Stool.cs",
        "Pages/Map.Table.cs",
        "Pages/Map.Bench.cs",
        "Pages/Map.OfficeChair.cs",
        "Pages/Map.Cubicle.cs",
        "Pages/Map.SitStandDesk.cs",
    ];

    /// <summary>Word-anchored, so <c>_stoolCheat</c> and <c>_tableCloth</c> are not the field. In .NET an
    /// underscore is a word character, so <c>\b_stool\b</c> is exactly "this name and no longer one".</summary>
    private static Regex Needle(string field) =>
        new(@"\b" + Regex.Escape(field) + @"\b", RegexOptions.CultureInvariant);

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NobodyOutsideTheSeatFamilyNamesARawSeatField()
    {
        var trespass = new List<string>();

        foreach (string path in ClientSources())
        {
            if (IsFamily(path))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (string field in TheFiveFields)
                {
                    if (Needle(field).IsMatch(lines[i]))
                    {
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} names {field} — {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(
            trespass.Count == 0,
            "#870 lane 6a · THE SEAT'S STATE IS THE SEAT'S. These five fields belong to the seven seat " +
            "partials and are read out here raw:\n" +
            string.Join("\n", trespass) +
            "\n\nAsk the family the question you actually mean instead — CaptainIsSeated, " +
            "CaptainIsOnAStool, SeatedTable, SeatedStoolPlate, SeatedIn, SeatedIsDocked, SeatedAlone, " +
            "TheStandUpConfirmIsUp — or tell it to do the thing: OweTheSitBeat, LeaveTheStoolBehind. If " +
            "none of them says what you need, ADD one small named member to Map.Seated.cs and say in its " +
            "docblock which site asked for it. Do not widen this guard's exemption list: it is the seat " +
            "family, and it is seven files.");
    }

    // ── AND IT IS NOT VACUOUS ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheSevenFamilyFilesAreAllReallyThere()
    {
        var missing = TheFamily
            .Where(rel => !File.Exists(Path.Combine(ClientRoot, rel.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();

        Assert.True(
            missing.Count == 0,
            "#870 lane 6a · this guard exempts seven files by path, and these do not exist:\n  " +
            string.Join("\n  ", missing) +
            "\n\nA path that matches nothing exempts nothing and proves nothing. If the seat family was " +
            "split or renamed, re-PATH this list — never delete a row to make the sweep quiet.");
    }

    [Fact]
    public void TheFiveNamesAreStillInsideTheFamily()
    {
        string family = string.Concat(
            TheFamily.Select(rel =>
                File.ReadAllText(Path.Combine(ClientRoot, rel.Replace('/', Path.DirectorySeparatorChar)))));

        var vanished = TheFiveFields.Where(f => !Needle(f).IsMatch(family)).ToList();

        Assert.True(
            vanished.Count == 0,
            "#870 lane 6a · ANTI-VACUOUS HALF. These names are no longer anywhere in the seat family:\n  " +
            string.Join("\n  ", vanished) +
            "\n\nWhich means the sweep above is passing because there is nothing left to find, not because " +
            "the family kept its state to itself. If a field was renamed, rename it here in the same " +
            "commit; if #870's 6b has moved the five onto a Seating object, this whole file is superseded " +
            "by that lane's own guard and should be rewritten, not deleted.");
    }

    // ── WHERE THE SOURCE IS ───────────────────────────────────────────────────────────────────────────

    /// <summary>Every shipped client source: the C# partials and the markup. Anything the compiler reads,
    /// this guard reads — <c>obj/</c> and <c>bin/</c> excluded, because a generated copy of a file is not a
    /// second author naming the field.</summary>
    private static IEnumerable<string> ClientSources() =>
        Directory.EnumerateFiles(ClientRoot, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(p => !Segments(p).Any(s =>
                s.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || s.Equals("bin", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p, StringComparer.Ordinal);

    private static string[] Segments(string path) =>
        Relative(path).Split('/');

    private static bool IsFamily(string path) =>
        TheFamily.Contains(Relative(path), StringComparer.Ordinal);

    private static string Relative(string path) =>
        Path.GetRelativePath(ClientRoot, path).Replace('\\', '/');

    private static string ClientRoot { get; } =
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary.");
    }
}
