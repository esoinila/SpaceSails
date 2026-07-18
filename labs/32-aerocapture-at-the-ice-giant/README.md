# Lab 32 — Aerocapture: the ice giant's haze is the free brake

The owner arrived at Uranus stranded: **29.8 km/s** relative to the planet, **32 pulses** left in the
tank, and a destination that needs stopping. He asked the only question a sailor asks when the fuel
gauge and the arrival disagree — *"could we use Uranus's own air as a free brake?"* This is a **lab, not
a button**: per the house R&D method, the probe prints the numbers before any feature ships. It adds no
new physics — it reuses the exact Core drag [lab 22](../22-the-air-brake/README.md) shipped (an
exponential atmosphere shell + one ballistic-coefficient knob, `Simulator.RunAdaptiveWithDrag` /
`DragReport`) — and answers the four questions aerocapture at an ice giant actually turns on:

- **the corridor vs entry speed** — at what periapsis depths does a pass skip out, capture into a bound
  orbit, or blow a heat/g budget? and how does that corridor's *width* change with arrival speed?
- **Δv shed per pass** — is one pass enough at ~36 km/s, or a multi-pass campaign, each apoapsis a
  bail-or-deepen decision?
- **the bill in game units** — g against the hull line, dynamic pressure as the heat proxy, and Δv shed
  as **pulses** the existing kernel already prices;
- **which worlds play** — Uranus, Neptune, Saturn, Jupiter, Titan, Venus, Mars, Earth.

Run it:

```bash
dotnet run --project labs/32-aerocapture-at-the-ice-giant -c Release
```

## The standard textbook take

Curtis, *Orbital Mechanics for Engineering Students*, treats aerocapture with the same exponential
(isothermal) atmosphere this lab uses — density `ρ(h) = ρ₀·exp(−h/H)` — and the drag deceleration
`a = ½·ρ·v²/BC`, where the **ballistic coefficient** `BC = m/(C_d·A)` bundles the whole ship into one
number (kg/m²). Aerocapture is the one-shot cousin of aerobraking: a hyperbolic arrival makes **one**
deep pass through the upper atmosphere, sheds enough speed to fall below escape, and comes out bound —
ideally with a small periapsis-raise burn at the first apoapsis so it never dips again. The textbook's
warning is exactly the one that bites here: because drag is exponential in depth, the *entry corridor*
is narrow, and it **narrows further the faster you arrive** — a hot arrival keeps more excess energy, so
it must dip deeper (toward the heat/g wall) to shed the same fraction. Past a critical speed the corridor
**closes**: there is no depth that both captures and survives. Curtis sets up the conic and hands you to
an integrator. So: numerically, through the game's own.

