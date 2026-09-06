using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #251 · THE STATIC-FIELD HAZARD OF A PARTIAL CLASS — no static field initializer may read a static field
/// of its own class that is DECLARED IN ANOTHER FILE.
///
/// <para><b>Why this gate exists at all.</b> #1163's crew cut <c>HavenInterior</c> along its concerns — the
/// station catalogue, the shape of one, the bar, the people in it, the wings, the weld — and that cut
/// <b>moved 33 pinned frames and reddened 14 guards</b>. Almost every coordinate in that class is measured
/// off <c>HallTopY</c>, and <c>HallTopY</c> is a <c>static readonly</c> rather than a <c>const</c> because it
/// is <c>Math.Cos</c> of a twelve-gon's apothem. <b>Static field initializers of a partial class run in the
/// order the compiler reads the FILES</b>, not the order a reader sees — so moving those declarations into
/// <c>HavenInterior.Bar.cs</c> let the SDK's glob hand that file to <c>csc</c> first, and every coordinate
/// initialised against <c>HallTopY == 0</c>. The build was clean. Nothing warned. The frame ledger caught
/// it, which is what the ledger is for.</para>
///
/// <para>#1171 met the sharpest form of the same thing in <c>CanteenRegulars</c>:
/// <c>StrangerPlates = PlatesOf(Faces)</c>, a <c>static readonly</c> whose initializer READS another
/// <c>static readonly</c>. Put those two in different files and the room fills with nameless strangers —
/// again with no warning, no error, and nothing but a pinned frame between the repository and a shipped
/// bug. That crew's own closing note is the reason this file exists: <i>"nothing warns the next person who
/// adds a computed static to a partial class."</i> Now something does.</para>
///
/// <para><b>The line this gate draws, and the one it deliberately refuses.</b> The obvious law is "every
/// static field of a multi-file partial lives in ONE file". It was written, measured against the tree, and
/// rejected — because it is <see cref="NoSourceFileIsTooLongTests"/>'s fifth bug class in gate form. On the
/// tree this gate landed on, <b>22 files declare a static field of <c>Map</c></b> (the plot inks, the combat
/// inks, the NPC inks, the tutorial tracks, the start points, the bank amounts…), and <b>2 declare one of
/// <c>HavenInterior</c></b>. The strict law would need a 24-file exception list on the day it landed, would
/// be turned off by everyone it inconvenienced, and would push the next partial's private colour constant
/// into an already-crowded file <i>for no safety at all</i> — because a colour that reads nothing cannot be
/// initialised in the wrong order. A gate whose exception list is longer than its subject has stopped
/// selecting anything.</para>
///
/// <para>What is dangerous is not the SPREAD. It is the <b>CHAIN</b>: one static field's initializer reading
/// another static field of the same class. A chain inside one file is fine and always has been — the
/// compiler runs a single file's initializers top to bottom, in the order they are written. A chain that
/// crosses a file boundary is a coin flip on the SDK's glob order. So that, precisely, is what
/// <see cref="NoStaticInitializerReadsAcrossAFileBoundary"/> forbids, and it is <b>green with zero
/// violations</b> on the tree it landed on: all twelve chains this repository has are whole, ten of them
/// inside <c>HavenInterior.cs</c>, off the twelve-gon's apothem.</para>
///
/// <para><b>The second law is what makes the first one able to fail.</b> A text-scanning guard that stops
/// recognising a field declaration goes VACUOUSLY green and nobody ever finds out — the fifth bug class
/// again, a world that can no longer tell pass from fail. <see cref="EveryChainIsWrittenDown"/> holds the
/// twelve chains this tree has BY NAME, and it goes red the moment the scanner finds a chain that is not
/// listed <i>or fails to find one that is</i>. So the scanner is proven, every run, to resolve real field
/// names out of real initializers; and a new chain cannot be added anywhere in <c>src/</c> without the
/// person adding it writing down which file holds it — which is the moment they read this docblock and
/// learn why it may not be split away from its reader.</para>
///
/// <para><b>Proven RED</b> by moving <c>HavenInterior.HallApothem</c> out of <c>HavenInterior.cs</c> into
/// <c>HavenInterior.Wings.cs</c> — one line, moved the way a concern-shaped split would move it. <b>The tree
/// built with no warning and no error</b>, and law 1 named the class, both files and both ends of
/// the chain. Quoted verbatim in the PR body; the field was put back.</para>
///
/// <para><b>And the compiler is not the backstop anybody hopes it is.</b> The same experiment run on
/// <c>CanteenRegulars.Faces</c> — a <c>Face[]</c> rather than a <c>float</c> — did stop the build, with
/// <c>CS8604: possible null reference argument</c>, because nullable analysis knows a non-nullable reference
/// type cannot be null there. That is luck, and only for reference types: <c>HallApothem</c> is a
/// <c>float</c>, its default is a perfectly legal <c>0</c>, and there is no diagnostic in the language for a
/// value that is merely WRONG. Every coordinate in the Haven, every seat count, every threshold in this
/// game is a value type.</para>
/// </summary>
public sealed class NoPartialClassSpreadsItsStaticFieldsTests
{
    // ── THE WRITTEN CHAINS ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 · Every place in <c>src/</c> where one static field initializer of a partial class reads another
    /// static field of the same class — <c>"Class.Reader reads Read"</c>, one row each.
    ///
    /// <para><b>Adding a row?</b> Then you have written a computed static into a partial class, and the two
    /// fields must be declared in the SAME file, in the order they are read. See the class docblock for what
    /// happens when they are not. <b>Deleting the field? Delete its row in the same PR</b> — a row with no
    /// chain behind it reddens this test, exactly as a stale size-gate row does.</para>
    /// </summary>
    private static readonly IReadOnlyList<string> WrittenChains =
    [
        // #1171 · the sharpest form: a static readonly whose initializer calls a helper ON another one.
        // Both live in CanteenRegulars.cs, and that class's docblock says why the five sibling partials
        // hold no static field at all.
        "SpaceSails.Core.CanteenRegulars.StrangerPlates reads Faces",

        // #1163 · the twelve-gon, and the deepest run in the tree: HallApothem is Math.Cos of the hall's
        // half-angle, so it cannot be a const, and the hall's south edge, its north edge, the customs desk,
        // the bar's far wall, the bar tops, the seven patron seats, the oracle's corner and both of the
        // Magpie's posts are all measured off it — four deep at the far end (HallApothem → HallTopY →
        // MagpieBarPost → MagpieRota). Every one of them is declared in HavenInterior.cs, in this order, and
        // THAT is why that file is 658 lines rather than about 300. This is the run whose concern-shaped
        // split moved 33 pinned frames.
        "SpaceSails.Client.Rendering.HavenInterior.HallBottomY reads HallApothem",
        "SpaceSails.Client.Rendering.HavenInterior.HallTopY reads HallApothem",
        "SpaceSails.Client.Rendering.HavenInterior.CustomsDesk reads HallBottomY",
        "SpaceSails.Client.Rendering.HavenInterior.BarTopY reads HallTopY",
        "SpaceSails.Client.Rendering.HavenInterior.BarTops reads HallTopY",
        "SpaceSails.Client.Rendering.HavenInterior.PatronSeats reads HallTopY",
        "SpaceSails.Client.Rendering.HavenInterior.OracleCorner reads HallTopY",
        "SpaceSails.Client.Rendering.HavenInterior.MagpieBarPost reads HallTopY",
        "SpaceSails.Client.Rendering.HavenInterior.MagpieRota reads MagpieBarPost",
        "SpaceSails.Client.Rendering.HavenInterior.MagpieRota reads MagpieBackPost",

        // #1046 · the vault's slot ids: the full list is the manual list plus the automatic ones. Both are
        // declared in Map.Vault.FrontDoor.cs — Map is a partial over 239 files, so this is the one chain in
        // the tree whose two ends could most easily drift apart.
        "SpaceSails.Client.Pages.Map.AllSlotIds reads ManualSlotIds",
    ];

