using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6b · THE SEAT KEEPS ITS OWN STATE — and now it IS one.
///
/// <para>6a made the rest of the client stop reading the seat's five fields raw: eleven call sites across
/// five files started asking the family a named question instead, and this guard's first shape held the
/// line at <i>"nobody outside these seven partials names <c>_table</c>"</i>.</para>
///
/// <para>6b is the lane that shape was protecting. The five fields — the open table, the stool under the
/// captain, the stand-up confirm, the sit beat still owed, and which floor the cubicle cache was read off —
/// are now five properties on <c>Map.Seating</c>, declared in one file, and the page carries ONE field where
/// it carried five. So the guard ratchets with it: the raw names are not "outside the family" any more,
/// they are <b>gone</b>, and the state is declared in exactly one place.</para>
///
/// <h3>The facts, and why each is here</h3>
///
/// <list type="number">
/// <item><b>The five raw names appear nowhere in the shipped client</b> — not in a <c>.cs</c> partial, not in
/// the <c>.razor</c> markup, not in a comment. Strictly stronger than 6a's sweep, which exempted seven
/// files.</item>
/// <item><b>The state is declared in exactly one file.</b> Each of the five property declarations appears
/// once across the whole client, all five in <c>Pages/Seating/Seating.cs</c>, and so does the one
/// <c>_seating</c> field. A second declaration anywhere is a second answer to "where am I sitting", which is
/// this repo's first named bug class.</item>
/// <item><b>Nothing re-seats the page.</b> <c>_seating</c> is written nowhere else: standing up EMPTIES the
/// seat, it does not swap in a different one.</item>
/// <item><b>The anti-vacuous half, and it is asked of the RUNNING TYPE rather than of the text.</b> A sweep
/// for absence passes gloriously on a tree where the state was simply deleted — the world could no longer
/// tell pass from fail, this repo's fifth named bug class. So the last fact reflects over a real
/// <see cref="Pages.Map"/>: it really has a <c>_seating</c>, that object really carries the five with the
/// types they always had, and all fifteen moved reads answer on BOTH the seat object and the page. That last
/// clause is the forwarding proof — it is what says no caller outside this family had to change.</item>
/// </list>
///
/// <para>Proven RED before it was trusted, by leaving one raw field behind on <c>Map.Cubicle.cs</c>: facts 1
/// and 2 both named it, by file, line and text. The verbatim output is in #870 lane 6b's PR body.</para>
///
/// <para>#870's 6c moves the VERBS onto the same object behind an explicit host surface, and deletes the
/// forwarder block the fourth fact currently insists on. When that lands, the Map half of the fifteen is the
/// clause to relax — deliberately, in that lane, and never by deleting a row to make a sweep quiet.</para>
/// </summary>
public sealed class TheSeatKeepsItsOwnStateTests
{
    /// <summary>The one file the seat's state is allowed to be declared in, relative to
    /// <c>src/SpaceSails.Client</c>.</summary>
    private const string TheOneFile = "Pages/Seating/Seating.cs";

    /// <summary>The one field the page keeps, exactly as it is declared.</summary>
    private const string TheOneField = "private readonly Seating _seating = new();";

    /// <summary>The five, before and after. Raw field name as the family used to declare it, then the
    /// property that replaced it, its declaration, and the type it still has.</summary>
    private static readonly (string Raw, string Property, string Declaration, string Type)[] TheFive =
    [
        ("_table", "Table", "public TableTalk? Table { get; set; }", "TableTalk"),
        ("_stool", "Stool", "public StoolSeat? Stool { get; set; }", "StoolSeat"),
        ("_standUpAsk", "StandUpAsk", "public bool StandUpAsk { get; set; }", "Boolean"),
        ("_sitBeatOwedSeconds", "SitBeatOwedSeconds", "public double SitBeatOwedSeconds { get; set; }", "Double"),
        ("_cubicleFloorKey", "CubicleFloorKey", "public string? CubicleFloorKey { get; set; }", "String"),
    ];

