# Image manifest — the surface pass (selfies, the canon ground, the missing tees)

Art the grok image lane generates. Each entry names the **slot in code**, the **destination file** under
`src/SpaceSails.Client/wwwroot/art/`, and a **composition spec**. Every slot already degrades cleanly
(`onerror`-hide, or a CSS gradient fallback), so the code is wired *first* and the JPG drops in behind it —
nothing breaks while a file is missing.

Raised by the owner, 2026-07-28: *"the miranda selfie should have reevers approaching and even being shot at
by the autocannon… now it is kind of lame. The T-shirts etc everywhere where they are missing."*

> **House rules for this set:** grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, **no text, no lettering, no logos** (the copy is written in code and must not be doubled in
> the pixels). No real likenesses. Homage, never reproduction. 16:9 for scene art, 1:1 for merchandise.

## Generation recipe

grok is the project's gen-AI art source (owner ruling 2026-07-18: **images only — no code, no git**).

```bash
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." \
     -m grok-4.5 --permission-mode bypassPermissions
```

- `grok-build` **no longer exists** — the only model is `grok-4.5` (verify with `grok models`).
- Requires an authenticated CLI: `grok login` (interactive; the owner must run it).
- Output is **JPEG bytes** even when the path ends `.png` — always name the file `.jpg`.
- grok **acts in the current working directory and ignores `--worktree`** — never point it at the primary
  checkout while work is in flight. Generate to the scratchpad, inspect, then copy into `wwwroot/art/`.

---

## 1. `art/selfie-monolith.jpg` — THE FIRST-CONTACT SELFIE ★ owner's ask

- **Slot:** `OfferSelfie(SelfieBeats.FirstMonolith, …)` in `Map.Surface.cs`; rendered as `.selfie-vista`
  behind the captain's portrait disc in `Map.razor`.
- **Why:** the beat shipped passing **no vista at all**, so the marquee once-in-a-life shot was a portrait
  floating on an empty stage. This is the "kind of lame" the owner named.
- **Composition (16:9):** Miranda's canon ground at the monolith maze. Deep background: the **monolith** —
  a vast, too-regular black slab, far older than anything human, lit only along one edge. Middle distance:
  three or four **Old Ones** shambling in out of the regolith haze toward the viewer, low and wrong-jointed,
  silhouetted, unhurried. Foreground right: the tube-mouth **autocannon (GATE-1)** mid-burst — hard muzzle
  flash, tracer streaks crossing the frame toward the pack, spent casings, the light throwing everything
  else into hard shadow. Bottom-left third deliberately **empty and darker**: that is where the captain's
  portrait disc composites in, so nothing important may live there.
- **Mood:** the most dangerous photograph anyone ever stopped to take. Absurd vanity in front of real death —
  the joke is that the captain paused for this.
- **Prompt:**
  > A wide cinematic view across a grey airless moon's regolith at the foot of a vast, impossibly regular
  > black monolith standing far in the background, only one edge catching light. In the middle distance,
  > four hunched ancient humanoid figures shamble toward the viewer through low dust haze, silhouetted, slow
  > and wrong-jointed. On the right foreground an automated turret fires — hard muzzle flash, bright tracer
  > streaks cutting left across the frame toward the figures, spent casings in the air, harsh raking shadows
  > thrown from the flash. The lower left of the frame is empty dark ground. Grimy lived-in used-future
  > sci-fi, muted desaturated palette, painterly, moody lighting, no text, no lettering, no people's faces.

## 2. `art/selfie-reveal-survived.jpg` — STILL STANDING

- **Slot:** `OfferSelfie(SelfieBeats.RevealSurvived, …)` in `Map.SecretLab.cs`.
- **Why:** same fault — this beat also shipped with no backdrop.
- **Composition (16:9):** the Vantar secret lab's cold room just after the reveal. A ruptured containment
  cylinder, frost blooming off the pipes, one failing amber emergency lamp, something's *shape* implied in
  the dark behind the glass and never resolved. Lower-left third kept dark and empty for the portrait disc.
- **Mood:** the room is worse than what happened in it. You are still standing; the room is not.
- **Prompt:**
  > The interior of an abandoned buried laboratory immediately after something went wrong: a cracked
  > containment cylinder, frost blooming across pipes and floor, a single failing amber emergency lamp, and
  > an unresolved dark shape suggested behind fogged glass. Wide cinematic framing, lower left of frame dark
  > and empty. Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody lighting, no
  > text, no lettering.

## 3. `art/treasure-miranda.jpg` — THE MISSING TREASURE CARD

- **Slot:** `TreasureMapArtCss(map.BodyId)` → `.tm-art` in `Map.razor`. Falls back to a plain CSS gradient
  today, which is why nobody noticed.
- **Why:** every other landable body has a `treasure-<bodyId>.jpg` — callisto, enceladus, europa, ganymede,
  luna, phobos, titan. **Miranda, the canon story body, is the only one without one.**
