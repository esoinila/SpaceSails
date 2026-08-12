using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #821 · THE CUBICLE YOU LOCK FROM INSIDE — the catch, the plate, the round that walks past, and the tap.
///
/// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we might
/// want to hide from guards in one toilet cubicle we lock from inside :-D"</i>, and then, an hour later:
/// <i>"also a function to wash hands with some film noir comment at the end ... about if we ever feel like
/// our hands are clean :-D"</i>.</para>
///
/// <h3>THE SENTRY'S OWN LAW</h3>
///
/// <para><b>It buys time, not safety.</b> That sentence is the whole design and every branch in this file
/// is written to keep it literally true. A guard who SAW you duck in knocks, then waits, and the challenge
/// is standing there when you open the door — the lock has bought you the length of a round and nothing
/// else. A guard who did not see you walks their beat past an OCCUPIED plate without breaking stride,
/// because a locked door is only suspicious to somebody who is already suspicious (#804's own ladder:
/// suspicion before pursuit, and a round has no reason to run after anybody on sight). There is no timer, no
/// meter, no roll, and no way for a door to make a captain safe.</para>
///
/// <h3>Why the lock is honest</h3>
///
/// <para>Owner: <i>"The lock is honest: E on the door inside = locked, plate flips to OCCUPIED. No timer
/// theatrics; the tension is the bootsteps on the tracker while you sit still."</i> So there is nothing here
/// that decays, nothing that can be failed, and nothing that fires by itself. The catch is a boolean the
/// captain owns; the only clock in the scene is the round's, on the instrument (#591's fan) the captain
/// already has.</para>
///
/// <para>Pure and wordless about state: this file holds no lock. It answers what a lock MEANS — who may pass
/// a shut door, what a plate says, what a man does when he watched you shut it — and the client owns the one
/// set of shut doors, replayed onto every deck rebuild the way a shot hasp already is (#803).</para>
/// </summary>
public static class CubicleLock
{
    // ── THE PLATE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What is stencilled on a cubicle's leaf when nobody is in it. The verb is on it (#783), and
    /// it is a VERB and not a state: a plate that reads FREE tells a captain nothing they cannot see.</summary>
    public const string VacantPlate = "🚪 VACANT · STEP IN";

    /// <summary>…and once the catch is over. The one word every washroom door in the world uses, which is
    /// exactly why it is the right one — it says nothing about who is behind it.</summary>
    public const string OccupiedPlate = "🚪 OCCUPIED";

    /// <summary>The plate a cubicle's leaf reads, given whether the catch is over. One function, so the door
    /// a guard walks past and the door the captain presses can never wear two different signs.</summary>
    public static string PlateFor(bool locked) => locked ? OccupiedPlate : VacantPlate;

    // ── THE CATCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// MAY THIS PRESS TURN THE CATCH? Only from INSIDE, which is the whole of what a cubicle lock is — there
    /// is no key, no card and no way to shut somebody else in.
    ///
    /// <para>Asked of Core rather than decided at the press, so a dev row, a captain's [E] and any guard
    /// that proves the law are all reading one answer.</para>
    /// </summary>
    /// <param name="inside">Whether the captain is standing in the cell's own box.</param>
    public static bool MayTurnTheCatch(bool inside) => inside;

    /// <summary>The catch going over. Said, because a control that changes the world silently is
    /// indistinguishable from a broken one (#603) — and because the sentence is the tell that the tension has
    /// started rather than ended.</summary>
    public const string LockedLine =
        "🚪 You turn the catch. The plate on the outside flips to OCCUPIED, the partition stops being a wall "
        + "and starts being a door, and you can hear the whole floor through it.";

    /// <summary>…and coming back off it.</summary>
    public const string UnlockedLine =
        "🚪 You turn the catch back. The plate reads VACANT again, which is a thing anybody in the room can "
        + "read from the far wall.";

    /// <summary>
    /// #821 · WHAT A SHUT DOOR DOES TO SOMEBODY ON THE OUTSIDE OF IT — and it is the only thing it does.
    ///
    /// <para>The refusal is SAID rather than left as a body sliding off a partition: a captain who walks
    /// into a locked cubicle and is simply stopped has learned nothing about why, and this particular fact —
    /// <i>somebody is in there</i> — is the one the whole feature is played against.</para>
    /// </summary>
    public const string RefusedFromOutsideLine =
        "🚪 The door does not give. There is somebody on the other side of it, and the plate has said so "
        + "since before you walked up.";

