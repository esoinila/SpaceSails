using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #618 / #582 slice 1 · <b>A GUARD, BUT NOT A GOOD ONE.</b> The man at the Hive's top-level door — the way
/// DOWN out of the top pressurised floor (<see cref="UndergroundComplex.TopPressurisedFloor"/>), which the
/// badge (#605) and the numpad (#602) already assumed somebody was standing in front of.
///
/// <para>Owner, 2026-09-29: <i>"there IS a guard, but not a good one."</i> Four ways past him, all of them meant
/// to work, and none of them a fight: a badge got through contacts, a lure out onto the surface where the tide
/// has him, a window in which he is simply not there, and a word — contractor — said the way he was told to
/// expect it.</para>
///
/// <h3>What this file is</h3>
///
/// <para>The whole of him that can be pure: which of three ways he is keeping the door this window
/// (<see cref="For"/>, one seeded answer per (ground, opening window), no <see cref="System.Random"/>), whether
/// the word works this window, the paper that passes, what the tide decides at the mouth of the tube, the tags
/// the durable register keeps about him, the dev cheat, and every word he or the page says about him — Fable's,
/// verbatim, and nothing else (<see cref="AllProse"/>).</para>
///
/// <h3>What he is not</h3>
///
/// <para>A threat. He never draws, never calls anybody, and the tier above him never appears here. The worst he
/// does is remember a face, which closes the talk and nothing else. He is in FRONT of the door, never instead of
/// it: passing him opens no gate the paper does not open — the pad and the card rows are the building's and are
/// untouched (#602, #605). Nobody in any line says what he is for, and the reserved word is absent.</para>
/// </summary>
public static class GateGuard
{
    // ── WHICH WAY HE IS KEEPING THE DOOR ─────────────────────────────────────────────────────────────────

    /// <summary>The three ways the door is kept, one per (ground, opening window).</summary>
    public enum Posting
    {
        /// <summary>He stands by the door in a coat issued to somebody bigger. Most windows.</summary>
        Posted,

        /// <summary>The chair by the door has his coat on it and nobody in it. The door is a door.</summary>
        Absent,

        /// <summary>He walks a short beat between the door and the canteen: the door is unmanned while he is
        /// away and manned when he is back.</summary>
        OnRound,
    }

    /// <summary>One window's answer: how the door is kept, and whether the word works on him this window.</summary>
    public readonly record struct Visit(Posting Posting, bool TalkWorks);

    /// <summary>Out of ten windows, how many find him posted. The rest split between the round and the empty
    /// chair — "most windows" for the first, "some windows" for each of the other two (the brief's own words).
    /// Flagged for tuning like every other rarity on this ground.</summary>
    public const int PostedInTen = 6;

    /// <summary>…and how many of the other four find him walking his round rather than gone.</summary>
    public const int OnRoundInTen = 2;

    /// <summary>The word works one window in three (the brief's "~1 in 3").</summary>
    public const int TalkWorksOneIn = 3;

    /// <summary>
    /// <b>ONE SEEDED STATE PER (GROUND, OPENING WINDOW).</b> Pure: the same ground in the same window is the
    /// same man on every machine, after every reload, and a test can drive every branch of it.
    /// </summary>
    /// <param name="groundId">The site whose door this is — the body the building is under.</param>
    /// <param name="openingWindow">The watch (<c>PatronRota.WatchIndex</c>) the excursion first reached the
    /// door in.</param>
    public static Visit For(string groundId, long openingWindow)
    {
        ArgumentNullException.ThrowIfNull(groundId);
        int face = DiceRule.Roll(DiceRule.Seed($"gate:posting:{groundId}", openingWindow), 10).Face;
        Posting posting = face <= PostedInTen ? Posting.Posted
            : face <= PostedInTen + OnRoundInTen ? Posting.OnRound
            : Posting.Absent;
        bool talk = DiceRule.Roll(DiceRule.Seed($"gate:talk:{groundId}", openingWindow), TalkWorksOneIn).Face == 1;
        return new Visit(posting, talk);
    }

    // ── THE WORDS — Fable canon, 2026-09-30, verbatim. Crews do not write canon. ──────────────────────────

    /// <summary>His plate, drawn over him.</summary>
    public const string Plate = "Gate";

    /// <summary>The card's title when the captain reaches his door while he is posted.</summary>
    public const string CardTitle = "THE MAN AT THE DOOR";

    /// <summary>Approach, posted — told once per excursion, on the card.</summary>
    public const string ApproachLine =
        "A man in a coat that was issued to somebody bigger. He looks at your hands, then at your face, then at your hands again.";

