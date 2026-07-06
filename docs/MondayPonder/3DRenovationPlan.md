# 3D / ship-interior renovation plan

## Context

The ship-interior views (Deck desk `7`) exist and work, but are visually bare: every interior is
**pure 2D-canvas vector drawing** with **no raster/image support anywhere** in the renderer. The
first-person mode (`F`) is a flat-shaded Wolfenstein-style column raycaster, not textured. The
cantina is currently just an HTML panel (news wire + "Pour a tot" button) with no bar imagery; the
three cabins are labels only (no bunks); there is no space toilet. Owner wants the interiors to
feel lived-in — the pirate cantina should read as a *bar*, cabins should have bunks, and yes, space
toilets.

Boarding today is a **shuttle-flight minigame** to the target's airlock — it does **not** walk you
through the raided ship's interior. Walking a raided ship in 3D would be a new feature (see Deferred).

### Decisions (owner)
- **Aesthetic: Hybrid** — GenAI/raster art as room backdrops + wall textures, with the vector
  consoles, avatar, droids, and HUD kept *on top* for legibility. Keeps the readable instrument feel.
- **Scope: through first-person texturing** — dress the top-down deck (bar, bunks, space toilet)
  *and* give the first-person walk real textured surfaces.
- **Art sourcing: my call** — Phase-0 spike of the `gemini`/`grok` CLIs for image generation; if
  viable, use GenAI for painterly layers; otherwise hand-authored layered SVG. Consuming pipeline
  is identical either way, so the keystone is unblocked regardless.

## Rendering facts (from the code map)

- Interior renderers: `Rendering/DeckView.cs` (top-down), `Rendering/FirstPersonView.cs`
  (raycaster), `Rendering/ShuttleFlightView.cs` (boarding). All share one `IRenderer`.
- Interop chain: `IRenderer` → `CanvasRenderer` (packs float opcode buffer: polyline=1, circle=2,
  polygon=3) → `RendererInterop` (`[JSImport]`/`[JSExport]`, `DrawFrame` zero-copy + `DrawTexts`
  JSON) → `wwwroot/renderer.js` (decodes opcodes; only `ctx` primitives + `fillText`; **no
  `drawImage`, no image cache**).
- Rooms/zones (deck units, +X bow) in `Rendering/DeckPlan.cs`: BRIDGE, **CANTINA** (has panoramic
  window + 3 tables), **CABIN 1/2/3** (labels only), SHUTTLE BAY, CARGO HOLD, ENGINE ROOM, CORRIDOR.
  `DeckPlan.Walls` / `Consoles` / `RoomLabels` are the tables to edit; `CastRay` already returns
  hit-position-along-wall + `isWindow`/`isHull` flags — the hooks for per-wall textures exist.
- Assets: no game art shipped; anything under `wwwroot/` is served automatically (like the scenario
  JSON mirrored by the `CopyScenariosIntoWwwroot` MSBuild target).

## Phases

### Phase 0 — art-sourcing spike (de-risk) — DONE ✅
**Outcome: Grok is the image engine.** The Gemini API key is free-tier (image models 429; Imagen
paid-only) and the Gemini CLI's OAuth has no image capability. The **Grok CLI has native `image_gen`
/`image_edit` tools** (its `imagine` skill) and produced an excellent on-style bar backdrop
(298 KB, `scratchpad/bar_grok.png`). Working recipe (needs a `Bash(grok:*)` allow rule in
`.claude/settings.local.json`, already added):
```sh
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." -m grok-build --permission-mode bypassPermissions
```
Locked style suffix for every room prompt: *"grimy lived-in used-future sci-fi, muted desaturated
palette, painterly concept-art style, moody lighting, no text or logos."*
- Note: `image_gen` emits **JPEG** bytes; name room files `.jpg` (or transcode). The renderer's
  image decode is content-based, so extension mismatch still loads, but keep names honest.

### Phase 1 — raster keystone (unlocks everything)
- `Rendering/IRenderer.cs`: add `DrawImage(imageId, dx, dy, dw, dh, alpha?)`.
- `Rendering/CanvasRenderer.cs`: add `OP_IMAGE` opcode; encode id + dest rect into the float buffer.
- `Rendering/RendererInterop.cs`: add `[JSImport] loadImage(url) -> id` (JS holds the bitmap; the
  float buffer can only carry an id + rect).
- `wwwroot/renderer.js`: image cache (`Map<id, ImageBitmap>`), `loadImage` export, `OP_IMAGE` branch
  calling `ctx.drawImage` with clip so art never bleeds past a room.
- Preload art at deck entry; draw-before-vector ordering so overlays stay on top.
- Verify: a placeholder image renders under the vector deck without breaking the primitive path.

### Phase 2 — cantina → bar
- Quick win first: the **Galley desk** is HTML — give it a bar backdrop image + styling (no renderer
  work). Trial the chosen art source here.
- Deck CANTINA zone (`DeckView.cs`): bar backdrop image + bar furniture (counter, stools, bottle
  shelf) as art/vector over `DeckPlan` cantina bounds; keep the CANTINA console + `[E]` on top.

### Phase 3 — cabins + space toilet
- CABIN 1/2/3 (`DeckView.cs` + `DeckPlan`): bunk art per cabin; small locker/footlocker dressing.
- New **HEAD** (space toilet): either a new small zone carved from the corridor/cabin block in
  `DeckPlan` (walls + label + a `HEAD` console) or a fixture inside a cabin. Add a gag `E`
  interaction (a one-liner, à la the rum locker), and a NewsWire/pulse quip. 🚽

### Phase 4 — first-person texturing
- Extend `Wall` (`DeckPlan.cs`) with a `material`/texture id; `CastRay` already yields the along-wall
  coordinate for sampling.
- `FirstPersonView.cs`: replace flat `DrawVStrip` wall strips with textured columns via the Phase-1
  `DrawImage` (sample a wall texture by `along`/distance). Window walls keep the real-sky path; add
  optional planet/sun disc bitmaps to the sky. Droid billboards can stay vector or become sprites.

### Deferred — raid-interior walk
- Walking the raided ship's interior during a boarding run (target-ship deck plans + a boarding
  sequence). Substantial new system; scope separately after the renovation lands.

## Verification
- Renderer keystone: unit-eyeball in the running client (`dotnet run` client project, port 5073,
  kill+restart per build) — placeholder image draws under vector overlays; primitive-only frames
  unaffected. `dotnet test tests/SpaceSails.Core.Tests` stays green (Core has no renderer, so this
  guards against accidental Core regressions).
- Per phase: walk the deck (`7`, then WASD), open the cantina, enter first-person (`F`), and confirm
  art placement, clipping, and overlay legibility in both light and the tilty-cantina state.

## Notes
- Determinism unaffected: art is presentation-only; no Core changes.
- Keep vector overlays (consoles, avatar, HUD, `[E]` prompts) always on top — legibility is the
  guardrail on the hybrid look.