    /// <summary>What pressing an unlocked cubicle's leaf from the room says. Nothing happens, and it says
    /// what WOULD: the catch is on the inside, so you have to be on the inside.</summary>
    public const string StepInsideFirstLine =
        "🚪 The catch is on the inside, which is where catches are. Step in first.";

    // ── THE HIDE ──────────────────────────────────────────────────────────────────────────────────────
    //
    // #804's ladder, unchanged and deliberately not extended: a round is somebody doing a job, a sighting
    // starts a CHALLENGE, and nothing in this game can start a chase. A locked door does not add a rung to
    // that ladder; it changes WHERE the man is standing when the rung he was already on gets taken.

    /// <summary>
    /// DOES THIS GUARD OPEN THE DOOR? <b>No.</b> Never, under any circumstance this game has.
    ///
    /// <para>It is a constant rather than a branch on purpose. A contract guard halfway through a shift does
    /// not haul open an occupied WC door on a floor where the worst thing that has ever happened is a line
    /// in a book — and the day somebody wants him to, it will be a different class of guard with its own
    /// numbers (the owner's own carve-out: <i>"no more ninja guards :-D ... those only maybe at the most
    /// sensitive levels"</i>) rather than a clause bolted onto this one.</para>
    /// </summary>
    public static bool OpensALockedCubicle(bool sawYouDuckIn) => false;

    /// <summary>
    /// DOES HE WAIT AT THE DOOR? Only if he watched you shut it.
    ///
    /// <para>This is the sentry's own law as one predicate. A man who saw somebody he had already registered
    /// step into a cubicle and hear the catch go over has a reason to be standing outside it; a man who came
    /// round the corner afterwards is looking at a door with OCCUPIED on it, which is what a washroom door
    /// says all day.</para>
    ///
    /// <para><paramref name="sawYouDuckIn"/> is the caller's own fact, taken off the SAME sightline the
    /// challenge is taken off (<see cref="PatrolBeat.Notices"/>) at the moment the catch went over — never a
    /// second opinion about who can see what.</para>
    /// </summary>
    public static bool WaitsAtTheDoor(bool sawYouDuckIn) => sawYouDuckIn;

    /// <summary>…and the other half of the same fact, named for the reader: a round that did not see you
    /// carries on walking it. The plate is not evidence.</summary>
    public static bool WalksPast(bool sawYouDuckIn) => !WaitsAtTheDoor(sawYouDuckIn);

    /// <summary>He knocks. Once, and then he waits — which is worse. Nothing in it threatens and nothing in
    /// it explains; it is a man being unhurried about paperwork through a partition.</summary>
    public const string KnockLine =
        "👮 Two knuckles on the partition, unhurried. \"Take your time.\" Then nothing — no second knock, no "
        + "footsteps going away, and the shadow under the door does not move.";

    /// <summary>What the captain has actually bought, said once when the boots stop outside. It is the
    /// sentry's own law in the captain's own words, and it is the line that stops the lock reading as an
    /// escape.</summary>
    public const string BoughtTimeLine =
        "🚪 A locked door buys time. It has never once bought safety, and the man outside knows the "
        + "difference better than you do.";

    /// <summary>The round that never noticed, going past. Said sparingly and only for a guard who was in the
    /// room — it is the reward for having got in unseen, and a captain who never hears it has not learned
    /// that the plate is doing nothing for them.</summary>
    public const string WalkedPastLine =
        "👣 Boots come into the room, stop at the basins, and go out again. Whatever he read on the plate, "
        + "it was the same thing it says all day.";

    /// <summary>…and the door opening onto a man who was already there. The challenge itself is #804's and
    /// this only names the moment, because a second card here would be two men doing one job.</summary>
    public const string OpenedOnHimLine =
        "🚪 You turn the catch and pull, and he is exactly where the shadow said he was — clipboard under "
        + "the arm, hand already out.";

    // ── SITTING IT OUT ────────────────────────────────────────────────────────────────────────────────

