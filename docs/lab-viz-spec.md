# Lab Viz — a visualization pop-up for the gravity labs

*Spec (2026-07-13). Owner ask: some labs — the Grand Tour (lab 19) in particular — deserve a
picture. Run the lab from the command line, get the numbers as always, and optionally get a
browser pop-up showing the trajectories the probe just computed, drawn in the game's visual
language. Copy what's copyable from the browser game; keep the labs' honesty rule intact.*

## The one-line contract

```sh
dotnet run --project labs/19-the-grand-tour -c Release -- --viz
```

prints exactly what it prints today **plus** writes `labviz/lab19-the-grand-tour.html` (a single
self-contained file, no external requests) and opens it in the default browser. Without `--viz`,
stdout is **byte-identical to today** — the READMEs' printed numbers are sacred.

## Architecture

Three pieces, one new project:

1. **`labs/SpaceSails.LabViz/`** — a tiny class library (net10.0, references
   `src/SpaceSails.Core` only). A lab builds a `VizScene` (bodies, named trajectory paths,
   event markers), and `LabViz.Show(scene, args)` serializes it to JSON, splices it into an
   embedded HTML viewer template, writes the file, and opens the browser.
2. **`viewer.html`** — the template, an embedded resource in LabViz. Single file, zero external
   dependencies (strict: no CDN scripts, no fonts, no fetch). Plain JS + 2D canvas. The camera
   is a port of the game's `src/SpaceSails.Client/Rendering/Camera.cs` (floating-origin
   world→screen, `MetersPerPixel` zoom with the same clamps, zoom-toward-cursor, pixel pan).
   The drawing follows `Map.razor`'s conventions: orbit rings as 128-segment polylines, body
   discs with a minimum pixel radius, labels gated on zoom, trajectory ribbons as polylines.
   Colors are lifted from the game's constants (`Map.razor` `OrbitColor`/`TrajectoryColor`
   region, ~line 1716) so a lab pop-up looks like a SpaceSails instrument, not a generic plot.
3. **Lab wiring** — labs that want a picture add a few `scene.AddPath(...)` calls where they
   already have the sample lists. First wave: **lab 19** (the showcase) and **lab 01** (the
   minimal example other labs copy).

Deliberately NOT reused: `CanvasRenderer`/`RendererInterop`/the opcode buffer. Those exist to
move floats across the WASM boundary; a standalone HTML file has no boundary to cross. We copy
the *visual language and camera math*, not the plumbing.

## LabViz API (the contract between the implementing agents)

```csharp
namespace SpaceSails.LabViz;

public sealed class VizScene
{
    public VizScene(string slug, string title, string? subtitle = null);
    // slug → output filename: labviz/<slug>.html

    /// Record a body's circular-orbit parameters; the viewer animates positions analytically.
    /// Convenience overload takes SpaceSails.Core CelestialBody records directly.
    public void AddBody(CelestialBody body, string? colorHex = null);
    public void AddBodies(IEnumerable<CelestialBody> bodies);

    /// A trajectory polyline. group = legend grouping ("main", "sweep", ...); groups can be
    /// toggled in the viewer. ghost=true → the time scrubber animates a ship dot along it.
    /// Samples are decimated to maxSamples (default 2500) preserving first and last points.
    public void AddPath(string label, IReadOnlyList<TrajectorySample> samples, string colorHex,
                        string group = "main", double width = 1.5, double opacity = 1.0,
                        bool ghost = false, int maxSamples = 2500);

    /// A point event: kind ∈ "burn" | "flyby" | "closest" | "event" (distinct glyphs).
    public void AddMarker(double simTime, Vector2d position, string label, string kind);

    public string ToJson();   // the scene document (schema below)
    public string ToHtml();   // template with JSON spliced in
}

public static class LabViz
{
    /// True iff args contains "--viz". Also honors "--viz-out=<path>" and "--viz-no-open".
    public static bool Wants(string[] args);

    /// Save scene.ToHtml() to labviz/<slug>.html under the current directory (or --viz-out),
    /// print the path to stdout (this line only appears WITH --viz, so the no-flag byte-identity
    /// rule holds), and open the default browser (UseShellExecute on Windows; open/xdg-open
    /// fallback) unless --viz-no-open.
    public static void Show(VizScene scene, string[] args);
}
```

Usage inside a probe (top-level statements — `args` is the magic variable):

```csharp
var viz = LabViz.Wants(args) ? new VizScene("lab19-the-grand-tour", "Lab 19 — The Grand Tour") : null;
// ... existing computation, unchanged ...
viz?.AddPath("itinerary", flownSamples, "#e8a33d", ghost: true);
if (viz is not null) LabViz.Show(viz, args);
```

## Scene JSON schema (v1)

Embedded in the HTML as `const SCENE = {...};`.

```jsonc
{
  "schema": 1,
  "title": "Lab 19 — The Grand Tour",
  "subtitle": "Earth → Jupiter → Saturn, flown with TCMs",
  "epoch": "1977-08-20T14:29:00.0000000Z",      // optional display epoch (ISO 8601 UTC): the
                                                // viewer maps simTime=0 here and shows calendar
                                                // dates; null → sim days only. Presentation only.
  "time": { "t0": 0.0, "t1": 1.26e8 },          // seconds; inferred from paths if not set
  "bodies": [
    { "id": "jupiter", "name": "Jupiter", "parentId": "sun", "bodyRadius": 6.9911e7,
      "orbitRadius": 7.785e11, "orbitPeriod": 3.74e8, "initialPhase": 1.23, "color": "#..." }
  ],
  "paths": [
    { "label": "itinerary", "group": "main", "color": "#e8a33d", "width": 1.5,
      "opacity": 1.0, "ghost": true, "samples": [[t, x, y], ...] }
  ],
  "markers": [ { "t": 0, "x": 1.4e11, "y": 0, "label": "TCM-1 (12.4 m/s)", "kind": "burn" } ]
}
```

