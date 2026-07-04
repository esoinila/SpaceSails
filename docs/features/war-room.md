# War room

What this is: the gun deck — a top-down tactical view of nearby contacts, weapon-range and
catch-radius rings, warning shots, threats, bribery, and the **heat** a robbery leaves behind.
Gentleman's-pirate rules: sinking cargo is worthless, taxing it pays, so there's no way to actually
destroy a ship here — only to make it heave to.

Where: the **War room desk** — press `3` or click **3 War room** in the station tab bar (see
[station-desks.md](station-desks.md)). Full-screen, not a pop-up card.

![War room](../tmp_pics/saturday/war-room.png)

## Weapon range vs. boarding range

Guns speak before shuttles fly: **weapon range is 2×10⁸ m**, well inside the boarding shuttle's
5×10⁸ m capture envelope (see [boarding-run.md](boarding-run.md)). The tactical circle (centered on
your ship, ~1×10⁹ m radius) draws a ring at weapon range around you and, for any active hunter, a
ring at its catch radius.

## Warning shots and compliance

Every ship (except unmanned cargo pods, which have no crew to warn) is deterministically either:

- **Compliant** (~75% baseline) — heaves to under a warning shot. A compliant or bribed target
  boards at **half** (`0.5×`) the usual boarding time — the fastest way to board anything in the
  game, faster than a perfect physical intercept.
- **Stubborn** (~25% baseline) — ignores the shot and calls its own muscle instead. The stubborn
  fraction creeps up **5% per heat level** you're carrying (capped at 60%) — word travels, and a
  known pirate gets a jumpier reception.

Which one a given ship is never changes randomly — it's hashed from the ship's own id, so warning
(or hailing) the same ship twice always gets the same answer.

**Hail** shows a canned threat/reply line inline — a surrender line for a compliant target, a
defiance line for a stubborn one, both pirate-flavored and, like compliance itself, deterministic
per ship. **Warn** (a warning shot) is enabled only while the target is inside weapon range and
never for pods.

## Bribery

**Bribe** buys the same compliance without ever raising heat — an inside job, nobody calls the
cavalry. Price is `cargoUnits × unitValue(cargoClass) × 0.35` — deliberately cheaper than what
robbing the ship outright would pay. Once bribed, a ship's status badge reads **🤝 bribed** and the
button disables itself (can't re-bribe, and disables if you're short on credits).

## Heat

Robbing a ship (not just warning it) raises **heat**, a 0–3 gauge shown as flames in the header
(`◌◌◌` → `🔥🔥🔥`). Heat decays **one level per 20 days**, **4× faster** (5 days) while you're
hidden in orbit at a haven — the header's cooling line spells out which rate applies. A warning
shot alone never raises heat; only an actual robbery does.

## Hunters

Every heat-raising robbery spawns one **hunter** — hired muscle — fitting out for **5 days** at the
nearest policed planet (inside the same 4×10¹¹ m central/outer split the traffic schedule uses,
excluding havens: nowhere policed is pirate country). Once fitted out it's a dumb, relentless,
thrust-limited pursuit (0.5 m/s² accel) straight at your current position, stepped in 60-second
sim-time quanta so it scales with warp like ordinary NPC traffic. The header shows the nearest
hunter's bearing and distance whenever one is active, and its catch-radius ring appears on the
tactical circle once close.

A hunter **catches** you inside **3×10⁸ m** at under **3,000 m/s** relative speed: the consequence
is your whole cargo hold seized plus a **500 cr** fine (the same shape as the ordinary Adrift
flow). Staying hidden in continuous haven orbit for **2 days** makes a hunter **break off** and
lose the scent.

## Hiding at havens

A haven is the escape valve for the whole loop: dock or orbit there to **cool off** (heat decays
4× faster), **trade** (cargo and, if it's also a far trading post, intel — see
[dark-web.md](dark-web.md)), and **repair**, all no-questions-asked per the "scum and villainy work
the outer reaches" worldbuilding.

See also: [boarding-run.md](boarding-run.md) for the boarding-time math the compliance factor
discounts, [local-space.md](local-space.md) for the panel this one sits alongside (right-middle of
the HUD), [dark-web.md](dark-web.md) for what a laser-ranged "lit up ⚠" ping means for a target
you're now hunting.
