# Image manifest — PROJEKTI KAAMOS (#411, #528)

Three plates for the polar-night arc: the pod that was held, the one who kept the berth, and the berth
answering. Each entry names the **slot in code**, the **destination file** under
`src/SpaceSails.Client/wwwroot/art/`, and the **exact prompt used** — this set is painted, so the prompts
here are a record, not a brief.

Filed against #528, whose finding is that *the game narrates its consequences in pulse lines* — a toast
that fades in a second and a half — and reserves the reveal card for wrecks and deaths. These three beats
are the arc's turnings and they were all toasts.

> **House rules for this set:** grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, **no text, no lettering, no logos, no numbers, no readable writing** — every word of the
> copy is written in code and must not be doubled in the pixels. No real likenesses. 16:9.
>
> **And one rule specific to this arc.** KAAMOS is a mystery that is kept by *not being answered*. The
> paintings may show that something was prepared, held, and never collected. They may never show what is
> under the ice, and they may never show anyone reacting to it. The dread here is clerical: a seal that was
> never broken, a stool nobody sits on, a line on a board that has never been switched off.

## The plate recipe (the vented-room card, generalised)

Four things, all load-bearing — see `RevealPlate` in `src/SpaceSails.Core/KaamosLore.cs`:

1. **A title that names the place and the verb.** Not "fragment acquired".
2. **One painted image of a CONSEQUENCE**, not an action shot.
3. **A caption that describes evidence and stops.** It never says what it means. The seals are latched;
   nobody tells you who decided not to send her.
4. **It fires at the moment it explains the most** — the single seam where the shard is actually
   assembled (`Map.Kaamos.TryAssembleKaamos`), so a plate can never be shown for a shard nobody found.

**Not every beat gets one.** Three of the six KAAMOS shards are deliberately plate-less: a line on a
dedication plaque, a log found in a drawer, and a coordinate bought over a counter each already arrive
with their own scene around them, and none is a turning. Over-carding cheapens the ones that are not —
`RevealPlatesArePaintedTests.TheBeatsThatAreTheRightSizeAsProseGetNoPlate` pins that decision.

---

## 1. `art/kaamos-cold-pod.jpg` — THE POD THAT WAS HELD

- **Slot:** `KaamosLore.PlateFor("cold-pod")`. Fires when the beach-comber's probe turns up the derelict
  supply pod on a dig (`Map.Surface` → `KaamosPodHere` → `TryAssembleKaamos`).
- **Reach it:** `/map?kaamos=pod&land=1` — land, take the metal detector out, probe any square.
- **Why this one first:** the single most paintable object in the arc. A cargo run that was *packed and
  then not sent*, still sealed, generations later.
- **Discipline:** the fragment's own prose already reads the manifest slug out loud (CONSUMABLES,
  WINTERING CREW, 40 SOULS). The plate must therefore **not** show that plate legibly — the words are the
  code's job, the dread is the image's. The manifest plate is present and scoured blank.

> **Prompt used:** A half-buried cargo supply pod lying in grey regolith dust on an airless moon, ribbed
> metal hull frost-cracked and split along its seams, generations old. Its heavy clamp seals are still
> latched and have never been broken. A rectangular manifest plate is bolted to its flank, scoured
> completely blank and illegible by decades of dust and cold, just faint scratched marks where writing once
> was. A shallow trench scraped away from one side by hand tools, a probe rod left standing in the dust.
> Grimy lived-in used-future sci-fi, muted desaturated palette of cold blue-grey and dust brown, painterly,
> moody, low raking light, long shadows, no text, no lettering, no numbers, no logos, no readable writing,
> no people.

## 2. `art/kaamos-berth-holder.jpg` — THE ONE WHO KEPT THE BERTH

- **Slot:** `KaamosLore.PlateFor("holders-tell")`. Fires when the bar seam yields the holder's tell
  (`Map.Kaamos.AskAboutKaamos`).
- **Reach it:** `/map?kaamos=holder` — dock anywhere, walk to the counter, the seam offers the tell.
- **Composition rule (the bar-patron idiom):** seen from **across the room**, past out-of-focus shoulders,
  the way every other patron in this game is framed. The face is turned away and unresolved — no
  likeness, and no portrait dignity either. He is furniture that talks.
