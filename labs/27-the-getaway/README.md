# Lesson 27 — The getaway

*The game has always computed the catch honestly — `EncounterRule` flies a thrust-only wolf (the
owner's standing ruling: hunters chase with fixed thrust, NO gravity, NO autopilot) and marks
`CaughtPlayer` the instant it comes inside the catch radius under the catch-speed cap. Nothing was
done with it: a collector that caught you simply went inert. PR-BUSTED turns that flag into a
GTA-style boarding scene, and its RESIST/RUN dice want to quote HONEST odds, not vibes. So this
lesson measures the chase from first principles: where is a catch physically EARNABLE (the wolves'
honesty contract — no rubber-banding, ever), and what are the player's three escapes actually worth
when flown through the real machinery — the **sling**, the **skim** heat-bleed, and the **phasing
juke**.*

```bash
dotnet run --project labs/27-the-getaway -c Release
dotnet run --project labs/27-the-getaway -c Release -- --viz
```

## Why this lesson exists

A pursuit needs a promise: that the wolf catches you when it has *earned* the catch, and never
otherwise. The alternative — rubber-banding, where the chase tightens because the story wants it to
— is the death of trust in a physics game. The game's wolf is deliberately dumb: `EncounterRule.
AdvanceHunter` accelerates it toward the player's LIVE position at a fixed `HunterAccelMps2`, feels
no gravity, and scores a catch only inside `CatchRadiusMeters` under `CatchRelativeSpeedMetersPerSecond`.
That is a *contract*, and this lesson reads it back as measured physics so the BUSTED pop-up can quote
the numbers the chase actually produces.

## The standard-textbook take

Pure pursuit — a pursuer always steering at the target's current position — is a classic curve
(Curtis doesn't cover it; it is a staple of ballistics and pursuit-evasion theory). The one identity
that matters here is energy-of-approach: a pursuer that thrusts at constant `a` from rest is only
still below a speed `u` after crossing `u²/(2a)`. Past that distance it is faster than `u` — so if
the catch requires arriving *under* `u`, a long chase self-defeats: the wolf arrives too hot and
overshoots. Everything below is that identity, flown.

## What the game adds that the textbook doesn't

Real orbital mechanics hands the *player* three ways to weaponize the overshoot, and each is a Core
system the game already flies with: a gravity **sling** (`SlingPlanner`, lesson 19) donates a
velocity change the thrust-only wolf would need hours of burn to match; a **skim** through a planet's
air (`Simulator` drag, lesson 22) sheds speed the dragless wolf keeps; a **phasing juke**
(`TransferMath.PhasingOrbit`, lesson 24) changes the player's clock so the wolf's aimed solution goes
stale. The measured envelope and the three margins become `PursuitOdds` — a pure Core query the
RESIST/RUN dice read.

## The numerical experiment

### A — the wolf's contract

```
thrust accel     a  = 0.50 m/s^2 (toward the player's LIVE position — no gravity, no autopilot)
catch radius     R  = 300,000 km
catch speed cap  u  = 3,000 m/s (a wolf roaring past faster than this does NOT catch)
integration quantum 60 s (same cadence as NPC traffic)
```

The killer identity: `u²/(2a)` = **9,000 km**. From a standing start the wolf is under its 3 km/s
catch cap only after accelerating across 9,000 km — chase a runner across more open space than that
and it arrives too hot to grab. The thrust-only wolf is fast, but it cannot be fast AND gentle.

### B — the pursuit envelope (where a catch is earnable)

Fly the REAL `AdvanceHunter` law from a spread of head starts against a player fleeing straight at a
spread of speeds. Cell = time-to-catch in days, or `runs` when the wolf never closes to a ≤cap grab
inside a 120-day horizon.

```
head start \ flee       0.0 km/s      0.5 km/s      1.0 km/s      2.0 km/s      3.0 km/s      5.0 km/s
------------------------------------------------------------------------------------------------------
      100,000 km        0.0 d        0.0 d        0.0 d        0.0 d        0.0 d        0.0 d
      200,000 km        0.0 d        0.0 d        0.0 d        0.0 d        0.0 d        0.0 d
      300,000 km        0.0 d        0.0 d        0.0 d        0.1 d         runs         runs
      400,000 km         runs         runs         runs         runs         runs         runs
      600,000 km         runs         runs         runs         runs         runs         runs
    1,000,000 km         runs         runs         runs         runs         runs         runs
    3,000,000 km         runs         runs         runs         runs         runs         runs
```

```
The overshoot, made concrete — three 'runs' cells, their CLOSEST pass and the relative speed there:
  head start    600,000 km, flee 1.0 km/s -> closest         45 km at   24,530 m/s (HOT — roars past)
  head start  1,000,000 km, flee 2.0 km/s -> closest        755 km at   31,690 m/s (HOT — roars past)
  head start  3,000,000 km, flee 0.0 km/s -> closest        795 km at   54,780 m/s (HOT — roars past)
```

There is a clean cliff, and it is **the catch radius itself**. Inside ~300,000 km at a modest flee
speed, the wolf grabs you (instantly, when you start inside its jaws). Past it, the wolf's closest
pass is fast — it screams *through* the 300,000 km catch radius above the 3 km/s cap (45 km away at
**24,530 m/s**!) and cannot grab. The boundary tightens as you run harder — at exactly R, a flee of 2
km/s is still caught but 3 km/s runs. **A wolf that "catches up" from far away always arrives too
hot.** That is the honesty contract: no rubber-banding, ever.

### C — the sling escape (a bend the wolf cannot follow)

Solve a real Earth→Jupiter crank with `SlingPlanner` (lesson 19's ApproachBurnState case, 12 R_J on
the Lead side):

```
burn 1291.3 m/s, 1 Newton iters -> pass 11.9 R_J (Lead side).
heliocentric speed  before 4.87 km/s  ->  after 16.89 km/s   (gain 12014 m/s, apoapsis 17.5 AU)
lever: one pulse at the aim shifts the far end of the pass by 5 Gm (re-trim after the pass).
```

The flyby donated **12.0 km/s** of heliocentric speed — free, from gravity, at a corner the
thrust-only wolf (no gravity) cannot thread. To match just that speed it would burn `12,014 / 0.5` =
**6.7 hours** of continuous, perfectly-aimed thrust — and through the pass it was pointed at your old
vector. Post-sling the player recedes at 12 km/s (**4× the catch cap**) on a 17-AU arc; classified
against the flown envelope, any reacquiring wolf is in the `Clear` region (Run odds `Certain`). The
sling is the **strategic** getaway — it doesn't shake a wolf on your tail this hour, it rewrites the
trajectory so the wolf's whole pursuit restarts from a boost-sized deficit. The tactical cousins are
next.

### D — the skim heat-bleed (drag off speed, and the wolf overshoots)

Dip Jupiter's air (lesson 22's shell) on a 5.5 km/s arrival, swept by depth; the corridor is the
depth that bleeds real speed under the 3 g sail-hole line.

```
arrival v_inf = 5.5 km/s
  peri alt km  min alt km   dv shed m/s    peak g               outcome
           20        13.5          3224      3.76  TOO DEEP: hull holed
           40        34.0          1567      1.95            clean skim
           60        54.2           717      1.00            clean skim
           80        74.4           321      0.52            clean skim
          100        94.6           127      0.26            clean skim
          130       124.7           -43      0.10           too shallow
          170       164.9           -72      0.03           too shallow
          220       215.2          -101      0.00           too shallow
          300       295.6          -125      0.00           too shallow
```

The deepest CLEAN skim, periapsis **40 km**, sheds **1,567 m/s** at 1.95 g — a pass lasting 7
minutes. Fly a wolf tailing 1.5 R astern, matched to the pre-skim velocity, through the pass:

```
Tailing wolf (1.5 R astern, matched) through the pass: closest 449,634 km at 10,118 m/s, ends 449,634 km astern, OVERSHOOTS.
```

The wolf feels neither drag nor gravity — it cannot make the periapsis turn and keeps the speed the
player just shed, ending the pass **10,118 m/s** slower and still 449,634 km back. The overshoot
margin is concrete: the wolf carries the 1,567 m/s the player shed as excess closing speed, and at
0.5 m/s² it needs `1,567 / 0.5` = **52 minutes** merely to null it — and a wolf over the 3 km/s cap
cannot grab meanwhile.

### E — the phasing juke (stale the intercept)

The player is on Ringside's lane (lesson 24's 92,640 km case). A wolf's FIRING solution is aimed at
the player's current plot — the straight coast — and a round hits within `OrdnanceRule`'s **0.5 Mm**
radius. Commit a phasing juke (dip inside, coast k laps): the real track walks off that plot, and
once it walks off by more than the hit radius the solution is a lie.

```
k     enter m/s   pulses   wait d   stale @1d Mm   stale @3d Mm   shot void after
------------------------------------------------------------------------------------
1         19.51        1     18.3           1.66           5.03             7.5 h
2          9.70        1     36.8           0.82           2.50            14.5 h
3          6.46        1     55.4           0.55           1.67            22.0 h
4          4.84        1     73.9           0.41           1.25            29.5 h
5          3.87        1     92.4           0.33           1.00            37.5 h
6          3.22        1    110.9           0.27           0.83            45.0 h
```

Read it as the owner does: no heat, ride the cheap high-k bus (a 3 m/s enter burn, slow to diverge)
and wait it out; heat on the tail, pay the k=1 fare to be **gone** — the biggest immediate staleness a
single up-front burn can buy, so the wolf's shot goes void soonest (**7.5 h** vs 45 h for k=6) and you
re-emerge on a new clock (k=1 back at the doorstep in ~18 d, k=6 in ~111). This is the SAME k-table
lesson 24 reads for economy — *"the cheaper-sooner tradeoff comes in handy when there is heat on us"*
— read here for evasion: change your clock and the solution the wolf computed is a lie.

### F — `PursuitOdds`, the honesty contract as a Core table

Section B's envelope and C/D/E's margins become a small pure Core query — data only, no gameplay
wiring — that the BUSTED pop-up's RESIST/RUN dice draw modifiers from. Printed straight from the API
so this README cannot drift from the code:

```
catch boundary head start = 300,000 km (= EncounterRule catch radius); runner cap = 3,000 m/s.

Geometry classes, from the flown envelope:
  InItsJaws   — within R (300,000 km) and under the 3,000 m/s cap — a grab is earnable now
  EvenChase   — at ~R but running hot (over 3,000 m/s) — the coin-flip band
  SternChase  — past R, inside ~2R — the wolf is closing but overshooting
  Clear       — well past R — the thrust-only wolf arrives too hot to grab

trick \ geometry
                InItsJaws    EvenChase    SternChase   Clear
--------------------------------------------------------------------
Run             Forlorn-3    EvenMoney+0  Likely+2     Certain+4
Sling           Slim-1       Likely+2     Certain+4    Certain+4
Skim            EvenMoney+0  Likely+2     Likely+2     Certain+4
PhasingJuke     Slim-1       EvenMoney+0  Likely+2     Certain+4

EscapeOdds -> dice modifier ladder (the number the RESIST/RUN roll adds):
  Forlorn    -3
  Slim       -1
  EvenMoney  +0
  Likely     +2
  Certain    +4
```

Each cell is the classification of a flown margin: **Run** is the envelope itself (hopeless in the
jaws, yours once past R); **Sling** is decisive with any room (12 km/s the wolf can't match); **Skim**
forces the overshoot (1,567 m/s kept); **PhasingJuke** grows with room and time (weak from the jaws,
where the wolf re-aims to your live position; decisive with a lane and a head start). Sample live
queries:

```
  head start    200,000 km, rel 0.5 km/s -> InItsJaws   | Run         odds Forlorn (dice -3)
  head start    300,000 km, rel 4.0 km/s -> EvenChase   | Sling       odds Likely (dice +2)
  head start    700,000 km, rel 1.0 km/s -> Clear       | Skim        odds Certain (dice +4)
  head start  2,000,000 km, rel 2.0 km/s -> Clear       | PhasingJuke odds Certain (dice +4)
```

With `--viz`: one sling escape (the whip past Jupiter in the sun frame, with aim-burn and flyby
markers) and one phasing juke (the k=1 dip walking off the stale coast plot, in Saturn's frame).

## Break it on purpose

1. **Widen the wolf's thrust and watch the cliff move.** `HunterAccelMps2` sets `u²/(2a)`; the whole
   envelope in Section B is that reach measured. Nudge the constant and rerun — the catch boundary is
   not a story knob, it is `u²/(2a)` in disguise.
2. **Skim too deep.** Section D's 20 km pass sheds the most speed (3,224 m/s) but crosses the 3 g line
   and holes the sail. Free braking has a floor — the corridor is exactly the depths that stay under
   it, and the deepest CLEAN skim (40 km) is the strongest honest heat-bleed.
3. **Juke with too many laps.** The k=6 dip is the cheapest burn (3.2 m/s) but takes 45 h to void the
   shot and 111 days to come home. The k-table is a bus schedule with a wolf at the stop: cheaper is
   slower, and when there is heat on you, slower is the wrong bus.

## The framing rule, kept

Standard physics presented as standard: pure pursuit and energy-of-approach are ballistics staples;
the sling is lesson 19's crank, the skim is lesson 22's drag, the juke is lesson 24's phasing.
Nothing here is invented — the envelope is `EncounterRule`'s own catch constants flown through its own
pursuit law, and the three escapes are the game's own Core systems measured against it. Every number
here came from running `Probe.cs`; change the code and rerun before trusting the tables.
