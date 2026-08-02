# Image manifest — PROJEKTI KAAMOS (#411, #528)

Three plates for the polar-night arc: the pod that was held, the one who kept the berth, and the berth
answering. Each entry names the **slot in code**, the **destination file** under
`src/SpaceSails.Client/wwwroot/art/`, and the **exact prompt used** — this set is painted, so the prompts
here are a record, not a brief.

Filed against #528, whose finding is that *the game narrates its consequences in pulse lines* — a toast
that fades in a second and a half — and reserves the reveal card for wrecks and deaths. These three beats
are the arc's turnings and they were all toasts.

> **House rules for this set:** grimy lived-in used-future sci-fi, muted desaturated palette, painterly,
> moody lighting, **no text, no lettering, no logos, no numbers, no readable writing** — every word of the
> copy is written in code and must not be doubled in the pixels. No real likenesses. 16:9.
>
> **And one rule specific to this arc.** KAAMOS is a mystery that is kept by *not being answered*. The
> paintings may show that something was prepared, held, and never collected. They may never show what is
> under the ice, and they may never show anyone reacting to it. The dread here is clerical: a seal that was
> never broken, a stool nobody sits on, a line on a board that has never been switched off.

## The plate recipe (the vented-room card, generalised)

Four things, all load-bearing — see `KaamosPlate` in `src/SpaceSails.Core/KaamosLore.cs`:

1. **A title that names the place and the verb.** Not "fragment acquired".
2. **One painted image of a CONSEQUENCE**, not an action shot.
3. **A caption that describes evidence and stops.** It never says what it means. The seals are latched;
   nobody tells you who decided not to send her.
4. **It fires at the moment it explains the most** — the single seam where the shard is actually
   assembled (`Map.Kaamos.TryAssembleKaamos`), so a plate can never be shown for a shard nobody found.

**Not every beat gets one.** Three of the six KAAMOS shards are deliberately plate-less: a line on a
dedication plaque, a log found in a drawer, and a coordinate bought over a counter each already arrive
with their own scene around them, and none is a turning. Over-carding cheapens the ones that are not —
`KaamosPlatesArePaintedTests.TheBeatsThatAreTheRightSizeAsProseGetNoPlate` pins that decision.

---

## 1. `art/kaamos-cold-pod.jpg` — THE POD THAT WAS HELD

- **Slot:** `KaamosLore.PlateFor("cold-pod")`. Fires when the beach-comber's probe turns up the derelict
  supply pod on a dig (`Map.Surface` → `KaamosPodHere` → `TryAssembleKaamos`).
- **Reach it:** `/map?kaamos=pod&land=1` — land, take the metal detector out, probe any square.
- **Why this one first:** the single most paintable object in the arc. A cargo run that was *packed and
  then not sent*, still sealed, generations later.
- **Discipline:** the fragment's own prose already reads the manifest slug out loud (CONSUMABLES,
  WINTERING CREW, 40 SOULS). The plate must therefore **not** show that plate legibly — the words are the
  code's job, the dread is the image's. The manifest plate is present and scoured blank.

> **Prompt used:** A half-buried cargo supply pod lying in grey regolith dust on an airless moon, ribbed
> metal hull frost-cracked and split along its seams, generations old. Its heavy clamp seals are still
> latched and have never been broken. A rectangular manifest plate is bolted to its flank, scoured
> completely blank and illegible by decades of dust and cold, just faint scratched marks where writing once
> was. A shallow trench scraped away from one side by hand tools, a probe rod left standing in the dust.
> Grimy lived-in used-future sci-fi, muted desaturated palette of cold blue-grey and dust brown, painterly,
> moody, low raking light, long shadows, no text, no lettering, no numbers, no logos, no readable writing,
> no people.

## 2. `art/kaamos-berth-holder.jpg` — THE ONE WHO KEPT THE BERTH

- **Slot:** `KaamosLore.PlateFor("holders-tell")`. Fires when the bar seam yields the holder's tell
  (`Map.Kaamos.AskAboutKaamos`).
- **Reach it:** `/map?kaamos=holder` — dock anywhere, walk to the counter, the seam offers the tell.
- **Composition rule (the bar-patron idiom):** seen from **across the room**, past out-of-focus shoulders,
  the way every other patron in this game is framed. The face is turned away and unresolved — no
  likeness, and no portrait dignity either. He is furniture that talks.
