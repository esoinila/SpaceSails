# 3D / ship-interior renovation plan

> **Start-fresh handoff.** This doc is self-contained: a new session can resume from the
> **Status** and **How to resume** sections without prior context. Last updated 2026-07-06.

## Status

| Phase | State |
|---|---|
| 0 — art-sourcing spike | ✅ **Done** — Grok `image_gen` is the image engine (recipe below) |
| 1 — renderer raster keystone + The Space Bar cantina backdrop | ✅ **Done** — shipped on branch `feature/3d-deck-renovation`, **PR #90**, verified in-browser |
| 2 — Galley *desk* HTML bar | ✅ **Done** — `the-space-bar-desk.jpg` backdrop behind the Galley panel, verified in-browser |
| 3 — cabin bunks + a space-toilet HEAD 🚽 | ✅ **Done** — `cabin-bunk.jpg` ×3 + `space-head.jpg`; starboard block re-carved into 3 cabins + a HEAD; gag `[E]` quip via a new deck-mode pulse toast; verified in-browser (walked in, fired the gag, first-person clean) |
| 4 — first-person wall texturing | ⬜ **Next** (biggest lift; needs a source-rect `DrawImage` on the shared renderer interop) |
| Deferred — walk the *raided* ship's interior | ⬜ separate new feature |

Unrelated in-flight work: **PR #89** (Debt Collector targetable/deterrable/sun-dodgeable +
inner-system havens) is a *different* branch — don't conflate.

## Context

