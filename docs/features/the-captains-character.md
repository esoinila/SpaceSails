# The captain's character — a ledger, not a meter

> *"It is the great trap of finding an honest criminal. I guess as a pirate one would have an emergency
> suit nearby and have a way to break the seal you sleep behind. In a sense our captain's actions could
> have this kind of TableTop RolePlaying Game morality map. How honest is the captain, what lines does he
> cross and in what situations he did so. That demeanor would certainly play a part in how much the crew
> trust their captain. Then again unit cohesion of going through hard times is pretty deep. Often soldiers
> report that they only care about the other people fighting beside them, so there probably would be a bit
> of that also. Also the captain would have a similar dilemma in HR of finding the honest trustworthy new
> replacement criminal pirate. Wrong choice in who to trust might be fatal as an undercover mole could have
> them all arrested or worse. Could we have a ledger about the moral character of the captain — a
> moral-o-meter in the ledger?"*
>
> — the owner, 2026-07-29, immediately after being handed a valve that opens his own crew's cabins to space

**Status:** filed, not built. This is the design; the first slice is proposed at the bottom.

---

## 0. The provocation

The captain's-consent gate exists because the atmosphere board can now be pointed inboard. The moment it
could, the owner did not ask for a safety — he asked what kind of person pulls that handle, and what the
people who saw him do it think afterwards.

That is the right question and it is bigger than venting. It touches boarding, the paperwork, what you tell
the wire, whether you file an honest report or a profitable one, and every wreck where the sensor could not
tell a survivor from the infestation and you pulled anyway.

---

## 1. The first correction: a map, not a meter

The owner said "morality map" and then, half a breath later, "moral-o-meter". **Those are different things
and the first one is right.**

A scalar — good ↔ evil, honest ↔ crooked — collapses precisely what makes this interesting. It cannot
express *"he has never once lied to the crew and he vented a compartment with a man in it"*, which is a
completely coherent captain and a far more interesting one than any point on a line. Worse, a meter invites
grinding: players optimise a number the moment they can see it move.

**So the source of truth is a LEDGER OF CROSSINGS.** Each entry records:

| field | why |
|---|---|
| **the line** | which principle it was — honesty, the crew's safety, the articles, a promise given |
| **the situation** | what was on the table when you crossed it. Starving is not the same as greedy. |
| **the cost** | what it bought, and what it cost, in the same entry |
| **who saw** | the whole hinge of §3 |

A read-out can be *derived* from that ledger for the UI. It must never be the thing that is stored.

### The idiom already exists

This is not a new UI problem. **The nerve ledger already works exactly this way** — a gauge nobody can read
directly, backed by a running list of `+1 the airlock closes behind you` / `−1 it was already in the room
with you`, and the game shows you the ledger *at the moment it explains the most* (the death card). Every
lesson learned building that applies here:

- entries carry their **reason**, so a number is never mysterious
- the list is short and recent, because a full transcript is not readable
- it is surfaced when it *means* something, not permanently in a corner

The captain's character is the same object with a longer memory and no decay.

---

## 2. The pirate's answer to the trap

> *"as a pirate one would have an emergency suit nearby and have a way to break the seal you sleep behind"*

This is a mechanic and a characterisation in one line, and it should be **a fact about the crew rather than
an item on a shelf**. A pirate crew sleeps suited-adjacent. They have all served under someone who thought
about the valve.

Consequences, all of them good:

- **Venting your own crew is unreliable.** It is a threat, not an execution. That is far more interesting:
  the handle you can pull and they know you can pull, and everyone aboard knows it might not work.
- **It makes the crossing about intent, not outcome.** The ledger records that you opened the valve on a
  berth with a man in it. Whether he got his helmet on in time is a separate fact, and the crew judge the
  first one.
- **It gives the crew a counter that is not "the game says no".** Nothing on the board is disabled. The
  crew's answer to a bad captain is competence, not a locked button.
- **It sets up the inverse scene**: a crew member who did *not* have a suit to hand, and why.

---

## 3. Trust is not morality — and who was watching is the hinge

> *"Then again unit cohesion of going through hard times is pretty deep. Often soldiers report that they
> only care about the other people fighting beside them."*

Correct, and it means **two axes, not one**:

- **CHARACTER** — the ledger of what you did.
- **COHESION** — what this crew has survived *with* you.

They are earned differently and they protect against different things. Cohesion is bought with shared
hardship: a boarding that went wrong and everyone came home, a soak waited out together, a debt collector
outrun. It is entirely possible — and should be — to have:

- a **monstrous captain with a devoted crew**, because he has brought them through four things that should
  have killed them, and
- a **decent captain nobody trusts**, because they have never been through anything together and he keeps
  filing honest reports that cost everyone their share.

That second one is the design's proof that this is not a morality system with a right answer.

### The hinge: witnesses

**A line crossed with nobody watching goes in the ledger; a line crossed in front of the crew goes in the
ledger AND into trust.**