    // ── LAW 1 · NO CHAIN CROSSES A FILE BOUNDARY ──────────────────────────────────────────────────────

    /// <summary>
    /// #251 · The hazard itself. A static field initializer of a partial class may read another static field
    /// of that class only when both are declared in the same file — anywhere else the reader is initialised
    /// against the type's default value, with a clean build and no warning.
    ///
    /// <para><b>Proven RED</b> by moving <c>CanteenRegulars.Faces</c> to <c>CanteenRegulars.Tops.cs</c>; the
    /// message is quoted in the PR body. See the class docblock for why the law is stated about the CHAIN
    /// and not about the spread.</para>
    /// </summary>
    [Fact]
    public void NoStaticInitializerReadsAcrossAFileBoundary()
    {
        List<string> crossings = EveryChain()
            .Where(c => !string.Equals(c.ReaderFile, c.ReadFile, StringComparison.Ordinal))
            .Select(c =>
                $"  {c.TypeName}.{c.Reader} is declared in {c.ReaderFile}:{c.ReaderLine}\n" +
                $"    and its initializer reads {c.Read}, which is declared in {c.ReadFile}.")
            .ToList();

        Assert.True(crossings.Count == 0,
            $"#251 · {crossings.Count} static field initializer(s) of a partial class read a static field of\n" +
            "the same class DECLARED IN ANOTHER FILE:\n" +
            string.Join("\n", crossings) + "\n\n" +
            "Static field initializers of a partial class run in the order the compiler reads the FILES, not\n" +
            "the order a reader sees, and the SDK's glob decides that order. Whichever file csc reaches first\n" +
            "runs its initializers against the OTHER file's fields still at their default — 0, or null — and\n" +
            "the build is clean, the warning list empty, and the bug invisible until a pinned frame moves.\n" +
            "PUT THE TWO DECLARATIONS BACK IN ONE FILE, in the order they are read (the reader after the\n" +
            "read), and say in that file's docblock that they may not be split. #1163 lost 33 pinned frames\n" +
            "and 14 guards to exactly this; #1171 met it again in CanteenRegulars.");
    }

