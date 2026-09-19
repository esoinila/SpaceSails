# The paperwork — what a wreck says about herself, and how you catch it lying

*Status: **designed, not built**. Owner's idea, 2026-07-29, straight after the atmosphere board landed.*

> "I like the idea that we need to visit multiple places in the ship to determine what happened. The bridge
> log and the vent panels, escape pods etc. The ships missions could be bogus in some ways which could be
> a hint at our big plot arcs."
> "They could have something that we know is not so :-D"

---

## 1. The one idea

The wreck lane already asks *what killed her*, and reads the answer off things bolted to her deck. This adds
the second question, which is the better one: **what was she doing out here, and is that true?**

The evidence for it is her **paperwork** — orders, charter, manifest, crew list, watch bill — and the way
you catch a lie in paperwork is never by reading one page harder. It is by reading two pages that cannot
both be true.

> **The rule for the whole feature: no station ever tells you a document is false.** It states what the
> document says. The contradiction is between two places you have stood, or between a page and something
> the captain already knows about the world. Naming it is the player's job — the same law the wreck causes
> already run on.

## 2. Why it makes the ship a place instead of a menu

Right now two evidence stations are enough to file. That is a *quota*, and a quota is something you clear.
Corroboration is different: the stations stop being tickets and start being **witnesses that disagree**.

- The **bridge log** says where she thought she was going.
- The **charter / orders** say who paid for it.
- The **manifest** says what she was carrying.
- The **crew list and watch bill** say who was aboard.
- The **pod cradles** say how many left ([safety-card.md](safety-card.md)).
- The **valve board** says what was done to her, and by whom ([atmosphere.md](atmosphere.md)) — the vented
  hull's single un-blown compartment is already exactly this kind of evidence.

None of those is a puzzle on its own. Any *two* of them can be a contradiction, and the wreck lane already
has the machinery for it: `Derelict.MisreadsAs` is a wrong answer the evidence supports. This just makes
some of the wrong answers **deliberate lies told by the ship's own owners** rather than honest confusions.

## 3. "Something that we know is not so" — the three tiers

The good part of the owner's phrasing is *we know*. The contradiction has to land against the **player's**
knowledge, not against a lore dump. So every bogus document reads at three levels, and the game never
announces which one you are on:

| You know | What the page is |
|---|---|
| **Nothing yet** | An odd detail. A date, a berth number, a countersignature. You will forget it. |
| **One piece of the world** | A snag. You have seen that berth named on a plaque at Ringside, and this manifest is filing for it. |
| **The matching arc fragment** | A contradiction with a name. You are holding the shard that makes this page impossible, and the wreck stops being an accident. |

That tiering is what stops it becoming a lecture. A first-voyage captain reads a slightly strange charter
and moves on; a captain four shards into an arc reads the same page and goes cold.

## 3b. THE NEWS WIRE IS THE OTHER WITNESS

> Owner: *"like something from the news feed … then the ship logs still use old lies"* — *"exposed
> coverups etc"*

This is the mechanism the tiering above needed, and it is already in the game. The
[news wire](news-wire.md) runs stories at the player continuously; most are colour. Some of them are
**retractions, exposures and admissions** — the moment a thing that was official becomes a thing that was
a lie.

And a wreck is a **time capsule of what was believed when she died.** Her log cites the notice that was
current. Her orders quote the safety bulletin that has since been withdrawn. Her charter names a hazard
that was later admitted never to have existed. *She is still repeating the cover story, because nobody
aboard her ever heard it retracted.*

```
   THEN                                   NOW                         ABOARD
   a notice is issued        ─────▶  the wire exposes it     ─────▶   her log still cites it
   (the cover story)                 (you read the story)             (and cannot know better)
```

That gives the feature three things it could not get any other way:

1. **The player's knowledge is real and earned.** "Something we know is not so" means *you read that
   story*. Not a codex entry — a headline you scrolled past three ports ago.
2. **Wrecks become re-readable.** A story breaking today can make a hull you already filed on suddenly
   legible. Going back to a ship you thought you had finished is now a thing a careful captain does.
3. **The dating is free.** How old her lie is tells you how long she has been out here, without a single
   number on a console.

### The loop closes both ways

The wire is not only an input. **Filing a fraud report is a story**, and the wire can run it — which means
a captain who catches a coverup in a dead ship's paperwork sees their own finding come back at them as
news, and other wrecks carrying the same lie become readable because of something *they* did.

That is the strongest version of this: the player is not looking up answers, they are **the reason the
answer exists.**

### The screen — where a story becomes a place you stood

