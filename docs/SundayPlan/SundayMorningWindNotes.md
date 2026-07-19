# Sunday morning wind — surfaces that differ, drinks that talk (2026-07-19)

*Provenance: owner rulings on Sunday morning before a two-day cruise (boarding ends 15:55).
Cruise mode: the PC stays ON, Fable orchestrates Opus lanes under the CI + UI gates, the owner
approves PRs from the Anthropic mobile app. The night shift (#353–#359) merged clean but is
largely unplaytested — the test matrix below is the safety net.*

## What the owner asked for

### The surfaces (variety is the law)
1. **Luna's outdoors must differ from Miranda's** (owner: "Earth Moon and Miranda out-doors were
   extremely similar maps... at least the walls of buildings should not be the same layout").
   The surface deck currently reuses one geometry; each landable body gets its OWN layout —
   authored or seeded-per-body, but visibly different ground.
2. **Outdoors everywhere they make sense** — the other shuttle destinations that can have an
   outdoors get one, so every port can hide / dead-drop something on nearby ground.

### The berth matrix (test every front door)
3. **Start a new game at EACH berth** and verify, per port: a walkable interior · a bar with a
   barkeep · patrons with tasks/rumors · an outdoors reachable by shuttle for hiding /
   dead-dropping. Findings become the gap list for the next lanes.

### The drinks that talk (the Larry menu)
4. **More than one drink type**: Space Gin, Space Beer, a local specialty per bar — colorful
   descriptions in the style of Leisure Suit Larry; more kinds welcome.
5. **The choice is the tell** (owner: "we want the drinks to give that little channel of info"):
   every character has a FAVORITE; when you offer a drink, what they choose — and what they say
   over it — tells you something. Learning a contact's favorite is progress; the drink is a
   dialogue channel, not just a trust coin. (Builds on #355's offer-first flow and #347.)

### Missions stay in the neighborhood
6. **Mission generation prefers nearby destinations** (owner: "adjust the missions to prefer
   staying in relatively nearby places. Having 10 year flights should be an exception in mid
   mission, not anything casual :-D"). The offer mix weights local-system hops (same planet's
   moons/stations, then neighbor planets) heavily; cross-system sagas appear RARELY — and when
   they do, #357's HaulReward already makes them pay like the exception they are. Pay-scale and
   offer-frequency are two halves of one law: long = rare + rich.

### The art backlog
7. Work the outstanding Grok image queue (hoard manifest, posters, gear art hooks from #348,
   plus whatever the surface-variety and drinks lanes need). Grok = images only; Fable runs the
   easel.

## Implementation queue (Sunday, pre-boarding 15:55)
- **Gate first**: the #293 CI UI gate (in flight) merges before anything else — it is the
  remote-approval confidence layer for the cruise.
- **Lane: per-body surfaces** — Luna ≠ Miranda ground truth (#1–#2).
- **Lane: the talking drinks menu** (#4–#5).
- **Audit: the berth matrix** (#3) — code-audit agent produces the port-by-port table; browser
  spot-checks after the gate lands.
- Art easel runs between lanes (#6).