    /// <summary>The fifteen reads that moved. Every one of them is a pure function of the five and nothing
    /// else — no excursion, no avatar, no clock — and every one was already being asked for BY NAME from
    /// outside the seat family, which is why <see cref="Pages.Map"/> still answers all fifteen.</summary>
    private static readonly string[] TheFifteenMovedReads =
    [
        "CaptainIsSeated",
        "SeatedTable",
        "CaptainIsRestingAtATable",
        "SeatedOnABenchInTheOpen",
        "SeatedIsDocked",
        "SeatedIsAConversation",
        "SeatedWithCompany",
        "SeatedCompanyLine",
        "SeatedOverheardLine",
        "CaptainIsOnAStool",
        "SeatedStoolPlate",
        "TableMovesOnTheTable",
        "StoolMovesOnTheTable",
        "TheStandUpConfirmIsUp",
        "TheSitBeatIsSettling",
    ];

    /// <summary>Word-anchored, so <c>_stoolCheat</c> and <c>_tableCloth</c> are not the field. In .NET an
    /// underscore is a word character, so <c>\b_stool\b</c> is exactly "this name and no longer one".</summary>
    private static Regex Needle(string name) =>
        new(@"\b" + Regex.Escape(name) + @"\b", RegexOptions.CultureInvariant);

    // ── (1) THE RAW NAMES ARE GONE ────────────────────────────────────────────────────────────────────