- **The evidence in frame:** two empty glasses, and the stools either side of him empty in a bar where
  nothing is empty. The caption names both and stops.

> **Prompt used:** A lone drinker on a corner stool at the far end of a grimy spacer bar, seen from across
> the room past out-of-focus shoulders. An old spacer in a worn insulated jacket, hunched, head turned away
> toward the bulkhead so the face is unresolved and in shadow. One empty glass in front of them and a
> second one already empty pushed aside. The stools either side are vacant though the rest of the bar is
> not. Low amber work-lighting, cigarette haze, scratched counter, riveted bulkhead. Grimy lived-in
> used-future sci-fi, muted desaturated palette, painterly, moody, melancholy, no readable text, no
> lettering, no numbers, no logos, no recognizable face.

## 3. `art/kaamos-berth-resolves.jpg` — ONE LINE STILL LIT

- **Slot:** `KaamosLore.PlateFor("berth-code")` — the capstone, and the loudest plate in the arc. Fires on
  the single edge where the berth-code resolves and `CanReachEnceladus` flips, alongside
  `KaamosLore.ReachNotice`.
- **Reach it:** `/map?kaamos=4`, then dock at any bar — the seam offers *"❄ Put the KAAMOS pieces
  together"*.
- **The whole point of the composition:** the board is enormous and dead, and **one** line is lit. The
  arc's truth is that the berth is still listed because, from under the ice, someone is still asking for
  it — and the picture says exactly that much and no more. There is no one in the concourse, because a
  figure looking at the board would be the game telling you how to feel about it.
- **Text rule matters most here.** A berth board is made of writing. Every character had to come out
  illegible or the image would have written the arc's copy for it.

> **Prompt used:** A tall berth-allocation board dominating the far wall of a dim, almost empty orbital
> exchange concourse. Dozens of rows of illuminated slot lines stacked in columns, every one of them dead
> and grey except a single line low on the board still burning a warm amber. The characters on every line
> are indistinct, worn, smeared and completely unreadable, only the rhythm of marks where writing would be.
> Worn deck plating, dust hanging in a shaft of light, a scuffed handrail in the foreground for scale.
> Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody, cold, quiet, no readable
> text, no lettering, no numbers, no logos, no people.

## 4. `art/kaamos-returned-filing.jpg` — RETURNED TO SENDER

