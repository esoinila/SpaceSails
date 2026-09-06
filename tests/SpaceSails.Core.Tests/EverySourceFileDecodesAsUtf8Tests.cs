using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Buffers;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #251 · EVERY FILE A PERSON WRITES IN THIS REPOSITORY IS UTF-8, AND THIS IS WHAT SAYS SO.
///
/// <para><b>Why it exists.</b> #1163 found two source files that git would not call text — a literal NUL
/// byte inside a string literal, which makes git print <i>"Binary file … matches"</i> for a grep and
/// <i>"Bin 28862 -&gt; 28302 bytes"</i> for a diff, so neither file could be code-reviewed at all. It fixed
/// those two and pinned the tree LF with a root <c>.gitattributes</c>. What neither the fix nor the
/// attributes file can see is the OTHER half of "this is text": <b>which bytes</b>. A file can be perfectly
/// LF, perfectly non-binary to git, and still not be UTF-8.</para>
///
/// <para>One was. <c>tests/SpaceSails.Client.Tests/TheHudSaysWhereTheAirComesFromTests.cs</c> carried a
/// lone <c>0xB7</c> at byte 1,589 — a Latin-1 middle dot that had lost its <c>0xC2</c> lead — inside a
/// comment, two bytes in front of a correctly-encoded ellipsis. It is the only file in the tree that a
/// UTF-8 reader could not decode, and it had been there for months, because <b>nothing in the toolchain
/// says so</b>: Roslyn assumes UTF-8 and substitutes U+FFFD rather than failing, git's heuristic only
/// looks for NULs, and a lone high byte in a comment changes no behaviour anybody would notice. It was
/// found by a crew whose script happened to read the tree with a strict decoder.</para>
///
/// <para><b>What it costs when it is not caught.</b> The same thing a wrong line ending costs, and #1160
/// paid an hour for that one: a guard that reads source AS TEXT gets a different text than the author
/// wrote. A `·` that arrives as U+FFFD will not match a `·` in a needle, and the message says the needle
/// is missing, not that the file is mis-encoded. Every source-shape guard in this repository — and there
/// are many — is stated over bytes somebody decoded.</para>
///
/// <para><b>The reading is the strictest one available.</b> <see cref="Utf8.ToUtf16"/> with
/// <c>replaceInvalidSequences: false</c> returns <see cref="OperationStatus.InvalidData"/> and, in its
/// <c>bytesRead</c>, the exact offset of the first byte it could not read — which is the whole message,
/// because "this file is not UTF-8" without an offset is a hunt. A UTF-8 BOM is fine and stays fine: those
/// three bytes decode to U+FEFF like any other character.</para>
///
/// <para><b>Proven RED</b> by putting the byte back — the single <c>0xB7</c> in
/// <c>TheHudSaysWhereTheAirComesFromTests.cs</c> — and running this:</para>
/// <code>
/// #251 · 1 file(s) under src/, tests/ or docs/ are not valid UTF-8:
///   tests/SpaceSails.Client.Tests/TheHudSaysWhereTheAirComesFromTests.cs — first bad byte 0xB7 at
///   offset 1589, in: "riton", "the-clinker",¶¶        // #677 · â¦and the one roc"
/// </code>
/// <para>and restored. The offset and the quoted context are what make the red actionable; the fix is
/// almost always one character that arrived from somewhere Latin-1.</para>
/// </summary>
public sealed class EverySourceFileDecodesAsUtf8Tests
{
    /// <summary>The kinds a person writes by hand and a guard, a compiler or a reader later reads as text.
    /// Deliberately NOT everything: <c>.png</c>, <c>.jpg</c> and <c>.pdf</c> are the artefacts art and the
    /// paper arrive in, and holding a binary to a text law would only teach people to turn the law off.
    /// </summary>
    private static readonly string[] TheKindsWeWrite = [".cs", ".razor", ".razor.css", ".md", ".json"];

    /// <summary>Where a person writes them. <c>obj/</c> and <c>bin/</c> are build output — generated code
    /// is not a thing a reader reads, and it is not a thing anybody can fix.</summary>
    private static readonly string[] TheTreesWeWrite = ["src", "tests", "docs"];

