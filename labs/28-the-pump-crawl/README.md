# Lesson 28 — The pump crawl

*Lesson 23 stopped the Titan-approach hemorrhage (#146: 167 pulses fighting Saturn, then the
autopilot starved). But the deeper wound of that night was not that the approach was expensive —
it was that the ship let itself get expensive-far from a fuel pump and only found out when the
tank read 8. A pump is not everywhere. Depots ride the rails at the **planets**, the **stations**
and the **havens** — but **never at an ordinary moon**. Titan is dry. Luna is dry. So "can I still
refuel?" is a real question with a real red line, and this lesson measures it honestly with the
game's own planner (`TransferPlanner`) and its own pulse kernel (`OrbitRule.PulsesFor`) — the same
code the flight software spends with, so the alarm and the bill can never drift apart.*

```bash
dotnet run --project labs/28-the-pump-crawl -c Release
```

## Why this lesson exists

The owner's #146 playtest ended with the ship on a wild Saturn ellipse, the tank at 8 pulses, and
the nearest refuel nowhere the captain could name. #157 asked the plain question — *"How do I fill
her up?"* — and #166 asked for the alarm: **amber below the 18% autopilot reserve, red when the
ship can't reach a pump.** All three need the same missing number: the honest pulse cost to reach
fuel from where you are. This lab produces it, sweeps it to find the red line per region, and hands
the game a small Core service (`FuelReachability`) that returns a verdict the banner strip and the
🦜 parrot can read.

## What the game prices that a fuel gauge doesn't

A fuel gauge shows what's in the tank. It cannot show *how far the tank has to stretch* — and in a
giant's well that distance is the whole story. Two facts make the crawl real:

- **Pumps are sparse.** `TrafficSchedule.GenerateDepots` puts a depot at every planet, every
  station and every haven, and at **no ordinary moon**. The two moons a captain actually parks at —
  Titan and Luna — are dry.
- **The reach is priced in pulses, not metres.** One pulse buys Δv equal to 1% of the ship's
  heliocentric speed (`OrbitRule.PulsesFor`). Deep in Saturn's well the ship rides fast, so a pulse
  there is a big pulse and the fare home in *pulses* can be small even when the *metres* are vast —
  the same Oberth economics lesson 23 taught, now read as a lifeline.

## The numerical experiment

### A — the pump map

The game's own depot list, printed. Read the two moons at the bottom: dry.

```
body                      kind      haven   has a pump?
--------------------------------------------------------
Mercury                   Planet    no      PUMP
Venus                     Planet    no      PUMP
Earth                     Planet    no      PUMP
Luna                      Moon      no      — dry —
Highport Satellite Works  Station   no      PUMP
Mars                      Planet    no      PUMP
Jupiter                   Planet    no      PUMP
Saturn                    Planet    no      PUMP
Titan                     Moon      no      — dry —
Enceladus                 Moon      yes     PUMP
Ringside Exchange         Station   yes     PUMP
Uranus                    Planet    no      PUMP
Neptune                   Planet    no      PUMP
```

### B — the reach

