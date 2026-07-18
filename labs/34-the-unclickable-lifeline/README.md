# Lab 34 — The unclickable lifeline

> Owner (2026-07-18): *"Yesterday the rescue-me button was barely clickable when we ran out of power.
> Let's somehow check for similar problems. We should have some kind of test to find such cases during
> our CI testing. Not sure at all how to do that, but this problem keeps biting us as we develop."*

Every other lab in this house measures physics. This one measures the **UI** the same honest way,
because the same class of bug keeps recurring: a critical control — the rescue affordance a stranded,
out-of-reaction-mass captain presses to whistle for a tow — is *present* but not *pressable*. It keeps
biting because reachability of a control buried in a ~15-layer overlay stack is **emergent**: it depends
on which panels are raised, their z-order, their footprints and the viewport size. Nobody can eyeball
that and be sure. So we stop eyeballing it and **compute** it.

> **How to run**
> ```bash
> dotnet run --project labs/34-the-unclickable-lifeline -c Release
> ```
> Every table below is verbatim probe output. Change the code and the numbers go stale — rerun and
> re-paste, never hand-edit a table.

---

## 1. Root cause of the reported case

The mechanism is documented right in the source, at `src/SpaceSails.Client/Pages/Map.razor:1571-1576`:

> *"The old strip rendered UNDER the masthead (z-order behind `.map-topstack`) and hid the one button a
> stranded captain needs — the #262 stranding proved it."*

The original adrift strip sat at `top:0.75rem` **as a child of `.map-topstack`** (`Map.razor.css:135-146`,
`z-index: 24`). Because `.map-topstack` is a positioned element with a `z-index`, it forms a **stacking
context** — nothing inside it can paint above `z 24` relative to its siblings. The ship's masthead /
pilot-in-command banner, painting later in that same context, came down **on top of** the strip. The one
button a dry-tank captain needs, buried under the ship's own nameplate. That is what "barely clickable"
looked like: the affordance was *there*, hit-testing just landed on the nameplate instead.

`#266` reacted by relocating the affordance to a bottom-centre pill (`.map-adrift`, `bottom:4.5rem`) and
raising it to `z-index: 30` — enough to clear the *then-known* base HUD (topstack 24, HUD 22, desk-shield
15). But **z-30 was a hand-tuned value, not a reserved band.** Nothing stopped the next pointer-events
overlay authored into the 31…1320 range from re-burying it, and nobody would notice until a playtest went
dry in exactly the wrong state. The real defect is not one z-value — it is that **reachability was left to
be re-verified by eye, every time.** This lab makes it a law.

**The fix (this PR):** `.map-adrift` moves into a reserved **distress-lifeline band, `z-index: 1340`** —
above every non-rescue overlay the state machine can raise (desk/deck pop-ups top out at 1320), just below
the rescue modal it opens (`.rescue-backdrop`, `z-index: 1360`). The lifeline is now, by construction, the
last thing anything may paint over. `src/SpaceSails.Client/Pages/Map.razor.css:337-362`.

---

## 2. The overlay stack, as the game actually paints it

`SpaceSails.Core.RescueLifeline` encodes the real Map HUD as data — every rectangle and z-index
transcribed from `Map.razor.css` at a cited line (heights are the one estimate; kept generous so the
audit errs toward *finding* overlaps). Section A of the probe prints the out-of-power stack:

```
A. THE OUT-OF-POWER OVERLAY STACK  (viewport 1280x800, y grows downward)
   layer                          z  hit-rect  x, y, w, h (px)     pointer-events
   --------------------------------------------------------------------------
   .map-dest-panel               12  336, 644, 608, 144            auto
   .map-dossier                  20  400, 676, 480, 112            auto
   .map-scope                     0  988, 508, 280, 280            auto
   .desk-layer                   15  0, 0, 1280, 800               auto
   .map-adrift-reopen          1340  520, 694, 240, 34             auto
```