    /// <summary>A badge shown through #605's own show verb — any badge that passes there.</summary>
    public const string BadgeLine = "He reads the badge longer than a badge takes to read. 'Go on, then.'";

    /// <summary>The move on the card: talk your way in.</summary>
    public const string TalkLabel = "TALK YOUR WAY IN";

    /// <summary>…and it works.</summary>
    public const string TalkWorkedLine =
        "'Contractor,' you say, and he nods the way a man nods at a word he has been told to expect.";

    /// <summary>…and it does not. After this the move is absent at this ground for the rest of the run.</summary>
    public const string TalkFailedLine = "'Nobody said.' He does not move, and he does not forget your face.";

    /// <summary>The move on the card: make a noise.</summary>
    public const string NoiseLabel = "MAKE A NOISE";

    /// <summary>…and he comes off the wall. Then he follows.</summary>
    public const string NoiseLine =
        "You put something metal against something hollow. He comes off the wall like a man who has been waiting all shift for a reason.";

    /// <summary>The tide takes him — told once, from the surface.</summary>
    public const string TideTakesHimLine =
        "He follows you into the light, which is the one thing he was told not to do. The tide does the rest. You do not watch.";

    /// <summary>The tide is down, and he stops.</summary>
    public const string TideDownLine =
        "He stops at the mouth of the tube, where the coat says he is allowed to stop, and goes back the way he came.";

    /// <summary>Absent — told once on reaching the door.</summary>
    public const string AbsentLine = "The chair by the door has a coat on it. The coat has no man in it.";

    /// <summary>On his round, when he walks away from the door — told once.</summary>
    public const string RoundLine =
        "He walks his round the way a man walks to the canteen. Six minutes, if the canteen is where you think it is.";

    /// <summary>The field book, on the first pass by any route, once per ground.</summary>
    public const string BookLine = "Past the man at the door. He is not the reason nobody comes down here.";

    /// <summary>…filed on the pin.</summary>
    public const string BookGlyph = "📍";

