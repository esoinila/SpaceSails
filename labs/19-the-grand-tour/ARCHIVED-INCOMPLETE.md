# Lab 19 — The Grand Tour — ARCHIVED, INCOMPLETE

**Status:** the probe is finished and runs end to end (exit 0); the lesson **README was never
written**. This directory is preserved on the `archive/lab-19-the-grand-tour-incomplete`
branch only — it is deliberately **not** on `main` and **not** wired into `SpaceSails.slnx`.

## What works

`Probe.cs` builds and runs against `SpaceSails.Core` and produces all four sections with real
numbers (verified 2026-07-08):

- **A — the promise:** direct Earth→Saturn (lesson 15) vs. an Earth→Jupiter leg + gravity
  assist; the ~1.5 km/s launch saving.
- **B — the crank, measured:** flyby aim-offset sweep at Jupiter, with the two-body bound
  (outgoing heliocentric speed must fall within |v_J ± v_inf|) shown holding, and
  impact-grade passes (inside 2 Jupiter radii) correctly discarded as unphysical.
- **C — the itinerary, flown with TCMs:** a departure-day × leg-length window scan, a b-plane
  aim sweep flown on the actual approach, and a launch + TCM-1 + TCM-2 budget vs. the direct
  Hohmann — including the honest "you refund the gift as a larger braking bill when you stop
  at Saturn" verdict.
- **D — who paid:** the rails-can't-recoil ledger note (energy created from nothing because
  Jupiter is on rails; cf. lesson 9's true n-body).

## To revive it

Write `README.md` in this directory from the probe's output (follow the house style of lessons
14–18: motivation, standard-textbook take, what the game simplifies away, the numerical
experiment with real printed output, "break it yourself"), then wire `Lab19.csproj` into
`SpaceSails.slnx` and add the lesson to the `labs/README.md` run-list and ladder — exactly as
Lab 18 was wired in commit history. Every number in the README must come from an actual run.

## Why it stopped

The Fable-model session that authored lessons 14–19 reached the end of its included-token
window mid-way through Lab 19's write-up, after the probe was complete but before the README.
