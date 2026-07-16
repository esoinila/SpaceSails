# Lesson 25 — The tide that takes it back

*The owner flew the first full mission end to end — contract at a bar, cross-well transfer,
auto-park at Enceladus, parcel delivered — and then watched the orbit quietly die: "it troubles
me greatly that the ship is left to be kind of in orbit by luck there. The orbit keeping should
always be the job of autopilot this close to a moon, never a manual task." The autopilot ACHIEVED
an orbit; it did not KEEP one. This lesson measures the two numbers that turn "achieved" into
"kept": WHERE a parked orbit around a small moon goes chaotic and how fast it dies, and what it
COSTS to hold the park against the tide — priced in the game's own pulses, flown in the real
N-body sim.*

```bash
dotnet run --project labs/25-the-tide-that-takes-it-back -c Release
```

## Why this lesson exists

Lesson 7 priced the bus stops (Hill spheres, insertion pulses) and lesson 16 went ashore — it
found that a moon is a bus stop nested inside a bus stop, its Hill sphere carved out of its
parent's, and that near the Hill edge the parent's tide strips a parking orbit. The autopilot
parks at 0.33 Hill on the strength of that. But "0.33 Hill is inside the stable band" is a
statement about the WINDOW, not about what happens over the next day. The owner's Enceladus park
sat inside the window and still fell out of the sky. Two things were missing: an honest map of how
fast a park decays where, and a keeping loop that spends pulses to hold it. This lesson is both.

## The standard-textbook take

**The Hill problem's forced eccentricity** (Hénon; Murray & Dermott ch. 3). A satellite on a
circular orbit at radius `r` around a moon that itself orbits a planet does not stay circular. In
the rotating frame the planet's differential pull — the tide, `a_tide ≈ 2 μ_planet r / D³` across
the park at the moon's distance `D` — forces an eccentricity that grows with `r/R_Hill`. Below
~0.4–0.5 Hill a prograde orbit's forced eccentricity is bounded and it survives; above it the
free and forced terms beat into chaos and the orbit strips within tens of orbits (lesson 16's
"prograde unravels past ~0.5 Hill"). Retrograde orbits ride higher (the distant-retrograde
island), but the autopilot flies prograde.

The catch a textbook states in fractions and the game must state in kilometres: the forced
eccentricity is a fraction of `r`, and `r` at 0.33 Hill is a fraction of the Hill radius, and the
Hill radius is a multiple of the BODY radius — and for a tiny deep-well moon that last multiple is
tiny. Enceladus's whole Hill sphere is **3.8 body radii**. So 0.33 Hill is 1.24 R — barely a
snowball's width off the surface — and a forced eccentricity of 0.3 puts periapsis *underground*.

## What the game adds that the textbook doesn't

The keeping LOOP, priced honestly. Station-keeping re-circularizes the drifted orbit — but with
one crucial choice the naive version gets wrong (measured below): the trim pins the two-body
**semi-major axis to the park radius** (vis-viva energy target), not to the current radius.
Re-circularizing at the current radius random-walks the semi-major axis into the ground when trims
fire off-radius; pinning the energy to the park orbit holds it. And the trim fires on a **cadence**
(once every quarter park period), not every tick — because the forced eccentricity is a *reversible*
oscillation that the tide undoes for free, and fighting it every tick pays for motion you'd get
back anyway. Trim on cadence and you pay for the SECULAR drift alone. Both choices are Core
(`OrbitKeeping`); the lab flies them and prices the result per body.

## The numerical experiment

### A — the drift sweep: where the band bites, and how fast

Ballistic parks flown in the live N-body field. `t_leave_band` is when the instantaneous two-body
verdict (`OrbitRule.ParkStability`) leaves Stable; `t_death` is the first REAL death — periapsis
under the physical surface (impact) or escape past the Hill sphere — both in park-orbit periods.
`—` = neither within 40 orbits.

