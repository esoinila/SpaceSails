# Flight assists — the sling, the skim, and the skip

*Owner direction (2026-07-14 evening): bending the track off close planetary passes should be
possible without tedious burn-tuning; Stargate Universe's gas-giant drag braking; the Apollo
return's atmosphere bounce as a thing to respect; maybe an "orbit this planet"-grade one-button
UI, used in tight spots to lose pursuers. Plan-only — the how, the honesty, and the build order.*

## Why this fits the game we already have

Three facts make these assists natural rather than bolted-on:

1. **The math is already ours.** Lesson 19/20 built b-plane aiming and Newton shooting against
   the live integrator (`ShootTo`, the crank tables, the lever warnings). A "sling" node is that
   code with a UI on it — the labs were the R&D department all along.
2. **Hunters are thrust-only by design** (the wolf-aim decision): they model neither gravity nor
   any atmosphere. A slingshot bend — and even more a drag pass — breaks their pursuit geometry
   *honestly*, no new cheat needed. These maneuvers being the canonical escape moves is already
   implied by the physics we ship.
3. **The plot desk already owns the grammar**: nodes on a scrubbable timeline, closest-pass
   markers, the armed "orbit this planet" insertion. Assists are two new node types in an
   existing language, not a new screen.

## The three assists

### 1. The sling (gravity assist, without the tedium)

Where: the plotting desk, on any **closest-pass marker** the projected track already computes.

UI: click the marker → **"⤴ Sling past ⟨planet⟩"** panel:

- a **pass-distance slider** (in planet radii, with the honest floor at 2 R — the same
  point-mass-lie line the labs draw) and a **side toggle** (lead/trail — brake or boost);
- OR "aim the exit": pick a target body / drag an exit arrow, and the solver finds the pass
  that yields it (lesson 19's TCM-1: Newton on the b-plane offset, solved with a small Vector
  burn at a node you choose earlier on the track);
- readouts before you commit: pass distance, Δv gained/shed, post-pass apoapsis/direction,
  pulse cost of the aiming burn, and the **lever warning** ("±1 pulse of aim error moves the
  far end by X Gm — re-trim after the pass").

Honesty rules: the solver runs the real integrator (no patched conics), it can FAIL ("no pass
this cheap bends you there — arrive faster or closer"), and results are shown as the labs would
print them. Solver work is cooperatively sliced (the #95 plan, option A) so the deck never
freezes.

### 2. The skim (aerobrake — the Stargate Universe move)

Needs the one new Core ingredient: an **atmosphere model** on bodies that plausibly have one
(gas giants, Venus, Earth, Titan): exponential density, `drag accel = −k·ρ(h)·|v|·v̂`, active
only inside a shell, integrated by the same simulator (deterministic, fixed-step near planets
as today). Two or three scalars per body in the scenario JSON — nothing exotic.

UI: on a closest-pass marker at an atmosphere-bearing body → **"🔥 Skim ⟨planet⟩"**:

- a **depth slider** (periapsis altitude inside the shell) with a live three-zone corridor
  gauge — *too shallow* (nothing happens), *the corridor* (Δv shed per pass, shown in pulses
  saved), *too deep* (hull damage: the sail-holed/disabled mechanic we already have — same
  consequence the gun inflicts, now self-inflicted);
- the projected ribbon re-draws through the drag pass so you SEE the orbit tighten.

Why players do it: braking without pulses when the tank is dry (the SGU scenario), and the
escape trick — a pursuer's dead-reckoned intercept assumes no drag, so a skim makes their
solution stale by construction. Surface that explicitly: "pursuit geometry broken — expect
re-acquisition in ~N d".

### 3. The skip (the Apollo bounce)

Free once the skim exists — it's the same drag model at entry-corridor speeds: come in too
shallow and fast and the atmosphere sheds a little speed and throws you back out (the bounce
Apollo crews had to respect), come in steep and you're in the damage zone. The corridor gauge
gains its third meaning at hyperbolic approach speeds, and the skip becomes a legitimate move:
bleed just enough at Earth/Venus to capture without a burn — or deliberately bounce to shed a
pursuer who committed to your entry track.

## Lab-first build order (the house pattern)

- **Lab 22 candidate — "The air brake":** implement the drag model in Core, then compute the
  corridor honestly: Δv shed vs. depth at Jupiter, the skip boundary at Earth-return speeds,
  the damage line, a fuel-out capture flown end-to-end. Real printed numbers; `--viz` shows the
  skim pass with the time-fade. The SAME Core code then powers the game node — the picture the
  lab draws IS the corridor gauge the game shows.
- **The sling needs no new physics** — only the solver-behind-UI packaging (plus #95's
  cooperative slicing so it's smooth).

## Suggested PR lanes (appends to TuesdayPlanPRs.md)

- **PR-G · The sling**: closest-pass "Sling past" panel, b-plane solver as a Core PlannerAssist
  (lifted from the lab-19 pattern, tested), Vector-burn emission into the existing plan,
  cooperative slicing. Cheat: `?sling=<body>` demo state.
- **PR-H · Lab 22 + the atmosphere model** (Core + lesson; parity/regression tests; scenario
  schema for atmosphere scalars).
- **PR-I · The skim & skip**: plot node + corridor gauge + damage wiring + pursuit-staleness
  message. Cheats for fuel-out and hot-entry states.

Order: G anytime (independent); H before I. All three compose with the quest arc — a masked
contract's ambush is a lot more fun when the escape plan is "thread Jupiter's cloud tops".

## Open questions for the owner

1. Damage currency for too-deep skims: hole the sail (existing disable mechanic), burn pulses
   as "emergency ablation", or a new hull meter? (Plan assumes: sail-holed, consistent with
   gun damage.)
2. Should hunters EVER learn to follow through a skim (a late-game "canny wolf"), or is
   atmosphere forever their blind spot (keeps the counter reliable)?
3. Sling solver placement: only at existing plan nodes, or may it propose its own node time?
4. Does the skim generate notoriety heat (dramatic, visible) or reduce it (you vanished)?