> Owner: *"I guess we could have gen AI images of major news that we want to use :-)"* — *"like a big
> screen on ship / station where the breaking story is :-D"*

Exactly right, and it solves a problem the rule above has: *"you saw the story"* is a much better condition
when seeing it is **somewhere you stand** rather than a line that scrolled past in a ticker.

So the major stories get a **screen** — a big one, in the places people gather: the station concourse, the
ship's mess. Ambient wire chatter stays a ticker; a **breaking** story takes the screen, with a canvas.

- **You watch it with `E`**, like every other console on every deck in this game — walk up, press E, the
  card comes up with the canvas and the story. That press is what marks the story **seen**, and it is the
  flag the wreck paperwork checks. No codex, no quest log: you were in the room when it broke, and you
  stopped to watch.

  *(This matters more than it looks. A ticker you cannot interact with would make "did you see it" a
  bookkeeping question about whether text was on screen. A console makes it a thing the captain chose to
  do — and choosing to stand and watch the news is exactly the kind of small deliberate act this game is
  made of.)*
- A retraction or an exposure is exactly the kind of story that deserves the screen, which means the
  stories that matter mechanically are the ones the player is most likely to have actually watched.
- It gives ports a rhythm: you dock, and the room is watching something.

**Art rule, inherited from every other set here:** the canvas is *photojournalism of the event*, never a
graphic with a headline on it. **No readable text, no lettering** — our copy is written in code and must
not be doubled in pixels. A tribunal room with empty seats. A dock with a hull under tarpaulin. A press
scrum outside an underwriter's office. The image carries the weight; the words stay ours.

Tracked in its own manifest when built (`docs/art-manifest-news.md`), same recipe as the wrecks and the
visions.

### The shape it needs

- Wire items gain an optional **`Exposes`** tag naming the claim they demolish (a notice number, a berth, a
  company line). Ambient stories keep no tag and change nothing.
- A wreck's papers cite claims by the same ids.
- A contradiction fires when the captain has **read the page and seen the story** — either order, because
  finding the ship first and the headline later is the better version.
- Which means the corroboration ledger (§5) has to be **per game-thread, not per wreck**: what the captain
  knows travels with them.

## 4. The bogus missions, and which arc each one feeds

Concrete, because "the mission could be bogus" needs specific lies to be worth building. Each one is a
**page that cannot be true**, paired with the thing that proves it.

### KAAMOS ([KaamosPlotline.md](KaamosPlotline.md))

1. **She was running supplies to a berth nobody files for.** Her orders name the ice-moon berth — the one
   the Ringside plaque says has sat listed and unclaimed for years. *Contradiction:* the runs stopped long
   before her charter was signed. Somebody was still sending ships.
2. **Her crew list is forty.** For a hull that berths twelve. The watch bill is signed by all forty, in one
   hand, for months. *Contradiction:* the `cold-pod` shard — `40 SOULS · DEST. KAAMOS · HOLD FOR CYCLER
   WINDOW`. Feeds `listed-berth` / `cold-pod`.
3. **The cycler window on her orders is a window that does not exist.** It is real arithmetic for a
   transfer nobody sells tickets for.

### NEBULA MUTUAL ([NebulaArc.md](NebulaArc.md))

4. **Insured for a cargo she never had room for.** The manifest and the hold do not match by an order of
   magnitude, and the policy is countersigned twice in the same hand. Feeds `fine-print` / `policy-terms`.
5. **A crew member signs the watch bill after their own death date.** The log has them standing watches for
   weeks past the entry that records them lost. *Contradiction:* if you hold `rebirth-glitch`, you know
   exactly what kind of company keeps a subscriber on the books after they die.
6. **The recovery clause names the crew as the insured asset.** Not the hull, not the cargo. Feeds
   `collector-writ` — the writ that repossesses the subscriber.

### The third seam ([the-archive-node.md](the-archive-node.md))

7. **A survey charter for a ship carrying a cold-archive node.** Nothing on her paperwork explains the one
   thing aboard her that still has power — and on `VentedByOneOfTheirOwn` that is the object that killed
   her. The page is not wrong about the accident; it is wrong about *why she was out here at all.*

## 5. How it wires, without touching the arcs

The arcs deliver fragments through named sources (`KaamosSource`, `NebulaSource`) and each delivering lane
binds to a fragment id rather than inventing lore. This becomes one more delivery route with a shape none
of the others have — **you have to earn it by corroborating, not by being present.**

Sketch, deliberately small:

```
Papers        : an authored pool of documents per wreck, seeded, each with a CLAIM
Claim         : a statement the world can contradict (a berth, a date, a count, a countersignature)
Contradiction : (claimA, claimB) | (claim, worldFact) -> the line, and optionally a fragment id
```

