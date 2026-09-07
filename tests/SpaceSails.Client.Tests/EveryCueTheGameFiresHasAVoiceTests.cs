using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// A CUE NAME NOBODY DEFINED IS A SILENCE NOBODY REPORTS.
///
/// <para><b>The bug this exists for (#938 D1).</b> <c>renderer.js</c> keeps one table, <c>CUES</c>, and
/// <c>playCue</c> opens with <c>const cue = CUES[kind]; if (!cue) { return; }</c> — a name the table does
/// not carry is not an error, a warning or a console line. It is nothing. Sixty-one call sites across the
/// client fired names the table never had: <c>alarm</c> forty-one times (every atmosphere breach, every
/// scuttle, BUSTED, the deflection impact), <c>blip</c> eleven, <c>gameover</c> five, <c>burn</c> and
/// <c>door</c> twice each. Every alarm in the game was mute and had always been mute.</para>
///
/// <para><b>Why nothing went red.</b> <c>CUES</c> had zero hits under <c>tests/</c>. The cue guards that
/// do exist (<c>TheRedPenIsAnInstrumentTests</c>) assert that a verb WRITES a <c>PlayCue(…)</c> call —
/// which is exactly as true of a name that sounds as of a name that does not. Nothing in the repo had
/// ever compared the two halves, so the only way to find this was to read the JS beside the C#.</para>
///
/// <para><b>The law.</b> Every cue name the shipped client hands <c>PlayCue</c> is a key in
/// <c>renderer.js</c>'s <c>CUES</c> table. Both halves are read out of the tree, so adding a caller with
/// a typo, or deleting a cue that still has callers, goes red on the next run.</para>
///
/// <para><b>Reading the call sites.</b> A cue argument is not always a literal — three sites hand a
/// ternary and two a <c>switch</c> expression, and #664's beat cue comes through a Core table. So the
/// scan does not parse an argument: from each <c>PlayCue(</c> it reads forward to the statement's
/// terminating <c>;</c> and takes every double-quoted literal in that span. Ternaries and switch arms
/// come out whole; the empty string (#664's "this beat is already making its own noise") is not a cue
/// and is skipped, exactly as <c>PlayTheBeatsCue</c> skips it.</para>
///
/// <para>Both scans assert they found a world first. A regex that matched nothing would be green
/// forever, and this repo has a bug class named for guards handed a world that cannot fail.</para>
/// </summary>
public class EveryCueTheGameFiresHasAVoiceTests
{
    /// <summary>
    /// THE LAW. Every name the client fires at <c>playCue</c> is a voice <c>renderer.js</c> can make.
    /// </summary>
    [Fact]
    public void Every_cue_the_client_fires_is_defined_in_the_renderer()
    {
        IReadOnlySet<string> voices = CueTableInRenderer();

        // A table that read as empty would make every caller legal. Say the count out loud.
        Assert.True(voices.Count >= 14,
                    $"only {voices.Count} cue(s) parsed out of {RendererPath} — the CUES table was not read, "
                    + "so this guard proved nothing");

        IReadOnlyList<(string Name, string Where)> fired = CueNamesFiredInSource();

        // Likewise: if the call-site scan found nothing, every cue in the table is trivially sufficient.
        Assert.True(fired.Count >= 100,
                    $"only {fired.Count} PlayCue call site name(s) found under {ClientRoot} — the scan "
                    + "proved nothing");

        var mute = fired.Where(f => !voices.Contains(f.Name))
                        .OrderBy(f => f.Name, StringComparer.Ordinal)
                        .ThenBy(f => f.Where, StringComparer.Ordinal)
                        .ToList();

        Assert.True(
            mute.Count == 0,
            $"{mute.Count} call site(s) fire a cue name that renderer.js's CUES table does not define — "
            + "playCue returns in silence on an unknown name, so these make no sound at all:\n  "
            + string.Join("\n  ", mute.Select(m => $"{m.Where}: \"{m.Name}\"")));
    }