    private static IEnumerable<string> EveryHandWrittenFile()
    {
        string root = TestTree.RepoRoot();
        foreach (string tree in TheTreesWeWrite)
        {
            foreach (string full in Directory
                .EnumerateFiles(Path.Combine(root, tree), "*", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal))
            {
                string rel = Path.GetRelativePath(root, full).Replace('\\', '/');
                if (rel.Contains("/obj/", StringComparison.Ordinal) ||
                    rel.Contains("/bin/", StringComparison.Ordinal) ||
                    rel.Contains("/node_modules/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (TheKindsWeWrite.Any(k => rel.EndsWith(k, StringComparison.Ordinal)))
                {
                    yield return rel;
                }
            }
        }
    }

    /// <summary>The first byte of this file that is not valid UTF-8, or <c>-1</c> if the whole file is.
    /// </summary>
    private static int FirstBadByte(byte[] bytes)
    {
        char[] into = new char[bytes.Length + 1];
        OperationStatus status = Utf8.ToUtf16(
            bytes, into, out int bytesRead, out _, replaceInvalidSequences: false, isFinalBlock: true);
        return status == OperationStatus.Done ? -1 : bytesRead;
    }

    /// <summary>The bytes around an offender, rendered so a person can see WHERE in the file it is without
    /// opening it — the bad byte's own neighbours, decoded loosely because by definition they cannot be
    /// decoded strictly.</summary>
    private static string Around(byte[] bytes, int at)
    {
        int from = Math.Max(0, at - 40);
        int to = Math.Min(bytes.Length, at + 20);
        string loose = Encoding.Latin1.GetString(bytes, from, to - from);
        return loose.Replace("\r", "¶").Replace("\n", "¶");
    }

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 · Every hand-written file in the trees a person edits decodes as strict UTF-8. One byte that
    /// does not is one guard away from reading a text its author never wrote.
    /// </summary>
    [Fact]
    public void NoHandWrittenFileIsMisEncoded()
    {
        List<string> offenders = [];
        foreach (string rel in EveryHandWrittenFile())
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(TestTree.RepoRoot(), rel));
            int at = FirstBadByte(bytes);
            if (at >= 0)
            {
                offenders.Add(
                    $"  {rel} — first bad byte 0x{bytes[at]:X2} at offset {at}, in: \"{Around(bytes, at)}\"");
            }
        }

        Assert.True(offenders.Count == 0,
            $"#251 · {offenders.Count} file(s) under src/, tests/ or docs/ are not valid UTF-8:\n" +
            string.Join("\n", offenders) + "\n\n" +
            "This whole repository is UTF-8 and LF (see the root .gitattributes). A lone high byte is\n" +
            "almost always a character that arrived from somewhere Latin-1 — `·` written as 0xB7 instead\n" +
            "of 0xC2 0xB7, `…` as 0x85, a curly quote as 0x92. Nothing in the toolchain will tell you:\n" +
            "Roslyn substitutes U+FFFD and carries on, and git only calls a file binary if it finds a NUL.\n" +
            "Write the character as UTF-8 and the byte goes away. If the file genuinely has to hold a raw\n" +
            "byte, it is not one of the kinds this law sweeps and does not belong under one of these\n" +
            "extensions.");
    }

    // ── THE GATE ITSELF ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 · The world this law is stated against can tell pass from fail. A sweep that found nothing, or
    /// that quietly stopped matching one of the extensions, would be green for ever over a tree full of
    /// mojibake — this repo's fifth named bug class.
    /// </summary>
    [Fact]
    public void THE_GATE_CanTellPassFromFail()
    {
        List<string> all = EveryHandWrittenFile().ToList();

        Assert.True(all.Count > 1_000,
            $"only {all.Count} hand-written file(s) found under src/, tests/ and docs/ — the sweep is blind.");

        // Each tree, and each kind, is really being reached. Named files rather than counts: a glob that
        // silently stopped matching would leave part of this law asserting nothing at all.
        Assert.Contains("src/SpaceSails.Core/Simulator.cs", all);
        Assert.Contains("src/SpaceSails.Client/Pages/Map.razor", all);
        Assert.Contains("src/SpaceSails.Client/Pages/Map.razor.css", all);
        Assert.Contains("tests/SpaceSails.Client.Tests/TheHudSaysWhereTheAirComesFromTests.cs", all);
        Assert.Contains("docs/testing-guide.md", all);
        Assert.All(TheTreesWeWrite, tree =>
            Assert.Contains(all, rel => rel.StartsWith(tree + "/", StringComparison.Ordinal)));
        Assert.All(TheKindsWeWrite, kind =>
            Assert.Contains(all, rel => rel.EndsWith(kind, StringComparison.Ordinal)));

        // No build output crept in.
        Assert.DoesNotContain(all, rel => rel.Contains("/obj/", StringComparison.Ordinal));
        Assert.DoesNotContain(all, rel => rel.Contains("/bin/", StringComparison.Ordinal));

        // And the reader really does refuse bad bytes rather than replacing them, which is the one thing
        // this whole law rests on: a decoder with the default fallback would hand back U+FFFD and pass.
        Assert.Equal(-1, FirstBadByte("a · b"u8.ToArray()));
        Assert.Equal(-1, FirstBadByte([0xEF, 0xBB, 0xBF, (byte)'x']));          // a BOM is fine
        Assert.Equal(2, FirstBadByte([(byte)'a', (byte)'b', 0xB7, (byte)'c']));  // and a lone 0xB7 is not
    }
}
