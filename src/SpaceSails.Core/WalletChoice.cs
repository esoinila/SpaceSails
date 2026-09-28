using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #836 · THE FLETCH WALLET — WHO ARE YOU TONIGHT, TO THIS MAN.
///
/// <para>Owner, evening playtest 2026-08-11, straight after passing his first badge check: <i>"I think I
/// should be able to pick the badge I show the guard... like Fletch ... suppose we have 4 different ID's ...
/// one of them real ... but not authorized for access to all places we roam"</i>.</para>
///
/// <para>Identity becomes INVENTORY. The wallet already held more than one piece of paper a guard's palm is
/// for — this site's pass, another site's pass, the cage's day-labour chit — and the read simply sorted them
/// and handed him the best one. That is the sim quietly playing the hand for the captain, which is the one
/// thing a gumshoe game may not do. So the question at a challenge stops being <i>do I have a badge</i> and
/// becomes <b>which name do I give him</b>.</para>
///
/// <h3>The 2026-08-08 ruling survives, because the choice moves EARLIER</h3>
///
/// <para>No TRY verb at the read, and no second paper once his hand is out. The choice happens during #833's
/// approach — he has said <i>hold on</i> and is crossing the floor — and the read on arrival is as automatic
/// as it ever was. It just reads WHAT WAS HANDED (<see cref="PatrolBeat.TheGuardReads"/>) rather than what
/// would have gone best.</para>
///
/// <h3>The informed-choice law, and the oracle it forbids</h3>
///
/// <para>Owner: <i>"when we select Id we should have some idea about which Id would be ok :-D"</i> — a blind
/// pick is a slot machine. But the idea comes from the CAPTAIN'S OWN KNOWLEDGE and from nowhere else, and
/// there are exactly two sources of it in this file:</para>
///
/// <list type="number">
/// <item><b>What is printed on the paper</b> (<see cref="Claims"/>, <see cref="NameOn"/>) — the face, the
/// site code, the tier. A paper never lies about what is printed on it; whether the printed thing is GOOD
/// HERE is the whole question, and the floor's own stencils are where that is answered.</item>
/// <item><b>What the book actually filed</b> (<see cref="HistoryLine"/>) — <i>worked here, twice</i>,
/// <i>refused on B2 · wrong site code</i>, <i>never shown</i>. Every one of those is a line this thread wrote
/// down at a read it went through, so a fresh captain reads <see cref="NeverShownLine"/> on every row.</item>
/// </list>
///
/// <para><b>There is deliberately no third source.</b> Nothing here asks the guard's own ladder what he WOULD
/// say — a hint that knew the answer before the man did would be the sim leaking through the fiction, and
/// the whole feature is a man reading his own wallet by lamplight.</para>
///
/// <h3>One ladder, two readers</h3>
///
/// <para><see cref="WhatHappens"/> is the ONE decision about how a read goes. The card's prose reads it
/// (<see cref="PatrolBeat.TheGuardReads"/>) and the book's note reads it (<see cref="ShownNote"/>), so the
/// sentence a captain is told and the line their book keeps can never come to disagree about the same
/// challenge — which is this repo's third named bug class, pointed at the one system whose entire register is
/// procedure.</para>
/// </summary>
public static partial class WalletChoice
{
    // ── WHAT A PALM IS FOR ────────────────────────────────────────────────────────────────────────────

    /// <summary>Is this the kind of thing a man on a rota puts his hand out for?
    ///
    /// <para>Two kinds, and the list is the guard's own ladder rather than a taste: a BADGE is the site
    /// vouching for a person and a CHIT is a foreman vouching for a shift, and he has an answer for both. An
    /// <see cref="Satchel.Kind.Authority"/> is an office vouching for a HOLE — it runs a shaft and it is not
    /// an identity — so it is not in the fan, and a captain cannot hand him one by accident.</para></summary>
    public static bool AGuardWouldReadIt(Satchel.Kind kind) =>
        kind is Satchel.Kind.Badge or Satchel.Kind.Chit;

