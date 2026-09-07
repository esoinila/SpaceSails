# Image manifest — the bars (#528, #655)

Two beats from the bar pass that landed on 2026-08-03, painted the day after. Each entry names the **slot in
code**, the **destination file** under `src/SpaceSails.Client/wwwroot/art/`, and the **exact prompt used** —
both are painted, so the prompts here are a record, not a brief.

> **House rules for this set:** grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, **no text, no lettering, no logos, no numbers, no readable writing** — every word of the
> copy is written in code and must not be doubled in the pixels. No real likenesses. 16:9.

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
