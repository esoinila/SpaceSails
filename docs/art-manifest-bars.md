# Image manifest — the bars (#528, #655, #247)

Two beats from the bar pass that landed on 2026-08-03, painted the day after — and from **2026-09-20, the
seven house pours** (#247, §3). Each entry names the **slot in code**, the **destination file** under
`src/SpaceSails.Client/wwwroot/art/`, and the **exact prompt used** — everything here is painted, so the
prompts are a record, not a brief.

> **House rules for this set:** grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, **no text, no lettering, no logos, no numbers, no readable writing** — every word of the
> copy is written in code and must not be doubled in the pixels. No real likenesses. 16:9 for the two beats;
> **1:1 for the drink plates**, which are thumbnails beside a menu row (#782) and are cropped square.

Every slot `onerror`-hides, so the code shipped first and the JPG dropped in behind it.

---

## 1. `art/oracle-rant.jpg` — THE CORNER, MID-RANT

- **Slot:** `OracleRant.ArtFile`, rendered inside the existing oracle card in `Pages/Map/OracleCard.razor`
  (`.oracle-art`, styled in `Pages/Map/OracleCard.razor.css`).
- **Reach it:** dock at any haven, walk the bar, press **E** on `◈ "STATIC" MARSH` on a watch she is present
  for (`PresenceChance` 0.55).
- **A backdrop, not a plate.** This card is a **conversation the captain stays inside**, turning the dial
  line by line and standing her drinks; a modal opening over it on every draw would be a card on a card. The
  picture belongs *in* her card, under her name, where it sits while she talks. That is why this one is an
  `<img>` and a CSS rule rather than a `RevealPlate`.
- **The one thing the frame may never do** is look more or less credible. A true line *"sounds nuts but IS
  true"* and the sifting is the whole mechanic, so nothing in the picture says whether she is right.
- The fizzing drink is the game's own detail, lifted from the empty-stool message: *"a half-finished drink
  still fizzing at the wrong frequency."* The room half-turned away is the other half of her character.

> **Prompt used:** The back corner of a cramped grimy spaceport bar, seen past the shoulders of other
> drinkers who have all half-turned away. In the corner booth a hunched figure in a filthy layered coat
> leans forward mid-sentence, hands spread, face lost in shadow under a hood. On the table in front of them
> a squat glass of cloudy drink is fizzing hard, its surface crawling with a fine unnatural standing pattern
> of ripples. Dim amber light, smoke, worn padding, dead pipes overhead. Grimy lived-in used-future sci-fi,
> muted desaturated palette, painterly, moody low-key lighting, no text, no lettering, no numbers, no logos,
> no readable writing, no recognisable faces.

- **Painted 2026-08-03**, first pass. The standing wave on the table came back exactly as asked.
- **Guarded:** `RevealPlatesArePaintedTests.TheOraclesCornerIsPainted`.

## 2. `art/bond-cognac.jpg` — TWO GLASSES, AND THE SHUDDER STOPS

- **Slot:** `StrangerBond.CognacPlate`, raised in `Map.Bond.TryBond` on the `Bond.Drink` outcome.
- **Reach it:** `/map?bond=1`, dock at a bar, and wait for the next scare — the cheat forces the hero beat.

**Why this one matters more than its size.** Nearly every card this lane has painted is somebody's worst
day: a hull that died, a room where the things stand up, a wallet on a floor. This is the beat where the
shudder stops and a stranger who had no reason to stay in the room stayed in it — the hero beat of the whole
bond system (owner, mid-storm: *"adversity makes the sharers bond… it bonds strangers. Let's use that."*) —
and it was a toast.

**A game that only ever hands out a picture for the bad news teaches the player that a picture IS bad
news**, which is a tell of a different kind and would quietly drain the dread out of every other plate in
the set. This is the counterweight.

- **Raised only on the outcome that MADE something** — the same gate the *"How you met"* memory rides
  (#655). A shared word or a notch of warmth is a passing grace and stays passing; over-carding cheapens the
  ones that are not.
- **Evidence, as always:** two glasses, two pairs of hands, and liquid that has not finished moving. Nobody's
  face, and not one word about what the alarm was for.

> **Prompt used:** Close on a battered metal bar counter in a station bar: two heavy tumblers of warm amber
> spirit have just been set down side by side, still rocking slightly, the liquid in both still sloshing
> from a shudder that has only just stopped. Two pairs of worn gloved hands rest on the counter either side
> of them, one reaching. Behind, the bar is out of focus and dim. Warm gold light from a low lamp,
> everything else cold and grimy. Grimy lived-in used-future sci-fi, muted desaturated palette warmed by one
> amber light source, painterly, moody, no text, no lettering, no numbers, no logos, no readable writing, no
> labels, no faces.

- **Painted 2026-08-03**, first pass. Warming a *muted desaturated* palette with **one named light source**
  rather than asking for "warm" outright is what kept it in the house style — the rest of the frame is as
  cold and grimy as every wreck in the set, and only the lamp is not.
- **Guarded:** `RevealPlatesArePaintedTests.EveryBeatPlateIsPaintedAndSaysItOnce`.

---

## 3. The seven house pours (#247) — EVERY BAR POURS ITS OWN

- **Slot:** `Interior.Barkeep.DrinkArtUrl`, carried onto the menu row by `DrinkMenu.SpecialtyOf` and drawn by
  the existing `.bar-menu-art` thumbnail in `Pages/Map/BarMenuCard.razor` (#780/#782). One plate per bar,
  keyed by body id — the same record that holds the pour's NAME and its LINE, so the photograph cannot end
  up being of a drink the bar stopped pouring.
- **Reach them:** `/map?ashore=1&dock=<bar>` → walk the counter, press **E**, press **📜 See the menu**. The
  house pour is the last row on the card and it is the only row on a haven card with a picture beside it.
- **Guarded:** `TheBoardIsNotThePourTests.EveryHouseDrinksPlateIsActuallyInTheFolder` and
  `…IsSpecifiedInAManifest` (this file is the specification those read).

**Owner's brief, verbatim:** *"many different themed drinks to the place and ingredients available… Each
bar's menu becomes generated drink-card art with IN-WORLD ingredients… The ingredient list doubles as
worldbuilding — every cocktail is a geography lesson (secretly edutainment strikes again)."*

**What every plate in this set is.** One cocktail, alone, on a **scarred steel counter**, shot close and
from slightly above — the same counter seven times, so the set reads as one world and the DRINK is what
changes. Square (1:1), because these are thumbnails beside a menu row and a wide crop would lose the glass.
**Each one carries its own tell** — one physical thing in the frame that says which port you are at without
a word of text, which is the whole point of the ingredient list being the worldbuilding.

> **⚠ These seven are a SPEC, not a transcript.** Entries 1 and 2 above quote *the prompt used*, because the
> lane that wrote them ran the generation. The plates below were painted in a separate art session and this
> lane did not run it, so what follows is the **composition brief** plus a **reading of the pixels that
> actually shipped** — every "what is in the frame" line below was written after opening the file and
> looking at it, never inferred from the drink's line in Core. Where the shipped frame answered the brief
> differently and better, the frame is what is written down.

> **The brief, shared by all seven:** A single cocktail standing alone on a scarred, scratched steel bar
> counter, close, slightly from above, square. Grimy lived-in used-future sci-fi, muted desaturated palette,
> painterly, moody low-key lighting from one source, the room behind it dark or out of focus. Each drink
> carries the tell below — a physical object, never a mood and never a symbol. No text, no lettering, no
> numbers, no logos, no readable writing, no hands, no faces.

| # | file | bar | the pour | what is in the frame |
| --- | --- | --- | --- | --- |
| 3.1 | `art/drink-space-bar.jpg` | `the-space-bar` · THE ROADSTEAD BAR | **DUST DEVIL** | a heavy tumbler, a coarse salt rim, chilli powder spilled red across the steel |
| 3.2 | `art/drink-cinder-roost.jpg` | `cinder-roost` · THE CINDER LOUNGE | **SULPHUR SOUR** | a lime-wedged coupe, and beside it a dropper bottle whose label is worn blank |
| 3.3 | `art/drink-ringside.jpg` | `ringside-exchange` · THE RINGSIDE BAR | **RINGSIDE** | rye over ice cut as a literal RING, and one worn coin on edge nobody is reaching for |
| 3.4 | `art/drink-the-tilt.jpg` | `the-tilt` · THE TILT BAR | **THE LIST** | a glass leaning hard, its liquid level with nothing, and the brine jar it came from |
| 3.5 | `art/drink-earthrise.jpg` | `selene-gate` · THE EARTHRISE BAR | **EARTHRISE** | one grey pitted lump in the glass that reads as rock, and Earth in the porthole |
| 3.6 | `art/drink-red-eye.jpg` | `red-eye` · THE STORMWATCH BAR | **THE BLINK** | cracked black pepper on the surface, and the Spot turning behind the wet window |
| 3.7 | `art/drink-the-deep.jpg` | `the-deep` · THE DEEP END | **TRENCH** | no ice in the glass at all — the frost ferns are growing up the wall instead |

> **3.1 · DUST DEVIL** (`the-space-bar`) — *brief:* a heavy thick-walled tumbler of spirit with a red chilli
> tincture curling through it, a coarse pale salt rim, salt and chilli powder spilled on the steel.
> **Shipped:** all of it, and better — the tincture hangs as one red ribbon that has not stirred in, the
> chilli powder is a rust-coloured drift across the counter like the flats outside, and there is smoke off
> the glass. The tumbler is visibly the heavy one. *"It is served in a heavy glass because the light ones
> leave"* is a fact about the picture as well as the line.

> **3.2 · SULPHUR SOUR** (`cinder-roost`) — *brief:* a pale sour in a coupe, lime, and the unnamed yellow
> bitters somewhere in frame. **Shipped:** the coupe is beaded with condensation, the lime wedge is on the
> rim, and the tell is what stands beside it — a small brown **dropper bottle whose paper label has worn
> completely blank.** That is the best possible answer to *"a yellow bitters the keep will not name"*: the
> bottle is right there and it still does not say. (The blank label is scuffed paper, not lettering — the
> set's no-readable-writing rule holds.)

> **3.3 · RINGSIDE** (`ringside-exchange`) — *brief:* rye over one shard of ring ice, and the coin. **Shipped:**
> the ice is cut as an actual **torus** — a ring, sitting in the rye with the drink visible through its hole —
> and one **worn coin stands on its edge** on the counter beside the glass, faintly stamped and quite
> illegible. Nobody is reaching for it. Exactly the line, twice over.

> **3.4 · THE LIST** (`the-tilt`) — *brief:* aquavit in a glass that leans, and the park brine. **Shipped:**
> the glass leans hard enough that its rim and its liquid disagree with each other and with the counter, with
> a twist over the lip; and the **jar of park brine** — dill, pale sliced greens, seeds at the bottom — stands
> open beside it. Everything in the room behind is off-axis too, which is the joke: *"out here everything
> leans; the glass is only honest."*

> **3.5 · EARTHRISE** (`selene-gate`) — *brief:* regolith-filtered vodka and curaçao over one lump of polar
> ice, with Luna in it somewhere. **Shipped:** a tall glass shading violet at the top to deep blue at the
> bottom, and floating in it **one grey pitted lump that reads as stone rather than as ice** — which is the
> whole of *"one lump of polar ice that took longer to get here than you did."* Behind the counter, a
> porthole onto grey regolith with the **Earth crescent** hanging over it. The only plate in the set with a
> window in it, and it has earned one.

> **3.6 · THE BLINK** (`red-eye`) — *brief:* gin, a hydroponic tomato, cracked black pepper; and the Spot.
> **Shipped:** a tall red glass with the tomato dissolved into pale orange curls that are still turning, a
> **slick of coarsely cracked pepper floating on top** and more of it scattered on the steel — and behind a
> rain-streaked window, **the Spot itself, mid-turn.** The drink and the storm are the same shape, which is
> the line's own joke landing in paint: *"finish it before the Spot moves; the Spot does not move."*

> **3.7 · TRENCH** (`the-deep`) — *brief:* dark rum, cold tea, ink bitters, **no ice**, and the walls are the
> ice. **Shipped:** a squat glass of something so nearly black that only the surface catches the lamp, not
> one cube in it — and **frost ferns growing up the bulkhead behind it** and out along the counter. One dim
> yellow lamp overhead and nothing else. The picture says the last sentence of the line without saying it.

- **Painted 2026-09-20**, first pass, all seven. Opened and read off the pixels the same day, which is why
  §3.2's and §3.3's rows above name the dropper bottle and the ring of ice rather than what the brief
  guessed.
- **The set's own discipline:** one counter, seven drinks, and the difference is always an INGREDIENT rather
  than a style. Seven differently-lit, differently-framed cocktails would have been seven pictures; seven
  pours on one counter is a system of bars. The tell is always a physical object in the glass or beside it —
  never a mood and never a symbol — because the ingredient list is the worldbuilding and the picture's job
  is to show that the list is true.

---

## 4 · DOWN BELOW — THE SERVICE LEVEL (#1253, 2026-09-20)

One canvas, for the floor under one station's concourse. Owner, 2026-09-20: *"could we add a basement level
to the observation deck station, so the tailing task could start from the basement cabin and end at the
observation deck? … The main hall could have multiple elevators… good for tailing."*

| file | where it is laid | state |
| --- | --- | --- |
| `art/selene-service-level.jpg` | the LOWER CONCOURSE at **Selene Gate**, across the ring under the hall — the concourse's own backdrop grammar (one canvas over the twelve-gon's box, at 0.95) | ✅ painted 2026-09-20 |

> **Prompt / composition (16:9).** A curved service corridor under worklight, running away from the viewer
> round the inside of a station ring: **numbered green cabin doors** in a row along the wall, **cable trays
> overhead**, and a **yellow lift cage at the far end**. Maintenance grey and worklight amber; no signage the
> eye can read, no people, and nothing that explains what the level is for.
>
> **Why it is one picture and not two.** The hall gets a canvas and the bar gets a canvas because they are
> two rooms with two moods. The service level is one corridor the whole way round; a second plate for the
> other half of the same corridor would be the same photograph at a different stretch.
>
> **The no-readable-writing rule holds.** The plates the player reads are DRAWN by the deck, not painted into
> the canvas: `CABIN n · CREW` on each leaf and `SERVICE LEVEL — NO PUBLIC ACCESS` on the concourse side of
> each car. The picture carries the light and the cable trays; the building does the talking, in the
> inspectorate register it does all its talking in.
