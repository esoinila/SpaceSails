# Lab 43 — What is actually ON a landing site?

**Question (owner, playtesting Miranda 2026-07-31):** *"the map is kind of boring... no door or
enclosed places"* — and *"I have never seen the landing site area expand yet... we should test it."*

Both are measurable from Core alone. This probe answers them without a browser, which matters
because the descent will not render in an automated tab (rAF is throttled when the tab is treated
as hidden, so `?land=1` hangs mid-descent).

## Run

```
dotnet run --project labs/43-miranda-sites/Lab43.csproj -c Release
```

## What it found (2026-07-31, main @ 1b59843)

Miranda offers **3** sites, into a 78 × 64 du field:

| site | name | scheme | wall segments | % of field |
|---|---|---|---|---|
| 0 | The Wild Plain | THE FALSE-SLAB MAZE (was THE MONOLITH MAZE until #649) | **12** | 12% |
| 1 | The Shadowed Rille | THE DEEP RUINS | 20 | 23% |
| 2 | The Ridge Camp | THE DEEP RUINS | 25 | 21% |

The canon ground — the authored maze, the one carrying the story — is the **thinnest** ground on
the moon. Every seeded site beats it.

### The ground can never grow, on any moon

| body | sites | secret lab (1-in-40) | expedition regions | can it expand? |
|---|---|---|---|---|
| Miranda | 3 | no | no | **NEVER** |
| Luna | 4 | no | no | **NEVER** |
| Phobos | 4 | no | no | **NEVER** |
| Europa | 2 | no | no | **NEVER** |
| Titan | 2 | no | no | **NEVER** |

`DeckPlan.AppendRegion` — *"the world grows, nobody teleports"* — is reachable on **two of the
three `expedition-site-*` rocks** and nowhere else in the solar system. That is the whole answer to
"I have never seen it expand": on every real moon, it cannot happen.

### Most lines on a moon are drawn as spaceship

The probe also counts `IsHull` segments, which draw in bright, thick pressure-hull ink. On The
Ridge Camp, **16 of 25** generated segments are hull — before counting the field's own rim. That is
the measurement behind #563's *"it seems artificial on a Moon"*.

## Why a lab and not a test

A test pins a law. This pins *a state of affairs we intend to change* — so it belongs here, where
it can be re-run after the #563 work to show the numbers moved, rather than in CI where it would
have to be edited every time the ground improves.

See #563.
