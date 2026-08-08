using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #784 · THE SHORT REST — sitting down relaxes and heals, and a short rest is SHORT.
///
/// <para>Owner ruling, live 2026-08-08: <i>"Sitting down relaxes and heals."</i> And, naming the whole
/// mechanic in one clause the next minute: <i>"it is like short rest in TTRPG."</i> That is the design
/// anchor and this file is written to it — bounded recovery, taken in the field at a table, interruptible
/// (the haulier can walk into the middle of it; a short rest is not a safe room), and categorically smaller
/// than the LONG rest, which is the ship's own bunk.</para>
///
/// <h3>Where it sits in an economy that already exists</h3>
///
/// <para>The nerve already has a relief seam with four members on it —
/// <see cref="NerveModel.DrinkKind.GalleyTot"/> at 10, the bar's pour at 18, a shared glass at 24, a pill at
/// 20 and a night in the bunk at 40. This file does not add a fifth kind, because a short rest is not a
/// DOSE: it is a run of beats with a ceiling, and the ceiling is the whole point. What it hands back is
/// counted in <see cref="NervePips"/> — the gauge moves in whole pips or it does not move, which is #480's
/// law, and a rest that quietly banked fractions would be exactly the invisible float-drift that law was
/// written to end.</para>
///
/// <para><b>The ceiling is set under the bunk on purpose.</b> <see cref="NervePipCapPerWatch"/> × 10 is 30,
/// and a night's sleep is 40. A captain must never be able to sit in a canteen instead of going home, and
/// the arithmetic — not a comment — is what stops them.</para>
///
/// <h3>The healing half</h3>
///
/// <para>Blows are <see cref="CaptainCondition"/>'s five, counted on the excursion. A short rest knits ONE
/// of them and never two (<see cref="HitsCapPerWatch"/>), and it takes <see cref="BeatsPerHitKnit"/> beats
/// of actually sitting there to do it. You leave a table better than you sat down at it; you do not leave it
/// whole.</para>
///
/// <h3>What a drink does, and what it does not</h3>
///
/// <para>Owner, filing the ritual: <i>"a bought drink/meal plausibly multiplies the nerve half (the
/// counter-to-table ritual pays)."</i> So the pour multiplies the RATE
/// (<see cref="DrinkNerveMultiplier"/>) and never the ceiling. The glass does not make the rest deeper — it
/// makes it land before the room takes it off you, which is the honest thing to sell in a mechanic whose
/// defining feature is that a stranger can end it mid-way. It does nothing at all to the healing half: a
/// drink has never knitted a rib.</para>
///
/// <para>Pure and deterministic: no clock, no <c>Random</c>, no world. The client owns how many beats have
/// been sat and what has already been given back this watch; this owns the arithmetic and the words.</para>
/// </summary>
public static class ShortRest
{
    // ── THE NERVE HALF ────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many whole nerve pips one seated watch-beat eases, dry. One — the smallest move the
    /// gauge can make, because #480 forbids any smaller one and a rest that moved nothing would be a
    /// mechanic the player could not see working. FLAGGED for the owner's tuning.</summary>
    public const int NervePipsPerBeat = 1;

    /// <summary>What a bought pour multiplies the per-beat ease by — the counter-to-table ritual, priced.
    /// It multiplies the RATE and never <see cref="NervePipCapPerWatch"/>: the glass buys you the rest
    /// SOONER, which in an interruptible mechanic is worth paying for. FLAGGED for the owner's tuning.
    /// </summary>
    public const int DrinkNerveMultiplier = 2;

    /// <summary>The most nerve, in whole pips, one watch of sitting down can ever give back. Three pips is
    /// 30 on the gauge — deliberately UNDER <see cref="NerveModel.SleepRestore"/> (40), so the ship's bunk
    /// is always the better rest and no captain can sit in a canteen instead of going home. This is the
    /// number that makes a short rest short. FLAGGED for the owner's tuning.</summary>
    public const int NervePipCapPerWatch = 3;

    // ── THE HEALING HALF ──────────────────────────────────────────────────────────────────────────────

    /// <summary>How many seated beats it takes to knit one of <see cref="CaptainCondition.MaxHits"/>. Three
    /// — long enough that a captain who sits down bleeding has to actually sit there, short enough that a
    /// watch spent waiting is never wasted. FLAGGED for the owner's tuning.</summary>
    public const int BeatsPerHitKnit = 3;

    /// <summary>How many blows a single watch of sitting can knit. ONE. You leave the table better than you
    /// sat down at it and you do not leave it whole; the rest of the five come back the way they always
    /// have, by going home. FLAGGED for the owner's tuning.</summary>
    public const int HitsCapPerWatch = 1;

    // ── WHAT ONE BEAT GIVES BACK ──────────────────────────────────────────────────────────────────────

    /// <summary>What a single seated beat handed over.</summary>
    /// <param name="NervePips">Whole nerve pips eased — <see cref="NervePips"/>' own unit, so the client
    /// multiplies by <see cref="Core.NervePips.PipUnit"/> and spends it through the ordinary relief seam.</param>
    /// <param name="Hits">Blows knitted, 0 or 1.</param>
    /// <param name="CapSpent">Nothing was handed over AND the reason is the ceiling rather than the clock —
    /// the captain has had everything a short rest has. Told out loud, because a beat that silently gives
    /// nothing is indistinguishable from a mechanic that has broken (#603).</param>
    public readonly record struct Eased(int NervePips, int Hits, bool CapSpent)
    {
        /// <summary>Did this beat give anything at all?</summary>
        public bool Anything => NervePips > 0 || Hits > 0;
    }

