using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1055 · ONE LEDGER, ONE RE-PIN VERB — where the snapshot guards keep their pins, and the only sanctioned
/// way those pins are ever rewritten.
///
/// <para><b>Why this exists.</b> The guards are excellent and this changes nothing about how strict they are.
/// It changes where their numbers LIVE. In one week: #1049 and #1051 re-pinned the same four rows of
/// <see cref="EveryFrameHashesTheSameTests"/> and collided in the merge, resolved only by a full
/// re-measurement — twice; #1054 added ONE private field to <c>Map</c> and had to hand-edit <b>thirty</b>
/// fingerprint texts, one line each; and every lane before them hand-transcribed numbers out of a failure
/// report into C# source. The numbers were right every time. The TRANSCRIPTION is the thing that cannot be
/// trusted, and the scattered homes are what made two lanes contest one hunk.</para>
///
/// <para><b>The format.</b> One plain-text ledger per suite, under <c>Ledgers/</c>, and one row per
/// (probe, scene):</para>
/// <code>&lt;probe&gt; | &lt;scene&gt; | &lt;value&gt;</code>
/// <para>A <b>probe</b> is one thing measured — <c>calls</c>, <c>sha256</c>, <c>sweep</c>,
/// <c>walked-view pen</c>, <c>the accumulator</c>. A <b>scene</b> is the world it was measured in —
/// <c>ship · under way</c>, <c>TheRegolithOnFoot.AHeldKey</c>, <c>a park bench</c>. The value is the rest of
/// the line, verbatim, so a value may contain anything but a newline (probe and scene may not contain the
/// <c>" | "</c> separator, and the writer refuses if one ever does).</para>
///
/// <para><b>The ordering is the merge strategy.</b> Rows are grouped by PROBE, in the order the measurement
/// produces the probes, and within a probe by SCENE in the suite's own order. That is deliberate: a re-pin is
/// almost never "one scene moved", it is "one PROBE moved, across many scenes" — #1054 moved <c>sweep</c> on
/// all thirty, #1040 moved <c>walked-view pen</c> on fifteen. Probe-major puts each of those in its own
/// contiguous block, so two lanes moving two different probes edit two blocks a hundred lines apart and git
/// merges them without a word. Scene-major would have interleaved them one line apart and conflicted.</para>
///
/// <para><b>Nothing here ever writes in CI.</b> <see cref="Write"/> throws unless
/// <c>SPACESAILS_REPIN=1</c> is set in the environment, and
/// <see cref="ThePinsAreRewrittenOnlyWhenAskedTests"/> proves it throws — a test that RUNS on every CI build
/// and would go red the day the gate stopped gating.</para>
/// </summary>
public static class PinLedger
{
    /// <summary>The opt-in. Nothing rewrites a ledger unless this reads exactly <c>1</c>.</summary>
    public const string RePinVariable = "SPACESAILS_REPIN";

    private const string Separator = " | ";

    /// <summary>One measurement: what was measured, where, and what it read.</summary>
    public readonly record struct Row(string Probe, string Scene, string Value)
    {
        public string Key => Probe + Separator + Scene;

        public override string ToString() => Probe + Separator + Scene + Separator + Value;
    }

    // ── WHERE THE LEDGERS LIVE ────────────────────────────────────────────────────────────────────────

    public static string DirectoryPath =>
        Path.Combine(RepoRoot(), "tests", "SpaceSails.Client.Tests", "Ledgers");

    public static string PathOf(string suite) => Path.Combine(DirectoryPath, suite + ".ledger.txt");

    /// <summary>The key one measurement is pinned under — <c>&lt;probe&gt; | &lt;scene&gt;</c>.</summary>
    public static string Key(string probe, string scene) => probe + Separator + scene;

