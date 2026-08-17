using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #838 · TWO IDIOMS, TWO PLACES. The maneuver-node planner plans in the vector view and offers four
/// trajectory-relative quick selects; the plus/minus idiom went back to reflex flying, where it works.
///
/// <para>Owner (2026-08-11): <i>"The actual scrub flying should probably only use the vector view but have
/// like quick selects for forward, backward, up and down directions. I think the plus- and minus flying is
/// something that works for reflex flying but our planning sailship flying almost always use the vector
/// view… Also that panel could be bigger to fit all the buttons properly."</i> Ruling A (2026-08-17).</para>
///
/// <h3>Why these guards read SOURCE</h3>
/// <para>The arithmetic of the four directions is Core's, and Core tests fly it
/// (<c>PlanningHappensInTheGhostsFrameTests</c>). What no Core test can see is the SHAPE of the panel:
/// which controls the planner offers, which frame its aim is solved in, and whether reflex flying still
/// has the keys it always had. Those are the three things this issue actually moved, and each is one
/// careless edit away from coming back — a re-added ± button, a heading read off <c>_ship</c> because it
/// was closer to hand, a "tidy-up" that deletes the arrow keys. So the guards are written against the
/// text of the page, and every one of them names what it would catch.</para>
/// </summary>
public sealed class ThePlannerPlansInTheVectorViewTests
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

    /// <summary>The source with its comments taken out. A guard that says "the planner never calls
    /// NudgeHeading" must not be satisfied — or defeated — by a comment SAYING so: the prose that records
    /// why a control left is exactly the text that would hide the control coming back.</summary>
    private static string CodeOnly(string source)
    {
        source = Regex.Replace(source, @"@\*.*?\*@", " ", RegexOptions.Singleline);   // razor comments
        source = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);   // block comments
        return Regex.Replace(source, @"^\s*//.*$", " ", RegexOptions.Multiline);      // line comments
    }

    /// <summary>Everything the plotting panel is made of — the plot partials plus the page markup — read
    /// as one subject, the way it read out of one file before #870 split it.</summary>
    private static string Planner() => string.Concat(
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Plot*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>The open burn step's editor — the panel this issue is about, sliced out of the page so a
    /// "+" somewhere else on the map can never answer for it.</summary>
    private static string BurnEditorMarkup()
    {
        string razor = Source("Pages", "Map.razor");
        int at = razor.IndexOf("<div class=\"map-plan-step-edit\">", StringComparison.Ordinal);
        Assert.True(at > 0, "Map.razor no longer has the burn step's editor — this guard cannot see the panel.");
        int end = razor.IndexOf("armed auto-orbit is the terminal step", at, StringComparison.Ordinal);
        Assert.True(end > at, "the burn editor's end landmark moved — this guard cannot bound the panel.");
        return razor[at..end];
    }

    /// <summary>One method's body, signature to the closing brace at its own indent.</summary>
    private static string MethodBody(string file, string signature)
    {
        string src = Source("Pages", file).Replace("\r\n", "\n");
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at > 0, $"{file} no longer has `{signature}` — this guard cannot see the act.");
        int end = src.IndexOf("\n\n", at, StringComparison.Ordinal);
        Assert.True(end > at, $"`{signature}` has no end this guard can find.");
        return src[at..end];
    }

    // ---- GUARD (c): the planner has no plus/minus; reflex flying still does ---------------------

    /// <summary>
    /// LAW ONE — the maneuver-node planner offers no ± control of any kind. Not the mode toggle, not the
    /// prograde/retrograde pair, not the ±15° nudges. This is the half of the ruling that is a DELETION,
    /// and a deletion is the easiest thing in the world to undo by accident.
    /// </summary>
    [Fact]
    public void ThePlanner_OffersNoPlusMinusControl()
    {
        string code = CodeOnly(Planner());
        Assert.DoesNotContain("NudgeHeading", code);
        Assert.DoesNotContain("ToggleBurnMode", code);
        Assert.DoesNotContain("SetAction(", code);

        string editor = CodeOnly(BurnEditorMarkup());
        Assert.DoesNotContain("±", editor);
        Assert.DoesNotContain("&minus;", editor);
        Assert.DoesNotContain(">+<", editor);
        Assert.DoesNotContain("↺", editor);
        Assert.DoesNotContain("↻", editor);
        Assert.DoesNotContain("ManeuverAction.", editor);   // no direction-by-action control survives
    }

    /// <summary>
    /// LAW TWO — and reflex flying keeps every bit of it. The owner's ruling MOVED the idiom; it did not
    /// retire it. The live +/−/arrow keys still scale the ship's velocity by the plan's own factors, which
    /// is the control this issue was careful not to touch.
    /// </summary>
    [Fact]
    public void ReflexFlying_KeepsItsPlusAndMinus()
    {
        string keys = CodeOnly(Source("Pages", "Map.Sim.Keys.cs"));
        Assert.Contains("case \"+\":", keys);
        Assert.Contains("case \"-\":", keys);
        Assert.Contains("case \"ArrowUp\":", keys);
        Assert.Contains("case \"ArrowDown\":", keys);
        Assert.Contains("ManeuverPlan.AccelerateFactor", keys);
        Assert.Contains("ManeuverPlan.DecelerateFactor", keys);
    }

    // ---- The four buttons, and the free aim that stays ------------------------------------------

    /// <summary>LAW THREE — the panel offers exactly the four quick selects Core names, wearing Core's
    /// words, and each one sets THIS node's direction. Nothing here hard-codes a fifth button or a private
    /// label, so the words the owner blesses are the words the panel shows.</summary>
    [Fact]
    public void ThePanel_OffersTheFourQuickSelects_InCoresOwnWords()
    {
        string editor = BurnEditorMarkup();
        Assert.Contains("NodeFrame.QuickSelects", editor);
        Assert.Contains("NodeFrame.Label(", editor);
        Assert.Contains("NodeFrame.Hint(", editor);
        Assert.Contains("SetNodeDirection(node,", editor);
        Assert.Contains("NodeAimedAlong(node,", editor);      // the pressed direction lights up
        Assert.Contains("NodeFrame.FrameNote(", editor);      // and the panel names whose frame it is
    }

    /// <summary>LAW FOUR — free aim survives as the exception. "The intermediate angles are the exception"
    /// is not "the intermediate angles are gone": the typed heading field and its rel/abs toggle stay.</summary>
    [Fact]
    public void FreeAim_StaysInTheVectorView()
    {
        string editor = BurnEditorMarkup();
        Assert.Contains("SetHeading(node, e)", editor);
        Assert.Contains("_burnAngleAbsolute", editor);
        Assert.Contains("Free aim", editor);
    }

    // ---- GUARD (a)/(d), client half: the frame is the node's, and nothing is cached --------------

    /// <summary>
    /// LAW FIVE — the aim is solved from the NODE'S own epoch. The whole issue turns on this line: read
    /// the frame off the live ship (<c>_ship.SimTime</c>) or off the scrub bar (<c>ScrubTime</c>) and every
    /// quick select silently aims at a course the ghost left hours ago. The live ship may appear here only
    /// as the off-the-horizon fallback Core documents, and it appears exactly once.
    /// </summary>
    [Fact]
    public void TheAim_IsSolvedAtTheNodesEpoch()
    {
        string aim = MethodBody("Map.Plot.Nodes.cs", "private void AimNode(PlanNode node, NodeDirection direction)");
        Assert.Contains("NodeFrame.HeadingAt(", aim);
        Assert.Equal(2, Regex.Matches(aim, Regex.Escape("node.SimTime")).Count);   // the ribbon AND the primary
        Assert.DoesNotContain("ScrubTime", aim);
        Assert.DoesNotContain("_ship.SimTime", aim);
        Assert.Single(Regex.Matches(aim, Regex.Escape("_ship.Position, _ship.Velocity")));
    }

    /// <summary>
    /// LAW SIX — and pressing a quick select always re-solves. No memo field, no "skip if unchanged" early
    /// return: re-time the node, press FORWARD again, and the ribbon is read afresh at the new epoch.
    /// </summary>
    [Fact]
    public void PressingAQuickSelect_AlwaysReSolves()
    {
        string set = MethodBody("Map.Plot.Nodes.cs", "private void SetNodeDirection(PlanNode node, NodeDirection direction)");
        Assert.Contains("AimNode(node, direction)", set);
        Assert.Contains("RebuildPlan()", set);
        Assert.Contains("ReprojectTrajectory()", set);
        Assert.DoesNotContain("return;", CodeOnly(set));      // nothing short-circuits the re-solve

        // And no cache to go stale: the plan node stores a heading, never a solved frame.
        string plot = CodeOnly(Planner());
        Assert.DoesNotContain("_cachedHeading", plot);
        Assert.DoesNotContain("_lastAimed", plot);
    }

    // ---- The panel that fits its buttons ---------------------------------------------------------

    /// <summary>LAW SEVEN — the panel got bigger, as asked, and the four buttons are laid out so none of
    /// them can wrap onto a line of its own.</summary>
    [Fact]
    public void ThePanel_IsBiggerAndLaysTheFourButtonsOut()
    {
        string css = Source("Pages", "Map.razor.css");

        Match width = Regex.Match(css, @"\.map-plot\s*\{[^}]*?width:\s*([0-9.]+)rem", RegexOptions.Singleline);
        Assert.True(width.Success, "the plotting panel no longer declares a width — this guard cannot measure it.");
        Assert.True(double.Parse(width.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 36,
            $"the plotting panel is {width.Groups[1].Value}rem — too narrow for its buttons again (#838 widened it from 32rem).");

        Match quick = Regex.Match(css, @"\.map-burn-quick\s*\{[^}]*\}", RegexOptions.Singleline);
        Assert.True(quick.Success, "the quick-select row has no layout of its own.");
        Assert.Contains("grid", quick.Value);
        Assert.Contains("repeat(4", quick.Value);

        // The ±/✚ toggle's rule went with the toggle.
        Assert.DoesNotContain(".map-plot-mode {", css);
    }
}
