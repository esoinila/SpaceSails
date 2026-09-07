using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1074 beat 4 · <b>CAREER-COST NPCs</b> — the colleague who kept going is not dead.
///
/// <para>#1074, verbatim: <i>"The colleague who kept going is not dead — 'reassigned where their skills are
/// most needed' (intranet register, pairs with the Lost Property line). Asking colleagues gets sincere
/// blankness or a changed subject; one keeps the missing one's mug on the shelf and will not say why."</i>
/// </para>
///
/// <para><b>WHAT THE ENFORCEMENT TIER COSTS A PERSON.</b> Beats 1 and 2 closed a working and fenced it; this
/// is the same office reaching one rung further down, into a shift's own roster. It is the cheapest possible
/// subtraction and the only one the doctrine allows outside the direct-confrontation rule: not a body, not a
/// disappearance, <b>a transfer</b>. A row goes into a register, a form is filed, and a man who was on nights
/// last week is on somebody else's establishment this week. Every word of it is true. Nobody is lying to
/// anybody, and that is precisely why it works.</para>
///
/// <para><b>THE THREE SENTENCES ARE THE WHOLE BEAT</b>, and all three are authored in #1074's canon pass of
/// 2026-09-03 and lifted character for character. Read them in order and there is nothing else to say:</para>
/// <list type="number">
/// <item>the register row — <see cref="ReassignedLine"/> — with no destination and no date;</item>
/// <item>the colleague, asked — <see cref="ColleagueLine"/> — sincere blankness, said as if it were the whole
/// answer, because to them it is;</item>
/// <item>the one who keeps the mug — <see cref="MugLine"/> — and nothing more, ever.</item>
/// </list>
///
/// <para><b>THE LAWS, and a guard for each (<c>TheCareerCostTests</c>).</b> Nobody says <i>missing</i>.
/// Nobody says <i>dead</i>. Nobody names the working. None of the three mentions the Authority — the office
/// that closed the dig signs the plate at the seal and the sign at the fence, and it has no business in a
/// canteen. §8's reserved word is absent. And #761 holds hardest here: <b>the row and the mug ARE the
/// telling</b>. There is no card, no pulse of its own, no nerve shock, no marker and nothing on the wire — a
/// captain reads a board and asks two people, and everything this beat has to say is in those three
/// sentences and one drawn mug.</para>
///
/// <para><b>A Scully reads an office that moved somebody and a colleague who liked them</b>, and is not being
/// fooled: that is exactly what the paperwork records and exactly what the colleague believes.</para>
///
/// <para><b>WHERE IT APPLIES.</b> A ground the Authority has closed the working of — which is
/// <see cref="StopOrder.On"/>, and which is <b>also every preserved ground</b>, because
/// <see cref="PreservationZone.Note"/> only ever takes a ground into care that is already in the stop
/// register and never removes it. One question, asked once, and the beat covers both states of the same
/// site without a second condition anybody has to keep agreeing.</para>
///
/// <para><b>AND NOWHERE ELSE, TO THE CHARACTER.</b> The two regulars are dealt out of a pool that stops
/// SHORT of them, exactly as #1063's mason is, so on every ground nobody has stopped — which is every ground
/// in almost every world — the canteen deals the same ten people, by the same dice, against the same length,
/// and not one person in the game moves seat. The register row is likewise not in the board's own catalogue
/// at all: it is pinned by <see cref="CanteenBoard.Pinned"/> on a stopped ground and there is no world in
/// which the ordinary deal can reach it.</para>
/// </summary>
public static class CareerCost
{
    // ── THE AUTHORED WORDS, VERBATIM FROM #1074's CANON PASS OF 2026-09-03 ───────────────────────────────
    //
    // Three sentences, and they are the only new player-facing prose this beat publishes. The way a feature
    // like this dies is one helpful line written to fill a gap (§13.20), and the gaps here are enormous:
    // where he went, when, who signed it, and whether the woman with the mug knows something. Every one of
    // those gaps is the beat, and every temptation to close one has to be answered with silence.