```
--- ENCELADUS --- Hill 949 km (3.8 R), park 313 km (0.33 Hill = 1.24 R), band ceiling 0.40 Hill, park period 3.6 h
  circular (e=0) parks swept OUTWARD by a/Hill:
      a/Hill    a (R)  t_leave_band   t_death    ending   (orbits)
        0.20     0.75           0.0       0.0    impact
        0.30     1.13           0.1       0.3    impact
        0.33     1.24           0.3       0.4    impact
        0.40     1.50           0.0       0.6    impact
        0.45     1.69           0.0       0.7    impact
        0.53     1.99           0.0       0.9    impact
        0.65     2.45           0.0       1.1    impact
        0.80     3.01           0.0       1.2    impact
  eccentric parks at the 0.33-Hill radius, swept by e:
           e  apo/Hill  t_leave_band   t_death    ending   (orbits)
        0.00      0.33           0.3       0.4    impact
        0.05      0.35           0.3       0.4    impact
        0.10      0.36           0.2       0.4    impact
        0.20      0.40           0.0       0.0    impact
        0.30      0.43           0.0       0.0    impact

--- LUNA --- Hill 61533 km (35.4 R), park 20306 km (0.33 Hill = 11.69 R), band ceiling 0.40 Hill, park period 72.1 h
  circular (e=0) parks swept OUTWARD by a/Hill:
      a/Hill    a (R)  t_leave_band   t_death    ending   (orbits)
        0.20     7.08             —         —  survived
        0.30    10.62             —         —  survived
        0.33    11.69             —         —  survived
        0.40    14.17           0.0         —  survived
        0.45    15.94           0.0         —  survived
        0.53    18.77           0.0         —  survived
        0.65    23.02           0.0       3.5    escape
        0.80    28.33           0.0         —  survived
  eccentric parks at the 0.33-Hill radius, swept by e:
           e  apo/Hill  t_leave_band   t_death    ending   (orbits)
        0.00      0.33             —         —  survived
        0.05      0.35             —         —  survived
        0.10      0.36             —         —  survived
        0.20      0.40             —         —  survived
        0.30      0.43           0.0         —  survived

--- TITAN --- Hill 52404 km (20.4 R), park 17293 km (0.33 Hill = 6.72 R), band ceiling 0.40 Hill, park period 41.9 h
  circular (e=0) parks swept OUTWARD by a/Hill:
      a/Hill    a (R)  t_leave_band   t_death    ending   (orbits)
        0.20     4.07             —         —  survived
        0.30     6.11             —         —  survived
        0.33     6.72             —         —  survived
        0.40     8.14           0.0         —  survived
        0.45     9.16           0.0         —  survived
        0.53    10.79           0.0       3.5    escape
        0.65    13.23           0.0       1.1    escape
        0.80    16.28           0.0       0.9    escape
  eccentric parks at the 0.33-Hill radius, swept by e:
           e  apo/Hill  t_leave_band   t_death    ending   (orbits)
        0.00      0.33             —         —  survived
        0.05      0.35             —         —  survived
        0.10      0.36             —         —  survived
        0.20      0.40           2.3         —  survived
        0.30      0.43           0.0         —  survived
```

Read the three moons against each other and the owner's report resolves itself. **Luna and Titan
have a stable core**: circular parks from the surface out to ~0.5 Hill survive the whole 40-orbit
horizon; only past ~0.53–0.65 Hill does the tide win and the orbit escapes (Titan 0.53 → escapes
in 3.5 orbits; the ragged Luna 0.65/0.80 pair is the step-sensitive chaotic coastline lesson 16
warned about, not a clean boundary). Their `t_leave_band = 0.0` rows are the harmless truth that
the forced eccentricity's apoapsis brushes just past the 0.40-Hill band ceiling and comes right
back — bounded oscillation, safe periapsis (Luna's periapsis never drops below 5 R, Titan's below
3 R; measured in B2).

**Enceladus has no stable park at all.** Every circular radius crashes — the deep ones because
0.33 Hill is 1.24 R and the tide forces e≈0.3, dropping periapsis *below the surface* within half
a day; the shallow rows (0.20 Hill = 0.75 R) because they start underground. The Hill sphere is
simply too tight to hold a snowball's parking orbit against Saturn. This is the owner's stranded
ship, reproduced from first principles: **at Enceladus the orbit is NOT kept by luck or by
geometry; it is kept only by active trimming, or not at all.**

### B — the cost of keeping

Park circular at 0.33 Hill and HOLD it: each cadence point, if the tide has pumped the eccentricity
past `OrbitKeeping.TrimEccentricity`, re-circularize with a park-radius-pinning trim, counting the
Δv and the pulses at the ship's real world speed. First, the cadence trade on the hard case —
Enceladus — showing why you must NOT trim every tick:

