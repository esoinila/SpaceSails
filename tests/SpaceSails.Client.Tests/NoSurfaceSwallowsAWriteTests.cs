using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 item 1 · A SURFACE MAY NOT SWALLOW A WRITE.
///
/// <para>This is the one way the decomposition can break the game while every test in the repo stays green,
/// and it broke it twice before this law existed. The markup that moved out of <c>Map.razor</c> does not only
/// READ the page: <c>@onclick="() => _walletOpen = !_walletOpen"</c> assigns a field on the PAGE, and so does
/// <c>@bind="_scrubOffsetSeconds"</c>, which is the same assignment with the compiler writing it for you.
/// Hand that member down as an ordinary one-way <c>[Parameter]</c> and the code still compiles, still runs,
/// and quietly does nothing: the component writes its own copy, the page never hears it, the satchel tab you
/// pressed does not change and the plot scrub does not move.</para>
///
/// <para><b>Nothing in the xUnit suites can see that</b> — none of them presses a button in a browser. Five
/// swallowed writes shipped inside this refactor's own first two commits and were found by an audit written
/// afterwards; a sixth (<c>@bind="_scrubOffsetSeconds"</c>) got past that audit too, because the audit looked
/// for an <c>=</c> and <c>@bind</c> does not have one. It was caught by the UI gate, in a browser, three
/// minutes of CI later. This law is the audit made permanent and made to cover both spellings.</para>
///
/// <para><b>The shape a written member must take</b> — the value in, the page's own setter out, and a private
/// property that keeps the member's original name so the moved markup still reads as it read in the page:</para>
/// <code>
/// [Parameter] public bool _walletOpenValue { get; set; }
/// [Parameter] public Action&lt;bool&gt; _walletOpenSet { get; set; } = default!;
/// private bool _walletOpen { get =&gt; _walletOpenValue; set { _walletOpenValue = value; _walletOpenSet(value); } }
/// </code>
///
/// <para><b>It found one the moment it was written</b>: <c>SaveLoadRack</c> writes <c>_renameDraft</c> from
/// the berth-rename field's <c>@oninput</c>, and that member had been handed down one-way. Fixed in the same
/// commit.</para>
///
/// <para><b>Proven RED</b> by collapsing that pair back to a one-way parameter — deleting the
/// <c>_renameDraftSet</c> parameter and the private property, and passing <c>_renameDraft</c> straight in:
/// "SaveLoadRack.razor writes `_renameDraft`, which is a one-way [Parameter] — the page will never hear
/// it."</para>
/// </summary>
public sealed class NoSurfaceSwallowsAWriteTests
{
    private static string RepoRoot()
    {
        for (DirectoryInfo? at = new(AppContext.BaseDirectory); at is not null; at = at.Parent)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string SurfacesDir =>
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map");

    /// <summary>Everything the moved block of one surface writes: an assignment it spells out, and an
    /// assignment <c>@bind</c> spells for it.</summary>
    internal static IReadOnlyList<string> WhatItWrites(string markup)
    {
        var written = new List<string>();

        // An assignment the markup spells: `_x = y`, `_x++`, `_x += 1`. NOT an attribute (`Attr="value"`,
        // which is an identifier immediately followed by `="`), and not a comparison.
        foreach (Match m in Regex.Matches(
            markup, @"(?<![\w.])([A-Za-z_]\w*)\s*(?:=(?![=>""'])|\+\+|--|\+=|-=|\*=|/=)"))
        {
            written.Add(m.Groups[1].Value);
        }

        // …and the assignment @bind writes for it. `@bind="_x"` on an element and `@bind-Foo="_x"` on a
        // component both compile to a setter that assigns to `_x`.
        foreach (Match m in Regex.Matches(markup, @"@bind(?:-[A-Za-z]\w*)?=""@?([A-Za-z_]\w*)"))
        {
            written.Add(m.Groups[1].Value);
        }

        return written;
    }

    /// <summary>The one-way parameters of a surface: the ones whose name IS the parameter, rather than the
    /// <c>…Value</c>/<c>…Set</c> pair standing behind a private property of the member's own name.</summary>
    private static HashSet<string> OneWayParameters(string file) =>
    [
        .. Regex.Matches(file, @"\[Parameter\] public [^\n]*? ([A-Za-z_]\w*) \{ get; set; \}")
            .Select(m => m.Groups[1].Value)
    ];

    [Fact]
    public void EveryPageMemberASurfaceWritesIsHandedDownTwoWay()
    {
        var swallowed = new List<string>();

        foreach (string path in Directory
            .EnumerateFiles(SurfacesDir, "*.razor", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.Ordinal))
        {
            string text = File.ReadAllText(path);
            int begins = text.IndexOf(MapMarkup.MarkupBegins, StringComparison.Ordinal);
            int ends = text.IndexOf(MapMarkup.MarkupEnds, StringComparison.Ordinal);
            if (begins < 0 || ends <= begins)
            {
                continue;
            }

            string markup = text[begins..ends];
            HashSet<string> oneWay = OneWayParameters(text);

            foreach (string name in WhatItWrites(markup).Distinct(StringComparer.Ordinal)
                                                        .OrderBy(n => n, StringComparer.Ordinal))
            {
                if (oneWay.Contains(name))
                {
                    swallowed.Add($"  {Path.GetFileName(path)} writes `{name}`, which is a one-way "
                                  + "[Parameter] — the page will never hear it.");
                }
            }
        }

        Assert.True(swallowed.Count == 0,
            $"#251 · {swallowed.Count} write(s) in the moved markup land on the component and stop there:\n"
            + string.Join("\n", swallowed) + "\n\n"
            + "The page's own field is what the markup wrote when it lived in the page, and it is what has to\n"
            + "be written now. Hand the member down as a PAIR and put its own name on a private property:\n\n"
            + "    [Parameter] public T <name>Value { get; set; }\n"
            + "    [Parameter] public Action<T> <name>Set { get; set; } = default!;\n"
            + "    private T <name> { get => <name>Value; set { <name>Value = value; <name>Set(value); } }\n\n"
            + "…and pass `<name>Value=\"@<name>\" <name>Set=\"@(v => <name> = v)\"` from Map.razor. Nothing in\n"
            + "the xUnit suites can see this failing on its own: it compiles, it runs, and the control simply\n"
            + "does nothing.");
    }

    /// <summary>#251 · The world this law is stated against can tell pass from fail. It must be reading real
    /// markup, it must actually FIND the writes that are there, and it must not be counting every attribute
    /// in the file as one.</summary>
    [Fact]
    public void THE_WRITE_AUDIT_CanTellPassFromFail()
    {
        // It finds a spelled assignment, and the one @bind spells for it.
        Assert.Contains("_walletOpen", NoSurfaceSwallowsAWriteTests.WhatItWrites(
            """@onclick="() => _walletOpen = !_walletOpen">"""));
        Assert.Contains("_scrubOffsetSeconds", WhatItWrites(
            """<input type="range" @bind="_scrubOffsetSeconds" @bind:event="oninput" />"""));
        Assert.Contains("_credits", WhatItWrites("""<DarkWeb @bind-Credits="_credits" />"""));

        // …and it does NOT call an attribute an assignment, or a comparison one.
        Assert.DoesNotContain("class", WhatItWrites("""<div class="satchel-page">"""));
        Assert.DoesNotContain("Dismiss", WhatItWrites("""<OverlayShell Dismiss="OverlayDismiss.Close" />"""));
        Assert.DoesNotContain("_satchelPage", WhatItWrites("""@if (_satchelPage == SatchelPage.Notes)"""));

        // And there are surfaces to hold it to, several of which really do write.
        string[] surfaces = [.. Directory.EnumerateFiles(SurfacesDir, "*.razor", SearchOption.TopDirectoryOnly)];
        Assert.True(surfaces.Length > 60, $"only {surfaces.Length} surface(s) — this law is guarding an empty room.");

        int writers = surfaces.Count(p =>
        {
            string t = File.ReadAllText(p);
            int b = t.IndexOf(MapMarkup.MarkupBegins, StringComparison.Ordinal);
            int e = t.IndexOf(MapMarkup.MarkupEnds, StringComparison.Ordinal);
            return b >= 0 && e > b && WhatItWrites(t[b..e])
                .Any(n => t.Contains($"public Action<", StringComparison.Ordinal) && t.Contains($"{n}Set ", StringComparison.Ordinal));
        });
        Assert.True(writers >= 8,
            $"only {writers} surface(s) hand a written member down two-way — either the pattern has been "
            + "unwound or this audit has stopped seeing the writes it is here to see.");
    }
}
