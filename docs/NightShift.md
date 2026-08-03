# The night shift

*How SpaceSails gets built while the owner is asleep. Written 2026-08-02.*

Companion to [`QAHandoff-StoryArcs.md`](QAHandoff-StoryArcs.md) — that one says **what to go and test**, this
one says **how to run unattended for eight hours without needing anybody.**

Owner's brief, verbatim:

> *"I will be coding all day with AI (but on work licenses) so I will need more AI driven management of coding
> at night on home license to really take use of my max. Maybe the Fable inspecting Opuses coding will do
> that. So I won't feel like I am leaving tokens unused. Tokens are my PRECIOUS... I want to convert them into
> cool features."*

So the measure of a good night is **merged features and sharpened decisions**, not activity. A night that
burns the budget producing six PRs the owner has to unpick in the morning is a bad night.

---

## 1 · The shape of the shift

**Fable plans and inspects. Opus subagents do all the legwork.** One task per subagent, one worktree each,
branched from `origin/our-own-ship-has-compartments`. Fable does not write implementation code — it reads
diffs, checks the laws below, and decides what merges.

```
git worktree add "<scratchpad>/wt-<issue>" -b <branch> origin/our-own-ship-has-compartments
```

**The base branch is `our-own-ship-has-compartments`, NOT `main`.** CI (`build-and-test`, `ui-gate`) runs on
every PR — the workflow has a bare `pull_request:` trigger with no branch filter.

### Never touch these

- **`D:\repo12\spaceSails`** — the owner's checkout, with a dev server on 5073. Work in worktrees only.
- **Files another session has modified.** `git status` before committing; commit **by explicit path**, never
  `git add -A`, if anything foreign is present.

### The stale-checkout trap

PRs squash-merge, so a worktree branched an hour ago is already behind. **`git fetch` and rebase before every
PR, and again if it sits.** Three sessions running lost time to this. A rebase can also silently defeat a
guard by reintroducing the fixed behaviour from the other side — **re-verify your guards still go RED after
rebasing.**

**Do not let a branch chase a moving base.** On 2026-08-02 one PR was rebased onto the head, then spent so
long on a local suite that the base moved twice more underneath it; it never pushed at all. Push the rebase
*first*, let CI start, and run your local checks while it does. A branch that is rebased-but-unpushed is
invisible to everyone and helps nobody.

And when a rebase reports a conflict, **read what the two sides were each doing before resolving.** The worst
conflict that night was two PRs editing `BuildBeacons` — and they turned out to touch different branches of
it (one the surface wash, one the underground one), so the correct resolution kept *both* and the clean
rebase confirmed it. Git flags textual adjacency; only you can see intent.

---

## 2 · Merge authority

Standing order from the owner: **both checks green ⇒ squash-merge, no need to ask.**

Ask (i.e. leave the PR open with a comment) only when:

- it is a **gameplay-feel judgement call** — how something reads, how hard something should be, what a card
  says about the world;
- it touches **canon** (see §3);
- it would **change something the owner has already ruled on** — check `docs/features/*.md`, the issue, and
  the memory index before overriding anything;
- the fix works but you are not certain it is the *right* fix. Say so plainly rather than merging and hoping.

Merging is cheap to do and expensive to undo. When genuinely torn: open the PR, write the two options and what
each costs, and move to the next task. A sharpened decision waiting in the morning is worth more than a
coin-flip merged at 3am.

---

## 3 · The laws that do not bend

1. **Canon.** Nothing in the game ever explains the Old Ones. A crew member may speculate; the game may never
   confirm. This holds hardest in the Hive, which is the most tempting place to break it.
2. **Inference horror.** The game does not announce. `SECURITY ALERTED` as a banner is the wrong shape; a lift
   that stops answering is the right one.
