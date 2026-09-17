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