- **Composition (16:9):** Miranda's ground from a low angle — the shattered cliff-country the moon is famous
  for, the monolith maze's outer wall running off frame, a scuffed patch of regolith where something was
  buried. Keep the centre-frame uncluttered: the card overlays a large ✗ there.
- **Prompt:**
  > A low wide view across the broken cliff-country of a small icy moon, enormous fractured escarpments
  > receding into black sky, a low ancient wall of too-regular dark stone running off the left of frame, and
  > a scuffed disturbed patch of pale regolith in the middle distance. Uncluttered centre of frame. Grimy
  > lived-in used-future sci-fi, muted desaturated palette, painterly, moody lighting, no text, no lettering.

## 4. `art/souvenir-surface-tshirt.jpg` — THE KIOSK TEE ★ owner's "T-shirts where they are missing"

- **Slot:** *not yet built.* The surface **🛒 SOUVENIR KIOSK** (`MoonSurface.cs`, sold in `VisitKiosk()`)
  sells a per-moon tee — `SurfaceSouvenir.TeeItem` / `TeeGag` — but prints it as a **pulse message with no
  image at all**. Every haven gift shop has a tee *and* a magnet (`HavenInterior.Specs`); the ground kiosk
  has neither. This is the real "missing T-shirt".
- **Plan:** one shared surface tee (the copy is already generated per moon, so the art can be generic — a
  blank pre-war shirt on a kiosk rail), shown in a small purchase card. Miranda may later earn its own.
- **Composition (1:1):** a cheap grey-white cotton tee hanging on a wire rail in a dusty vending kiosk,
  regolith dust in the creases, the print faded and **illegible** (no readable text — the gag is written in
  code), harsh single overhead strip light, vacuum-sealed packets behind.
- **Prompt:**
  > A cheap faded grey-white cotton t-shirt hanging on a wire rail inside a dusty automated vending kiosk on
  > an airless moon, fine pale dust settled in the creases, the chest print completely worn away and
  > illegible, harsh single overhead strip light, vacuum-sealed packets stacked behind. Square composition.
  > Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody lighting, no text, no
  > lettering, no logos.

## 5. `art/souvenir-surface-magnet.jpg` — the kiosk magnet (optional, matches the havens)

- **Slot:** as above — only if the kiosk card ships with a second shelf item.
- **Why:** every haven shop is a tee **and** a magnet since the owner's 2026-07-19 note (*"The eye bar has
  two T-shirts and no magnets :-D"*). The ground kiosk should not regress to a single item.
- **Composition (1:1):** a small enamelled souvenir fridge magnet lying on a scuffed steel counter, shaped
  like a cratered moon disc, the paint chipped, no readable lettering.
- **Prompt:**
  > A small chipped enamelled souvenir fridge magnet shaped like a cratered grey moon, lying on a scuffed
  > steel counter under hard light, paint worn at the edges. Square macro composition. Grimy lived-in
  > used-future sci-fi, muted desaturated palette, painterly, moody lighting, no text, no lettering.

---

## Status

| # | File | Code wired | Art generated |
|---|---|---|---|
| 1 | `selfie-monolith.jpg` | ✅ | ✅ |
| 2 | `selfie-reveal-survived.jpg` | ✅ | ✅ |
| 3 | `treasure-miranda.jpg` | ✅ (dynamic path, gradient fallback) | ✅ |
| 4 | `souvenir-surface-tshirt.jpg` | ✅ (kiosk card built) | ✅ |
| 5 | `souvenir-surface-magnet.jpg` | ✅ (kiosk card built) | ✅ |

**Complete.** All five generated with `grok-4.5` and shipped. The kiosk purchase card was built in the same
pass (`_kioskCard` in `Map.Surface.cs`, rendered in `Map.razor` on the existing `view-object` idiom), so the
walked ground finally has a gift shop that shows you what it sold you — the tee and the magnet the haven
shops have had since #367.

Every slot still `onerror`-hides, so a future body-specific override can drop in over the shared art without
a code change.

---

## #528 round two — two beats on the ground that were only ever a sentence

### `art/lab-door-regolith.jpg` — A SEALED DOOR, BURIED FLUSH WITH THE REGOLITH

- **Slot:** `SecretLab.DoorPlate`, raised in `Map.SecretLab.TrySecretLabDetectorReveal`.
- **Why it needed one:** it is the only find in the beach-comber lane that is not a thing you pick up, and
  the decision it opens — *force it, or walk away and pretend you never found it* — is one of the sharpest
  in the game. It was being offered over a toast that fades in a second and a half.
- **Discipline:** the picture shows the door and NOTHING about what is behind it. There is no marking on it,
  which is the point: an unmarked door is what you get when the marking would have been the crime.

> **Prompt used:** A heavy armoured hatch buried flush with the grey regolith of an airless moon, most of it
> still under decades of undisturbed dust, one corner swept clean by hand to reveal machined steel and a
> ring of massive locking dogs. Completely unmarked — no plate, no stencil, no marking of any kind. A probe
> rod stands upright in the dust beside it and a shallow scrape trails away. Empty plain and hard black sky
> beyond. Grimy lived-in used-future sci-fi, muted desaturated palette of dust brown and cold steel,
> painterly, moody, low raking light, long shadows, no text, no lettering, no numbers, no logos, no readable
> writing, no people.

