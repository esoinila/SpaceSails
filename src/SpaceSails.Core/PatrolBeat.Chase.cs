using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: #835's other branch — he comes after you, he catches you, and the top rung back to the sky (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── #835 · THE OTHER BRANCH: HE COMES AFTER YOU, AND HE CATCHES YOU ───────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, having been escorted four times off one floor by one man who then
    // just kept walking: "they need to catch us .... like reevers :-D we could use that code :-D" — and,
    // in the same breath, the constraint that keeps the register: "just no damage by default :-D".
    //
    // EVERY RUNG BELOW IS PROCEDURE. He does not shout, he does not draw anything, and nothing that happens
    // when he reaches you touches the body. He says a number into a radio, he runs — badly, for a man of his
    // age — he takes your arm, and then the building does the only two things it knows how to do: it walks
    // you to a lift, and if you have used up its patience it walks you all the way to the sky.
    //
    // WHAT IS DELIBERATELY NOT HERE. There is no alert state, no floor-wide hunt, no second guard summoned
    // by the radio, and no memory of a chase past the shift. #618's canon constraint is unspent: this is a
    // cover that can blow, not a detection meter. One man comes; one man gives up or one man catches you.

    /// <summary>
    /// #835 · WHY HE IS RUNNING. THE WHOLE LIST, and a captain can read every one of them off their own
    /// evening — which is the difference between an escalation and an ambush.
    ///
    /// <para><see cref="None"/> is what a sighting buys and what almost every stop on almost every floor
    /// will ever be. Nothing in the game hands out any other value except the three places named on each
    /// member, so "ambient rounds never chase a walking captain" is a fact about this enum rather than a
    /// clause somebody remembered to write.</para>
    /// </summary>
    public enum Provocation
    {
        /// <summary>Nothing has happened. He hails, he walks over, he reads (#833), and the worst of it is
        /// a walk back to the car (#804). THE DEFAULT, and the only answer an ordinary round ever gets.</summary>
        None = 0,

        /// <summary>You have walked off on him before, this watch, and now you are doing it again. The first
        /// one is free and stays free — <see cref="WalkedAwayLine"/>, the man who stops and writes — because
        /// a guard who followed you the first time would make #833's whole approach a trap.</summary>
        WalkedAwayTwice,

        /// <summary>He watched you take a hasp off a door with a gun. The one crime these floors actually
        /// have a verb for (#803's DESIGNATE, and the shot that goes in <c>GunfireHeard</c>'s own ledger),
        /// so it is the one crime listed. It is deliberately not "he heard it somewhere": the ledger already
        /// keeps the noise, and a man who came running at a bang three corridors away would be the
        /// floor-wide hunt this feature does not have.</summary>
        SeenAtTheHasp,

        /// <summary>He has written you down <see cref="EscortsAWatchAllows"/> times already this watch. The
        /// owner's own finding: <i>the fiction strains when the same guard books the same face four times
        /// and just keeps walking.</i></summary>
        BookedTooManyTimes,
    }

    /// <summary>Is this a reason to come after somebody? The one gate, so the answer cannot be spelled twice.
    /// A caller with <see cref="Provocation.None"/> in its hand is a caller that must walk away.</summary>
    public static bool EarnsIt(Provocation why) => why != Provocation.None;

    /// <summary>#835 · HOW MANY TIMES ONE WATCH WILL WRITE YOU DOWN AND STILL JUST WALK YOU TO THE LIFT.
    /// Three, because the owner hit four in one evening and the fourth was the one that read as a bug in the
    /// fiction. It is the SAME number on both sides of the ladder — the stop that stops being an escort is
    /// the stop that becomes the way out of the building — so the escalation cannot grow a second threshold
    /// that drifts from this one. FLAGGED for the owner's tuning.</summary>
    public const int EscortsAWatchAllows = 3;

    /// <summary>Has this watch already spent its patience? Asked with the escorts BEFORE the current one, so
    /// the fourth stop is the different one.</summary>
    public static bool BookedTooOften(int escortsThisWatch) => escortsThisWatch >= EscortsAWatchAllows;

    /// <summary>#835 · How many hails a captain may simply walk away from before the round stops accepting
    /// it. One: the first is #833's own beat and its own tell, and taking that away would make the approach
    /// something to be frightened of rather than something to decide about.</summary>
    public const int HailsYouMayWalkAwayFrom = 1;

    /// <summary>Is walking off this time the one too many? Asked with the walk-aways INCLUDING this one.</summary>
    public static bool WalkingOffEarnsIt(int walkedAwayThisWatch) =>
        walkedAwayThisWatch > HailsYouMayWalkAwayFrom;

    /// <summary>#835 · The beat of radio before he moves. He does not run and talk at once — he stands still
    /// for this, which is the warning, and it is the same courtesy the hail was: a second and a half in which
    /// a captain who is quick can already be round the corner. FLAGGED.</summary>
    public const double CallItInSeconds = 1.6;

    /// <summary>
    /// #835 · HOW FAST HE COMES. Twice his round's own pace and still comfortably short of the captain's own
    /// legs (9.0 du/s on the deck), which is not a compromise — it is the whole of rung five: <b>a captain
    /// who runs for the lift gets to the lift.</b> He catches people who hesitate, people who are cornered
    /// and people who thought a locked door would open for them. FLAGGED for the owner's tuning, and it may
    /// never be raised past the captain's walk without the escape stopping being an escape.
    /// </summary>
    public const double AfterYouSpeed = WalkSpeed * 2;

    /// <summary>#835 · How long he will keep it up. He is a retired cop and not a wolf: past this he is
    /// blowing, and a captain who has stayed ahead of him for forty seconds has genuinely got away.</summary>
    public const double AfterYouSecondsCap = 40.0;

    /// <summary>#835 · How far ahead of him you have to get before he has lost you. Comfortably outside the
    /// eye's own reach at the moment he gives up, so the end of it is never a man staring straight at you and
    /// deciding not to bother.</summary>
    public const double LosesYouBeyondDu = MarkerSightDu - 4.0;

    /// <summary>#835 · Is he still coming? Same shape as <see cref="StillComing"/>, on purpose: the walk-up
    /// and the run are the same question at two speeds, and a second shape would be a second set of edge
    /// cases. False ends it in a clean escape and never in a despawn — he is still standing there.</summary>
    public static bool StillAfterYou(
        double secondsAfterYou, double guardX, double guardY, double captainX, double captainY)
    {
        if (double.IsNaN(secondsAfterYou) || secondsAfterYou > AfterYouSecondsCap)
        {
            return false;
        }
        double dx = captainX - guardX, dy = captainY - guardY;
        return (dx * dx) + (dy * dy) <= LosesYouBeyondDu * LosesYouBeyondDu;
    }

    /// <summary>#835 · Has he got you? <see cref="ReeverChase.Caught"/> verbatim — the owner named that code
    /// and this is it, down to the radius: a catch is two bodies touching, and #469 already settled what
    /// touching means on this ground. A hand on your arm and a hand on your throat are the same geometry;
    /// everything that makes them different happens afterwards, and none of it is in the body.</summary>
    public static bool HasYou(double guardX, double guardY, double captainX, double captainY) =>
        ReeverChase.Caught(guardX, guardY, captainX, captainY);

    /// <summary>#835 · THE WARNING THAT THE NEXT THING IS HAPPENING. Four words of radio, said standing
    /// still, and then he moves — the whole beat exists so that being run down is something a captain
    /// watched start.</summary>
    public const string CallsItInLine =
        "📻 Two fingers on the shoulder mic, a floor number and a direction, and that is the entire message. " +
        "Then the clipboard goes under his arm and he comes after you, and he is quicker than he looks.";

    /// <summary>#835 · What he says when he has you, and it is always the reason. #603's law: a consequence
    /// names why. Nothing in any arm of this is a threat; every arm of it is a man filling in the part of
    /// the form that asks what happened.</summary>
    public static string WhyHeCame(Provocation why) => why switch
    {
        Provocation.WalkedAwayTwice => WalkedOffTwiceLine,
        Provocation.SeenAtTheHasp => SawYouAtTheHaspLine,
        Provocation.BookedTooManyTimes => BookedLine(EscortsAWatchAllows + 1),
        _ => WalkedOffTwiceLine,
    };

    /// <summary>He asked you to hold on, twice, and twice you kept walking.</summary>
    public const string WalkedOffTwiceLine =
        "🔒 \"Twice,\" he says, when he has the breath for it. \"You walked off on me twice.\" He is not " +
        "angry about it. He is filling in the part of the form that asks why.";

    /// <summary>He watched the hasp come off. Said the way a man reads an address back over a counter.</summary>
    public const string SawYouAtTheHaspLine =
        "🔒 \"I watched you do that,\" he says. \"So there's no version of tonight where I didn't.\" He does " +
        "not say what it was and he does not need to; he says it the way you read an address back over a " +
        "counter.";

    /// <summary>The clipboard, turned round. The number in the sentence is the constant, so the man and the
    /// rule can never come to be counting different evenings.</summary>
    public static string BookedLine(int times) =>
        $"🔒 \"That's {times} times tonight,\" he says, and turns the clipboard round, and it genuinely is " +
        $"{times} lines with times written against them. \"I can keep doing these. I'd rather not.\"";

    /// <summary>#835 · The card. It names what has happened to your arm, because that is the whole of what
    /// has happened to you.</summary>
    public const string CaughtLabel = "👮 A HAND ON YOUR ARM";

    /// <summary>
    /// #835 · WHAT BEING CAUGHT IS. Evidence, and then it stops (§13.9) — and the evidence is that nothing
    /// has been done to you. <b>Zero damage is not a number this card hides; it is the thing the card is
    /// about.</b>
    /// </summary>
    public static string CaughtCard(string plate) =>
        $"{plate} — and he is out of breath, which is somehow the worst of it. No lamp, no raised hand, " +
        "nothing swung. He takes your upper arm the way a steward takes an elbow, waits until you have " +
        "stopped moving, and lets go of it again.\n\n" +
        "Nothing is broken and nothing is bleeding, and none of that was ever on the table. What has " +
        "happened is that the rest of your evening has become somebody's shift, and there is a form for it.";

    /// <summary>#835 · The read a catch hands back: the reason, on the card, with what it costs under it.
    /// Same two-armed <see cref="Read"/> the wallet uses, so the client raises the identical card for a
    /// challenge and for a catch and neither can grow a presentation of its own.</summary>
    /// <param name="plate">Who has you (<see cref="PlateOf"/>).</param>
    /// <param name="why">What earned it.</param>
    /// <param name="escortsThisWatch">Escorts BEFORE this one — the same number
    /// <see cref="BookedTooOften"/> is asked at the walk, so the sentence and the sim cannot disagree about
    /// which floor this walk ends on.</param>
    public static Read TheGuardHasYou(string plate, Provocation why, int escortsThisWatch)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return new(
            false, WhyHeCame(why), CaughtLabel, CaughtCard(plate),
            BookedTooOften(escortsThisWatch) ? KickOutDueLine : EscortLine);
    }

    /// <summary>#835 · A CLEAN ESCAPE, AND HE IS STILL STANDING THERE. Never a despawn: the man who stopped
    /// running is the same man, on the same rota, on the same floor, and he has now seen which way you went.</summary>
    public const string LostYouLine =
        "👮 The boots stop somewhere behind you. There is one more word on the radio, and then the corridor " +
        "is a corridor again — except that somebody now knows which way you went.";

    // ── #835 · THE TOP RUNG: BACK TO THE SKY ──────────────────────────────────────────────────────────
    //
    // Owner: "If we get kicked out then maybe we end up back to the surface :-D" — and the reason it is the
    // right top rung is that it costs the game's two oldest currencies without inventing anything: the walk
    // back is half a tank of air, and the way back in is a piece of paper you no longer have.
    //
    // It is the escort (#833) that simply keeps going: to the car, into the car, up. The building does not
    // do drama; it does paperwork and doors.

    /// <summary>#835 · What the card says the walk is this time. He is not pressing the button for your
    /// floor.</summary>
    public const string KickOutDueLine =
        "👮 This time he does not press the button for your floor. He gets in with you, and the panel is " +
        "already lit for the top.";

    /// <summary>#835 · THE BIG TEXT, as the tube doors part. Authored by the owner, verbatim, and it wears
    /// the descent plate's own typography — the same wall stencil that says <c>−162 m</c> on the way down,
    /// because the ejection is the same building saying the same kind of thing. No red, no klaxon, no shake:
    /// the vacuum banner immediately under it is the actual punchline.</summary>
    public const string KickedOutBigText = "KICKED OUT";

    /// <summary>#835 · The one quiet line, once, at pulse size, as the doors close behind you. Owner's copy,
    /// verbatim and unglyphed — a sentence this flat does not want a picture in front of it.</summary>
    public const string DoorsCloseLine = "The doors do not slam. They just close.";

    /// <summary>#835 · How long the big text stays painted on the shed wall before it comes down. Long enough
    /// to read twice standing still, short enough that it never becomes wallpaper — #694's lesson, paid for
    /// by a facility name that drew on all thirteen floors. FLAGGED.</summary>
    public const double KickedOutPlateSeconds = 14.0;

    /// <summary>#835 · THE PASS, TAKEN — and SAID, because a possession that leaves the satchel silently is
    /// the sim doing something the prose never mentioned. Only ever said when there was one to take.</summary>
    public const string PassRevokedLine =
        "🪪 At the top he puts his hand out for the pass and does not give it back. \"You'll want a different " +
        "one of these,\" he says, and puts it in his breast pocket with the others.";

    /// <summary>What the book keeps of that. It is the mechanic's whole future consequence stated as a fact:
    /// the way back in is now somebody else's paperwork.</summary>
    public const string PassRevokedNote =
        "The site pass went into a man's breast pocket at the top of the cage. Getting back down this shaft " +
        "means turning up with a different piece of paper.";

    /// <summary>What the book keeps of the ejection itself. It records what happened and never the
    /// mechanic.</summary>
    public const string KickOutNote =
        "Walked to the car, taken all the way up and put out through the tube onto the regolith. No voices " +
        "and nothing taken but the pass. The tank started running again on the doorstep.";

    /// <summary>Every authored sentence this feature owns, for the canon grep. It walks THIS, so a line
    /// added tomorrow is checked tomorrow — #709's discipline, and the reason a catalog is a method rather
    /// than a list somebody remembers to update.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string plate in Plates)
        {
            yield return plate;
        }
        yield return HeardLine;
        yield return HailLine;
        yield return WalkedAwayLine;
        yield return PumpsLine;
        yield return EscortDoneLine;
        yield return EscortCutLine;
        yield return EscortHeldLine;
        yield return ChallengeLabel;
        yield return ChallengeCard("◈ A PLATE");
        yield return SatisfiedLine;
        yield return WrongSiteLine("luna");
        yield return WrongPaperLine;
        yield return NothingLine;
        yield return EscortLine;
        yield return EscortNote;
        yield return CallsItInLine;
        yield return WalkedOffTwiceLine;
        yield return SawYouAtTheHaspLine;
        yield return BookedLine(EscortsAWatchAllows + 1);
        yield return CaughtLabel;
        yield return CaughtCard("◈ A PLATE");
        yield return LostYouLine;
        yield return KickOutDueLine;
        yield return KickedOutBigText;
        yield return DoorsCloseLine;
        yield return PassRevokedLine;
        yield return PassRevokedNote;
        yield return KickOutNote;
        yield return BadgeIssuedLine;
        yield return BadgeGist;
        yield return BadgeTitle("luna");
        yield return BadgeTier;
    }
}