    [Fact]
    public void NoRawSeatFieldIsNamedAnywhereInTheClient()
    {
        var trespass = new List<string>();

        foreach (string path in ClientSources())
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach ((string raw, string property, _, _) in TheFive)
                {
                    if (Needle(raw).IsMatch(lines[i]))
                    {
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} still names {raw} (now _seating.{property}) — " +
                            lines[i].Trim());
                    }
                }
            }
        }

        Assert.True(
            trespass.Count == 0,
            "#870 lane 6b · THE SEAT'S STATE IS ONE OBJECT. These are the five loose fields the seat used " +
            "to keep on the page; they no longer exist, because the state is five properties on Map.Seating " +
            $"in {TheOneFile}. Still named raw here:\n" +
            string.Join("\n", trespass) +
            "\n\nInside the seat family, say _seating.Table / _seating.Stool / _seating.StandUpAsk / " +
            "_seating.SitBeatOwedSeconds / _seating.CubicleFloorKey. Outside it, ask the question you " +
            "actually mean — CaptainIsSeated, CaptainIsOnAStool, SeatedTable, SeatedStoolPlate, SeatedIn, " +
            "SeatedIsDocked, SeatedAlone, TheStandUpConfirmIsUp — or tell it to do the thing: OweTheSitBeat, " +
            "LeaveTheStoolBehind. If none of them says what you need, ADD one small named member to Seating " +
            "and say in its docblock which site asked for it. Do not re-declare a field out here.");
    }

    // ── (2) AND THE STATE IS DECLARED IN EXACTLY ONE PLACE ────────────────────────────────────────────

    [Fact]
    public void TheFiveAreDeclaredInExactlyOneFileAndItIsSeating()
    {
        var wrong = new List<string>();
        var sources = ClientSources().ToList();

        foreach (string declaration in TheFive.Select(f => f.Declaration).Append(TheOneField))
        {
            List<string> found = sources
                .Where(p => File.ReadAllText(p).Contains(declaration, StringComparison.Ordinal))
                .Select(Relative)
                .ToList();

            if (found.Count != 1 || found[0] != TheOneFile)
            {
                wrong.Add(
                    $"  `{declaration}` is declared in {found.Count} file(s): " +
                    (found.Count == 0 ? "NONE" : string.Join(", ", found)));
            }
        }

        Assert.True(
            wrong.Count == 0,
            $"#870 lane 6b · THE SEAT IS DECLARED ONCE, AND IT IS DECLARED IN {TheOneFile}:\n" +
            string.Join("\n", wrong) +
            "\n\nZero files means the declaration was reworded and this guard is now reading a dead string — " +
            "fix the row, never delete it. Two files means there are two seats, and a captain sitting on one " +
            "of them is standing up according to the other.");
    }

    [Fact]
    public void NothingEverRESeatsThePage()
    {
        var written = new List<string>();

        foreach (string path in ClientSources())
        {
            if (Relative(path) == TheOneFile)
            {
                continue;   // its own declaration, which is the one assignment allowed to exist
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"\b_seating\s*=(?!=)"))
                {
                    written.Add($"  {Relative(path)}:{i + 1} — {lines[i].Trim()}");
                }
            }
        }

        Assert.True(
            written.Count == 0,
            "#870 lane 6b · NOTHING RE-SEATS THE PAGE. `_seating` is readonly and assigned once, at its " +
            "declaration. Written again here:\n" + string.Join("\n", written) +
            "\n\nStanding up EMPTIES the seat (`_seating.Table = null`); it does not swap in a different " +
            "one. A second Seating is a second answer to \"where am I sitting\".");
    }

    // ── (3) AND IT IS NOT VACUOUS — ASKED OF THE RUNNING TYPE ─────────────────────────────────────────

    [Fact]
    public void TheSeatObjectReallyCarriesTheFiveAndAnswersAllFifteen()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags Any = Hidden | BindingFlags.Public;

        FieldInfo? seating = typeof(Pages.Map).GetField("_seating", Hidden);
        Assert.True(
            seating is not null,
            "#870 lane 6b · ANTI-VACUOUS HALF. Map has no `_seating` field at all, which means the sweeps " +
            "above are passing because there is nothing left to find rather than because the seat kept its " +
            "state to itself. That is this repo's fifth named bug class. If the field was renamed, rename " +
            "it here in the same commit.");

        Type seat = seating!.FieldType;
        var missing = new List<string>();

        foreach ((string raw, string property, _, string type) in TheFive)
        {
            PropertyInfo? on = seat.GetProperty(property, Any);
            if (on is null)
            {
                missing.Add($"  {property} (was {raw}) is not on {seat.Name} at all");
            }
            else if (!on.PropertyType.Name.Contains(type, StringComparison.Ordinal))
            {
                missing.Add(
                    $"  {property} (was {raw}) is a {on.PropertyType.Name} and used to be a {type} — " +
                    "this lane was supposed to keep every type exactly as it was");
            }
        }

        // …and the fifteen answer on BOTH. On the seat object, because that is where they moved; and on the
        // page, because 6b changed no caller — every one of these names is still asked for by the markup,
        // the cancel chain, the surface tick or the deck's own state hand-off, spelled as it always was.
        foreach (string read in TheFifteenMovedReads)
        {
            if (seat.GetProperty(read, Any) is null && seat.GetMethod(read, Any) is null)
            {
                missing.Add($"  {read} does not answer on {seat.Name} — it was supposed to move there");
            }

            if (typeof(Pages.Map).GetProperty(read, Any) is null
                && typeof(Pages.Map).GetMethod(read, Any) is null)
            {
                missing.Add(
                    $"  {read} no longer answers on Map — 6b promised every existing caller keeps its " +
                    "spelling, so this one's forwarder is missing (see Map.Seated.cs's 6b block)");
            }
        }

        Assert.True(
            missing.Count == 0,
            "#870 lane 6b · ANTI-VACUOUS HALF, asked of the running type rather than of the text:\n" +
            string.Join("\n", missing) +
            "\n\nWhen 6c moves the VERBS and deletes the forwarder block, the Map half of the fifteen is " +
            "what relaxes — deliberately, in that lane, with the callers updated in the same commit.");
    }

    // ── WHERE THE SOURCE IS ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheOneFileIsReallyThere()
    {
        string path = Path.Combine(ClientRoot, TheOneFile.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(
            File.Exists(path),
            $"#870 lane 6b · this guard names one file by path and it does not exist: {TheOneFile}. A path " +
            "that matches nothing exempts nothing and proves nothing. If the seat's state moved again, " +
            "re-PATH this constant — never delete the row to make the sweep quiet.");
    }

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
