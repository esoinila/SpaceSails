// Lab 34 — The unclickable lifeline
//
// Teaching voice: every other lab in this house measures physics. This one measures the UI the same
// honest way — because the owner kept getting bitten by it: "the rescue-me button was barely clickable
// when we ran out of power. We should have some kind of test to find such cases during our CI testing.
// Not sure at all how to do that." This lab answers "how to do that."
//
// The rescue affordance — the pill a stranded, out-of-reaction-mass captain presses to whistle for a
// tow — lives at the bottom of a ~15-layer overlay stack (masthead, desk shields, nav panels, dossier,
// modals). Whether it is actually PRESSABLE is emergent geometry: it depends on which layers are up,
// their z-order, their footprints and the viewport size. No one can eyeball that stack and be sure. So
// we compute it: SpaceSails.Core.OverlayLayout turns "is this control reachable?" into a pure-geometry
// query over rectangles + z-indexes, and SpaceSails.Core.RescueLifeline encodes the ACTUAL Map HUD as
// data (every number transcribed from Map.razor.css, line-cited). This probe prints the survey of
// CI-able detection approaches, then the reachability tables the regression gate asserts.
//
// IRONCLAD RULE: every number in labs/34-the-unclickable-lifeline/README.md came from running this
// probe. Change the code and the printed tables go stale — rerun and re-paste, never hand-edit a table.

using SpaceSails.Core;
using static SpaceSails.Core.OverlayLayout;

static string Pad(string s, int w) => s.Length >= w ? s : s + new string(' ', w - s.Length);
static string PadL(string s, int w) => s.Length >= w ? s : new string(' ', w - s.Length) + s;

Console.WriteLine("================================================================================");
Console.WriteLine(" LAB 34 — THE UNCLICKABLE LIFELINE  (issue #293)");
Console.WriteLine(" Reachability of the rescue affordance, measured as geometry — no browser needed.");
Console.WriteLine("================================================================================");

// ---------------------------------------------------------------------------------------------------
// SECTION A — the overlay stack, as the game actually paints it (standard 1280x800 canvas).
// The real Map HUD, transcribed from Map.razor.css. This is the haystack the lifeline hides in.
// ---------------------------------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("A. THE OUT-OF-POWER OVERLAY STACK  (viewport 1280x800, y grows downward)");
Console.WriteLine("   " + Pad("layer", 26) + PadL("z", 6) + "  " + Pad("hit-rect  x, y, w, h (px)", 30) + "pointer-events");
Console.WriteLine("   " + new string('-', 74));

Rect vp = RescueLifeline.Viewport();
Overlay pillLifeline = RescueLifeline.ReopenPill(vp, RescueLifeline.LifelineZIndex);

void PrintOverlay(Overlay o)
{
    Rect r = o.Bounds;
    string rect = $"{r.X:F0}, {r.Y:F0}, {r.W:F0}, {r.H:F0}";
    Console.WriteLine("   " + Pad(o.Name, 26) + PadL(o.ZIndex.ToString(), 6) + "  " + Pad(rect, 30) + (o.PointerEvents ? "auto" : "none"));
}

foreach (Overlay o in RescueLifeline.OutOfPowerOverlays(vp))
{
    PrintOverlay(o);
}
PrintOverlay(pillLifeline);
Console.WriteLine();
Console.WriteLine($"   The lifeline pill sits at z {RescueLifeline.LifelineZIndex} — above every panel above, below the");
Console.WriteLine("   rescue modal (.rescue-backdrop z 1360) it opens. That ordering is the invariant.");

