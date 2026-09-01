using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #949 · THE NAV WALKTHROUGH — eight steps, in order, each with a picture.
///
/// <para>Owner, 2026-08-18, having just flown a scrubbed multi-step plan for the first time and posted a
/// screenshot of the Plotting panel: <i>"We should have a help page where we show multi-step plan and the
/// use of the schrub and burn. New player seeing this image would understand how to play. I really like
/// the increment and decrement options."</i></para>
///
/// <para>What can actually go wrong with a help page, and is guarded here:</para>
/// <list type="number">
///   <item><b>IT IS UNREACHABLE.</b> A page nobody links to is a page nobody reads. The route is read off
///   the COMPILED component's <see cref="RouteAttribute"/> and matched against the literal hrefs in
///   Map.razor's Nav toolbar, the Guide and the Captain desk — so a renamed route breaks the test rather
///   than the link.</item>
///   <item><b>A STEP GOES MISSING, OR THE ORDER DRIFTS.</b> The eight steps are the loop; seven of them in
///   the wrong order is worse than none, because the reader will trust it.</item>
///   <item><b>THE PICTURES ROT.</b> Deliberately no screenshots — a photograph of the live panel is stale
///   the afternoon somebody moves a button, and nothing goes red when it does. Every illustration is an
///   inline SVG sketch built in the page's own code block, and this sweep insists on one per step.</item>
///   <item><b>THE BUTTON FACES DRIFT.</b> This is the repo's named bug class — a sentence reporting one
///   thing while the sim does another. The ± faces on the page must be the SAME
///   <see cref="NodeFrame"/> calls the real burn row renders, never typed copies, so moving a step
///   constant moves the help page with it.</item>
/// </list>
/// </summary>
public sealed class TheNavHelpPageTeachesTheWholeLoopTests
{
    private static string RepoRoot()
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

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    private static string HelpPage() => Source("Pages", "HelpNav.razor");

    /// <summary>The route the compiled component actually answers on — asked of the type, not of the
    /// text, so a link is checked against the real thing.</summary>
    private static string CompiledRoute(Type page) =>
        page.GetCustomAttributes<RouteAttribute>().Single().Template;

    /// <summary>The eight steps, in the order a trip is flown. This list IS the specification; the page is
    /// checked against it, and it is checked against the page.</summary>
    private static readonly (string Id, string Heading)[] Steps =
    [
        ("step-1", "1. Pick a target"),
        ("step-2", "2. Press Plot"),
        ("step-3", "3. Scrub to a future time"),
        ("step-4", "4. + Add burn at scrub"),
        ("step-5", "5. Iterate with the ± buttons"),
        ("step-6", "6. + Add orbit (or dock) at scrub"),
        ("step-7", "7. Arm it"),
        ("step-8", "8. Warp"),
    ];

    // ── LAW 1 · THE PAGE EXISTS, AND EVERY WAY IN LEADS TO IT ──────────────────────────────────────

    /// <summary>The page is a real routable component at a real route. RED PROOF: delete the
    /// <c>@page</c> directive and there is no RouteAttribute to read.</summary>
    [Fact]
    public void TheHelpPageIsRoutable()
    {
        Type page = typeof(SpaceSails.Client.Pages.HelpNav);
        Assert.Equal("/help/nav", CompiledRoute(page));
    }

    /// <summary>
    /// THE ? ON THE NAV TOOLBAR STILL LEADS HERE — VIA THE CARD.
    ///
    /// <para>#949 · The road grew a hop and this guard grew with it, rather than being relaxed. The <c>?</c>
    /// was an <c>&lt;a href="/help/nav" target="_blank"&gt;</c>; it raises the in-game plotting card now, and
    /// the CARD's foot carries the link on to this page. The reason is in Map.NavHelp.cs: the <c>?</c> is
    /// pressed mid-plan, and a full page in a second tab answers a question about the panel by taking the
    /// panel off the screen.</para>
    ///
    /// <para>What is asserted is the whole chain, both hops, so neither can quietly stop resolving: the
    /// toolbar carries a <c>?</c> BUTTON (that pressing it raises the card is proved by pressing, in
    /// <see cref="TheHelpCardTeachesTodaysPanelTests"/>), and the card links to this page's own compiled
    /// route. RED PROOF: point the card's foot at /guide alone and this goes red naming the route.</para>
    /// </summary>
    [Fact]
    public void TheNavToolbarQuestionMarkLeadsToTheHelpPageThroughTheCard()
    {
        string map = Regex.Replace(Source("Pages", "Map.razor"), @"\s+", " ");
        // `[^<]*` and not `[^>]*`: the button's own @onclick is a lambda, so there is a `>` INSIDE the tag
        // and the obvious pattern never matches. There is no `<` in it, which is the honest boundary here.
        Assert.Matches(@"<button[^<]*ToggleNavHelp[^<]*>\?</button>", map);

        string route = CompiledRoute(typeof(SpaceSails.Client.Pages.HelpNav));
        Assert.Contains($"href=\"{route}\"", Source("Components", "PlottingHelp.razor"), StringComparison.Ordinal);
    }

