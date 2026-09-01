# boot-every-scene — the owner's QA method, automated

**Script:** [`.claude/workflows/boot-every-scene.js`](../../.claude/workflows/boot-every-scene.js)

The owner's standing method — *"open EVERY scene and check all the parts are in the right place"* —
has caught nearly every expensive bug in this project, and almost none of them were visible to
reasoning or to Core tests. This workflow runs that method as a fan-out after every release, so the
looking happens even when nobody has an evening for it.

## What it does

Three phases:

1. **Catalog** (one Sonnet agent, low effort): reads `docs/testing-guide.md` and extracts the N most
   substantive dev-start cheat scenes — URL, a condensed copy of the guide's own **"what a tester
   should see"** (that text is the oracle; the workflow author's memory never is), and at most one
   trivial documented interaction. Rows not included are counted out loud (no silent caps).
2. **Look** (one Sonnet agent per scene, fanned out): boots the scene headless against a locally
   served build, waits out the WASM boot (first load 15–40 s — a loading canvas is never judged),
   screenshots, checks the console, and reports anomalies only where the guide implies otherwise:
   `broken` / `wrong` / `cosmetic`. Style opinions and slow first loads are explicitly not findings.
3. **Verify** (one Opus skeptic per non-cosmetic anomaly, pipelined — no barrier): re-boots the same
   scene and tries to **refute** the claim, waiting even longer and performing the documented step.
   An anomaly is `real` only if the skeptic reproduces it with their own eyes. This kills the classic
   false positives: judging mid-boot, throttled frames, a missed interaction.

Returns `{ scenes, confirmed }` — every scene's verdict plus the list of skeptic-confirmed anomalies.
The lead triages `confirmed` into issues in the owner's screenshot style.

## Preconditions

- A local build is being served (see the `local-playtest` skill): worktree at the tip under test,
  `dotnet run -c Release --urls http://localhost:<port>`. The sweep's headless browsers are separate
  clients with their own storage — they do not disturb a playtest tab on the same server.

## Arguments

| arg | default | meaning |
| --- | --- | --- |
| `baseUrl` | `http://localhost:5073` | the served build to sweep |
| `maxScenes` | `10` | scenes per sweep; the catalog prefers newest-feature and richest-expectation rows |

## Cost profile

~1 + N + (anomaly count) agents: catalog on Sonnet/low, lookers on Sonnet, skeptics on Opus. At the
default 10 scenes a clean sweep is ~11 agents; a noisy one a few more. The lead spends tokens only on
the brief and the triage.

## Known limits (v1)

- Look-only plus at most one documented keypress per scene — multi-step flows (a full dig, a
  conversation ladder) are judged by their boot state, not walked end to end.
- The catalog picks N scenes; a full-catalog sweep means raising `maxScenes` and accepting the agent
  count. What was dropped is always logged.
