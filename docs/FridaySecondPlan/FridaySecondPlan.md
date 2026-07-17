# Friday, second wind — the long arm and the buried hoard (2026-07-17)

*Where the first Friday ended: fourteen PRs merged in one day. The morning train shipped the ship's
voice (#190), deliberate piracy (#186), orbit-KEEPING (#193) and three new labs (28/29/30); the
evening shipped the UI-clarity pass (#197), ⛽ BUY FUEL (#198) and the shuttle-bay airlock (#199);
the owner then played three hours on the fresh build — two missions, one pod pirated, the first
fuel ever bought — and filed a fifteen-finding storm that the night shift closed the same night in
five more lanes (#214–#218). Core suite grew 498 → 597. The §0 ruling was verified live at the end:
the banner over a held Enceladus orbit reads "🛰 AUTOPILOT HOLDS THE ORBIT — trim ≈27 p/day", the
exact number Lab 25 measured. Owner's verdict at bedtime: "Now the game is coming alive... The
world is bigger now in the game." And in the morning he brought a dream.*

## 0. THE DREAM — the owner's morning brief, distilled as design law

> *"What do pirates do when they fear the long arm of the law taking their loot? They bury it into
> treasure islands with treasure maps. ... [If the collectors catch us:] pop-up of options — bribe,
> resist boarding (one last stand with all guns blazing, Bolivia style). Is it game over if they
> catch us, or loss of loot and minimum fuel to reach fuel station? I guess it should depend on
> wanted level. ... I am thinking Grand Theft Auto BUSTED scenario here — cheapest would be they
> just steal certain amount / percentage of coin, like the parrot warns us. ... Having buried
> treasure chests [to] hide our loot could be our salvation. Those would also hide evidence against
> us. This could also be a mission type — go fetch a hidden loot for a customer from place X (buried
> on Phobos monument + y paces towards direction X). ... Our social connections [could] help us get
> our footing — gas by wire transfer via some anonymized darkweb way, with strings attached. ...
> We could also loan TO our contacts and use them as a bank of sorts."*

**Ground truth that makes this a founding, not a retrofit:** `EncounterRule` computes
`CaughtPlayer` honestly (catch radius + relative speed) — and the client does NOTHING with it.
A collector that catches you today simply goes inert. The entire catch experience is unbuilt.
Three lanes build it as ONE economy of consequence: the law takes what it can SEE; burying and
banking make loot unseen; contacts make recovery possible; Lab 27 supplies the honest physics of
the chase.

### PR-BUSTED — the catch encounter (never game over)

- When `CaughtPlayer` flips: warp yanks to 1×, the ship is grappled, and a boarding pop-up opens
  in the collector's voice. Options, GTA-BUSTED grammar:
  1. **SUBMIT** — they confiscate a heat-scaled share of what they can SEE: carried coin and
     hot-flagged cargo (proposal for the owner: heat 1 → 20%, heat 2 → 35%, heat 3 → 50% of
     carried coin + all stolen-flagged cargo; numbers are his to set). Heat clears to 0 — the
     debt is considered collected.
  2. **BRIBE** — pay a fixed heat-scaled fee to keep the cargo; heat does NOT clear (you bought
     this patrol, not the law). Declining is free.
  3. **RESIST** — the Bolivia option: the pop-up closes and the boarding becomes a fight on the
     existing weapons machinery. Win (break the hunter) → keep everything, heat pins to MAX and
     the next wave is meaner. Lose → SUBMIT terms plus a harsher cut (and a future damage-control
     hook).
- **The mercy law (the owner's "minimum fuel to reach fuel station", now computable):**
  confiscation NEVER takes the tank below `FuelReachability`'s nearest-pump reach + cushion, and
  never the last ~100 cr for the berth fee. Lab 28's rule becomes the law's own restraint — the
  law wants you taxed, not dead. Not game over, ever. (Do space pirates hang? No — the worst
  outcome is the centrifuge dock: broke, taxed, and grounded near a pump, spinning for gravity
  while you plan the comeback.)
- **The parrot warns the price**: at each heat crossing the squawk names the current confiscation
  share ("Heat two, captain — they'll take a third of the purse if they catch us!") — the #166
  channel already fires on exactly those edges.

### PR-HOARD — buried treasure (the shuttle door's second life)

- At any landable moon/asteroid in shuttle range, the bay airlock (#199) gains a **"⛏ Bury a
  chest"** destination: fly the door down, cache coin and/or cargo on the surface.
- **What burying buys**: cached loot is invisible to confiscation (the law takes what it can see)
  and stolen-flagged cargo buried is evidence off the books — a heat-decay assist while hidden.
- **The map is the artifact**: burying produces a treasure map — a full-screen card with a big
  background image of the body and a BIG CAPTION naming it ("PHOBOS — from the monolith, 40 paces
  anti-spinward"), bearing + paces from the best local landmark. Landmark quality varies by body;
  the monolith is the flagship — this lane pulls **#164 (Phobos + monolith)** onto the board as
  its first landing site. (Map art: the grok image lane is the established image source.)
- **X always marks the spot** (owner's Indiana Jones nod): every generated map carries a big red
  X, and the X is always exactly right — the in-world barflies insist "X never marks the spot,"
  and in this game the professor is wrong every single time. An honest game keeps honest maps.
- **The mission kind**: "fetch a cache" — a bar contact hands you a map to SOMEONE ELSE'S hoard;
  navigate, land, walk the paces, dig, deliver. The recovery flow and the mission flow are the
  same code path.

### PR-WIRE — the favor bank (both directions, on the ContactLedger)

- **BORROW**: a contact with real history (`ContactHistory.MissionsCompleted` — the ledger we
  seeded in #186) can wire anonymized gas money through the dark web when you're broke at a pump.
  Strings attached, owner's menu: a flat fee, an interest-bearing debt on the ledger, or a
  favor-debt — an obligation contract that arrives later ("you owe Madam Coil one quiet delivery").
- **LEND / DEPOSIT** (the owner's addition): park YOUR coin with a trusted contact — the bank of
  sorts. Deposited coin is invisible to confiscation like buried cargo; withdrawing needs their
  desk (visit or comms). Trust scales with history; the ledger shows balances both ways. A
  contact's goodwill is now a number you can literally bank on.
- One record, both signs: `ContactHistory` grows a signed credit balance; the celebration and the
  loot ledger already write to the same book.

## 1. Morning state (all merged, all live)

Fourteen PRs + one hotfix on main; suite 597/597; live site publishing on merge; §0 verified in
flight. The night also taught process law now recorded in memory: integration-branch-first before
any merge button, a brace-balance check after any hand-stitched CSS (the night's one casualty —
CSS has no compiler, only the bench flight caught it), and the bench flight is mandatory.

## 2. Broken windows (fit around the lanes)

- **#219** — the plan-trusting collision alarm is only wired on one arm path; the destination-card
  arm still cries wolf. Every arm path caches the plan's most-severe pass.
- **#220** — the backstop shouts red at a healthy HELD orbit; keeping earns the same alarm-trust
  as the armed plan (and the holding line should quote the park altitude, not the wobble).
- **#167** — burn FX (thruster flash + rumble + first audio), still unbuilt from Friday §2.
- **#172** — warp-to-arrival / skip-to-next-event, still unbuilt; easier now that the plan and the
  alerts both know every upcoming event.

## 3. The lab lanes

- **Lab 27 "The getaway" — PROMOTED: it IS PR-BUSTED's physics.** Escape-from-pursuit computed
  honestly: thrust-only hunters (by design) vs orbital tricks — the sling escape, the skim
  heat-bleed, the phasing juke. Produces the wolves' honest pursuit envelope (when a catch is
  physically earnable) AND the player's escape assists; RESIST/RUN odds in the pop-up quote the
  lab's numbers, not vibes.
- **Lab 26 "The soft arrival"** — Titan aerocapture as an arrival mode (96-pulse moon run → ~30).
- **Lab 21 "The Commuter"** — the Persephone cycler, still reserved, still wanted.

## 4. The theme lane

- **#160 tutorial mission** — the milk run, now narrated by a ship that speaks at every step.
- **#164 Phobos + monolith** — pulled onto the board by PR-HOARD (first treasure island).
- **#161 boot profile / #162 new-voyage picker** — housekeeping when a lane runs short.

## 5. OWNER RULINGS (answered over coffee, 2026-07-17 morning — now law)

0. **The dice are the engine.** A new pillar: TTRPG mechanics — roll for initiative, on-screen
   dice animation, mostly auto-played encounters with a few choices — are the DELIBERATE cheap
   way to make playable the things we don't want to deep-code (a little homage to the hobby).
   Up-speccing the Bolivia option is where many small purchasable helpers live (dice modifiers,
   never OP). One Core dice/modifier rule, every consequence system rolls on it.
1. **Confiscation**: the collectors take **all stolen cargo from heists committed while under
   heat** (hot-flagged at theft time). When heat fully cools, stolen cargo launders — it becomes
   safe(r). If the coin is all squirreled away (banked/buried), they take a MINIMUM from cargo
   instead — the law never leaves empty-handed. The mercy law (reach-a-pump + berth fee) stands.
2. **Bribe**: the amount is DICE — rolled on screen, modifiers visible.
3. **RESIST stakes ladder**: heat 1–2 → lose = harsher cut. Heat 3 → **the full Bolivia**: the
   encounter runs on dice (initiative, a few choices), and it ends one of two ways —
   **(a) FLEE**: you fight clear, the collector is left tied up at their own ship (that hunter
   disappears from the board), and you escape carrying heat 2; or **(b) THE FREEZE-FRAME**: a
   generated still in the spirit of Butch Cassidy and the Sundance Kid's last charge — two
   silhouetted rogues bursting into blinding muzzle-flash light, sepia freeze, game-over music
   under one massive volley (an HOMAGE composition: our pirates, no film-frame reproduction, no
   real likenesses; caption a paraphrased wink at "thought we were in trouble", not the verbatim
   line). Death is not deletion: **brain-backup resurrection** (the Iain M. Banks / Culture
   homage — tolerant and forgiving without ever being weak; a positive future, deliberately).
   You wake at the nearest haven's clinic in an insurance rustbucket; the ship and everything
   VISIBLE aboard is gone (this is where the kerosene-explosion art budget goes — pics of the
   ship blowing up) — and everything BURIED or BANKED is untouched. The hoard is the salvation:
   the dream's loop, closed.
4. **Cache discovery risk: YES** — caches can be found, by rivals and BY US (finding other
   people's loot is play); the risk is the reason to split hoards across many small caches.
5. **The favor bank runs on distress**: fencing money to safety WHILE HEATED costs a cut
   proportional to heat (dice-rolled) — always less than the collector would take; deposits made
   while CALM earn decent interest. Same dice engine.
6. **Withdrawal is in the contact's character**: each contact's sheet says how they deal — an
   asteroid hermit banks in person only; a dark-web-native contact wires anywhere. The character
   sheet (a small per-contact record on the ledger) is now canon.

## 5b. SECOND COFFEE ADDENDUM (owner, same morning — filed as #225/#226/#227)

- **#225 The vault**: the things of personal value — relationships, balances, caches, maps, dice
  items, insurance, sanity — persist as versioned, field-tolerant JSON + checksum (browser store +
  export/import). Old files load their understood parts forever; a failed checksum loads anyway but
  says so ("tampered — ledger marked"). Not a physics snapshot — a pirate's LIFE, durable. Ships
  right after the second-wind lanes land (they define the records).
- **#226 The sanity meter**: stress fills it (heat time, boardings, a survived Bolivia, long dry
  voyages); shore leave drains it (bars, tots, the Space Bar break — the Galley finally matters);
  a FULL meter triggers a dramatic dice-scripted episode, never a stat-death. Work-only piracy is
  now bad seamanship by rule.
- **#227 Pirate Insurance**: re-birth taxes you (clinic bill + the rustbucket as uninsured
  default); a purchasable policy softens it — better hull, covered bills, one honest sentence of
  fine print. The BUSTED lane ships the insurance SEAM today so the policy lane never reopens it.

## 6. The testing protocol (unchanged — it caught everything, including the lead's own brace)

Owner flies fresh builds hands-on and streams findings; the lead triages from the live tab with
code-level root causes, files issues mid-flight, dispatches Opus lanes same-session; merges under
explicit or standing approval only, integration-branch-first; Gemini cold-reads what ships; every
lab README pastes only numbers a probe actually printed. Non-worktree lanes own the main working
tree. The bench flight is not optional — it is the only compiler some bugs will ever meet.
