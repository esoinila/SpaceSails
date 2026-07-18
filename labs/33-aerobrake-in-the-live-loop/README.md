# Lab 33 — Aerobrake in the LIVE loop: the campaign Lab 32 printed, now flown for real

[Lab 32](../32-aerocapture-at-the-ice-giant/README.md) proved the ice giant's haze is a priced brake — but
it flew in a **clean, planet-centred frame**: the body pinned at the origin, zero rail velocity, so the
two-body energy stayed pristine and the drag was isolated. That is the right way to *print* the physics. It
is not the way the game *flies* it. In the live sim **Uranus orbits the Sun at ~6.8 km/s**, its atmosphere
translates **with** it (the air is not at rest), and the integrator is **n-body** (the Sun tugs the whole
time). This lab asks the one question the pinned frame cannot answer:

> Does the aerobrake campaign still **converge** under the real integrator — moving atmosphere, live solar
> perturbation — and does the shipped Core quote (`Aerobrake.Price`, #290) reproduce Lab 32's headline
> **before the context-menu button ever renders**?

It is the R&D that stands behind #290's shipped affordance. It flies the **same** Core drag
(`Simulator.RunAdaptiveWithDrag` / `DragReport`) — no forked model — but in a **two-body-rail** ephemeris
(Sun pinned, Uranus on its real sol.json orbit), so `v_rel` now genuinely carries Uranus's heliocentric
motion. The #276 impact/integrator fixes are the ground it stands on.

Run it:

```bash
dotnet run --project labs/33-aerobrake-in-the-live-loop -c Release
```

## Section A — the shipped quote reproduces Lab 32's headline (the button prints first)

`Aerobrake.Price(uranus, 29.8 km/s, tank = 32 p)` — the exact call the context menu makes:

```
outcome            : HybridBridge  (corridor CLOSED)
arrival v_inf      : 29.8 km/s   periapsis entry 36.6 km/s
free shed (≤3g)    : 4.0 km/s at 2.94 g  (q 3.46 kPa)
capture Δv needed  : 15.3 km/s   bridge 11.3 km/s
propulsive bill    : 42 p   aerobrake bill 31 p   SAVED 11 p
passes             : 6 (5 free tightening after the capture)
menu label         : 🪂 Aerobrake at Uranus — the haze pays ≈11 p (bridge burn ≈31 p vs 42 propulsive)
```

The Core quote agrees with Lab 32 to the pulse: **42 propulsive (impossible on 32), 31 air-assisted (one
pulse inside the tank), ~11 saved, corridor closed at 29.8 km/s.** `Price` flies the same pinned kernel
Lab 32 owns, so this is the merged lab's number, now behind a shipping API.

## Section B — the LIVE hybrid capture: one hot pass + a bridge burn goes bound, for real

Placed on a 29.8 km/s hyperbolic approach to the **moving** Uranus and flown through the live n-body
integrator, with the captain braking retrograde at periapsis onto a wide bound orbit:

```
arrival rel energy : 4.440E+008 J/kg (>0, hyperbolic)
hot pass (no burn) : peak 2.96 g, shed 4075 m/s, still hyperbolic (needs the bridge)
capture Δv needed  : 15318 m/s to escape; quote's bridge 11272 m/s
bridge SPENT live  : 13.0 km/s (brake to the 40 R_U capture orbit)
post-burn          : rel energy -2.768E+007 J/kg, CAPTURED, apoapsis 7.3 R_U
```

The air-assisted bridge **captures under the live integrator, moving atmosphere and all** — the maneuver is
real, not a pinned-frame artifact.

### The honest live refinement (a note for the owner's quote calibration)

Watch the two bridge numbers: the **quote** prices the bridge as *capture-Δv − the FULL free shed*
(15.3 − 4.0 = 11.3 km/s, Lab 32's energy-accounting convention). The **live at-periapsis burn is a touch
larger — ~13 km/s** — for an honest reason the pinned energy accounting glossed: a burn fired *at periapsis*
only gets the credit of the shed that happened *on the way down* (~half the 4 km/s). The other half sheds on
the climb-out **after** the burn — where it isn't wasted, it just becomes the first slice of the free
tightening. So the quote is a **close, slightly optimistic estimate**; the live single-pass capture runs a
few pulses dearer, with the difference repaid as free tightening (Section C). In v1 this is advisory only —
the player flies the real numbers on the live skim gauge — but it is flagged for the owner as a
**quote-calibration** question alongside the Q3 risk-currency ruling.

## Section C — the LIVE free tightening campaign: apoapsis drops, pass after pass

Seeded on a just-captured wide orbit (gentle 300 km periapsis), each free pass flown continuously through the
live integrator — the drag bites only in the cloud tops, the vacuum arc coasts on the same call:

```
  pass   peak g   shed m/s   rel energy J/kg   apoapsis R_U
     1     0.19        726         -59018804           2.86
     2     0.09        395         -66482540           2.42
     3     0.04        205         -70406141           2.23
     4     0.02        104         -72495929           2.13
     5     0.01         52         -73658388           2.08
     6     0.01         27         -74354635           2.05
```

Apoapsis **4.00 → 2.05 R_U over 6 live passes, strictly monotone-decreasing**, each at a placid sub-0.2 g.
The free tail **converges under the real integrator** — the moving frame does nothing to spoil it. (The
apoapsis climbs down more gently than Lab 32's pinned C2 because this seed starts far tighter; the shape of
the curve — every lap strictly lower — is the invariant that matters.)

## Section D — determinism

```
two identical live passes: post states equal = True, peak g equal = True, shed equal = True
```

Fixed slice, no wall clock, no randomness: client WASM and any replay agree to the bit.

## Section E — the Galilean check: the moving atmosphere sheds exactly what the pinned frame did

```
pinned frame  : peak 2.960 g, shed 4074.5 m/s
moving frame  : peak 2.960 g, shed 4074.5 m/s
Δ shed = 0.00 m/s, Δ peak = 0.0000 g
```

Because drag depends only on speed **relative to the air**, the live moving-frame pass and Lab 32's
pinned-frame pass shed the *same* Δv at the *same* peak g. This is the numerical proof that **the pinned lab
was honest** — the frame it chose for clean energy bookkeeping did not distort the physics one bit.

## Break it yourself

```
1. Arrive at 6 km/s v_inf instead of 29.8: outcome SoloCapture (corridor OPEN — near-free), 6 passes, 4 p saved.
2. A 3 km/s v_inf: outcome SoloCapture — the haze just catches you.
```

1. **Speed is the gate, not aim.** Drop the arrival to a typical ~6 km/s and the corridor that was *closed*
   at 29.8 reopens to a near-free solo aerocapture — the affordance flips from a hybrid bridge to a pure
   air brake all on its own.

## For the AEROBRAKE affordance (the conclusion)

This lab clears the shipped #290 button on four counts:

1. **The quote is honest and reproduces the merged lab.** `Aerobrake.Price` gives Lab 32's 42/31/11 headline
   through a pure, deterministic, unit-tested API.
2. **The campaign converges live.** The free tightening tail strictly lowers apoapsis under the real n-body
   integrator with a moving atmosphere — the maneuver the button arms is real.
3. **One physics.** The Galilean check proves the pinned pricing frame and the live flying frame are the same
   drag — no forked model, no drift between what the gauge quotes and what the ship flies.
4. **A flagged refinement.** The live at-periapsis bridge runs a few pulses dearer than the energy-accounting
   quote; v1 ships the quote as advisory and flags the calibration for the owner.

---

*Every number in this README came from running the probe. If you change the code, rerun and re-paste —
never hand-edit a table.*
