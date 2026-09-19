using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1244 · <b>WHETHER ANYBODY IS LOOKING IS A YIELD INPUT, AND NOTHING ELSE.</b>
///
/// <para>#1244 gave the boot a new fact about the browser: <c>document.visibilityState</c>, read so the
/// staged hand-back can tell a served timer from a rationed one. It is the first thing the boot has ever
/// known that is <b>not</b> a function of the URL — two captains opening the identical link, one of them
/// with the tab in front and one with it behind, must still be handed the identical sky, and no hash in
/// <c>TheBootBuildsTheSameWorldTests</c> (the 88-URL sweep) may move because one of them alt-tabbed.</para>
///
/// <para>Those hash sweeps cannot catch this themselves: they run OFF a browser, where
/// <see cref="OperatingSystem.IsBrowser"/> is false, the module is never imported and the answer is a
/// constant <c>false</c> for every world they build. A reader that crept into a planner would be invisible
/// to every one of them and visible only to a player. So the law is held HERE, where it can be: the
/// question may be ASKED in exactly two places — the interop wrapper that owns it, and the one seam that
/// consumes it — and nowhere else in the shipping client.</para>
///
/// <para>Proven red by adding <c>if (RendererInterop.PageIsHidden()) { seed++; }</c> to
/// <c>Map.Sim.World.Build.cs</c>: the sweep names the file and the line.</para>
/// </summary>
public class TheHiddenTabDecidesNothingAboutTheWorldTests
{
    /// <summary>The two places that may know. One owns the reading (and is the only file allowed to name
    /// the JS export), one spends it on the choice of hand-back.</summary>
    private static readonly string[] TheOnlyTwoPlacesThatMayAsk =
    [
        Path.Combine("Rendering", "RendererInterop.cs"),
        Path.Combine("Pages", "Map.Sim.Boot.cs"),
    ];

    /// <summary>Every way this repo could spell the question — the C# wrapper, the JS export it stands on,
    /// and the two DOM properties underneath both, so a second reader could not be opened by going around
    /// <see cref="SpaceSails.Client.Rendering.RendererInterop"/> rather than through it.</summary>
    private static readonly string[] TheWaysToAsk =
    [
        "PageIsHidden",
        "pageIsHidden",
        "visibilityState",
        "document.hidden",
    ];

    [Fact]
    public void Only_the_yield_seam_and_its_interop_may_ask_whether_anybody_is_looking()
    {
        var offences = new List<string>();

        foreach (string path in ShippingClientSources())
        {
            string relative = Path.GetRelativePath(ClientRoot, path);
            if (TheOnlyTwoPlacesThatMayAsk.Any(allowed =>
                    relative.Equals(allowed, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                // The law is about CODE that reads it, not about prose describing the law — and every one
                // of these files is heavily commented, this lane's own most of all.
                string line = lines[i];
                string code = StripTheComment(line);
                foreach (string spelling in TheWaysToAsk)
                {
                    if (code.Contains(spelling, StringComparison.Ordinal))
                    {
                        offences.Add($"{relative}:{i + 1}  {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(offences.Count == 0,
            "#1244 — whether anybody is looking at the tab decides HOW the boot hands the frame back and "
            + "nothing else. These lines ask it somewhere else, and a world that can hear the question is a "
            + "world that two captains on the identical URL could be handed differently — which no hash in "
            + "TheBootBuildsTheSameWorldTests can see, because that sweep runs off a browser where the "
            + "answer is always false:\n  " + string.Join("\n  ", offences)
            + $"\n\nThe only two places that may ask: {string.Join(", ", TheOnlyTwoPlacesThatMayAsk)}");
    }

    /// <summary>The renderer's hidden-document HEARTBEAT is a different thing and is allowed — it is why
    /// warp time keeps passing when the player looks away (#167) — so this guard states out loud that it
    /// is not sweeping <c>renderer.js</c> blind: the file must still carry both the heartbeat and the one
    /// export the yield seam stands on, or the exclusion above has stopped being about anything.</summary>
    [Fact]
    public void The_renderers_own_two_uses_are_both_still_there()
    {
        string js = File.ReadAllText(Path.Combine(ClientRoot, "wwwroot", "renderer.js"));

        Assert.Contains("export function pageIsHidden", js, StringComparison.Ordinal);
        Assert.Contains("export function serveTheShortestTimersOnATick", js, StringComparison.Ordinal);
        Assert.Contains("entry.hiddenTimerId = setInterval", js, StringComparison.Ordinal);
    }

    /// <summary>Every C# and Razor source the client ships — the whole surface a world could be built
    /// from. <c>obj/</c> and <c>bin/</c> are generated copies of these same files and would double every
    /// finding.</summary>
    private static IEnumerable<string> ShippingClientSources() =>
        Directory.EnumerateFiles(ClientRoot, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                     || p.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                                    StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                                    StringComparison.OrdinalIgnoreCase));

    /// <summary>The code half of a line — everything before a <c>//</c> that is not inside a string
    /// literal, plus nothing at all for a line that is only a doc comment.</summary>
    private static string StripTheComment(string line)
    {
        string trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("*", StringComparison.Ordinal)
            || trimmed.StartsWith("@*", StringComparison.Ordinal))
        {
            return "";
        }

        bool inString = false;
        for (int i = 0; i < line.Length - 1; i++)
        {
            if (line[i] == '"' && (i == 0 || line[i - 1] != '\\'))
            {
                inString = !inString;
            }
            else if (!inString && line[i] == '/' && line[i + 1] == '/')
            {
                return line[..i];
            }
        }
        return line;
    }

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