    // ── READING ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The rows of a suite's ledger, in file order. Comment lines (<c>#</c>) and blanks are the
    /// header and carry no pin.</summary>
    public static IReadOnlyList<Row> Read(string suite)
    {
        string path = PathOf(suite);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"there is no pin ledger for `{suite}` at {path} — so this suite is asserting nothing at all. "
                + $"Take the measurement: {Invocation}", path);
        }

        var rows = new List<Row>();
        int lineNumber = 0;
        foreach (string raw in File.ReadAllText(path).Replace("\r\n", "\n").Split('\n'))
        {
            lineNumber++;
            if (raw.Length == 0 || raw[0] == '#')
            {
                continue;
            }
            string[] parts = raw.Split(Separator, 3);
            if (parts.Length != 3)
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(path)} line {lineNumber} is not a ledger row "
                    + $"(`<probe> | <scene> | <value>`): {raw}");
            }
            rows.Add(new Row(parts[0], parts[1], parts[2]));
        }
        return rows;
    }

    /// <summary>The pinned rows of a suite, by <see cref="Row.Key"/>.</summary>
    public static IReadOnlyDictionary<string, Row> Pinned(string suite)
    {
        var by = new Dictionary<string, Row>(StringComparer.Ordinal);
        foreach (Row row in Read(suite))
        {
            if (!by.TryAdd(row.Key, row))
            {
                throw new InvalidDataException(
                    $"{suite}.ledger.txt pins `{row.Key}` twice — a ledger with two rows for one measurement "
                    + "cannot tell pass from fail.");
            }
        }
        return by;
    }

    // ── WRITING, AND THE GATE IN FRONT OF IT ──────────────────────────────────────────────────────────

    public static string Invocation =>
        $"{RePinVariable}=1 dotnet test tests/SpaceSails.Client.Tests -c Release "
        + "--filter FullyQualifiedName~ThePinsAreRewrittenOnlyWhenAsked "
        + "--logger \"console;verbosity=detailed\"";

    /// <summary>
    /// Rewrite a suite's ledger from a fresh measurement — <b>and refuse to, unless a human asked.</b>
    ///
    /// <para>This is the whole of requirement 3 on #1055: CI compares, only an explicit
    /// <c>SPACESAILS_REPIN=1</c> writes. The refusal is a thrown exception rather than a silent no-op,
    /// because a re-pin command that quietly did nothing would be the worst of both worlds.</para>
    /// </summary>
    public static void Write(string suite, string preamble, IEnumerable<Row> rows)
    {
        if (Environment.GetEnvironmentVariable(RePinVariable) != "1")
        {
            throw new InvalidOperationException(
                $"a pin ledger is never rewritten unless somebody asks: {RePinVariable} is not `1`, so "
                + $"`{suite}` was left exactly as it is. CI only ever COMPARES. To re-pin, run:"
                + Environment.NewLine + "  " + Invocation);
        }

        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(PathOf(suite), Render(suite, preamble, rows));
    }

    /// <summary>The file, exactly as <see cref="Write"/> would lay it down — separated out so the ledger can
    /// be rendered and compared without anything being written anywhere.</summary>
    public static string Render(string suite, string preamble, IEnumerable<Row> rows)
    {
        var sb = new StringBuilder();
        sb.Append("# SpaceSails · the ").Append(suite).Append(" pin ledger — MACHINE-WRITTEN (#1055).\n");
        sb.Append("# DO NOT HAND-EDIT. Every number below was measured, never transcribed.\n");
        sb.Append("#\n");
        sb.Append("# Rows are `<probe> | <scene> | <value>`, grouped by probe in the order the measurement\n");
        sb.Append("# produces them and, within a probe, by scene in the suite's own order. A re-pin is almost\n");
        sb.Append("# always one PROBE moving across many scenes, so probe-major keeps each re-pin in its own\n");
        sb.Append("# contiguous block and two lanes moving two different probes merge without a conflict.\n");
        sb.Append("#\n");
        sb.Append("# TO RE-PIN — run the measurement, never a text editor:\n");
        sb.Append("#   ").Append(Invocation).Append('\n');
        sb.Append("# It rewrites every ledger and PRINTS the report: rows moved, old → new, the delta per row,\n");
        sb.Append("# and for a sweep row the NAME of the field that appeared or disappeared. The report is what\n");
        sb.Append("# a reviewer reads; this file is what CI compares against.\n");
        sb.Append("#\n");
        foreach (string line in preamble.Replace("\r\n", "\n").Split('\n'))
        {
            sb.Append("# ").Append(line).Append('\n');
            }
        sb.Append("\n");

        foreach (Row row in Order(rows))
        {
            if (row.Probe.Contains(Separator, StringComparison.Ordinal)
                || row.Scene.Contains(Separator, StringComparison.Ordinal)
                || row.Value.Contains('\n', StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"a ledger row cannot carry `{Separator}` in its probe or scene, nor a newline in its "
                    + $"value — this one does: {row}");
            }
            sb.Append(row).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Probe-major, first-appearance order within each — see the class note on why that ordering is
    /// the merge strategy.</summary>
    public static IReadOnlyList<Row> Order(IEnumerable<Row> rows)
    {
        var probes = new List<string>();
        var byProbe = new Dictionary<string, List<Row>>(StringComparer.Ordinal);
        foreach (Row row in rows)
        {
            if (!byProbe.TryGetValue(row.Probe, out List<Row>? bucket))
            {
                probes.Add(row.Probe);
                byProbe[row.Probe] = bucket = [];
            }
            bucket.Add(row);
        }
        return [.. probes.SelectMany(p => byProbe[p])];
    }

    // ── THE DIFF-SHAPE REPORT ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// What the crews used to assemble by hand in a PR body: which rows moved, from what to what, and by how
    /// much. <paramref name="explain"/> lets a suite hand back the sentence only it can write — for the field
    /// sweep, the NAME of the field that appeared or disappeared.
    /// </summary>
    public static string Report(
        string suite,
        IReadOnlyDictionary<string, Row> pinned,
        IReadOnlyList<Row> fresh,
        Func<Row, Row, string?>? explain = null)
    {
        var moved = new List<string>();
        var appeared = new List<string>();
        var probes = new SortedSet<string>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Row row in fresh)
        {
            seen.Add(row.Key);
            if (!pinned.TryGetValue(row.Key, out Row was))
            {
                appeared.Add($"  + {row.Probe} | {row.Scene} | {row.Value}");
                probes.Add(row.Probe);
                continue;
            }
            if (string.Equals(was.Value, row.Value, StringComparison.Ordinal))
            {
                continue;
            }
            probes.Add(row.Probe);
            string note = explain?.Invoke(was, row) is { Length: > 0 } said ? "   ← " + said : "";
            moved.Add($"  {row.Probe} | {row.Scene}{Delta(was.Value, row.Value)}{note}"
                + $"{Environment.NewLine}      was: {was.Value}"
                + $"{Environment.NewLine}      now: {row.Value}");
        }

        var vanished = pinned.Values.Where(r => !seen.Contains(r.Key)).ToList();

        var sb = new StringBuilder();
        sb.Append($"── {suite} ─────────────────────────────────────────────").Append(Environment.NewLine);
        sb.Append($"  {fresh.Count} row(s) measured, {pinned.Count} pinned; ")
          .Append($"{moved.Count} moved, {appeared.Count} new, {vanished.Count} gone.")
          .Append(Environment.NewLine);
        if (probes.Count > 0)
        {
            sb.Append("  probes touched: ").Append(string.Join(", ", probes)).Append(Environment.NewLine);
        }
        foreach (string line in moved.Concat(appeared)
                     .Concat(vanished.Select(r => $"  − {r.Probe} | {r.Scene} | {r.Value}")))
        {
            sb.Append(line).Append(Environment.NewLine);
        }
        if (moved.Count == 0 && appeared.Count == 0 && vanished.Count == 0)
        {
            sb.Append("  nothing moved — the ledger already says what the game does.")
              .Append(Environment.NewLine);
        }
        return sb.ToString();
    }

    /// <summary>The arithmetic a crew writes by hand: <c>364 → 341</c> is <c>−23</c>. Both sides of a row
    /// usually open with a count (<c>1591</c>, <c>744 fields, sha256 …</c>, <c>210720 calls, sha256 …</c>), and
    /// when they do, the difference of those counts is the whole shape of the change.</summary>
    private static string Delta(string was, string now)
    {
        if (LeadingCount(was) is not { } a || LeadingCount(now) is not { } b || a == b)
        {
            return "";
        }
        return $"   Δ {(b > a ? "+" : "−")}{Math.Abs(b - a).ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>The count a row opens with, if it opens with one at all.
    ///
    /// <para>The digits have to END the value or be followed by a space, and that clause is not fussiness: a
    /// sha256 begins <c>58db0ba4…</c>, and without it the report cheerfully announced that a digest had
    /// "moved by −54908". A hash has no arithmetic. Only <c>364</c>, <c>744 fields, sha256 …</c> and
    /// <c>210720 calls, sha256 …</c> do.</para></summary>
    private static long? LeadingCount(string value)
    {
        int i = 0;
        while (i < value.Length && char.IsAsciiDigit(value[i]))
        {
            i++;
        }
        if (i == 0 || (i < value.Length && value[i] != ' '))
        {
            return null;
        }
        return long.TryParse(value[..i], NumberStyles.None, CultureInfo.InvariantCulture, out long n)
            ? n
            : null;
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A type, written the way a person writes it — the roster rows are read by humans looking for
    /// the field that appeared.</summary>
    public static string TypeLabel(Type t)
    {
        if (Nullable.GetUnderlyingType(t) is { } inner)
        {
            return TypeLabel(inner) + "?";
        }
        if (t.IsArray)
        {
            return TypeLabel(t.GetElementType()!) + "[]";
        }
        if (t.IsGenericType)
        {
            string bare = t.Name[..t.Name.IndexOf('`', StringComparison.Ordinal)];
            return bare + "<" + string.Join(", ", t.GetGenericArguments().Select(TypeLabel)) + ">";
        }
        return t.Name;
    }

    public static string RepoRoot()
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
}
