using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 item 1 · A SURFACE THAT LEAVES THE PAGE MUST TAKE THE PAGE'S CSS SCOPE WITH IT.
///
/// <para>This is the failure the decomposition would have shipped silently. Blazor's CSS isolation gives
/// every component its own scope identifier and compiles <c>Map.razor.css</c>'s rules to
/// <c>.selector[b-&lt;the page's scope&gt;]</c>. Move a block of markup into <c>Pages/Map/&lt;Surface&gt;.razor</c>
/// and its elements are stamped with a DIFFERENT identifier — so every rule that styled them stops matching.
/// No build error. No test. The card just renders unstyled, and you find out by looking at it.</para>
///
/// <para>(Only the 168 <c>::deep</c> rules would survive, because those compile to
/// <c>[b-&lt;page&gt;] .selector</c> and the moved markup is still a DOM descendant of <c>.map-page</c>. The
/// other four fifths of the stylesheet would not.)</para>
///
/// <para><b>The fix is one line of build, and this is the law that keeps it true.</b> Every surface carries a
/// <c>.razor.css</c> of its own — carrying no rules, only the scope — and the csproj pins that scope to the
/// page's own, so <c>b-4dqsdx4p75</c> lands on exactly the elements it landed on before the cut and the
/// shipped stylesheet does not change by a byte. A new surface added without its carrier gets no scope at
/// all, and this says so by name.</para>
///
/// <para><b>Verified against the compiler, not just asserted here:</b> built with
/// <c>-p:EmitCompilerGeneratedFiles=true</c>, the razor generator's output for the surfaces carries the
/// page's identifier — 252 occurrences of <c>b-4dqsdx4p75</c> in <c>NavHud_razor.g.cs</c>, 130 in
/// <c>SatchelPanel_razor.g.cs</c>, 3 in <c>ScuttleEpitaphCard_razor.g.cs</c> — the same literal the page's
/// own <c>Map_razor.g.cs</c> wears. That is the reading this file's source-level law stands in for.</para>
///
/// <para><b>Proven RED</b> by deleting <c>Pages/Map/ScuttleEpitaphCard.razor.css</c>: "1 surface(s) under
/// Pages/Map/ have no .razor.css beside them: ScuttleEpitaphCard.razor".</para>
///
/// <para>#251 · <b>AND THE SAME LAW OVER THE SENSORS DESK</b>, where it bites harder. The tracking post's
/// markup is cut into surfaces under <c>Pages/Stations/TrackingPost/</c> the same way, and
/// <c>TrackingPost.razor.css</c> has FORTY rules with NOT ONE <c>::deep</c> among them — so unlike Map's
/// sheet, where a fifth of the rules would have survived a missing carrier on their own, here every single
/// rule is scope-dependent and every single one would stop matching. Both decompositions are held to this
/// one law, by the one list below.</para>
/// </summary>
public sealed class EverySurfaceWearsThePagesCssScopeTests
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

    private static string ClientDir => Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

    /// <summary>Every decomposed page in the repo: where its surfaces live, and the two csproj entries that
    /// have to pin the page and its surfaces to one identifier.</summary>
    private static IReadOnlyList<(string Label, string SurfacesDir, string SheetPin, string SurfacePin)> Decompositions =>
    [
        ("Pages/Map/", Path.Combine(ClientDir, "Pages", "Map"),
            "Pages/Map.razor.css", "Pages/Map/*.razor.css"),
        ("Pages/Stations/TrackingPost/", Path.Combine(ClientDir, "Pages", "Stations", "TrackingPost"),
            "Pages/Stations/TrackingPost.razor.css", "Pages/Stations/TrackingPost/*.razor.css"),
    ];

    private static string SurfacesDir => Path.Combine(ClientDir, "Pages", "Map");

    private static string DeskSurfacesDir => Path.Combine(ClientDir, "Pages", "Stations", "TrackingPost");

    /// <summary>The head of a top-level rule in a stylesheet — a selector at column 0 up to its brace.</summary>
    private const string RuleHead = @"(?m)^[.#\[a-zA-Z][^{}\n]*\{";

    private static string Csproj => File.ReadAllText(Path.Combine(ClientDir, "SpaceSails.Client.csproj"));

    /// <summary>Every surface has the carrier that gives it a scope at all.</summary>
    [Fact]
    public void EverySurfaceHasItsOwnStylesheetBesideIt()
    {
        string[] naked =
        [
            .. Decompositions
                .SelectMany(d => Directory
                    .EnumerateFiles(d.SurfacesDir, "*.razor", SearchOption.TopDirectoryOnly)
                    .Where(p => !File.Exists(p + ".css"))
                    .Select(p => d.Label + Path.GetFileName(p)))
                .OrderBy(n => n, StringComparer.Ordinal)
        ];

        Assert.True(naked.Length == 0,
            $"#251 · {naked.Length} surface(s) have no .razor.css beside them: "
            + string.Join(", ", naked) + ".\n\n"
            + "A component with no stylesheet of its own gets no CSS SCOPE, and its page's rules are scoped:\n"
            + "every rule that styles this surface would stop matching it the moment it moved out of the page,\n"
            + "silently. Add a carrier next to it — a comment is enough — and the csproj's CssScope entry hands\n"
            + "it the page's own identifier. (Under Pages/Stations/TrackingPost/ that is ALL of the desk's\n"
            + "rules: TrackingPost.razor.css has not one ::deep rule to survive on its own.)");
    }

    /// <summary>The page and its surfaces are pinned to ONE identifier, and it is the same one.</summary>
    [Fact]
    public void ThePageAndItsSurfacesArePinnedToTheSameScope()
    {
        MatchCollection pins = Regex.Matches(
            Csproj, @"<None\s+Update=""([^""]+)""\s+CssScope=""([^""]+)""\s*/>");

        Dictionary<string, string> byPath = pins.ToDictionary(
            m => m.Groups[1].Value.Replace('\\', '/'), m => m.Groups[2].Value, StringComparer.Ordinal);

        foreach ((string label, _, string sheetPin, string surfacePin) in Decompositions)
        {
            Assert.True(byPath.ContainsKey(sheetPin),
                $"SpaceSails.Client.csproj no longer pins {sheetPin}'s CSS scope — without the pin the page "
                + "and its surfaces are free to drift onto different identifiers, and the drift is invisible.");
            Assert.True(byPath.ContainsKey(surfacePin),
                $"SpaceSails.Client.csproj no longer pins {label}'s CSS scope. Every surface would take an "
                + "identifier of its own and lose the page's stylesheet with it.");
            Assert.Equal(byPath[sheetPin], byPath[surfacePin]);
            Assert.Matches("^b-[a-z0-9]+$", byPath[sheetPin]);
        }

        // …and the two decompositions do NOT share an identifier: Map's rules must not start matching the
        // desk's elements, which is what one scope for both would quietly do.
        Assert.NotEqual(byPath["Pages/Map.razor.css"], byPath["Pages/Stations/TrackingPost.razor.css"]);
    }

    /// <summary>#251 · The world this law is stated against can tell pass from fail: there are surfaces to
    /// hold it to, and the stylesheet really does depend on the scope — the great majority of its rules are
    /// NOT <c>::deep</c>, so they only ever match an element wearing the page's identifier.</summary>
    [Fact]
    public void THE_SCOPE_LAW_CanTellPassFromFail()
    {
        string[] surfaces = [.. Directory.EnumerateFiles(SurfacesDir, "*.razor", SearchOption.TopDirectoryOnly)];
        Assert.True(surfaces.Length > 60,
            $"only {surfaces.Length} surface(s) under Pages/Map/ — this law is guarding an empty room.");

        string sheet = MapStylesheet.Text;
        int deep = Regex.Matches(sheet, @"::deep").Count;
        int selectors = Regex.Matches(sheet, RuleHead).Count;
        Assert.True(deep > 100, $"only {deep} ::deep rule(s) — is this still Map.razor.css?");
        Assert.True(selectors > deep * 3,
            $"{selectors} rule(s) against {deep} ::deep — if the sheet were mostly ::deep, the scope would "
            + "hardly matter and this law would be guarding nothing.");

        // #251 · the same two questions of the sensors desk, where the answer is starker: there are surfaces
        // to hold the law to, and NOT ONE of that sheet's rules survives a missing carrier, because not one
        // of them is ::deep.
        string[] deskSurfaces = [.. Directory.EnumerateFiles(DeskSurfacesDir, "*.razor", SearchOption.TopDirectoryOnly)];
        Assert.True(deskSurfaces.Length >= 8,
            $"only {deskSurfaces.Length} surface(s) under Pages/Stations/TrackingPost/ — this law is guarding "
            + "an empty room.");

        string deskSheet = File.ReadAllText(Path.Combine(ClientDir, "Pages", "Stations", "TrackingPost.razor.css"));
        int deskRules = Regex.Matches(deskSheet, RuleHead).Count;
        Assert.True(deskRules > 20, $"only {deskRules} rule(s) — is this still TrackingPost.razor.css?");
        Assert.True(Regex.Matches(deskSheet, @"::deep").Count == 0,
            "TrackingPost.razor.css has grown a ::deep rule. That is not wrong in itself, but the sentence "
            + "this law leans on — that EVERY rule in the desk's sheet is scope-dependent, so a missing "
            + "carrier unstyles all of it — would no longer be the whole truth.");
    }
}
