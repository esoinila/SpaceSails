# Treasure-map art manifest (PR-HOARD, #223 / #164)

The treasure-map card (`Map.razor` → `.treasure-map-card`) has a big background image slot per body.
Until these land, the card fills the slot with a deterministic per-body gradient placeholder
(`TreasureMapArtCss`). Drop a real asset at `src/SpaceSails.Client/wwwroot/art/treasure-<bodyId>.jpg`
and point the `.tm-art` `background-image` at it (behind the red X), same convention as the existing
bar/cabin art.

House framing rule (lessons 11/12): the PLACE is real; the fiction is what we BUILD or BURY there.
No real likenesses, no film-frame reproductions. Landscape / wide (card slot is ~34rem × 12rem).

## Composition notes (grok image lane)

| bodyId | Body name | Composition note |
|---|---|---|
| **phobos** | **Phobos** (flagship, #164) | The 85 m monolith — a lone boulder throwing a long hard shadow on grey regolith near the Stickney crater rim, Mars a rust crescent low on the black horizon. A meeting place for deals; a scuffed patch of ground in the foreground where a chest went down. |
| luna | Luna | Bright cratered maria, Earthlight, a survey landing beacon and bootprints; a low cairn of stacked stones as the landmark. |
| europa | Europa | Cracked ice plains, chaos terrain lineae in rust and cream, Jupiter huge and banded on the horizon; a beacon frozen into the ice. |
| ganymede | Ganymede | Grooved grey-tan terrain, old dark cratered patches beside bright young grooves, Jupiter distant; a leaning survey mast. |
| callisto | Callisto | The most cratered world — a dense saturation of impact scars, dark dusty ice, faint Jupiter; a half-buried lander leg as landmark. |
| titan | Titan | Orange methane haze, dim diffuse light, dark dune fields and a hint of a liquid-methane shoreline; a weathered beacon in the murk. |
| enceladus | Enceladus | Blinding-white fresh ice, blue "tiger-stripe" fractures, a distant plume catching sunlight over the limb; a beacon on a snowfield. |

## Card-frame art (optional, one-off)

- A parchment/scrimshaw map border for `.treasure-map-card` — aged spacer-vellum, burn-edged, a
  compass rose that reads "spinward / anti-spinward" rather than N/E/S/W. The red X is drawn in CSS
  (`.tm-x`) and must stay on top of any art.
