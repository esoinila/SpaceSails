using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #937 · THE NUMBERS GET THE SAME FRONT DOOR. Owner (2026-08-18, flying the scrub): <i>"we could have the
/// small + / − symbols also on the thrust amount number dialog for iterating the scrub panel. Now writing a
/// new number there I have to switch to another input to see what the effect of numeric change is. So for
/// all those scrub burns the ±5° type iteration buttons would be useful with some comparable
/// increment."</i>
///
/// <para>And the ruling the same sitting: <i>"only the fine / ultra-fine tuning would be the captain
/// entering the numeric value to the field, but the rough estimation with those + − buttons like the
/// angle."</i></para>
///
/// <h3>Why these guards read SOURCE, and where they stop</h3>
/// <para>How far one press moves a number and where it stops is ARITHMETIC, and Core's
/// <c>TheNumbersNudgeTooTests</c> flies it — including two impostors it proves go red. What no Core test
/// can see is the SHAPE of the panel: that the steps are real buttons a mouse can reach, that a press goes
/// through the SAME act a typed value takes (so a button can never reach a magnitude the field refuses and
/// there is no second solve path to drift), that the epoch floor is one law rather than three copies, and
/// that both pairs still fit a 390px phone. Those four are what this file holds, and each names what it
/// would catch.</para>
/// </summary>
public sealed class TheNumbersNudgeTooTests
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
        MapMarkup.Read(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    /// <summary>The source with its comments taken out — a guard about what the panel DOES must never be
    /// answered by prose SAYING it does. The razor comment above these buttons quotes the owner in full,
    /// including the "± " he typed, so without this every ±-guard below would be a lie.</summary>
    private static string CodeOnly(string source)
    {
        source = Regex.Replace(source, @"@\*.*?\*@", " ", RegexOptions.Singleline);
        source = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(source, @"^\s*//.*$", " ", RegexOptions.Multiline);
    }

    /// <summary>The open burn step's editor, sliced out of the page the same way #838's and #926's guards
    /// slice it, so a button somewhere else on the map can never answer for this panel.</summary>
    private static string BurnEditorMarkup()
    {
        string razor = Source("Pages", "Map.razor");
        int at = razor.IndexOf("<div class=\"map-plan-step-edit\">", StringComparison.Ordinal);
        Assert.True(at > 0, "Map.razor no longer has the burn step's editor — this guard cannot see the panel.");
        int end = razor.IndexOf("armed auto-orbit is the terminal step", at, StringComparison.Ordinal);
        Assert.True(end > at, "the burn editor's end landmark moved — this guard cannot bound the panel.");
        return razor[at..end];
    }

    /// <summary>
    /// One method's WHOLE body — signature to its matching closing brace, or to the semicolon of an
    /// expression-bodied member. Deliberately NOT the "signature to the first blank line" slice #838's
    /// guards use: <c>ApplyPulses</c> has a paragraph break in the middle of it, and a slicer that stopped
    /// there would end before the assignment and the re-solve, quietly passing a method that had lost
    /// both. A guard that can only see the top of what it judges is the green test that asserts nothing.
    /// </summary>
    private static string MethodBody(string file, string signature)
    {
        string src = Source("Pages", file).Replace("\r\n", "\n");
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at > 0, $"{file} no longer has `{signature}` — this guard cannot see the act.");

        int open = src.IndexOf('{', at + signature.Length);
        int arrow = src.IndexOf("=>", at + signature.Length, StringComparison.Ordinal);
        if (arrow > 0 && (open < 0 || arrow < open))
        {
            int semi = src.IndexOf(';', arrow);
            Assert.True(semi > arrow, $"`{signature}` is expression-bodied but never ends.");
            return src[at..(semi + 1)];
        }

        Assert.True(open > at, $"`{signature}` has no body this guard can find.");
        int depth = 0;
        for (int i = open; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}' && --depth == 0) return src[at..(i + 1)];
        }

        throw new Xunit.Sdk.XunitException($"`{signature}` has no closing brace this guard can find.");
    }

    /// <summary>Every plot partial, read as one subject.</summary>
    private static string Planner() => string.Concat(
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Plot*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>One <c>map-burn-steps</c> group, by its aria-label — the magnitude row or the time row.</summary>
    private static string StepGroup(string ariaLabel)
    {
        string editor = CodeOnly(BurnEditorMarkup());
        Match m = Regex.Match(
            editor,
            "<div class=\"map-burn-steps\" role=\"group\" aria-label=\"" + Regex.Escape(ariaLabel) + "\".*?</div>",
            RegexOptions.Singleline);
        Assert.True(m.Success, $"the burn editor has no step group labelled \"{ariaLabel}\" — the buttons "
            + "the owner asked for are not on the panel, or they are no longer a group a screen reader can name.");
        return m.Value;
    }

    // ── GUARD (d) · THE STEPS ARE REAL BUTTONS, FOUR TO A NUMBER, WEARING CORE'S FACES ───────────────

    /// <summary>
    /// LAW ONE — the burn's magnitude has four real buttons beside its field: coarse down, fine down, the
    /// input, fine up, coarse up. Mouse-only, like the ±5° pair — the owner's complaint was precisely that
    /// he had to TYPE and then leave the field, so a keyboard affordance would not answer it at all.
    ///
    /// <para>RED PROOF: delete either coarse button and the count assertion fails; wire one to
    /// <c>SetPulses</c> instead of <c>NudgeNodePulses</c> and the handler assertion fails; hard-code a face
    /// ("+5 p") in the panel instead of reading <c>NodeFrame</c> and the "one place" assertion fails.</para>
    /// </summary>
    [Fact]
    public void TheMagnitudeHasFourRealStepButtons_AndTheTypedFieldStaysBetweenThem()
    {
        string group = StepGroup("Step this burn's size in pulses");

        Assert.Equal(4, Regex.Matches(group, "<button", RegexOptions.IgnoreCase).Count);
        foreach (string press in new[]
                 {
                     "NudgeNodePulses(node, -1, true)", "NudgeNodePulses(node, -1, false)",
                     "NudgeNodePulses(node, 1, false)", "NudgeNodePulses(node, 1, true)",
                 })
        {
            Assert.Contains(press, group);
        }

        // The typed field is the exception the ruling keeps — and it sits IN this group, between its steps.
        Assert.Contains("map-plot-pulses", group);
        Assert.Contains("SetPulses(node, e)", group);
        int firstButton = group.IndexOf("<button", StringComparison.Ordinal);
        int input = group.IndexOf("map-plot-pulses", StringComparison.Ordinal);
        int lastButton = group.LastIndexOf("<button", StringComparison.Ordinal);
        Assert.True(firstButton < input && input < lastButton,
            "the pulses field is not between its four steps — the row reads as an input with buttons "
            + "bolted on one end instead of a number in the middle of its own range.");

        // Every face and hint comes from Core: the step lives once, so button and act cannot drift.
        Assert.Equal(4, Regex.Matches(group, Regex.Escape("NodeFrame.NudgeMagnitudeLabel(")).Count);
        Assert.Equal(4, Regex.Matches(group, Regex.Escape("NodeFrame.NudgeMagnitudeHint(")).Count);
        Assert.DoesNotContain(" p<", group);      // no hand-typed face beside Core's
    }

    /// <summary>
    /// LAW TWO — and the node's time has its own four, an hour and a day either side of a readout that
    /// says when the burn fires, so the effect of the press is visible in the same glance.
    ///
    /// <para>RED PROOF: drop the readout and the "the row names its subject" assertion fails; wire a time
    /// button to <c>RetimeToScrub</c> (which would silently drag the node to the scrub bar instead of
    /// stepping it) and the handler assertion fails.</para>
    /// </summary>
    [Fact]
    public void TheNodesTimeHasFourRealStepButtons_AroundAReadoutOfWhenItFires()
    {
        string group = StepGroup("Step this burn's time along the course");

        Assert.Equal(4, Regex.Matches(group, "<button", RegexOptions.IgnoreCase).Count);
        foreach (string press in new[]
                 {
                     "NudgeNodeEpoch(node, -1, true)", "NudgeNodeEpoch(node, -1, false)",
                     "NudgeNodeEpoch(node, 1, false)", "NudgeNodeEpoch(node, 1, true)",
                 })
        {
            Assert.Contains(press, group);
        }

        Assert.DoesNotContain("RetimeToScrub", group);
        Assert.Contains("FormatSimTime(node.SimTime)", group);   // the row names the thing it moves
        Assert.Contains("map-burn-epoch-readout", group);

        Assert.Equal(4, Regex.Matches(group, Regex.Escape("NodeFrame.NudgeEpochLabel(")).Count);
        Assert.Equal(4, Regex.Matches(group, Regex.Escape("NodeFrame.NudgeEpochHint(")).Count);
    }

    // ── GUARD (a) · ONE SOLVE PATH — THE PRESS TAKES THE ROAD THE TYPED VALUE TAKES ──────────────────

    /// <summary>
    /// LAW THREE — a magnitude press and a typed magnitude are ONE act. Both funnel through
    /// <c>ApplyPulses</c>, which is the only place in the planner that writes <c>node.Pulses</c>, and it
    /// clamps, checks the reaction-mass budget and re-solves. That is what makes the button honest: it can
    /// never reach a magnitude the field would have refused, and there is no second solve path to drift.
    ///
    /// <para>RED PROOF, three ways: give <c>NudgeNodePulses</c> its own body that assigns
    /// <c>node.Pulses</c> and calls <c>RebuildPlan()</c> and the "one writer" assertion fails naming the
    /// second one; drop <c>ReprojectTrajectory()</c> from <c>ApplyPulses</c> and the re-solve assertion
    /// fails — and that is exactly the bug the owner reported, a number changed with nothing on screen
    /// moving; drop the budget check and the mass assertion fails.</para>
    /// </summary>
    [Fact]
    public void AMagnitudePressTakesTheSameRoadAsATypedNumber()
    {
        string nudge = CodeOnly(MethodBody("Map.Plot.Nodes.cs",
            "private void NudgeNodePulses(PlanNode node, int sign, bool coarse)"));
        string typed = CodeOnly(MethodBody("Map.Plot.Nodes.cs",
            "private void SetPulses(PlanNode node, ChangeEventArgs e)"));
        string apply = CodeOnly(MethodBody("Map.Plot.Nodes.cs",
            "private void ApplyPulses(PlanNode node, int value)"));

        // Both doors lead to the one act, and the press asks Core for the step.
        Assert.Contains("ApplyPulses(node,", nudge);
        Assert.Contains("NodeFrame.NudgeMagnitude(node.Pulses, sign, coarse, MinNodePulses, MaxNodePulses)", nudge);
        Assert.Contains("ApplyPulses(node, value)", typed);

        // Neither door does the work itself.
        foreach (string forbidden in new[] { "node.Pulses =", "RebuildPlan(", "ReprojectTrajectory(" })
        {
            Assert.DoesNotContain(forbidden, nudge);
            Assert.DoesNotContain(forbidden, typed);
        }

        // The one act clamps, pays, and re-solves.
        Assert.Contains("Math.Clamp(value, MinNodePulses, MaxNodePulses)", apply);
        Assert.Contains("_reactionMassPulses", apply);
        Assert.Contains("node.Pulses = value", apply);
        Assert.Contains("RebuildPlan()", apply);
        Assert.Contains("ReprojectTrajectory()", apply);

        // And it really is the ONLY writer of a node's magnitude in the whole planner: exactly one
        // assignment to node.Pulses across every Map.Plot*.cs partial.
        string planner = CodeOnly(Planner());
        int writers = Regex.Matches(planner, @"node\.Pulses\s*=[^=]").Count;
        Assert.True(writers == 1,
            $"{writers} places in the planner write a node's magnitude — ApplyPulses is supposed to be "
            + "the only one, so the clamp, the reaction-mass budget and the re-solve cannot be skipped.");
    }

    /// <summary>
    /// LAW FOUR — an epoch press re-solves too, and it asks Core for the step and the floor rather than
    /// doing its own arithmetic. It re-sorts, because a burn stepped past its neighbour is a legal plan and
    /// the plan is ordered by time.
    ///
    /// <para>RED PROOF: drop <c>ReprojectTrajectory()</c> and the ribbon stops moving under the press —
    /// the exact silence #937 exists to end — and this fails; inline <c>node.SimTime += 3600</c> instead of
    /// calling <c>NodeFrame.NudgeEpoch</c> and the delegation assertion fails, taking the floor with
    /// it.</para>
    /// </summary>
    [Fact]
    public void AnEpochPressReSolves_AndAsksCoreForItsStepAndItsFloor()
    {
        string body = CodeOnly(MethodBody("Map.Plot.Nodes.cs",
            "private void NudgeNodeEpoch(PlanNode node, int sign, bool coarse)"));

        Assert.Contains("NodeFrame.NudgeEpoch(node.SimTime, sign, coarse, NodeEpochFloor())", body);
        Assert.Contains("SortNodes()", body);
        Assert.Contains("RebuildPlan()", body);
        Assert.Contains("ReprojectTrajectory()", body);
        Assert.DoesNotContain("return;", body);        // nothing short-circuits the re-solve
        Assert.DoesNotContain("3600", body);           // the step is Core's, not a number typed here
        Assert.DoesNotContain("86400", body);
    }

    // ── GUARD (b), client half · THE FLOOR IS ONE LAW, NOT THREE COPIES ──────────────────────────────

    /// <summary>
    /// LAW FIVE — "no earlier than one minute out from now" is written once, in <c>NodeEpochFloor</c>, and
    /// the three paths that time a node all read it: adding a burn at the scrub, the re-time button, and
    /// the epoch steps. Three copies of a constant is how the fourth caller ends up with a different rule.
    ///
    /// <para>RED PROOF: re-inline <c>Math.Floor(_ship.SimTime) + 60</c> into any of the three and the
    /// "one floor" count fails, naming how many copies it found.</para>
    /// </summary>
    [Fact]
    public void TheEpochFloorIsWrittenOnce_AndEveryNodeTimingPathReadsIt()
    {
        string nodes = CodeOnly(Source("Pages", "Map.Plot.Nodes.cs"));

        Assert.Contains("private double NodeEpochFloor() => Math.Floor(_ship.SimTime) + 60;", nodes);

        // Exactly one place computes it — the declaration — and everywhere else calls it.
        int copies = Regex.Matches(nodes, Regex.Escape("Math.Floor(_ship.SimTime) + 60")).Count;
        Assert.True(copies == 1,
            $"the one-minute-out floor is written out {copies} times in Map.Plot.Nodes.cs — copies are how "
            + "the next caller ends up with a different rule about when a burn may be scheduled.");
        foreach (string caller in new[]
                 {
                     "private void AddBurnAtScrub()",
                     "private void RetimeToScrub(PlanNode node)",
                     "private void NudgeNodeEpoch(PlanNode node, int sign, bool coarse)",
                 })
        {
            Assert.Contains("NodeEpochFloor()", CodeOnly(MethodBody("Map.Plot.Nodes.cs", caller)));
        }
    }

    // ── GUARD (c) · THE FACES CARRY UNITS, AND NO BARE ± COMES BACK ──────────────────────────────────

    /// <summary>
    /// LAW SIX — #916's deletion still stands. Eight new signed buttons went onto this panel and not one
    /// of them is a bare plus-or-minus: every face is Core's, and every one of Core's carries a unit
    /// (<c>p</c>, <c>h</c>, <c>d</c>). This is the guard that keeps this feature and #838's
    /// <c>ThePlanner_OffersNoPlusMinusControl</c> agreeing instead of fighting.
    ///
    /// <para>RED PROOF: plant a literal <c>±</c> or a bare <c>&gt;+&lt;</c> button on the panel and this
    /// fails — as does #838's own guard, which is the point.</para>
    /// </summary>
    [Fact]
    public void TheStepButtonsBringBackNoBarePlusMinus()
    {
        string editor = CodeOnly(BurnEditorMarkup());

        Assert.DoesNotContain("±", editor);
        Assert.DoesNotContain(">+<", editor);
        Assert.DoesNotContain(">-<", editor);
        Assert.DoesNotContain("&minus;", editor);

        // No face is typed into the page at all: all twelve signed faces on this panel (the four ±5°
        // pair's two, the four magnitude steps, the four time steps) are read out of Core.
        foreach (string coreFace in new[]
                 {
                     "NodeFrame.NudgeLabel(", "NodeFrame.NudgeMagnitudeLabel(", "NodeFrame.NudgeEpochLabel(",
                 })
        {
            Assert.Contains(coreFace, editor);
        }
        Assert.Equal(10, Regex.Matches(editor, Regex.Escape("NodeFrame.NudgeMagnitudeLabel("))
                              .Count
                          + Regex.Matches(editor, Regex.Escape("NodeFrame.NudgeEpochLabel(")).Count
                          + Regex.Matches(editor, Regex.Escape("NodeFrame.NudgeLabel(")).Count);
    }

    // ── GUARD (e) · THE PANEL STILL FITS, AND BOTH PAIRS SURVIVE A 390px PHONE ───────────────────────

    /// <summary>
    /// LAW SEVEN — #782's law is MEASURE the fit. #838 measured the open step editor at a 32rem panel as
    /// 28.6rem usable, i.e. 3.4rem of card padding + step indent + editor padding; on a 390px phone the
    /// panel's <c>max-width: 100%</c> clamps it to 24.375rem, leaving ~20.9rem. Both step rows are asked
    /// to fit inside that, from the stylesheet's own declared widths — so BOTH pairs survive at phone
    /// width and neither collapses to the fine step alone.
    ///
    /// <para>RED PROOF: widen <c>.map-burn-steps .btn</c> to 4.4rem (the ±5° pair's width) and the time
    /// row comes to 22.4rem — this fails naming the row and the number. Widen
    /// <c>.map-burn-epoch-readout</c> past ~7.3rem and the same row fails.</para>
    /// </summary>
    [Fact]
    public void BothStepPairsFitA390PixelPhone()
    {
        string css = Source("Pages", "Map.razor.css");

        double panelRem = Rem(css, @"\.map-plot\s*\{[^}]*?width:\s*([0-9.]+)rem");
        Assert.True(panelRem >= 36, $"the plotting panel is {panelRem}rem — #838's guard already says that "
            + "is too narrow, and these rows were measured against 38rem.");

        double button = Rem(css, @"\.map-burn-steps \.btn\s*\{[^}]*?min-width:\s*([0-9.]+)rem");
        double gap = Rem(css, @"\.map-burn-steps\s*\{[^}]*?gap:\s*([0-9.]+)rem");
        double pulses = Rem(css, @"\.map-plot-pulses\s*\{[^}]*?width:\s*([0-9.]+)rem");
        double readout = Rem(css, @"\.map-burn-epoch-readout\s*\{[^}]*?min-width:\s*([0-9.]+)rem");

        // 390px at a 16px root, less the 3.4rem of chrome #838 measured between panel edge and editor.
        const double usableAt390 = 390.0 / 16.0 - 3.4;

        double magnitudeRow = 4 * button + pulses + 4 * gap;
        double timeRow = 4 * button + readout + 4 * gap;

        Assert.True(magnitudeRow <= usableAt390,
            $"the magnitude row asks for {magnitudeRow:0.00}rem of the {usableAt390:0.00}rem an open step "
            + "editor has at 390px — it would break onto a second line or push the panel past the viewport.");
        Assert.True(timeRow <= usableAt390,
            $"the time row asks for {timeRow:0.00}rem of the {usableAt390:0.00}rem an open step editor has "
            + "at 390px — it would break onto a second line or push the panel past the viewport.");

        // Both rows also declare wrap as the backstop, so a future face that outgrows its min-width breaks
        // the line rather than the panel (#253's viewport clamp).
        Match row = Regex.Match(css, @"\.map-burn-steps\s*\{[^}]*\}", RegexOptions.Singleline);
        Assert.True(row.Success, "the step rows have no layout of their own.");
        Assert.Contains("flex-wrap: wrap", row.Value);
    }

    private static double Rem(string css, string pattern)
    {
        Match m = Regex.Match(css, pattern, RegexOptions.Singleline);
        Assert.True(m.Success, $"the stylesheet no longer declares `{pattern}` — this guard cannot measure the fit.");
        return double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    // ── #905's ledger · no new Map field ─────────────────────────────────────────────────────────────

    /// <summary>
    /// LAW EIGHT — the steps invented no state. A press writes the node it was handed and re-solves; there
    /// is no "last step size", no "pending magnitude", nothing that can go stale against the plan. The
    /// coarse/fine choice is which BUTTON was pressed, carried as an argument.
    ///
    /// <para>RED PROOF: add a <c>_coarseSteps</c> or <c>_lastNudged</c> field to any plot partial and this
    /// fails naming it.</para>
    /// </summary>
    [Fact]
    public void TheStepsInventedNoMapField()
    {
        string planner = CodeOnly(Planner());
        foreach (string invented in new[]
                 { "_coarseStep", "_coarseSteps", "_stepSize", "_lastNudged", "_pendingPulses", "_pendingEpoch" })
        {
            Assert.DoesNotContain(invented, planner);
        }
    }
}
