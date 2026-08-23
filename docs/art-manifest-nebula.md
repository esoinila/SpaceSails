# Image manifest — NEBULA MUTUAL and the CONVERGENCE (#422, #528)

Arc 2 shipped with **no art of its own and no manifest.** The best-written character in the game — the
adjuster who has filed the same subscriber six times and shaken six certain hands — had no face, while a
routine collector shakedown had a painted portrait (`busted-collector-hail.jpg`). And the CONVERGENCE, the
biggest reveal in the game, was a text div.

That inversion is the whole of #528's complaint, and this is the file that answers it for arc 2.

> **House rules for this set:** grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, **no text, no lettering, no logos, no numbers, no readable writing** — every word of the
> copy is written in code and must not be doubled in the pixels. No real likenesses. 16:9.
>
> **And one rule specific to this arc.** NEBULA's horror is **clerical**. It is a company that files you.
> Nothing in these pictures may show a body, a tank, a slab, or anyone reacting to anything — the moment
> the image is *spooky* it stops being evidence and becomes a haunted house. What it may show is a counter,
> a bell, a stamp, a stool, shelving, and the fact that the shelving does not stop. The dread is that the
> paperwork is normal and the aisle behind it is not.

## Where each plate goes

Plates are raised through the shared reveal-card seam (`Map.StoryPlate`, added by the KAAMOS lane — see
[art-manifest-kaamos.md](art-manifest-kaamos.md) for the recipe). The words live in Core beside the
predicate that decides the beat happened (`NebulaLore.PlateFor`), keyed by the same fragment ids the pool
and the gate use.

**Four of the six NEBULA shards get no plate on purpose.** The glitch on the resurrection card, the
poster's grey line, the collector's writ and the clinic's second page all arrive *inside a host card that
already carries a picture* (the BUSTED modal, the poster). A second frame there stacks a card on a card,
which is not service — it is noise. `RevealPlatesArePaintedTests` pins that decision.

---

## 1. `art/nebula-adjuster.jpg` — THE ONE WHO FILES YOU

- **Slot:** `NebulaLore.PlateFor("adjuster-tell")`, raised in `Map.Nebula.AskAboutNebula`.
- **Reach it:** `/map?nebula=adjuster` — dock anywhere, walk to the counter.
- **Idiom:** the bar-patron plate — seen **from across the room**, past out-of-focus shoulders, the way
  every other patron in this game is framed. Not a portrait; he does not get portrait dignity.
- **The evidence in frame:** he is the only person in the bar reading, and the folio fell open at a page
  nobody had to look up. The caption names both and stops. Nothing about what he sells.

> **Prompt used:** A tired insurance adjuster nursing a drink alone at a corner table of a grimy spacer bar,
> seen from across the room past out-of-focus shoulders. Neat pressed grey company coat and a soft collar,
> badly out of place among worn insulated jackets and patched coveralls. A fat dog-eared policy folio open
> on the table beside the glass, its pages blank and smeared with no readable writing, one finger resting on
> a line of it. Head lowered, face in shadow and unresolved. Low amber work-lighting, haze, scratched table,
> riveted bulkhead. Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody, weary, no
> readable text, no lettering, no numbers, no logos, no recognizable face.

## 2. `art/nebula-truth.jpg` — WHAT THE PREMIUM BUYS

- **Slot:** `NebulaLore.PlateFor("policy-terms")` — the capstone, raised alongside `NebulaLore.TruthNotice`
  on the single edge that flips `KnowsTheTruth`.
