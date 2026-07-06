# Getting the solve off the deck — a plan (and only a plan) for threaded fire control

*Status: PONDER. No code. Written 2026-07-06 as the agreed follow-up to the wolf-aim PR (#94). The owner's framing: "not at all sure how straightforward that is, but we could write a markdown plan about it anyways."*

## The problem, measured

`FireControl.Solve` and the 🔭 window scan run synchronously on the Blazor WASM UI thread. Every Newton iteration is 3 real simulator flights; every scan is ~7 seed probes × ~25 flights plus a full solve. The deck freezes for the duration:

- knife-fight solves: sub-second — fine;
- the 14.7-day wolf-aim scan in live testing: ~15–30 s frozen;
- a 90-day cross-system solve: **2 m 16 s** frozen (measured, long-shots PR #79);
- the historic blind auto-aim escalation once locked the deck ~10 minutes.

We already mitigated by *policy* (auto-aim only signals on windows >20 d; the panel warns "solves grind the whole deck"). This plan is about mitigating by *architecture*.

## The asset we're sitting on

The solver is a **pure, deterministic function of serializable value types**: shooter `ShipState`, max muzzle speed, aim point, `t_hit`, and an ephemeris that rebuilds byte-identically from the scenario JSON. No shared mutable state, no clocks, no randomness — determinism is law in Core, and it makes every option below *possible*. Whatever we pick, the isolation boundary is already drawn.

## The options

### A. Cooperative slicing on the UI thread (no threads at all)

Make the solve **resumable**: a solve session that advances a bounded budget of integrator steps per UI frame (`await Task.Yield()` between slices), feeding each completed `IterationStep` to the existing convergence-trace display as it happens.

- **Pros:** zero deployment risk — works on GitHub Pages exactly as today; trivially cancelable (the scrub button aborts the session); the "CALCULATING FIRING SOLUTION" display becomes *real* (iterations appear live instead of replayed after the freeze); determinism untouched (same arithmetic, same order, just paused between slices).
- **Cons:** total wall time doesn't improve (slightly worse); needs Core surgery — `Solve`'s inner flights (`RunAdaptive`) must yield mid-flight or a single months-long flight is itself a multi-second slice. Sketch: keep the pure `FireControl.Solve` intact (tests and labs depend on it), add a `FireControl.SolveSession` wrapping the same internals with a `bool Advance(int maxIntegratorSteps)` state machine. UI pumps it from `OnTick`.
- **Effort guess:** one PR. The state machine around the Newton loop + restart list is the fiddly part; the physics doesn't change.

### B. Real .NET threads in WASM (`WasmEnableThreads`)

- **Status must be re-verified against current .NET 10 docs before committing** — multithreaded Blazor WebAssembly has been experimental/postponed for several releases and may still not be supported.
- **Hard blocker for us regardless:** WASM threads need `SharedArrayBuffer`, which needs cross-origin isolation (COOP/COEP response headers) — **GitHub Pages cannot set response headers.** The `coi-serviceworker` shim exists but is a deployment hack on the critical path of the whole game.
- Verdict: watch item, not a plan.

### C. A dedicated web worker running a second .NET runtime

Community-proven route (e.g. SpawnDev.BlazorJS.WebWorkers / BlazorWorker-style): spawn a plain dedicated worker, boot a second WASM runtime in it, message-pass JSON. **A plain worker needs no COOP/COEP** — Pages-safe. The worker loads Core, rebuilds the ephemeris from the scenario JSON, runs the *unchanged pure* `Solve`, streams `IterationStep`s back as messages.

- **Pros:** true parallelism — the deck never hiccups; the scan's 7 probe aim-times could even fan out across workers (the porkchop sweep is embarrassingly parallel); Core needs **no changes at all**.
- **Cons:** a dependency + interop layer to own; second runtime download (~tens of MB, but same cached assets) and a seconds-long first-spawn warmup (spawn at world load, not at first SOLVE); keeping the worker's inputs honest (scenario JSON + shooter snapshot + predicted target path must travel in the message — the wolf-aim pursuit path is just a `TrajectorySample[]`, fine); Debug-WASM interpreter is ~100× slow in the worker too (dev-only pain).
- **Effort guess:** two PRs — worker infrastructure + solve relocation.

### D. Solve on a server

Dead on arrival for the main build: single-player ships as **static files on GitHub Pages** and plays offline. Only ever relevant to the MP session host, where the authoritative server could solve for all clients — file under MP revival, not here.

## Recommendation

**Phase 1 = A**, because it ships the felt improvement (a live, cancelable, honest CALCULATING display; a deck that never freezes) with zero deployment risk and no new dependencies. **Phase 2 = C** if cross-system solve latency still annoys after A — and it's the natural home for a parallel window scan. B stays a watch item; D belongs to multiplayer.

A nice property of doing A first: the `SolveSession` boundary (snapshot in, iteration stream out, cancel token) is *exactly* the message contract C needs later. A is not throwaway work on the road to C.

## Determinism and staleness (both phases)

- Solve against an immutable **snapshot** taken at press time (shooter coasted to T+lead, target path, sim time) — never against live mutating state. This is already true today by accident of synchrony; async makes it a stated contract.
- A finished solution may be stale by the time it lands. The doctrine already handles this — `ArmFire` re-solves at the current clock, and `ValiditySeconds` prices delay — but async should surface it: *"solution aged 43 s · keeps ~10 min"* on the verdict line.
- The sim keeps running while solving (that's the point). A moved aim, switched round, or lost target cancels the session — same rules that void an unlocked solution today.

## Open questions for the owner

1. Is a *responsive-but-equally-slow* solve (A) enough of a win on its own, or is raw solve latency the actual complaint?
2. For C later: is a seconds-long worker warmup at world load acceptable? (Alternative: lazy-spawn on first gun-deck visit.)
3. Should the window scan fan out in parallel (C makes 7-probe scans ~7× faster wall-clock) — or is scan latency fine once it stops freezing the deck?
4. Does the parrot get to say something when a long solve finishes while you're on another desk? ("SOLUTION'S READY, CAPTAIN! SQUAWK!") — the async solve makes that moment exist for the first time.