// ---------------------------------------------------------------------------------------------------
// SECTION B — the CI-able detection approaches, and what each can / cannot catch.
// ---------------------------------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("B. HOW COULD CI CATCH 'BARELY CLICKABLE'?  — four approaches weighed");
Console.WriteLine();
(string Approach, string Catches, string Misses, string CiCost)[] approaches =
[
    ("(a) bUnit render-tree",
        "control present/enabled in a state",
        "geometry: overlap, z-order, viewport",
        "HIGH: no bUnit today; +pkg, +Client ref"),
    ("(b) geometry law + registry",
        "occlusion, z-order, off-viewport, tap size",
        "runtime CSS not in the registry (hand-sync)",
        "LOW: pure C# in Core, existing xUnit"),
    ("(c) CSS z-index/overlay audit",
        "raw z-order inversions in the stylesheet",
        "footprints, viewport, which states co-occur",
        "MED: needs a CSS parser in the pipeline"),
    ("(d) Playwright hit-testing",
        "the real truth: elementFromPoint in a browser",
        "nothing — but only the states the script drives",
        "VERY HIGH: browser stage, headless Chromium"),
];
Console.WriteLine("   " + Pad("approach", 30) + Pad("catches", 46) + "CI cost today");
Console.WriteLine("   " + new string('-', 96));
foreach ((string a, string c, string _, string cost) in approaches)
{
    Console.WriteLine("   " + Pad(a, 30) + Pad(c, 46) + cost);
}
Console.WriteLine();
foreach ((string a, string _, string m, string _) in approaches)
{
    Console.WriteLine("   " + Pad(a, 30) + "blind to: " + m);
}

// ---------------------------------------------------------------------------------------------------
// SECTION C — the regression gate's numbers: is the lifeline reachable, at every size?
// ---------------------------------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("C. THE GATE — lifeline reachability in the out-of-power state, per viewport");
Console.WriteLine("   " + Pad("size", 12) + Pad("name", 12) + Pad("verdict", 14) + Pad("free w x h (px)", 18) + "free %");
Console.WriteLine("   " + new string('-', 62));

(double W, double H, string Name)[] sizes =
[
    (1280, 800, "desktop"),
    (1024, 768, "laptop"),
    (390, 844, "phone-tall"),
    (844, 390, "phone-wide"),
    (320, 480, "min-canvas"),
];
foreach ((double w, double h, string name) in sizes)
{
    Rect v = RescueLifeline.Viewport(w, h);
    Overlay pill = RescueLifeline.ReopenPill(v, RescueLifeline.LifelineZIndex);
    ReachResult r = Evaluate(pill, v, RescueLifeline.OutOfPowerOverlays(v));
    string size = $"{w:F0}x{h:F0}";
    string free = $"{r.FreeWidth:F0} x {r.FreeHeight:F0}";
    Console.WriteLine("   " + Pad(size, 12) + Pad(name, 12) + Pad(r.Verdict.ToString(), 14) + Pad(free, 18) + $"{r.FreeFraction * 100:F0}%");
}

// ---------------------------------------------------------------------------------------------------
// SECTION D — the gate has teeth: it FAILS the layout that shipped the original bug, and the reserved
// band beats a hostile pop-up the old z-30 could not.
// ---------------------------------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("D. THE GATE HAS TEETH  (viewport 1280x800)");
Console.WriteLine();

ReachResult buried = Evaluate(RescueLifeline.BuriedStrip(vp), vp, [RescueLifeline.MastheadBand(vp)]);
Console.WriteLine("   pre-#262 buried strip (top:0.75rem, under the masthead z 24):");
Console.WriteLine($"      verdict = {buried.Verdict};  occluded by = [{string.Join(", ", buried.OccludedBy)}]");
Console.WriteLine("      -> the gate, had it existed, would have failed the build on the reported bug.");
Console.WriteLine();

Overlay hostile = RescueLifeline.DeskBandPopupOverBottom(vp);
ReachResult atOld = Evaluate(RescueLifeline.ReopenPill(vp, RescueLifeline.PreLabZIndex), vp, [hostile]);
ReachResult atNew = Evaluate(RescueLifeline.ReopenPill(vp, RescueLifeline.LifelineZIndex), vp, [hostile]);
Console.WriteLine("   a desk-band pop-up (z 1320, pointer-events) lands over the bottom-centre:");
Console.WriteLine($"      pill at z {RescueLifeline.PreLabZIndex} (pre-lab)   -> {atOld.Verdict}");
Console.WriteLine($"      pill at z {RescueLifeline.LifelineZIndex} (reserved) -> {atNew.Verdict}");
Console.WriteLine("      -> the reserved lifeline band is what makes reachability a law, not a hope.");

Console.WriteLine();
Console.WriteLine("================================================================================");
Console.WriteLine(" Recommendation: approach (b) graduates. Pure C# in Core, runs in the existing");
Console.WriteLine(" dotnet-test CI with zero new infrastructure. See README section 'The verdict'.");
Console.WriteLine("================================================================================");
