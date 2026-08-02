# Image manifest — the wrecks (#488)

> **A tenth cause has since landed** — `VentedByOneOfTheirOwn`, the ship one of her own opened to space.
> Her canvas lives in [art-manifest-visions.md](art-manifest-visions.md) with the archive-node visions,
> because she is that feature's wreck. Same rules as this set.

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
| 1 | DriveFailure | `wreck-drive-failure.jpg` | ✅ | ✅ |
| 2 | ReactorCascade | `wreck-reactor-cascade.jpg` | ✅ | ✅ |
| 3 | HullBreach | `wreck-hull-breach.jpg` | ✅ | ✅ |
| 4 | LifeSupportFailure | `wreck-life-support.jpg` | ✅ | ✅ |
| 5 | NavigationalError | `wreck-navigational-error.jpg` | ✅ | ✅ |
| 6 | Mutiny | `wreck-mutiny.jpg` | ✅ | ✅ (3rd pass) |
| 7 | Piracy | `wreck-piracy.jpg` | ✅ | ✅ |
| 8 | Infested | `wreck-infested.jpg` | ✅ | ✅ |
| 9 | InsuranceJob | `wreck-insurance-job.jpg` | ✅ | ✅ |

**All nine painted and shipped, 2026-07-29.**

## 10. `art/death-derelict.jpg` — DIED ABOARD SOMEBODY ELSE'S SHIP (#621)

- **Slot:** `DeathNarration.ArtFile(cause, DeathPlace.Derelict)` — every death inside a hull.
- **Why:** #574 gave the derelict its own prose and its own tail and then left it the away team's PICTURE.
  A captain killed deep inside a wreck was shown `death-reevers.jpg` — boot prints in regolith, an open
  chest, an Earth in a grey sky — under the pool's own sentence, *"No dust to leave a mark in — just a
  corridor, and then not you."* The prose denied the dust the picture was made of. `Joined` had the same
  fault (a crowd of Old Ones standing on a moon), and `Suffocated` resolved to `death-suffocated.jpg`, a
  file this game has never shipped: a broken image in the middle of a death card.
- **Composition (16:9):** a suited body slumped sitting against the wall of a derelict's interior corridor,
  seen from a few metres. Dark mirrored visor. Riveted steel bulkhead ribs, exposed conduit, a heavy sealed
  pressure door with a wheel handle in the gloom behind. Frost rimed along the plating seams. A dropped tool
  and a loose glove hanging **weightless** — she has no gravity and no air, and the picture has to say both
  without a word. One faint smeared handprint in the grime above the body: it reads as a suffocation OR as
  something that had you against the bulkhead, and the card never says which. **No sky, no stars, no window,
  no regolith, no blood, no creature.**
- **Painted 2026-08-02.** Clean of lettering.

### The nest, before and after (`vented-nest-intact.jpg` / `vented-nest-dead.jpg`)

Both were painted long ago; only the **after** was ever wired. `vented-nest-intact.jpg` — the nest alive,
grown up over the racks, its mouths open — sat in `wwwroot/art` with nothing in the codebase pointing at
it, which meant the *setup* for this hull's best payoff was missing while the picture of it was on disk.
Wired by #528: the before-card raises once per boarding, the first time the captain stands in the nest
compartment while the sim says it is still infested and still holding air.

- **Slots:** `NestPlates.Live` / `NestPlates.Dead` (Core — the after-card's copy used to be a literal in
  `Map.Venting`), raised in `CheckVentPayoffUnderfoot`.
- **Why the pair matters:** #380's law — *an event must introduce its fiction one beat earlier.* "What the
  vacuum left" only lands as hard as it does if you saw what was there before it left.
- **Guarded:** `NestPlatesTests` — both painted, the two pictures and captions genuinely different (a pair
  that showed the same image would say the vacuum changed nothing), and **neither plate names what made
  it.** Same law as `TheHiveTests.NothingDownHereEXPLAINSAnything`, one deck up.

### Still unpainted in this family

- `art/death-suffocated.jpg` — the name `ArtFile(cause)` carried for a year with nothing behind it. Now
  unreachable (every away place answers before it), so it is a *nice-to-have*, not a gap: a card for a
  suffocation with no place named at all.

### What the set taught us

- **Never say "torch."** The mutiny prompt asked for an arms locker *"opened with a cutting torch"* and got, twice,
  a **burning torch** mounted on the wall of a ship that has been cold and airless for decades — the one thing
  that contradicts every other canvas in the set. Third pass said *"forced open with a plasma cutter long ago,
  its cut edges cold and blackened"* plus an explicit *"no fire, no flame, no burning, no torches, no candles,
  no glowing embers, no working lights"* and came back right. Reject-for-continuity is worth the extra pass:
  the wrecks share a world, and one lit flame breaks all nine.
- **Ask for evidence and the model finds its own.** Nobody asked for the abandoned chess game in the
  life-support berth, or the second workings circled in a different hand on the navigation plan. The prompts
  that describe a *situation* rather than a *composition* come back with details worth keeping.
- The nine went in one batch of five plus four earlier, no retries except mutiny.

> Sidelight: `wreck-insurance-job.jpg` is a lifeboat bay with every cradle empty and the clamps neatly stowed
> — which is exactly the scene the owner described the same morning as a whole feature. See
> [features/safety-card.md](features/safety-card.md).
