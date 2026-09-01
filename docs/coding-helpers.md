# Coding helpers — grok & gemini CLIs

This project can offload implementation work to two locally-installed AI coding CLIs, in addition to
the primary Claude Code session. The intent (see the working agreement in
[SpaceSails_plan_detailed.md](SpaceSails_plan_detailed.md) §9): the senior session writes the
per-milestone build sheet and reviews/verifies/commits; a helper does the bulk coding to save tokens.

Both run **headless** (one-shot, non-interactive) and can read/edit files in this repo.

## grok

- **Models:** `grok-composer-2.5-fast` (default, fast coder) and `grok-build` (heavier). List with `grok models`.
- **Headless run:**
  ```sh
  grok -p "implement M3 per docs/m3-spec.md" --permission-mode acceptEdits --disable-web-search
  ```
- **Heavier model:** add `-m grok-build`.
- **Useful flags:** `--check` (self-verification loop), `--worktree [name]` (isolate edits in a git
  worktree for clean review), `--effort {low..max}`, `--best-of-n N`, `--output-format json`.
- **Auth:** `grok login` / check with `grok models`.

## gemini

- **Model:** Gemini 3.5 Pro (default with the installed credits). `gemini 0.49.x`.
- **Headless run:**
  ```sh
  gemini -p "implement M3 per docs/m3-spec.md" --approval-mode auto_edit
  ```
- **Approval modes:** `default` (prompt), `auto_edit` (auto-approve edits), `yolo` (auto-approve all
  tools), `plan` (read-only). `-m <model>` to pick a model, `-o json|stream-json` for structured output.

## Routing (default)

- **grok-build** for integration-heavy milestones (e.g. M5 traffic/planner, M9 multiplayer).
- **grok composer-2.5-fast** for lighter UI/mechanics milestones (e.g. M3 fly-the-ship, M4 plotting).
- **gemini** as an alternative/fallback implementer, or where Gemini is the better fit.

## Rules when using a helper

1. Give it a **written build sheet** (like [m2-spec.md](m2-spec.md)) — don't make it invent architecture.
2. **One implementer per milestone at a time.** Never let two helpers (or a helper + a subagent) edit
   the same milestone's files concurrently — that only creates conflicts.
3. The senior session **reviews the diff, builds, runs tests, verifies behavior, and commits.** Helpers
   do not commit or push.
4. Honor the §9 constraints: determinism is law in `SpaceSails.Core`; UI = Razor + Bootstrap only; JS
   lives only in `renderer.js` (+ future `input.js`).

## House laws for structural work (#870)