- **The evidence in frame:** two empty glasses, and the stools either side of him empty in a bar where
  nothing is empty. The caption names both and stops.

> **Prompt used:** A lone drinker on a corner stool at the far end of a grimy spacer bar, seen from across
> the room past out-of-focus shoulders. An old spacer in a worn insulated jacket, hunched, head turned away
> toward the bulkhead so the face is unresolved and in shadow. One empty glass in front of them and a
> second one already empty pushed aside. The stools either side are vacant though the rest of the bar is
> not. Low amber work-lighting, cigarette haze, scratched counter, riveted bulkhead. Grimy lived-in
> used-future sci-fi, muted desaturated palette, painterly, moody, melancholy, no readable text, no
> lettering, no numbers, no logos, no recognizable face.

## 3. `art/kaamos-berth-resolves.jpg` — ONE LINE STILL LIT

- **Slot:** `KaamosLore.PlateFor("berth-code")` — the capstone, and the loudest plate in the arc. Fires on
  the single edge where the berth-code resolves and `CanReachEnceladus` flips, alongside
  `KaamosLore.ReachNotice`.
- **Reach it:** `/map?kaamos=4`, then dock at any bar — the seam offers *"❄ Put the KAAMOS pieces
  together"*.
- **The whole point of the composition:** the board is enormous and dead, and **one** line is lit. The
  arc's truth is that the berth is still listed because, from under the ice, someone is still asking for
  it — and the picture says exactly that much and no more. There is no one in the concourse, because a
  figure looking at the board would be the game telling you how to feel about it.
- **Text rule matters most here.** A berth board is made of writing. Every character had to come out
  illegible or the image would have written the arc's copy for it.

> **Prompt used:** A tall berth-allocation board dominating the far wall of a dim, almost empty orbital
> exchange concourse. Dozens of rows of illuminated slot lines stacked in columns, every one of them dead
> and grey except a single line low on the board still burning a warm amber. The characters on every line
> are indistinct, worn, smeared and completely unreadable, only the rhythm of marks where writing would be.
> Worn deck plating, dust hanging in a shaft of light, a scuffed handrail in the foreground for scale.
> Grimy lived-in used-future sci-fi, muted desaturated palette, painterly, moody, cold, quiet, no readable
> text, no lettering, no numbers, no logos, no people.

---

## Generation recipe

grok is the project's gen-AI art source (owner ruling 2026-07-18: **images only — no code, no git**).

```bash
grok -p "Call your image_gen tool (aspect_ratio 16:9) with prompt: '<PROMPT>'. Save the result to '<ABS PATH>' and confirm." \
     -m grok-4.5 --permission-mode bypassPermissions
```

- Only model is `grok-4.5`. **`grok update`** refreshes auth (the TUI's ctrl-u, no browser needed), and it
  must be run **from PowerShell, not Bash** (different auth state).
- grok ignores `--worktree` and acts in its CWD: **generate into the scratchpad, look at every one, then
  copy into `wwwroot/art/`.** Everything it saves is a JPG whatever the extension says.

## Status

| # | Slot (`KaamosLore.PlateFor`) | File | Code wired | Art | Guard |
|---|---|---|---|---|---|
| 1 | `cold-pod` | `kaamos-cold-pod.jpg` | ✅ | ✅ | ✅ |
| 2 | `holders-tell` | `kaamos-berth-holder.jpg` | ✅ | ✅ | ✅ |
| 3 | `berth-code` | `kaamos-berth-resolves.jpg` | ✅ | ✅ | ✅ |

The guard is `tests/SpaceSails.Core.Tests/KaamosPlatesArePaintedTests.cs`: every plate must be keyed to a
real fragment and must name a JPG that is **actually on disk**. The `onerror`-hide law is what makes
shipping code before art safe, and it is exactly why a plate pointed at a file nobody painted is otherwise
**invisible** — it does not throw, it does not log, it just leaves a hole in the card forever. The csproj
copies `wwwroot/art/kaamos-*.jpg` beside the test assembly on every build (the `CssZBandSyncTests` idiom),
so the test reads the live art directory rather than a snapshot. Proven RED before shipping by pointing
`cold-pod` at `kaamos-cold-pod-nope.jpg` and by misspelling its key.