The three bottom-centre panels (`dest-panel`, `dossier`, `scope`) share the lifeline's anchor zone and
overlap its footprint — but all sit **below** it in z-order, which is exactly the invariant to hold.

---

## 3. How could CI catch "barely clickable"? — four approaches weighed

```
B. HOW COULD CI CATCH 'BARELY CLICKABLE'?  — four approaches weighed

   approach                      catches                                       CI cost today
   ------------------------------------------------------------------------------------------------
   (a) bUnit render-tree         control present/enabled in a state            HIGH: no bUnit today; +pkg, +Client ref
   (b) geometry law + registry   occlusion, z-order, off-viewport, tap size    LOW: pure C# in Core, existing xUnit
   (c) CSS z-index/overlay audit raw z-order inversions in the stylesheet      MED: needs a CSS parser in the pipeline
   (d) Playwright hit-testing    the real truth: elementFromPoint in a browser VERY HIGH: browser stage, headless Chromium

   (a) bUnit render-tree         blind to: geometry: overlap, z-order, viewport
   (b) geometry law + registry   blind to: runtime CSS not in the registry (hand-sync)
   (c) CSS z-index/overlay audit blind to: footprints, viewport, which states co-occur
   (d) Playwright hit-testing    blind to: nothing — but only the states the script drives
```

The detail behind the one-liners:

- **(a) bUnit render-tree assertions.** Render `Map` with bUnit and assert the rescue `<button>` is in
  the tree and not `disabled` in each distress state. Genuinely useful for *presence/enabled*, and it is
  the natural home for "is the affordance even rendered when `Adrift`?". But bUnit renders to a DOM
  **without layout** — it has no box model, no computed z-index, no viewport. It **cannot** see that a
  higher panel paints over the button, or that it fell off the bottom edge. And there is **no bUnit in
  this repo today**: adopting it means a new test project referencing `SpaceSails.Client`, the bUnit +
  AngleSharp packages, and component-fixture scaffolding. High cost, and it still misses the geometry that
  is the actual bug.

- **(b) A critical-controls registry checked against layout laws.** The `#253 MenuLayout` pattern
  generalized: the control and every co-raisable overlay are rectangles + z-indexes (the registry), and a
  pure-geometry law (`OverlayLayout`) measures how much of the control's hit-rect survives on-screen and
  clear of every higher pointer-events layer. Catches occlusion, z-order inversion, off-viewport, and
  sub-tap-target slivers. Its one blind spot: it audits a **hand-transcribed** copy of the CSS, not the
  live stylesheet, so a CSS edit that is *not* mirrored into the registry escapes — the same "rerun the
  probe" discipline every lab already lives by. **Cost is near zero:** pure C# in `Core`, run by the
  xUnit suite that already gates every build.

- **(c) A z-index / overlay CSS audit.** Parse `Map.razor.css` and flag raw z-order inversions. Cheap-ish
  to state, but a stylesheet has **no footprints, no viewport, and no notion of which elements are on
  screen at once** — it would drown in false positives (every high-z modal "covers" everything) while
  missing the real question: *in this state, at this size, is the lifeline reachable?* Needs a CSS parser
  in the pipeline for a weaker signal than (b).

- **(d) Headless-browser hit-testing (Playwright).** The gold standard: drive real Chromium, put the ship
  in the out-of-power state, and call `elementFromPoint` on the button's centre — if it returns another
  element, fail. This catches **everything**, including CSS the registry never modelled. But the cost for
  *this* repo is real: CI today is plain `dotnet build` / `dotnet test` on GitHub Actions. Playwright
  means a browser download + install step, a published WASM build to serve, a new browser job, and the
  flake budget headless browsers always bring. It only ever checks the states the script explicitly
  drives, so coverage is a maintenance treadmill. Worth it eventually for a broad affordance sweep — not
  worth it to ship the *first* gate this week.

---

## 4. The first regression gate