Over one weekend in August 2026, twenty-odd crews moved roughly forty thousand lines of this repo into
new containers without changing a single thing the game does — the family split, the size gate, the
seat and the round becoming objects behind a written interface
([architecture.md § Where the code lives](architecture.md#where-the-code-lives--the-families)). These
are the laws that made that safe, written down because the next crew that moves code is going to need
them and none of them are obvious.

**A pure-move lane carries a mechanical purity proof, not a review.** "I only moved it" is a claim, and
a claim in a PR body is worth what the reader's patience is worth. So the lane measures it instead:
the sorted set of member names, the count of `///` doc lines, the count of `// ──` banners, the sha of
the sorted docblock text, and the blunt one — **every non-blank line of the old file still exists,
the same number of times, somewhere in the new ones; and the only lines that appeared are file
headers.** `tests/scripts/lane6pb_purity.py` and `lane6pc_purity.py` are the two that are checked in;
each runs with `--prove-it-can-fail`, which plants a reworded docblock, a deleted banner and a renamed
member on a scratch copy and watches all three go red. A proof that cannot fail is not a proof — this
repo has paid for that lesson more than once.

**A behaviour-bearing split is SNAPSHOT-FIRST.** When a method genuinely has to change shape — a
1,656-line boot becoming twenty-eight named stages, a 1,058-line `Draw` becoming seventeen passes —
the guard is captured **on the old code, committed on its own, and pushed before a line moves.** Then
the split has to reproduce every hash exactly. The four shapes that exist:
`TheBootBuildsTheSameWorldTests` boots the shipping page at 75 dev URLs and diffs every instance field
against a page that never booted; `EveryFrameHashesTheSameTests` records the renderer's draw-call
transcript; `EveryFrameLeavesTheSameFingerprintTests` pins 30 frames (six worlds × five input
sequences); `EveryRoundFingerprintsTheSameTests` walks thirteen cases and 7,100 frames of the patrol.
Each is **shown RED before it is trusted** — swap two passes in the conductor, or make one transition
forget one assignment.

**And the pins live in a ledger, not in source (#1055).** Three of those four keep their numbers in
`tests/SpaceSails.Client.Tests/Ledgers/*.ledger.txt`, one row per (probe, scene), grouped by probe so
two lanes moving two different probes merge without a conflict. **Nobody types a number into one.**
A re-pin is a command that RUNS the measurement and prints the report you paste into the PR:
`SPACESAILS_REPIN=1 dotnet test tests/SpaceSails.Client.Tests -c Release --filter
FullyQualifiedName~ThePinsAreRewrittenOnlyWhenAsked --logger "console;verbosity=detailed"`. CI never
writes: the writer throws without the opt-in, and a test proves it throws. Full instructions are in
[docs/testing-guide.md, Appendix B](testing-guide.md).

**Reals in a fingerprint go to five significant figures, and the Linux runner is the arbiter.** A
boot sweep that was green on Windows reddened all 75 cases on ubuntu, and the diff was one field:
`Math.Sin`/`Cos`/`Atan2`/`Sqrt` disagreeing in the **fourteenth** significant digit. Rounding to twelve
figures reddened all 75 again, because the traffic planners run an *iterative solve* and a last-bit
difference in a seed does not stay last-bit. Measured off that run: 113,436 numbers compared, 447
differ, worst 6.19e-8 relative — the two machines agree to about **7.2 significant figures and no
further.** G6 is the first precision that holds; **G5 was chosen**, because a hash cannot carry a
tolerance and G5's 1e-5 grid is 160× the worst divergence rather than 16×. It cost the guard nothing —
the sweep still separates 59 distinct worlds out of 75 at every precision from G12 down to G2. The
general rule: **a frame transcript through a `float`-only interface is portable by construction; a
WORLD is not.** If your snapshot can carry a `double`, it is platform-dependent until you round it.

**A guard that names a page member BY NAME through reflection is invisible to the compiler.** Two
lanes passed their own CI on the same day and still landed a broken tree: one moved a field onto a new
object, the other's frame ledger read that field by name string, and neither branch contained the
other. If your lane moves a field, **repoint every reflection census in the SAME PR**, and re-pin any
field-census row with a git-diff proof that exactly one line changed. Better still, name as little as
the guard can get away with — the boot sweep names nothing (it diffs a booted page against a virgin
one and takes whatever differs) and survived four lanes landing under it without a line of change.

**Source-shape guards are ratchets, and each has an anti-vacuous half.** The size gate
(`NoSourceFileIsTooLongTests`), the two host-interface member counts (28 and 21), and "no file outside
the family names its fields" are all written as a number or a list that may only come down.
Every one of them also asserts that the world it is stated against can tell pass from fail: the sweep
must find hundreds of files, not zero; the family files must exist at the paths it exempts; the field
names must still be found inside them; the threshold must sit clear of the nearest real case. A
guard whose world is empty, or whose threshold selects everything, is this repo's fifth named bug
class — green and asserting nothing.

**A test that greps source by a needle is re-PATHED, never re-needled.** When a file becomes fifteen
files, the guard that read it reads all fifteen concatenated in ordinal order. Change the path, never
the search string and never the `Assert` — a filename swap that quietly narrows what a guard can see
is a silent weakening, and at least one guard in this repo counts an occurrence whose two halves ended
up in different files. When a rename genuinely forces a needle to change,
`tests/scripts/lane6pb_asserts.py` and `lane6pc_asserts.py` are the accounting: take every
`Assert.`-bearing line of every touched guard, blank the string literals, and hash what is left. Same
hash ⇒ the claim did not move.

**Two mechanics, both learned the hard way.** Create PRs **from Bash with a UTF-8 body file** —
PowerShell mojibakes emoji titles and this project's titles are emoji. And **merge in its own command,
then verify the PR reads `MERGED` before deleting anything** — a cleanup chained onto an unverified
merge is how a branch disappears with work still on it.
