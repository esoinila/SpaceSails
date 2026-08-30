---
name: release
description: Publish a SpaceSails release to GitHub Pages - merge the working base branch into main with a narrative publish commit and see the deploy through to the live site. Use when the owner asks to publish, release, ship to Pages, cut a release, or when an owner-approved standing publish order covers the batch. Triggers on - publish, release, ship it, push to pages, new version live.
---

# Release — base to main to the live site

The game ships from **main**, but no work happens there: lane PRs squash-merge into the working base branch (`our-own-ship-has-compartments`), and main is the release ledger — content merges from the base plus one narrative "Publish …" commit per release. Never commit content directly to main.

## Preconditions

- Every PR meant for the release is merged to the base and **CI is green on the base tip**.
- The owner approved this publish, or an owner-approved **standing** publish order covers the batch. Cite whichever applies in the merge message.

## Procedure

1. `git fetch origin main our-own-ship-has-compartments`
2. **Safety check — main must hold no unique content:**
   `git rev-list origin/our-own-ship-has-compartments..origin/main --no-merges` must print nothing (main may only be ahead by past "Publish …" merge commits). If anything prints, STOP and reconcile before merging — something landed on main that the base never got.
3. **Publish merge, in a throwaway worktree** (never the shared checkout — its branch belongs to the session using it):
   ```
   git worktree add <scratchpad>/wt-publish origin/main
   cd <scratchpad>/wt-publish
   git merge --no-ff origin/our-own-ship-has-compartments -m "Publish <narrative>: <what this release carries, in the house register> (owner-approved[, standing], <date>)"
   git push origin HEAD:main
   ```
   The message is the release's one line of history — name what actually shipped, the way the merged PR titles do; don't write "merge base into main".
4. Remove the worktree.
5. **The deploy is `pages.yml`**, triggered on push to main: it builds the client and force-pushes it to the public build-only repo **esoinila/SpaceSails-play**, whose main branch serves GitHub Pages. Watch it:
   `gh run list --workflow pages.yml --limit 1` → `gh run watch <id> --exit-status` (background it).
6. **If Pages hangs after a green run** (build repo updated but the site stale), kick it:
   `gh api -X POST repos/esoinila/SpaceSails-play/pages/builds`
7. **Verify the live site**: https://esoinila.github.io/SpaceSails-play — confirm the release's own change is visible (open a feature this release added; the WASM assets are fingerprinted, so a hard reload shows the truth). A green Action is not a verified release; the pixels are.
8. Report what shipped — to the owner, and on the issue(s) the release closes out if one is the story of the batch.

## Notes

- Base branch: `our-own-ship-has-compartments`. Main: release ledger only.
- Lane PRs: squash-merge to base on green CI (standing order); this skill starts after that.
- The Pages deploy is for players — the development loop uses the `local-playtest` skill instead; don't wait on Pages to test.