    /// <summary>
    /// THE FAN: every piece of paper in the wallet a guard would read, in ONE stable order.
    ///
    /// <para>This site's own pass first, then the other sites' passes by their id, then the chits by theirs.
    /// The order is a law rather than a convenience: it is what <see cref="DefaultFor"/> falls back on, and a
    /// list that re-sorted itself between the hail and the arrival would be a captain handing over a paper
    /// they did not pick.</para>
    /// </summary>
    public static IReadOnlyList<Satchel.Item> Fan(string bodyId, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var papers = new List<Satchel.Item>();
        foreach (Satchel.Item item in carried ?? [])
        {
            if (AGuardWouldReadIt(item.Kind))
            {
                papers.Add(item);
            }
        }

        papers.Sort((a, b) =>
        {
            int rank = RankOf(bodyId, a).CompareTo(RankOf(bodyId, b));
            return rank != 0 ? rank : string.CompareOrdinal(a.Id, b.Id);
        });
        return papers;
    }

    /// <summary>Which band of the fan a paper rides in. Three, and the first has exactly one member.</summary>
    private static int RankOf(string bodyId, Satchel.Item paper) =>
        paper.Kind == Satchel.Kind.Badge
            ? (string.Equals(PatrolBeat.SiteOfBadge(paper.Id), bodyId, StringComparison.Ordinal) ? 0 : 1)
            : 2;

    /// <summary>Does the chooser open at all? Only when there is a choice to make — one paper is exactly
    /// today, with no chooser and no friction, and that is a promise this feature keeps.</summary>
    public static bool Fans(string bodyId, IReadOnlyList<Satchel.Item>? carried) =>
        Fan(bodyId, carried).Count > 1;

