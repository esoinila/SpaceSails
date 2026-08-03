# Playtest script — the main plot arcs (2026-08-03)

*Owner's live run, localhost. Server: `./run.ps1` (Release, port 5073). Every link is one beat;
the right column is what the beat must do. Stream reactions; the session converts them to fixes live.*

Base: `http://localhost:5073`

## Act 1 — PROJEKTI KAAMOS (the arc you must find)

| # | Link | The beat must… |
|---|------|----------------|
| 1 | [The front door](http://localhost:5073/map?kaamos=bounce) `?kaamos=bounce` | Freight docket at a mission desk bounces: *BERTH HELD, AWAITING CYCLER WINDOW*. "Held" should hook. Fee printed on the button. |
| 2 | [The plaque](http://localhost:5073/map?dock=ringside-exchange) `?dock=ringside-exchange` | Walk to the dedication plaque — fragment 1, earned by reading. |
| 3 | [The cold pod](http://localhost:5073/map?kaamos=pod&land=1) `?kaamos=pod&land=1` | Probe the ground: the pod, *40 souls, logged HELD, not lost*. New painting. |
| 4 | [Vantar's log](http://localhost:5073/map?secretlab=1&land=1) `?secretlab=1&land=1` | The lab console log — it never names the project, and nothing announces it. |
| 5 | [The berth-holder](http://localhost:5073/map?ashore=1&kaamos=holder&dock=ringside-exchange) `?ashore=1&kaamos=holder` | Boot straight into the bar (new `?ashore`). His line should land: *"You don't file for that berth, spacer. You keep it."* Portrait painted. |
| 6 | [The capstone](http://localhost:5073/map?kaamos=all&ashore=1) `?kaamos=all&ashore=1` | The berth-code resolves — plate, not toast. Ledger countdown counts to the gate (4), not the pool (5). |
| 7 | [The filing](http://localhost:5073/map?kaamos=hq) `?kaamos=hq` | File at the counter; the cycler window comes round; the news wire breaks the beat (first ArcNewsBreaks ever). |
| 8 | [The head office](http://localhost:5073/map?kaamos=hq&land=1&floor=24) `?kaamos=hq&land=1&floor=24` | 24 listed floors, **no unlisted band**. Lift asks nothing. Perfect order, nobody home. Floor 24: forty made beds and a forty-first — the 40-nerve throw. Also floors 23 and 12. |

## Act 2 — NEBULA MUTUAL (the arc you already live)

| # | Link | The beat must… |
|---|------|----------------|
| 9 | [Die once](http://localhost:5073/map?death=impact&start=earth) `?death=impact&start=earth` | Four-stage death card → wake → the **green glitch line**, one beat, never explained. |
| 10 | [The adjuster](http://localhost:5073/map?ashore=1&nebula=adjuster) `?ashore=1&nebula=adjuster` | *"Six times. Different faces, same number."* He has a face now. No button spends silently. |
| 11 | [Policy terms](http://localhost:5073/map?nebula=all&ashore=1) `?nebula=all&ashore=1` | The capstone names no shard; the truth notice rides its cold-archive plate. |
| 12 | [The archive node](http://localhost:5073/map?nerve=2&archive=1&land=1) `?nerve=2&archive=1&land=1` | The hold is three degrees warm, no warning, no noun. Five visions; at nerve 2 you can barely afford the dwell. |
| 13 | [THE CONVERGENCE](http://localhost:5073/map?converge=1) `?converge=1` | **Known open #422**: this card still announces the truth. Judge rec B (collision of testimonies) against what you feel here. |

## Act 3 — the world that isn't ours

| # | Link | The beat must… |
|---|------|----------------|
| 14 | [The monolith + the watch](http://localhost:5073/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1) `…&watchers=1` | 85 m, 1:4:9, a 370-du shadow to walk up, nothing between the landing band and the stone. Then **stand still 40 s**: the playground moment. Six variants, no creature, zero nerve. Nobody has seen this live. |
| 15 | [The false slab](http://localhost:5073/map?dock=the-tilt&body=miranda&site=0&land=1) `body=miranda&site=0` | Quarried, mortared, tool-marked — *human*. The difference from #14 must read without a word of explanation. |

## Encores

- [The oracle](http://localhost:5073/map?oracle=1&ashore=1) `?oracle=1&ashore=1` — Static Marsh, no longer repeating herself; Esc closes her card.
- [The bond](http://localhost:5073/map?ashore=1&bond=1) `?ashore=1&bond=1` — the cognac; it now files *"🥂 How you met"* to the ledger.
- [The scuttle death](http://localhost:5073/map?death=scuttled&wreck=1&land=1) — *"Her log will not mention it."*
- [The vented hull](http://localhost:5073/map?wreck=ventedbyoneoftheirown&land=1) — her log finally agrees with her evidence stations.
- [Hive for contrast](http://localhost:5073/map?secretlab=deep&land=1&floor=20&air=90) — a branch office, right before or after #8, to feel what "HQ outclasses them" means.

## Session rules (from the QA handoffs)

- The tab Claude opens is the tab you play — on any "look at this", Claude screenshots FIRST, then we debug.
- A finding needs its URL. These are the URLs.
- Kill + restart `dotnet run` after any fix lands (no `watch`).
