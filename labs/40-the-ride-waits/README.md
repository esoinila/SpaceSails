# Lab 40 — The ride waits

> *"…our autopilot would have to have a new mode (that does not cost an arm and a leg) that just keeps us
> at approximate relative shuttle-range position … that the ship does not leave them behind :-D"*
> — the owner, proposing boardable derelicts (issue #488)

A landing works because the ground holds still under you. A station works because you clamp to it. A
**derelict does neither**: it is a free-floating object on its own trajectory, and the moment the away
team launches, the mothership is a second free-floating object on a slightly different one.

Before writing that autopilot mode, this lab asks the three questions whose answers decide its design —
and the answer to the first one reframed the whole feature.

Run it: `dotnet run --project labs/40-the-ride-waits/Lab40.csproj -c Release`

**Every number below came from that run.** If you change the probe, rerun and re-paste — never hand-edit
a table.

---

## The setup

The shuttle's reach is `ShuttleRange.RangeMeters` — **500,000 km**. Holding the ship *at* that edge would
be cruel, so the working leash is half of it.

```
shuttle reach      : 500,000 km  (ShuttleRange.RangeMeters)
hold band (leash)  : 250,000 km  (half the reach)
start offset       :  25,000 km
```

Two ways to be asked to wait — and this distinction turned out to *be* the finding:

- **Fixed offset** — park at a constant displacement from the wreck and match its velocity. The naive
  reading of "hold relative position", and the one a player would assume works.
- **Co-orbital** — where the wreck *orbits* something, share that orbit: identical radius and speed,
  displaced only **along the track**. Same semi-major axis ⇒ same period ⇒ no phasing.

A fixed offset near a small body is nonsense if the offset dwarfs the orbit itself — 25,000 km
"alongside" a wreck circling Enceladus at 757 km is not alongside anything, it is a different trajectory.
So the ask is capped to a quarter of the orbital radius:

```
  regime                          | hold offset asked for
  --------------------------------|----------------------
  deep space (2.5 AU, no well)    | 25,000 km
  high over Earth (60 Re)         | 25,000 km
  low over Luna (3 Rl)            | 1,303 km
  low over Enceladus (3 Re)       | 189 km
  close over Jupiter (3 Rj)       | 25,000 km
```

## A — does the ship even wander off?

No thrust at all. How long until the gap opens past the leash?

```
  regime                          | fixed offset | co-orbital
  --------------------------------|--------------|-----------
  deep space (2.5 AU, no well)    |        never | never
  high over Earth (60 Re)         |       29.7 d | 29.8 d
  low over Luna (3 Rl)            |        never | never
  low over Enceladus (3 Re)       |        never | 19.9 d
  close over Jupiter (3 Rj)       |        4.7 d | never
```

**The ride does not vanish on you.** Endurance is measured in days-to-never everywhere. So the mode is
not a rescue from an imminent problem — it is a **guarantee**, which is a different design brief: it must
be cheap and boring and always on, not dramatic.

The one place a naive hold genuinely fails is **close over Jupiter: 4.7 days** on a fixed offset. And
notice the inversion in that row — the fixed offset leaves, the co-orbital one *never* does.

## B — the hand-off error

A real drop is never perfectly matched. Sweeping a residual relative velocity at shuttle launch, against
the good (co-orbital) hold:

```
  regime                          |    0 m/s |    1 m/s |   10 m/s |  100 m/s
  --------------------------------|----------|----------|----------|---------
  deep space (2.5 AU, no well)    |    never |    never |    never |   28.6 d
  high over Earth (60 Re)         |   29.8 d |   11.9 d |    never |    7.6 d
  low over Luna (3 Rl)            |    never |    never |    never |    never
  low over Enceladus (3 Re)       |   19.9 d |   19.8 d |   22.9 d |   13.9 d
  close over Jupiter (3 Rj)       |    never |    never |    never |    never
```

Even a sloppy 100 m/s hand-off buys **a week or more** everywhere. The non-monotonic rows (1 m/s worse
than 10 m/s over Earth) are the honest signature of a chaotic three-body neighbourhood, not noise in the
measurement — an error can nudge you onto a *more* stable relative orbit as easily as a less stable one.

**Match before you launch, but do not agonise over it.**

## C — what the hold costs

A dead-band keeper: trim only when the gap leaves the leash — the owner's *"approximate"* — priced in
pulses through `OrbitRule.PulsesFor`, the same kernel the autopilot's real burns spend with.

```
  regime                          | FIXED OFFSET                | CO-ORBITAL
                                  | trims  p/day  worst Δv      | trims  p/day  worst Δv
  --------------------------------|-----------------------------|--------------------------
  deep space (2.5 AU, no well)    |     0    0.0      0.00 km/s |     0    0.0      0.00 km/s
  high over Earth (60 Re)         |     0    0.0      0.00 km/s |     0    0.0      0.00 km/s
  low over Luna (3 Rl)            |     0    0.0      0.00 km/s |     0    0.0      0.00 km/s
  low over Enceladus (3 Re)       |     0    0.0      0.00 km/s |     0    0.0      0.00 km/s
  close over Jupiter (3 Rj)       |     2   44.4     33.29 km/s |     0    0.0      0.00 km/s
```

**The headline: a co-orbital hold costs ZERO pulses per day in every regime measured.**

Not "cheap" — *free*. The owner asked for a mode that does not cost an arm and a leg; the physics says it
need not cost anything at all, because two objects sharing an orbit are not fighting anything. There is
nothing to pay for.

The fixed-offset column is the cautionary tale. Close over Jupiter it needs 2 trims in 5 days, and each
recovery is a **33.29 km/s** burn — 111 pulses a go against a 500-pulse tank, because by the time the gap
has opened a quarter-million kilometres the two orbits have diverged violently and hauling back is a
transfer, not a trim. **That is the arm and the leg, and it is caused entirely by holding the wrong thing.**

---

## What this means for the feature

1. **Implement the co-orbital hold, not a position hold.** "Keep station on the wreck" must mean *share
   its trajectory* — match velocity and let orbital mechanics do the work. A keeper that chases a fixed
   point in space is the expensive version of the same idea and buys nothing.
2. **The quote can honestly read "free".** Arm-time should say so plainly: *"holding — the ship will be
   here when you get back."* No pulses/day figure is needed for the ordinary case, because there isn't
   one. That is a nicer promise than the orbit keeper's *"trim ≈27 p/day"* (#193/Lab 25) and it is earned.
3. **The failure mode to guard is the HAND-OFF, not the fuel.** Endurance is set almost entirely by how
   well the ship matched the wreck before the shuttle left. So the mode's real precondition is a match
   check at arm time — refuse to launch the away team while the relative velocity is large — rather than
   a fuel gate.
4. **Deep inside a giant's well is the one place to warn.** It is also the most interesting place to put
   a wreck, and now there is a mechanic there: a derelict close over Jupiter is a site where the hold is
   genuinely harder and the fiction of "the ship slid away while you were inside" can be *earned* rather
   than faked.
5. **The hypothesis in #488 was half right.** Holding station *is* free outside a well — but the reason
   is not the tidal gradient being small. It is that a co-orbital hold has nothing to fight *anywhere*.
   The gradient only bites when you insist on the wrong hold. **The design changed because of this lab**,
   which is what the lab was for.

## Provenance

`Probe.cs` flies the live `Simulator` over Lab 17's `sol.json` field, uses `ShuttleRange` for the leash
and `OrbitRule.PulsesFor` for the bill — the same Core code the game spends with, so the lab's number and
the game's number are one number.

A first cut of section C closed the standing gap "in an hour", which is a 69 km/s burn, and produced a
nonsense bill of ~2,000 pulses/day against a 500-pulse tank. The keeper's job is to stop the divergence,
not to teleport; the return window is now 12 h. Left here as a note because the wrong version *looked*
plausible in a table.