    /// <summary>Two more doors in, both of which the owner asked for: the Guide's plotting section, and
    /// the Captain desk's Tutorials tab (where starting a lesson already lives).</summary>
    [Fact]
    public void TheGuideAndTheCaptainsTutorialsBothLinkToIt()
    {
        string route = CompiledRoute(typeof(SpaceSails.Client.Pages.HelpNav));
        Assert.Contains($"href=\"{route}\"", Source("Pages", "Guide.razor"), StringComparison.Ordinal);
        Assert.Contains($"href=\"{route}\"", Source("Pages", "Stations", "Captain.razor"), StringComparison.Ordinal);
    }

    /// <summary>…and the walkthrough hands the reader on to the full reference, so the short page is a
    /// door and not a dead end.</summary>
    [Fact]
    public void TheHelpPageLinksOnToTheFullGuide()
    {
        string guideRoute = CompiledRoute(typeof(SpaceSails.Client.Pages.Guide));
        Assert.Contains($"href=\"{guideRoute}\"", HelpPage(), StringComparison.Ordinal);
    }

    // ── LAW 2 · EIGHT STEPS, IN ORDER ──────────────────────────────────────────────────────────────

    /// <summary>Every step is on the page, spelled as specified, and they appear top to bottom in flying
    /// order. RED PROOF: swap any two headings, or drop one, and the sequence assert names it.</summary>
    [Fact]
    public void ThePageNamesEveryStepInOrder()
    {
        string page = HelpPage();

        var found = Regex.Matches(page, @"<h2 class=""h5"" id=""(step-\d)"">([^<]+)</h2>")
            .Select(m => (Id: m.Groups[1].Value, Heading: m.Groups[2].Value.Trim()))
            .ToArray();

        Assert.Equal(Steps.Length, found.Length);
        for (int i = 0; i < Steps.Length; i++)
        {
            Assert.Equal(Steps[i].Id, found[i].Id);
            Assert.Equal(Steps[i].Heading, found[i].Heading);
        }
    }

    /// <summary>The step the owner named as the whole point — "I really like the increment and decrement
    /// options" — teaches all three pairs by name: aim, size, and when.</summary>
    [Fact]
    public void TheIterateStepTeachesAllThreePairs()
    {
        string flat = Regex.Replace(HelpPage(), @"\s+", " ");
        Assert.Contains("NodeFrame.NudgeLabel(-1)", flat, StringComparison.Ordinal);
        Assert.Contains("NodeFrame.NudgeMagnitudeLabel(-1, true)", flat, StringComparison.Ordinal);
        Assert.Contains("NodeFrame.NudgeEpochLabel(-1, true)", flat, StringComparison.Ordinal);
        Assert.Contains("The course re-solves under every press", flat, StringComparison.Ordinal);
    }

    /// <summary>Step six carries the #965 arrive step's own vocabulary — the ✓/✗ badge, and the ruling
    /// that an ✗ is a to-do list and not a refusal.</summary>
    [Fact]
    public void TheArriveStepIsTaughtWithItsBadge()
    {
        string flat = Regex.Replace(HelpPage(), @"\s+", " ");
        Assert.Contains("✓ VALID", flat, StringComparison.Ordinal);
        Assert.Contains("✗ INVALID", flat, StringComparison.Ordinal);
        Assert.Contains("+ Add orbit at scrub", flat, StringComparison.Ordinal);
        Assert.Contains("+ Add dock at scrub", flat, StringComparison.Ordinal);
    }