    /// <summary>
    /// Every word this feature puts on a screen, in one list: the plate, the title, the two labels it owns and
    /// the ten sentences. The badge move wears #605's own label (<see cref="GuardStop.ShowLabel"/>) and is not
    /// this file's word. Nothing else is authored and nothing else may be said.
    /// </summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Plate;
        yield return CardTitle;
        yield return TalkLabel;
        yield return NoiseLabel;
        yield return ApproachLine;
        yield return BadgeLine;
        yield return TalkWorkedLine;
        yield return TalkFailedLine;
        yield return NoiseLine;
        yield return TideTakesHimLine;
        yield return TideDownLine;
        yield return AbsentLine;
        yield return RoundLine;
        yield return BookLine;
    }

    // ── THE MOVES ON HIS CARD ────────────────────────────────────────────────────────────────────────────

    /// <summary>The badge, through #605's own verb and label.</summary>
    public const string ShowMove = "gate:show";

    /// <summary>The word.</summary>
    public const string TalkMove = "gate:talk";

    /// <summary>The noise.</summary>
    public const string NoiseMove = "gate:noise";

    /// <summary>
    /// <b>THE MOVES ON THE CARD, AND ONLY THE ONES THAT EXIST.</b> No move is ever drawn and refused: a badge
    /// that would not pass is not offered, and a spent TALK is ABSENT rather than greyed (the Kosh law). MAKE
    /// A NOISE is always there while he is at the door.
    /// </summary>
    /// <param name="aBadgePasses">Whether the wallet holds a pass this door reads as good
    /// (<see cref="ThePassThatPasses"/>).</param>
    /// <param name="faceRemembered">Whether a failed word at this ground is on the register
    /// (<see cref="FaceTag"/>).</param>
    public static IReadOnlyList<Encounter.Move> MovesOnTheCard(bool aBadgePasses, bool faceRemembered)
    {
        var moves = new List<Encounter.Move>(3);
        if (aBadgePasses)
        {
            moves.Add(new Encounter.Move(ShowMove, GuardStop.ShowLabel, Encounter.Requirement.SatchelItem));
        }

        if (!faceRemembered)
        {
            moves.Add(new Encounter.Move(TalkMove, TalkLabel, Rolled: true));
        }

        moves.Add(new Encounter.Move(NoiseMove, NoiseLabel));
        return moves;
    }

    /// <summary>
    /// #605 · <b>THE PAPER THAT PASSES HERE</b> — the first pass in the wallet that the building's own ladder
    /// (<see cref="WalletChoice.WhatHappens"/>) answers <see cref="WalletChoice.Outcome.Worked"/> for, on the
    /// floor he keeps. Any tier the ladder takes; nothing is judged twice.
    /// </summary>
    public static Satchel.Item? ThePassThatPasses(
        string bodyId, int level, long watch, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (Satchel.Item item in carried ?? [])
        {
            if (item.Kind == Satchel.Kind.Badge
                && WalletChoice.WhatHappens(bodyId, level, watch, item) == WalletChoice.Outcome.Worked)
            {
                return item;
            }
        }

        return null;
    }

    // ── THE REGISTER'S TWO TAGS ──────────────────────────────────────────────────────────────────────────

    /// <summary>He has your face: the talk is absent at this ground for the rest of the run.</summary>
    public static string FaceTag(string groundId) => $"gate:face:{groundId}";

    /// <summary>The captain has been past him at this ground by some route — the book's once.</summary>
    public static string PastTag(string groundId) => $"gate:past:{groundId}";

    // ── THE ROUND ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How long he stands at the door between rounds, in the seconds his legs walk on.</summary>
    public const double AtTheDoorSeconds = 90.0;

    /// <summary>How long he is away from the door once he sets off — six minutes, which is what the line says
    /// and what the walk is timed to. The walk back is started early by exactly as long as the walk out took,
    /// so the door is unmanned for the six minutes the captain was told about.</summary>
    public const double AwaySeconds = 6.0 * 60.0;

    /// <summary>When a man at the canteen end of his round sets off back: the away clock has run to six minutes
    /// less the walk it will take to get home (taken to be the walk out).</summary>
    public static bool HeHeadsBack(double awaySeconds, double walkOutSeconds) =>
        awaySeconds >= AwaySeconds - Math.Max(0.0, walkOutSeconds);

    /// <summary>How near the door he has to be for the door to be manned.</summary>
    public const double MannedWithinDu = 2.0;

    /// <summary>How near him the captain has to come to have reached his door — the reach the card goes up at.</summary>
    public const double ReachDu = 6.0;

    /// <summary>…and how far away the captain has to have walked before reaching him again raises the card again.
    /// A band and not one number, so a captain standing on the line is not carded every frame.</summary>
    public const double LeftHimDu = 10.0;

    // ── THE MOUTH OF THE TUBE ────────────────────────────────────────────────────────────────────────────

    /// <summary>What the tide does with a man standing at the mouth of the tube.</summary>
    public enum AtTheMouth
    {
        /// <summary>He is standing there, deciding nothing.</summary>
        Standing,

        /// <summary>The tide is up, and it has him. Told once; never drawn.</summary>
        Taken,

        /// <summary>The tide is down; he stops where the coat says he may, and goes back.</summary>
        GoesBack,
    }

    /// <summary>
    /// How long he stands at the mouth before he turns back: one of the tide's own mean gaps
    /// (<see cref="ReeverTide.MeanGapSeconds"/>), and no number of this file's. Long enough that a tide that is
    /// running will usually hand one up while he stands there, short enough that a quiet ground sends him home.
    /// </summary>
    public const double AtTheMouthSeconds = ReeverTide.MeanGapSeconds;

    /// <summary>
    /// <b>THE TIDE DECIDES.</b> The tide is UP when one of its Old Ones is on the field while he is standing in
    /// the light; it takes him then. If the mouth's time runs out with the field quiet — or on a ground that
    /// runs no tide at all — the tide was down, and he goes back.
    /// </summary>
    public static AtTheMouth TheTideAnswers(double secondsAtTheMouth, bool aTideOneIsUp) =>
        aTideOneIsUp ? AtTheMouth.Taken
        : secondsAtTheMouth >= AtTheMouthSeconds ? AtTheMouth.GoesBack
        : AtTheMouth.Standing;

    // ── THE DEV START ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Read <c>guard=posted|absent|round</c> off an address — the <see cref="SpikeIt.CheatIn"/> way: a
    /// dev latch that writes no world at parse time and no field on the page. It forces HOW the door is kept
    /// and never whether the word works, which stays the window's own.</summary>
    public static Posting? CheatIn(string? uri)
    {
        int q = uri?.IndexOf('?', StringComparison.Ordinal) ?? -1;
        if (uri is null || q < 0)
        {
            return null;
        }

        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (pair.StartsWith("guard=", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair["guard=".Length..]).ToLowerInvariant() switch
                {
                    "posted" or "1" => Posting.Posted,
                    "absent" or "0" => Posting.Absent,
                    "round" => Posting.OnRound,
                    _ => null,
                };
            }
        }

        return null;
    }
}
