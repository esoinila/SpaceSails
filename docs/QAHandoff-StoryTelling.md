# QA handoff — are we telling the story nicely?

*Written 2026-08-02 evening for the Fable-led story run. The owner's holiday ends tomorrow; from
2026-08-03 he works ~8 h/day and the AI team runs the queue. His brief, verbatim in spirit: go through
the main plot arc scenes with cheat starts, see if we are telling the story nicely, gen-AI pics where
needed, make new cheat starts so nobody has to play their way anywhere.*

This is the **story lens** companion to [`QAHandoff-StoryArcs.md`](QAHandoff-StoryArcs.md), which is the
**bug lens**. Everything that document says about worktrees, the five bug classes, proven-RED guards,
canon, and the two environment traps applies here unchanged. **Read it first.**

---

## 0 · Owner rulings for this run (2026-08-02)

1. **Art merges green unseen.** Gen-AI image PRs follow the standing order (both checks green ⇒
   squash-merge); the owner reviews in-game later and we redo what he dislikes.
2. **Prose and cards may be changed freely.** Reword narration, add reveal cards, fix
   sentence-vs-sim lies without asking. Anything **structural** — a new scene, arc order, a mechanic —
   becomes an **issue**, not a build. (Exception: a lane the owner already designed — Core merged,
   feature doc written, art painted — is sanctioned build work, not a new scene. The archive node is
   the type specimen.)
3. **Autonomous loop authorized.** Fable self-paces, merges green PRs, files sharp issues, and keeps
   the queue moving while the owner is away.

## 1 · What "telling it nicely" means — the story-quality criteria

Judge every beat against these, in order:

1. **The beat exists on screen.** A truth that lives only in Core constants or a writers' bible is not
   being told. (The reveal shock hooks 40/30/64 are the standing example: designed, never consumed.)