    // ── LAW 2 · EVERY CHAIN IS WRITTEN DOWN, WHICH IS HOW LAW 1 CAN FAIL ───────────────────────────────

    /// <summary>
    /// #251 · The scanner reading <c>src/</c> is a text scanner, and a text scanner that quietly stops
    /// recognising a field declaration turns law 1 VACUOUSLY green — the fifth bug class, a guard whose world
    /// can no longer tell pass from fail. So the chains this tree HAS are written down by name, and this test
    /// compares the two lists in both directions.
    ///
    /// <para>Red when a chain appears that nobody wrote down — which is the point where the person who wrote
    /// a computed static into a partial class reads this class's docblock and learns that its two ends may
    /// never be split apart. Red when a written chain is no longer found — either the field is gone (delete
    /// the row in the same PR) or the scanner has rotted (fix it before trusting law 1 again).</para>
    /// </summary>
    [Fact]
    public void EveryChainIsWrittenDown()
    {
        List<string> found = EveryChain()
            .Select(c => $"{c.TypeName}.{c.Reader} reads {c.Read}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        List<string> written = WrittenChains.OrderBy(s => s, StringComparer.Ordinal).ToList();

        List<string> unwritten = found.Except(written, StringComparer.Ordinal).ToList();
        List<string> vanished = written.Except(found, StringComparer.Ordinal).ToList();

        Assert.True(unwritten.Count == 0 && vanished.Count == 0,
            "#251 · the static-initializer chains in src/ are not the ones written down in\n" +
            "NoPartialClassSpreadsItsStaticFieldsTests.WrittenChains.\n\n" +
            (unwritten.Count == 0 ? "" :
                $"NOT WRITTEN DOWN ({unwritten.Count}) — a computed static was added to a partial class:\n" +
                string.Join("\n", unwritten.Select(s => "  " + s)) + "\n" +
                "  Add each row to WrittenChains with a comment saying WHICH FILE holds both ends, and keep\n" +
                "  the two declarations there. Read this class's docblock first: this is the one kind of\n" +
                "  member a concern-shaped split of a partial class may not move.\n\n") +
            (vanished.Count == 0 ? "" :
                $"WRITTEN DOWN BUT NOT FOUND ({vanished.Count}):\n" +
                string.Join("\n", vanished.Select(s => "  " + s)) + "\n" +
                "  Either the field is gone — delete its row in the same PR, the way a stale size-gate row\n" +
                "  is deleted — or the scanner below has stopped recognising a declaration it used to read,\n" +
                "  in which case law 1 is passing on an empty world and must not be trusted until it does.\n"));
    }

    // ── THE SCANNER ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The checkout, walked for once per assembly rather than once per source file.</summary>
    private static readonly Lazy<string> Root = new(TestTree.RepoRoot);

    /// <summary>One end-to-end chain: a static field whose initializer names another static field of its own
    /// class, with the file each of them is declared in.</summary>
    private sealed record Chain(
        string TypeName, string Reader, string ReaderFile, int ReaderLine, string Read, string ReadFile);

    /// <summary>One static field declaration with an initializer, at class-member depth.</summary>
    private sealed record StaticField(string Name, string File, int Line, string Initializer);

    /// <summary>
    /// #251 · Every chain in <c>src/</c>, over classes declared <c>partial</c> in more than one file — a
    /// single-file class cannot have the hazard, because the compiler has only one order to read it in.
    ///
    /// <para>Both directions are covered by one sweep: every static field's initializer is checked against
    /// every static field of the same class, so a reader moved away from what it reads and a read moved away
    /// from its reader are the same finding.</para>
    /// </summary>
    private static IEnumerable<Chain> EveryChain()
    {
        var declaredIn = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var fieldsOf = new Dictionary<string, List<StaticField>>(StringComparer.Ordinal);

        foreach (string file in EverySourceFile())
        {
            ScanOneFile(file, declaredIn, fieldsOf);
        }

        foreach ((string typeName, List<StaticField> fields) in fieldsOf.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!declaredIn.TryGetValue(typeName, out HashSet<string>? files) || files.Count < 2)
            {
                continue;   // not a partial spread over files — no glob to get the order wrong.
            }

            var declaringFileOf = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (StaticField f in fields)
            {
                declaringFileOf[f.Name] = f.File;
            }

            string bare = typeName[(typeName.LastIndexOf('.') + 1)..];

            foreach (StaticField f in fields.OrderBy(f => f.File, StringComparer.Ordinal).ThenBy(f => f.Line))
            {
                foreach (string named in NamesRead(f.Initializer, bare).OrderBy(s => s, StringComparer.Ordinal))
                {
                    if (named != f.Name && declaringFileOf.TryGetValue(named, out string? readFile))
                    {
                        yield return new Chain(typeName, f.Name, f.File, f.Line, named, readFile);
                    }
                }
            }
        }
    }

