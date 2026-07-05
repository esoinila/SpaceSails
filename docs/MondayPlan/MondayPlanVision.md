# Monday Plan — The Cover Story (vision seeds)

*Owner + Fable, captured 2026-07-05 while building M29. Fresh-context starting point for the
next session; docs/SundayPlan/ shows how the last two plans ran (vision → PR lanes → chain →
approve). Raw owner quotes distilled.*

## The owner's idea, in his words (distilled)

Set a course somewhere innocent — "on trade route to Mercury from Earth" — to look
non-piratey. With that cover story flying, watch which targets become **conveniently close at
some mid-travel course change**. Plotting a direct intercept reveals intent — a prey that
scans us can see our course closing on theirs — but doing it in **two steps hides the
intention**. The sensors desk is the instrument for this. And the cover story is also a
**comms artifact**: it's what we reply to the hired muscle when hailed — the captain's
standing order quoted back as an alibi.

## What already exists (M29 shipped the seed)

- **En-route passes** on the Sensors desk: every *known* contact (live sensor or telescope
  track) whose predicted coast the current plotted course brushes by, with min distance,
  when, and a 🎯 hand-off to the war room. This is the "who drifts conveniently close along
  my innocent course" half, working today.
- The captain's articles already ARE the declarable cover ("Make for: Mercury orbit",
  "Trade run: Earth → Mercury"); the intercept clock, firing solutions and target dossier
  give the pounce once the two-step is flown.

## The feature set to design

1. **NPC intention inference (the counter-detection)**: ships that can SEE us evaluate our
   course against theirs — a burn that drops our closest approach to them below a threshold
   flips them suspicious (evasive burn, tight-beam call for help, timetable goes dark).
   Symmetric honesty: the same InterceptEstimate we use on them, run by them on us, gated by
   THEIR sensor picture of us (they can only judge what they've observed — our old course, a
   stale fix, the glare). Two-step intercepts beat it exactly when the second burn happens
   outside their detection or reads as plausible for the declared route.
2. **The declared cover story**: a captain's-desk choice (defaults to the standing order)
   that comms quotes when hailed — by hunters, by prey, by havens. Consistency matters: a
   declared Earth→Mercury run whose course fits stays boring; a "trade run" that keeps
   changing course near haulers earns suspicion even without a detected intercept.
3. **Suspicion as a per-ship ledger** (mirrors compliance/aware): unaware → uneasy →
   alarmed, driven by (a) detected course convergence, (b) cover-story inconsistency,
   (c) being pinged/laser-ranged/aware from M27. Alarmed ships burn evasively (breaking
   locks by the model — the M28 cone impulse term already prices this honestly).
4. **Sensors-desk planning aid**: when scrubbing a plotted burn, show for each en-route pass
   whether the target would SEE the burn (in their detection range at burn time) — the
   "does this course change give us away?" readout. The two-step play, made explicit.
5. **The parrot knows**: "they're watching the wake, captain…" when suspicion is detected.
6. **The decoy drone** (owner: "maybe one future option 😀"): the deluxe upgrade over the
   fake beacon — a physical drone that CONTINUES our declared course transmitting our
   transponder signal while the real hull goes hunting. Unlike the signal-side ghost
   (M29's FAKE mode, busted by any observer whose optics see the real us), the decoy IS a
   real flying object: an optical check *confirms* the lie unless the observer gets close
   enough to resolve that the hull is a drone. Costs: buy the drone (dock upgrade tier),
   one-shot expenditure, and losing it adrift is evidence with our name on it. Owner's
   *Serenity* (the Joss Whedon movie) variant: decoys as CHAFF — minimal-capability fakes that "look like a ship when
   no one really looks", scattered as false targets to saturate a hunting ship's picture;
   the hunter must spend telescope time (or a close pass) per contact to tell hulls from
   ghosts — our own track-quality mechanics, turned against the wolf.

## The target taxonomy (owner, late addition)

Three categories, deliberately separate:

1. **Navigation target** — a world we're making for (the DEST lock; possibly the cover story).
2. **Sensors target** — a hull we're *curious* about, reason undecided. The telescope ledger
   IS this category: tracking a ship commits us to nothing and shouldn't read as hostile.
3. **Committed intent** — the curiosity resolves into one of FOUR postures (owner):
   **trade** (send shuttles/drones — the peaceful branch, shown in the same sensors
   selection, "we just don't act aggressive with those"), **pirate** (intercept clock →
   boarding), **attack** (firing solution → slug/missile — hunters, or burning bridges), or
   **defend** (a threat we merely watch: keep the fix sharp, mind the eyes race, be ready).

Implication to design: the sensors desk should offer BOTH branches on a tracked contact
(trade affordance when the geometry supports it, 🎯 when it doesn't or when we choose
violence), and ship-to-ship cooperative trading (hailing a hauler to deal, not board) may
deserve to exist as the peaceful resolution of a track.

## Open design questions for the owner

- Does suspicion decay (like heat) or stick per ship?
- Should a *consistent* cover story lower boarding compliance rolls (they weren't braced)?
- Do havens gossip suspicion into the news wire ("a 'trader' out of Earth keeps changing
  course near the He3 lane")?

## Working agreement unchanged

Determinism is law in Core; UI = Razor + Bootstrap; JS only in renderer.js; every lab number
from a real probe run; senior reviews/verifies; owner approves PRs.