    /// <summary>
    /// THE OTHER DIRECTION. A cue with no caller is dead weight in the palette — not a crash, but the
    /// half of the pairing that tells us the table and the game still describe the same game. Kept as a
    /// separate fact so a deliberate spare (armed ahead of the beat that will use it) reads as one line
    /// of exception rather than a suppressed law.
    /// </summary>
    [Fact]
    public void Every_cue_the_renderer_defines_has_a_caller_somewhere()
    {
        IReadOnlySet<string> voices = CueTableInRenderer();
        var fired = CueNamesFiredInSource().Select(f => f.Name).ToHashSet(StringComparer.Ordinal);

        Assert.True(voices.Count >= 14, $"only {voices.Count} cue(s) parsed — the table was not read");
        Assert.True(fired.Count >= 5, $"only {fired.Count} distinct cue name(s) fired — the scan proved nothing");

        var orphans = voices.Where(v => !fired.Contains(v)).OrderBy(v => v, StringComparer.Ordinal).ToList();

        Assert.True(orphans.Count == 0,
                    "renderer.js defines cue(s) nothing in the client ever fires: "
                    + string.Join(", ", orphans));
    }

    // ── Reading the two halves ────────────────────────────────────────────────────────────────────────

    /// <summary>The keys of <c>const CUES = { … };</c> in <c>renderer.js</c>. The table is one object
    /// literal of one-line entries, so the keys are read off the line starts; entries are separated from
    /// the comment prose around them by requiring a <c>{</c> after the colon.</summary>
    private static IReadOnlySet<string> CueTableInRenderer()
    {
        string js = File.ReadAllText(RendererPath);

        int open = js.IndexOf("const CUES = {", StringComparison.Ordinal);
        Assert.True(open >= 0, $"no `const CUES = {{` in {RendererPath} — renderer.js changed shape");
        int close = js.IndexOf("\n};", open, StringComparison.Ordinal);
        Assert.True(close > open, $"the CUES table in {RendererPath} is never closed");

        string table = js[open..close];

        return Regex.Matches(table, @"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:\s*\{", RegexOptions.Multiline)
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Every cue name the shipped client hands <c>PlayCue</c>, with the file and line that
    /// hands it.</summary>
    private static IReadOnlyList<(string Name, string Where)> CueNamesFiredInSource()
    {
        var found = new List<(string, string)>();

        foreach (string path in Directory.GetFiles(ClientRoot, "*.cs", SearchOption.AllDirectories)
                                         .Concat(Directory.GetFiles(ClientRoot, "*.razor", SearchOption.AllDirectories))
                                         .OrderBy(p => p, StringComparer.Ordinal))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            string src = File.ReadAllText(path);
            string name = Path.GetRelativePath(RepoRoot, path).Replace('\\', '/');

            foreach (Match call in Regex.Matches(src, @"\bPlayCue\("))
            {
                // The interop's own declaration and the docblocks that quote it are not call sites.
                int semicolon = src.IndexOf(';', call.Index);
                if (semicolon < 0)
                {
                    continue;
                }

                int lineStart = src.LastIndexOf('\n', Math.Max(call.Index - 1, 0)) + 1;
                string lead = src[lineStart..call.Index].TrimStart();
                if (lead.StartsWith("//", StringComparison.Ordinal) || lead.StartsWith("*", StringComparison.Ordinal))
                {
                    continue; // prose about a cue, not a cue.
                }

                string span = src[call.Index..semicolon];
                int line = src.Take(call.Index).Count(c => c == '\n') + 1;

                foreach (Match literal in Regex.Matches(span, "\"([^\"\\\\\n]*)\""))
                {
                    string cue = literal.Groups[1].Value;
                    if (cue.Length == 0)
                    {
                        continue; // #664: Core says this beat already makes its own noise.
                    }
                    found.Add((cue, $"{name}:{line}"));
                }
            }
        }

        return found;
    }

    private static string RendererPath =>
        Path.Combine(ClientRoot, "wwwroot", "renderer.js");

    private static string ClientRoot => Path.Combine(RepoRoot, "src", "SpaceSails.Client");

    private static string RepoRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir, "src", "SpaceSails.Client")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("Could not find the repository root above the test assembly.");
        }
    }
}