    /// <summary>What a beat spent sitting still in a cubicle sounds like. Owner's own note on where the
    /// tension lives: <i>"No timer theatrics; the tension is the bootsteps on the tracker while you sit
    /// still."</i> So not one of these is about the door, the catch, or how long you have — they are the
    /// room, heard through a partition, by somebody with nothing to do but listen.</summary>
    public static readonly IReadOnlyList<string> SatAndListenedLines =
    [
        "A while goes by. The cistern above you refills, decides it is happy, and stops. Somewhere past the "
        + "partition a tap is dripping onto porcelain at about the rate of a slow pulse.",
        "Nothing. The extractor over the door changes note, the way extractors do, and the whole room gets a "
        + "little quieter without getting any warmer.",
        "A while goes by. Two people come in, wash, and go out again talking about a rota, and neither of "
        + "them says anything you could take to anybody.",
        "Nothing. The light in here is on a timer and has just decided you are not in the room; you move a "
        + "hand and it changes its mind.",
    ];

    /// <summary>Which of them this beat gets. Told rather than left to be inferred from a control that went
    /// quiet (#603/#757), and by ordinal rather than by a die because the wait beat's counter is the one the
    /// whole seated machine is already keyed on.</summary>
    public static string NothingHappens(int beat)
    {
        IReadOnlyList<string> pool = SatAndListenedLines;
        return pool[((beat % pool.Count) + pool.Count) % pool.Count];
    }

    /// <summary>Sitting down on it. The FIRST clause confirms the state change (#783's law) and the rest is
    /// what a captain would actually notice, which is that the door is a door and not a wall.</summary>
    public const string SatDownLine =
        "🚻 You sit down on the closed lid with your knees against the door. It is a partition, a catch and "
        + "eighty centimetres of floor, and it is the most privacy this building has offered you yet.";

    /// <summary>…and getting up off it.</summary>
    public const string StoodUpLine = "You stand up off the lid.";

    /// <summary>What the strip calls the seat.</summary>
    public const string SeatPlate = "🚻 IN A CUBICLE";

    /// <summary>Where you are, when you are sitting in one.</summary>
    public const string Setting = "A cubicle in the block's public washroom";

    /// <summary>What sitting a while in here is FOR — and it says the sentry's law in the label, because the
    /// label is what a captain reads before they decide to spend the beat.</summary>
    public const string WaitLabel = "SIT IT OUT — listen to the room";

    /// <summary>…and the way off it.</summary>
    public const string StandLabel = "Stand up";

    /// <summary>
    /// THE CUBICLE, as an <see cref="Encounter.Scene"/> — two moves and no third, on the machine every other
    /// seat in this game already runs on.
    ///
    /// <para>The move IDS are <see cref="SittingAlone.Wait"/> and <see cref="SittingAlone.Stand"/> and
    /// deliberately not new ones, for the reason the bench and the office chair made the same choice: a
    /// saved game and a guard both key on the id. Only the labels and the words are the washroom's.</para>
    /// </summary>
    public static Encounter.Scene TheCubicle() => new(
        "hive:cubicle",
        SeatPlate,
        Setting,
        SatDownLine,
        [
            new(SittingAlone.Wait, WaitLabel),
            new(SittingAlone.Stand, StandLabel, Says: StoodUpLine),
        ]);

    /// <summary>Where a cubicle's watch-scoped state is keyed from, so a cell and a canteen top with the same
    /// ordinal on the same floor can never share a wait counter. The office chair's own reason
    /// (<see cref="RingOffice.ApproachOrdinalBase"/>), one room along, and far enough past it that no ring
    /// will grow into the gap.</summary>
    public const int ApproachOrdinalBase = 9000;

    /// <summary>This cubicle's ordinal for the wait counter.</summary>
    /// <param name="roomNumber">The ring room it stands in.</param>
    /// <param name="cellIndex">Its ordinal in that room.</param>
    public static int ApproachOrdinal(int roomNumber, int cellIndex) =>
        ApproachOrdinalBase + (roomNumber * 64) + cellIndex;

    // ── THE TAP ───────────────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "also a function to wash hands with some film noir comment at the end ... about if we ever feel
    // like our hands are clean :-D"
    //
    // A short beat, one sentence, no card and no fanfare — the customer line's own delivery. Seeded per
    // (site, watch, beat) exactly as the canteen's regulars are, so a basin repeats itself only the way a
    // tired thought does.