- **Pure Core**, like every other wreck rule: papers are authored, seeded per wreck, deterministic.
- A contradiction only *fires* when the captain has read **both** sides. Reading one page is never enough —
  that is the whole feature.
- Firing may assemble an arc fragment. It never states the arc's truth; it states the impossibility.
- The existing report fork gains a third road: file the accident, file the *fraud*, or say nothing. The
  fraud road is worth more and makes an enemy — which is already the shape `InsuranceJob` uses.

## 6. What this buys the wreck lane

- **A reason to walk the whole ship** rather than the two nearest consoles.
- **A reason to come back to a hull you have already read**, once you know more than you did.
- **Arc fragments that cost curiosity rather than luck** — you find these by being thorough, which is a
  skill the rest of the salvage loop already rewards.
- And the thing the owner has been building toward all week: the two big arcs stop being things you hear
  about in bars and start being things you *catch people doing* in the paperwork of dead ships.

## 7. Build order

1. **Core: `ShipPapers`** — the authored pool, the claim type, the contradiction table, seeded selection
   per wreck. Pure and tested; no client.
2. **Stations** — the charter/orders and the crew list join the log and the manifest as places you stand.
   The A* audit picks them up for free, on every cause, the day they land.
3. **The corroboration ledger** — which pages this captain has read, per wreck, and which contradictions
   have fired. Vault-persisted, additive, like every other ledger here.
4. **Arc hooks** — a fired contradiction may assemble a `KaamosSource`/`NebulaSource` fragment.
5. **The third road on the report card** — file the fraud.

## 7b. What the story pass found first (#533, 2026-08-02)

Before any of the above is built, the papers a wreck *already* carries were walked cause by cause. Three
things were wrong, and all three are the same shape: **a switch with no arm for a cause**, which does not
error and does not look empty — it quietly borrows the fallback and reads as an ordinary ship.

- **Her log said the opposite of her evidence.** `?wreck=ventedbyoneoftheirown&land=1` — the hull `?archive=1`
  boots into. The cause station said *"the log runs on for months after that, in one immaculate hand, signing
  forty names on and off watch"*; the bridge log, one room away, said *"The log ends 31 years ago. The last
  entries are ordinary ship's business, and then there are no more."* The words the log should have spoken
  already existed and were already Core-tested — `HullVenting.VentedShipLogLine`, the eleven months of watch
  rotations signed by four people who were in vacuum when it was written — **read by nothing.** Three other
  causes (ReactorCascade, HullBreach, Infested) were borrowing the same fallback and now have their own.
- **Her evidence station had no name.** No arm in `CauseStation`, `CauseStationName` or the renderer's
  `CauseLabel`, so she got the fallback point (0, 0) and a console labelled **THE WRECK**. The point turned
  out to be the right one — her evidence is *"every door was thrown from the SPINE side"*, so the place to
  read it is the corridor, looking fore and aft at every hatch dogged from your side — so it is declared now
  rather than reached by accident, and it is called `🚪 THE HATCH DOGS — SPINE SIDE`.
- **The decision card told the pirates they were never here.** The footing under her name read *"and nobody
  has been aboard since"* on every hull in the fleet — including `?wreck=piracy`, whose manifest two rooms
  away says *"whoever boarded her was in a hurry"* and whose evidence says the airlock was cycled from
  OUTSIDE, and `?wreck=infested`, where something has been aboard the whole time. What is true of all ten is
  that **nobody came back for her**.

The log and manifest prose moved from a private switch in `Map.Wreck.cs` into `Derelict`, beside the evidence
it has to agree with, and the renderer's second list of station names became `glyph + CauseStationName`. Both
were two places holding one fact, and both had drifted. `TenHullsTenStoriesTests` and
`EveryHullNamesItsOwnEvidenceTests` now walk **all ten causes** rather than asserting the three symptoms —
the failure mode is a cause nobody wrote an arm for, and only walking all ten finds the eleventh.

## 7c. The derived anomaly, and the audit of the other four (#533, 2026-09-19)

> *"The story ones are exceptional in some sense that we are left to wonder. Like what was such a rich ship
> doing there-kind of things 😎"* — owner, on this issue

