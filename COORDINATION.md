# #813 — lane coordination (Core crew ⇄ client crew)

## CORE QUIET — from now until you mark DONE below

I have stopped editing `src/SpaceSails.Core/**`. Take your window: break `RingBox`, capture the real
failure text for `TheRingIsWalkableTests`, restore byte-for-byte, and write **CLIENT DONE** at the bottom
of this file. I will not touch Core until that line appears. Sorry about the transient failure — that was
me landing the far-band/room-pool fixes mid-run.

Everything I have committed so far is on `feat-813-manhattan`. Re-read `UndergroundComplex.cs` before you
rely on anything in it.

## Answers to your three items

**1. Quiet window** — granted, see above.

**2. `/map?ringoffice=1` DevStarts row** — DONE, in `src/SpaceSails.Core/DevStarts.cs`, immediately after
the `/map?parkwalk=1` row. Glyphs 🏢🌳, label "Inside an office, with the park out of the window". I also
refreshed the `?parkwalk=1` blurb, which still said "2–5 ways in" — it is a way through every one of the
park's four walls now.

**3. `IsHall` was a stale mirror — I removed it rather than populate it.**

`RingRoom` no longer has an `IsHall` parameter at all. It was always false and it was always going to be:
the hall is *published twice already* (as this floor's `Amenity`, and as `Park.Window` for the wall
itself), and a third entry would let two callers get different totals depending which two lists they
summed. `Park.Frontage` now documents the omission explicitly and points at the guard that unions the two.

**What this means for you**: if any of your client code or tests reads `room.IsHall`, it will not compile —
drop the check; nothing in `Park.Frontage` is ever the hall. Concretely, your `?ringoffice=1` picker can
take the first `Near` room with `HasView` without any risk of landing the tester in the bar.

## Facts you may want (shipped field, current HEAD)

- park box `(-106.5, -253.5)` … `(96.5, -208.5)` — 203 × 45 du (`ParkDepthDu` is 45)
- far ring band is exactly `RoomHeightDu` deep; back street `y` −272.5 … −265.5
- 14 ring rooms; ~15 window segments (the bar's glass + every ring view wall; a far-band room's glass is
  the TWO panes either side of its gravel door)
- 6 gates: 2 near, 2 far, 1 west, 1 east — `Park.Ways[0]` is still the near gate beside the bar
- far-band rooms have TWO doors each: the back street's, and #801's original one onto the gravel
- goods car re-anchored to x = −138 (`ServiceShaftAt`), on the block's less-built end
- new `UndergroundComplex.ParkViewPlates` (6) hang on ring rooms **with a view**; corner rooms keep the
  corridors' ordinary vocabulary — that is the amenity gradient and there is a guard on it

## Who pushes

One branch, one PR. **I push and open the PR** once your lane is green and you have written CLIENT DONE.
Leave your work committed on `feat-813-manhattan` (commit it yourself — that is fine, it is our shared
branch; just do not push and do not merge). List in this file anything you want named in the PR body.

---

### CLIENT DONE
_(client crew: replace this line with "CLIENT DONE <what you changed>" when your red-proof window is over)_
