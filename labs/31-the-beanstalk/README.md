# Lesson 31 — The beanstalk

*Everybody "knows" the space elevator is science fiction, and everybody is quoting **Earth**. Earth
is the hardest place in the inner system to build one, by a margin so wide that the honest answer is
not "not yet" but "not with matter". The owner asked for the other question on a Friday morning —
"a space elevator **somewhere** where materials stress would not be an issue" — and it has a real
answer, several of them, and they are already places in this game. This lesson is one exponential,
evaluated honestly over six bodies, printing a verdict per body that it is never allowed to assert.*

```bash
dotnet run --project labs/31-the-beanstalk -c Release
```

## Why this lesson exists

Lab 30 measured Luna's other launch industry — the mass driver, one impulse and then the pod is a
stone. This is its rival: the cable that never lets go. It matters here because the elevator is the
one piece of megastructure the game can afford to take *literally*. Its two ends are ports, its
middle is a ride, and whether it exists at a body is not a matter of taste — it is
`exp(ΔΦ / (σ/ρ))`, and you can look it up in an afternoon.

The finding is that the difficulty and the *reward* are the same number, pointing opposite ways.
Every body where a beanstalk would transform your logistics is a body where the cable cannot be
built; every body where the cable is trivial is a body barely worth lifting off. The interesting
places are the two in the middle, and one of them is the Moon.

## The standard-textbook take

A cable of **uniform stress** — the same σ everywhere, which is what you build if you are not
wasting mass — must taper as you climb, because each cross-section carries everything below it.
Balancing the tension gradient against the effective gravity in the frame the cable co-rotates with
gives the classical relation (Artsutanov 1960; Isaacs *et al.* 1966; **Pearson 1975**):

```
A(r) / A(anchor) = exp( ρ · [Φ(r_max) − Φ(r)] / σ )
```

so the whole cable's **taper ratio** — widest cross-section over narrowest — is

```
taper = exp( ΔΦ / (σ/ρ) )
```

where **ΔΦ** is the effective-potential climb from the surface anchor to the point of maximum
tension, and **σ/ρ** is the material's **specific strength** in J/kg. That is the entire lesson.
Everything else is knowing which Φ to write down.

- For a body that **spins** fast enough to have a synchronous orbit above its surface, Φ is the
  co-rotating potential `−μ/r − ½ω²r²` and the tension peaks at the synchronous radius, where the
  effective gravity is zero. (Curtis ch. 2 for the two-body pieces.)
- For a body that is **tidally locked** — Luna, Phobos, Deimos — its day *is* its year, so its own
  synchronous radius falls outside its Hill sphere and there is no cable to stand up on its own
  rotation. The cable is hung from the **primary** instead, through the interior Lagrange point,
  where the tension peaks. Φ is then the rotating-frame potential of the circular restricted
  three-body problem (Curtis §2.12), and L1 is found by root-finding on its gradient.

## What the game adds that the textbook doesn't

Two things, and both are about honesty rather than physics.

**The constants are the game's own.** Earth, Mars, Luna and Phobos take μ and radius verbatim from
`scenarios/sol.json` — the same numbers the ship flies with — so any conclusion here transfers 1:1
into the live game. Ceres and Deimos are *not* in the game, so they are cited to JPL instead, and
the probe prints which is which in its own source column. A lab that mixes the two without saying so
is quoting itself.

**The verdicts are computed, not written.** Section D's five grades come out of `Beanstalk.Verdict`
applied to each body's own taper column, and that file is compiled twice: once into this probe and
once, **linked**, into `tests/SpaceSails.Core.Tests/Lab31BeanstalkTests.cs`. The guard holds every
printed verdict to a property of the numbers above it — using the *test file's* own literal
thresholds, because importing the lab's constants would move the guard along with the claim — and
then reads this README back and finds each verdict line **verbatim**. The lesson cannot drift from
its own arithmetic without going red.

## The numerical experiment

### A — the bodies, and whose constants they are

```
body     anchor             mu m3/s2   radius km   day (h)   g m/s2  source
------------------------------------------------------------------------------------------------
Earth    synchronous       3.986e+14      6371.0     23.93    9.820  sol.json + sidereal day
Mars     synchronous       4.283e+13      3389.5     24.62    3.728  sol.json + sidereal day
Luna     through L1        4.905e+12      1737.4    654.83    1.625  sol.json
Ceres    synchronous       6.263e+10       469.7      9.07    0.284  JPL SBDB
Phobos   through L1        7.100e+05        11.0      7.66    0.006  sol.json
Deimos   through L1        9.615e+04         6.2     30.31    0.003  JPL Mars fact sheet

  Luna    Hill radius   61532.73 km   L1 at   58026.77 km   = 33.40 body radii,  56289.37 km of clear air
  Phobos  Hill radius      16.58 km   L1 at      16.57 km   =  1.51 body radii,      5.57 km of clear air
  Deimos  Hill radius      21.30 km   L1 at      21.30 km   =  3.43 body radii,     15.10 km of clear air
```