```
=== Section B1: the trim CADENCE — riding the reversible oscillation vs fighting it ===
  cadence f  trims/day  Δv/day m/s  pulses/day  peri min (R)   held?
       0.02      332.0      817.51       332.0         1.063     yes
       0.05      117.8      699.53       117.8         1.028     yes
       0.10       57.8      638.12        57.8         0.946     yes
       0.25       26.5      540.22        26.8         0.861     yes
       0.50        9.5      219.85         9.5         0.697   CRASH
       1.00        3.6       41.89         3.6         0.750   CRASH
```

The U is the whole lesson. Trim too OFTEN (cadence 0.02 = every 4 minutes) and you fight the
reversible forced oscillation: 332 trims and 818 m/s a day, most of it undone by the tide for free.
Trim too RARELY (half a period or a whole one) and the eccentricity climbs past the surface between
checks and the ship CRASHES. The knee is **once every quarter park period**: 27 pulses a day holds
Enceladus with the actual trajectory never touching the surface (osculating periapsis dips to
0.86 R — a linear extrapolation the tide never lets it reach). The keeping bill at that cadence,
per moon, over a 60-orbit hold:

```
=== Section B2: the keeping bill at the chosen cadence, per moon (60-orbit hold) ===
moon            days  trims/day  Δv/day m/s  pulses/day  peri min (R)   held?
enceladus        9.0      26.55     541.647       26.77         0.861     yes
luna           180.3       1.33      29.818        1.33        10.367     yes
titan          104.7       2.28      77.131        2.42         6.086     yes
```

Enceladus is genuinely the expensive park — **27 pulses a day**, ~8 days of keeping on a 250-pulse
tank — and the lab says so honestly instead of pretending a deep well is free. Luna (**2 p/day**)
and Titan (**3 p/day**) are cheap: their stable core barely needs holding, so keeping only nips the
slow secular drift. All three held the whole propagation with periapsis clear of the surface.

### C — the table Core consumes, and the estimate for every other moon

Δv/day scales with the tide the parent raises across the park, `a_tide = 2 μ_p r / D³`. Fit
`Ktide = Δv/day ÷ (a_tide · 86400)` and its mean prices any moon the lab never flew. The pulses/day
the arm-time contract quotes is these Δv/day priced at the parked ship's world speed (a pulse deep
in Saturn's fast well buys more Δv than a slow one — Luna rides 29.6 km/s, so its trims cost fewer
pulses per m/s than Enceladus's 7.6 km/s park).

```
moon           a_tide m/s²     Δv/day     Ktide  world km/s  p/day@world
enceladus       1.761E-003    541.647     3.559        7.57           27
luna            2.850E-004     29.818     1.211       29.57            2
titan           7.192E-004     77.131     1.241        4.88            3

mean Ktide = 2.004
```

Those three rows are pasted verbatim into `src/SpaceSails.Core/OrbitKeepingTable.cs`, and the mean
Ktide is `OrbitKeeping.TideBudgetConstant`, the fallback for un-measured moons. The game then quotes
the trim budget at arm time ("insertion armed … trim ≈27 p/day") and holds the park with these very
constants until the captain deliberately disarms (double-confirmed) or the tank runs dry — a LOUD
handback, after which the #180 degradation alert takes over as the backstop.

## Break it on purpose

1. **Re-circularize at the current radius.** Point `OrbitKeeping.Trim` at the ship's own radius
   instead of the park radius and rerun B1. Enceladus crashes at EVERY cadence, and — the tell —
   *tighter* tolerance crashes while looser holds: frequent off-radius trims random-walk the
   semi-major axis into the ground. Energy pinning is the fix.
2. **Trim every tick.** Set the cadence to 0.02 and read the treadmill: 332 trims a day for the
   same orbit a 27-trim cadence holds. You are paying to undo the tide's reversible work.
3. **Park Enceladus at 0.5 Hill.** Move the park out and rerun the sweep. The forced eccentricity's
   apoapsis reaches into the chaotic band and the orbit strips — the deep well has no good answer,
   only a least-bad one, which is why it is the expensive park.

## The framing rule, kept

Standard physics presented as standard: the Hill problem's forced eccentricity and the prograde
stability boundary (Hénon; Murray & Dermott ch. 3; lesson 16's own measurement). The keeping loop —
energy-pinned trims on a quarter-period cadence — is ours, but the trade it navigates (reversible
forced oscillation vs irreversible secular drift) is textbook station-keeping doctrine, computed
honestly on our rails and priced in the game's own pulses. Every number above came from running the
probe; change the code and rerun — never hand-edit a table.
