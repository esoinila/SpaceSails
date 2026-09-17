# Test links — the 2026-09-17 run (#161, #1199 follow-ups)

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A. Every link boots the live build at
`https://esoinila.github.io/SpaceSails-play/map?…` straight into the situation, using only cheats that
exist in the query whitelist. Read Appendix A for what each key does.*

Two owner rulings from 2026-09-17, both of them "what shipped is right, the *clock* on it is wrong".

---

## 1 · The observation walk, at a wait you can sit through (#1199, PR #1201 follow-up)

Owner, on the walk as #1201 shipped it: **"YES, shorten the wait"** — it was
`Escort.PatienceSeconds`, a quarter of the bar's own watch, and every NPC-patience clock in this game is
**sim** time. At warp 1 that is **one hour of the player's own evening**, spent standing at the mouth of an
empty tube with nothing to do. The walk now has its own named fraction, `ObservationWalk.WaitFraction`, at
one eightieth of a watch: **180 s of sim time — three minutes at warp 1.**

Everything else about the beat is unchanged, deliberately: the walk still keeps Selene Gate's **edge 5**
(the owner's other ruling the same day — the MEDBAY panel stays spent), it is still walked **after last
call**, the card, the note through the one funnel, the spent-once key and the later pulse are all verbatim,
and **no other patience clock moved** — an escort still stands in a cabinet doorway for a full hour of
station time.

| what | link | what to look for |
| --- | --- | --- |
| **The walk is empty, and you can play the wait** (#1199 / #1062, this PR) | `/map?dock=selene-gate&ashore=1&simhours=4` — clamped at Selene Gate, ashore in **THE EARTHRISE BAR**, with the station clock four hours in so the room is **past last call** (the walk is only dealt in the last quarter of a watch; last call is at 10,800 s of 14,400). The person of interest at this berth is the haven's own named regular, **GILT-EYE**, and the rota has him at the bar on this watch. | He gets up from his top and crosses the concourse **due west**, out onto `OBSERVATION WALK` — glass floor, the limb under it, a rail at the blind end. Follow him: stay inside legible range with a clear line and **he stops and waits for you to go past** (no card, no line — he just turns). Break the line, and on a frame nobody is looking at him he is simply **not on the floor any more**. Then walk out to the rail yourself. **At warp 1 the card comes up about three minutes after the last moment you had eyes on him** — it used to be an hour. The card is the absence; the note files under his own name on THREADS; nothing is pulsed over the card. |

**Reading the wait off the clock while you play:** the wait is measured from the last frame he was visible,
in **sim** seconds, so the warp slider is the fast-forward if you want it — but the point of this change is
that you should not need it. Sit at the rail at warp 1 and the beat lands inside a normal pause.

**What would say this regressed:** the card arriving the instant you reach the rail (the wait stopped being
waited), or the watch turning over before it arrives at all (the wait grew back past the quarter-watch of
room the room has left after last call).

---

## 2 · The boot, with no long freeze in it (#161, this run)

Nothing to play and nothing that looks different — this is the half that must look **exactly** the same.
Boot any link in this file, or any of Appendix A's, and the world is byte-identical to the one the previous
build produced from the same seed: the same eight founding freighters, the same pods, the same depots, the
same mass-driver cadence, in the same order.

What changed is *when the browser gets the frame back*. The founding traffic is now planned one **piece of
work** at a time rather than one ship at a time, so the longest single block the boot hands the main thread
falls from **5.09 s to 1.54 s** on a dev (interpreted) build. **What a tester should see: nothing** — no
"page unresponsive" dialog on a cold load, a front door that is still pressable in a fifth of a second, and
a loading line that now counts the freighters up in smaller steps. Open the browser console and the boot
narrates its own stages under `[SpaceSails] boot ·`, each with what that stage cost; the gate reads the
worst of those lines as a budget of its own.

---

## 3 · Every hidden key comes out from behind the furniture (#440, this PR)

Owner, live 2026-07-26: *"Do an issue about all hidden functionalities like that so they are really clearly
told to a new player somewhere on the UI."* #443 fixed the shovel and #444 gave the ground its one lesson
card; this is the sweep of everything else. Four keys were being met by accident, and one of them
(**`B`**) was named in no in-the-moment telling anywhere in the shipped game.

Nothing below is a new panel or a new pop-up. Every word lands in a strip, a caption or a hover the game
already draws.

| what | link | what to look for |
| --- | --- | --- |
| **`B` — the favour bank, at the one fixture that has one** | `/map?ashore=1` — docked and already standing in the bar. Walk to any **◈** patron's table. | The strip along the **bottom of the screen** used to read one fixed sentence — *"docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ Q — helm"* — whatever you were standing at. Step inside the `[E]` ring of a patron's table and it **grows a rung**: `💰 B — open an account at this table`. Step away and the rung goes. Press `B` and the favour-bank card opens on that contact; press it a pace further off and you still get the old refusal (*"Stand at a contact's table to open an account."*) — the bar offers the press exactly where the press answers, which is the `[E]`-plate law (#870 lane 7b) applied to the second verb at the same fixture. |
| **`M` — the mute, off the regolith** | any of the links above, or `/map?start=wreck` | Last rung on the same strip, in the words the ground's keybar has always used: `🔊 M — mute` / `🔇 M — unmute`, and it **swaps as you press it**. Before this, `M` was written down on the surface keybar only — a captain who never landed never met the sound switch at all. |
| **`H` — the captain's remote, on the ground** | `/map?dock=the-tilt&site=0&land=1` — boots you on the regolith with the sling loaded. | The keybar along the bottom now carries `🤖 H — weapons tight` **while you have a bot with you**, and flips to `🤖 H — WEAPONS TIGHT (press to free)` once it is set. Compare a derelict (`/map?start=wreck`, board her): the wreck's bar has said this since #538 — the ground, where the sentries were invented and where the pack actually comes, never did. |
| **`+` `−` `↑` `↓` `Shift` — the drive** | `/map?start=wreck` (any free-flying start; a berth start has no Nav toolbar) | Hover the **⛽ FUEL** gauge on the Nav desk. It used to read *"Reaction mass: 40 of 40 pulses"* and stop; it now finishes the sentence — *"— + / − (or ↑ / ↓) fires one; hold Shift for a ±1% trim"*. This is the only control in the game with **no button anywhere**, so the gauge that counts the pulses is the only honest place to say how one is spent. |
| **`P` — the plotting table** | `/map?start=wreck&dest=saturn` | Hover **🗺 Plot** on the Nav toolbar: the tip already named the body it would aim at, and now ends `(P)`. Press `P` with nothing focused and the table opens; the tip then reads *"Back to flying live… (P)"*. |
| **`V` — the vent** | `/map?scenario=electric` | Open the hull-charge board and hover ⚡ **Dump her charge**: the sentence ends `(V)`. The point of the key is that an arcing hull is a hull you are *not* standing at a console for. |

**And the half that must look exactly the same.** On the **regolith** nothing about the bar moved but the
one `H` rung: `WASD`, the `[E]` ladder (your feet, then the bin, then the ground), `T`/`⇧T`, `G`, the
satchel's `I`, the shoot-the-lock plate and `M` are all in the order they were in, and the standing prompt
and the tracker captions are untouched. Off the regolith, a deck with **nothing to add** — mid-flight, on
her own bridge — draws the same two fixed sentences it always drew, because the page hands the renderer
`null` and the renderer falls back to its own consts.

**What would say this regressed:** the `💰 B` rung showing at a table you cannot actually bank at (or
missing at one you can — the bar and the key ask the same `NearestConsoleSpot`), the deck strip losing its
`Q`/`E`/`WASD` opening, or the ground's keybar naming `H` while the sling is empty.

**The law behind it.** `EveryBoundKeyIsNamedOnTheGlassTests` sweeps both key tables and fails if a bound
key is not named by some in-the-moment telling. The Captain's Guide is deliberately not one of the sources
it reads — it names nearly every key in the game, and a guard that admitted it would pass on any world at
all. Nor are code comments: the first draft passed for `P` off one of its own justifications.
