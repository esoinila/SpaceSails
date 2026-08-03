// Lab 43 · What is actually ON Miranda's landing sites?
//
// The owner, playtesting 2026-07-31: "the map is kind of boring... no door or enclosed places",
// "I have never seen the landing site area expand yet... we should test it", "There should be huts
// . i.e lockable spaces there not just U shapes."
//
// This probe answers all three from Core, deterministically, with no browser in the loop:
//   1. Which sites does Miranda offer, and what ground does each one generate?
//   2. How many features, of what shape, and how much of the field do they occupy?
//   3. CAN a landing site ever expand here? (The append-a-region machinery exists — SecretLab and
//      ExpeditionRegions — so the question is only whether Miranda can ever roll into it.)
using SpaceSails.Core;
using SpaceSails.Labs.Lab43;

// --svg writes one picture per site under labviz/ — the only way to LOOK at a landing site without the
// game, since a descent will not render in an automated browser tab (rAF is throttled when the tab counts
// as hidden, so ?land=1 hangs mid-descent).
bool wantSvg = Array.IndexOf(args, "--svg") >= 0;

// The field the client hands SurfaceLayout, verbatim from MoonSurface's constants.
// #573 · Read from Core. This lab used to hand-copy the client's constants and therefore printed
// confident measurements of a field the game had stopped shipping.
SurfaceLayout.Field field = SurfaceLayout.DefaultField;

double fieldW = field.RightX - field.LeftX;      // 78 du
double fieldH = field.TopY - field.BottomY;      // 64 du

Console.WriteLine($"FIELD  {fieldW} x {fieldH} du  ({fieldW * fieldH:N0} du^2)");
Console.WriteLine();

foreach (string body in new[] { "miranda", "luna", "phobos", "europa", "titan" })
{
    Console.WriteLine($"=== {body.ToUpperInvariant()} — {LandingSites.Count(body)} sites ===");

    foreach (LandingSite site in LandingSites.For(body))
    {
        SurfaceLayout.Plan plan = SurfaceLayout.For(body, field, site.LayoutSalt);

        // The bounding box of everything the generator actually placed.
        double x0 = double.MaxValue, x1 = double.MinValue, y0 = double.MaxValue, y1 = double.MinValue;
        foreach (SurfaceLayout.Wall w in plan.Walls)
        {
            x0 = Math.Min(x0, Math.Min(w.X1, w.X2)); x1 = Math.Max(x1, Math.Max(w.X1, w.X2));
            y0 = Math.Min(y0, Math.Min(w.Y1, w.Y2)); y1 = Math.Max(y1, Math.Max(w.Y1, w.Y2));
        }
        string extent = plan.Walls.Count == 0
            ? "(nothing placed)"
            : $"{x1 - x0:F0} x {y1 - y0:F0} du = {(x1 - x0) * (y1 - y0) / (fieldW * fieldH) * 100:F0}% of the field";

        Console.WriteLine($"  site {site.Index}  {site.Name,-22} salt='{site.LayoutSalt}'  scheme={plan.Scheme}");
        Console.WriteLine($"           {plan.Walls.Count,3} wall segments · {plan.Landmarks.Count} landmark(s) · {extent}");

        // How many of those segments are HULL-flagged — i.e. drawn in spaceship ink on a moon.
        int hull = 0;
        foreach (SurfaceLayout.Wall w in plan.Walls) { if (w.IsHull) { hull++; } }
        Console.WriteLine($"           {hull} of them flagged IsHull (drawn as pressure hull)");

        (double shx, double shy) = SurfaceShelter.PlaceOn(body, site.LayoutSalt, field);
        Console.WriteLine($"           SHELTER at ({shx:F0}, {shy:F0})  [tube mouth is (-7, -20)]");

        if (wantSvg)
        {
            Directory.CreateDirectory("labviz");
            string path = Path.Combine("labviz", $"site-{body}-{site.Index}.svg");
            File.WriteAllText(path, SiteSvg.Draw(body, site, field, plan, tubeLeft: -9, tubeRight: -5));
            Console.WriteLine($"           [svg] {path}");
        }
    }

    // Can this body's ground EVER grow? Two gates, both seeded and both pure.
    bool lab = SecretLab.Present(body);
    Console.WriteLine($"  secret lab present? {(lab ? "YES" : "no")}   (1 in {SecretLab.OrdinaryOneInN} on an ordinary moon)");
    Console.WriteLine($"  expedition regions? no — those need an 'expedition-site-*' body id");
    Console.WriteLine($"  => the ground here can expand: {(lab ? "YES (via the lab's concealed door)" : "NEVER")}");
    Console.WriteLine();
}

// #531 · The dead stations. Print each one's damage and, with --svg, draw her.
Console.WriteLine("=== DEAD STATIONS (#531) — which parts of her you can walk to ===");
foreach (string stationId in new[] { "station-wreck-1", "station-wreck-2", "kepler-transfer", "the-orchard" })
{
    var severed = StationWreck.SeveredTubes(stationId);
    Console.WriteLine($"  {stationId,-18} {StationWreck.WalkGroups(stationId).Count} walk-groups · " +
        $"severed: {string.Join(", ", severed)}");
    foreach (StationWreck.Access a in StationWreck.Accesses(stationId))
    {
        string how = a.Kind == StationWreck.AccessKind.ServiceableLock ? "lock cycles" : "CUT YOUR WAY IN";
        Console.WriteLine($"       {a.Name,-34} {how}");
    }
    if (wantSvg)
    {
        Directory.CreateDirectory("labviz");
        string path = Path.Combine("labviz", $"station-{stationId}.svg");
        File.WriteAllText(path, StationSvg.Draw(stationId));
        Console.WriteLine($"       [svg] {path}");
    }
}
Console.WriteLine();

// The three authored expedition rocks, for contrast — the only places the owner COULD have seen growth.
Console.WriteLine("=== EXPEDITION ROCKS (the only grounds with sealed doors today) ===");
foreach (string id in new[] { "expedition-site-ruins", "expedition-site-wreck", "expedition-site-tunnel" })
{
    Console.WriteLine($"  {id,-26} secret lab: {(SecretLab.Present(id) ? "YES" : "no")}");
}
