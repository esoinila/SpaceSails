# LinkedIn post — draft (2026-08-03, post-holiday)

*Status: DRAFT for Erno to edit and post. Not published. Numbers verified against the repo on
2026-08-03 (they match the manuscript v0.4). Voice: first person, Erno.*

---

A millennium-era party game of mine just came back to life — as an experiment in what one person
plus an AI team can ship in a summer holiday.

**SpaceSails** is a hard-ish sci-fi sailing game in the browser: real orbits, a ship with
compartments, moons you land on, and things under the ice that are never explained to you.

The numbers from 24 active days:
- **424 pull requests** merged (≈18/day), every one CI-gated
- **~2,900 unit tests**, each regression guard *proven to fail* on the broken code before it ships
- **41 physics "labs"** — runnable lessons where the game's own engine teaches orbital mechanics
- **140+ gen-AI painted story canvases**, wired behind a degradation law so a missing file never breaks the game
- Two long story arcs, a detective layer, secret labs, and an 85-metre monolith on Phobos

How it worked: I was the product owner and playtester — streaming design rulings from the couch,
sometimes from the gym. Claude (Anthropic's Fable 5) ran as head coder, orchestrating teams of
Opus agents that wrote the code, painted the art, filed the issues, and merged their own green PRs
while I slept or worked. The discipline that made it safe: CI as the only arbiter, one source of
truth per fact, and a house rule that *a scene nobody can reach on demand is a scene that ships
broken* — so every story beat has a cheat URL and a test.

The most surprising lesson: the expensive bugs were never visible to reasoning or to tests — and
were obvious on sight. So we made *playing* a first-class QA method and wrote the manuscript about
it.

🎮 Play it (free, in the browser): https://esoinila.github.io/SpaceSails-play
📦 Source (MIT): https://github.com/esoinila/SpaceSails
📄 The write-up ("SpaceSails: Secretly a Classroom") lives in the repo: docs/paper/

Origin story: the design is descended from a party game I ran at the turn of the millennium.
Some ideas just need 25 years and a very patient co-pilot.

#AI #GameDev #ClaudeCode #IndieDev #OrbitalMechanics

---

*Editing notes (delete before posting):*
- *Swap "hard-ish sci-fi" for your own phrase if it grates.*
- *The gym line is true (the QA handoff records it) — keep or cut.*
- *If you post after more merges, refresh the PR/test counts from `docs/paper/spacesails-paper.tex` §numbers.*
- *The post image is `docs/linkedin-post-image.jpg` (owner-approved 2026-08-03): a 2×2 of the zoomed Saturn skim (live), the K-77 sentry deployed on Miranda (live), the monolith (painted), and the bar with Mars in the window (live). Three of four quadrants are real gameplay captured during an actual run — the sentry line and the death that preceded it were earned, not staged.*