    /// <summary>#1074 · <b>THE REGISTER ROW.</b> Authored, verbatim. <b>No destination and no date</b> — and
    /// notice that neither is a lie by omission in a register, because a register row never carries either.
    /// It is the most ordinary line a personnel system ever wrote, which is the Scully law working exactly as
    /// it is meant to.</summary>
    public const string ReassignedLine = "Reassigned where their skills are most needed.";

    /// <summary>#1074 · <b>THE COLLEAGUE, ASKED.</b> Authored, verbatim. Sincere blankness: he is not
    /// covering, he is not frightened and he is not being careful with you — he is telling you what he knows,
    /// and this is what he knows. <i>Administration</i> is the office he would ring about a payslip and is
    /// not the office on the plate at the seal; nothing in this beat mentions that one.</summary>
    public const string ColleagueLine = "Transferred, I think. Administration would know where.";

    /// <summary>#1074 · <b>THE ONE WHO KEEPS THE MUG.</b> Authored, verbatim, and the whole of what she will
    /// ever say about it. <b>No explanation, ever</b> — the mug is the testimony and the sentence is only the
    /// sound of a person declining to move it.
    ///
    /// <para>The seam already changes the subject and nothing here has to: a regular says their one breath the
    /// first time they are asked and every later ask pulses their PLATE and nothing else (#709's
    /// once-per-person law, in <c>HiveRegularInteract</c>). So this line is said once and the room offers
    /// nothing further, which is exactly what the canon asks for.</para></summary>
    public const string MugLine = "That stays where it is.";

    // ── WHO SAYS THEM, IN THE ROOM'S OWN PLATE IDIOM ─────────────────────────────────────────────────────
    //
    // NOT NEW PROSE, on #1063's MasonPlate precedent: a plate is the canon's own noun phrase for a person set
    // in the SHOUTED idiom every regular in that room already wears, so the room's canon grep sees a workman
    // and not a character. The canon calls them "colleagues" off the closed working's shift, and "one
    // regular" who keeps the mug.

    /// <summary>#1074 · Who the colleague reads as, at a glance, before he says anything. He is on the shift
    /// the roster still lists (<see cref="CanteenBoard.RosterHead"/> is certainly pinned on this ground), and
    /// that is the whole of him.</summary>
    public const string ColleaguePlate = CanteenRegulars.Glyph + " A COLLEAGUE OFF THE SAME SHIFT";

    /// <summary>#1074 · …and who keeps the mug. <b>Deliberately the dullest plate in the room</b>: the canon's
    /// own noun for her is <i>one regular</i>, and a plate that said what she was keeping would announce the
    /// beat across a room before the captain had asked anybody anything. The mug is drawn behind her seat and
    /// a player either looks or does not.</summary>
    public const string MugPlate = CanteenRegulars.Glyph + " A REGULAR";

    /// <summary>#1074 · The heading the register row reads across the room. <b>Not authored copy</b>: it is
    /// the canon's own noun — the <i>register</i> — in the board's SHOUTED-DEPARTMENT-DASH-STATUS form that
    /// every other notice on that cork already wears (<c>LOST — TOOLBAG</c>, <c>CATERING — WITHDRAWN</c>),
    /// which is <see cref="Burial.NoticeHead"/>'s move for its reason. The sentence underneath is the
    /// authored one and it is untouched.</summary>
    public const string RegisterHead = "REGISTER — PERSONNEL";

    // ── THE NAME ON THE ROW ──────────────────────────────────────────────────────────────────────────────
    //
    // THE CANON AUTHORED NO NAME, and this file writes none. The row names a hand off the shift the board is
    // still advertising, and the name is DEALT from the one pool of people-names the game already prints
    // (FieldDossier's given and family lists, which is where every stranger reconstructed out of their own
    // kit gets theirs). Nothing new is invented to be a person: a name that existed only here would be a
    // character this beat had minted, and the doctrine's first law is that nobody in this arc is a character.
    //
    // Seeded on the GROUND and nothing else, so the row names the same hand on every visit and two captains
    // comparing notes agree — the same discipline the board's own deal keeps.

