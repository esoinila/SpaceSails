---
name: local-playtest
description: Serve SpaceSails locally and playtest it in a shared Chrome tab with the owner. Use whenever verifying UI or gameplay changes, reproducing an owner-reported bug, or running a live playtest — instead of waiting for the GitHub Pages deploy, which is far too slow for a development loop. Triggers on - playtest, test locally, launch the game, serve the client, look at the UI, debug what the owner sees.
---

# Local playtest — serve it yourself, share the tab

The GH Pages deploy is for players, not for the development loop. For testing, host locally and put the game in a Chrome tab that the owner and Claude **share**: the owner plays in the very tab Claude drives, so any bug the owner finds can be read off the pixels immediately.

## 1. Serve locally

- **Serve from your lane's worktree, never the shared checkout** (`D:\repo12\spaceSails` may be in use by another session; worktrees live under `D:/repo12/wt/<name>`).
- **`dotnet run`, NOT `dotnet watch`** — watch has burned us. There is no hot reload in this loop: **kill and restart the server after every build**.
- Pick **your own port** (another lane may be serving too). The client's default is 5073; pass yours explicitly:

  ```
  dotnet run --project src/SpaceSails.Client -c Release --urls http://localhost:5190
  ```

  Run it in the background so the session stays free. `-c Release` matters: **Debug WASM is ~100× slower** and makes the game feel broken when it isn't.

## 2. Launch the shared Chrome tab

Use the `claude-in-chrome` tools (load them via ToolSearch in ONE batched call if deferred):

1. `tabs_context_mcp` first — see what's open.
2. `tabs_create_mcp` with the local URL, e.g. `http://localhost:5190/map?scenario=sol` — or better, a **dev-cheat URL** that boots the exact scene under test (`docs/testing-guide.md` is the cheat catalog; the front door's ⚙ DEV START SITES list mirrors many of them).
3. Tell the owner the tab is up. **The owner plays in this same tab.**

## 3. The debugging contract

- On any owner report of "see this / look here / now" — **screenshot FIRST, then read the bug off the pixels**. Never navigate, reload, or close the tab first: reloading destroys the state being reported.
- Console: `read_console_messages` with a `pattern` filter. Network: `read_network_requests`.
- After a fix: rebuild, **kill + restart** the server, then reload the tab — and say so, since the reload resets the owner's game state.

## The owner's tab is the owner's session — full stop

- **Never point the owner's tab at anything else** — not a scratch page, not a dev artifact, not a different port. Navigating it away ends the excursion the owner was in (excursions are not persisted) and destroys whatever was being reported. Burned exactly this way 2026-08-01.
- To check your **own** work, open a **fresh tab** with `tabs_create_mcp` and drive that one. Only take over the owner's tab after its state has been read.
- **Never `dotnet build` against the checkout that is serving the tab.** The build rewrites the fingerprinted `_framework` assets under the running server and the tab dies on the next hard reload ("Failed to fetch dynamically imported module: dotnet.<hash>.js"). Build in a different worktree — or stop the server, build, and restart as ONE step, then tell the owner to hard-reload.

## Caveats (learned the hard way)

- An MCP-driven tab that is not focused is `document.hidden`: rAF throttled, timers clamped — **performance numbers from it are worthless**. Judge feel only when the owner has the tab focused.
- **Don't run test suites or builds while the owner is playtesting** — the CPU contention shows up as jank in the game and gets reported as a bug.
- A hidden tab suspends rAF entirely: if the sim looks frozen, check tab focus before checking the code.
- When done, kill the background server; leave the tab to the owner.