The ship-interior views (Deck desk `7`) work but were visually bare: every interior was **pure
2D-canvas vector drawing** with **no raster/image support** in the renderer. First-person (`F`) is a
flat-shaded Wolfenstein-style column raycaster, not textured. The cantina *desk* (Galley) is still a
plain HTML panel; the three cabins are labels only (no bunks); there is no space toilet. Owner wants
lived-in interiors — the pirate cantina should read as a *bar*, cabins should have bunks, and yes,
space toilets. Boarding is a shuttle-flight minigame; it does **not** walk you through the raided
ship (that's the Deferred item).

### Decisions (owner)
- **Aesthetic: Hybrid** — raster art as room backdrops + wall textures, vector consoles/avatar/HUD
  kept *on top* for legibility.
- **Scope: through first-person texturing** — dress the top-down deck (bar ✅, bunks, space toilet)
  and texture the first-person walk.
- **Art source: Grok `image_gen`** (see Phase 0).

## Art-sourcing recipe (Phase 0 outcome) — Grok `image_gen`

Gemini was a dead end here (free-tier key → image models 429 / Imagen paid-only; the Gemini CLI's
OAuth has no image capability). **Grok** has native `image_gen`/`image_edit` tools (its `imagine`
skill) and generates on-style art. Needs a `Bash(grok:*)` allow rule in `.claude/settings.local.json`
(already added). Recipe:
```sh
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm with a dir listing + byte size." -m grok-build --permission-mode bypassPermissions
```
- **Must** use `-m grok-build` (image tools live there) and `--permission-mode bypassPermissions`
  (`acceptEdits` is not enough — `image_gen` isn't an "edit").
- **Locked style suffix** for every room prompt, so the ~8 rooms cohere:
  *"grimy lived-in used-future sci-fi, muted desaturated palette, painterly concept-art style,
  moody lighting, no text or logos."*
- Output is **JPEG** bytes → save room files as `.jpg`. Drop them in
  `src/SpaceSails.Client/wwwroot/art/` (served automatically as static web assets; no csproj change).

## As-built: the raster keystone (Phase 1)

Reusable for every room backdrop and, later, first-person wall textures.
- **`Rendering/IRenderer.cs`** — `int RegisterImage(string url)` (idempotent per URL, returns a
  stable id) and `void DrawImage(int id, float x, float y, float w, float h, float alpha = 1f)`.
- **`Rendering/CanvasRenderer.cs`** — `OP_IMAGE = 4f`; `RegisterImage` dedupes via a
  `Dictionary<string,int>` and calls `RendererInterop.LoadImage(id, url)` once; `DrawImage` encodes
  `[OP_IMAGE, id, x, y, w, h, alpha]` (7 floats).
- **`Rendering/RendererInterop.cs`** — `[JSImport] LoadImage(int id, string url)`.
- **`wwwroot/renderer.js`** — `const OP_IMAGE = 4`; an `images` `Map<id, HTMLImageElement>`; a
  `loadImage(id, url)` export; and an `OP_IMAGE` branch that `ctx.drawImage(img, x,y,w,h)` at the
  given `globalAlpha` (drawImage confines the bitmap to the rect — no bleed; a not-yet-decoded id
  draws nothing that frame).

**Usage pattern** (from `DeckView.Draw`, the template for any new room backdrop):
```csharp
int barArt = _renderer.RegisterImage("art/the-space-bar.jpg"); // idempotent; call per frame is fine
(float x, float y) = P(4, 10);           // top-left of the CANTINA zone (deck units -> screen)
_renderer.DrawImage(barArt, x, y, 14f * scale, 7f * scale, 0.9f); // BEFORE walls/consoles/overlays
```
Draw backdrops right after `BeginFrame`/projection so the vector overlays land on top. Cantina zone
is deck units x∈[4,18], y∈[3,10]; `P(dx,dy) = (ox + dx*scale, oy - dy*scale)`.

## Rendering facts (code map)

- Interior renderers share one `IRenderer`: `Rendering/DeckView.cs` (top-down),
  `Rendering/FirstPersonView.cs` (raycaster), `Rendering/ShuttleFlightView.cs` (boarding).
- Rooms/zones in `Rendering/DeckPlan.cs` (deck units, +X bow): BRIDGE, **CANTINA** (panoramic
  window + 3 tables; now backdropped), **CABIN 1/2/3** (labels only), SHUTTLE BAY, CARGO HOLD,
  ENGINE ROOM, CORRIDOR. `DeckPlan.Walls` / `Consoles` / `RoomLabels` are the tables to edit.
  `CastRay` already returns hit-position-along-wall + `isWindow`/`isHull` — the hooks for per-wall
  textures exist.
- The Galley desk (the "cantina" console target) is `Pages/Stations/Galley.razor` — plain HTML/CSS,
  no renderer involvement (so Phase 2's desk bar is a CSS job).

## Remaining phases

### Phase 2 — cantina → bar (deck backdrop ✅ already done in Phase 1)
- **Galley desk** (`Pages/Stations/Galley.razor`): give the HTML panel a bar backdrop image + styling
  (CSS `background`), so the "Pour a tot" screen reads as a bar. No renderer work.
- Optional: add vector bar furniture (counter/stools/bottle shelf) over the deck cantina if the
  backdrop alone wants reinforcing — but the backdrop already carries it.

### Phase 3 — cabins + space toilet ✅ **Done** (as-built)
- **Art:** `art/cabin-bunk.jpg` (reused across all three cabins) + `art/space-head.jpg`, both
  generated portrait (Grok `aspect_ratio 9:16`) since the berths are ~1:2 tall zones.
- **Geometry (`DeckPlan.cs`):** the old three-cabin block was re-carved into **4 berths of 3.5 du**
  across x∈[4,18], y∈[-10,-3] — stern-to-bow **CABIN 3 [4,7.5] · CABIN 2 [7.5,11] · CABIN 1
  [11,14.5] · HEAD [14.5,18]** — each with a **2.5 du corridor door** (matches the original CABIN 3
  door width, so passable) and full-depth dividers at x=7.5/11/14.5. New `ConsoleKind.Head` + a
  `HEAD 🚽` console at (16.25,-6.5); cabin `RoomLabels` moved to y=-9 to clear the console.
- **Backdrops (`DeckView.cs`):** bunk art in the three cabins, toilet art in the HEAD, drawn under
  the vector overlay exactly like the bar (top-left `P(x0,-3)`, size `(x1-x0)*scale × 7*scale`).
- **Gag (`Map.razor`):** `ConsoleKind.Head` → `ShowPulseMessage(HeadQuip())`, six deterministic
  one-liners (`HeadLines`, indexed by `SimTime` like `RumLines`). `LocationHint` returns `HEAD` for
  x>14.5, y<-3.
- **⚠ Gotcha fixed:** the Nav HUD that hosts `_pulseMessage` is gated to `ShipDesk.Nav`, so **any
  deck-mode quip (HEAD, Cargo, Shuttle, Vent) was rendering nowhere.** Added a `.deck-pulse-toast`
  overlay (shown when `_deckMode && _pulseMessage`) in `Map.razor` + `Map.razor.css`. Reuse this for
  future deck interactions.
- **Verified:** walked through the new corridor door into the HEAD, `HEAD 🚽 [E]` prompt showed,
  `E` fired the gag (toast confirmed via DOM), first-person renders the HEAD as a clean 4-wall room
  with `HEAD` location hint. (Toast is timing-flaky to *screenshot* under 100× Debug WASM, not buggy.)

### Phase 4 — first-person texturing
- **Renderer prerequisite:** the current `DrawImage` only blits a whole image into a dest rect. A
  raycaster needs a **source-rect variant** — add `DrawImageSlice(imageId, sxFrac,syFrac,swFrac,shFrac,
  dx,dy,dw,dh, alpha)` with *normalized* source coords (JS multiplies by `naturalWidth/Height`, so C#
  never needs the texture's pixel size). New opcode (`OP_IMAGE_SRC = 5`) across `IRenderer` /
  `CanvasRenderer` / `renderer.js`. **This touches the shared interop every view uses — verify the
  solar map still renders after.**
- Extend `Wall` (`DeckPlan.cs`) with a `material`/texture id; `CastRay` already yields the along-wall
  coordinate for sampling.
- `FirstPersonView.cs`: replace flat `DrawVStrip` wall strips with textured columns via the new slice
  call (sample source x by `frac(along / textureSpanDu)`, full height). Window walls keep the
  real-sky path; optionally add planet/sun disc bitmaps to the sky.

### Deferred — raid-interior walk
- Walking the raided ship's interior during a boarding run (target-ship deck plans + a boarding
  sequence). Substantial new system; scope separately.

## How to resume (fresh session)
1. `git checkout feature/3d-deck-renovation` (Phase 1 lives here; PR #90).
2. Generate room art with the Grok recipe above (batch the whole set in one go for style
   consistency), saving to `src/SpaceSails.Client/wwwroot/art/<room>.jpg`.
3. Place each with the `RegisterImage` + `DrawImage` pattern (Phase-3 rooms in `DeckView.cs`).
4. Build the client: `dotnet build src/SpaceSails.Client/SpaceSails.Client.csproj -c Debug`
   (dev server is `dotnet run` on the Client project, port **5073** — **kill+restart per build**;
   Debug WASM is ~100× slower).
5. Verify in-browser: launch Sol → Deck desk (`7`) → walk (WASD) → cantina → first-person (`F`);
   confirm placement, clipping, overlay legibility, and the tilty-cantina state.
6. `dotnet test tests/SpaceSails.Core.Tests` stays green (guards Core — art is presentation-only).

## Guardrails
- Determinism unaffected: all art is presentation-only, **no Core changes**.
- Vector overlays (consoles, avatar, HUD, `[E]` prompts) always draw **on top** — legibility is the
  guardrail on the hybrid look.
