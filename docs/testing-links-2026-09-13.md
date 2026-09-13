# Test links — the 2026-09-12/13 night run (PRs #1184–#1194, #1196)

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A. Every link boots the live build at
`https://esoinila.github.io/SpaceSails-play/map?…` straight into the situation, using only cheats that
exist in the query whitelist. Read Appendix A for what each key does.*

The run: **eleven PRs.** Eight are play you can reach from a URL; two are named remainders of
things that shipped earlier in the week; one (#1189) is an instrument rather than a scene. **No drawn
frame changed anywhere in this run** — the 66 phase hashes and the 32 seat fingerprints reported *zero
moved* on every single PR — so §2 is the half that must look exactly the same.

*(#1196 landed the morning after and is filed here rather than alone: same base, same batch. #1195 is
a size-law refactor with nothing to play.)*

---

## 1 · Things to play

| what | link | what to look for |
| --- | --- | --- |
| **The fine that closes a folder** (#711 slice 1, PR #1196) — *"A cover is a small stack the player assembles: the worn identity, one sacrificial truth, and what it protects. Getting caught at the sacrifice COSTS something real (the fine, heat, a confiscation — it must hurt, or discovery feels fake) and PAYS something real (that entity's suspicion is spent; the challenge/inspection outcome for them is settled until new cause)."* (head coder, canon 2026-08-09) | **No single link reaches both ends, and here is why, honestly**: the desk that hands a parcel over is aboard a ship clamped at a berth (`?dock=`), and the round that finds it walks a floor underground (`?patrol=1`, which implies `secretlab=deep&land=1&floor=2` and rides the shuttle down at boot). **Two steps, one boot:** `/map?dock=the-space-bar&patrol=1` — the deep rock co-orbits The Space Bar and the clamp is kept across the excursion, so **(1)** ride the lift up and end the excursion, then **Comms desk → 🕸 dark web market → `📦 Take it`**; **(2)** shuttle back down to **The Deep Hermit's Rock** and take the lift to **B2**, where the forced round is still walking (the `?patrol=` count lives for the boot, and `SpawnPatrolFor` re-reads it on every lift ride). **Do not split it across two URLs**: a `?dock=` boot builds a fresh world and never peeks the vault, so a parcel taken on one link is not aboard on the next. | **At the desk**, a fourth off-the-books row under the chip's buyer, the inspector's card and the fence's key: `UNLISTED PARCEL`, and under it *"Somebody's small parcel with nobody's name on it. The kind of thing a hauler carries and does not list."* The verb is `📦 Take it` and it carries **no price** — no coin moves. Taking it says nothing out loud (the look card in the satchel is what you read about it), and the row is then **absent, never greyed** — one per hull at a time. · **On B2**, let a guard see you. The card is the round's **own** — same label, same painting of the same man — and the body is the whole of it: *"An unlisted parcel. Fined, filed, and the inspector is already looking at the next hull."* Coin leaves the purse (the BUSTED card's own bribe, called and not restated, at **this outfit's** meter — so the same captain is quoted differently at a hot haven), the box is gone from the satchel, and the field book reads *"A parcel found aboard that was on no manifest. Paid the fine on the spot and watched it carried off."* **No paper is ever asked for**: the read is over at the parcel. · Then the payoff, which is **a silence**. That outfit's rounds simply stop stopping — walk the same floor and nobody walks over. No card, no pulse, no field-book line says so, and none should. · **The tell** — *"An unlisted parcel. The inspector notes it, does not reach for the fine book, and keeps looking."* — is **one outfit per world seed and never the first one to catch you**, so it needs a fine already paid somewhere; when it fires there is **no fine and nothing on file**, it rides the **front** of the ordinary wallet card with every consequence under it landing unchanged, and the book reads *"A parcel found aboard that was on no manifest. Carried off, and no fine written."* |
| **The moon is a safety-deposit box** (#319 slice 1, PR #1194) — *"Let's add another options to hide onto the planet / site. Anything from the inventory that is light enough… hiding evidence of our piracy without losing it. Maybe we steal something too important to risk carrying it around, like a superchip prototype etc."* (owner, live 2026-07-18) | `/map?fetch=picked-chip&dock=the-space-bar` — the roadster's **data chip already in the pocket** — then walk to the **shuttle-bay door** and open the boarding panel for Phobos | the panel that packs the chest grows one row: **🎒 BURY A THING FROM THE SATCHEL**, listing what is light enough in the order it is carried, named by the satchel's own namer. Press to pick (`◉`), **press again to un-pick**. It is **absent**, not greyed, on an empty-handed captain. Then ride down and press `[E]` on bare regolith: the **same** `DIG HERE`, the same 2D6, the same ✗, the same return dig — and the register, said once when what goes in is not the chest: *"Not coin this time. Something that is safer in the ground than in the hold."* Dig it back up and it is in your coat again. |
| **The absence has a shape** (#1063 s2, PR #1184) — a kind of note nothing else in the game files under | `/map?buried=1&land=1` → ride down, find the **maintenance ledger** in the first room searched on the listed bottom, then open 📓 NOTES | **two** entries under that ground, not one. The ledger you carried out, and under a mark you have never seen — **⬚** — the captain's own hand: *"Looked for the paper a raise this size leaves: permit, complaint, invoice. Nothing under either story. The absence has a shape, and I have measured it."* Written **once per ground**; the ledger's own entry is untouched beside it. On THREADS it stacks under the site's operator. |
| **Sealed ≠ full** (#1063 s2, PR #1184) — **no cheat, on purpose** | `/map?expedition=1` → take the team down, then force **every** `⚙ SEALED DOOR` on the ground | one leaf — the same one on that moon every time — opens on a room with **nothing in it**: no landmark, no `🗝 DISCOVERY CACHE`, no credits, no name, and the walls and bounds exactly as they were. **No reveal cue, no card, no ring on the fan.** Said once: *"Sealed, and empty. A room somebody closed because there was nothing in it, which is a reason."* Once per captain-lifetime — force every door on the next three sites and all of them pay. |
| **The worst page comes out** (#798 item 2, PR #1185) | `/map?spread=1` (seated at a cabinet top, three finds in the sleeve) | every **worked, multi-page** paper row now carries a third control between the pen and the shredder. Press it: the document becomes **two rows** — `…, page 3 of 4` and `…, 3 pages of 4`. Said once, at the press: *"One sheet, folded twice, goes where the book goes. What is left is a folder anyone would be bored to find."* A one-sheet paper, an undug one, and an already-split one **draw no control at all** (a verb that does not apply is not a refusal). Stand up and the press refuses out loud. |
| **…and the folder goes in a bin looking like nothing** (#798, PR #1185) | `/map?rip=1` → dig a paper at a table first, split it, then 🗑 the folder | the filed note's last clause is now the folder's own: *"A file about nothing much, in a bin, where files about nothing much go."* Bin the **sheet** instead and the note says the other thing. |
| **A file on somebody comes apart too** (#798 remainders, PR #1188) | same `/map?spread=1` — the third find in the sleeve is a **dossier** (`🗃 a file on somebody`) | the dossier row splits exactly as paper does, and its two rows are `a file on somebody, page 2 of 3` / `a file on somebody, 2 pages of 3`. **The kept sheet now costs the sleeve nothing** — split a document with a full sleeve and the sheet goes in while the folder is refused. The **compromising chip** from between the roadster's seats is never offered the scissors (it has no pages). |
| **The rota witness stops working for free** (#417, PR #1186) | `/map?finder=1` ashore, then walk up to the case's witness at the bar | walking up no longer hands you the lead. It says, once a watch: *"I work the rota. I don't work for you."* **Buy him a glass** through the bar's own offer — the case joins him to the drinkable list while it wants him loosened. Refused: *"Keep it. I'm on shift."* — the watch is spent, the lead is not; come back next watch. Taken: *"One drink. Then I never saw you."* and the lead files in the same press. |
| **The key's other two roads** (#535 s2, PR #1187) — **the fence** | `/map?dock=the-space-bar&credits=5000` → Comms desk → 🕸 **dark web market** | a `🗝` row beside the chip's and the inspector's card: *"Fresh this cycle. Ask me what it costs and I'll ask you what a clean record costs."* · **Buy the key**. The price is **three times the BUSTED card's bribe** and moves with your heat. The row is **absent**, never greyed, when the port has already dealt one this watch. |
| **…and the favour** (#535 s2, PR #1187) — **no boot cheat exists for goodwill** | sit with a contact you are **close** to (goodwill ≥ 6) and have never lied to, at any bar; the verb is on the seat's own card | **Take the favour**, and the line is printed *before* the press, not after: *"You never lied to me. That is rarer than what I'm about to hand you. Don't bring it back."* It costs exactly the band that qualified it, once per contact per lifetime. **Closing the card declines** — there is no decline button, and no greyed verb (a greyed verb would announce that this person has something). Taking it at the bar **also empties the fence's shelf at that berth for the watch**: one key per port per watch, two writers, one tag. |
| **Trying to be dockable, or capture, or aboard her** (#243, PR #1189) | `/map?target=npc-0` (her dossier up at boot) → **click her hull on the map** to make her the capture target | a new **conditions strip** in the Nav readouts, above the clamp panel: `🎯 Boarding <callsign> needs:` and one chip per criterion — range and rel-speed, each with its reading, its gate and ✅/❌. **▼ means improving, ▲ means worsening** (hover says the word); the rel-speed chip carries **no** arrow, deliberately. Mirrored in the Scope's top-right corner as a glyph plus one ✓/✗ per chip. The same strip reads `⚓ Clamping at <haven> needs:` on a dock approach and `🛰` while an insertion is armed; **no gate live ⇒ no strip at all**. |
| **The frame that was eating your cruise** (#242, PR #1189) | `/map?dest=jupiter` → Plot desk, plan a burn | a `text-info` line in the **Plot panel head** whenever ≥15 % of the ship's motion is motion she shares with the plot frame, with a one-press `☀ switch to Sun` chip that goes through the page's own frame verb. The standing explanation is in the ⓘ **Plotting** card as a quote paragraph, not a seventh step. |
| **THERE SHE IS** (#238, PR #1190) | `/map?fetch=intel` → press **🔭** on the quest card → warp until the pass lands | the reveal is no longer a marker and a checklist tick. Bird **first** (*"DUDE. THERE. Is. The CAR!"*), then a receipt in the ledger's `🎯 Mission` section, **warp yanked to 1×**, then the card: `🔭 THERE SHE IS — the roadster, sunward of Mars`, with `show me` and `Close`. ESC dismisses. Boot cheats that park you alongside her stay silent. |
| **GOT IT** (#238, PR #1190) | `/map?start=wreck&fetch=active` → prise the wallet · and `/map?start=wreck&fetch=active-chip` for the twin | `💾 GOT IT — the wallet, from between the seats` (and `— the data chip, from between the seats` on the 1-in-4 twin), same card at the smaller size, **no `show me`** — the ship is alongside the thing she just prised. **`?fetch=…-chip` has never worked until this PR**: the injector read the suffix, the whitelist two files away did not. Worth one press of `/map?start=wreck&fetch=intel-chip` to confirm the twin's whole arc now opens. |
| **The 77 % sweep that hid the hunt** (#238 b3, PR #1192) | `/map?fetch=intel` → 🔭 on the quest card (queues the aimed job) → Sensors desk → start a **hand-flown sweep** | under the Sensor-tasks box header (the hand-flown sweep has no row of its own), the owner's own sentence: **`sweep holds the scope — 🔭 Roadster fix waits behind it`** — and the **same string** on the Sensors chip's second line, standing on the track count and never on the beacon line. It goes quiet the moment the sweep is stopped, and it says nothing when what is queued behind is routine. |
| **The tourist shop becomes an outfitter** (#325/#332, PR #1191) | `/map?dock=the-space-bar&credits=5000` → the berth's services column | two new cards beside the pump and the armory. `🫁 EXTENDED TANK` — **80 cr** here, 100 at The Deep (it is `RoundPrice × 2`, per house). They stack; one is fitted at the start of the next excursion and says so once: *"Extended tank fitted. The arithmetic is kinder today: twice the walk, and the walk back is still half."* Then `/map?dock=the-space-bar&land=1` — the ground itself reaches twice as far (the backstop, the wavy edge, the air meter's full mark) and **the reserve is untouched**. |
| **The refill the cabinet promised since #343** (#332, PR #1191) | `/map?nerve=1&dock=the-space-bar&credits=5000` → med bay, swallow the cabinet empty, then the berth's services column | the empty line no longer names the backlog: *"MED KIT: the pill cabinet is empty — the calming stock is spent. Any haven's chandlery sells the refill."* `💊 MED-KIT REFILL` charges **one glass per pill missing** (36–48 cr for a full cabinet), so swallowing one costs one. |

**Three mechanical strings changed with #1194 and are flagged for your voice if you want them** (all
three are plumbing, not prose, so none was written as canon): the bury pulse's noun
(`Chest buried` → **`In the ground`** when no chest went in), the surface HUD's holding line
(`CARRYING THE CHEST` → **`SOMETHING TO PUT IN THE GROUND`**), and the recovery tail
(`1 thing from the satchel back in the satchel` / `…stays down there — no room on you for it.`).
The manifest **counts and never names** — `1 thing from the satchel` — so a hoard line read over your
shoulder in a bar is not the one place in the game where burying evidence advertises evidence.

**Still open on #319:** dead-drop messages for a named recipient, the geocache escrow / dark-web sale,
the NPC-insurance ledger flag, and the counterfeit ✗.

## 2 · Things that must look exactly the same

**The two ledgers that measure a drawn frame did not move once this run**: `FrameHashes` 66 rows, 0
moved; `SeatFingerprints` 32 rows, 0 moved — on every PR in this run, **#1196 included** (its diff
carries no `.txt` at all). Everything that *did* move is **state shape**, and each row names its own
cause:

| PR | what moved | cause |
| --- | --- | --- |
| #1184 | 30 sweep rows, 830 → 831 fields | `_emptySealSpentOn` |
| #1186 | 30 sweep rows, 830 → 831 fields | `_finderWitnessWatch` |
| #1190 | 30 sweep rows, 832 → 836 fields | the four mission-moment fields |
| #1191 | 30 sweep rows +1 field; 15 `the surface` rows +64 chars | `_extendedTanks`; the excursion's tank flag — **every `AirSeconds` reading re-pinned unchanged** (1200 · 1223.8 · 1247.8 · 1275) |
| #1185 | `SatchelPanelMarkup.baseline.txt` +18 lines | exactly the split control |
| #1189 | `NavHudMarkup.baseline.txt` +71 lines, **zero deletions** | the two new surfaces only; nothing existing moved |
| #1192 | `TrackingPostMarkup.baseline.txt` +2 blocks | the hold line at its two render sites |
| #1187, #1188, #1194 | **nothing** | no ledger, no baseline, no razor change (#1194's row is inside `BoardTargetCard`, which no baseline covers) |
| #1196 | **no ledger row and no markup baseline** — but **five xUnit guards** moved | each for a named reason, spelled out under the table |

**#1196's five, and why each moved.** None of them is a picture: no `.txt` baseline and neither frame
ledger is in the diff, so nothing drawn changed. What moved is five pinned guards, and the argument for
each is the thing to check rather than the number:

1. **`TheBlackOpsKeyTests` — the `Satchel.Kind` roll-call** gains `"Parcel"` **at the END**. Every
   ordinal below it is untouched, which is the whole of what that guard polices: nothing was inserted,
   so no existing save reads back as a different object.
2. **`TheSatchelTests` — the "future kind" junk row.** It read `"9:1:x"` and was green only while 9 was
   past the end of the enum; appending turned it into a perfectly good parcel and the guard caught it.
   It is now **derived off the enum's own length**, so the next appended kind is caught by this case
   rather than quietly retiring it.
3. **`TheEscortIsAWalkTests`** follows the walk-up's new signature —
   `TheRoundStopsAtYou(ex, g, book, simTime)`.
4. **`ALockedCubicleBuysTimeTests`** follows the sighting loop's —
   `StopTheRoundIfAnybodySeesYou(sight, book)`. The law it pins (nobody new notices you while the catch
   is over) is untouched; **CI caught this one**, not the author's local filter.
5. **`ThePatrolKeepsItsOwnStateTests` — the page ratchet, 21 → 23.** The one that needs arguing, and the
   guard's own note says so: the round learned to find a parcel and needs two things the floor cannot
   derive — **`WorldSeed`** (which universe this is, folded to an *answer* rather than the thread id) and
   **`PayTheFine`** (a *verb*, not the purse: the round cannot read `_credits` and cannot spend it on
   anything else). The ledger and the sim clock ride `AdvancePatrol`'s existing parameters instead,
   which is why it is two and not four.

**And the twelve readers #1194 audited must each be exactly as blind as they were.** *Nothing was
taught anything and no flag was added* — a buried row simply leaves the one `_satchel` list, so it is
absent from all twelve at once. If a buried thing is still visible in any of these, that is the bug:

`BinnableFinds` (🗑) · `SpreadableFinds` (✍) · `CompromisingChip.InThePocket` (the blackmail client and
the fence) · `CarryingABlackOpsKey` (the BUSTED wake) · `Inspectorate.Held` · `AuthorityCardIds` (the
lift) · `PatrolBeat.BadgeHeld` (a guard's beat) · `CanteenTable.Cover.Held` (the mess-hall cover) ·
`Wallet` (a locked door) · `TableShowables` (a bar table) · `CountOf(Rounds)` (a hand-load) ·
`BuildVault` → `SatchelSection` (the save).

So a difference anywhere below is a bug, not a redesign. A quick once-over:

| scene | link |
| --- | --- |
| the ship, the deck plan, the satchel's pages | `/map` |
| the Nav HUD with **no** gate live — no strip at all | `/map?dest=titan` |
| the clamp panel's own rows on a dock approach | `/map?dock=selene-gate` then undock and come back in |
| a bar ashore, the case at a top | `/map?barcase=1` · `/map?tablescene=1` |
| the seated spread and the bin | `/map?spread=1` · `/map?rip=1` |
| the Sensors desk with nothing held up | `/map?dock=the-space-bar` → Sensors |
| a standard-tank excursion (unchanged arithmetic) | `/map?land=1` |
| the Hive, the ground, the busted stages | `/map?found=1&dark=1` · `/map?buried=1&land=1` · `/map?death=collector` |
| a patrolled floor with **nothing** in the pocket — the wallet ladder exactly as it was | `/map?patrol=1` · `/map?badge=1` |

## 3 · Rulings waiting on you

- **#711** — four judgement calls the slice flagged for you. **(a) What the parcel is *for* is slice 2.**
  Today it is taken and one day found; there is no delivery, no payer and no consequence for losing it
  other than the fine — deliberate for slice 1 (Layer 1 has to exist before Layer 2 can be protected by
  it), but it means a captain who is never inspected has carried a box for nothing. **(b) The ID-CHECK
  gate band and the lift panel's card-read do not find it.** Only the round on foot asks about a parcel;
  widening it is a design call (is a machine a thing that finds a box?) rather than a build one.
  **(c) The tell is one outfit per world seed, and never the first one to catch you** — the mechanic is
  taught by the fine that closes a folder, so the first find must be a fine or the exception has nothing
  to break; on that arm no folder closes and the captain is told nothing. **(d) No `docs/features/`
  section** — the three neighbouring off-the-books rows (#233, #1149, #535 s2) have none either, so this
  follows them; say the word and all four get one. Two lesser ones: the row costs **nothing** at the desk
  (a price would need a carrying fee this economy makes no statement about, and a receipt is the opposite
  of the object), and the fine **floors at zero, not at `BustedRule.MinBerthFeeCr`** (that floor is the
  confiscation's mercy law, and a fine is a form, not a seizure).
- **#1063** — the early-gate on the empty seal is a real law that the ordinary game almost never
  exercises (one function, one guard, if you want it expressed differently).
- **#798** — a torn sheet costs the sleeve **zero**, not a fraction (integer arithmetic; the alternative
  is rescaling the 24 you can read). `Satchel.SpaceLine` still says *"Room for N more sheets"* while
  counting documents — fixing that is prose, so it wants a Fable line. The folder still plots at the
  tracker, and the bin is still not chosen.
- **#535** — the fence's window is **a watch**, not a day (one call from being a day); taking the favour
  at a berth silently empties the fence's shelf there, by design and never announced.
- **#243** — the strip sits **above** the clamp panel, so a dock approach reads the same numbers twice;
  the piracy pop-up's numbers changed hand (`1.20 M km` → `1,200,000 km`); ▼/▲ sense.
- **#238** — two of the four by-itself transitions (the hunt kill, the moon-haven delivery) get **no**
  card, on the grounds that they are already the loudest thing on the glass; four words in the register
  make them loud. The chip twin's item phrase is assembled, not authored.
- **#325/#332** — **a haven added without a bar would have no chandlery.** True of no haven today
  (7 interiors, 7 keeps); either every haven gets a keep or the fallback rate wants naming.
- **#1149** — a haven's chandlery row for a berth with no keep would want a house voice, if you want it.
- **#319** — the weight threshold is **1 and refuses nothing today**, deliberately: everything a captain
  can walk with costs one place or none, so the rule bites the day something costs two rather than
  inviting a second weight scale now. **One thing at a time** (the cache's field is a list, so this
  widens later without touching the vault); the pick is made **at the shuttle door**, not at the shovel,
  and it is a pointer — four rounds picked and two fired bury two. A pocket with no room on the way back
  up **leaves it in the ground at the same ✗** rather than destroying it. Only a chest in the sling
  spends the purse. And the three mechanical strings above, if you want them in your own voice.
