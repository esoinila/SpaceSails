# Image manifest — the wrecks (#488)

Nine ships that died nine different ways. Owner: *"we should have some GEN AI art to really tell the story
of each ship."* Each cause gets its own canvas, shown twice — when the away team reads the **cause's own
station** (you are standing and looking at the thing) and again on the **decision card** (you are deciding
about it, and only once you have actually looked).

> **The rule for this whole set: the art shows the EVIDENCE, never the conclusion.** Empty lifeboat
> cradles, not a caption saying *fraud*. Barricades built from the inside, not a monster. Naming what it
> means is the captain's job, and a wreck that lies must still be able to lie.

House style: grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody lighting,
**no text, no lettering, no readable signage** (all copy is written in code and must not be doubled in the
pixels). No people's faces. Interiors of a long-dead ship, lit by helmet lamps and failing emergency
strips — nobody has been here in decades.

Destination: `src/SpaceSails.Client/wwwroot/art/`. Every slot `onerror`-hides, so an unpainted cause reads
as text alone rather than a broken frame — the code is already wired and shipped.

## Generation recipe

```bash
cd <scratchpad>
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." \
     -m grok-4.5 --permission-mode bypassPermissions
```

`grok-4.5` is the only model (`grok-build` is gone). Needs an interactive `grok login` — the session
expires, so check `grok models` first. grok acts in the CWD and ignores `--worktree`: generate to the
scratchpad, eyeball each one, then copy into `wwwroot/art/`.

---

## 1. `art/wreck-drive-failure.jpg` — the drive quit and she never arrived

> The engineering space of a long-dead spaceship, lit only by a helmet lamp. Cold clean drive bells with no
> scorching at all, fuel gauges still reading full, a maintenance panel opened and never closed, and a
> single crew jacket left over the back of a chair. Nothing is burned or broken — that is the horror.
> Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody lighting, no text, no
> lettering, no faces.

## 2. `art/wreck-reactor-cascade.jpg` — the reactor ran away with her

> The aft end of a spaceship torn open from the inside: shielding peeled outward in petals, structural ribs
> glowing dull with old heat stains, slagged deck plating, a dosimeter still blinking on a bulkhead. Deep
> shadow beyond the breach where the hull simply stops. Grimy lived-in used-future sci-fi, muted desaturated
> palette, painterly, moody lighting, no text, no lettering.

## 3. `art/wreck-hull-breach.jpg` — something small went through her at speed

> The interior of a dead spaceship with a hole the width of a thumb punched clean through four decks in a
> perfectly straight line, stars visible through the far end, every locker along that line blown open from
> the inside by decompression, papers and debris frozen mid-scatter. Grimy lived-in used-future sci-fi,
> muted desaturated palette, painterly, moody lighting, no text, no lettering.

## 4. `art/wreck-life-support.jpg` — the air plant failed and everything else kept working

> A ship's life-support bay, immaculate and intact, every indicator still lit green — except the scrubber
> stacks, which are choked solid with grey crust. Behind it a crew berth with the bunks neatly made up.
> Nothing is damaged. Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody
> lighting, no text, no lettering, no faces.

## 5. `art/wreck-navigational-error.jpg` — the arrival burn was never coming

> A dead ship's navigation post: a flight plan pinned to the console, worked twice in two different
> handwritings to two different answers, dividers and a straightedge left across it, the plotting screen
> dark. Through the bridge window, empty starfield with nothing in it at all. Grimy lived-in used-future
> sci-fi, muted desaturated palette, painterly, moody lighting, illegible handwriting only, no readable
> text, no lettering.

## 6. `art/wreck-mutiny.jpg` — the crew stopped flying her to settle something

> A ship's corridor with two crude barricades facing each other down its length — welded deck plate, strapped
> crates, torn bedding — built by two groups against each other. An arms locker in the wall opened with a
> cutting torch, its edges still bright. Scorch marks on both sides. Grimy lived-in used-future sci-fi,
> muted desaturated palette, painterly, moody lighting, no text, no lettering, no people.

## 7. `art/wreck-piracy.jpg` — she was boarded, stripped in a hurry, and left under way

> A ship's cargo hold stripped to bare frames and empty tie-down rails, cut cargo netting hanging loose,
> the outer airlock door cut open from the OUTSIDE with the cut edges bent inward. Deeper in the shot, a
> second hold still fully loaded and untouched — whoever did this was in a hurry. Grimy lived-in
> used-future sci-fi, muted desaturated palette, painterly, moody lighting, no text, no lettering.

## 8. `art/wreck-infested.jpg` — she is not empty ★ the one with the cannon

> The deep hold of a long-dead ship, helmet-lamp lit: crude barricades built across every doorway from the
> INSIDE, spent shell casings drifted against them, an emptied arms locker — and beyond, in the dark, a
> mass of pale fibrous nesting material grown over the cargo racks and up the bulkheads over many years.
> Deep scratch marks on the deck plating. Something implied in the shadows and never resolved. Grimy
> lived-in used-future sci-fi, muted desaturated palette, painterly, moody lighting, no text, no lettering,
> no clearly visible creature.

## 9. `art/wreck-insurance-job.jpg` — she was lost on purpose

> A ship's lifeboat bay with every cradle EMPTY and the release clamps neatly stowed rather than blown —
> nobody left in a hurry. A cargo seal broken and carefully re-set with fresh wire, a clipboard of manifests
> countersigned twice in the same hand, and a bunk room stripped of personal effects. Everything is tidy,
> which is what is wrong with it. Grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, illegible handwriting only, no readable text, no lettering.

---

## Status

| # | Cause | File | Code wired | Art |
|---|---|---|---|---|
| 1 | DriveFailure | `wreck-drive-failure.jpg` | ✅ | ⬜ |
| 2 | ReactorCascade | `wreck-reactor-cascade.jpg` | ✅ | ⬜ |
| 3 | HullBreach | `wreck-hull-breach.jpg` | ✅ | ⬜ |
| 4 | LifeSupportFailure | `wreck-life-support.jpg` | ✅ | ⬜ |
| 5 | NavigationalError | `wreck-navigational-error.jpg` | ✅ | ⬜ |
| 6 | Mutiny | `wreck-mutiny.jpg` | ✅ | ⬜ |
| 7 | Piracy | `wreck-piracy.jpg` | ✅ | ⬜ |
| 8 | Infested | `wreck-infested.jpg` | ✅ | ⬜ |
| 9 | InsuranceJob | `wreck-insurance-job.jpg` | ✅ | ⬜ |

All nine slots ship wired and degrade cleanly; they are waiting on an authenticated `grok login`.
