# Lab 41 — Can you get there from here?

> *"THERE IS NO WAY TO GO TO THE BACK OF THE SHIP … we need some kind of CI test to spot similar problems.
> Maybe do a lab with A-star algorithm and rig it up to our CI tests."*
> — the owner, standing in a derelict that was sealed in half

He was right twice over. Two mutiny barricades spanned the full width of the wreck's spine corridor and cut
her in two — the cargo manifest, four compartments and the whole aft end were unreachable — and **every
build was green the entire time.**

That is the lesson this lab exists to make unmissable:

> **A wall you cannot pass has no test that fails.**

Type checks don't walk rooms. Unit tests of the pieces don't walk rooms. The only thing that catches a room
sealed by accident is something that actually tries to walk from A to B — which is A*, and which is now
wired into CI as `WreckLayoutTests`.

Run it: `dotnet run --project labs/41-can-you-get-there/Lab41.csproj -c Release`

**Every number below came from that run.**

---

## A — the algorithm, on the smallest honest example

A corridor six units tall with a barricade across the middle. Once solid, once leaving a 2 du gap.

```
  barricade FULL HEIGHT      → NO ROUTE — the corridor is sealed
  barricade with a 2 du gap  → reached in 32 steps
```

**That is the entire failure mode.** The same wall, two units shorter, and the ship works.

A* expands the cheapest frontier square first — cost-so-far plus a straight-line guess to the goal — and
either arrives or exhausts the reachable set. Two details matter for a deck:

- **The captain has width.** A square counts as walkable only if it is clear of every wall by
  `AvatarRadius` — the same `SurfaceCollision.Blocked` test the live movement uses, so the audit and the
  game agree by construction rather than by comment.
- **No cutting corners.** A diagonal is only taken when both its orthogonal neighbours are clear, or the
  walk could slip between two walls that touch — a route the real collision would never allow, and a
  "reachable" verdict the player could not honour.

## B — why the grid step matters

The walk samples on a grid. Too coarse and it steps straight over a real doorway and "proves" a ship sealed
that isn't; too fine and an audit that should be instant isn't.

```
  step   | stern reachable | wall-clock
  -------|-----------------|-----------
  2.00   | yes             | 0.9 ms
  1.00   | yes             | 1.6 ms
  0.50   | yes             | 2.8 ms
  0.25   | yes             | 5.7 ms
```

The wreck's doorways are 6 du wide, so every step here finds them — the sweep is here to show the *shape* of
the trade, not because the current geometry is marginal. The audit runs at **0.50**: fine enough for a
doorway a captain could actually use, cheap enough to walk every cause on every CI run in single-digit
milliseconds.

**If doorways ever get narrower, this table is where you check the step still resolves them.**

## C — the audit, over every cause

This is exactly what `WreckLayoutTests` asserts on every run.

```
  cause                | stations | compartments | bow | stern
  ---------------------|----------|--------------|-----|------
  DriveFailure         | all ok   | 7 ok         | ok  | ok
  ReactorCascade       | all ok   | 7 ok         | ok  | ok
  HullBreach           | all ok   | 7 ok         | ok  | ok
  LifeSupportFailure   | all ok   | 7 ok         | ok  | ok
  NavigationalError    | all ok   | 7 ok         | ok  | ok
  Mutiny               | all ok   | 7 ok         | ok  | ok
  Piracy               | all ok   | 7 ok         | ok  | ok
  InsuranceJob         | all ok   | 7 ok         | ok  | ok
```

It earned its keep on its **first run**: `HullBreach`'s evidence station was standing exactly on the
bulkhead that NEAR HOLD and LIFEBOAT CRADLES share, so the one console that explains the wreck could not be
walked to. Nobody had noticed, because that cause hadn't been rolled yet.

## D — the bug, reproduced

A guard that only ever passes guards nothing. Put the original full-height barricades back:

```
  stern reachable : NO
  sealed stations : the cargo (the decision), the cargo manifest, the drive bells
```

That's the ship the owner was standing in, and the red test we now have for it. The audit names **every**
unreachable thing at once rather than failing on the first, so one red run tells the whole story of what
the geometry sealed off.

---

## What this changed

1. **The wreck's geometry moved into Core** (`WreckLayout`), the same split `SurfaceLayout` already uses.
   It lived in the client where no test could reach it — which is *why* the bug survived. Layout that
   cannot be walked by a test is layout that will eventually seal a room.
2. **One list feeds walls and doors.** Door centres were separate literals in the wall builder and the door
   builder. That's how you get a gap nobody can see — and a gap nobody can see is the same as no gap.
3. **A rule, written where it can be broken:** *damage may never seal the spine.* Damage that belongs in the
   corridor (barricades) must leave a gap wider than the captain; damage that crosses the ship (a breach) is
   drawn as the holes it made, not as a line through the middle.
4. **A* over a flood fill,** because the route is what makes a red test readable. A flood fill says "not
   connected"; A* hands back the path to what *is* reachable, and the shape of that tells you which wall
   did it.

## Provenance

`Probe.cs` walks `DeckReachability` over `WreckLayout`'s geometry using `SurfaceCollision`'s own
blocked-point test — the same Core code the captain moves with, so the lab's verdict and the game's walls
are one thing.

The honest history: this wall was "fixed" three times before it was understood. Twice by reasoning from the
symptom (*a captain who can't pass ⇒ the gap must be too narrow*) and once by tracing the generator by hand.
The width was never the problem on any of those attempts. **Walking it would have answered in milliseconds
what three rounds of reasoning got wrong** — which is the whole argument for this lab existing.
