# Lab 43 — The sharpest point

> *"the charge is being equaluzed could have plasma ball like beautifull effect if physics supports it"*
> — the owner, asking for something beautiful and attaching a condition to it

**If physics supports it.** So before anybody animated anything, this lab asked the physics five questions. It
came back with a prettier effect than the one I was going to draw, a correction to a fiction the game has been
telling since M7, and a trade nobody had to invent.

Run it: `dotnet run --project labs/43-the-sharpest-point/Lab43.csproj -c Release`

**Every number below came from that run.** Bands and thresholds come from `HullCharge`, so the lab and the
board cannot drift apart.

---

## A — where a discharge starts

Field strength is potential over radius of curvature, so a discharge leaves from whatever is *sharpest*.

```
  at 20 kV on the hull:
  feature                     radius       surface field      vs the hull
  the hull herself                 10 m       0.002 MV/m   ×       1
  a hull plate edge              0.05 m       0.400 MV/m   ×     200
  a handrail                     0.02 m       1.000 MV/m   ×     500
  the mast tip                  0.005 m       4.000 MV/m   ×    2000
  an antenna whip              0.0005 m      40.000 MV/m   ×   20000

  field-emission onset at a metal point ≈ 1000 MV/m
  → the whip is 4.0% of the way there while the hull is at 0.000%
```

**FINDING 1 — it is a plume off her antenna, never a ball around her.** The whip runs **20,000×** the field of
the hull skin. Drawing a sphere of light around the ship would be drawing the one place the discharge *cannot*
start. Draw it at the sharpest extremity and it is both truer and better looking — which is the happy case where
physics hands you the better picture for free.

## B — what one discharge is worth

```
  hull capacitance, R = 10 m: 1.11 nF

  band       charge   potential      stored       comparison
  QUIET       0.18      3.6 kV     0.007 J   less than a dropped coin
  RISING      0.20      4.0 kV     0.009 J   less than a dropped coin
  GLOWING     0.55     11.0 kV     0.067 J   a static shock off a door handle
  ARCING      0.90     18.0 kV     0.180 J   a static shock off a door handle
  ARCING      1.00     20.0 kV     0.223 J   a static shock off a door handle
```

**FINDING 2 — it is delicate.** A fully wound hull holds **0.22 J**: a static shock off a door handle, about a
fiftieth of a camera flash. So the effect must read as a *filament and a snap*, and the dump must never get an
explosion cue. Even ARCING — the band with the thunder on it — is energetically a door handle. The drama is
real; the joules are not.

## C — how long it lasts

```
  charge on a fully wound hull: 22.3 µC

  emitter                       current      time to shed it
  a leaky dielectric path         0.001 mA       22.3 s
  a passive grounding strap         0.1 mA      222.5 ms
  a hollow-cathode contactor         10 mA        2.2 ms
  a hard arc to space              1000 mA       22.3 µs
```

**FINDING 3 — the honest event is milliseconds, so the long version has to be sold differently.** Through the
contactor she sheds it in **2.2 ms**; a hard arc takes **22 µs**. The game's plate sits for 7 s, which is a
stylisation by a factor of ~3,000. The fix is not to slow the spark down — it is to show **one bright frame and
an afterglow** for the dump, and to sell any *sustained* light as **the cathode running**, which genuinely is
continuous. A slow-motion arc would be the kind of lie that makes everything next to it suspect.

## D — can anybody see her glow? No.

```
  discharge, as visible light (1% of 0.22 J over 2.2 ms): 1.0 W
  her hull reflecting sunlight at 1 AU:                        86 kW
  → the flash is 85514 × DIMMER than simply being lit by the sun
```

**FINDING 4 — and this one corrects the game.** `SensorModel.ChargeGlowFactor` has said since M7 that a charged
hull is *seen* up to 3× farther. Optically that is impossible: her discharge is **85,514× dimmer than her own
reflected sunlight**, and a ship that is merely sitting in the sun outshines her own lightning by five orders of
magnitude.

But the *number* is fine — it was the metaphor that was wrong. A charged hull in a plasma is a broadband **radio**
source; arcs are impulsive interference; the sheath around her is a thing an instrument can find. So the factor
stays exactly as it is and the fiction changes:

> **She is not brighter. She is LOUDER.**

`HullCharge.VisibilityLine` now says *audible*, not *visible* — *"a wound hull is a broadband radio source, and
everything with a receiver gets that for free without pointing it at you."* Running dark was never about photons.

## E — what the automatic is up against

```
  environment                  density      temp     arriving current
  cold outer dark               1.0e+4 /m³     10 eV      0.001 mA
  middling space                1.0e+5 /m³    100 eV      0.034 mA
  inner-system halo             1.0e+6 /m³   1000 eV      1.065 mA
  inside a plasma stream        1.0e+7 /m³   5000 eV     23.819 mA

  a hollow-cathode contactor emits ~10 mA
```

**FINDING 5 — the contactor loses inside a stream, and that is a whole mechanic.** It out-argues the cold dark
(×10,000), middling space (×300) and the inner halo (×10) without noticing. In a stream **23.8 mA arrives against
~10 mA emitted** and the cathode simply cannot win.

Which hands the design a trade that nobody sat down and invented:

| | the stream |
|---|---|
| what it gives | the free push — `PlasmaEnvironment.StreamAcceleration`, months faster than a ballistic transfer |
| what it costs | the automatic can only take the edge off; she rides high and loud the whole way |

**You may go fast or you may go unheard.** That is now in the code (`HullCharge.ContactorHoldTarget`,
`ContactorWinsHere`), and the board says so out loud when the cathode is running flat out and losing anyway.

---

## What changed because of this lab

1. **The renderer effect is a plume at the mast, not a ball** — and it should be one bright frame plus afterglow.
   (Follow-up: the canvas animation itself, #528 item 7.)
2. **No explosion cue on the dump.** 0.22 J does not get a bang.
3. **`ChargeGlowFactor` is radio, not light.** Number kept, fiction corrected, line rewritten.
4. **The stream is where the automatic loses** — implemented, and the best kind of mechanic: derived rather than
   designed.
