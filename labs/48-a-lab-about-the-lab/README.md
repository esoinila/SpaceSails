# Lab 48 — A lab about the lab

> *"The underground layer checking in CI might be a case for lab in itself (lab about lab)"* 😄
> — owner, 2026-08-01

**Question:** the A\* audit can tell you **that** a room in the Hive is sealed. Can anything tell you
**why**?

## Why this exists

[#587](https://github.com/esoinila/SpaceSails/issues/587) was a handful of rooms that were drawn, offered
`🔦 SEARCH THE ROOM`, and could not be entered — on 35 of the 94 floors under the solar system's
clandestine sites. `YouCanWalkTheHiveTests` found it and printed this:

```
luna B1: 3 of 11 places cannot be reached: (82, -118), (104, -118), (34, -148)
```

Which is the truth, and is almost no help. A coordinate does not tell you which wall put it there. The
evening that followed was one build at a time: widen the doorway (no change), add a claim ledger (no
change), suspect the room placer (wrong), suspect grid sampling (wrong). The actual cause — a single wall
lying across two mouths on the spine's top face — is the sort of thing the eye finds in a quarter of a
second **if there is something to look at**, and the game cannot be screenshotted from an automated browser
tab (rAF is throttled when the tab counts as hidden, so the WASM boot never completes).

So: something to look at.

## Run

```bash
# every floor of every clandestine site — the CI-friendly table
dotnet run --project labs/48-a-lab-about-the-lab/Lab48.csproj -c Release

# one body, every floor, with a picture per floor into labviz/
dotnet run --project labs/48-a-lab-about-the-lab/Lab48.csproj -c Release -- --body luna --svg

# one floor. --floor takes a DEPTH, the same way the game's own ?secretlab=1&land=1&floor=N cheat does
dotnet run --project labs/48-a-lab-about-the-lab/Lab48.csproj -c Release -- --body luna --floor 4 --svg
```

| flag | what it does |
| --- | --- |
| `--body <id>` | one body instead of all ten |
| `--floor <n>` | one floor, as a depth (`4` = B4), clamped to the site's real bottom |
| `--all-floors` | every floor (the default when `--floor` is absent) |
| `--svg [dir]` | write a picture per floor; `labviz/` unless a directory is given |

It **exits 1** if any floor has something the captain cannot reach, so it is runnable from a shell script.

## Reading the picture

Pictures are written to `labviz/`, which is gitignored — run the lab and open one.

| ink | what it is |
| --- | --- |
| **dark teal wash** | everywhere the captain can actually stand, flooded from where the lift doors open |
| **bright blue bars** on the spine | every mouth the generator *cut* — one per rib, plus the lift alcove |
| **pale hull lines** | the walls, exactly as `HiveInterior` draws them |
| **violet** | a doorway that opens |
| **amber** | a door that never opens, with its sign |
| **teal dot** | a room you can walk into and search |
| **red ring with a cross** | a room that is drawn, offers a console, and **cannot be entered** |
| **gold dot** | where the lift doors put you |

The whole lab is in one visual rule:

> **A bright blue mouth with a wall lying on top of it, and an unwashed island behind it, is the bug.**

An open mouth has wash running through it. A sealed one has a hull line straight across it, and everything
that mouth served is a dark island with red crosses in it. On the pre-fix generator, luna B1 renders with
the entire right-hand rib unwashed — **0** wash runs inside that corridor against **88** after the fix —
and the lift alcove likewise, **0** against **8**. The caption goes red and reads
`8/10 rooms reachable · lift SEALED`.

Note that a **locked** room is also unwashed, and that is correct, not a bug: it has an amber door and no
teal dot. Only the red crosses are defects.

## What it reads, and what it must never do

Every number and every line comes from the shipping objects:

| drawn from | what |
| --- | --- |
| `UndergroundComplex.Build` | walls, doorways, locked doors, room centres, and `Ribs` (added by #587 so a mouth can be *named* from outside the generator) |
| `UndergroundComplex.ShaftAt` | the lift |
| `HiveInterior.FloorDeck` | the deck — and therefore `DeckPlan.CollisionField` |
| `HiveInterior.SpawnOn` | where the flood starts |
| `DeckReachability.Reachable` | the wash |
| `DeckReachability.CanReach` | the per-room verdict — the same question `YouCanWalkTheHiveTests` asks |
| `MoonSurface.ExpeditionField` | the field envelope |

**It does not own a copy of the geometry.** A lab that re-implements a generator so it can be pictured is
drawing a building the game does not have, and an invisible bound is the worst possible place for that —
this project has paid for it twice already (`docs/features/the-landing-site.md`, *"why this document
exists"*). If a picture from this lab and a screenshot from the game ever disagree, one of them is a bug
and the lab is not allowed to be the reason.

It references the **client**, not just Core, and that is load-bearing. #587 was a mismatch between the
walls Core *draws* and the collision field the boots *walk*; a picture of Core's geometry alone would have
shown a perfectly good building. Same reason `tests/SpaceSails.Client.Tests` exists.

The wash and the verdict come from one lattice inside `DeckReachability`, so a point the flood colours is
a point the walk can reach, by construction rather than by comment.

## What it found

Run at the merge of #587 (2026-08-01), all ten bodies:

```
94 floor(s) walked · 0 with something the captain cannot reach.
```

(Ninety-four, not the "~130" #587 was filed with — the ten seeded sites are 94 floors deep between them.
The lab counts what it walks.)

Run against the generator one commit earlier, the same sweep reports 35 floors, every one of them the two
room columns flanking the right-most rib plus the lift alcove — which is precisely the list #587 was filed
with, now with a picture attached to each one.

## Lesson

*An audit that names a coordinate has told you where to start looking. An audit that draws the floor has
told you what to fix.* The A\* flood was the right guard and it was not enough on its own: it detected #587
the day the Hive shipped and cost an evening anyway. Detection and diagnosis are two different tools, and
a project that has one of them will keep paying for the other.