**What we model (lab 22's ingredient, unchanged):**

- `density(h) = refDensity · exp(−h / scaleHeight)`, **exactly zero** at/above a hard shell top;
- `drag accel = −0.5 · ρ · |v_rel| · v_rel / BC`, with **BC = 120 kg/m²** (the game's one knob);
- `v_rel` = ship velocity − the body's rail velocity (the air translates with the planet).

**What we ignore on purpose:** spin, lift, and *all* heating physics. "Too deep" is charged off **peak
deceleration** — the sail-holing hull damage the gun already inflicts, now self-inflicted — against the
Core constant `Atmosphere.SailHoleDecelG = 3 g`. This is a game-tuned model, and it says so.

### The atmosphere shells (Section A)

Jupiter, Saturn, Titan, Venus and Earth **mirror `scenarios/sol.json` exactly**. Uranus, Neptune and
Mars carry **no atmosphere in the game yet** — the shells below are **this lab's proposal**, game-tuned
in lab 22's spirit: thin, and with a shell top well under **0.15 body radii** so no existing gameplay
trajectory (orbit insertion parks at ~0.5 Hill radii, tens of thousands of km out) ever clips one. The
ice giants get a puffier upper shell than the gas giants on purpose — their arrivals are the hypersonic
30 km/s leftovers of a botched transfer, and a Jupiter-thin 4×10⁻⁶ shell would force impractically deep,
lethal dips. This is the R&D that would justify shipping these shells to `sol.json`.

```
body      refDensity kg/m^3 scaleHeight km shell top km  top / R  v_esc km/s      source
Jupiter               4E-06           30.0          400   0.0057        60.2    sol.json
Saturn                5E-06          120.0          700   0.0120        36.1    sol.json
Uranus              1.4E-05          120.0         1000   0.0394        21.4    PROPOSAL
Neptune             1.8E-05          100.0          900   0.0366        23.6    PROPOSAL
Titan                   5.3           40.0          300   0.1165         2.6    sol.json
Venus                    65           16.0          150   0.0248        10.4    sol.json
Earth                   1.2            8.0          140   0.0220        11.2    sol.json
Mars                   0.02           11.0          120   0.0354         5.0    PROPOSAL
```

## The corridor at Uranus (Section B)

The stranding: `v_inf = 29.8 km/s`, so the periapsis entry speed is
`√(v_inf² + v_esc²) = √(29.8² + 21.4²) ≈ 36.7 km/s`. Sweep periapsis depth on that fixed arrival:

```
  peri alt km  min alt km  dv shed m/s   peak g    q kPa                           outcome
           40        35.5         7077     4.90     5.76  OVER-G: hull holed, still leaves
           80        76.3         5149     3.68     4.33  OVER-G: hull holed, still leaves
          120       116.9         3732     2.72     3.21        SKIPS OUT, v_inf 25.1 km/s
          160       157.3         2696     2.00     2.35        SKIPS OUT, v_inf 26.4 km/s
          220       217.7         1649     1.24     1.46        SKIPS OUT, v_inf 27.8 km/s
          300       298.0          852     0.65     0.76        SKIPS OUT, v_inf 28.8 km/s
          400       398.3          372     0.29     0.34        SKIPS OUT, v_inf 29.3 km/s
```

Read it top to bottom: every survivable depth (≤ 3 g) still **skips out** — it sheds 0.4–3.7 km/s but
keeps enough energy to fly back to infinity. The only depths that shed the ~15 km/s needed to capture are
already **over the g line, and still leave anyway**. At 29.8 km/s there is simply **no single pass that
both captures and survives** — the corridor is *closed*. Watching it close is the whole lesson:

```
Corridor width vs entry speed (safe single-pass = captures AND peak g <= 3):
  v_inf km/s  entry km/s   capture band km   safe band km   width km         verdict
         4.0        21.7            60-360         60-360        300   corridor OPEN
         8.0        22.8            30-200         30-200        170   corridor OPEN
        12.0        24.5            20-110         20-110         90   corridor OPEN
        16.0        26.7             20-50          30-50         20   corridor OPEN
        20.0        29.3            (none)         (none)          0          CLOSED
        24.0        32.1            (none)         (none)          0          CLOSED
        29.8        36.7            (none)         (none)          0          CLOSED
```

The corridor is **300 km wide at a gentle 4 km/s arrival** and narrows monotonically to **~20 km at 16
km/s**, then **closes above ~16 km/s** — exactly Curtis's hot-corridor warning, computed. The owner's
29.8 km/s is nearly **double** the critical speed. So the honest first answer to *"can I aerocapture?"*
is **no, not on air alone** — not at the speed he arrived.

## One pass, or a campaign? (Section C)

If one survivable pass can't capture, what can? Two honest facts, priced.

**C1 — the free pass and the propellant bridge.** The deepest pass that stays at/under 3 g sheds a real
chunk of speed for free; the rest is a bridge you must buy:

```
C1. Deepest FREE pass at/under 3 g: periapsis 110 km (2.94 g), sheds 4046 m/s.
    To capture, the 36.6 km/s periapsis speed must drop below local escape 21.3 km/s
    = shed 15.3 km/s. One free pass pays 4.0 km/s; the bridge is 11.3 km/s.
```

The air pays **4.0 of the 15.3 km/s** at the 3 g limit; **11.3 km/s** remains. And there is a hard
geometric catch: a hyperbolic arrival gives you **one** periapsis pass — shed too little and you leave
and never return. So a pure multi-pass air campaign **cannot even begin** here (the first pass must get
you bound, and no survivable first pass does). You bridge the gap with a periapsis burn *on that same
pass*, or you accept a hull-holing over-g dip.

**C2 — the free tightening campaign, once bound.** After the assisted capture drops you onto a wide bound
orbit, *now* every subsequent pass is free money. Seeded at a just-captured 60 R_U orbit with a gentle
300 km periapsis, flown pass by pass (each next skim reconstructed from the exit orbit's energy and
angular momentum *exactly*, lab 22's symmetry trick, so the long coast never corrupts the ledger):

```
  pass  peri alt km  dv shed m/s   peak g    energy J/kg      apoapsis
     1        294.3          665     0.22      -17452150      12.1 R_U
     2        281.7          742     0.23      -32261770       6.1 R_U
     3        266.6          857     0.24      -48685683       3.7 R_U
     4        246.6         1054     0.26      -67924178       2.4 R_U
     5        213.7         1579     0.29      -94714288       1.4 R_U
```

Five free passes drop the apoapsis **60 → 12 → 6 → 3.7 → 2.4 → 1.4 R_U** and shed **4.9 km/s for zero
fuel**, each at a placid ~0.25 g. Every apoapsis row is a **bail-or-deepen decision**: stop here and
circularize, or dip again. Notice the periapsis **creeping down** (294 → 214 km) — with no fuel to
re-raise it, free tightening digs deeper every lap, so it's a race you eventually lose to the hull line.
That is the mechanic a future AEROBRAKE step would expose: the cheap tail of the capture is free; the
expensive *first* bite, at a hot arrival, is not.

## The bill in game units (Section D)

The game has no thermal model, so "heat" is charged as **hull load off peak-g** against the 3 g line
(lab 22's currency); peak **dynamic pressure** (the `q kPa` column) is the physical heat proxy a future
thermal system would read from the same `DragReport`. Δv is priced in **pulses** with the very kernel the
autopilot spends with, `OrbitRule.PulsesFor` (one pulse buys 1% of world speed as Δv):

```
Owner's tank: 32 pulses. Uranus heliocentric orbital speed 6.8 km/s (the world-speed floor).
Pure-propulsive capture: shed 15.3 km/s at ~36.6 km/s world speed = 42 pulses  => IMPOSSIBLE (over 32).
Aerocapture-assisted: the 3 g pass sheds 4.0 km/s FREE; you buy only the 11.3 km/s bridge = 31 pulses.
  => the haze pays 11 pulses of the capture bill and drops peak-g from a solo capture's lethal load to 3 g.
  => 31 pulses is INSIDE the owner's 32 — the air is what makes the stranding survivable.
Desperate solo capture (no fuel, accept the hull): NONE exists — at 29.8 km/s every single pass either
skips out (shallow) or augers in (deep) before it can shed enough. The bridge burn is mandatory.
```

There is the headline the owner wanted, in his own units: **a pure-propellant capture costs 42 pulses —
he has 32, so it is flatly impossible.** The haze pays **11 pulses** of that bill and brings the burn down
to **31** — *one pulse inside the tank*. The air isn't a nice-to-have here; it is the entire difference
between stranded and home. And there is no free lunch to be had by skipping the burn: at 29.8 km/s **no**
single pass captures at all, hull-be-damned — you skip out shallow or auger in deep. The bridge is
mandatory; the air just makes it affordable.

## Which worlds play (Section E)

Same 3 g corridor test, each world flown at a *typical* (non-stranding) arrival speed. `grab g` is the
peak g of the shallowest pass that goes bound — the single number that sorts the whole solar system:

```
world       H km  v_esc km/s  v_inf km/s  entry km/s    grab g   safe corridor                 verdict
Jupiter       30        60.2         5.5        60.5       0.3        30-95 km          PLAYS (gentle)
Saturn       120        36.1         5.5        36.5       0.2       16-340 km          PLAYS (gentle)
Uranus       120        21.4         6.0        22.2       0.3       52-265 km          PLAYS (gentle)
Neptune      100        23.6         6.0        24.3       0.3       58-260 km          PLAYS (gentle)
Titan         40         2.6         2.0         3.3      12.8          (none)aero-LANDS (air too thick)
Venus         16        10.4         5.0        11.5     310.3          (none)aero-LANDS (air too thick)
Earth          8        11.2         3.0        11.6       0.7        82-94 km          PLAYS (gentle)
Mars          11         5.0         3.0         5.9       0.8        56-64 km          PLAYS (gentle)
```

Three families fall out:

- **The giants, Earth, Mars — thin high shells — PLAY** at their typical speeds: a gentle sub-g skim
  corridor tens to hundreds of km wide. The ice giants play here **too** — the stranding is closed only
  because 29.8 km/s is a botched-transfer freak, not a normal Uranus arrival (~6 km/s plays fine).
- **Titan and Venus are the anti-training-wheels.** Their shells are so thick (5.3 and 65 kg/m³) that
  *any* entering pass sheds far more than escape — you **cannot skip out** (forgiving!) but you cannot
  capture **gently** either: the grab is **12.8 g at Titan, 310 g at Venus**. You don't aerocapture to
  orbit; you make a full atmospheric **entry** — a landing. Titan is still the "deep" teaching case the
  issue framed it as: a *guaranteed* grab, the exact opposite failure mode to the ice giant's skip-away.
  (This is the first time anything has flown the sol.json Titan/Venus shells for capture — lab 22 only
  ever flew Jupiter and Earth — and it reveals they are landing atmospheres, not skim atmospheres.)

## Break it yourself

```
1. Halve the speed to 14 km/s v_inf: the safe corridor is 20-80 km, 60 km wide.
   Slower arrivals need less shed, so the capture band clears the g line — speed, not depth, is the gate.
2. Neptune, 14 km/s v_inf, 120 km dip: sheds 3223 m/s at 1.64 g, skips. Its higher v_esc (23.6 km/s) makes every arrival hotter.
3. Mars, 3 km/s v_inf: a 60 km pass sheds 1455 m/s (captures); a 10 km pass AUGERS IN.
   The thin shell is the anti-Titan: too little air to capture high, too little room to capture deep.
```

1. **Halve the speed.** The corridor that was *closed* at 29.8 km/s reopens to a comfortable 60 km at 14
   km/s — the gate is arrival *speed*, not the depth you aim. Aim doesn't save a hot arrival; a slower
   approach does.
2. **Neptune runs hotter.** Same dip, same speed, but Neptune's higher escape speed makes every arrival
   hotter than Uranus's — the ice giants are not interchangeable.
3. **Mars is the anti-Titan.** A thin shell can't brake a fast arrival high, and there's no room to brake
   it deep before the ground stops you — too little air, the mirror image of Titan's too much.

## For the AEROBRAKE step (the conclusion)

This lab is the R&D behind a future **AEROBRAKE step variant** for the insertions-as-steps lane
(**#262 / PR-D1**). The numbers say what that step must model and expose to the captain:

1. **A corridor gauge keyed to arrival speed, not just depth.** The step must show the pilot that the
   safe band *closes* above a critical speed (here ~16 km/s at Uranus) — the "you arrived too hot for
   this to work on air alone" verdict has to be a first-class outcome, not a silent failure.
2. **A hybrid capture, priced honestly.** The cheap tail (free multi-pass tightening) and the expensive
   head (the propellant bridge on the one hyperbolic pass) are different beasts. The step should quote
   both — *"the haze pays N pulses, you pay the bridge"* — using `OrbitRule.PulsesFor`, so an aerobrake
   arrival slots into the same pulse ledger as every other insertion step.
3. **Ice-giant + Mars atmosphere shells for `sol.json`.** The proposed Uranus / Neptune / Mars shells
   here are the concrete data that addition needs; Titan and Venus already carry shells, and this lab is
   the note that they are **landing** atmospheres, not skim atmospheres — an AEROBRAKE step should refuse
   (or reframe as a landing) at those.

The owner's question has a clean answer: **yes, the ice giant's haze is a free brake — worth ~11 pulses
and the difference between a 42-pulse impossibility and a 31-pulse ride home — but only as an *assist* to
a bridge burn at 29.8 km/s, and only a solo brake at all below ~16 km/s.**

---

*Every number in this README came from running the probe. If you change the code, rerun and re-paste —
never hand-edit a table.*
