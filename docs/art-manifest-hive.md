# Image manifest — the Hive (#585, #528)

Art for the underground facility. Each entry names the **slot in code**, the **destination file** under
`src/SpaceSails.Client/wwwroot/art/`, and a **composition spec**. Every slot degrades cleanly
(`onerror`-hide), so the code ships first and the JPG drops in behind it — nothing breaks while a file is
missing.

Raised by the owner, 2026-08-02, playing B1 of a records annex:

> *"I see there is a nice lock here at the end of the corridor.... maybe we could have a gen-AI image for it
> and a pop-up to tell the story?"* — and a minute later — *"the authority card could also have a gen ai
> image to really tell the story here :-D"*

> **House rules for this set:** grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, **no text, no lettering, no logos, no numbers** — every word of the copy is written in code
> and must not be doubled in the pixels. No real likenesses. 16:9.
>
> **And one rule specific to this place:** the pictures may show that the operation was enormous, funded,
> staffed and inspected. They may never show what it produced. Nothing organic, nothing on a slab, nothing
> in a tank. The horror down here is administrative and it has a filing system — see
> `TheHiveTests.NothingDownHereEXPLAINSAnything`, which greps the prose for exactly this and which the art
> must not go around.

## Inference horror — the house technique for this set

Owner, naming it (2026-08-02):

> *"kind of horror theme in lovecraft way ... like finding massive collar designed for Cthulhu's neck :D"*

**The object is mundane. The implication is not.** A collar is a collar — leather, buckles, a maker's stamp.
Nothing about it is frightening; its *dimensions* are the entire horror, and nobody in the frame says so.

This is also the safest way to carry the Reever canon. An object that implies by dimension **cannot** explain
anything — it is a collar — while telling the viewer more than any log entry would. The canon grep passes not
because we were careful with words but because the object is genuinely not talking.

For a picture, that means:

1. **Scale is the subject.** A collar with nothing beside it for scale is a painting of a collar. There must
   be something ordinary in frame — a bench, a doorway, a hand, a standard crate — and the artifact must be
   wrong against it.
2. **Paint it as a catalogue would.** Even light, square on, workshop or store-room. No dutch angles, no
   dramatic uplighting, no fog. The moment the picture is *spooky* it stops being evidence.
3. **Nobody reacts.** No figure recoiling, no torch beam picking it out. If a person is in frame they are
   working.
4. **Show the object, never the user.** No silhouettes, no restraints in use, nothing implied by a shape in
   the dark. The viewer supplies the animal or does not — and **both are fine.** That is what makes it
   horror rather than a puzzle.

## Generation recipe

grok is the project's gen-AI art source (owner ruling 2026-07-18: **images only — no code, no git**).

```bash
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." \
     -m grok-4.5 --permission-mode bypassPermissions
```

- Only model is `grok-4.5`. Requires an authenticated CLI — **`grok update`** refreshes it (the TUI's
  ctrl-u, no browser needed), and it must be run **from PowerShell, not Bash** (different auth state).
- Output is **JPEG bytes** even when the path ends `.png` — always name the file `.jpg`.
- grok **acts in the current working directory and ignores `--worktree`** — generate into the scratchpad,
  inspect, then copy into `wwwroot/art/`.

---

## 1. `art/the-sealed-way.jpg` — THE WAY ON, CLOSED ★ owner's ask

- **Slot:** `UndergroundComplex.SealedWayArtUrl`, raised by `HiveSignInteract` the first time the captain
  presses **E** on a rib's far end (`⟶ SECTOR 9 · 1.4 km`).
- **Why:** it is the best number in the building. A corridor somebody cut 1.4 km into a moon and then
  sealed — the scale of the dig and the decision to close it, in one frame.