    /// <summary>
    /// #784 · ONE SEATED BEAT.
    ///
    /// <para>Pure arithmetic against the ledger the caller keeps. Everything that decides the answer is an
    /// argument, so a guard can walk a whole watch of beats and watch the ceiling bite rather than trusting
    /// the numbers in the comments above.</para>
    /// </summary>
    /// <param name="beatsAlreadySat">How many beats have been sat at a table THIS watch, before this one,
    /// from zero. It is what the healing counts on: the knit lands on every
    /// <see cref="BeatsPerHitKnit"/>th beat.</param>
    /// <param name="withDrink">Whether there is a bought pour in front of the captain (see
    /// <see cref="DrinkNerveMultiplier"/>). Food is not a pour and a drunk captain is not resting — both are
    /// the caller's to decide, because both are facts about a world this file does not have.</param>
    /// <param name="nervePipsEasedThisWatch">Pips this watch has already handed back.</param>
    /// <param name="hitsKnitThisWatch">Blows this watch has already knitted.</param>
    /// <param name="hitsTaken">Blows the captain is carrying. Nothing to knit is not a cap being spent — it
    /// is an unmarked captain, and the game must not tell them their rest ran out when it did not.</param>
    public static Eased Beat(
        int beatsAlreadySat,
        bool withDrink,
        int nervePipsEasedThisWatch,
        int hitsKnitThisWatch,
        int hitsTaken)
    {
        int wanted = NervePipsPerBeat * (withDrink ? DrinkNerveMultiplier : 1);
        int roomLeft = Math.Max(0, NervePipCapPerWatch - Math.Max(0, nervePipsEasedThisWatch));
        int pips = Math.Max(0, Math.Min(wanted, roomLeft));

        int beat = Math.Max(0, beatsAlreadySat) + 1;
        bool due = BeatsPerHitKnit > 0 && beat % BeatsPerHitKnit == 0;
        bool hurt = hitsTaken > 0;
        bool hitRoom = Math.Max(0, hitsKnitThisWatch) < HitsCapPerWatch;
        int hits = due && hurt && hitRoom ? 1 : 0;

        // The ceiling is only NEWS when it is actually the thing in the way. A captain who is not hurt has
        // not run out of healing, and a beat that is simply not the third one has not run out of anything.
        bool capSpent = pips == 0 && hits == 0 && (roomLeft == 0 || (due && hurt && !hitRoom));
        return new Eased(pips, hits, capSpent);
    }

    /// <summary>
    /// #784 · A WHOLE SITTING, folded — what <paramref name="beats"/> beats at a table are worth in total.
    ///
    /// <para>Nothing in the game calls this: the client applies one <see cref="Beat"/> at a time, because a
    /// short rest is interruptible and a total paid up front would be a promise the room is allowed to
    /// break. It exists so the ceiling can be stated as one number in a guard and read as one number by the
    /// owner tuning it.</para>
    /// </summary>
    public static Eased Rest(int beats, bool withDrink, int hitsTaken)
    {
        int pips = 0, hits = 0;
        bool capSpent = false;
        for (int i = 0; i < Math.Max(0, beats); i++)
        {
            Eased e = Beat(i, withDrink, pips, hits, Math.Max(0, hitsTaken - hits));
            pips += e.NervePips;
            hits += e.Hits;
            capSpent |= e.CapSpent;
        }
        return new Eased(pips, hits, capSpent);
    }

    // ── WHAT IT FEELS LIKE ────────────────────────────────────────────────────────────────────────────
    //
    // Short, and appended after whatever the room already said this beat. The silence lines (#757) are the
    // event; this is the body's footnote to them, and it must never be longer than the sentence it follows.

    /// <summary>A dry beat that eased something.</summary>
    public const string EasedLine = "Sitting down is doing what sitting down does. Your hands are quieter.";

    /// <summary>The same beat with a bought pour in front of you — #783's relaxation register, in one
    /// sentence rather than three, because it plays every beat and a paragraph would wear out.</summary>
    public const string EasedWithDrinkLine =
        "The cold glass sweats into your hand and your shoulders come down half an inch.";

    /// <summary>A beat that knitted one of the five.</summary>
    public const string KnitLine =
        "Whatever you did to yourself out there stops asking about it, at least while you are sitting on it.";

    /// <summary>…and what a captain is told when the ceiling has been reached. A short rest is short, and
    /// the game says so rather than handing back a beat that silently did nothing.</summary>
    public const string CapSpentLine =
        "Sitting longer is only sitting longer now. What is left of you comes back in a bunk, not a chair.";

    /// <summary>The body's footnote to this beat, or null when the beat had nothing to say. One place, so a
    /// caller cannot decide for itself which sentence a recovery earns.</summary>
    public static string? Line(in Eased eased, bool withDrink)
    {
        if (eased.Hits > 0)
        {
            return KnitLine;
        }
        if (eased.NervePips > 0)
        {
            return withDrink ? EasedWithDrinkLine : EasedLine;
        }
        return eased.CapSpent ? CapSpentLine : null;
    }

    /// <summary>What the nerve ledger calls this, so a recovery reads back as legibly as a loss (#480:
    /// nothing may move the gauge anonymously).</summary>
    public const string LedgerLabel = "you sat down for a while";

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return EasedLine;
        yield return EasedWithDrinkLine;
        yield return KnitLine;
        yield return CapSpentLine;
        yield return LedgerLabel;
    }
}