`OverlayLayout.Evaluate` returns a verdict — `Reachable`, `OffViewport`, `Occluded`, `Disabled`, or the
owner's own word **`BarelyClickable`** (a sliver survives, but smaller than a 24 px fingertip — WCAG
2.5.8). The gate (`tests/SpaceSails.Core.Tests/RescueLifelineTests.cs`) asserts the lifeline is
`Reachable` in the out-of-power state, at every standard viewport size:

```
C. THE GATE — lifeline reachability in the out-of-power state, per viewport
   size        name        verdict       free w x h (px)   free %
   --------------------------------------------------------------
   1280x800    desktop     Reachable     240 x 34          100%
   1024x768    laptop      Reachable     240 x 34          100%
   390x844     phone-tall  Reachable     240 x 34          100%
   844x390     phone-wide  Reachable     240 x 34          100%
   320x480     min-canvas  Reachable     240 x 34          100%
```

100% free at every size — the whole pill, clear. And the gate has **teeth**: it fails the layout that
shipped the original bug, and it proves the reserved band earns its place:

```
D. THE GATE HAS TEETH  (viewport 1280x800)

   pre-#262 buried strip (top:0.75rem, under the masthead z 24):
      verdict = Occluded;  occluded by = [masthead/pilot-banner]
      -> the gate, had it existed, would have failed the build on the reported bug.

   a desk-band pop-up (z 1320, pointer-events) lands over the bottom-centre:
      pill at z 30 (pre-lab)   -> Occluded
      pill at z 1340 (reserved) -> Reachable
      -> the reserved lifeline band is what makes reachability a law, not a hope.
```

The buried-strip row is a **red the gate would have caught**: had this law existed, the #262 stranding
would have failed CI, not a playtest. The desk-band-pop-up row is a **forward guard** — not a state that
co-occurs with adrift *today*, but proof that only an affordance lifted into the reserved band survives an
overlay the old `z-30` could not.

---

## The verdict

**Approach (b) — the critical-controls registry checked by the geometry law — graduates into the
permanent CI suite.** It is the only option that catches the *actual* failure mode (geometric occlusion,
not mere absence) at **zero new infrastructure**: it is `SpaceSails.Core` code, gated by the `dotnet test`
run that already blocks every merge. It ships in this PR.

What it would take to go further, in cost order:

1. **Grow the registry (free, do this next).** Add the other never-hide affordances — the `#212` family:
   the dock button, the pause/abort controls, the busted-modal acknowledge — as named critical controls,
   each with the distress/overlay states it must survive. The law already handles them; they just need
   registry entries.
2. **Close the hand-sync gap (cheap).** The registry transcribes the CSS; a small build check (or a
   source-generator reading the `z-index` values out of `Map.razor.css`) would fail the build when a CSS
   z-index drifts from its registry constant, removing the one blind spot in (b).
3. **Add (a) bUnit for the presence half (medium).** Once a `Client` test project exists, a thin bUnit
   test that the rescue `<button>` renders and is enabled in each distress state complements (b)'s
   geometry — presence and reachability, together.
4. **Add (d) Playwright as a nightly sweep (high, later).** Not a per-commit gate — a scheduled job that
   drives the real build and hit-tests the registry's controls end-to-end, catching the CSS the registry
   never modelled. Justified once the affordance set is large enough that hand-transcription risk
   outweighs the browser-stage cost.

Ship (b) now; it turns "this problem keeps biting us" into a build that goes red the moment it tries to.

## Files

- `src/SpaceSails.Core/OverlayLayout.cs` — the reachability law (pure geometry).
- `src/SpaceSails.Core/RescueLifeline.cs` — the critical-controls registry (real Map HUD, CSS-cited).
- `tests/SpaceSails.Core.Tests/OverlayLayoutTests.cs` — the law's properties.
- `tests/SpaceSails.Core.Tests/RescueLifelineTests.cs` — **the regression gate.**
- `src/SpaceSails.Client/Pages/Map.razor.css:337-362` — the fix (`.map-adrift` → reserved band z 1340).
- `labs/34-the-unclickable-lifeline/Probe.cs` — this lab's probe.
