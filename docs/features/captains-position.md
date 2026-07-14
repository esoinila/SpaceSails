# The captain's position

What this is: the desk where the captain sets the ship's **mission** — the goal, not the piloting.
Every other desk keeps doing its own job (Nav flies, Sensors watches, Trade deals); the captain's
desk is where you decide *what for*. The owner's framing: "the mission of the ship could be the
captain's position... he selects the goal of the ship. The detail of that is the summary info then
on the other stations" — the Expanse bridge feel, where a crew works its stations off the captain's
standing orders.

Where: the **Captain desk** — press `0` or click **0 Captain** in the station tab bar (see
[station-desks.md](station-desks.md)). It leads the tab bar, ahead of Nav, because the captain's
word comes before the helm's.

![The captain's position](../tmp_pics/saturday/captain.png)

## The ship's articles

The top of the desk is a single, uncluttered statement of the current mission — large text, no
instruments competing for attention. That's deliberate: unlike the instrument-heavy desks (Sensors'
scope wall, the War room's tactical circle), the captain's desk doesn't have "one big instrument"
to fill 70% of the screen. There's just the one thing that matters: the order currently standing.

## Selecting a mission

Below the articles, the desk lists every mission you can select, grouped by kind:

- **Free sailing** — the default; no orders given.
- **Hunt** — pick a cargo class to run down (e.g. "Hunt: He3 haulers"). Options come from the
  distinct cargo classes actually carried by the scenario's traffic.
- **Trade run** — pick a directed route (e.g. "Trade run: Earth → Mars"). Options come from the
  scenario's route pairs, one per direction.
- **Lay low** — pick a haven to hide at (e.g. "Lay low: Enceladus"). Options come from the
  scenario's haven bodies.
- **Survey** — pick a corridor to chart end to end (e.g. "Survey: Saturn–Mars corridor"). Options
  come from the scenario's trade-anchor pairs, direction collapsed (Saturn↔Mars is one corridor
  regardless of which way cargo actually runs).

**One click selects — no confirm dialog.** The captain's word is final the moment it's given; the
mission updates instantly and every desk's summary chip reflects it on the very next render.

If a scenario has no traffic section (e.g. the Wheel of the World), Hunt/Trade run/Survey simply
offer nothing to pick — Free sailing and Lay low (which only needs the body list, not traffic)
still work.

## The mission chip

Every desk except the captain's own shows a `☠ Captain` chip at the **top** of the summary strip —
the reserved slot the desk framework left for it (see
[station-desks.md](station-desks.md#the-summary-chips)). Its line is exactly the mission's tight
one-liner (`ShipMission.Describe()` in `SpaceSails.Core`): "Hunt: He3 haulers", "Trade run: Earth →
Mars", "Lay low: Enceladus", "Survey: Saturn–Mars corridor", or "Free sailing". Click it to jump
straight to the Captain desk, same as any other chip.

## The mission model (Core)

`SpaceSails.Core.ShipMission` is a small, pure, deterministic record: a `MissionKind` plus whichever
optional fields that kind needs (target cargo, origin/destination body ids, haven body id, or a
corridor's two body ids). `Describe()` is a pure function of the record — it humanizes body ids for
display ("mercury-compute" → "Mercury Compute") without needing an ephemeris lookup, so it stays
usable anywhere in the client without threading scenario state through it.

`SpaceSails.Core.MissionCatalog.Build(ephemeris)` generates the four selectable groups from a
scenario's ephemeris and traffic definition in one deterministic pass — first-seen order walking
the scenario's route/body lists, never sorted or shuffled, so two builds off the same scenario
always produce identical, identically-ordered option lists.

## The ledger

The captain's **📜 Ledger** tab (was "Quests") is the ship's one brief — jobs and intel in the same
place, because the owner's playtest found bought tips effectively vanished (a route tip only surfaced
as a "Known" tag deep on the dark-web market view). Two sections:

- **⚓ Jobs** — the contract cards, unchanged: work picked up ashore, its staged checklist, and the
  🔭 "point the scope" hook while a fetch hunt is pre-scan.
- **🔭 Tips & intel** — every whisper worth keeping. Each **scope-intel** fix (the Fixer's transponder
  estimate) lists its grounded numbers and keeps its 🔭 button — the same handler as the Comms card,
  which jumps to Sensors with the prioritized scan already queued. Each **route tip** (a Gilt-Eye
  gift, a dark-web buy) states the ship's real route in plain words and offers `→ dark web` (Comms,
  dark-web node selected) and, when the ship is a known contact, `→ dossier` (Comms, that contact
  selected). Where we recorded it, a provenance line reads **"&lt;giver&gt; · &lt;station&gt; · day N"**;
  older or bought-blind entries render unattributed rather than being withheld. A tip with no action
  is still listed, labeled *background — may matter later*. Empty, it says so honestly: "No tips yet —
  buy someone a round ashore."

The pattern the owner asked to generalize: **the ledger never does the task — it jumps you to the
desk that acts on it, context pre-loaded.** Receipt messages when a tip lands now say where it went —
"…filed in the Captain's ledger (0)" — so the desk (key `0`) is named at the moment of filing. Test
cheat: `?tip=route` seeds a route tip so the Tips & intel rendering is reachable without a bar
(see [haven-interior-walk.md](haven-interior-walk.md)).

## Persistence

Session-only, same as everything else in this build — the mission lives in a field on
`Pages/Map.razor` and resets on reload. No save system exists yet; when one lands, the mission
belongs in it same as credits, cargo, and heat.

## What's NOT here yet

Mission-relevant *highlighting* on other desks (the hunted cargo class glowing on Trade, the
lay-low haven highlighted on Nav/Comms) is future work per the addendum in
`docs/SaturdayPlan/StationDesks.md` — this PR ships the mission model, the desk, and the chip; the
highlighting hooks are a natural follow-up once Sensors/Comms/Trade have stable row identities to
highlight against.
