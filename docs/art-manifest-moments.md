# Image manifest — THE STORY MOMENTS (#528)

Art for the moments the game narrates. Each entry names the **beat** in `StoryBeats.Beat`, the **destination
file** under `src/SpaceSails.Client/wwwroot/art/`, and a **composition spec** ready to hand to the image lane.

The code already knows every filename (`StoryBeats.ArtFile`), so a beat whose JPG has not been painted yet still
fires — title and caption carry it, and the `<img>` hides itself. **Drop the JPG at the path and it appears.**

---

## House rules for this set

> **IT IS A SPACESHIP. IT IS NEVER A SEA SHIP.** Owner, on the first two canvases of this set: *"Lets keep the
> theme space not sea ship."* Both had to be regenerated — "pirate freighter" and "gun deck" were read as
> Age-of-Sail, and came back with an open-air naval deck under an overcast sky and a *sea horizon*. The words
> "pirate", "sail", "deck", "hold" and "bridge" all carry a nautical gravity that the image model follows unless
> it is pushed. Every prompt in this file therefore carries an explicit negative list, and new ones must too:
>
> ```
> STRICTLY NO water, NO sea, NO ocean, NO waves, NO sky, NO clouds, NO daylight, NO horizon,
> NO open-air deck, NO wooden deck, NO cloth sails, NO rigging, NO naval vessel, NO harbour.
> ```
>
> And the positive framing that actually works: **"INTERIOR OF A SPACESHIP"** / **"EXTERIOR IN DEEP SPACE
> against BLACK SKY AND STARS"**, plus one concrete space detail — a starfield through a thick viewport, radiator
> fins, thrusters, grated deck plating, conduit.
>
> (Our ships *do* carry sails — solar/mag sails — and that is exactly the trap: say **gossamer reflective film
> and spars**, never "sail" alone.)

Also standing, from the earlier manifests:

- **Homage, not reproduction.** Our pirates; no film frames, **no real likenesses**.
- **No text, no lettering, no logos** — captions are written in code and must not be doubled in pixels.
- Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody lighting, cinematic.
- `aspect_ratio 16:9` for scene art. Generate to the scratchpad, eyeball it, then copy into `wwwroot/art/`.
- **Never point the image tool at a checkout** — it ignores `--worktree` and acts in the CWD.