The Earth–Moon L1 lands at **58,027 km** from the Moon's centre — the published figure, found by
bisection rather than by the cube-root Hill approximation, which matters enormously at the bottom of
the table: **Phobos's entire Hill sphere is 16.58 km, and its surface is at 11.** The cable has
**5.57 km** of clear air to work with. That is not a limitation to design around; it is the finding.

### B — the climb: how far up the potential the cable's waist sits

```
body       tension peak   altitude km     dPhi J/kg   char. length km
---------------------------------------------------------------------
Earth       synchronous      35793.17    4.8492E+07           4938.00
Mars        synchronous      17038.18    9.5196E+06           2553.63
Luna                 L1      56289.37    2.6988E+06           1660.91
Ceres       synchronous        722.12         58595            206.41
Phobos               L1          5.57        9.7138              1.66
Deimos               L1         15.10        8.9274              3.57
```

Geostationary at **35,793 km** and areostationary at **17,038 km** are the textbook altitudes;
Earth's **48.49 MJ/kg** and its **4,938 km** characteristic length are Pearson's numbers, arrived at
here from the game's own μ. Then read the last two rows again. Lifting one kilogram the **whole
height** of a Phobos beanstalk, ground to tension peak, costs about **ten joules**. A dropped
teaspoon on Earth does more work than that.

### C — the taper table

```
material                   sigma GPa    rho   MJ/kg  tier
-----------------------------------------------------------------
steel wire                       2.0   7900    0.25  Commercial
Kevlar 49                        3.6   1440    2.50  Commercial
Zylon PBO                        5.8   1560    3.72  Commercial
CNT fibre, best spun             6.0   1300    4.62  LaboratoryRecord
CNT single tube, theory        130.0   1300  100.00  Theoretical

body        steel wire    Kevlar 49    Zylon PBO    CNT fibre   CNT theory
--------------------------------------------------------------------------
Earth          1.5e+83      2.7e+08       461756        36559         1.62
Mars           2.1e+16        45.05        12.94         7.87         1.10
Luna             42632         2.94         2.07         1.79         1.03
Ceres             1.26         1.02         1.02         1.01         1.00
Phobos            1.00         1.00         1.00         1.00         1.00
Deimos            1.00         1.00         1.00         1.00         1.00
```

**Thresholds, stated before the data** (lab 46's rule — a threshold agreed after you have seen the
numbers is a description, not a threshold): a cable is **practical at taper ≤ 10**, and the material
has **stopped mattering at all at taper ≤ 1.1**. "Real" means somebody has made some; the theory
column is on the table precisely so that *"nothing real does it"* can be said with a number behind it.

Read the Earth row. A steel-wire Earth elevator wants a taper of **10⁸³** — there are about 10⁵⁰
atoms in the planet. Kevlar gets it to 10⁸, Zylon to 460,000, and the best carbon-nanotube fibre
anyone has ever spun to **36,559**. Only the *single-tube theoretical ceiling* — 130 GPa on a
perfect tube, a number describing a molecule and not a cable — brings Earth in at 1.62.

### D — the verdicts, computed from the table above

```
  Earth: BEYOND ANY REAL MATERIAL — best real material (CNT fibre, best spun) tapers 36559; taper 10 needs 21.1 MJ/kg and the best ever made is 4.62.
  Mars: ONLY WITH THE BEST FIBRE EVER SPUN — best real material (CNT fibre, best spun) tapers 7.87; taper 10 needs 4.13 MJ/kg and the best ever made is 4.62.
  Luna: BUILDABLE WITH FIBRE YOU CAN BUY TODAY — best real material (CNT fibre, best spun) tapers 1.79; taper 10 needs 1.17 MJ/kg and the best ever made is 4.62.
  Ceres: BUILDABLE WITH STEEL WIRE — best real material (CNT fibre, best spun) tapers 1.01; taper 10 needs 0.0254 MJ/kg and the best ever made is 4.62.
  Phobos: A LONG ROPE WITH A ROCK ON THE END — best real material (CNT fibre, best spun) tapers 1.00; taper 10 needs 4.22E-06 MJ/kg and the best ever made is 4.62.
  Deimos: A LONG ROPE WITH A ROCK ON THE END — best real material (CNT fibre, best spun) tapers 1.00; taper 10 needs 3.88E-06 MJ/kg and the best ever made is 4.62.
```