    /// <summary>Every hand-written C# file under <c>src/</c>, repo-relative with forward slashes, in a stable
    /// order. <c>obj/</c> and <c>bin/</c> are build output and are not a thing a reader reads.</summary>
    private static IEnumerable<string> EverySourceFile()
    {
        foreach (string full in Directory
            .EnumerateFiles(Path.Combine(Root.Value, "src"), "*.cs", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(Root.Value, full).Replace('\\', '/');
            if (!rel.Contains("/obj/", StringComparison.Ordinal) &&
                !rel.Contains("/bin/", StringComparison.Ordinal))
            {
                yield return rel;
            }
        }
    }

    private static readonly Regex PartialDeclaration = new(
        @"^\s*(?:(?:public|internal|private|protected|file|sealed|abstract|static|partial|unsafe|new|readonly|ref)\s+)*" +
        @"partial\s+(?:class|struct|record\s+struct|record|interface)\s+(?<name>\w+)",
        RegexOptions.Compiled);

    private static readonly Regex FileScopedNamespace = new(@"^\s*namespace\s+(?<ns>[\w.]+)\s*;", RegexOptions.Compiled);

    private static readonly Regex ModifiedMember = new(
        @"^\s*(?<mods>(?:(?:public|internal|private|protected|static|readonly|volatile|unsafe|new|required|const)\s+)+)(?<rest>\S.*)$",
        RegexOptions.Compiled);

    private static readonly Regex Identifier = new(@"(?<![\w.])(?<id>[A-Za-z_]\w*)", RegexOptions.Compiled);

    /// <summary>
    /// #251 · Read one file into the two dictionaries: which partial types it declares, and which static
    /// fields WITH AN INITIALIZER it declares at class-member depth.
    ///
    /// <para>Depth is counted over the line with its string literals, character literals and comments
    /// removed, so a brace inside a label never opens a scope. A type's members are the lines at exactly one
    /// level inside its declaration, which keeps a nested type's statics — and a <c>static</c> local
    /// function's locals — out of the parent's list. A <c>const</c> is skipped: it is a compile-time value
    /// folded into every use, so no order can be wrong.</para>
    /// </summary>
    private static void ScanOneFile(
        string rel,
        Dictionary<string, HashSet<string>> declaredIn,
        Dictionary<string, List<StaticField>> fieldsOf)
    {
        string[] lines = File.ReadAllLines(Path.Combine(Root.Value, rel));

        string ns = string.Empty;
        int depth = 0;
        bool inBlockComment = false;
        var open = new List<(string Name, int BodyDepth, bool Entered)>();

        for (int i = 0; i < lines.Length; i++)
        {
            string code = Code(lines[i], ref inBlockComment);

            Match nsMatch = FileScopedNamespace.Match(lines[i]);
            if (nsMatch.Success)
            {
                ns = nsMatch.Groups["ns"].Value;
            }

            if (open.Count > 0 && open[^1].Entered && depth == open[^1].BodyDepth)
            {
                (string? name, string? initializer, int consumed) = ReadStaticFieldAt(lines, i, inBlockComment);
                if (name is not null && initializer is not null)
                {
                    string typeName = ns.Length == 0 ? open[^1].Name : $"{ns}.{open[^1].Name}";
                    if (!fieldsOf.TryGetValue(typeName, out List<StaticField>? list))
                    {
                        fieldsOf[typeName] = list = [];
                    }
                    list.Add(new StaticField(name, rel, i + 1, initializer));

                    // Walk the depth (and the block-comment state) over the lines the initializer spanned:
                    // line i is already stripped into `code`, the rest are stripped here.
                    depth += Delta(code);
                    for (int k = i + 1; k <= i + consumed; k++)
                    {
                        depth += Delta(Code(lines[k], ref inBlockComment));
                    }
                    i += consumed;
                    continue;
                }
            }

            Match decl = PartialDeclaration.Match(lines[i]);
            int after = depth + Delta(code);

            if (decl.Success)
            {
                string typeName = ns.Length == 0 ? decl.Groups["name"].Value : $"{ns}.{decl.Groups["name"].Value}";
                if (!declaredIn.TryGetValue(typeName, out HashSet<string>? where))
                {
                    declaredIn[typeName] = where = new HashSet<string>(StringComparer.Ordinal);
                }
                where.Add(rel);
                open.Add((decl.Groups["name"].Value, depth + 1, false));
            }

            for (int k = 0; k < open.Count; k++)
            {
                if (after >= open[k].BodyDepth)
                {
                    open[k] = (open[k].Name, open[k].BodyDepth, true);
                }
            }

            while (open.Count > 0 && open[^1].Entered && after < open[^1].BodyDepth)
            {
                open.RemoveAt(open.Count - 1);
            }

            depth = after;
        }
    }

    /// <summary>
    /// #251 · A static field declaration with an initializer, starting at <paramref name="at"/> — its name,
    /// the text of its initializer (which may run over many lines), and how many EXTRA lines it spanned.
    ///
    /// <para>What separates a field from everything else that carries the same modifiers is an <c>=</c> that
    /// is not part of a two-character operator and that stands before the first <c>(</c> on the line: a
    /// method has its parameter list first, an expression-bodied member has <c>=&gt;</c>, and a property has
    /// its accessors in braces, so the token before the <c>=</c> is not an identifier.</para>
    /// </summary>
    private static (string? Name, string? Initializer, int Consumed) ReadStaticFieldAt(
        string[] lines, int at, bool inBlockComment)
    {
        bool comment = inBlockComment;
        string code = Code(lines[at], ref comment);

        Match m = ModifiedMember.Match(code);
        if (!m.Success)
        {
            return (null, null, 0);
        }

        string[] mods = m.Groups["mods"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!mods.Contains("static", StringComparer.Ordinal) || mods.Contains("const", StringComparer.Ordinal))
        {
            return (null, null, 0);
        }

        string rest = m.Groups["rest"].Value;
        int eq = -1;
        for (int k = 0; k < rest.Length; k++)
        {
            // Step OVER a balanced group rather than stopping at it: a method's parameter list ends the
            // search harmlessly (no `=` follows it on the line, and a default argument's `=` is inside it),
            // while a tuple type's `(float X, float Y)` must not hide the `=` that comes after it — that is
            // how HavenInterior.CustomsDesk, which reads HallBottomY, went unseen the first time.
            if (rest[k] is '(' or '[')
            {
                int close = SkipGroup(rest, k);
                if (close < 0)
                {
                    break;
                }
                k = close;
                continue;
            }
            if (rest[k] == '=' &&
                (k + 1 >= rest.Length || (rest[k + 1] != '=' && rest[k + 1] != '>')) &&
                (k == 0 || "=!<>+-*/%&|^".IndexOf(rest[k - 1]) < 0))
            {
                eq = k;
                break;
            }
        }
        if (eq < 0)
        {
            return (null, null, 0);
        }

        string[] before = rest[..eq].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (before.Length < 2 || !Regex.IsMatch(before[^1], @"^\w+$"))
        {
            return (null, null, 0);   // a property's `}`, an indexer, a tuple target — not a plain field.
        }

        var initializer = new System.Text.StringBuilder(rest[(eq + 1)..]);
        int nesting = Brackets(rest[(eq + 1)..]);
        int line = at;
        while ((nesting > 0 || !rest[(eq + 1)..].Contains(';', StringComparison.Ordinal)) && line + 1 < lines.Length)
        {
            line++;
            string next = Code(lines[line], ref comment);
            initializer.Append('\n').Append(next);
            nesting += Brackets(next);
            if (nesting <= 0 && next.Contains(';', StringComparison.Ordinal))
            {
                break;
            }
            if (line - at > 500)
            {
                break;   // a runaway; the guard is better silent here than looping the suite.
            }
        }

        return (before[^1], initializer.ToString(), line - at);
    }

    /// <summary>Every identifier an initializer names, plus the ones it reaches through the class's own
    /// name (<c>CanteenRegulars.Faces</c>), so that a qualified read counts as the read it is.</summary>
    private static IEnumerable<string> NamesRead(string initializer, string bareTypeName)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match m in Identifier.Matches(initializer))
        {
            names.Add(m.Groups["id"].Value);
        }