Recipe (PowerShell; the auth lives there, not in the Bash tool's environment):

```powershell
Set-Location <scratchpad>
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS/FWD/SLASH/PATH>' and confirm." -m grok-4.5 --permission-mode bypassPermissions
```

If it reports *"You are not authenticated"*: run **`grok update`** (the non-interactive equivalent of the TUI's
Ctrl-U). It restores the session without a browser.

---

## 1. `art/first-shot.jpg` — THE FIRST ROUND YOU EVER FIRED ✅ PAINTED
- **Beat:** `FirstShotFired` · once per captain, ever · **PLATE** (it happens mid-fight)
- **Fires:** `BARREL LOCKED` in `Map.Combat`.
- **Composition:** interior of a sealed pressurised weapons bay, seen from behind the breech of a mag-rail cannon
  that has just fired — recoil cradle still rocking, spent slug casing rolling on grated deck plating, thin smoke
  in low amber worklight, black space and stars through a thick viewport. Two crew in patched flight coveralls
  **not** looking at the gun sight — looking at each other, one frozen mid-motion.
- **Why:** a smuggler becomes a pirate exactly once, and it used to be a status line that faded in 1.5 s.

## 2. `art/sail-holed.jpg` — HER SAIL IS GONE ✅ PAINTED
- **Beat:** `SailHoled` · cooled 6 min · **PLATE**
- **Fires:** the hit that leaves a ship adrift and boardable.
- **Composition:** exterior, deep space, black sky and stars: a small cargo spacecraft drifting, her enormous
  gossamer reflective film blown out mid-span, torn ribbons peeling away, broken spars trailing, debris
  glittering. The hull **intact** and her windows still **lit warmly from inside** — someone alive in there. No
  engine plume. One distant sun flare low in frame.
- **Discipline:** a **consequence**, not an explosion. The horror is that she is fine except for the one thing she
  needed.

## 3. `art/collector-hail.jpg` — GRAPPLES ✅ PAINTED
- **Beat:** `CollectorHail` · every time (rare by nature) · **CARD**, and it never defers — it *is* the danger
- **Slot:** `.busted-collector-hail` (was `docs/art-manifest-busted.md` item 3, unpainted since PR-BUSTED).
- **Composition:** interior of a spacecraft bridge over the shoulder of a silhouetted captain; through the angled
  forward viewports the armoured hull of a far larger spacecraft fills the view against stars — thrusters,
  radiator fins, plating — with magnetic grapple cables stretched taut across the frame and clamped to our hull.
  Red-orange running lights wash the dim bridge the colour of a docking clamp.

## 4. `art/crew-deputation.jpg` — A DEPUTATION ✅ PAINTED
- **Beat:** `CrewDeputation` · once ever · **CARD** (defers while in danger)
- **Fires when wired:** `CrewTemp.Standing` first reaches `Petition` — *"the last cheap moment"*.
- **Composition:** interior spaceship corridor, conduit overhead, grated floor: three crew in patched coveralls
  outside a cabin door, caps in hands, one with his knuckles raised and **not knocking yet**. Seen from the
  captain's side of the door, the doorframe dark in the foreground. They have clearly agreed who will say it.
- **Caption discipline:** it says what they want fixed and nothing about consequences.

## 5. `art/crew-meeting.jpg` — THE MEETING YOU WERE NOT ASKED TO ✅ PAINTED
- **Beat:** `CrewMeeting` · every time · **CARD**
- **Fires when wired:** `Ultimatum`, and the owner's *"crew considers captain changing and has secret meetings"*.
- **Composition:** the ship's cantina at an odd watch — interior spacecraft mess, lamps down, a viewport of stars
  behind — five crew round one table and a chair pulled out that **nobody is sitting in**. Not one of them looks
  at the door.
- **Why it matters:** this is the card that makes the crew sheet frightening, and it pairs with the scuttling
  charges' second key (#522): these are the people who will or will not turn it.

## 6. `art/arc-news.jpg` — THE STORY BREAKS ✅ PAINTED
- **Beat:** `ArcNewsBreaks` · every time · **CARD**
- **Fires when wired:** an arc beat landing on the wire (Nebula Mutual #422, KAAMOS #411).
- **Composition:** a station concourse interior — pressurised, ribbed ceiling, a planet or ring visible through a
  gallery window — dominated by a big screen mid-broadcast, the room's faces turned up to it. One figure in the
  foreground walking **away** from the screen, because it is not news to them.
- **Cross-ref #380** (*events must introduce their fiction one beat earlier*): this card is that beat.

## 7. `art/charge-let-go.jpg` — SHE LETS GO ✅ PAINTED
- **Beat:** `ChargeLetGo` · cooled 10 min · **PLATE**
- **Fires when wired:** the hull discharge (#523's dump).
- **Composition:** exterior, close on the mast and antennae of a small spacecraft against black space: a
  blue-white discharge core sitting on the mast tip with filaments raking outward into the dark, the hull below it
  edge-lit by its own light for one instant. Stars behind.
- **Physics note:** a real discharge is a **plume off the sharpest extremity**, not a sphere around the ship —
  draw it off the mast and it is both truer and better looking. The canvas version of this effect (a live
  renderer animation rather than a still) is the follow-up the owner asked for: *"could have plasma ball like
  beautifull effect if physics supports it."*
- **The canvas obeys the lab.** It came back as a filament plume off the antenna whip's tip with the hull edge-lit
  below — which is Lab 43's Finding 1 (the whip runs **20,000×** the hull's surface field) drawn without being
  told twice. Nothing to regenerate.

## 8. `art/fire-aboard.jpg` — THERE IS FIRE IN HER ✅ PAINTED
- **Beat:** `FireAboard` · every time · **CARD** (defers while in danger)
- **Fires:** `HullFire.FoundLine`, when a captain first finds one burning aboard a derelict (#524).
- **Composition:** interior of a derelict spacecraft compartment, frost on the near bulkheads and grated deck,
  looking through an open pressure hatch into the next compartment where a low fire is burning in the debris —
  black smoke rolling along the deckhead and being drawn out through the hatch. Nobody in frame.
- **Why it is a card and not a plate:** the three roads (valve / pumps / dog the hatch) are a decision, and the
  frost in the foreground is the argument for the third one — this hull has been cold for forty years and one
  pocket of her was not.
- **Cadence note:** this is the one beat in the set with `EveryTime` cadence, and the cadence test made that
  choice justify itself in writing rather than inherit a default.
