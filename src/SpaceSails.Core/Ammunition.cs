using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #603 · WHAT IS IN THE ROUNDS YOU FOUND. Owner, across a design run:
///
/// <para><i>"sometimes we find like 6 rounds ... and take them ... those rounds might be special types
/// even."</i> · <i>"I like the choice of variety... some lab found exploding rounds might be too dangerous
/// to use to close by targets."</i> · <i>"the rounds from an illegal lab can do weird things and make one
/// wonder the purpose they were needed in the first place ... like what monster / opponent needs such
/// :-D"</i> · <i>"those rounds would go through several reevers if in group also"</i> · <i>"some special
/// ammo that only uses one round per reever"</i></para>
///
/// <h3>The rule the whole table obeys</h3>
/// <para><b>Variety, not upgrade</b> (owner ruling). A round is defined by WHERE IT IS WRONG, never by how
/// much better it is. The lab round is devastating down a corridor and will kill you at arm's length; the
/// issue stores are safe everywhere and run out.</para>
///
/// <h3>And the ammunition is EVIDENCE</h3>
/// <para>A round is designed AGAINST something, so its specification implies a target — and the game never
/// says what. A two-stage penetrator that arms after travel and does its work on the far side of the first
/// thing it meets raises exactly one question, and nobody answers it. That is the same technique as the
/// collar sized for a neck no animal has: the object testifies and nothing narrates
/// (<c>docs/art-manifest-hive.md</c>, inference horror).</para>
/// </summary>
public static class Ammunition
{
    /// <summary>A kind of round. The id is what the satchel and the vault carry.</summary>
    public readonly record struct Kind(
        string Id,
        string Name,
        double MinimumRangeDu,
        int Penetrates,
        int HitsToKill,
        string Spec)
    {
        /// <summary>Does firing at something this close endanger the firer?</summary>
        public bool TooCloseFor(double rangeDu) => rangeDu < MinimumRangeDu;
    }

    /// <summary>The ordinary issue: what the armoury sells and the tube racks. Safe at any range, stops the
    /// first thing it hits, and you will need a lot of it.</summary>
    public static readonly Kind Issue = new(
        "issue", "issue ball", MinimumRangeDu: 0, Penetrates: 1, HitsToKill: SentryBot.RoundsPerReever,
        "Standard sentry ball. Nothing remarkable, which down here is itself remarkable.");

    /// <summary>#603 · THE LAB ROUND. Owner: <i>"only uses one round per reever"</i>, <i>"would go through
    /// several reevers if in group"</i>, <i>"too dangerous to fire to too near targets"</i>.
    ///
    /// <para>All three are the same fact stated three ways: it arms after travel and does its work AFTER
    /// penetration. That is why one is enough, why a line of them goes down together, and why it must not be
    /// fired at something already on top of you.</para></summary>
    public static readonly Kind LabTwoStage = new(
        "lab-2stage", "two-stage penetrator", MinimumRangeDu: 7.0, Penetrates: 4, HitsToKill: 1,
        "Arms after travel; the second charge fires on the far side of the first thing it meets. " +
        "Proofed to a mass bracket well above anything on the manifest. No maker, no lot number, and a " +
        "use-case written by somebody who assumed the reader already knew.");

    /// <summary>Every kind the game knows, issue first.</summary>
    public static IReadOnlyList<Kind> All { get; } = [Issue, LabTwoStage];

    /// <summary>Look one up by the id a satchel item carries. Unknown ids fall back to issue rather than
    /// throwing — an edited save should not cost a captain their game.</summary>
    public static Kind ById(string? id)
    {
        foreach (Kind k in All)
        {
            if (string.Equals(k.Id, id, StringComparison.Ordinal))
            {
                return k;
            }
        }
        return Issue;
    }

    /// <summary>
    /// #603 · Whether a bot loaded with this may fire at something <paramref name="rangeDu"/> away, and why
    /// not if it may not.
    ///
    /// <para>Owner's ruling on what happens below the minimum: <i>"the gun complains but also gives override
    /// option to just fire"</i>. So this REFUSES and explains; the override is the caller's to offer, and
    /// taking it is a decision the captain made out loud — the same consent idiom as venting a compartment
    /// with somebody in it.</para>
    /// </summary>
    public static (bool Allowed, string? Refusal) MayFire(Kind kind, double rangeDu)
    {
        if (!kind.TooCloseFor(rangeDu))
        {
            return (true, null);
        }

        return (false,
            $"🔫 It will not fire. The {kind.Name} arms after travel and that target is inside the " +
            $"arming distance — at {rangeDu:F0} du the second charge goes off level with you, not with it. " +
            "Back off, or tell it to fire anyway.");
    }

    /// <summary>How many targets one round of this accounts for in a line — the owner's <i>"would go through
    /// several reevers if in group"</i>. Never fewer than one.</summary>
    public static int TargetsPerRound(Kind kind) => Math.Max(1, kind.Penetrates);

    /// <summary>What the captain reads when they pick some up. Describes the ROUND and stops: it never says
    /// what it was made for, because that is the question worth leaving open.</summary>
    public static string FoundLine(Kind kind, int count) =>
        kind.Id == Issue.Id
            ? $"🔫 {count} loose rounds of issue ball, still in the clip somebody left them in."
            : $"🔫 {count} rounds, and they are not issue. {kind.Spec}";
}