- **Reach it:** `/map?nebula=4`, then dock at any bar — *"▓ Put the NEBULA small print together"*.
- **The composition IS the fine print.** A claims counter is the most boring object a station has: a
  shutter, a bell, a stamp block, a stool. The horror is one architectural fact — **the office has no back
  wall.** The paper shelves become racks, the racks become cold storage, and the aisle carries on past the
  last light anybody bothered to install. Everything the prose says (*"the premium buys STORAGE, and the
  original never leaves the dark"*) is in the picture without one word of it being said.
- **Nobody is at the counter,** because a clerk would make it a transaction. It is an institution.

> **Prompt used:** A small insurance claims counter in a station concourse, shutter half lowered, a service
> bell and a rubber stamp block on the worn ledge, a clerk's stool empty behind it. The office behind the
> counter has no back wall: it simply continues, and the filing shelves become racks of sealed cylindrical
> cold-storage vessels receding away into total darkness for an impossible distance, frost creeping along the
> rails. The counter is flatly and brightly lit like any dull public office; everything past the third rack
> is black. Nobody present. Bureaucratic, orderly, banal in the foreground and enormous behind, muted
> desaturated palette, painterly, cold, no text, no lettering, no numbers, no logos, no readable writing, no
> people.

## 3. `art/convergence.jpg` — TWO MYSTERIES. ONE TRUTH. ★

- **Slot:** `ArcConvergence.ArtFile`, rendered in the `convergence-card` (`Map.razor`) between the subtitle
  and the body, `onerror`-hiding like every other slot. Fires once per universe from
  `Map.Nebula.MaybeFireConvergence`.
- **Reach it:** `/map?converge=1`.
- **It is a PLATE, not an illustration.** The card's copy is live — the arc's own passes keep moving it,
  and #422 has an open recommendation on the body — so an image that depicted the paragraph would need
  repainting every time a sentence changed, and worse, it would say the thing out loud.
- **What it shows instead is the SHAPE of the reveal:** two entirely different filing systems — one warm,
  wooden, papered, a bureaucracy; one cold, steel, frosted, a cold store — running toward each other down
  one aisle and meeting at a single shared cabinet, the joinery seamless, as though it had always been one
  building. Two mysteries; one truth. **Nobody in the room**, because a figure looking at it would be the
  game telling the captain how to feel about the biggest thing that has ever happened to them.
- **CSS:** `.convergence-img` is capped at `34vh` rather than the view-object's `70vh`. This card is mostly
  prose and the picture is its establishing shot, not its subject — the paragraph has to stay on screen
  with it.

> **Prompt used:** An enormous cold records hall seen straight down its own centre aisle, where two
> completely different filing systems run toward each other and meet. On the left, warm brown wooden ledger
> shelving and bound paper files, a clerk's brass lamp, an old bureaucracy. On the right, cold steel and
> frosted glass racks of sealed cylindrical storage vessels, blue-white, frost on the rails. They converge in
> the middle of the frame on one single shared cabinet that belongs to both, the joinery seamless, as if it
> had always been one building. Nobody in the room. Vast scale, ceiling out of sight, cold blue-grey and
> dust-brown palette, painterly, architectural, quiet, dread, no text, no lettering, no numbers, no logos, no
> readable writing, no people.

---

## 4. `art/nebula-rep-fess.jpg` — HARLAN FESS, OUTER REACHES DESK

- **Slot:** the rep's own pitch card (#973 L2) — not a `RevealPlate`, so no sweep names it; it is the face
  at the top of the panel Harlan raises when he reaches your table.
- **Reach it:** the same walk-across-the-floor as above.
- **Idiom:** the ONE portrait this arc allows, and it is allowed because it is the arc's inversion — the
  adjuster is framed from across the room and denied portrait dignity precisely so that the *salesman* can
  have it. He is delighted to see you. He has never seen you.
- **The arc's clerical rule still holds:** nothing spooky, nothing reacting. The horror is that a man this
  pleased to meet you is holding your file.

> **Prompt used:** Portrait of a relentlessly cheerful middle-aged insurance salesman standing in a grimy
> spaceport concourse bar, mid-pitch, leaning slightly forward with both hands open in a here-me-out gesture,
> big warm practised smile, thinning hair combed over, a slightly too-tight synthetic suit with a company
> lapel pin, a battered document folder tucked under one arm. Behind him out of focus: a dim used-future bar,
> bottles, a wall poster. grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody
> lighting, no text, no lettering, no signage.

## Generation recipe

grok is the project's gen-AI art source (owner ruling 2026-07-18: **images only — no code, no git**).

```bash
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." \
     -m grok-4.5 --permission-mode bypassPermissions
```

- Only model is `grok-4.5`. **`grok update`** refreshes auth, and it must be run **from PowerShell, not
  Bash** (different auth state).
- grok ignores `--worktree` and acts in its CWD: **generate into the scratchpad, look at every one, then
  copy into `wwwroot/art/`.** Everything it saves is a JPG whatever the extension says.

## Status

| # | Slot | File | Code wired | Art | Guard |
|---|---|---|---|---|---|
| 1 | `NebulaLore.PlateFor("adjuster-tell")` | `nebula-adjuster.jpg` | ✅ | ✅ | ✅ |
| 2 | `NebulaLore.PlateFor("policy-terms")` | `nebula-truth.jpg` | ✅ | ✅ | ✅ |
| 3 | `ArcConvergence.ArtFile` | `convergence.jpg` | ✅ | ✅ | ✅ |
| 4 | the rep's pitch card (#973 L2) | `nebula-rep-fess.jpg` | ✅ | ✅ | — |

The signing flashback wears the arc's ONE bleached plate, `art/flashback.jpg` (#973 L1) — it is raised as
`StoryBeats.Beat.Flashback` with the subject `signing`, and no second painting was added for it.

The guard is `tests/SpaceSails.Core.Tests/RevealPlatesArePaintedTests.cs` — it sweeps both arcs' plates,
the convergence, **and every `DeathNarration.ArtFile(cause, place)` pairing**, and holds each to a file that
is actually on disk. That last sweep exists because `death-suffocated.jpg` was named in code for a year
while the game never shipped the file (#621 found it, #636 fixed it): the `onerror`-hide law is what makes
code-before-art safe, and it is exactly what makes a missing painting invisible. Proven RED by restoring
the pre-#636 mapping — the sweep fails naming the file.

## Still unpainted in this arc

- **The clinic** — where every captain in the game wakes up, and it is a bill and a paragraph. See #528's
  ranked list; it wants the second page of the ledger, not the waiting room.
- **The collector's writ** — the shard is a *document*, and it has no document.