Units: SI meters/seconds, heliocentric ecliptic plane — identical to `Vector2d`/`ShipState`
conventions in Core. **Body position convention must match `CircularOrbitEphemeris` exactly**
(parent chaining included); the implementer reads `src/SpaceSails.Core/CircularOrbitEphemeris.cs`
and documents the exact angle formula in both the C# XML docs and a comment in the viewer JS.
A unit test recomputes positions from an emitted scene using that formula and compares against
the real ephemeris at several times (the parity gate — this is what keeps the picture honest).

## Viewer features

Must-have:

- Dark, game-styled canvas; camera pan (drag) / zoom (wheel, toward cursor), `MetersPerPixel`
  clamped like the game's `Camera.cs`; initial framing fits all paths with margin.
- Bodies animated analytically at scrub time t; orbit rings; discs (min 2 px); labels.
- Paths as polylines with per-path color/width/opacity; legend listing groups with
  click-to-toggle (lab 19's aim-offset sweep family lives in one toggleable group).
- Time control: scrubber over [t0, t1] + play/pause + log-scale speed slider (the game's warp
  slider, simplified). Ghost ship dot(s) on `ghost` paths, linear interpolation between samples.
- Readout strip: sim time as `d 123.4 (y 0.34)`, ghost heliocentric distance in AU and speed in
  km/s (finite difference of neighboring samples).
- Markers with glyph by kind + label; a marker's label always renders regardless of zoom.
- Title/subtitle overlay; footer line "SpaceSails Gravity Lab — generated by Lab NN".

Nice-to-have (do not gate the PR on these): hover-nearest-sample readout, keyboard shortcuts
(space = play/pause, arrows = scrub).

## Lab 19 wiring (the showcase)

Recording observes lists the probe already computes; where a needed `ProjectAdaptive` result is
currently discarded before the winner is known, re-projecting the winning candidate after the
scan is fine (deterministic engine — same numbers). Record:

- **Bodies**: the probe's 9-body table (sun + 8 planets).
- **`sweep` group** (Section B, the crank): one path per aim offset (7 paths), thin, faded
  single hue, labels `aim −3.0e9 m` etc. Toggleable as a group, default ON.
- **`main` group** (Section C, the flown itinerary): the winning Earth→Jupiter→Saturn flight —
  launch leg and post-TCM legs as recorded/re-projected — orange (`TrajectoryColor`),
  `ghost: true`.
- **Markers**: departure burn, TCM-1, TCM-2 (kind `burn`, labels include Δv), Jupiter closest
  approach (kind `flyby`, label includes pass distance in R_J), Saturn closest pass
  (kind `closest`).
- **README**: a short "Seeing it" section documenting `-- --viz`.

## Lab 01 wiring (the minimal example)

One `AddBodies`, one ghost path for the computed freefall/orbit, one marker. Its purpose is to
be the ~6-line diff other labs copy. README gets the same "Seeing it" footnote.

## Tests

In `tests/SpaceSails.Core.Tests`, new `LabVizTests.cs`; the tests project gains a
ProjectReference to SpaceSails.LabViz.

1. JSON schema: scene with bodies/paths/markers round-trips; required fields present.
2. Decimation: >maxSamples input decimates to ≤maxSamples, first/last preserved, times monotonic.
3. **Ephemeris parity**: positions recomputed from emitted body parameters (using the documented
   viewer formula, incl. a parent-chained moon) match `CircularOrbitEphemeris` to 1e-6 relative.
4. HTML self-containment: output contains the spliced `const SCENE`, and matches no external-
   resource patterns (`src="http`, `href="http`, `@import`, `fetch(`).
5. `LabViz.Wants` parsing: `--viz`, `--viz-out=`, `--viz-no-open`, and absence.
6. Byte-identity guard is by construction (all LabViz stdout is behind `Wants(args)`), asserted
   in review, not in a test that shells out.

Manual verification (release gate, done by the reviewing agent/lead): run lab 01 and lab 19 with
`-- --viz`, open the HTML in a real browser, scrub, toggle the sweep group, screenshot.

## Solution wiring

- `SpaceSails.LabViz.csproj` added to `SpaceSails.slnx` under `/labs/`.
- `Lab01.csproj` and `Lab19.csproj` gain a ProjectReference to LabViz.
- `labs/README.md`: new "Seeing a lesson: --viz" section; `docs/features/lab-viz.md` short
  outward-facing page linked from it.
- `labviz/` added to `.gitignore` (generated output).

## Non-goals (v1)

- No live/streaming viz while the probe runs; the pop-up is post-hoc.
- No reuse of the Blazor `?scenario=` path (that remains the "play the lesson in the game"
  channel; this is the "see the computation" channel).
- No 3D, no WebGL, no charting library.
- Wiring the other 17 labs — follow-up PRs once the pattern is proven.