This is what makes the whole thing play rather than merely record. It means:

- the wreck's compartments are a different decision when a crewman is standing at the board with you
- the away team is a *witness list* as well as a roster
- there is a real, ugly, entirely in-genre incentive to be alone when you do certain things — and the game
  never says so out loud

The captain's own knowledge of what he did belongs to the **nerve**, which already exists and already
charges him privately. Trust belongs to the crew, who can only charge him for what they saw.

---

## 4. Hiring an honest criminal

> *"the captain would have a similar dilemma in HR of finding the honest trustworthy new replacement
> criminal pirate. Wrong choice in who to trust might be fatal as an undercover mole could have them all
> arrested or worse."*

The design rule this needs is one the game already knows how to write: **you cannot verify. You can only
accumulate evidence.**

That is the wreck-cause system exactly — a sensor that cannot tell a survivor from the infestation, evidence
that corroborates or contradicts, and a conclusion the captain has to reach and can be wrong about. A recruit
carries a hidden disposition. What you get to see is:

- their **papers**, which may be a cover story (and the news wire can contradict them, same as a wreck's log)
- who **vouches** for them, and what that voucher's own ledger looks like
- how they **behave** over time — and time is expensive, which is the owner's next point

**The mole must be rare and catastrophic**, not a coin flip. A game where one in three hires is an informant
teaches paranoia; a game where one in twenty is teaches *judgement*, and makes the tell worth reading.

---

## 5. Onboarding is the real cost

> *"at work we have these questionnaires about coder morale, Winningtemp, that measure the satisfaction of
> employees, as also there it is a big investment to truly onboard new employees. It is said that it takes
> like 6 months to make them truly productive."*

This is the mechanic that makes losing a crew member *hurt* in a way the current game cannot express. A
replacement is not a unit of the same value with a fresh name. They arrive:

- **slower** — the six-month ramp, compressed to whatever the game's clock makes legible
- **cohesion-zero** — they have been through nothing with you, so they are the least loyal person aboard
  regardless of how good the captain's ledger looks
- **unread** — you do not yet know what they are

Which means **the true cost of venting a compartment with your own man in it is not the man. It is the six
months.** That is a far better disincentive than any morality penalty, because the player feels it in the
schedule rather than in a scolding.

And the Winningtemp parallel is the right one for the UI: **a periodic, low-ceremony read of how the crew are
doing**, which is a signal to act on rather than a score. Not a report card on the captain — a temperature.

---

## 6. Why this belongs in *this* game

The owner's own framing: *"I do like that train of thought, since it is kind of philosophical and belongs
into the game."*

It does, and specifically because SpaceSails already refuses to tell the player what things mean. The
life-sign sensor will not say what is alive. The wreck's evidence will let you file a wrong cause. The
nerve gauge is quantised so you cannot read your own state precisely. A character system in this game must
obey the same law:

> **The ledger records what you did. It never tells you what you are.**

No alignment label, no "you are now Ruthless (3/5)". The crew's behaviour is the read-out. The player draws
the conclusion, and can be wrong about themselves — which is the most honest thing a game about this subject
can do.

Provenance note: the Ropecon TTRPG sheets (Somevaikuttajat Egyptissä, Fail Forward / sanity) are the owner's
own prior art here and the canonical reference for how these tables should *feel* — see the memory note on
the Ropecon system, and lane #226.

---

## 7. Proposed first slice

Small, shippable, and it earns its keep immediately because the crossings already exist in the code:

1. **`CaptainsLedger` in Core** — an append-only list of `Crossing(Line, Situation, Cost, Witnesses)`, pure
   and seeded, with the read-out derived and never stored.
2. **Wire the crossings the game ALREADY has**: venting a compartment holding a survivor, filing a wreck
   cause you know is wrong, boarding an authorized target, breaking a tow promise. These are all already
   decision points with an existing "you did it" moment — the ledger just listens.
3. **Witnesses = the away team roster** at the moment of the act. No new UI.
4. **Show it where the nerve ledger is shown** — at the death card first, because that is where it explains
   the most, exactly as the nerve ledger already proved.

Deliberately NOT in the first slice: crew trust, cohesion, hiring, moles, the six-month ramp. Those need a
crew model with individuals in it, and the ledger has to exist and feel right before anything depends on it.

---

## Open questions for the owner

1. **Does your own ship have unknowns?** The captain's-consent gate currently rests on the line *"nobody
   aboard her is a question mark"* — which is what makes the wreck's board a fast weapon and hers a slow
   one. A stowaway, or a crewman who missed muster, would break that cleanly and make her board as
   uncertain as a derelict's. Better story, weaker rule. Your call.
2. **Should the crew ever refuse?** A locked button is the cheap version. The expensive, better version is
   that they do it slowly, or badly, or one of them is not at their post afterwards.
3. **Is the ledger ever visible in full**, or only at moments like the death card? The nerve ledger's
   restraint is a large part of why it works.