    /// <summary>#1074 · <b>The rostered hand the row names</b>, on this ground. Deterministic in the body id
    /// alone: a register row that changed name between excursions would be a register nobody kept.</summary>
    public static string HandOn(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        IReadOnlyList<string> given = FieldDossier.GivenNames;
        IReadOnlyList<string> family = FieldDossier.FamilyNames;
        string first = given[DiceRule.Roll(DiceRule.Seed($"career:hand:given:{bodyId}"), given.Count).Face - 1];
        string last = family[DiceRule.Roll(DiceRule.Seed($"career:hand:family:{bodyId}"), family.Count).Face - 1];
        return $"{first} {last}";
    }

    /// <summary>#1074 · <b>THE ROW ITSELF</b>, as it is pinned: the hand, then the authored line, and nothing
    /// else on the paper. A register row is a name and a disposition — no destination, no date, no signature
    /// and no office.</summary>
    public static string RegisterBody(string bodyId) => $"{HandOn(bodyId)}. {ReassignedLine}";

    /// <summary>#1074 · The row as the board carries it, paired — for the board's own consistency law — with
    /// the colleague sitting ten feet away. <b>Nothing renders <c>Pairs</c>, ever</b> (#709): the cross
    /// reference exists so the room is internally true and so a guard can prove it, and the player either
    /// notices that the man he just asked is on the same shift as the name on the cork, or does not.</summary>
    public static CanteenBoard.Notice RegisterRow(string bodyId) =>
        new(RegisterHead, RegisterBody(bodyId), ColleaguePlate);

    // ── THE MUG ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // "A mug on the shelf behind one regular's seat, and if the captain asks about it: 'That stays where it
    // is.' Nothing more; the subject changes. The mug is the whole testimony."
    //
    // NO ART IS ADDED. It is the glass the canteen already draws itself with — Interior.TheKeep's own glyph,
    // the one the field book files a night at that counter under — set on the shelf behind a chair. A new
    // picture for a mug would be a prop with a budget line, and the point of the object is that it is the
    // most ordinary thing in the room.
    //
    // AND CORE OWNS WHERE (§13.15). The renderer is handed a coordinate the way it is handed the board's, so
    // the drawn mug and the seated regular cannot come out of two authors.

    /// <summary>#1074 · The mug, as it is drawn: the canteen's own glass. Never a new picture — see the
    /// remarks above.</summary>
    public const string MugGlyph = Interior.TheKeep.Glyph;

    /// <summary>#1074 · How far behind a top the shelf stands. Clear of the chair ring
    /// (<see cref="CanteenRegulars.ChairRingDu"/>) by a hand's width, so the mug is on the wall side of
    /// whoever is sitting there and never on top of them.</summary>
    public const double ShelfBehindDu = 1.1;

    /// <summary>
    /// #1074 · <b>WHERE THE MUG IS</b>, for the top the regular who keeps it is sitting at — behind the seat,
    /// which in this room's own coordinates is the counter side (the board hangs against the same wall).
    ///
    /// <para>Clamped to the room the top is in rather than trusted: a top against the back wall would put the
    /// shelf outside the canteen, and a mug drawn through a wall is the drawn world and the built world
    /// disagreeing about a thing a captain can see. Where behind will not fit, the shelf is the near side
    /// instead — a room with one wall of shelving is not a fact this beat needs.</para>
    /// </summary>
    /// <param name="top">The top she is at, as Core seated her.</param>
    /// <param name="amenity">The room it stands in, whose own box does the clamping.</param>
    public static (double X, double Y) MugAt(
        in CanteenRegulars.TableSeat top, in UndergroundComplex.Amenity amenity)
    {
        double behindY = top.Y + CanteenRegulars.ChairRingDu + ShelfBehindDu;
        if (amenity.Contains(top.X, behindY))
        {
            return (top.X, behindY);
        }
        return (top.X, top.Y - CanteenRegulars.ChairRingDu - ShelfBehindDu);
    }

    /// <summary>#1074 · Every player-facing string this beat publishes, for the canon grep — the same
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps. The dealt name is not here: it is
    /// not prose, it is a row's subject, and it is swept where it is rendered instead.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return RegisterHead;
        yield return ReassignedLine;
        yield return ColleaguePlate;
        yield return ColleagueLine;
        yield return MugPlate;
        yield return MugLine;
    }
}
