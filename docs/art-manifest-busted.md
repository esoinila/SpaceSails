# Image manifest — PR-BUSTED (the catch)

Art the grok image lane generates later. Each entry names the **placeholder slot** in code (a CSS
art box in `Map.razor` today), the **destination file** under `src/SpaceSails.Client/wwwroot/art/`,
and a **composition spec**. Drop the JPG at the path and swap the emoji/gradient placeholder for an
`<img>` (or an `OP_IMAGE` blit) — no code shape changes.

> House rules for this set: **HOMAGE, not reproduction** — our pirates, no film-frame copies, **no
> real likenesses**. Sepia, cinematic, painterly. 4:3-ish to match the existing bar/hall art.

## 1. `art/busted-freeze-frame.jpg` — THE FREEZE-FRAME (Bolivia game-over)
- **Slot:** `.busted-freeze-art` in `Map.razor` (the `BustedEncounter.Stage.FreezeFrame` panel).
- **Composition:** two silhouetted rogues (our pirate captain + first mate) charging shoulder-to-
  shoulder out of a battered airlock into **blinding muzzle-flash light** — the frame blown out
  white-gold at the centre, figures near-black against it, dust and cordite. **Sepia freeze**, held
  like a final still. An homage to Butch Cassidy and the Sundance Kid's last charge — *the spirit,
  not the frame*. No faces resolvable, no real actors.
- **Mood:** defiant, doomed, romantic. The last good run.
- **Caption baked in code (paraphrased wink, not the film line):** *"…and here we thought this was
  the easy money."*

## 2. `art/busted-ship-explosion.jpg` — THE OLD SHIP BLOWING UP (resurrection)
- **Slot:** `.busted-freeze-art.busted-explosion` in `Map.razor` (the `Resurrected` panel).
- **Composition:** the player's old hull erupting — a kerosene-orange fireball against black space,
  debris and a snapped mast/sail spar tumbling, the collector's grapple line going slack. This is
  where "the kerosene-explosion art budget" goes (owner). One clean hero explosion, no HUD.
- **Mood:** total loss, but clean — the brain-backup already fired; this is just the hardware dying.

## 3. (optional, later) `art/busted-collector-hail.jpg` — the collector's hail portrait
- **Slot:** could back the `Demand` panel header.
- **Composition:** a hard-eyed repo captain in a cramped cutter cockpit, a debt-ledger glowing, a
  boarding net coiled behind. Menacing but businesslike — they want you taxed, not dead.
- **Priority:** low — the Demand panel reads fine text-only for now.
