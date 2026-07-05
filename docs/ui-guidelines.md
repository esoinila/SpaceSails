# UI guidelines — master–detail trees

*Owner direction (2026-07-05): "organize the UI display of info under master-detail-tree where
it fits. This saves screen real estate. Only the details of the relevant item are shown. …on
top level the trade posts only need to show if they are at range by any means now, then
drilling into the data reveals the details that can be more generous now."*

## The principle

When a desk presents a **hierarchy of things** (places → posts → actions → items), lay it out
as **master–detail, like File Explorer**: a tree on the left, and a detail pane on the right
that describes *only the selected node*.

Two rules make it work:

1. **Lean levels.** A tree row is one line: icon, name, and at most **one** badge carrying the
   single most decision-relevant fact at that altitude. For a trading post that fact is
   *reachability by any means right now* (`same orbit` / `drones` / `shuttles` / `out of
   reach`). No prices, no distances-with-decimals, no button rows in the tree.
2. **Generous leaves.** The detail pane can afford full sentences: exact prices with fees,
   transfer minutes, plain-words disabled reasons ("not enough credits (price + ferry fee)"),
   and the alternatives ("close in and match orbit to buy dockside — no fee"). Because only
   one node's details render at a time, generosity costs no screen real estate.

Nothing renders "loose": every piece of business UI hangs under the thing it belongs to. The
dock market taught us this — it used to float as its own column, visually severed from Earth;
now it is the *dockyard node's detail*, tied to the body you're docked at.

## Reference implementation

The **Trade desk** (`Stations/LocalSpace.razor`, since the trade-tree PR):

```text
🪐 Earth                      ← level 0: place
  ⚓ Earth dockyard  [docked]  ← level 1: posts (badge = the one fact)
  🛰 Earth Depot    [shuttles]
     🛒 Buy                   ← level 2: what you can do there
        📦 Machinery × 4      ← level 3: the item
     📤 Sell your hold here
🪐 Highport Satellite Works
  🏭 Highport …    [out of reach]
```

Selecting any node fills the detail pane with that node's business — the dockyard shows the
dock market (sell/refill/upgrades, passed in as a `RenderFragment` so the bindings stay in
Map.razor), an item leaf shows stock, face value, and the one actionable buy priced for the
current reach tier.

## Conventions we've adopted (and why)

From file-manager UX (validated in a design consult with Gemini 3.5 Pro):

- **Breadcrumbs** at the top of the detail pane (`Earth › Earth Depot › Buy › Machinery`),
  every segment clickable — the highest-impact convention: you always know where you are and
  can climb back up without hunting in the tree.
- **One actionable button per detail**, priced for the situation as it is *now*; alternatives
  appear as info lines, not as competing buttons.
- **Disabled means explained.** A grayed control always says why, in plain words, and what
  would change it.
- **Selection heals.** If the selected node disappears (stock sold out, contact left reach),
  selection falls back to the most useful surviving node (the dockyard when docked, else the
  nearest post) instead of a blank pane.

## Roadmap (apply when the data grows into them)

- **Flatten levels that stop earning their row.** With one good per depot, the item leaf is
  cheap; when manifests diversify, consider dropping the leaf level and making the *Buy*
  node's detail a **sortable item list** — cross-post price comparison is the pain to design
  for ("comparison blindness" is the biggest risk of deep trees).
- **Partial sells**: the Sell node's detail should become an inventory view — sell chosen
  units of chosen classes, not the whole hold or nothing.
- **Keyboard navigation**: ↑/↓ moves selection, → drills in, ← climbs out; type-ahead-find
  when lists grow. Adopt together, not piecemeal.
- **Expand/collapse** only once trees outgrow a screen — while everything fits, an
  always-expanded tree is simpler and one click cheaper.

## Where it fits next (candidates)

- **Comms departures board**: body → route → departures, with the dossier as the detail.
- **War room**: contact list as master, the intercept clock/firing panel as detail (partly
  true already; the contact list could adopt the lean-row rule).
- **Cargo manifest**: class → items, once cargo grows attributes worth a detail pane.

## Where it does NOT fit

- **Order-sensitive queues** — the Sensor tasks list is a *carousel*, its vertical order IS
  the information; a tree would hide it.
- **The map itself** — the sky is spatial, not hierarchical. Click-menus on the map stay
  contextual popups; the tree pattern is for desks.
