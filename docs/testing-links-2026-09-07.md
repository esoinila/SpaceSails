# Test links — the 2026-09-06/07 run (PRs #1080–#1182)

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A. Every link boots the live build at
`https://esoinila.github.io/SpaceSails-play/map?…` straight into the situation, using only cheats that
already exist (this run added features, not cheats). Read Appendix A for what each key does.*

The run: ~100 PRs merged, ~35 issues closed. Two halves — **the rulings you gave on 09-06 shipped as play**,
and **the #251 refactor phase finished** (nothing hand-written in `src/` is over 800 lines; the composed
page, the bundle and every pinned frame are byte-identical, so the second half should be *invisible* — if
anything looks different, that is a bug, not a redesign).

---

## 1 · Things to play (your rulings, as shipped)

| what | link | what to look for |
| --- | --- | --- |
| **The claim is the scene** (#1151 s1, PR #1155) — only your death is processed for you; every other loss is three presses at a `NEBULA MUTUAL · CLAIMS` machine on a concourse | `/map?start=wreck&target=collector` then get picked up and walk the concourse; or `/map?ashore=1&dock=ringside-exchange` to see the kiosk | the kiosk on great ports and working berths, never at outposts; policy → true name (a former name is refused) → wire entry; the payout arrives with the rep's line; the counter on the vault; the flashback card from the second claim on |
| **Lodge it with me, then** (#1151 s2, PR #1158) — the rep takes the same form the machine takes | any bar ashore with a loss on the wire — Fess's table (or Kolt's, on the moon) | the offer goes BEFORE the pitch; a half-filled kiosk form re-hosts on his card instead of restarting |
| **She gets her glory name** (#1151 s3, PR #1159) | `/map` → walk aft to the engine-room builder's plate | her seed dealt her the **bolted** plate ("Her name is on a plate bolted over another plate. The old bolts are a different thread."); ex-HALYARD under it; the kiosk's second press now has two rows |
| **A claims call costs what a laser ping costs** (#1151 s3) | captain's remote inside tight-beam range of a kiosk port | the same exposure as keying the beam — paid where the beam is keyed |
| **The writ that waited is served** (#1151 s4, PR #1162) — collectors act only where the master is; one writ, never a queue | `/map?start=wreck&target=collector` | `WRIT · AWAITING THE MASTER` on the ledger while you are off her; served at that berth when you are back aboard, on the day's terms; a second collector holds station |
| **Refuges are built to code** (#608 ruling → #1149, PRs #1152/#1156) — the failed one is a TOLD beat with art; the safety inspector is one more wallet ID | `/map?secretlab=deep` and walk the refuge floors; `/map?card=all` for a wallet full of IDs | the inspection tag on the rack; the failed refuge's plate + look card (quiet by design — say if you want a sentence); the inspector's card honoured at every refuge floor and a bet everywhere else; its fence price |
| **Reevers do not see through a closed door** (#442 → PR #1154; moon too → PR #1161) | `/map?land=1&shelter=1&reevers=2` — step into the shelter and shut the door | they lose you behind a shut leaf, in a hull and in a hut alike; a sleeper behind a door stays asleep |
| **The Old Ones use doors** (#563 Q2, PR #1157) — legacy and nostalgia, hiding what they can really do | `/map?found=1&reevers=3` or `/map?secretlab=deep&reevers=2&dark=1` | a locked leaf is a wall to them; an unlocked shut leaf they haul open at their own walking pace (the leaf slides part-way over first) and leave open; §10 of the worldbuilding notes carries THE BIG DOORS lore |
| **The station oracle can be left** (#997 wave, PR #1173) | `/map?oracle=1&ashore=1` | every pop-up in the game now has a driven way out — the undriven register is at zero |
| **The panel quotes the park the pilot flies** (#286 → #1177/#1179, PRs #1178/#1181) | `/map?dest=miranda` and arm the autopilot | banner row, holding line, coaching line and the emergency-descent tip all quote ONE number; no shipped body clamps (Miranda has 363× headroom), so this is a "nothing changed" check |

## 2 · Things that must look exactly the same (the refactor half)

The page (`Map.razor` 8,830 → 765 lines), the Nav HUD, the desks, the satchel (983 → 127 over ten
surfaces), the stylesheet (1,359 → 1,105), the Hive and Haven interiors, the surface HUD, every renderer —
all split by concern under laws that measure byte-identity (composed page hash, bundle rule-set hash, 66
frame hashes, 2,120 fingerprints, 32 seat fingerprints: 0 moved on every PR). A quick once-over:

| scene | link |
| --- | --- |
| the ship, the deck plan, the satchel's six pages | `/map` |
| the Nav HUD and a plotted trip | `/map?dest=titan` |
| a bar ashore, the case at a top | `/map?barcase=1` |
| the table scene and the counter | `/map?tablescene=1` · `/map?counter=1` |
| the park with the case out | `/map?park=1&spread=1` |
| the Hive, dark | `/map?found=1&dark=1` |
| a moon, a shelter, a sentry with rounds | `/map?land=1&shelter=1&mags=3` |
| the busted stages | `/map?death=collector` (any cause in Appendix A) |

## 3 · Rulings waiting on you (all on #938's 09-07 04:20 digest)

#1151 (one hull claimed twice? the rep's accept-button face; plate vs register wording; berth-not-orbit
service), #563 (Kolt's eyes; the captain's expedition fog), #1149 (inspection rate; a told beat for the failed
refuge), #997 (register complete?), and #422's use of the claims counter (Fable's design slice).
