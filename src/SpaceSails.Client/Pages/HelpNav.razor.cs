using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// HelpNav — the code-behind for HelpNav.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of HelpNav.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class HelpNav
{
    // ── THE SKETCHES ───────────────────────────────────────────────────────────────────────────────
    //
    // Deliberately crude, deliberately inline, deliberately NOT a screenshot. Each one draws the SHAPE
    // a new captain is looking for on the real panel — a slider with a handle, a row of small buttons
    // with units on them, a badge at the right end of a step — at a size that reads on a phone.
    //
    // The dark plate matches the game's own plot panel so the shapes are recognisable when the player
    // switches tabs, and every glyph is drawn with an explicit fill so no theme can wash it out.

    private const string Plate = "#1b1f27";      // the plot panel's dark ground
    private const string Ink = "#e9ecef";        // body text on the plate
    private const string Dim = "#8d97a5";        // secondary text
    private const string Info = "#4dd0e1";       // the ± button ink (btn-outline-info)
    private const string Good = "#2fbf71";       // ✓ VALID
    private const string Bad = "#e0555f";        // ✗ INVALID
    private const string Warn = "#e8b84b";       // the dock/arm accent

    /// <summary>Opening tag for every sketch: one viewBox, scaled to its column, labelled for screen
    /// readers by the caller's own heading.</summary>
    private static MarkupString Frame(string title, string body) => new(
        $"""
        <svg viewBox="0 0 320 120" role="img" aria-label="{title}" class="w-100 help-nav-sketch"
             style="max-width:320px;height:auto;">
          <rect x="0" y="0" width="320" height="120" rx="8" fill="{Plate}"/>
          {body}
        </svg>
        """);

    /// <summary>A small pill button face — the repeated shape in four of the eight sketches.</summary>
    private static string Pill(double x, double y, double w, string face, string ink) =>
        $"""
        <rect x="{x}" y="{y}" width="{w}" height="18" rx="4" fill="none" stroke="{ink}" stroke-width="1"/>
        <text x="{x + w / 2}" y="{y + 13}" fill="{ink}" font-size="10" font-family="monospace"
              text-anchor="middle">{face}</text>
        """;

    private MarkupString SketchTarget() => Frame("A planet on the map, clicked, with a destination reticle on it",
        $"""
        <circle cx="70" cy="60" r="4" fill="{Ink}"/>
        <text x="70" y="82" fill="{Dim}" font-size="10" text-anchor="middle">ship</text>
        <circle cx="235" cy="48" r="9" fill="{Warn}"/>
        <circle cx="235" cy="48" r="17" fill="none" stroke="{Info}" stroke-width="1.5" stroke-dasharray="4 3"/>
        <line x1="235" y1="24" x2="235" y2="31" stroke="{Info}" stroke-width="1.5"/>
        <line x1="235" y1="65" x2="235" y2="72" stroke="{Info}" stroke-width="1.5"/>
        <text x="235" y="90" fill="{Ink}" font-size="10" text-anchor="middle">Mars — destination</text>
        <path d="M78 58 Q150 30 214 46" fill="none" stroke="{Dim}" stroke-width="1" stroke-dasharray="3 4"/>
        """);

    private MarkupString SketchToolbar() => Frame("The Nav toolbar, with the amber Plot button standing out on it",
        $"""
        <rect x="14" y="42" width="292" height="36" rx="6" fill="#11151b"/>
        {Pill(24, 51, 52, "Follow", Dim)}
        {Pill(82, 51, 24, "&#128269;+", Dim)}
        <rect x="114" y="49" width="64" height="22" rx="4" fill="{Warn}"/>
        <text x="146" y="64" fill="#11151b" font-size="11" font-family="monospace" text-anchor="middle">&#128506; Plot</text>
        {Pill(186, 51, 22, "?", Dim)}
        <text x="216" y="64" fill="{Dim}" font-size="10" font-family="monospace">warp  1×</text>
        <text x="24" y="30" fill="{Ink}" font-size="11">Nav desk toolbar</text>
        <text x="24" y="98" fill="{Dim}" font-size="10">Plot is the loud one — press it and the sim pauses</text>
        """);

    private MarkupString SketchScrub() => Frame("The Scrub slider with its handle dragged to the right",
        $"""
        <text x="16" y="26" fill="{Ink}" font-size="11" font-family="monospace">Scrub: day 143</text>
        <line x1="16" y1="48" x2="304" y2="48" stroke="{Dim}" stroke-width="3" stroke-linecap="round"/>
        <line x1="16" y1="48" x2="212" y2="48" stroke="{Info}" stroke-width="3" stroke-linecap="round"/>
        <circle cx="212" cy="48" r="8" fill="{Info}"/>
        <text x="16" y="70" fill="{Dim}" font-size="9">now</text>
        <text x="304" y="70" fill="{Dim}" font-size="9" text-anchor="end">+2 y</text>
        <circle cx="60" cy="96" r="4" fill="{Ink}"/>
        <circle cx="150" cy="96" r="4" fill="none" stroke="{Dim}" stroke-dasharray="2 2"/>
        <circle cx="240" cy="96" r="4" fill="none" stroke="{Dim}" stroke-dasharray="2 2"/>
        <text x="150" y="112" fill="{Dim}" font-size="9" text-anchor="middle">ghosts at the scrubbed time</text>
        """);

    private MarkupString SketchAddBurn() => Frame("The + Add burn at scrub button and the step it appends",
        $"""
        {Pill(16, 14, 128, "+ Add burn at scrub", Ink)}
        <rect x="16" y="46" width="288" height="26" rx="4" fill="#11151b"/>
        <circle cx="28" cy="59" r="4" fill="{Info}"/>
        <text x="40" y="63" fill="{Ink}" font-size="10" font-family="monospace">&#9656; FORWARD &#215;6</text>
        <text x="296" y="63" fill="{Dim}" font-size="10" font-family="monospace" text-anchor="end">day 143</text>
        {Pill(16, 80, 60, "&#9654; FWD", Info)}
        {Pill(80, 80, 60, "&#9664; BACK", Dim)}
        {Pill(144, 80, 60, "&#9650; UP", Dim)}
        {Pill(208, 80, 60, "&#9660; DOWN", Dim)}
        """);

    private MarkupString SketchIterate() => Frame("A burn row's three pairs of increment and decrement buttons",
        $"""
        <text x="16" y="20" fill="{Dim}" font-size="9">aim</text>
        {Pill(52, 8, 42, NodeFrame.NudgeLabel(-1), Info)}
        {Pill(98, 8, 42, NodeFrame.NudgeLabel(1), Info)}
        <text x="16" y="52" fill="{Dim}" font-size="9">size</text>
        {Pill(52, 40, 42, NodeFrame.NudgeMagnitudeLabel(-1, true), Info)}
        {Pill(98, 40, 42, NodeFrame.NudgeMagnitudeLabel(-1, false), Info)}
        <rect x="144" y="40" width="34" height="18" rx="4" fill="#11151b" stroke="{Dim}"/>
        <text x="161" y="53" fill="{Ink}" font-size="10" font-family="monospace" text-anchor="middle">6</text>
        {Pill(182, 40, 42, NodeFrame.NudgeMagnitudeLabel(1, false), Info)}
        {Pill(228, 40, 42, NodeFrame.NudgeMagnitudeLabel(1, true), Info)}
        <text x="16" y="84" fill="{Dim}" font-size="9">when</text>
        {Pill(52, 72, 42, NodeFrame.NudgeEpochLabel(-1, true), Info)}
        {Pill(98, 72, 42, NodeFrame.NudgeEpochLabel(-1, false), Info)}
        <text x="161" y="85" fill="{Ink}" font-size="10" font-family="monospace" text-anchor="middle">d143</text>
        {Pill(182, 72, 42, NodeFrame.NudgeEpochLabel(1, false), Info)}
        {Pill(228, 72, 42, NodeFrame.NudgeEpochLabel(1, true), Info)}
        <text x="16" y="110" fill="{Info}" font-size="9">the course re-solves under every press</text>
        """);

    private MarkupString SketchArrive() => Frame("The arrive step, once invalid and once valid",
        $"""
        {Pill(16, 8, 132, "&#128752; + Add orbit at scrub", Info)}
        {Pill(154, 8, 132, "&#9875; + Add dock at scrub", Warn)}
        <rect x="16" y="38" width="288" height="26" rx="4" fill="#11151b" stroke="{Bad}"/>
        <circle cx="28" cy="51" r="4" fill="{Bad}"/>
        <text x="40" y="55" fill="{Ink}" font-size="10" font-family="monospace">orbit at Mars</text>
        <rect x="232" y="43" width="62" height="16" rx="4" fill="{Bad}"/>
        <text x="263" y="55" fill="#11151b" font-size="9" font-family="monospace" text-anchor="middle">&#10007; INVALID</text>
        <text x="40" y="78" fill="{Bad}" font-size="9" font-family="monospace">pass 3.60 M km, need &#8804; 3.00 M km</text>
        <rect x="16" y="86" width="288" height="26" rx="4" fill="#11151b" stroke="{Good}"/>
        <circle cx="28" cy="99" r="4" fill="{Good}"/>
        <text x="40" y="103" fill="{Ink}" font-size="10" font-family="monospace">orbit at Mars</text>
        <rect x="240" y="91" width="54" height="16" rx="4" fill="{Good}"/>
        <text x="267" y="103" fill="#11151b" font-size="9" font-family="monospace" text-anchor="middle">&#10003; VALID</text>
        """);

    private MarkupString SketchArm() => Frame("The arrive step's editor, with the Arm button in it",
        $"""
        <rect x="16" y="12" width="288" height="24" rx="4" fill="#11151b"/>
        <text x="30" y="28" fill="{Ink}" font-size="10" font-family="monospace">&#9662; orbit at Mars</text>
        <rect x="240" y="16" width="54" height="16" rx="4" fill="{Good}"/>
        <text x="267" y="28" fill="#11151b" font-size="9" font-family="monospace" text-anchor="middle">&#10003; VALID</text>
        <rect x="24" y="46" width="272" height="22" rx="4" fill="{Good}"/>
        <text x="160" y="61" fill="#11151b" font-size="10" font-family="monospace" text-anchor="middle">&#128752; Arm the arrival at Mars</text>
        <text x="160" y="90" fill="{Good}" font-size="9" text-anchor="middle">&#8220;the plan is complete&#8221;</text>
        {Pill(24, 100, 176, "scrub to it", Info)}
        {Pill(208, 100, 88, "&#10006; remove", Bad)}
        """);

    private MarkupString SketchWarp() => Frame("The warp slider pushed up, and a burn firing on the ribbon",
        $"""
        <text x="16" y="24" fill="{Dim}" font-size="10" font-family="monospace">warp</text>
        <line x1="56" y1="20" x2="256" y2="20" stroke="{Dim}" stroke-width="3" stroke-linecap="round"/>
        <line x1="56" y1="20" x2="206" y2="20" stroke="{Warn}" stroke-width="3" stroke-linecap="round"/>
        <circle cx="206" cy="20" r="7" fill="{Warn}"/>
        <text x="266" y="24" fill="{Ink}" font-size="10" font-family="monospace">1000&#215;</text>
        <path d="M28 96 Q120 40 292 62" fill="none" stroke="{Info}" stroke-width="2"/>
        <circle cx="28" cy="96" r="4" fill="{Ink}"/>
        <circle cx="120" cy="61" r="4" fill="{Warn}"/>
        <text x="120" y="52" fill="{Warn}" font-size="9" text-anchor="middle">burn fires</text>
        <circle cx="292" cy="62" r="8" fill="{Good}"/>
        <text x="286" y="86" fill="{Ink}" font-size="10" text-anchor="end">in orbit</text>
        """);
}
