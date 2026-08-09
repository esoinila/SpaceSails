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

- **Slot:** `UndergroundComplex.AuthorityCardFallbackArtUrl` — since **#695** this is the FALLBACK face, for a
  card whose id nothing can parse. Every card the game actually mints takes its office's own portrait (§2a).
  Originally raised by `HiveHaulInteract` on the first `Haul.Key` the captain turns up.
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

## 2a. Five card faces, one per issuing office ★ owner's ask — #695

Owner, wallet in hand: *"I have 3 ID cards but they all have the same gen AI image."*

He is right and the fix was already half-built: `CardTitle` has rolled one of exactly **five offices** off
`hive:card:{body}:{band}` since #679, and only the picture was a single constant. So there are five pictures
now, keyed to **the same roll** — `UndergroundComplex.OfficeOf(card)` answers once and both the letterhead and
the face read its answer. Not two sums that agree; one record with two fields
(`UndergroundComplex.CardOffice(Letterhead, ArtUrl)`).

- **Slot:** `UndergroundComplex.AuthorityCardArtUrl(card)` — a pure function of the card id, no stored state,
  so a wallet loaded off a save shows the faces it was minted with. Read by the `HiveHaulInteract` reveal and
  by `CarriedObject.Card` (the satchel's look-card).
- **Common brief (all five):** a worn identity card held up in a gloved hand, laminate over a metal core,
  corners rounded by years in a pocket. A **small institutional portrait** — flat light, plain background, not
  smiling — kept small and soft so no likeness reads. Stamps, countersignature boxes and an embossed seal
  rendered as **ink texture and pen-pressure only, no legible letters or numbers anywhere.** House rules §0
  apply: the card may prove the operation was enormous, funded and inspected, never what it produced.
- **Why five rather than one:** a captain carrying three cards can now tell them apart *at a glance* — the
  same job #679's site code does for the wallet list, done in pixels for the card that is open in front of
  them. Each office's personality is the entire point; a set of five near-identical laminates would be the
  original complaint with extra steps.

Painted 2026-08-05. The **as-painted** column is what is in the frame, not what was asked for — a regeneration
works from this, and a spec that describes a picture the folder does not contain is the third named bug class
with a JPG attached.

| # | Office (the roll's order — **do not reorder**, it re-issues every card in every wallet) | File | As painted |
|---|---|---|---|
| 0 | `OFFICE OF WORKS · SUB-REGISTRY` | `art/the-authority-card-works.jpg` | **The grubby one.** A works pass lives in a pocket on a job, and this one has the fingerprints to prove it — thumbprints in three colours of ink across the face, scrawled countersignature lines, a burnt corner. Green bench lamp, parts bins, a pegboard of tools behind it. A sub-registry that stamps everything, and did. |
| 1 | `MINISTRY LIAISON · UNNUMBERED` | `art/the-authority-card-liaison.jpg` | **The expensive one.** The only card in the set held in a *clean* black glove, in a lit institutional corridor with a door standing open at the end. Grey laminate, a proper embossed seal, ghost field-lines that were filled in once. An office that did not have to explain itself, and did not. |
| 2 | `ESTATES · SPECIAL PROJECTS` | `art/the-authority-card-estates.jpg` | **The property one.** Held over a drafting table: rolled and tied site drawings, a surveying instrument in its case. Gold leaf flaking off the seal. Special Projects is what an estates department calls a building it is not going to describe, and the drawings are of it. |
| 3 | `PROCUREMENT · SCHEDULE C` | `art/the-authority-card-procurement.jpg` | **The one that bought things.** A tagged pass on a wire loop against a stack of stencilled shipping crates, its own face a ticked schedule column — somebody signed for every line. Schedule C is a line item; this belonged to whoever cleared it. |
| 4 | `INSPECTORATE · NO STANDING` | `art/the-authority-card-inspectorate.jpg` | **No standing, and it looks it.** The only card with no room around it at all — a bare bulb and dark, no bench, no desk, nothing that would place the holder anywhere. Worn thin, stamped in three colours by offices that outranked them. Its authority is the one thing on it that has not faded. |

> **On the lettering rule.** House law §0 forbids readable text, and these five keep it — every stamp,
> signature and field is illegible ink-texture, and the faces are soft enough that no likeness reads. What they
> DO carry is pseudo-script: scribble that is legibly *handwriting-shaped* without being words, and on the
> procurement card, crate stencils in the same register. That is a deliberate acceptance, not an oversight —
> the copy is all written in code and none of it is doubled in the pixels, which is the rule's actual purpose.
> A regeneration should hold the same line rather than trying for a blank card, which reads as a mock-up.

**Guarded by** `RevealPlatesArePaintedTests.EveryOfficeIssuesItsOwnPaintedFace` (all five on disk, all five
distinct, the fallback painted and not one of them) and
`TheHiveCardsTests.TheFaceOfACardIsTheOfficeOnItsLetterhead` (the face and the letterhead are the same roll —
proven RED against a transcribed second roll off `hive:cardart:{body}:{band}`).

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

## 4a. The two silent finds ★ owner's ask — #725

Owner's audit question, walking the four handoff floors: *"Are we giving enough attention to plot-significant
finds? They should have a Gen-AI image and their own dialog by our standards."* THE SHAFT and DEAD AIR met it.
The two the handoff doc actually sends playtesters to did not: **B21's corrected plate** — #592's whole
arithmetic in one sign — was a wall stencil, and **B17's staff mess** was map furniture. A player who walks
past a stencil at deck-plan zoom has missed the reveal of the arc, and the game never knows.

Both cards are the shape §0 allows and not the other one: they **show harder and refuse to conclude.** Neither
adds a subtitle, a hint or a verb, and neither may ever quote the plate's text — it varies by site kind, and a
card that transcribed a sign the renderer draws would double the copy in the pixels' own register.

### `art/the-plate.jpg` — ▣ THE PLATE

- **Slot:** `UndergroundComplex.UnlistedLobbyArtUrl`, raised once per excursion on the first arrival at
  `IsUnlistedLobby` — #694's plate law minus the entrance lobby, so it is the band lobby and nothing else.
- **As painted (2026-08-06):** a bare-pour lobby wall cut into raw rock, one caged lamp on a gooseneck, a
  folding chair with somebody's coat and a flask beside it. On the wall, a **wide patch of newer green paint
  laid over a larger pale rectangle**, and a **small blank steel plate** screwed on over the patch — two
  coats and two decisions, in that order, readable at a glance. The lift door stands open to the right, warm
  and empty. **No lettering anywhere:** the plate is blank steel, which is exactly right, because the copy
  says the plate is not the name of anything you rode down through and the picture must not settle what it
  says instead.
- **Feeling:** a crew that stencils for a living, sent down here to change an answer. Good work, both times.

### `art/the-staff-mess.jpg` — 🍽 THE STAFF MESS

- **Slot:** `UndergroundComplex.StaffMessArtUrl`, raised once per excursion on **entering the room** — the
  only room-entry card in the game. `Amenity.Contains` (→ `RefugeHolds`) is the containment law.
- **As painted (2026-08-06):** a concrete canteen under working fluorescent strips. A bank of four vending
  machines along one wall, **lit and stocked**; a serving counter with stacked trays and cutlery squared into
  their racks; steel tables with the chairs pushed in level. A heavy door at the far end with a **green pass
  reader still live** beside it. One dropped napkin on the floor, and nothing else out of place. **No
  lettering:** the machines' fronts are goods and glow, no brands, no prices.
- **Feeling:** the shift has not come, and the machines are not the kind that wonder. §0's rule holds without
  effort here — nothing in the frame is organic, and the horror is that the room is *tidy*.

**Guarded by** `RevealPlatesArePaintedTests.TheTwoSilentFindsArePainted` (both on disk, neither borrowing a
sibling's canvas), `TheSilentFindsGetACardTests` (which floor, which room, and the prose verbatim) and
`TheSilentFindsAreRaisedOnceTests` (one caller each, each fenced by its own latch, and the mess's box holding
exactly the machines the deck draws).

## 4b. The hall, and the doors along the back of it ★ owner's ask — #751

Owner, 2026-08-06: *"The Canteen is way too small… It needs to house like 80 customers… I am thinking like Mos
Eisley Space port size bar,"* and, an hour later, *"Definitely want to make the B1 bar be fancy ... and have
cabinet-spaces for sensitive negotiations."*

Both rooms are story-grade and take §4a's pattern exactly: a first-entry card, once per excursion, raised by
standing in the room rather than by pressing anything. The hall's card is about **money** and the cabinet's is
about **memory**; neither is about what the building is for, and §0's canon rule holds without effort because
neither room has anything to do with what is below.

### `art/b1-cantina-hall.jpg` — 🍸 THE HALL

- **Slot:** `UndergroundComplex.CantinaHallArtUrl`, raised once per excursion on **entering the B1 cantina
  hall**. `Amenity.Contains` is the containment law and, for a hall, that is the hall's own carved box
  (`UndergroundComplex.Hall.Contains`) rather than a room-sized one.
- **Composition:** a *fancy* company canteen at Mos Eisley scale, underground. Poured concrete structure, but
  laid out like a hotel dining room somebody over-funded: twenty round tops in a 2/4/6 mix with **cloth on
  them**, a long bar counter down the far wall, poured pillars with **brass collars** breaking the sightlines,
  and light that was chosen rather than installed. Working people in work clothes at most of the tables,
  eating, nobody looking at the viewer. Along the back wall, a **row of three padded doors**. Muted
  desaturated palette, painterly, warm key light off the counter. **No lettering, no logos, no numbers.**
- **Feeling:** #601's funding trail as a room. The money does not mind being seen feeding contractors; it
  minds being asked. Nobody in frame finds any of it strange.

**#756 · The hall's art is now worn TWICE.** Besides the first-entry card above, `b1-cantina-hall.jpg` is the
floor's own **backdrop** — drawn under the vector overlay across the hall's published box, through the same
`DeckPlan.Backdrop` seam the ship's CANTINA has worn `art/the-space-bar.jpg` through since the 3D renovation.
Core publishes the url on the hall (`UndergroundComplex.Hall.ArtUrl`, chosen by `HallArtFor`) so the renderer
picks nothing; alpha is `HallArtAlpha` (0.72, a shade under the ship's 0.9 because this room is thirty times a
cabin's floor area) and walls, plates, tops, consoles and the captain all draw OVER it. Any hall wears one by
adding a row to `HallArtFor` — the park (#759) and the head office's dining room need nothing else.

### `art/b1-bar-desk.jpg` — 🍹 THE COUNTER'S OWN DESK

- **Slot:** `CounterService`'s `Barkeep.DeskArtUrl`, drawn on the **counter service card** (#756) — the dialog
  that opens when the captain presses `[E]` at THE COUNTER. Not a floor backdrop and not a first-entry card:
  it is the picture of the thing you are standing at, on the panel you are standing at it through.
- **Composition:** a long polished counter top with a brass rail, backlit bottle shelves glowing out of carved
  rock, a brass espresso machine venting steam with cups warming on top, worn red leather stools — and **NO
  ONE behind it**. No legible lettering.
- **Feeling:** skeleton canon (#618) said as furniture. The counter does its own serving, which is worse.

**#780 — the same file now has a SECOND slot, and that is the point.** Owner, live: *"see how in the space
bars we have the image of bar desk at the spot where the bar desk is."* The desk is drawn on the **deck** as
well, over the counter's own carved box (`UndergroundComplex.Hall.Spots`, at `SpotArtAlpha` 0.96 — harder than
the hall's floor art at 0.72, because a counter is an OBJECT with edges and not the room's ambience). One
picture, two jobs: the panel you order through and the furniture you walk up to. Any fixture after it wears
one by publishing a `SpotArt` where it is carved — the park's windows (#759) are next in line.

### `art/b1-park-behind-windows.jpg` — 🌳 THE VIEW FROM THE STOOL

- **Slot:** `Interior.TheStools.SeatedArtUrl`, drawn on the **counter service card** in place of the bar desk
  **while the captain is up on a stool** (#756). Owner: *"I do not see the park through the bar windows?"* —
  standing you are looking at the counter, seated you are looking over it, so the picture follows the posture.
  This is the park's first appearance in the game; #759 keeps the rest of it.
- **Composition:** a window wall behind the counter, and beyond the glass real grass in mown courses under a
  painted sky. No people, no signage, no legible lettering.
- **Feeling:** the most expensive thing in the building, and nobody in the room looks at it. Nothing in the
  game ever says what a green is doing under a hundred and fifty metres of rock (§13.8), and the caption says
  only what it IS.

### The card under the glass, photographed — 🍽🥃 FIVE MENU ITEMS

- **Slot:** `Drink.ArtUrl` (#780), a thumbnail beside the row on the counter's menu. Optional per item, and
  optional on purpose: COMPANY COFFEE has none and every haven bar's card stays text, so a row without a
  picture draws as a row and not as a hole.
- **Files:** `art/food-cage-breakfast.jpg`, `art/food-subbasement-stew.jpg`, `art/drink-local-pour.jpg`,
  `art/drink-bottom-shelf.jpg`, `art/drink-long-drop.jpg`.
- **Feeling:** the menu jokes about the deep and is never right about it. The photographs are appetising in
  the way a company canteen's photographs are appetising, which is its own joke.

### `art/b1-your-own-table.jpg` — 🪑 THE TABLE YOU TOOK (waiting)

- **Slot:** `SittingAlone.WaitingArtUrl`, drawn on the **sit panel** (#757/#778) — the card that opens when the
  captain presses `[E]` at a top with nobody at it. Same idiom as the counter's desk one section up: the
  picture of the thing you are sitting at, on the panel you are sitting at it through. Chosen by
  `SittingAlone.ArtFor(resting: false)`, off the same flag that picks the panel's opening sentence, so the
  picture and the prose can never describe two different minutes.
- **Composition:** first person from your own chair. Worn scratched steel tops, a tin mug and a folded sheet of
  paper in front of you, and **the chair opposite pulled slightly out and empty**. The hall alive and blurred
  beyond — working people eating and arguing in overalls, poured pillars, hanging lamps — and, small in the far
  background, a keep at the lit counter. No lettering.
- **Feeling:** the empty chair **is** the wait beat. Sitting down alone in a room where nobody knows you is a
  choice to be findable, and this is that choice with nobody in it yet.

### `art/b1-short-rest.jpg` — 🥾 THE SHORT REST (resting)

- **Slot:** `SittingAlone.RestingArtUrl`, the SECOND state of the same panel — `ArtFor(resting: true)`, which
  `SittingAlone.SitReadsAsRelaxed` decides: a pour bought at the counter still in hand (#784's own
  `APourInFrontOfYou`, the one reading there is), **or** a quiet watch. Owner,
  live: *"Definitely a cold drink and legs up on adjacent chair and some notebooks / papers on table when we
  rest there and some mystic looking food."*
- **Composition:** the same table and the same chair — **your boots up on it**, laces trailing. A tall sweating
  glass of something amber, an open notebook covered in handwriting with loose papers and a pen beside it, and
  two tin plates of iridescent violet-and-green **food nobody would recognise**. The hall carries on behind,
  warm and busy. No lettering.
- **Feeling:** the game's one good minute. Nobody in this building needs anything from you, and it will not
  last. The empty chair of the waiting plate is the chair your feet are on — same table, same minute, other
  posture.

**Canon note (#783):** the relaxation prose is the owner's own, lifted verbatim into `SittingAlone`
(`RelaxedSitLine`, `TheDrinkLine`, `StoodUpRelaxedLine`). The rest has **two openings**, ruled at canon
review: the filed line names a cold glass sweating into your hand, and the trigger fires on a quiet watch
*with or without* a purchase — so a drinkless rest gets `RelaxedSitDryLine` instead (#740: a sentence owns its
own facts). The boots are the rest and are always there; the glass is the purchase and is named only when
somebody bought one. Whether a rest *heals* anything is #784's lane; this pair of pictures and the lines
beside them are the register, not the mechanic.

**Seam with #784, stated once:** `Map.CaptainIsRestingAtATable` is *"is the captain at a solo table"* — the
short rest's own trigger, true on every solo sit. `SittingAlone.SitReadsAsRelaxed` is *"does this sit read as
relaxed"* — words and pictures only. A back-to-the-wall watch still gives your breath back; it is simply not
the sentence about boots, and not the picture of them. The **pour** has ONE reading and it is #784's
(`Map.APourInFrontOfYou`), because a panel keeping its own window could say *"the cold glass sweat into your
hand"* on a beat the rest engine had already decided there was no pour.

**Guarded by** `RevealPlatesArePaintedTests.TheTablesTwoStatesArePainted` (both on disk, neither borrowing the
counter's canvas) and `YouCanSitAtAnEmptyTableTests.THE_SIT_PANEL_DrawsTheTableItIsAPanelFor` (in the card's
own subtree, framed, and with no text over it — #782).

### `art/b1-cabinet.jpg` — 🚪 THE CABINET

- **Slot:** `UndergroundComplex.CabinetArtUrl`, raised once **total** (never once per door) on entering any of
  the three cabinets. `UndergroundComplex.Hall.CabinetAt` is the containment law.
- **Composition:** a small enclosed side room off the hall. Six chairs around one round table wiped past
  clean, one heavy **padded door** shut, no window and no line of sight out. On the wall a **plain wall
  telephone with no dial**, square on and unremarked. Empty of people. Even light, catalogue-flat, nothing
  spooky — the room is furniture, and the dimensions of what it is *for* are the whole of the horror.
  **No lettering.**
- **Feeling:** a room with no memory, in a building whose entire output is records.

**Canon note:** the telephone with no dial is canon furniture of a cabinet from here on — it receives and
never dials. It has no mechanics and nothing anywhere explains it.

**Guarded by** `RevealPlatesArePaintedTests.TheHallAndTheCabinetArePainted` (both on disk, neither borrowing a
sibling's canvas) and `TheCantinaHallTests` (which room, once, and the prose verbatim).

## 5. Plot items get a card of their own ★ owner's ask — #614

Owner: *"we could have gen-AI images of plotwise important items… maybe they say something about what door
they open."*

Two objects in the Hive already have a card (§1 the sealed way, §2 the authority card) and everything else a
captain picks up is a row in a list. The items that carry PLOT should each get the same treatment — the art,
and a sentence about what the thing is *for*.

**The rule the second half needs.** "Says what door it opens" is the tempting version and it is a quest
marker: an item that names its lock does the captain's thinking, and this whole facility is built on the
opposite law (**inference horror**, §0). The line describes the **lock**, the way the paperwork that issued
the item would describe it — *runs shaft 2 of a facility that is not this one* — and leaves working out
which building that is to the player. Same rule the sealed way already follows: it says what it is, never
what to do about it.

- `art/the-penetrator.jpg` (PAINTED 2026-08-02) — **the two-stage round.** One shell, out of its packing, on a bench. The
  casing machined to a standard nobody uses for pest control, a driving band, a second stage visible at the
  break. Beside it the packing crate stencilling, deliberately **out of focus and illegible**. Feeling: this
  was made to go through something that was expected to be shot at, and somebody signed for a case of it and
  filed the case under consumables. **Minimum-range warning is a fact about the round, not a label on the
  picture — no lettering anywhere in the image.**
- `art/the-collar.jpg` (PAINTED 2026-08-02) — **the annular item.** Owner: *"kind of horror theme in a Lovecraft way… like
  finding a massive collar designed for Cthulhu's neck"* and, of the inventory: *"it would be our precious
  and it would have admire and discuss options :-D"*. A single band of dark metal on a pallet, **big enough
  that the pallet is the only thing telling you the scale.** Machined inside, with fixing points spaced for
  something with a circumference no catalogued animal has. Wear polish on the inner face — **it has been
  worn.** No creature, no bones, no explanation. The horror is entirely in the measurement.

**Both are canon-bound:** neither the art nor the line may say what wore the collar or what the round was
issued against. *(House law §0, and the grep that enforces it.)*

---

## #528 round two — the reveal that had no frame

### `art/lab-they-stand.jpg` — THEY ARE STANDING OFF THEIR BENCHES

- **Slot:** `SecretLab.TheyStandPlate`, raised in `Map.SecretLab.FireSecretLabReveal` on the
  `RevealOutcome.ItSalvagesYou` branch.
- **Reach it:** land, take the metal detector out, sweep the door square, force the door, read the CORE LOG
  and roll under 9 on the d20.
- **Why it needed one:** the loudest moment on this ground, and the *other* branch of the very same roll
  already ends in a painted selfie against this same room. The bad half got a pulse line.
- **Why it is not a tell:** it fires strictly AFTER the d20 has resolved and been shown. The captain already
  knows which way it went before the picture arrives.
- **Canon-bound** (house law §0): the caption is benches, restraints and a count. It never says what they
  are, and it never will. `TheHiveTests.NothingDownHereEXPLAINSAnything`, one deck up.

> **Prompt used:** A buried clandestine laboratory chamber deep under an airless moon: poured concrete and
> cold steel, two long rows of low steel benches with restraint cradles, lit only by failing emergency
> strips and one helmet lamp. Most cradles still hold still, grey, gaunt humanoid figures lying under a skin
> of frost. Three of them have come off their benches and are standing on the floor, backs and shoulders to
> the viewer, heads turned away, no faces visible. Deep shadow beyond, dust hanging in the beam. Grimy
> lived-in used-future sci-fi, muted desaturated palette of concrete grey and sick green, painterly, moody
> low-key lighting, no text, no lettering, no numbers, no logos, no readable writing, no visible faces.

- **Painted 2026-08-03**, first pass. Guarded by `RevealPlatesArePaintedTests.EveryBeatPlateIsPaintedAndSaysItOnce`.

---

## #804 — the round stops at you

### `art/the-round-stops-at-you.jpg` — 👮 THE CHALLENGE

- **Slot:** `PatrolBeat.ChallengeArtUrl`, drawn on the ViewObject card `Map.Patrol.TheRoundStopsAtYou` raises
  the moment a guard on a beat registers the captain. **One plate for all four rungs** of the wallet read
  (this site's pass, another site's pass, the cage chit, nothing at all): the card's body is the same
  sentence whichever way it goes, because the man in the picture has not read the wallet yet either, and the
  verdict lives in the card's own amber row (#736) rather than in the art.
- **Composition:** a contract guard in a shotcrete corridor of bolt plates, **palm out and up**, clipboard
  under the arm, a laminated pass on his chest. Bored patience, not menace — a man who has done this a
  hundred times tonight. Faint chalk scrawls on the walls (an accidental #794 nod). 16:9. No lettering.
- **Feeling:** the register the owner named on the same issue — *"except when they give the
  we-are-not-so-different-you-and-I speech"*. He is a tired employee on a rotation with a badge like the one
  in your wallet, filed by the same hand upstairs. Nothing in the picture says so and nothing ever will.
- **Canon-bound** (house law §0): the plate may never show what the rounds are guarding, and the guard is
  never drawn as a villain.

- **Generated by the owner 2026-08-08**; wired 2026-08-09 (#804 shipped caption-only under the degradation
  law). Guarded by `TheRoundIsWalkableTests.TheChallengeCardWearsThePaintingAndThePaintingShipped` — the card
  names it AND the jpg is on disk, because an art seam that hides its own failure needs both halves.