        foreach (Match m in Regex.Matches(initializer, $@"(?<![\w.]){Regex.Escape(bareTypeName)}\.(?<id>\w+)"))
        {
            names.Add(m.Groups["id"].Value);
        }

        return names;
    }

    /// <summary>The index of the bracket that closes the group opening at <paramref name="at"/>, or -1 when
    /// it does not close on this line.</summary>
    private static int SkipGroup(string text, int at)
    {
        int nesting = 0;
        for (int k = at; k < text.Length; k++)
        {
            if (text[k] is '(' or '[')
            {
                nesting++;
            }
            else if (text[k] is ')' or ']')
            {
                nesting--;
                if (nesting == 0)
                {
                    return k;
                }
            }
        }
        return -1;
    }

    private static int Delta(string code) => code.Count(c => c == '{') - code.Count(c => c == '}');

    private static int Brackets(string code) =>
        code.Count(c => c == '{') - code.Count(c => c == '}') +
        code.Count(c => c == '[') - code.Count(c => c == ']') +
        code.Count(c => c == '(') - code.Count(c => c == ')');

    /// <summary>
    /// #251 · One line with its string literals, character literals and comments taken out, so that braces,
    /// brackets and <c>=</c> counted off it are the ones the compiler sees. Verbatim strings, interpolated
    /// strings and escapes are all handled the same crude way — the point is only that a <c>{</c> inside a
    /// label never opens a scope.
    /// </summary>
    private static string Code(string line, ref bool inBlockComment)
    {
        var kept = new System.Text.StringBuilder(line.Length);
        int i = 0;

        while (i < line.Length)
        {
            if (inBlockComment)
            {
                if (line[i] == '*' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    inBlockComment = false;
                    i += 2;
                    continue;
                }
                i++;
                continue;
            }

            char c = line[i];

            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                break;
            }
            if (c == '/' && i + 1 < line.Length && line[i + 1] == '*')
            {
                inBlockComment = true;
                i += 2;
                continue;
            }
            if (c == '"')
            {
                bool verbatim = i > 0 && (line[i - 1] == '@' || (i > 1 && line[i - 2] == '@'));
                int j = i + 1;
                while (j < line.Length)
                {
                    if (!verbatim && line[j] == '\\')
                    {
                        j += 2;
                        continue;
                    }
                    if (line[j] == '"')
                    {
                        if (verbatim && j + 1 < line.Length && line[j + 1] == '"')
                        {
                            j += 2;
                            continue;
                        }
                        break;
                    }
                    j++;
                }
                i = j + 1;
                continue;
            }
            if (c == '\'')
            {
                int j = i + 1;
                while (j < line.Length)
                {
                    if (line[j] == '\\')
                    {
                        j += 2;
                        continue;
                    }
                    if (line[j] == '\'')
                    {
                        break;
                    }
                    j++;
                }
                i = j + 1;
                continue;
            }

            kept.Append(c);
            i++;
        }

        return kept.ToString();
    }
}
