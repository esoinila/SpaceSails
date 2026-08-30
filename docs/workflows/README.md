# The workflow shelf

Multi-agent orchestration **as code** — the factory half of this project's tooling. A workflow is a
deterministic script (loops, fan-out, phases) that spawns subagents to do the legwork, with the lead
agent sitting only at the two judgment points: the design going in and the verdicts coming out.
Skills capture *procedures a person follows*; workflows capture *pipelines a script drives*.

Owner's direction (2026-08-30): *"utilize cheaper agents for routine stuff and automate the things in
the development cycle that can be automated with code… then focus the big brain agent resources more
on the big decisions and plot lines."*

## How to run one

- The runnable scripts live in **`.claude/workflows/*.js`** — invoke by name
  (`Workflow({name: "boot-every-scene", args: {...}})`) or by path
  (`Workflow({scriptPath: ".claude/workflows/boot-every-scene.js", args: {...}})`).
- Each script has a matching markdown **here in `docs/workflows/`** — what it does, when to run it,
  its arguments, cost profile, and preconditions. Read the markdown before running; keep both files
  in step when editing either.

## The house rules for authoring one

1. **Cheap models look, stronger models refute, the lead judges.** Routine fan-out stages run on
   Sonnet; adversarial verify stages on Opus; design and synthesis stay with the lead. A finding
   survives only if a skeptic reproduces it — plausible-but-unverified is not a result.
2. **The oracle is a document the repo already maintains** (the testing guide's "what a tester should
   see", a spec, a manifest) — never the prompt author's memory of it.
3. **No silent caps**: if a sweep bounds coverage (top-N, sampling), it `log()`s what was dropped.
4. **Pipeline over barrier** unless a stage genuinely needs all prior results at once.
5. Workflows are read-only reporters by default. One that *edits* must follow the lane rules
   (worktree isolation, PR, CI as the only merge gate) like any crew.

## The shelf

| Workflow | One line | Run it when |
| --- | --- | --- |
| [boot-every-scene](boot-every-scene.md) | Boot the documented dev-start scenes headless, judge each against the testing guide, adversarially verify anomalies | After every publish, or after a big merge batch |

Related skills (procedures, not pipelines): `.claude/skills/local-playtest` (the dev loop's serve +
shared tab) and `.claude/skills/release` (base → main → live site).