3. **Prove a guard can fail.** Revert the fix, watch the test go RED, restore. Every time. No exceptions.
4. **A green test that asserts nothing** — the fifth named bug class. Use `SurfaceLayout.DefaultField`, never a
   typed-in envelope. Measure a threshold against what the generator can actually produce. See
   [`features/the-landing-site.md`](features/the-landing-site.md#why-this-document-exists).
5. **One source of truth.** Two places computing one fact is the bug, even while they agree.
6. **Core stays pure and deterministic** — seeded off `DiceRule.Seed`/`DiceRule.Roll`. No `DateTime`, no
   `Random`.
7. **Full suite before every PR.** `dotnet test -c Debug` from the repo root, ~25 min, currently ~2550 Core +
   ~30 client. A PR that says "tests pass" without the numbers has not run them.

   **…but CI is the arbiter, and on a busy night the local number is not trustworthy.** This is not a
   theory — on 2026-08-02 three workers each went looking for a full local Core count and *not one of them
   got a reliable answer.* The same code aborted at **2533, then 2531, 2512 and 2484** on successive runs,
   purely from testhost/MSBuild lock contention between concurrent sessions. With several workers running at
   once, that is the **normal** overnight condition, not an anomaly.

   So when the box is busy:
   - run the **client** suite (fast, stable) and **targeted Core over every file you touched**;
   - let CI's `build-and-test` — the whole solution, clean, on Linux — be the verdict;
   - **say plainly in the PR that you could not get a full local number.** Quoting a count you do not trust
     is worse than reporting the gap, because the next person reads it as evidence.
   - `Stop-Process -Name testhost -Force -ErrorAction SilentlyContinue` clears a jam. If a run has been
     thrashing for twenty minutes, kill it and push — getting the PR in front of CI beats another hour of
     local flakiness.
8. **Comment style.** Heavy `// #NNN · TITLE` blocks that quote the owner verbatim and explain **why**, not
   what. This codebase's comments are its design record; match the neighbours.

---

## 4 · The queue

Work top-down. Anything blocked, skip and say why.

### Tier 1 — build these

| # | what | notes |
| --- | --- | --- |
| #615 | keep / leave on a find | LEAVE must not destroy it — the room remembers (#573). KEEP with a full satchel is where the 12-slot cap finally means something |
| #601 | the funding trail, the grant office, the wheat-grant joke | the Hive's running joke *and* its best cover. Owner has given a lot of material: 10 applications per grant, corridors filled with office supplies before the cut-off |
| #625 | the tracker washes the lift head's RAW spot | one-line fix, all the risk is in the guard. Two-sources shape hidden by a tolerance |
| #599 | 0.5 du notch either side of the lift alcove | cosmetic, small, safe |

### Tier 2 — build after #618 is ruled on

| # | what | blocked because |
| --- | --- | --- |
| #602 | locked floors + the numpad | its payoff is "security arrives", which does not exist |
| #605 | badges and camouflage | a disguise needs somebody to fool |

### Tier 3 — QA sweeps

Run the [QA handoff](QAHandoff-StoryArcs.md) coverage map, hardest-neglected first: boardable derelicts and
sectioned hulls (#488, #531, #533, #537), then the black-ops sweep (#538, #535), Q-ship tells (#534),
scuttling (#525), the instrument panels (#524, #523).

Each finding becomes a PR with a proven guard, or an issue written as *what to decide*.

### Decisions — sharpen, do not build

**#618** (who is guarding the Hive — blocks Tier 2), **#619** (the refuge that failed), **#620** (admire and
discuss), **#610** (shooting a door open without it becoming a skeleton key).

If a night's work surfaces the answer to one of these, **write the answer into the issue with the evidence.**
That is worth more than a feature.

---

## 5 · What to leave in the morning

One comment on the newest merged PR, or a fresh issue, containing:

- what merged, with PR numbers
- what is open and why it is open
- **anything found that was not looked for** — this is the most valuable line of the report, every time
- what you would do next

Findings need reproductions. **A finding without a reproduction is a rumour** — the URL, what you expected,
what the sim did, and what the screen said.

---

## 6 · Environment notes that save hours

- `dotnet run` (**not** `watch`) — kill and restart the process per build.
- File-lock jams: `Stop-Process -Name testhost -Force -ErrorAction SilentlyContinue`.
- An MCP-driven browser tab is `document.hidden` — rAF throttled, timers clamped. `?land=1` hangs mid-descent
  and any timing measured there is worthless. You cannot drive the game that way at night; reason against the
  code and build targeted harnesses instead.
- `jq` is not on PATH in the Bash tool; `gh --jq` works.
- grok is the image source and **images only** — run it from PowerShell, from the scratchpad, never pointed at
  a checkout. Recipe is in the memory index.