    /// <summary>
    /// #821 · THE LINE POOL, VERBATIM FROM THE ISSUE. Authored on #821 and copied here without a comma
    /// moved — a string drifted in copy is a joke delivered wrong, and there is a guard that hashes these
    /// six against the comment for exactly that reason.
    ///
    /// <para>The sixth is allowed to brush the restore theme and — standing canon law — does not STATE it.
    /// Nothing confirms what no sensor ever saw. It is also the RARE one; see <see cref="WashWeights"/>.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> WashLines =
    [
        "You wash your hands. The water runs grey, then clear. The water is easily satisfied.",
        "The soap does what soap does. It was not the dirt you were thinking about.",
        "You scrub past the knuckles. Whatever it is, it does not come off at a sink.",
        "Clean, says the water. The water has not read your case notes.",
        "You dry your hands on the roller towel, and somewhere a ledger stays exactly as it was.",
        "Good as new. You have met new. You know better.",
    ];

    /// <summary>
    /// How often each line comes up, as tickets in one hat. Five ordinary lines and the last one weighted
    /// LOW by the owner's ruling: the line that brushes the restore is a thing you hear once in a while and
    /// then wonder about, and a punchline on a fifth of the presses is a punchline nobody hears any more.
    ///
    /// <para>A weight table rather than a re-roll, so the distribution is a fact a guard can count rather
    /// than a loop somebody has to reason about. FLAGGED for the owner's tuning.</para>
    /// </summary>
    public static readonly IReadOnlyList<int> WashWeights = [4, 4, 4, 4, 4, 1];

    /// <summary>The size of the hat. Public so a guard can pin the weighting without reaching into it.</summary>
    public static int WashTickets
    {
        get
        {
            int total = 0;
            foreach (int w in WashWeights)
            {
                total += w;
            }
            return total;
        }
    }

    /// <summary>
    /// WHICH LINE THIS WASH CLOSES ON. Seeded on (site, watch, beat) — the canteen regulars' own idiom, and
    /// the same three facts, so two basins on one floor on one shift say the same thing and the shift turning
    /// over changes what the room has to say.
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="watch">The frozen canteen watch.</param>
    /// <param name="beat">Which wash of this excursion it is — so pressing twice is two lines and not one
    /// said twice.</param>
    public static string WashLine(string bodyId, long watch, int beat)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        int ticket = DiceRule.Roll(
            DiceRule.Seed($"hive:wash:{bodyId}:{beat}", watch), WashTickets).Face;

        for (int i = 0; i < WashLines.Count; i++)
        {
            ticket -= WashWeights[i];
            if (ticket <= 0)
            {
                return WashLines[i];
            }
        }

        // Unreachable: the tickets are the sum of the weights. Kept honest rather than thrown — a pool that
        // could not answer would be a press that did nothing, which is the one thing #603 forbids.
        return WashLines[^1];
    }

    /// <summary>#821 · How much a wash gives back, in nerve pips, and it is ONE. Washing your hands is a
    /// beat, not a rest: the pips tick once so the press has a consequence a captain can see, and the ceiling
    /// below is what stops a basin being a bench.</summary>
    public const int WashPips = 1;

    /// <summary>…and how many washes a watch pays for. One. A row of four basins is a room and not an
    /// income, and a captain standing at a tap pressing [E] for the rest of a shift should get the sentences
    /// and nothing else. FLAGGED for the owner's tuning.</summary>
    public const int WashPipsPerWatch = 1;

    /// <summary>What the strip calls the act where the control is drawn.</summary>
    public const string WashLabel = "Wash your hands";

    // ── THE PROSE SWEEP ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Every sentence this file can put on a screen, for the canon sweep. It walks the POOL rather
    /// than listing it, so a seventh line added tomorrow is swept tomorrow — #709's discipline, and the
    /// reason a catalog is a method rather than a list somebody remembers to update.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string line in WashLines)
        {
            yield return line;
        }
        foreach (string line in SatAndListenedLines)
        {
            yield return line;
        }
        yield return SatDownLine;
        yield return StoodUpLine;
        yield return SeatPlate;
        yield return Setting;
        yield return WaitLabel;
        yield return StandLabel;
        yield return VacantPlate;
        yield return OccupiedPlate;
        yield return LockedLine;
        yield return UnlockedLine;
        yield return RefusedFromOutsideLine;
        yield return StepInsideFirstLine;
        yield return KnockLine;
        yield return BoughtTimeLine;
        yield return WalkedPastLine;
        yield return OpenedOnHimLine;
        yield return WashLabel;
    }
}