- **Composition (16:9):** a corridor of poured concrete and bolted steel receding to a **massive sealed
  bulkhead** that fills the far end, floor to ceiling. The seal is newer than the corridor: its frame is a
  different, cleaner shade than the walls either side. Heavy hexagonal bolt heads around the rim, no handle,
  no wheel, no panel — nothing on it that a hand could use. A small blank stencilled plate at chest height,
  **illegible / no readable characters**. Service conduits run along the ceiling *into* the wall beside the
  seal and stop, cut and capped. Lighting: one working strip overhead near the viewer, the rest dead, so the
  bulkhead is lit indirectly and reads as mass rather than detail. Dust undisturbed on the floor. No people.
  No bodies. Absolutely nothing organic.
- **Feeling:** not a door you failed to open. A decision somebody made and paid for.

## 2. `art/the-authority-card.jpg` — THE COUNTERSIGNATURE ★ owner's ask

- **Slot:** `UndergroundComplex.AuthorityCardArtUrl`, raised by `HiveHaulInteract` on the first
  `Haul.Key` the captain turns up.
- **Why:** the card is the one object down here that still *works*, and it works because an office that
  denies existing never got round to revoking it. That is the whole tone of the facility in a hand prop.
- **Composition (16:9):** a worn identity card lying on a steel bench under a work lamp, held at a slight
  angle by a gloved hand at the edge of frame. Laminate over a metal core, corners rounded by years in a
  pocket, one deep scratch across the face. It carries a **small portrait photograph** of a person
  photographed institutionally — flat light, plain background, not smiling — kept small and slightly out of
  focus so no likeness reads. Below it, the ghosts of a **stamped grade block and two countersignature
  boxes**, both filled, rendered as ink texture and pen-pressure only — **no legible letters or numbers
  anywhere in the image.** An embossed seal catching the lamp at a low angle. Background: the dark of an
  emptied records room, one rank of shelving just visible and out of focus.
- **Feeling:** a staff pass for a job nobody will admit was a job. Bureaucratic, not sinister-looking —
  the sinister part is that it is still valid.

---

## Not for this set

The **descent card** (`art/the-descent.jpg`, `UndergroundComplex.DescentArtUrl`) is a #585 slot that
predates this manifest and is already wired; it is listed here only so nobody paints it twice.

## 3. `art/death-underground.jpg` — DIED IN A SECRET LAB ★ owner's ask

- **Slot:** `DeathNarration.ArtFile(cause, DeathPlace.Underground)` — every death on a Hive floor.
- **Why:** owner, having suffocated on B2 and been handed the away-team card: *"now we have the suffocated
  on surface one :-D"* and *"let's make a died in a secret lab photo also :-D"*. The red-shirt card is a
  figure on regolith with a sky over it, which is the one thing this death does not have.
- **Composition (16:9):** a suited body slumped against the wall of a poured-concrete corridor, seen from a
  few metres. Dark visor. An open, empty satchel beside them. Drag marks through undisturbed dust. A heavy
  sealed bulkhead in the gloom behind. One working light strip; everything else dark. **No blood, no
  violence, no creature** — this is suffocation, quiet and administrative.
- **Painted 2026-08-02.** Clean of lettering.

## 4. `art/the-dead-air.jpg` — THE FIRST DEAD FLOOR ★ owner's ask

- **Slot:** `UndergroundComplex.VacuumArtUrl`, raised once per excursion the first time the captain steps
  out onto a floor that does not hold pressure.
- **Why:** *"there should be a warning or something :-D"* · *"maybe pop-up about you have air or you are in
  vacuum type ... it is vital info"* · *"on surface there are emergency shelters :-D"*. The rule was being
  announced in a pulse that fades in eight seconds, between one about bench hardware and one about dust.
- **Composition (16:9):** looking down a long dark concrete corridor. **The air is gone and the picture has
  to say so without a word** — fine dust hanging perfectly still in a suit-lamp beam, frost rimed along one
  wall seam, emergency lighting glowing uselessly on a circuit nobody has paid for. No people, nothing
  organic. Cold, still, enormous: a place built for people and containing none.
- **Painted 2026-08-02.** Clean of lettering.