An **anomaly** is a fact you can verify and cannot explain: two numbers the game already computes, read off
two instruments, that do not sit together — and **nobody aboard is left to ask.** `WreckAnomaly` (Core, pure,
seeded off the hull's id) deals **zero or one per hull**, and only ever one her own numbers really support.

**It is not a cause and never becomes one.** The report still names what happened; the anomaly is a fact in
the field book beside it, filed under the hull as a `📍` subject (#741). `Derelict.WreckCause` does not grow
a word for it, and a guard says so.

### How often

`WreckAnomaly.CarriesOneInN = 3` — one in three **of the hulls whose facts support one**, which is the only
honest place for a rarity budget to sit: a hull the numbers cannot say anything about is never dealt one
whatever the rate is. Measured over 400 seeded hulls on an unlisted road: **97 support one, 39 carry one** —
about one hull in ten of the whole fleet. FLAGGED for tuning, and the number lives in that one constant.

### The one that shipped

| | |
| --- | --- |
| **RICH HULL, POOR ROAD** | `Derelict.Wreck.AssessedValueCr` — the value on her own manifest — against `ArrivalTube.ScheduledTonnage` at the berth she was found off: the traffic that is **on a board**. #541's rule already says The Tilt and The Deep carry real tonnage and **none of it is listed anywhere**, so the second number was waiting to be read. |
| **the line** | *"Assessed at {value}. Traffic on this road, listed, this year: none."* |
| **in the book** | *"{hull} — assessed at {value}, no listed traffic on this road this year, and nobody aboard to ask"* |
| **"rich"** | the top quarter of the span every wreck's cargo is assessed inside (`Derelict.AssessedFloorCr … AssessedCeilingCr`), so retuning the span moves the threshold instead of leaving it behind. |

### The four that did not, and exactly what each one is waiting for

**Never invent a number to make one fit.** An anomaly the captain cannot go and check is not an anomaly, it
is a caption. The canon lines below are Fable-authored and kept **here** rather than as constants in Core: a
string in Core that nothing reads is a truth the game is not telling, and #646 found one of those the
expensive way.

| # | canon line | what it needs | status |
| --- | --- | --- | --- |
| 2 | *"Adrift {years} years. A search cone at this range closes in a season."* | an independent second number. `Derelict.SearchConeRadiusMeters` is `years × DriftConePerYearMeters` **and nothing else** — the two numbers the line puts side by side are one number written twice, so every inequality between them is true of every hull or of none. And nothing in the game prices a SEARCH, so "closes in a season" has nothing to be checked against. | **not derivable** — it would be a threshold that selects everything, this repo's fifth named bug class |
| 3 | *"Manifest: {n} lines. Hold: {m} lots. Nobody signed for the difference."* | a count of manifest lines and a count of lots. The manifest is a value and a sentence; the hold has no lots. | waits on **`ShipPapers`** (§7, build order 1) |
| 4 | *"Boat cradles: {c}, all empty. Crew, by the list: {k}."* | the crew list. The cradles are real and countable from the doorway (`WreckLayout.CradleCount`, `Derelict.LifeboatsLaunched`); **`{k}` does not exist anywhere in the game.** | waits on the **crew list and watch bill** (§7, build order 2) |
| 5 | *"Hull laid down {y1}. The pump on the forward bulkhead was made {y2}."* | a year on a fitting. Her laid-down year is real (`ShipHistories.For`); no fitting on any hull in the game carries a year of manufacture. A pump with a date on it is authored content, not a derived fact. | needs **content**, and should be authored rather than derived |

### The discipline, as guards

- **It survives being filed** — `ThePaperworkGainsNoCause`: ten causes, ten causes, and `Derelict.cs` does not
  name the feature.
- **No resolution, ever** — `NothingOutsideTheReadingAndTheBookReadsAnAnomaly` sweeps the whole `src` tree:
  exactly two files may name `WreckAnomaly`, the one that computes it and the one that reads it out and files
  it. A contact who explains one, or an arc card that resolves one, turns that red.
- **It concludes nothing** — `NoAnomalyStringConcludesAnything`: no causal word (*because, why, must, so that,
  explains, means*) and no reserved word (*kaamos, nebula, reever, fraud, warship…*) in any line or gist.
- **Only what her numbers support** — every dealt anomaly's inequality is re-derived from the hull's own
  numbers over the whole seeded population, both on a listed road and on an unlisted one.
- **The fittings stay identical** — the eight `StandardFittings` are untouched, the layout does not know the
  feature exists, and the frame-hash ledger for the wreck scenes did not move a row.

## 8. Rules kept

- **The station reports, it never concludes.** No page is ever labelled false by the game.
- **Two witnesses or nothing.** A single document is colour; the contradiction is the content.
- **It reads at every knowledge level** — odd detail, snag, or impossibility, and the game never says which.
- **Deterministic.** Authored pool, seeded per wreck; the same ship lies the same way in every universe.
