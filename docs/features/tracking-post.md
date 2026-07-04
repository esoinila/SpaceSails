# Tracking post

What this is: the ship's telescope station — aim it at a patch of sky, sweep it over sim time,
and hold a ledger of ships that don't publish a timetable (the secretive He3 haulers and anyone
else the [traffic board](traffic-board.md) can't already tell you about).

Where: press the **Track 📡** toolbar button on the map.

![Tracking post](../tmp_pics/saturday/tracking-post.png)

## The sun-blind rosette

The card's left side draws a small egg-shaped "rosette" around your ship: how far the telescope
can see, by look direction, relative to the sun. Detection range follows a cosine ramp between two
extremes:

- Pointed **straight at the sun** — near-blind, only **8%** of base range (glare swamps
  everything but the brightest returns).
- Pointed **straight away from the sun** (anti-sunward) — full base range, the pirate's best
  hunting angle: dark sky, targets lit from behind you.

Base range is **6.0×10¹¹ m** — about six times the ship's passive proximity sensor — but only
along the telescope's aimed bearing; sweep somewhere useless and you see nothing no matter how far
away a target sits. The wedge overlaid on the rosette shows your current aim; any tracked contact
appears as a colored dot at its true bearing and range fraction (green = solid lock, amber = fair,
red = fading).

## Sweeping

Set a **bearing** (0–359°) and an **arc width** (5–360°) with the two sliders, or pick one of the
ready-made **scanning programs** from the dropdown — a corridor watch for every pair of trade-anchor
bodies present in the scenario (Venus, Earth, Mars, Jupiter, Saturn), padded 8° on each side so a
normal maneuver doesn't slip a target out of the wedge mid-sweep.

Click **Start sweep**. Sweeping isn't instant: a full 360° survey takes **6 sim-hours**, so a
narrow wedge finishes faster than a wide one (time scales linearly with arc width). A progress bar
tracks it; **Stop sweep** aborts early. When it completes, every candidate whose bearing falls
inside the wedge and whose distance is within the telescope's sun-relative range at that bearing
gets detected and added to the ledger.

## The tracked-targets ledger

Once a sweep finds a ship, keeping the lock is cheap:

- A brand-new track starts at **40% quality**.
- **Confirm** does a short, cheap re-look at the target's *predicted* position (dead-reckoned
  forward from the last observation) rather than a fresh full sweep — succeeds only if the target
  is still inside its predicted uncertainty cone and within telescope range, and bumps quality by
  **+35%** (capped at 100%) on success. A target that burned hard enough to leave the cone slips
  the re-acquire — go sweep again.
- Left unconfirmed past **5 days**, quality decays **20%/day** beyond that horizon. Below **5%**
  quality the entry drops off the ledger entirely — the contact is lost for good until a fresh
  sweep finds it again.
- **Drop** removes an entry manually (e.g. to free a slot).

The ledger table shows each entry's callsign, a quality bar, days since last confirm, and Confirm/
Drop buttons. A target you've laser-ranged from the [dark web](dark-web.md) panel shows an
**aware ⚠** tag next to its callsign — it knows it's been pinged.

## Telescope count — the upgrade axis

How many ships you can hold on the ledger at once (**MaxTracks**) is a permanent upgrade bought at
the dock alongside reaction mass, sensor range, and cargo hold — see
[dock-and-economy.md](dock-and-economy.md). Level 0 holds 1 track; each level adds one more, up to
4 simultaneous tracks at max level. Trying to add a new contact past the cap fails outright — drop
something or upgrade first.

## Emphasis on the nav map

A tracked ship renders brighter on the map itself, with a small uncertainty ring around it sized
off the same growth formula the [traffic board](traffic-board.md)'s prediction cone uses — except
scaled down by the track's own quality: a fresh, high-quality reconfirm shrinks the ring to as
little as **30%** of the ordinary cone width, while a shaky or stale track barely tightens it at
all. A good track is a visibly better intercept than an unaided pin.

See also: [traffic-board.md](traffic-board.md) for what's on the public board versus what only the
telescope finds, [dark-web.md](dark-web.md) for selling your tracked finds and laser-ranging one
for a perfect fix, [dock-and-economy.md](dock-and-economy.md) for the telescope upgrade.