Five grades, one exponential, no opinions. **Luna's cable is a Kevlar cable** — taper 2.94, a
material with a datasheet and a price, which is Pearson's 2005 lunar-elevator conclusion reproduced
from the game's own ephemeris. **Ceres wants steel wire.** And at Phobos and Deimos the taper is
1.00 to every digit the table prints: the material is not a design input, it is a rounding error.

### E — what it saves, and which body is the flagship

The rocket's bill below is **ideal** — no gravity losses, no drag, and the ground's own rotation
counted as free speed — so Earth's real figure is 1.5–2 km/s worse than shown. The cable's bill is
zero propellant: a climber runs on mains power. Propellant is a 1 t parcel at Isp 320 s.

```
body        release point  rocket dv m/s  propellant kg/t  stock material?
--------------------------------------------------------------------------
Earth          sync orbit        11460.8          37556.5               no
Mars           sync orbit         5090.8           4064.5               no
Luna                   L1         2421.3           1163.2              yes
Ceres          sync orbit          403.9            137.4              yes
Phobos                 L1            8.4              2.7              yes
Deimos                 L1            4.7              1.5              yes

  FLAGSHIP: Luna — BUILDABLE WITH FIBRE YOU CAN BUY TODAY, saving 2421 m/s = 1163 kg of propellant per tonne shipped.

  (The biggest prize is Earth at 37556 kg/t — and it is BEYOND ANY REAL MATERIAL.
  The board's whole shape is that the cable gets easy exactly where the saving gets small.)
```

**The flagship rule, also stated before the data:** of the bodies whose cable can be spun from
material you can order by the kilometre *today*, the flagship is the one where building it saves the
most propellant per tonne. A laboratory record is a measurement, not a supply chain, so it does not
qualify a body — which is exactly what disqualifies the biggest prize on the board.

**The flagship is Luna**, and it is not close. It is the only body on the table where a cable made of
existing, purchasable fibre replaces a *serious* rocket: **2,421 m/s, 1,163 kg of propellant saved
per tonne shipped** — more than the parcel itself weighs. Ceres, the next one down, saves 137 kg.
Phobos saves under three.

### F — what a safety factor does, and what Mars is waiting for

```
body       best real taper    with SF 2x  verdict survives?
-----------------------------------------------------------
Earth                36559       1.3e+09                n/a
Mars                  7.87         61.88     NO — falls out
Luna                  1.79          3.22                yes
Ceres                 1.01          1.03                yes
Phobos                1.00          1.00                yes
Deimos                1.00          1.00                yes
```

Working stress is not breaking stress. Halve the allowable and the exponent doubles, which **squares
the taper**: 3 becomes 9, 13 becomes 167. Only Mars changes grade — and it is the row the table
argues about anyway. Mars wants **4.13 MJ/kg** for a taper of 10 and the best fibre ever spun is
**4.62**, so on paper Mars is already possible and in practice it is not: it is not waiting on a
breakthrough, it is waiting on a **factory** that can make kilotonnes of the best thing anyone has
made metres of.

And there is a second Martian problem no material fixes: areostationary sits at **20,428 km** from
Mars's centre, and **Phobos orbits at 9,377 km**. A Mars cable crosses Phobos's orbit twice a
Martian day, forever. *The moon that makes the easiest beanstalk in the system is standing in the way
of the hardest one.*

## The finding, in one breath

**Earth is not a hard engineering problem, it is the wrong planet.** Luna through L1 is a cable you
could order this afternoon, and it is the flagship because it is the only place where "buildable"
and "worth building" overlap. And at Phobos the elevator is not an elevator: it is a long rope with a
rock on the end, and the only reason nobody has built one is that nobody has been there.

## The game hook — the elevator as a HAVEN pair

*(Design prose, not in-game text. Nothing here is a shipped string.)*

