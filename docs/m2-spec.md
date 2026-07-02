# M2 — Renderer & Camera: implementation work package

*Companion to the milestone entry in `SpaceSails_plan_detailed.md` §8 (M2). This is the concrete
build sheet. Senior groundwork is already in the tree — build on it, do not redesign it.*

## Goal (acceptance criteria, verbatim from the plan)

- Canvas interop layer (`renderer.js` + C# `CanvasRenderer : IRenderer`): draw circles, polylines,
  text, screen-space icons; a `requestAnimationFrame` loop calling into .NET.
- Camera: pan, zoom (mouse wheel, ~12 orders of magnitude — whole system → ship close-up),
  floating origin, follow-ship mode.
- Render scenario A (`scenarios/sol.json`) live at time warp with a warp control (Bootstrap
  toolbar: 1×…10,000×, pause).
- **Accept:** browser shows planets orbiting; smooth at 60 fps with **500 dummy trajectory polylines**.

## What already exists (senior groundwork — use as-is)

- `src/SpaceSails.Client/Rendering/IRenderer.cs` — `IRenderer` interface, `RgbaColor`, `TextAlign`.
  **Implement this interface; do not change its signatures.** All draw calls are in **screen space
  (pixels, Y down)**.
- `src/SpaceSails.Client/Rendering/Camera.cs` — floating-origin camera with `WorldToScreen`,
  `ScreenToWorld`, `ZoomBy`, `PanByPixels`, `CenterOn`, `SetViewport`, `MetersPerPixel` (clamped).
  **Use it for every world→pixel conversion. No world-meter math in JS.**
- `scenarios/sol.json` is served to the client at runtime path `scenarios/sol.json` (wired in the
  client `.csproj`). Load it with `HttpClient.GetStringAsync("scenarios/sol.json")` then
  `ScenarioLoader.Parse(json)` (NOT `LoadFile` — no filesystem in WASM). Build the ephemeris with
  `CircularOrbitEphemeris.FromScenario(scenario)`.

The Core sim API you need: `ICelestialEphemeris.Bodies`, `.Position(bodyId, simTime)`,
`CircularOrbitEphemeris`, `Vector2d`.

## Architecture (follow this — it is the load-bearing part)

**Rule 1 — precision.** Every world coordinate is a `double` in meters and stays in C#. JS only ever
receives `float` pixel values produced by `Camera.WorldToScreen`. Never send meters to JS.

**Rule 2 — batching.** At 500 polylines/frame you cannot afford one interop call per primitive.
`CanvasRenderer` accumulates all primitives issued between `BeginFrame`/`EndFrame` into buffers and
flushes them to `renderer.js` in **one** call per frame for the vertex data.

### Interop protocol (implement exactly this shape)

Use **`System.Runtime.InteropServices.JavaScript`** (`[JSImport]`/`[JSExport]` partial methods) —
this is the fast .NET-WASM path and lets you hand JS a zero-copy `Float32Array` view over a C#
buffer via `[JSMarshalAs<JSType.MemoryView>] Span<float>`.

`renderer.js` is an ES module (`wwwroot/renderer.js`) providing:

- `initCanvas(canvasId)` → look up the `<canvas>`, cache its 2D context.
- `drawFrame(canvasId, buffer, length)` where `buffer` is a `Float32Array` view. Decode with an
  opcode loop and paint. Encoding (all values float32):
  - **Frame header** (first 6 floats): `[widthPx, heightPx, bgR, bgG, bgB, bgA]` → set canvas
    size if changed, clear to background.
  - Then a sequence of primitives, each `[opcode, ...fields]`:
    - `OP_POLYLINE = 1`: `r, g, b, a, widthPx, n, x0, y0, x1, y1, … (n points)`. Colors 0..255.
    - `OP_CIRCLE = 2`: `hasFill(0|1), fr, fg, fb, fa, sr, sg, sb, sa, strokeWidthPx, x, y, radiusPx`.
- `drawTexts(canvasId, json)` — text is rare (≈10 labels/frame); pass a JSON string array of
  `{x, y, text, r, g, b, a, font, align}`. Decode and `fillText`. (Keeps strings out of the float
  buffer.) Call once per frame after `drawFrame`.

Game loop:

- `renderer.js` owns the `requestAnimationFrame` loop. Start it from C# (`startLoop(canvasId)`); each
  frame it calls the C# `[JSExport]` method `Tick(double highResTimestampMs)`.
- `Tick` computes `dtRealSeconds` from the timestamp delta, advances `simTime += dtReal * warp`,
  then renders: `BeginFrame` → draw orbit rings + bodies + the 500 dummy polylines → `EndFrame`
  (which invokes `drawFrame`), then `drawTexts`.
- Pause = warp 0. Warp values: 1, 10, 100, 1000, 10000 (buttons or a slider) + a pause toggle.

If `[JSImport]`/`[JSExport]` marshalling of `MemoryView` proves troublesome, the acceptable
fallback is `IJSInProcessRuntime.InvokeVoid` passing the float buffer — but still **one call per
frame**, never per primitive, and measure that 500 polylines hold 60 fps.

## Rendering content for the demo page (`Pages/Home.razor` or a new `Pages/Map.razor`)

1. Full-viewport `<canvas>` with a Bootstrap toolbar overlay (warp buttons, pause, current warp,
   sim-time readout, zoom readout).
2. Draw for each body: its **orbit ring** as a polyline (sample the circle in world space via the
   ephemeris parent + radius, ~128 segments, `WorldToScreen` each point), and a filled **circle**
   for the body itself. Body pixel radius = `max(2, bodyRadiusM / MetersPerPixel)` so planets stay
   visible when zoomed out. Label each body with `DrawText`.
3. **500 dummy trajectory polylines**: generate once (deterministic, e.g. seeded arcs or projected
   ship paths scattered across the system) and redraw every frame — this is the perf gate. A simple
   source: pick 500 random start states and `Simulator.Project` short arcs, or synthesize sine-wobbled
   rings. They just need to be 500 real multi-point polylines transformed through the camera each frame.
4. Input: mouse wheel → `Camera.ZoomBy` toward cursor; drag → `Camera.PanByPixels`; a "follow ship"
   toggle that calls `CenterOn` each frame (a dummy ship is fine for M2). Wire pointer/wheel events
   via `@onwheel`, `@onpointerdown/move/up` in Razor (no JS input layer yet — that's a later `input.js`).

## Constraints (from the working agreement, §9)

- **UI = Razor + Bootstrap only.** Custom CSS allowed **only** for the canvas/HUD layer. No JS
  frameworks. JS lives **only** in `renderer.js`.
- Do not widen the milestone. If you need a Core interface that doesn't exist, add the minimal
  version and note it in the PR description. (You should not need to — Core is sufficient.)
- Keep `SpaceSails.Core` untouched unless strictly necessary; the renderer is a Client concern.

## Definition of done

- `dotnet build` clean (0 warnings), existing `dotnet test` still green.
- `dotnet run --project src/SpaceSails.Client` serves a page where planets visibly orbit at warp,
  zoom spans system↔close-up smoothly, and 500 polylines render without stutter.
- Brief notes in the PR description: interop path chosen, measured frame time with 500 polylines.