    /// <summary>Is this paper still in the wallet? Asked before a choice made at the hail is honoured at the
    /// read — a pass can be taken off you between the two (<see cref="PatrolBeat.PassRevokedLine"/>).</summary>
    public static bool StillHeld(IReadOnlyList<Satchel.Item>? carried, Satchel.Item paper)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (item.Kind == paper.Kind && string.Equals(item.Id, paper.Id, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    // ── WHAT IS PRINTED ON IT ─────────────────────────────────────────────────────────────────────────

    /// <summary>The glyph the row wears — each paper's own, from the file that mints it. A row that invented
    /// its own icon would be a second answer to what a thing looks like.</summary>
    public static string GlyphOf(Satchel.Item paper) =>
        paper.Kind == Satchel.Kind.Badge ? PatrolBeat.BadgeGlyph : CanteenTable.ChitGlyph;

    /// <summary>
    /// THE NAME ON THE PAPER — the first thing a chooser row says, because it is the first thing the man
    /// will read out loud.
    ///
    /// <para>Everything the site issued you carries YOUR name, spelled the way the rota spells it
    /// (<see cref="PatrolBeat.BadgeIssuedLine"/>). The one paper that does not is #718's chit — the one that
    /// came with a YES-BUT, written under a name the Hand picked — and the whole of the Fletch game is that
    /// the captain can see, in the wallet, that the two are not the same person.</para>
    /// </summary>
    /// <param name="captainName">Whose face is on your own papers. Passed in rather than looked up, because
    /// Core does not own the thread the captain is being played on.</param>
    public static string NameOn(Satchel.Item paper, string captainName)
    {
        ArgumentNullException.ThrowIfNull(captainName);
        return paper.Kind == Satchel.Kind.Chit
               && string.Equals(paper.Id, CanteenTable.ChitUnderAnotherNameId, StringComparison.Ordinal)
            ? AnotherNameLine
            : captainName;
    }

    /// <summary>What the row says where a name would be, on the one paper that is not made out to you. It
    /// names no person: #718 has not ruled on who the Hand wrote down, and a name invented here would be
    /// canon typed into a chooser row.</summary>
    public const string AnotherNameLine = "THE NAME THE HAND CHOSE";

    /// <summary>What the paper CLAIMS — its printed face, verbatim, from whichever file prints it. No
    /// summary and no editorial: the row shows the card, and the captain does the reading.</summary>
    public static string Claims(Satchel.Item paper) =>
        paper.Kind == Satchel.Kind.Badge
            ? (PatrolBeat.BadgeFaceOf(paper.Id) ?? UnreadableFaceLine)
            : CanteenTable.ChitTitle;

    /// <summary>A pass whose face this build cannot read — a save from another build, a paper from a lane
    /// that has not shipped. It is shown and it is honest about itself; it is never quietly hidden, because a
    /// wallet that dropped rows would be a captain missing a paper they are carrying.</summary>
    public const string UnreadableFaceLine = "A PASS · THE FACE OF IT MEANS NOTHING TO YOU";

    // ── WHICH ONE IS ALREADY IN YOUR HAND ─────────────────────────────────────────────────────────────

    /// <summary>
    /// THE DEFAULT — <i>last shown at this site, else the real one</i>, and deterministic to the letter.
    ///
    /// <para>A default is not a convenience here, it is the thing that keeps the beat honest: the chooser
    /// opens during a walk-up a captain may simply ignore, so whatever is in the hand when he arrives has to
    /// be the paper a reasonable person would already be holding. Last-shown-here is the captain's own habit;
    /// the site's own pass is the fallback, and after that the fan's own order decides.</para>
    /// </summary>
    public static Satchel.Item? DefaultFor(
        string bodyId, IReadOnlyList<Satchel.Item>? carried, IReadOnlyList<Shown>? book)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        IReadOnlyList<Satchel.Item> fan = Fan(bodyId, carried);
        if (fan.Count == 0)
        {
            return null;
        }

        // The habit: the last paper this captain handed to anybody in this building, if it is still in the
        // wallet. Walked backwards, because the LAST one is the habit and the first one is history.
        var rows = new List<Shown>(book ?? []);
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(rows[i].BodyId, bodyId, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Satchel.Item paper in fan)
            {
                if (string.Equals(paper.Id, rows[i].PaperId, StringComparison.Ordinal))
                {
                    return paper;
                }
            }
        }

        // …else the real one, which is the first band of the fan and needs no second rule.
        return fan[0];
    }

    // ── WHAT THE CHOOSER SAYS ─────────────────────────────────────────────────────────────────────────

    /// <summary>The chooser's title. It names the decision rather than the mechanic, and it is the one place
    /// in the game that says the quiet part.</summary>
    public const string ChooserLabel = "🪪 WHICH ONE OF YOU IS HE MEETING?";

    /// <summary>The line under the title. It states the clock — he is walking, and he arrives whether or not
    /// you have decided — because a choice under time pressure has to say that it is one.</summary>
    public const string ChooserHint =
        "He is crossing the floor. Whatever is in your hand when he gets here is the thing he reads, and " +
        "there is no swapping it in front of him.";

    /// <summary>What the row's chosen state is called, for the control that carries it.</summary>
    public const string InYourHandLine = "in your hand";

    /// <summary>The way out that changes nothing. Closing the fan keeps whatever was already in the hand —
    /// there is no arm of this dialog that leaves the captain holding nothing by accident.</summary>
    public const string KeepItLine = "Keep the one you have";

    /// <summary>Every authored sentence this file owns, for the canon grep. It walks the catalog itself, so a
    /// line added tomorrow is checked tomorrow — the discipline <see cref="PatrolBeat.AllProse"/> keeps.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return ChooserLabel;
        yield return ChooserHint;
        yield return InYourHandLine;
        yield return KeepItLine;
        yield return NeverShownLine;
        yield return AnotherNameLine;
        yield return UnreadableFaceLine;
        foreach (Outcome how in Enum.GetValues<Outcome>())
        {
            yield return ReasonTag(how);
            yield return ShownNote(PatrolBeat.Badge("luna"), "luna", -2, how, "SOMEBODY");
        }
    }
}