- **Slot:** `KaamosLore.BouncePlate` — **not** in `PlatesById`, on purpose. That dictionary is keyed by
  fragment id and every key in it must be a real pool fragment; the returned filing is the arc's front door
  (#635) and deliberately not a fragment. It gets its own constant and its own guard
  (`RevealPlatesArePaintedTests.TheKaamosFrontDoorIsPainted`).
- **Fires when:** the captain puts their own hull's number on a freight agent's docket and the board sends
  it straight back (`Map.Kaamos.TakeKaamosBounceFiling`).
- **Reach it:** `/map?kaamos=bounce` — dock anywhere, walk to a bar patron, press `[E]`, take the job.
- **Why it earns a plate at all**, when three of the six shards do not: for most captains this is the
  **first thing PROJEKTI KAAMOS ever says**, and #528's whole finding is that the beats which turn a story
  get a picture. The three plate-less shards each already arrive with a scene around them. This one is a
  parcel on a counter, and without the card it is a toast that fades in a second and a half.
- **The evidence in frame:** four return stamps overlapping the same corner of one docket, each fainter
  than the last; the consignee line filled and the delivery line blank; nobody behind the counter; nobody
  throwing it away. The caption names those and stops. It never says who is not answering.
- **Text rule:** a docket is made of writing, so every mark on it had to come out as an illegible smear —
  the same discipline the berth board in #3 needed, for the same reason.

> **Prompt used:** A small battered parcel sitting alone on a scuffed freight counter in a dim orbital
> exchange office, wrapped in worn brown shipping fabric and bound with old strapping. A paper docket is
> glued to its top corner and four rubber return stamps overlap on that same corner, each fainter than the
> last, every one of them a completely illegible smeared ink mark with no readable characters. A worn
> rubber stamp and a dried ink pad lie beside it. Behind the counter a tall rack of empty pigeonhole slots,
> all dark. Low amber office lighting, dust hanging in the air, riveted bulkhead, scratched countertop.
> Grimy lived-in used-future sci-fi, muted desaturated palette of brown and cold grey, painterly, moody,
> melancholy, quiet, no text, no lettering, no numbers, no logos, no readable writing, no people.

## 5. `art/kaamos-head-office.jpg` — THE HEAD OFFICE

- **Slot:** `UndergroundComplex.HeadOfficeArrivalArtUrl`, handed out by
  `UndergroundComplex.FirstDescentCard(bodyId)`. Like the front door's plate it hangs off a **predicate**
  rather than an arc's fragment pool, so it has its own guard
  (`RevealPlatesArePaintedTests.TheHeadOfficesEstablishingShotIsPainted`).
- **Fires when:** the lift reaches the first floor of the head office, once per excursion — the same seam
  the Hive's own THE SHAFT card uses, choosing between the two by building.
- **Reach it:** `/map?kaamos=hq&land=1`, walk to the lift head, ride down.
- **Why it is its own canvas:** the owner's ruling is that a player who has crawled a Hive should recognise
  the rank difference **without being told it**, and the first descent is the one frame where that can be
  said in a breath. Showing the Hive's shaft card here would throw the whole thing away at the cheapest
  possible moment. It is built out of the same four things the Hive's is — a shaft, a directory, a lobby,
  a floor — with every one answered differently.
- **The evidence in frame:** lobby lighting fully on; stone facing and a coffered ceiling somebody paid for;
  a directory board with every row present and illegible; a long empty coat rack; an empty bench; a polished
  floor with no dust and no footprints. Nobody. The caption names those and stops — it may say the lamps
  come up ahead of the car, and it may never say who turned them on.
- **Text rule:** a directory board is made of writing, so every row had to come out as a smudge; the same
  discipline the berth board in §3 needed.

> **Prompt used:** The doors of a lift opening onto an enormous underground lobby, deep beneath an ice moon.
> A vast institutional reception hall in poured concrete and pale stone facing, brutalist, expensively built,
> with a coffered ceiling receding into distance and a long empty reception counter. Warm amber lighting is
> fully on across the whole space and immaculately maintained. A tall directory board mounted beside the lift
> surround, its rows of entries worn into completely illegible smudged marks with no readable characters. A
> row of empty coat hooks and one long empty bench. The floor is polished and spotless with no dust and no
> footprints. Absolutely nobody present, no people anywhere. Grimy lived-in used-future sci-fi but this room
> is kept, muted desaturated palette with cold blue-white spill from one side, painterly, moody, enormous
> sense of scale, quiet, no text, no lettering, no numbers, no logos, no readable writing, no people.

## 6. `art/kaamos-wintering-hall.jpg` — FORTY-ONE

- **Slot:** `UndergroundComplex.WinteringHallArtUrl`, raised on stepping out at **B23 · THE WINTERING
  HALL** and again on `[E]` at the console there.
- **Reach it:** `/map?kaamos=hq&land=1&floor=23`.
- **Why it exists:** it is the room this whole arc was written for, and the one place
  `KaamosLore.RevealSanityShockHook` (40.0) is spent. If any beat in the game earns a canvas, it is this
  one.
- **The evidence in frame:** rows of identical narrow berths, every one of them MADE — blanket turned
  back at the same angle, pillow squared — and none occupied; a wall of thick glass with black water
  behind it; the lighting fully on; a floor with no dust and no footprints; **one berth apart from the
  rest**. Nobody. The card counts, because counting is a thing the captain does with their own eyes; the
  picture never says whose the last one is, and neither does anything else, ever.
- **Composition rule:** no figure anywhere, and nothing that looks recently disturbed. The horror is that
  it is READY.

> **Prompt used:** An enormous long dormitory gallery deep beneath an ice moon, one vast room with a low
> ribbed concrete ceiling and a wall of thick dark glass along one side holding back black water. Four long
> rows of identical narrow institutional berths receding into the distance, each one immaculately made up
> with the blanket turned back and the pillow squared, none of them occupied and no people anywhere in the
> frame. Warm even lighting fully on across the whole hall. At the very end of the nearest row, slightly
> apart from the others, one final berth made up exactly the same way. The floor is polished and completely
> free of dust and footprints. Grimy lived-in used-future sci-fi but this room is kept, muted desaturated
> palette of warm grey and cold blue-green from the water, painterly, moody, enormous sense of scale,
> deeply quiet, no text, no lettering, no numbers, no logos, no readable writing, no people.

## 7. `art/kaamos-berth-office.jpg` — ONE LINE STILL LIT

- **Slot:** `UndergroundComplex.BerthOfficeArtUrl`, raised at **B24 · THE BERTH OFFICE**, the deepest
  floor of the deepest building in the game.
- **Reach it:** `/map?kaamos=hq&land=1&floor=24`.
- **The one deliberate inconsistency, and it is the beat:** every other room in the head office is
  immaculate, and this one is knee-deep. It is untidy with **its own output** — the log has been printing
  continuously and folding itself onto the floor, and nobody has emptied it, because emptying it is not a
  thing anybody ever wrote down. The card says exactly that and stops.
- **The evidence in frame:** a wall board of stacked slot lines, all dead but one still burning amber; a
  console still printing; a drift of continuous paper across the floor; one chair pushed neatly in.
  Nobody. It is the same image as §3's berth board seen from the other end of the same conversation, and
  it is deliberately a DIFFERENT painting — one canvas doing two rooms would be a lie about a place.

> **Prompt used:** A small deep-underground clerical office, one console desk against a bare concrete wall
> with a single dim monitor screen. Above it a tall wall-mounted allocation board of stacked slot lines,
> every line dark and dead except one low on the board still burning a warm amber, and the characters on
> every line are worn into completely illegible smeared marks with no readable characters. A continuous
> printed paper log has spooled out of a slot in the console and folded itself into a deep drift across the
> floor, generations of it, undisturbed. One empty office chair pushed neatly in. Absolutely nobody present,
> no people anywhere. Grimy lived-in used-future sci-fi, muted desaturated palette of concrete grey with one
> amber light source, painterly, moody, cold, quiet, no text, no lettering, no numbers, no logos, no
> readable writing, no people.

---

## Generation recipe

grok is the project's gen-AI art source (owner ruling 2026-07-18: **images only — no code, no git**).

```bash
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." \
     -m grok-4.5 --permission-mode bypassPermissions
```

- Only model is `grok-4.5`. **`grok update`** refreshes auth (the TUI's ctrl-u, no browser needed), and it
  must be run **from PowerShell, not Bash** (different auth state).
- grok ignores `--worktree` and acts in its CWD: **generate into the scratchpad, look at every one, then
  copy into `wwwroot/art/`.** Everything it saves is a JPG whatever the extension says.

## Status

| # | Slot (`KaamosLore.PlateFor`) | File | Code wired | Art | Guard |
|---|---|---|---|---|---|
| 1 | `cold-pod` | `kaamos-cold-pod.jpg` | ✅ | ✅ | ✅ |
| 2 | `holders-tell` | `kaamos-berth-holder.jpg` | ✅ | ✅ | ✅ |
| 3 | `berth-code` | `kaamos-berth-resolves.jpg` | ✅ | ✅ | ✅ |
| 4 | `BouncePlate` (#635, the front door — not a fragment) | `kaamos-returned-filing.jpg` | ✅ | ✅ | ✅ |
| 5 | `UndergroundComplex.HeadOfficeArrivalArtUrl` (#411, the first descent — not a fragment) | `kaamos-head-office.jpg` | ✅ | ✅ | ✅ |
| 6 | `UndergroundComplex.WinteringHallArtUrl` (#411, B23) | `kaamos-wintering-hall.jpg` | ✅ | ✅ | ✅ |
| 7 | `UndergroundComplex.BerthOfficeArtUrl` (#411, B24) | `kaamos-berth-office.jpg` | ✅ | ✅ | ✅ |

The guard is `tests/SpaceSails.Core.Tests/RevealPlatesArePaintedTests.cs`: every plate must be keyed to a
real fragment and must name a JPG that is **actually on disk**. The `onerror`-hide law is what makes
shipping code before art safe, and it is exactly why a plate pointed at a file nobody painted is otherwise
**invisible** — it does not throw, it does not log, it just leaves a hole in the card forever. The csproj
copies `wwwroot/art/kaamos-*.jpg` beside the test assembly on every build (the `CssZBandSyncTests` idiom),
so the test reads the live art directory rather than a snapshot. Proven RED before shipping by pointing
`cold-pod` at `kaamos-cold-pod-nope.jpg` and by misspelling its key.
