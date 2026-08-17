using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #926 · THE PLANNER NAMES ITS FRAME — and offers the trip's. Owner (2026-08-17, playing #916's vector
/// planner): <i>"the real thrust amounts are dependent on the coordinate origin. I had to remember to
/// switch to Sun to get the ship to really start moving from Earth towards Mars."</i>
///
/// <para>Plus the owner's addition the same evening: <i>"Let's add the plus and minus buttons to the burn
/// scrub angle … the vector rotation is good for flying with mouse alone, without inputting … like ±5
/// degrees."</i></para>
///
/// <h3>Why these guards read SOURCE, and where they stop</h3>
/// <para>WHEN the offer stands, and WHICH frame it names, are arithmetic — they live in Core and Core's
/// <c>TheTripHasItsOwnFrameTests</c> flies them against the real <c>sol.json</c>, with the offer appearing
/// and standing down on demand. What no Core test can see is the SHAPE of the panel: that the reading
/// frame is named unconditionally, that the offer is rendered behind that Core decision and behind
/// nothing else, that its one button touches only the plot frame, and that the nudges are real buttons a
/// mouse can reach. Those four are what this file holds, and each names what it would catch.</para>
/// </summary>
public sealed class ThePlannerNamesItsFrameTests
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

    /// <summary>The source with its comments taken out — a guard about what the panel DOES must never be
    /// answered by prose SAYING it does.</summary>
    private static string CodeOnly(string source)
    {
        source = Regex.Replace(source, @"@\*.*?\*@", " ", RegexOptions.Singleline);
        source = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(source, @"^\s*//.*$", " ", RegexOptions.Multiline);
    }

    /// <summary>The open burn step's editor, sliced out of the page the same way #838's guards slice it,
    /// so a frame chip somewhere else on the map can never answer for this panel.</summary>
    private static string BurnEditorMarkup()
    {
        string razor = Source("Pages", "Map.razor");
        int at = razor.IndexOf("<div class=\"map-plan-step-edit\">", StringComparison.Ordinal);
        Assert.True(at > 0, "Map.razor no longer has the burn step's editor — this guard cannot see the panel.");
        int end = razor.IndexOf("armed auto-orbit is the terminal step", at, StringComparison.Ordinal);
        Assert.True(end > at, "the burn editor's end landmark moved — this guard cannot bound the panel.");
        return razor[at..end];
    }

    /// <summary>Every plot partial, read as one subject.</summary>
    private static string Planner() => string.Concat(
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Plot*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>One method's body, signature to the first blank line at its own indent.</summary>
    private static string MethodBody(string file, string signature)
    {
        string src = Source("Pages", file).Replace("\r\n", "\n");
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at > 0, $"{file} no longer has `{signature}` — this guard cannot see the act.");
        int end = src.IndexOf("\n\n", at, StringComparison.Ordinal);
        Assert.True(end > at, $"`{signature}` has no end this guard can find.");
        return src[at..end];
    }

    // ── GUARD (d) · THE READING FRAME IS NAMED, ALWAYS ──────────────────────────────────────────────

    /// <summary>
    /// LAW ONE — the panel names the frame it is reading the plan in every time it is open, not only when
    /// something is wrong with it. The owner's whole complaint was that nothing on screen said which
    /// origin the numbers were quoted about, so a note that appears only alongside the offer would leave
    /// the silence exactly where it was for every plan that has no destination.
    ///
    /// <para>RED PROOF, two ways: delete <c>TripFrame.ReadingNote(PlotFrameName())</c> from the frame note
    /// and the first assertion fails; move it inside the <c>@if</c> that renders the offer and the
    /// ordering assertion fails, naming both positions.</para>
    /// </summary>
    [Fact]
    public void ThePanelNamesTheFrameItIsReadingIn_Unconditionally()
    {
        string editor = CodeOnly(BurnEditorMarkup());

        Assert.Contains("TripFrame.ReadingNote(PlotFrameName())", editor);
        Assert.Contains("NodeFrame.FrameNote(", editor);      // #916's half of the sentence stands too

        int note = editor.IndexOf("TripFrame.ReadingNote(", StringComparison.Ordinal);
        int firstIf = editor.IndexOf("@if (", StringComparison.Ordinal);
        Assert.True(firstIf > 0, "the burn editor has no conditional at all — this guard cannot tell "
            + "unconditional markup from conditional markup any more.");
        Assert.True(note < firstIf,
            $"the reading-frame note sits at {note}, after the panel's first conditional at {firstIf} — "
            + "it is only shown some of the time, which is the silence #926 exists to end.");

        // And the name itself is derived from the one truth, never stored.
        string frame = MethodBody("Map.Plot.Frame.cs", "private string PlotFrameName()");
        Assert.Contains("_plotFrameBodyId", frame);
        Assert.Contains("\"Sun\"", frame);
    }

    // ── GUARD (b), client half · THE OFFER IS RENDERED BEHIND CORE'S DECISION ───────────────────────

    /// <summary>
    /// LAW TWO — the line and the button are rendered behind <c>TripFrameOffer</c> and nothing else, and
    /// <c>TripFrameOffer</c> asks Core. That is what makes Core's behaviour guard (the offer stands only
    /// with a destination, and only while the frames differ) govern what the player actually sees.
    ///
    /// <para>RED PROOF: render the line unconditionally and the "inside the conditional" assertion fails;
    /// re-implement the decision in the panel with its own <c>if</c> instead of calling
    /// <c>TripFrame.At</c> and the delegation assertion fails — and the offer would be free to drift from
    /// the law Core proves.</para>
    /// </summary>
    [Fact]
    public void TheOfferIsRenderedOnlyBehindCoresDecision()
    {
        string editor = CodeOnly(BurnEditorMarkup());

        int gate = editor.IndexOf("@if (TripFrameOffer(node.SimTime) is", StringComparison.Ordinal);
        Assert.True(gate > 0, "the trip-frame offer is no longer gated on TripFrameOffer(node.SimTime).");
        Assert.True(editor.IndexOf("TripFrame.OfferLine(", StringComparison.Ordinal) > gate,
            "the offer's line is rendered outside its gate — it would accuse the captain of a frame he is "
            + "already in, and of a trip he has not set.");
        Assert.True(editor.IndexOf("TripFrame.OfferButton(", StringComparison.Ordinal) > gate,
            "the offer's button is rendered outside its gate.");

        // The decision itself is Core's, asked with the two pieces of state that already exist.
        string offer = MethodBody("Map.Plot.Frame.cs",
            "private (string? FrameId, string FrameName, string DestinationName)? TripFrameOffer(double simTime)");
        Assert.Contains("TripFrame.At(", offer);
        Assert.Contains("_destinationBodyId", offer);
        Assert.Contains("_plotFrameBodyId", offer);

        // #905's frame ledger: no new Map field. The offer is a QUESTION asked as the panel draws, derived
        // from _plotFrameBodyId + _destinationBodyId, so there is no third copy of the frame to go stale.
        string plot = CodeOnly(Planner());
        foreach (string invented in new[] { "_tripFrame", "_tripFrameOffer", "_frameOffer", "_offeredFrame" })
        {
            Assert.DoesNotContain(invented, plot);
        }
    }

    // ── GUARD (c) · THE PRESS MOVES THE FRAME AND NOTHING ELSE ─────────────────────────────────────

    /// <summary>
    /// LAW THREE — pressing the offer changes the plot frame, full stop. The quick selects go on aiming in
    /// the NODE's frame (#916) whichever origin the map is drawn about — an escape burn IS Earth-prograde
    /// — so a burn's stored heading must be byte-identical before and after the press.
    ///
    /// <para>(What may legitimately change is which button is LIT: the frame is also what up/down are
    /// measured from, so a node aimed UP at Earth is no longer "up" once the plan is read heliocentric.
    /// The stored aim does not move; only the reading of it does.)</para>
    ///
    /// <para>RED PROOF: add a re-aim to <c>SetPlotFrame</c> — <c>AimNode(node, …)</c>, a
    /// <c>HeadingDegrees</c> assignment, a <c>RebuildPlan()</c> — and this fails naming the line. That is
    /// the exact edit a well-meaning "keep the burn pointed the same way on screen" would make, and it
    /// would silently re-plan the captain's course under him.</para>
    /// </summary>
    [Fact]
    public void PressingTheOffer_MovesOnlyThePlotFrame()
    {
        string set = CodeOnly(MethodBody("Map.Plot.Frame.cs", "private void SetPlotFrame(string? bodyId)"));

        Assert.Contains("_plotFrameBodyId = bodyId", set);
        foreach (string forbidden in new[]
                 { "HeadingDegrees", "AimNode(", "SetNodeDirection(", "NodeFrame.", "RebuildPlan(", "_planNodes" })
        {
            Assert.DoesNotContain(forbidden, set);
        }

        // The button's only handler is that method, with the offered frame and nothing else.
        string editor = CodeOnly(BurnEditorMarkup());
        Assert.Contains("SetPlotFrame(_offer.FrameId)", editor);
    }

    // ── The owner's addition · five-degree nudges, for the mouse alone ─────────────────────────────

    /// <summary>
    /// LAW FOUR — the two nudges are real buttons on the panel, one per sign, and they turn the aim by
    /// Core's one constant. Owner: <i>"the vector rotation is good for flying with mouse alone, without
    /// inputting"</i> — so a keyboard-only affordance would not answer the request at all.
    ///
    /// <para>RED PROOF: drop either button and the count assertion fails; wire one to the free-aim field
    /// instead of <c>NudgeNodeAim</c> and the handler assertion fails; hard-code 5 in the panel instead of
    /// reading <c>NodeFrame</c> and the "one place" assertion fails.</para>
    /// </summary>
    [Fact]
    public void TheFiveDegreeNudgesAreTwoRealButtonsAMouseCanReach()
    {
        string editor = CodeOnly(BurnEditorMarkup());

        Assert.Contains("NudgeNodeAim(node, -1)", editor);
        Assert.Contains("NudgeNodeAim(node, 1)", editor);
        Assert.Equal(2, Regex.Matches(editor, Regex.Escape("NodeFrame.NudgeLabel(")).Count);
        Assert.Equal(2, Regex.Matches(editor, Regex.Escape("NodeFrame.NudgeHint(")).Count);

        // Both live in a real <button>, not a key handler and not a text field.
        Match group = Regex.Match(editor, "<div class=\"map-burn-nudge\".*?</div>", RegexOptions.Singleline);
        Assert.True(group.Success, "the nudge pair has no group of its own on the panel.");
        Assert.Equal(2, Regex.Matches(group.Value, "<button", RegexOptions.IgnoreCase).Count);

        // The number lives once, in Core.
        Assert.DoesNotContain("5°", editor);          // no hand-typed face beside Core's
        string nudge = CodeOnly(MethodBody("Map.Plot.Nodes.cs", "private void NudgeNodeAim(PlanNode node, int sign)"));
        Assert.Contains("NodeFrame.Nudge(node.HeadingDegrees, sign)", nudge);
        Assert.Contains("ReprojectTrajectory()", nudge);   // the ribbon re-solves, as for any heading edit
    }

    /// <summary>
    /// LAW FIVE — and #916's deletion stands: the planner still shows no bare plus-or-minus glyph. The
    /// nudges wear a DEGREE sign because they are an angle, not the reflex-flying factor control that was
    /// sent back to the live keys.
    ///
    /// <para>RED PROOF: label a nudge "±5" or plant a lone "+" button on the panel and this fails — as
    /// does #916's own <c>ThePlanner_OffersNoPlusMinusControl</c>, which is the point: the two guards agree
    /// rather than fight.</para>
    /// </summary>
    [Fact]
    public void TheNudgesDoNotBringBackThePlusMinusIdiom()
    {
        string editor = CodeOnly(BurnEditorMarkup());
        Assert.DoesNotContain("±", editor);
        Assert.DoesNotContain(">+<", editor);
        Assert.DoesNotContain("ToggleBurnMode", editor);
    }

    // ── The words the Guide teaches ────────────────────────────────────────────────────────────────

    /// <summary>
    /// LAW SIX — the Guide's plotting section carries the owner's blessed sentence about frames, and the
    /// nudge clause. RED PROOF: reword either and this fails on the exact text.
    /// </summary>
    [Fact]
    public void TheGuideTeachesTheFrameAndTheNudge()
    {
        string guide = Source("Pages", "Guide.razor");
        Assert.Contains(
            "The frame you plot in decides what 'moving' looks like — a trip is best read in the frame of the body both ends go round.",
            Regex.Replace(guide, @"\s+", " "));
        Assert.Contains("nudge five degrees at a", Regex.Replace(guide, @"\s+", " "));
    }
}