The door family already has two members and one grammar. The **docking tube** is a door you
understand as a clamp; the **shuttle bay airlock** (#163/#199) is *"the door you understand as a
flight"* — you walk to it, a pop-up lists what is in range, you pick a berth, and the crossing itself
is the trip. There is no minigame in the middle because the walk *is* the middle.

The beanstalk is that family's **vertical member**: **the long ride**.

- **A HAVEN pair, not a place.** An elevator is two ports and a line between them: a **surface
  berth** at the anchor and a **counterweight dock** at the top, each a walkable interior in its own
  right, joined by one door. The map draws both ends and the cable as the line between them — the
  only structure in the game that is *visibly* two places at once.
- **The ride is the door.** Walk to the elevator airlock at the counterweight dock, and you are
  handed exactly one destination instead of the shuttle bay's list of many: down. The sim clock
  advances by the climb, you step off in the surface concourse, and the same door at the bottom
  brings you back. Symmetric, never stranding — the #199 rule, turned ninety degrees.
- **What makes it different from the shuttle.** The shuttle bay lists *berths in range* and costs
  fuel and a fare; the elevator has **one** destination, costs almost nothing, and cannot be denied
  to you by traffic or heat. It is the one crossing in the game with no flight in it at all. That is
  the whole flavour: an industrial commute.
- **Phobos: a monolith AND a beanstalk.** Worldbuilding-notes §7 already calls Phobos the strangest
  port in Sol — the 85 m monolith on the Stickney rim, the treasure-island map cards, deals sealed in
  the monolith's shadow. This lab supplies the fourth piece with a number behind it: at Phobos the
  cable is **5.57 km** long and its taper is **1.00**, so the port that shouldn't exist has an
  elevator no engineer had to be clever about. *Nobody agrees who built the monolith; everybody
  agrees the anchor bolts for the cable went in suspiciously easily.*
- **The rhyme with Luna's mass drivers.** Lab 30 measured Luna's other launch industry: fling the pod
  and let go. Now the same moon is the flagship for the industry that **never** lets go — and it is
  the flagship by a factor of eight over the next candidate. Two ways off one rock, one violent and
  one patient, competing for the same cargo. The mass driver is cheap, ballistic and interceptable
  (lab 30's whole pirate lesson); the cable is expensive, precise, and cannot be robbed in flight
  because there is no flight. That is a trade lane with an argument inside it.

## Break it

Three ways to damage this lesson on purpose, each of which teaches something the table alone doesn't.

1. **Delete the centrifugal term.** In `Beanstalk.MeasureSynchronous`, change `Phi` to
   `-body.Mu / r` and rerun. Earth's ΔΦ jumps from 48.5 to 53.1 MJ/kg and the taper column moves by
   orders of magnitude — that missing 4.7 MJ/kg is the planet's own spin, doing about a tenth of the
   work of holding the cable up. Now try it on Ceres, which spins in nine hours, and watch a much
   bigger fraction disappear.
2. **Trust the Hill approximation instead of bisecting.** Replace `L1Distance`'s root-find with
   `HillRadius(body)` and rerun. Luna barely notices (58,027 → 61,533 km, a 6% error in a number
   whose exponent is small). Phobos's tether gets **16.58 km** of clear air instead of 5.57 — three
   times too long — because at Phobos the L1 point and the Hill radius are *both* just above the
   ground and the difference between them is the whole structure. An approximation is only as good as
   the smallest thing it is being asked about.
3. **Move a threshold and watch the guard.** Change `PracticalTaper` from 10 to 1000 and run
   `dotnet test --filter Lab31`. Three tests go red at once: the property check (which carries its
   own literal 10), the published grades, and the README read-back — because Mars jumps from *only
   with the best fibre ever spun* to *buildable with fibre you can buy today*, and the sentences in
   this file stop being true. That is the lesson's real subject:
   **a verdict is only worth printing if something breaks when the numbers move underneath it.**

## Sources

- **Y. Artsutanov (1960)**, *Into the Cosmos by Electric Train* (Komsomolskaya Pravda) — the original
  proposal.
- **J. D. Isaacs, A. C. Vine, H. Bradner, G. E. Bachus (1966)**, "Satellite Elongation into a True
  Sky-Hook", *Science* **151**, 682 — the independent rediscovery.
- **J. Pearson (1975)**, "The orbital tower: a spacecraft launcher using the Earth's rotational
  energy", *Acta Astronautica* **2**, 785–799 — the taper relation used here, and the source of
  Earth's ~4,960 km characteristic length.
- **J. Pearson, E. Levin, J. Oldson, H. Wykes (2005)**, *Lunar Space Elevators for Cislunar Space
  Development*, NIAC Phase I final report — the lunar cable through L1 with materials that exist,
  which section D reproduces from the game's own ephemeris.
- **L. Weinstein (2003)**, "Space Colonization Using Space-Elevators from Phobos", *AIP Conf. Proc.*
  **654**, 1227 — the Phobos tether.
- **H. D. Curtis**, *Orbital Mechanics for Engineering Students* — ch. 2 for the two-body pieces and
  §2.12 for the restricted three-body problem and its Lagrange points.
- **Body constants**: `scenarios/sol.json` for Earth, Mars, Luna and Phobos (the game's own
  ephemeris, verbatim — held to it by `Lab31BeanstalkTests`); JPL Small-Body Database for Ceres and
  the NASA/JPL Mars fact sheet for Deimos; sidereal rotation periods from the standard planetary
  references.
- **Material properties** are representative published values for each fibre *class* — a class, not a
  quote from one lot's certificate. Steel wire and Kevlar 49 are ordinary datasheet figures; Zylon
  PBO is Toyobo's published tensile strength; the CNT fibre row is the best-reported macroscopic spun
  yarn, and the CNT theory row is the single-tube calculated ceiling, flagged in its own tier because
  nobody has ever made a metre of it.
