# Dock & economy

What this is: fencing your loot, refilling your fuel, and spending credits on a permanently
better ship.

Where: fly within a market's **port zone** (about 0.067 AU — deliberately generous), then press
`4` (or click **4 Trade** in the station tab bar — see [station-desks.md](station-desks.md)) to
open the Trade desk. As of PR-13 the desk is a three-column trading floor: [local space
contacts](local-space.md) on the left, this dock market in the middle column (its header always
reads "Dock market", with a "Docked at *body*" badge once you're actually docked), and the
[cargo manifest](#the-cargo-manifest) on the right. Markets exist at **Earth, Mars, and Venus**
only — depots and other planets are plunder targets, not places to sell (see
[depots.md](depots.md)).

## Selling cargo

- **Sell cargo** pays out your full hold at once, priced per unit by cargo class: He3 is the
  prize (1200 cr/unit), then Compute cores (400), Alloys (300), Machinery (250), Ice (100), and
  anything unclassified (50). He3 pays best; ice pays the rent.
- **Refill mass** tops your reaction-mass pulses back up to capacity, for free.

## Upgrades

Four permanent upgrade tracks, each bought as a level-up with escalating price (2000 cr, then
doubling each level: 4000, 8000, 16000…):

| Track | Effect per level |
|---|---|
| Reaction mass | +150 pulse capacity (base 250) |
| Sensor range | ×1.4 detection range (compounding) |
| Cargo hold | +10 unit capacity (base 10) |
| Telescope | +1 simultaneous tracked target (base 1, cap 3 levels → 4 tracks) — see the
  [tracking post](tracking-post.md) |

Each row shows your current level, what the *next* level buys, and the credit price — the buy
button disables itself if you can't afford it.

## Running dry

If you burn through every mass pulse away from a market, you go **Adrift** — an alert bar appears
with a **Request rescue** button. Docking clears the Adrift state and tops off your pulses for
free, so the real cost of running dry is distance, not cash.

## The cargo manifest

The Trade desk's third column (PR-13) is a read-model over the hold: every cargo class currently
aboard, its unit count, and its estimated fence value at the same per-unit prices as **Sell cargo**
above — a running total at the bottom always matches the aggregate hold shown elsewhere (the Nav
readout, the Trade summary chip). It's read-only — selling still happens through **Sell cargo**
here or a [local-space](local-space.md) drone trade — but whichever way cargo leaves or enters the
hold (boarding, selling, a drone transfer, a hunter's confiscation, a rescue fee), the manifest
updates with it. While a drone transfer is in flight, the same panel shows its progress bar large,
right above the manifest table.

See also: [depots.md](depots.md) and [traffic-board.md](traffic-board.md) for where cargo comes
from, [boarding-run.md](boarding-run.md) for how it gets into your hold.
