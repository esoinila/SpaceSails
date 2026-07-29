# Image manifest — the visions, and the ship that vented herself

Six canvases: the tenth wreck cause, and the five things an archive node gives back. See
[features/the-archive-node.md](features/the-archive-node.md) for what they are for.

> **The rule for this set, inherited from the wrecks: the art shows the EVIDENCE, never the conclusion.**
> Racked jars, not a caption saying *you are the collateral*. And for the handlers specifically — **the
> image must not resolve what they are.** The moment a canvas answers that question the feature has spent
> something it cannot get back.

House style for the **wreck** canvas: as [art-manifest-wrecks.md](art-manifest-wrecks.md) — grimy
lived-in used-future sci-fi, muted desaturated palette, painterly, cold, dead, **no fire, no working
lights** (this cost three passes on the mutiny canvas; do not relearn it).

House style for the **visions**: deliberately DIFFERENT. These are not rooms the captain is standing in
— they are what a stored mind made of a place it had no eyes in. Colder, cleaner, larger, wrong in
scale; light that was not installed for people; no grime, no wear, no comfort. Where the wrecks are
cramped and human, the visions are enormous and orderly. **No text, no lettering, no faces.**

Destination: `src/SpaceSails.Client/wwwroot/art/`. Every slot `onerror`-hides.

---

## 1. `art/wreck-vented.jpg` — she was opened to space by one of her own

> The interior of a long-dead spaceship seen through an open compartment doorway: everything beyond the
> frame is hard vacuum, frost-white, absolutely still, contents lifted slightly off the deck and frozen
> in place where the air left. In the foreground, on this side of the door, one small compartment that
> still has air — a chair, a jacket over its back, a mug, dust settled flat and normal. A heavy manual
> valve board on the bulkhead with every single handle pulled down to the same position, and a ship's log
> book left open beside it. The doors were closed from THIS side. Grimy lived-in used-future sci-fi,
> muted desaturated palette, painterly, cold, no fire, no flames, no working lights, no text, no
> lettering, no people, no bodies.

## 2. `art/vision-rows.jpg` — the rows

> An impossibly long cold-storage hall seen down its own centre line: racks of sealed cylindrical vessels
> going away in both directions past where the light reaches, stacked to a ceiling that is not visible.
> Perfectly regular, perfectly clean, frost on the rails, every vessel with a blank metal tag and no
> writing on it. Vast, orderly, and utterly silent. Enormous scale, cold blue-grey palette, painterly,
> architectural, light with no obvious source, no text, no lettering, no people.

## 3. `art/vision-handlers.jpg` — the handlers ★ *must not resolve*

> Three tall many-jointed silhouettes working together at a rack of sealed vessels in a vast cold
> archive, seen from a distance and mostly in shadow — long limbs, too many joints, unhurried, leaning in
> toward one vessel as if listening to it rather than looking at it. They are handling it with obvious
> care. Deliberately unresolved and ambiguous: it must be impossible to tell whether they are people in
> articulated rigs, remote waldos, or something else. Backlit, silhouetted, out of focus, no clear
> anatomy, no face, no eyes. Cold blue-grey palette, painterly, enormous quiet space, no text, no
> lettering.

## 4. `art/vision-intake.jpg` — the intake

> A cheap medical intake bay remembered wrongly: one gurney, and above it a scanning rig lowering into
> place — but the rig has TWO identical heads where there is only one patient, and two identical sets of
> cabling running away in different directions, one toward a filing wall and one toward a billing
> terminal. Immaculate, bureaucratic, brightly and flatly lit, nobody in the room. Cold clean palette,
> painterly, unsettling symmetry, no text, no lettering, no faces, no people.

## 5. `art/vision-wintering.jpg` — the wintering

> Black water with no surface anywhere, going down forever, and suspended in it at enormous distance
> something the size of a city district, keeping perfectly still — a vast dark structure that might be
> grown and might be built, its outline never fully resolving in the murk. Faint regular points of light
> across it in no pattern anyone designed. Overwhelming scale, near-black palette with the faintest cold
> blue, painterly, no text, no lettering, no creature clearly visible.

## 6. `art/vision-your-rack.jpg` — the rack with your number

> One sealed cylindrical storage vessel in extreme close-up in a vast cold archive, frost on its collar,
> a stencilled serial number on the collar band rendered as indistinct worn characters. The vessel is
> occupied — a dark shape suspended inside, not clearly visible. Behind it the racks recede away into the
> dark for kilometres. Intimate and enormous at once. Cold blue-grey palette, painterly, illegible worn
> stencilling only, no readable text, no lettering, no face.

---

## Generation recipe

```bash
cd <scratchpad>
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." \
     -m grok-4.5 --permission-mode bypassPermissions
```

`grok-4.5` is the only model. Needs an interactive `grok login`. grok acts in the CWD and ignores
`--worktree`: generate to the scratchpad, eyeball each one, then copy into `wwwroot/art/`.

## Status

| # | Slot | File | Code wired | Art |
|---|---|---|---|---|
| 1 | `WreckCause.VentedByOneOfTheirOwn` | `wreck-vented.jpg` | ✅ | ✅ |
| 2 | `archive-rows` | `vision-rows.jpg` | ✅ | ✅ |
| 3 | `archive-handlers` | `vision-handlers.jpg` | ✅ | ✅ |
| 4 | `archive-intake` | `vision-intake.jpg` | ✅ | ✅ |
| 5 | `archive-wintering` | `vision-wintering.jpg` | ✅ | ✅ |
| 6 | `archive-your-rack` | `vision-your-rack.jpg` | ✅ | ✅ |

"Code wired" here means Core names the file and the client's existing `onerror`-hide slot will show it
the moment it exists; the node's own client lane (the field, the card, the switch) is still to come.
