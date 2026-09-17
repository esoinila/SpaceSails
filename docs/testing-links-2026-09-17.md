# Test links — the 2026-09-17 rulings (PRs #1204, #1205)

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A. Every link boots the live build at
`https://esoinila.github.io/SpaceSails-play/map?…` straight into the situation, using only cheats that
exist in the query whitelist. Read Appendix A for what each key does.*

The run: **two owner rulings, recorded the same morning, and one lane each.** Both are decisions that had
been sitting open on the story passes — one about a card that said too much, one about a line that could
not be said at all — and both of them close.

| ruling | issue | what shipped |
| --- | --- | --- |
| *"love all those, let's pick the recommended one"* | [#422](https://github.com/esoinila/SpaceSails/issues/422) option **B** | the convergence card stops explaining |
| *"yes let's have that possibility … it certainly closes a story arc for that captain"* | [#640](https://github.com/esoinila/SpaceSails/issues/640) option **A** | the run can end |

---

## 1 · Things to play

| what | link | what to look for |
| --- | --- | --- |
| **THE CONVERGENCE is a collision now** (#422 option B, PR #1204) — the card used to be eight sentences of third-person exposition that spent every secret both arcs still had to give, at a bar *below* either arc's own capstone, with a button that told you how to feel about it | [`?converge=1`](https://esoinila.github.io/SpaceSails-play/map?converge=1) | **Not a paragraph.** A stamp of two marks (`◼ ❄`) with no words, the plate, then **two sentences one above the other with nobody named over either of them** — *"It still calls the manifest in. Every window, right on the tick. Same forty names. I stopped reading who was speaking them."* and *"I've filed the same subscriber six times. Different faces, same number. Every one of them shook my hand certain they were the first."* — then one closing line, *"You have been carrying both of these for a while."*, and a plain **Close**. No copies, no premiums, no archive, no Vantar, no KAAMOS, nothing about the Old Ones. The player does the arithmetic or nobody does. |
| **…and both lines really are in your ledger** (#422, the audit) | same link → close the card → **Captain's desk → the ledger** | Both quoted sentences are readable there, in the shards that speak them. That is not luck: the joint bar is still **3 + 3**, but one of each three is now the shard the card quotes (`holders-tell`, `adjuster-tell`), because the closing line claims you have been carrying them and a bare count could not make that true. |
| **NO PATTERN ON FILE — the run ends** (#640 option A, PR #1205) — the line has been authored, wording-tested and **read by nothing** since the archive node landed, because a card saying POLICY CLOSED over a sim that then resurrects you is this project's most expensive bug class | [`?nopattern=1&death=impact`](https://esoinila.github.io/SpaceSails-play/map?nopattern=1&death=impact) | The ordinary four-stage death — the art, the seeded line, `…wake up`. Press it and **watch what does not happen**: no clinic, no bill, no rustbucket, no successor, no new face, no filing line, no rebirth glitch. One sentence: *"NO PATTERN ON FILE — POLICY CLOSED AT SUBSCRIBER REQUEST. The clinic's welcome loop does not play. Nobody comes. You did read the label."* One way out, `Close the book`, and it opens the **front door** rather than the ship's drawer — there is no ship to go back to. |
| **…and the thread is closed, not deleted** (#640) | same link → press through to the front door | That captain is still on the shelf, with their retirees, their selfies and every banked berth. What is gone is **Continue**: it will not resume that run, and if it was your only one the door offers a new voyage instead. Another captain's thread is untouched. Loading a moment you banked still works, and should — a save is a moment that was still being lived. |
| **The handle it all hangs off, unchanged** (#640) | [`?archive=1&land=1`](https://esoinila.github.io/SpaceSails-play/map?archive=1&land=1) → walk aft to the **DEEP HOLD** | **Nothing was added to the handle.** `⏻ PURGE NODE — RESIDENT PATTERN NOT RECOVERABLE` is still the whole of the warning, there is still no confirmation dialog, and the line at the pull still names no resident. The collar — which a *bad* throw buys you before you pull, never a good one — is still the only way to know whose number is on the jar. If you ever see an "are you sure?", something has gone wrong. |
| **The other place to be reckless** (#640) | [`?nopattern=1`](https://esoinila.github.io/SpaceSails-play/map?nopattern=1) | A live run that has already spent its last life, and nothing in the world will mention it again. Go and do something dangerous. |

Choose the place you die in by combining: `?nopattern=1&death=collector` (the BUSTED ladder),
`?nopattern=1&death=suffocated&dock=the-tilt&land=1` (a landing party). There is deliberately no
`?place=` — the world you boot into decides that, as it has since #621.

---

## 2 · The half that must look exactly the same

- **Every other death.** The ordinary wake is untouched: clinic, bill, rustbucket, succession, the filing
  line, the rebirth glitch, the clinic's second page. The Client guard for #640 presses the *same* button
  on the *same* death with a pattern still on file and gets the clinic — that control is what makes the
  rest of it mean anything.
- **Both arcs' capstones.** `berth-code` and `policy-terms` are pinned **word for word** by
  `TheKaamosCapstoneTextIsUnchanged` / `TheNebulaCapstoneTextIsUnchanged`: "the card stops explaining"
  was not paid for by moving the explanation into a capstone. Each arc still has its own reveal to give,
  which was the actual complaint on #422.
- **The convergence plate.** `art/convergence.jpg` did not change by a pixel, although the entire body it
  sits over was replaced. It was painted against the *shape* of the reveal rather than any sentence in it,
  which is what a plate is for.
- **Both shard texts.** `holders-tell` and `adjuster-tell` read exactly as they always did; the two
  sentences simply exist once now, as consts the shards quote back, so the card can never drift from the
  world.
- **The whole boot.** #640 adds one dev-start URL and one field to the boot's query holder. The world
  sweep moved **exactly one line, and it was an addition** — the new URL's own, which hashes identically
  to `?death=impact` because `?nopattern=` builds no different world. The query sweep re-pinned all 86,
  which is what a new `BootQuery` field always does there.