### `art/outpost-effects.jpg` — WHAT WAS LEFT ON THE FLOOR

- **Slot:** `SurfaceOutpost.EffectsPlate`, raised in `Map.Outpost.OutpostEffectsInteract` beside the filed
  line and the dossier kit.
- **Why it needed one:** the console in the game most obviously ABOUT a person, and the only picture it had
  was the dossier's own (`dossier-effects.jpg`, which is the dossier, not the floor).
- **ONE canvas for all four covers**, on the wrecks' anti-tell law (`Derelict.LogArtFile`): the objects are
  the same four objects in every hut, and `SurfaceOutpost.EffectsLine` already carries the whole of the
  difference between a survey hut, a relay shack, a sample store and a clinic.
- **Canon-bound:** it implies an industry that went dark and a state worth hiding from, and says nothing at
  all about what is walking around outside.

> **Prompt used:** The contents of a dead stranger cleared out of a wallet and laid out on the cold gritty
> floor of a small sealed hut on an airless moon, seen from directly above in the beam of a dropped helmet
> lamp: a worn leather wallet fallen open and empty, a blank laminated card on a snapped fabric lanyard, a
> folded sheet of paper creased into quarters and turned blank side up, a creased photograph lying FACE DOWN
> showing only its blank grey back, a few plain unmarked metal tokens. Every surface in frame is completely
> blank — no printing, no barcode, no photograph of a person, no marking of any kind anywhere. Dust and
> frost at the edges of the frame. Grimy lived-in used-future sci-fi, muted desaturated palette of grey and
> faded brown, painterly, moody low-key lighting, no text, no lettering, no numbers, no logos, no barcodes,
> no readable writing, no faces, no people.

- **A second pass was needed here.** The first came back with a photo-ID badge carrying a face and a
  barcode — the one thing this set is not allowed to have. Naming the blankness of every surface
  *individually* ("turned blank side up", "lying FACE DOWN showing only its blank grey back") is what fixed
  it; a blanket "no text" at the end of the prompt is not enough when the prompt has asked for documents.
- **Painted 2026-08-03.** Both guarded by `RevealPlatesArePaintedTests.EveryBeatPlateIsPaintedAndSaysItOnce`.

### `art/collector-boat-down.jpg` — THEY DID NOT COME FOR THE SHIP

- **Slot:** `CollectorLanding.ArrivalPlate`, raised in `Map.Surface.LandTheCollectors`.
- **Why it needed one:** four loaded lines narrate this pursuit and every one of them was a toast. The
  arrival is the worst place for that — it is the ONLY warning the player gets, and after it the only
  information in the world is a tracker fan. A sentence that fades in a second and a half is not a warning.
- **The caption is `ClosingLine`**, which until this pass was written, reviewed, shipped and **referenced by
  nothing at all**. It is exactly the right words for this picture, so this is where it finally gets said.
  Split into `ClosingCaption` on the `Derelict.LogCaption` idiom (one text, two readers, one glyph).

> **Prompt used:** A second small landing boat set down hard on grey regolith a few hundred metres from the
> viewer on an airless moon, landing lights off, no beacon, its ramp already down and a wedge of interior
> light spilling onto the dust. Four suited figures are walking out from it, spread wide apart in a loose
> line, unhurried, coming toward the viewer across an empty plain. Long low shadows, a hard black sky. Grimy
> lived-in used-future sci-fi, muted desaturated palette of dust grey and cold blue, painterly, moody, no
> text, no lettering, no numbers, no logos, no readable writing, no visible faces.

### `art/collector-siege.jpg` — THEY SETTLE IN TO WAIT

- **Slot:** `CollectorLanding.SiegePlate`, raised in `Map.Surface` beside `ShelterIsNotSanctuaryLine`.
- **The one rule this frame lives by:** it shows them **settled, not attacking**. Nothing in it is a fight.
  The clock is your tank, and what makes it horrible is how comfortable everyone else looks. A refuge that
  silently fails to be one is the worst thing a survival game can do, and the picture has to say so as
  plainly as the line already does.

> **Prompt used:** The view from inside a small pressurised surface shelter on an airless moon, looking out
> through a thick scratched round viewport at grey regolith. Outside, three suited figures have taken up
> positions in a loose arc on the dust and settled down to wait — one seated on a rock, one standing with
> arms folded, one crouched — patient, in no hurry at all, facing the shelter. The inside of the port is
> fogged at its edges with breath frost. Grimy lived-in used-future sci-fi, muted desaturated palette of
> dust grey and dim amber interior light, painterly, moody, no text, no lettering, no numbers, no logos, no
> readable writing, no visible faces.

- **Painted 2026-08-03**, first pass each. Guarded by
  `RevealPlatesArePaintedTests.EveryBeatPlateIsPaintedAndSaysItOnce`.
