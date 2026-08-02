# Image manifest — PR-BUSTED (the catch)

Art the grok image lane generates later. Each entry names the **placeholder slot** in code (a CSS
art box in `Map.razor` today), the **destination file** under `src/SpaceSails.Client/wwwroot/art/`,
and a **composition spec**. Drop the JPG at the path and swap the emoji/gradient placeholder for an
`<img>` (or an `OP_IMAGE` blit) — no code shape changes.

> House rules for this set: **HOMAGE, not reproduction** — our pirates, no film-frame copies, **no
> real likenesses**. Sepia, cinematic, painterly. 4:3-ish to match the existing bar/hall art.

## 1. `art/busted-freeze-frame.jpg` — THE FREEZE-FRAME (Bolivia game-over)
- **Slot:** `.busted-freeze-art` in `Map.razor` (the `BustedEncounter.Stage.FreezeFrame` panel).
- **Composition:** two silhouetted rogues (our pirate captain + first mate) charging shoulder-to-
  shoulder out of a battered airlock into **blinding muzzle-flash light** — the frame blown out
  white-gold at the centre, figures near-black against it, dust and cordite. **Sepia freeze**, held
  like a final still. An homage to Butch Cassidy and the Sundance Kid's last charge — *the spirit,
  not the frame*. No faces resolvable, no real actors.
- **Mood:** defiant, doomed, romantic. The last good run.
- **Caption baked in code (paraphrased wink, not the film line):** *"…and here we thought this was
  the easy money."*

## 2. `art/busted-ship-explosion.jpg` — THE OLD SHIP BLOWING UP (resurrection)
- **Slot:** `.busted-freeze-art.busted-explosion` in `Map.razor` (the `Resurrected` panel).
- **Composition:** the player's old hull erupting — a kerosene-orange fireball against black space,
  debris and a snapped mast/sail spar tumbling, the collector's grapple line going slack. This is
  where "the kerosene-explosion art budget" goes (owner). One clean hero explosion, no HUD.
- **Mood:** total loss, but clean — the brain-backup already fired; this is just the hardware dying.

## 2b. The Bolivia's three beats (#528, painted 2026-08-02)

The freeze-frame above closes a script whose **three beats were text with buttons under them.** A captain
who *held the line* — the good ending — never saw one frame of the stand they had just survived. That is
the #528 inversion in its purest form: the game's most cinematic scene, unillustrated, next to a painted
still of the way it goes wrong.

- **Slot:** `BoliviaEncounter.ArtFile(beat.Id)`, rendered above `@beat.Narration` in the
  `Stage.Bolivia` panel (`Map.razor`), `onerror`-hiding. **Keyed by the beat's id, never its index** — a
  script is a list somebody will edit, and an index would shuffle the pictures out from under the words.
- **Reach it:** get to heat 3 and RESIST.
- **Register:** the freeze-frame's, deliberately — deep sepia monochrome, blown-out highlights, near-black
  silhouettes, heavy grain, no faces resolvable. **These are ACTION shots**, which is the one place in this
  game where that is correct: the Bolivia *is* the charge, and its consequence card already exists at the
  end of the script.
- **CSS:** `.bolivia-beat-img` is capped at `28vh` and cropped, because the three choice buttons under it
  are the reason the card is open. A picture that pushed them off the fold would make the scene worse.

> **`art/bolivia-breach.jpg`** — *"The airlock buckles inward — boarders in the smoke."*
> An inner airlock door buckling inward off its hinges into a cramped ship corridor, hard smoke pouring
> through the gap, backlit silhouettes of boarders pushing through it. Seen from the defenders end of the
> corridor, low and close. Deep sepia monochrome, blinding blown-out white-gold light through the doorway,
> figures near-black against it, dust and cordite hanging in the beam, buckled deck gratings and torn
> conduit. Cinematic film still, painterly, heavy grain, no faces resolvable, no text, no lettering, no
> numbers, no logos.

> **`art/bolivia-crossfire.jpg`** — *"Muzzle-flash fills the deck — nowhere clean to stand."*
> A cramped ship corridor filled end to end with muzzle-flash light and smoke, silhouetted figures at both
> ends firing past each other, nowhere in the frame that is not lit. Spent casings scattered across the deck
> plating, a burst pipe venting white vapour across the middle, pocked bulkheads. Deep sepia monochrome,
> blown-out highlights, near-black silhouettes, cinematic film still, painterly, heavy grain, chaotic, no
> faces resolvable, no text, no lettering, no numbers, no logos.

> **`art/bolivia-run.jpg`** — *"One clear run to their ship — and the door left open."*
> A short exposed stretch of docking gantry between two ships, seen from behind cover at the near end. The
> far hatch stands open and lit warm from inside, and the crossing between is completely bare and raked by
> hard light. Two low crouched silhouettes in the foreground gathered to break for it, one hand on the rail.
> Deep sepia monochrome, blown-out highlights, near-black figures, drifting smoke, cinematic film still,
> painterly, heavy grain, tense, held breath, no faces resolvable, no text, no lettering, no numbers, no
> logos.

Guarded by `RevealPlatesArePaintedTests.EveryBoliviaBeatIsPainted`, which walks
`BoliviaEncounter.Script` — the thing the client actually renders — so **inserting a beat and forgetting
its painting fails the build.** Proven RED by blanking the crossfire slot.

## 3. (optional, later) `art/busted-collector-hail.jpg` — the collector's hail portrait
- **Slot:** could back the `Demand` panel header.
- **Composition:** a hard-eyed repo captain in a cramped cutter cockpit, a debt-ledger glowing, a
  boarding net coiled behind. Menacing but businesslike — they want you taxed, not dead.
- **Priority:** low — the Demand panel reads fine text-only for now.