    // ── LAW 3 · ONE PICTURE PER STEP, AND NONE OF THEM A SCREENSHOT ────────────────────────────────

    /// <summary>Eight sketch helpers, eight calls, one per step — and each helper really draws an
    /// <c>&lt;svg&gt;</c>. RED PROOF: delete a sketch and the counts stop matching the step list.</summary>
    [Fact]
    public void EveryStepCarriesExactlyOneDrawnIllustration()
    {
        string page = HelpPage();

        string[] declared = Regex.Matches(page, @"private MarkupString (Sketch\w+)\(\)")
            .Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(Steps.Length, declared.Length);

        // Every declared sketch is called exactly once from the markup (the declaration itself is the
        // second occurrence of the name, hence two matches per sketch and no more).
        foreach (string sketch in declared)
        {
            int uses = Regex.Matches(page, Regex.Escape("@" + sketch + "()")).Count;
            Assert.Equal(1, uses);
        }

        // …and the shared frame really emits an svg element with a viewBox.
        Assert.Contains("<svg viewBox=", page, StringComparison.Ordinal);
    }

    /// <summary>NO SCREENSHOTS, EVER. A picture of the live panel dates the moment a button moves and
    /// nothing goes red when it does. The page may reference no image asset of any kind.</summary>
    [Fact]
    public void ThePageShipsNoScreenshot()
    {
        string page = HelpPage();
        Assert.DoesNotContain("<img", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("art/", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("background-image", page, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Every sketch is labelled for a reader who cannot see it — a help page that only helps
    /// people with working eyes is half a help page.</summary>
    [Fact]
    public void EverySketchCarriesItsOwnDescription()
    {
        string page = HelpPage();
        int frames = Regex.Matches(page, @"Frame\(""[^""]{10,}"",").Count;
        Assert.Equal(Steps.Length, frames);
        Assert.Contains(@"role=""img"" aria-label=""{title}""", page, StringComparison.Ordinal);
    }

    // ── LAW 4 · THE BUTTON FACES ARE BORROWED, NEVER TYPED ─────────────────────────────────────────

    /// <summary>
    /// The ± faces on the page come from <see cref="NodeFrame"/>, the same calls the real burn row
    /// renders — so the page cannot claim a step size the panel does not offer. Proven by asking Core
    /// for today's faces and insisting the page contains no TYPED copy of any of them.
    /// <para>RED PROOF: paste "+5 p" into the page as a literal and this goes red; change
    /// <c>NudgePulsesCoarse</c> and the page follows without an edit.</para>
    /// </summary>
    [Fact]
    public void TheStepFacesAreBorrowedFromCoreAndNeverTyped()
    {
        // Strip the razor comment block: it QUOTES the faces in prose to explain the rule, which is
        // exactly the place a literal is harmless and a ban would be silly.
        string page = Regex.Replace(HelpPage(), @"@\*.*?\*@", " ", RegexOptions.Singleline);

        string[] liveFaces =
        [
            NodeFrame.NudgeMagnitudeLabel(1, true),
            NodeFrame.NudgeMagnitudeLabel(-1, true),
            NodeFrame.NudgeMagnitudeLabel(1, false),
            NodeFrame.NudgeMagnitudeLabel(-1, false),
            NodeFrame.NudgeEpochLabel(1, true),
            NodeFrame.NudgeEpochLabel(-1, true),
            NodeFrame.NudgeEpochLabel(1, false),
            NodeFrame.NudgeEpochLabel(-1, false),
            NodeFrame.NudgeLabel(1),
            NodeFrame.NudgeLabel(-1),
        ];

        foreach (string face in liveFaces)
        {
            Assert.DoesNotContain(face, page, StringComparison.Ordinal);
        }

        // …and the page really does ask Core for them, rather than not mentioning them at all.
        Assert.Contains("NodeFrame.NudgeMagnitudeLabel", page, StringComparison.Ordinal);
        Assert.Contains("NodeFrame.NudgeEpochLabel", page, StringComparison.Ordinal);
        Assert.Contains("NodeFrame.NudgeLabel", page, StringComparison.Ordinal);
    }
}
