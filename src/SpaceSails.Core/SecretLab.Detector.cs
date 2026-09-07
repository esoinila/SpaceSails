using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #585 · THE DETECTOR GETS WARMER. Owner: <i>"the detector should also give detecting readings near
/// it."</i>
///
/// <para>The probe was all-or-nothing: stand on the exact square and it PINGS, stand anywhere else on a
/// 310 x 260 field and it says nothing at all. That is not a search, it is a lottery with 4 000 tickets —
/// which is why the owner could only find a lab by already knowing it was there. Now there is a bearing
/// to walk and a needle to watch, and it only wakes on a moon there is a reason to search, because a
/// detector that hums everywhere hands the player every lab in the system for free.</para>
///
/// <para>Split out of <c>SecretLab.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class SecretLab
{
    /// <summary>#585 · THE DETECTOR GETS WARMER. Owner: <i>"the detector should also give detecting readings
    /// near it."</i>
    ///
    /// <para>The probe was all-or-nothing: stand on the exact beach-comber square and it PINGS, stand on one
    /// of the eight around it and it shrieks, stand anywhere else on a 310 x 260 field and it says nothing at
    /// all. That is not a search, it is a lottery with 4 000 tickets — which is why the owner could only find
    /// a lab by already knowing it was there.</para>
    ///
    /// <para>A real detector is a gradient. This is the hot-and-cold every treasure hunt has run on since
    /// they were invented, and it turns a field into something you can WORK: pick a bearing, walk it, watch
    /// the reading, turn when it cools. The exact square still pings — that is still the moment — but now
    /// there is a way to steer toward it.</para>
    ///
    /// <para>It only wakes on a moon you have a reason to search (see <see cref="MoonWorthLookingAt"/>),
    /// because a detector that hums on every world would hand the player every lab in the system for free and
    /// make the clue chain pointless.</para></summary>
    public enum Reading { Silent, Faint, Steady, Strong, Screaming }

    /// <summary>How far out the detector says anything at all.</summary>
    public const double DetectorRange = 62.0;

    /// <summary>What the needle is doing at this distance from the hidden door.</summary>
    public static Reading ReadingAt(double distance) => distance switch
    {
        <= 7.0 => Reading.Screaming,
        <= 18.0 => Reading.Strong,
        <= 34.0 => Reading.Steady,
        <= DetectorRange => Reading.Faint,
        _ => Reading.Silent,
    };

    /// <summary>What the captain hears. Written so the DIRECTION of change is the information — "it climbs",
    /// "it falls away" — because a number would be a map and this is meant to be a search.</summary>
    public static string ReadingLine(Reading reading, bool warmer) => reading switch
    {
        Reading.Faint => warmer
            ? "📻 The detector finds something to say — one slow tick, then another. Not nothing. Not yet anything."
            : "📻 The ticking thins out and stops. Whatever it was, it is behind you now.",
        Reading.Steady => warmer
            ? "📻 A steady return under your boots — metal, buried, and far too regular to be ore. It gets no quieter as you walk."
            : "📻 The return drops back to a tick. You have walked past the shoulder of it.",
        Reading.Strong => warmer
            ? "📻 The needle stops pretending. Something big and made is under this ground, and it is close."
            : "📻 The needle eases off. Close, still — but not as close as you were.",
        Reading.Screaming => warmer
            ? "📻 The detector screams and will not stop. You are standing on it. Sweep the squares here and probe."
            : "📻 It screams on, quieter by a hair. Do not wander — it is within a few paces of your boots.",
        _ => "",
    };

    /// <summary>#585 · WHICH MOON A CLUE NAMES. Owner, having found a lab only because he knew it was there:
    /// <i>"We will be needing some kind of clue in the plot arc to the radar to really find it in reasonable
    /// time in the game :-D ... now we kind of found it by just knowing it is here somewhere :-D"</i>
    ///
    /// <para>He is right, and the loop was half-built: the tracker already draws a wide vague wash for a lab
    /// (a tip narrows a search, it never ends one) — but that wash was gated on having ALREADY found the
    /// place, so it helped on a return visit and did nothing on the first. The clue had no way in.</para>
    ///
    /// <para>This closes it. A clue found anywhere in the gumshoe chain — a file in a facility, a docket in a
    /// ruin, something a dead specialist's family knows — names a MOON THAT ACTUALLY HAS ONE. Never a moon
    /// that does not: a game that sends you three days out on a false lead is not being mysterious, it is
    /// wasting your evening.</para></summary>
    public static string? MoonWorthLookingAt(IReadOnlyList<string> candidates, ulong seed)
    {
        if (candidates is null || candidates.Count == 0)
        {
            return null;
        }

        var withLabs = new List<string>();
        foreach (string body in candidates)
        {
            if (Present(body))
            {
                withLabs.Add(body);
            }
        }
        if (withLabs.Count == 0)
        {
            return null;
        }
        return withLabs[(int)(seed % (ulong)withLabs.Count)];
    }

    /// <summary>How a clue reads when it finally names somewhere. It gives a MOON, never a spot — the walk is
    /// still yours, and the tracker will only ever wash the general area.</summary>
    public static string LeadLine(string moonName) =>
        $"🔎 A place name, in among the rest of it, in a context that makes no sense unless somebody was " +
        $"running something there: {moonName}. Written the way people write a thing they are not supposed " +
        "to have written down. Your tracker will know roughly where to wash when you are standing on it.";

    public static bool IsProximitySquare(in Placement p, int squareX, int squareY) =>
        System.Math.Abs(squareX - p.DoorSquareX) <= 1 && System.Math.Abs(squareY - p.DoorSquareY) <= 1;

    /// <summary>Whether a probe of this square lands exactly on the hidden door — the reveal.</summary>
    public static bool IsDoorSquare(in Placement p, int squareX, int squareY) =>
        squareX == p.DoorSquareX && squareY == p.DoorSquareY;
}
