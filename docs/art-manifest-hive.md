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