`TransferPlanner`'s own pulse estimate from each representative ship state to each in-well pump —
or its honest refusal. A "doorstep" is the free-flying, ready-to-leave state just outside a moon's
Hill sphere (the planner rightly refuses a departure from *inside* a moon's well).

```
ship state                          -> Enceladus   -> Ringside   -> Highport    cheapest
----------------------------------------------------------------------------------------
Enceladus doorstep (haven moon)          at pump            48             - 0 (at pump)
Titan doorstep (DRY moon)                     77            29             -29 (ringside)
Saturn mid-well cruise                        32            27             -27 (ringside)
Ringside dock (haven station)                  4       at pump             - 0 (at pump)
Luna doorstep (DRY moon)                       -             -       refused    STRANDED
```

Two findings jump out. From a parked-at-Titan doorstep the nearest pump (Ringside) is **29 pulses**
away and the reliable inner haven (Enceladus) is **77**. And **Luna is stranded**: its only in-well
depot host is Highport, a *low-Earth-orbit* station whose match speed (~7.6 km/s) exceeds the 5 km/s
capture cap, so the planner honestly refuses the plunge. Luna has no pump it can reach — a real gap
the game should close with a Luna-orbit depot (#157). The lab reports the gap rather than paper over
it.

### C — the red line per region

The **red line** is the cheapest-pump reach from the worst state a captain reaches in a region: dip
one pulse below it and no pump is reachable. Measured, and set beside the flat 18% autopilot reserve
(45 p on the base 250-pulse tank):

```
region                                    red line (p)          nearest pump  flat 18% covers?
----------------------------------------------------------------------------------------------
inner Saturn moons (parked at Titan)                29     ringside-exchange               yes
Saturn well cruise                                  27     ringside-exchange               yes
Luna neighborhood (parked at Luna)          2147483647                (none)               n/a
```

The margin is monotone in the tank — cross the red line and the verdict flips exactly once:

```
(cheapest reach to ringside-exchange = 29 pulses)
remaining pulses        margin     can reach a pump?
----------------------------------------------------
69                          40                   yes
45                          16                   yes
39                          10                   yes
30                           1                   yes
29                           0                   yes
28                          -1         NO — STRANDED
10                         -19         NO — STRANDED
9                          -20         NO — STRANDED
```

The **Saturn-well red lines are 27–29 pulses** to the nearest pump; **Luna's is infinite** (no
reachable pump). The nearest-pump reach clears the flat 45-p reserve for the Saturn moons — but see
Section D for why that "yes" is a trap.

### D — the #146 reserve

#146 asks: *what reserve should the ship refuse to dip below during a moon approach deep in a giant's
well?* Not a flat fraction of the tank — the **fare back to a pump from where the approach leaves
you**. Two pumps serve the Saturn well: Ringside (a ring station, cheap only when its phase is
favourable) and Enceladus (the inner haven, always there on the inner lane). Prudence prices the
reach to the pump you can always count on:

```
reach parked-at-Titan -> Ringside (phase-dependent) : 29 pulses
reach parked-at-Titan -> Enceladus (always-there)   : 77 pulses
flat 18% autopilot reserve on the base tank         : 45 pulses
RECOMMENDED well-aware reserve (nearest + cushion)  : 74 pulses
```

The reliable-haven reach (**77 p**) already **exceeds the flat 45-p reserve by 32 p**. A ship that
held only the flat floor and then found Ringside out of phase would be a burn short of the only pump
it could count on — exactly the #146 starve. **The recommended reserve rides the reach:
`nearest-pump reach + the normal 18% cushion` (74 p from parked-at-Titan), never a fixed fraction the
well outgrows.** That is the number the armed autopilot should quote and hold at arm time.

### E — the Core service, agreeing with the tables

`FuelReachability.Assess(state, remaining, tank, well)` returns the nearest-pump cost, the margin,
and a verdict — run here on genuinely-parked states (the service lifts a parked-at-moon state to the
moon's doorstep to price the crawl):

```
ship state                          remaining   nearest   margin               verdict
--------------------------------------------------------------------------------------
Parked at Enceladus (at pump)              30         0       30           Comfortable
Docked at Ringside (at pump)               30         0       30           Comfortable
Parked at Titan, half tank                125        29       96           Comfortable
Parked at Titan, thin                      60        29       31                  Thin
Parked at Titan, stranded                  20        29       -9      CannotReachAPump
Saturn cruise, half tank                  125        27       98           Comfortable
Parked at Luna (dry, no pump)             125      none        -      CannotReachAPump
```

The three verdicts are the #166 fuel alarm: **Comfortable** clears the well-aware reserve (or the
ship is alongside a pump); **Thin** is the amber squawk (a pump is still reachable but the tank is
below `reach + 18%` — 74 p at Titan); **CannotReachAPump** is the red squawk (the fare exceeds the
tank, or no pump is reachable at all — Titan at 20 p, and Luna always).

## What ships in the game

`FuelReachability` (Core), a pure function of sim state:

```csharp
FuelReachability.Assess(sim, ephemeris, ship, remainingPulses, tankCapacity, wellBodyId)
  -> Assessment(Verdict, NearestDepotBodyId, NearestDepotPulses, RemainingPulses,
                MarginPulses, SafeReservePulses, Reachable, Reason)
```

- **Verdict** ∈ { Comfortable, Thin, CannotReachAPump } — the #166 alarm's three states.
- **SafeReservePulses** = `nearest reach + flat 18% cushion` — the well-aware amber floor. Collapses
  to the plain 18% of #166 when the ship is alongside a pump (reach 0); rises to include the fare
  home out in the well.
- Pumps are the depot-bearing children of the well (stations + haven moons — the same rule
  `GenerateDepots` spawns them by); each is priced by `TransferPlanner`, a pump you're alongside costs
  0, and a parked-at-moon state is priced from that moon's doorstep.

The `#166 ShipAlerts` lane wires the red line into the banner + parrot squawk (see the PR body).

## Break it on purpose

1. **Strand yourself on Luna.** The lab reports Luna's red line as infinite. Add a Luna-orbit haven
   to `scenarios/sol.json` and rerun — watch a pump appear and the red line drop to a finite reach.
   That is #157's fix, measured before it ships.
2. **Wait out a Ringside window.** The 29-p Ringside reach is phase-favourable. Nudge the departure
   epoch and watch it climb toward Enceladus's phase-robust 77 — why the reserve prices the haven,
   not the cheapest pump of the moment.
3. **Fill the tank and cross the well.** Sweep `remainingPulses` past 74 at parked-at-Titan and watch
   the verdict step Comfortable → Thin → CannotReachAPump exactly at the measured lines.

## The framing rule, kept

Standard physics presented as standard: the reach is `TransferPlanner`'s Lambert/phasing solve, the
price is `OrbitRule.PulsesFor`, the pumps are the scenario's own depots. The fictional part of
SpaceSails is WHERE the pumps are, never HOW the fare is computed.
