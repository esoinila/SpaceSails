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