2. **You can reach it on demand.** *"A scene nobody can reach on demand is a scene that ships
   broken"* — the boot's own rule, written beside the `?query=` readers in `Map.Sim.World.Query.cs`
   (it was `Map.Sim.cs` until #870 split the boot out). An arc beat with no cheat start is a finding;
   file it or fix it.
3. **The fiction arrives one beat early** (#380). An event must introduce its fiction before it needs
   it. A tell you can only read after the trap is not a tell (#534's law, generalized).
4. **The sentence matches the sim** (bug class 3). Read every line the game prints against what the
   sim did. A card described in prose must be in the pocket.
5. **Inference horror, not announcement.** `SECURITY ALERTED` as a banner is the wrong shape; a lift
   that stops answering is the right one. The game never confirms the Old Ones. A crew member may
   speculate; a sensor may not.
6. **The moment has its picture** (#528). Big beats get a card or plate; the manifests are the backlog
   format; the code ships first and the JPG drops in behind (`onerror`-hide law).
7. **Cadence is honest.** Once-ever beats fire once ever; every-time cards don't spam; a deferred card
   still arrives. (The `StoryBeats` cadence law exists — on `main` only, see §5.)

## 2 · The arc queue — what to walk, with the URLs

Dev server per worker: `dotnet run --project src/SpaceSails.Client --urls http://localhost:<OWN PORT>`
in your **own worktree**. The full verified cheat table: `QAHandoff-StoryArcs.md` §2 and
`testing-guide.md` Appendix A. MCP browser tabs are `document.hidden` — `?land=1` hangs there; judge by
code + targeted harness + prose reading, and screenshot only what renders without timing.

| # | Arc / beat chain | Quick starts | Known gaps (from the 2026-08-02 code scout) |
|---|---|---|---|
| 1 | **The archive node + the five visions** (arc 2's only in-person scene) | none — **build the client lane** per `features/the-archive-node.md`; Core `ArchiveNode.cs:236-285`; art `vision-*.jpg` all painted | zero client references; no field in the walk loop, no card, no handle. Add `?archive=1` style cheat in the same PR. |
| 2 | **PROJEKTI KAAMOS** (#411) | `?kaamos=N` (1..5), `?kaamos=all`; lab log via `?secretlab=1&land=1`; plaque via `?dock=ringside-exchange` | cold-pod probe find and holder-at-bar have **no seat cheat** (fragments 2 & 4 only reachable as pre-assembled intel); the **Enceladus climax is hook-only** — sharpen as an issue, do not build |
| 3 | **NEBULA MUTUAL + THE CONVERGENCE** (#422, #553) | `?nebula=N`, `?nebula=all`, `?converge=1` | adjuster-at-bar has no seat cheat; rebirth-glitch beat organically needs dying (see #4); reveal shock hooks unconsumed |
| 4 | **Death, rebirth, the clinic** (the arc the player lives) | none direct — `?floor=2&air=10` (underground suffocation), `?reevers=8` (overdraw), `?collectors=20` (surface repo boat) | **no `?death=` cheat exists**; the death card's 4-stage grammar, Bolivia ladder and BUSTED prose have never had a story pass. Add the cheat, then walk every cause × place. |
| 5 | **Wreck stories** (#533, #426) | `?wreck=<cause>&land=1` for all ten causes | each cause has its own hull + art; read all ten evidence-station narratives against their causes; scuttle epilogue (`?wreck=1` then scuttle) |
| 6 | **The Hive plot items + reveal cards** (#528, #614) | `?secretlab=1|deep&land=1`, `?floor=N`, `?air=N` | most-played, least likely to yield bugs — but #528's audit (which moments lack cards) starts here; the collar/penetrator cards are the house style to match |
| 7 | **Surface story furniture** — monolith, selfies, hoard/burial, outpost | `?dock=the-space-bar&body=phobos&site=0&land=1` (the monolith, #649), `?dock=the-tilt&site=0&land=1`, `?hoard=mine\|rumor\|both`, `?outpost=1`, `?reevers=N` | **the owner's open question, found typed into `FridaySecondPlan.md`: "Does burying the treasure still work?" — answer it here with evidence**; monolith nerve hit + selfie beat check |
| 8 | **Bar / social beats** — stranger-bond, Oracle, Magpie rota, talking drinks | `?bond=1`, `?dock=the-space-bar`, `?simhours=0\|5\|9`, `?backroom=`, `?crack=` | #410 (static bars) is the standing complaint; check the Oracle leaks the rebirth glitch (#428 wants a `?oracle=1` presence cheat — add it) |

Work top-down: the ordering is *story payoff per token*, and #1–#4 are where shipped narrative is
currently unreachable or untold.

## 3 · New cheat starts to add (each one a small PR with a guard)

The rule: boot-time params, parsed by `ReadEveryQueryKey` in `Map.Sim.World.cs` and consumed by the six
`Map.Sim.World.Query*.cs` readers (#870 — it was one loop in `Map.Sim.cs` when this was written), documented
in `testing-guide.md` Appendix A **in the same PR**, and listed in `DevStarts.cs` if a button earns
its place. A cheat is a feature; prove it with a test where the seam allows.

- `?archive=1` — the archive node aboard the seeded wreck, visions unlocked from the first touch.
- `?death=<cause>` (+ optional `&place=underground|derelict|surface`) — stage the death card at any
  cause × place without dying for real. This is the only way the 4-stage card grammar ever gets a
  regression pass.
- `?kaamos=pod` / `?kaamos=holder` — seat the organic finds (the beach-comber cold pod square under
  your feet; the berth-holder at this bar) rather than skipping them.
- `?nebula=adjuster` — seat the adjuster at this bar.
- `?oracle=1` — force the Oracle present and ranting (#428 asked for exactly this).
- `?nerve=N` — set the nerve gauge at boot; without it no sanity beat can be tested on demand.

## 4 · The art lane

- **On this branch zero referenced art files are missing** (82 literal refs, 117 files, checked
  2026-08-02). The art work is therefore **new slots for untold moments** (#528 audit), not
  backfilling.
- Recipe, rules and the *generate-in-scratchpad-then-copy* law: any `art-manifest-*.md` §"how to
  paint" — grok 4.5, PowerShell not Bash, images only, no text/lettering/logos in frame, 16:9 scenes,
  1:1 merch, grimy used-future, painterly, muted.
- Every new slot follows the degradation law: the code ships first, `onerror`-hides, the JPG drops in
  behind. Add the slot to the relevant manifest with the prompt used.
- Art PRs merge green unseen (owner ruling §0).

## 5 · The finding that outranks this document: the fork

`main` and `our-own-ship-has-compartments` have **diverged** (30 vs 45 commits, merge-base
`fce6bd2`). `main` alone has the `StoryBeats.cs` cadence engine, `Map.StoryCards.cs`, and the 11
painted moment canvases (arc-news, crew-deputation, fire-aboard, the three arrival tubes…); this
branch alone has the entire Hive/surface lane. **The arc-news beat — an arc landing on the news wire —
is the exact storytelling device arcs 2–3 above are missing, and it is marooned on `main`.**

Reuniting the branches is a decision plus a heavy merge: **owner's call on direction and timing.**
Filed as an issue; until he rules, all PRs keep targeting `our-own-ship-has-compartments` and nobody
cherry-picks across the fork on their own authority.

## 6 · Working method (unchanged, restated)

- **Fable orchestrates; Opus workers do the legwork.** One arc per worker, one scratchpad worktree
  each off `origin/our-own-ship-has-compartments`, own dev port. Never touch `D:\repo12\spaceSails`'s
  working tree.
- `git fetch` + rebase **before** the PR, then re-verify guards go RED after rebasing.
- CI is the arbiter — run targeted tests locally, let CI run the 22-minute suite.
- Findings become either a **PR** (prose/card/cheat fixes, guard proven RED where the seam allows) or
  an **issue** (structural / owner-taste), always with the reproducing URL, what you expected, what
  the sim did, and what the screen said. A finding without a reproduction is a rumour.
- Standing order: both checks green ⇒ squash-merge. Gameplay-feel judgement calls wait for the owner.
