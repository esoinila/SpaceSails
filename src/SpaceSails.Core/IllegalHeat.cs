using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #715 · <b>ILLEGAL HEAT IS OWED TO WHOEVER YOU CROSSED.</b>
///
/// <para>Owner's rulings, 2026-08-05: <i>"we need to get out and let the heat of discovery to the site cool
/// down. New meter for illegal heat"</i> — and, in the same breath, the shape of it: <i>"I guess the illegal
/// heat should be targeted at the entity we crossed … so not like the Casinos that distribute cheaters lists
/// in Vegas"</i>.</para>
///
/// <h3>One key, one holder, and no list</h3>
///
/// <para>Heat is banked against an <b>operator</b> (<see cref="SiteOperator.Of"/>) and against nothing else.
/// Never a body — a rock does not hold a grudge. Never the world — the world is not one police force. It does
/// not propagate to another outfit under any circumstances, and the fiction pays for that rather than a
/// clause here: <b>a clandestine operator cannot warn its competitors without admitting the basement
/// exists.</b> Saying "watch for this ship" out loud is saying "we have an unlisted hole and somebody was in
/// it", to the one audience that would know what that means.</para>
///
/// <para>That makes it a resource rather than damage. Burn one outfit and work another; the second one has
/// heard nothing, because from where they sit nothing happened. And it makes the world legible in the way the
/// issue asks for: a captain who learns that two sites answer to the same people learns it <i>by being
/// treated differently at the second one</i>, which is why nothing this file prints ever names the outfit.</para>
///
/// <h3>Where it lives</h3>
///
/// <para>In the <see cref="ContactLedger"/> — the book #770 already calls "#715's per-entity ledger" — under
/// <see cref="LedgerId"/>, one entry per outfit. That is the honest model (the thing that remembers you is
/// the outfit's own memory of you) and it has three consequences worth naming: it round-trips through the
/// vault for free, an old save loads with no heat at all, and <b>#905's frame ledger stays pinned</b> — no
/// new field on the page, none on the excursion.</para>
///
/// <h3>Cooling is in ABSENCE</h3>
///
/// <para>Owner's word, exactly: <i>get out</i>. <see cref="Cool"/> cools every outfit EXCEPT the one whose
/// ground the captain is standing on; on their ground the clock is only advanced, never banked, so time spent
/// under their lights can never be spent again as time away from them. Going away is a move the player learns
/// to make.</para>
///
/// <h3>It is not #582's meter, and it is not the ship's</h3>
///
/// <para>The ship's heat (<see cref="HeatState"/>, <see cref="EncounterRule"/>) is what the LAW thinks of a
/// hull, and #582 will extend it per-place. This is what ONE COMPANY thinks of a captain. Different key,
/// different holder, no shared truth to disagree about — which is the whole of the answer #618 asked for, and
/// there is a guard whose only job is that raising either one leaves the other exactly where it was.</para>
/// </summary>
public static class IllegalHeat
{
    // ── WHO IS OWED ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The prefix an outfit's book is filed under in the contacts ledger. It is a prefix rather than
    /// a bare id for one reason: a company is not somebody you drank with, and a keyspace where an outfit
    /// called <c>argent</c> could collide with a contact called <c>argent</c> would be one book holding two
    /// unrelated relationships under one name.</summary>
    public const string LedgerPrefix = "outfit:";

    /// <summary>Where one outfit's memory of you is filed.</summary>
    public static string LedgerId(string operatorId)
    {
        ArgumentNullException.ThrowIfNull(operatorId);
        return LedgerPrefix + operatorId;
    }

    /// <summary>Is this row of the contacts book an outfit's memory rather than a person's?</summary>
    public static bool IsAnOutfitsBook(string contactId) =>
        contactId is not null && contactId.StartsWith(LedgerPrefix, StringComparison.Ordinal);

    // ── THE CROSSINGS, AND WHAT EACH COSTS ──────────────────────────────────────────────────────────────

    /// <summary>
    /// #715 · <b>THE WHOLE LIST OF WAYS TO BURN SOMEBODY.</b> Five, and every one of them is a moment where a
    /// captain asked an outfit's own machinery or an outfit's own man for something and was told no to their
    /// face.
    ///
    /// <para><b>What is deliberately NOT here.</b> Being caught in a cabinet with a dogged door is not a
    /// crossing: nobody read anything, nobody refused anything, and the memory of that belongs to a different
    /// holder entirely (the counter's book, #770). Nor is a shot fired, a lock blown or a room emptied —
    /// those are the LAW's business and the ship's heat is where they land.</para>
    /// </summary>
    public enum Crossing
    {
        /// <summary>A card the panel would not take, at a gate of theirs. The commonest one, and the
        /// cheapest: being refused mostly costs you the refusal.</summary>
        RefusedCardAtAGate,

        /// <summary>#760 · A standing sent over the air in the ship's name, and refused. It costs what the
        /// same paper costs at the same gate — the whole of what #929 needed out of this meter.</summary>
        RefusedSend,

        /// <summary>#763 · A wake-word pressed at hardware they are listed to answer for, and refused.</summary>
        RefusedPress,

        /// <summary>#804/#835 · The round takes you off the floor — a challenge your wallet could not answer,
        /// or a man who ran you down and has your arm. It is dearer than a machine's no because a person was
        /// there and a line went into a book with the time on it.
        ///
        /// <para>The challenge failing and the walk to the car are ONE crossing, banked at the moment the
        /// walk begins, and that is not a saving: they are the same thirty seconds, and a meter that charged
        /// both would be one crossing banked twice — precisely what this feature's fourth guard exists to
        /// catch.</para></summary>
        TheEscort,

        /// <summary>#835 · The round has spent its patience and walks you all the way to the sky, taking the
        /// pass back on the way. The dearest thing on the list, because it is the only one where the outfit
        /// stops treating you as a problem for this floor and starts treating you as a problem.</summary>
        TheKickOut,
    }

    /// <summary>What one crossing costs. Small numbers on purpose: the pressure in this meter comes from
    /// doing things again, not from any single evening. FLAGGED for the owner's tuning.</summary>
    public static int WeightOf(Crossing why) => why switch
    {
        Crossing.RefusedCardAtAGate => UndergroundComplex.RefusedCardHeat,
        Crossing.RefusedSend => UndergroundComplex.RefusedCardHeat,
        Crossing.RefusedPress => UndergroundComplex.RefusedCardHeat,
        Crossing.TheEscort => 2,
        Crossing.TheKickOut => 4,
        _ => 0,
    };

    /// <summary>#715 · WHAT THIS CROSSING COSTS AND WHO IS OWED IT, at this site. The same publication
    /// <see cref="UndergroundComplex.RefusedAtTheGate"/> makes — one shape, so the four things that publish a
    /// charge and the one thing that banks it can never come to disagree about what a charge is.</summary>
    public static UndergroundComplex.HeatCharge Charge(string bodyId, Crossing why)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return new UndergroundComplex.HeatCharge(SiteOperator.Of(bodyId).Id, WeightOf(why));
    }

    /// <summary>As hot as one outfit will ever get about one captain. A ceiling rather than a runaway,
    /// because past the top rung there is nothing further for the meter to buy — every effect it has is
    /// already on. FLAGGED.</summary>
    public const int Ceiling = 12;

    /// <summary>How long OFF an outfit's ground one point of it takes to go away. An hour of sim time, so a
    /// burned evening is walked off in a working day rather than in a voyage. FLAGGED.</summary>
    public const double CoolsOnePointEverySeconds = 3600.0;

    // ── THE BOOK ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What this outfit remembers, right now. Zero is the answer for everybody the captain has never
    /// crossed, which is almost everybody almost always.</summary>
    public static int HeatAt(ContactLedger book, string operatorId)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(operatorId);
        return book.For(LedgerId(operatorId)).HeatOwed;
    }

    /// <summary>The same question asked of a place: what does whoever runs THIS site remember. Every effect
    /// in the game asks it this way, so no caller ever re-derives the operator of a body.</summary>
    public static int HeatAtSite(ContactLedger book, string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HeatAt(book, SiteOperator.Of(bodyId).Id);
    }

    /// <summary>
    /// #715 · <b>BANK A PUBLISHED CHARGE. THE ONE CALL, and there is deliberately no second one.</b>
    ///
    /// <para>Every crossing in the game arrives here as a <see cref="UndergroundComplex.HeatCharge"/> — the
    /// lift panel's refused read, #760's refused send, #763's refused press, and the round's own two. A
    /// charge owed to nobody (<see cref="UndergroundComplex.NothingOwed"/>, and every accepted anything) is a
    /// no-op, so a caller may bank unconditionally and never has to decide whether something counted.</para>
    ///
    /// <para>The stamp moves on every charge: a fresh crossing restarts the cooling clock, which is what
    /// "the heat of discovery" means — it is the discovery that is recent, not the total.</para>
    /// </summary>
    /// <returns>What the outfit's memory now stands at, for a caller that wants to say something about it.</returns>
    public static int Bank(ContactLedger book, UndergroundComplex.HeatCharge charge, double simTime)
    {
        ArgumentNullException.ThrowIfNull(book);

        if (charge.Points <= 0 || string.IsNullOrEmpty(charge.OperatorId))
        {
            return string.IsNullOrEmpty(charge.OperatorId) ? 0 : HeatAt(book, charge.OperatorId);
        }

        string id = LedgerId(charge.OperatorId);
        int standing = book.For(id).HeatOwed;
        int room = Math.Max(0, Ceiling - standing);
        return book.ApplyHeat(id, NameOf(charge.OperatorId), Math.Min(charge.Points, room), simTime).HeatOwed;
    }

    /// <summary>The heading an outfit's book is kept under. The company's own letterhead where this build
    /// knows it, and the id itself where it does not — never an invented company (#760's law about a standing
    /// this register cannot place).</summary>
    private static string NameOf(string operatorId) =>
        SiteOperator.ById(operatorId) is { } op ? op.Name : operatorId;

    /// <summary>
    /// #715 · <b>COOL EVERYBODY YOU ARE NOT STANDING ON.</b> The owner's ruling, as arithmetic.
    ///
    /// <para>Called once a frame with the outfit whose ground the captain is on, or null when they are
    /// nowhere near anybody's ground — in the sky, in a haven, on a rock with no site under it. Every outfit
    /// with heat on the book cools by the elapsed sim time; <b>the one underfoot cools by nothing</b>, and
    /// its clock is advanced so that the hours spent under their lights can never be banked later as hours
    /// away from them.</para>
    ///
    /// <para>It creates nothing. An outfit with no memory of you is an outfit this method does not touch, so
    /// a captain who has never crossed anybody has an empty book after ten thousand frames.</para>
    /// </summary>
    /// <param name="book">The contacts ledger.</param>
    /// <param name="operatorIdUnderfoot">Whose ground the captain is standing on, or null.</param>
    /// <param name="simTime">Now.</param>
    public static void Cool(ContactLedger book, string? operatorIdUnderfoot, double simTime)
    {
        ArgumentNullException.ThrowIfNull(book);
        if (double.IsNaN(simTime))
        {
            return;
        }

        List<ContactHistory>? hot = null;
        foreach (ContactHistory h in book.Entries.Values)
        {
            if (h.HeatOwed > 0 && IsAnOutfitsBook(h.ContactId))
            {
                (hot ??= []).Add(h);
            }
        }
        if (hot is null)
        {
            return;
        }

        foreach (ContactHistory h in hot)
        {
            string operatorId = h.ContactId[LedgerPrefix.Length..];
            double since = simTime - h.HeatStampSimTime;

            // Their ground, or a clock that has gone backwards (a save loaded over a longer game): the stamp
            // is brought to now and nothing is banked. Neither of those is time spent away from them.
            if (string.Equals(operatorId, operatorIdUnderfoot, StringComparison.Ordinal)
                || double.IsNaN(since) || since < 0)
            {
                book.ApplyHeat(h.ContactId, h.DisplayName, 0, simTime);
                continue;
            }

            int points = (int)(since / CoolsOnePointEverySeconds);
            if (points <= 0)
            {
                continue;
            }

            // The remainder is KEPT rather than rounded away — the stamp moves by whole points only, so a
            // captain who leaves and comes back and leaves again cools at the same rate as one who stayed
            // away, instead of losing the part-hour every time the frame is asked.
            book.ApplyHeat(
                h.ContactId, h.DisplayName, -Math.Min(points, h.HeatOwed),
                h.HeatStampSimTime + (points * CoolsOnePointEverySeconds));
        }
    }

    // ── WHAT IT COSTS YOU, AT THEIR DOORS AND NOWHERE ELSE ──────────────────────────────────────────────

    /// <summary>#715/#804 · How much heat buys one rung of the round's patience. The round already keeps a
    /// ladder — <see cref="PatrolBeat.EscortsAWatchAllows"/> walks back to the car before the walk becomes a
    /// walk to the sky — and heat does not add a second ladder beside it. It starts you further up the one
    /// that is there. FLAGGED.</summary>
    public const int HeatPerRung = 4;

    /// <summary>#715 · <b>WHERE THE ROUND'S SUSPICION STARTS at this outfit's site.</b> The number of escorts
    /// already against you the moment a watch begins, which is exactly what "they are warier here" means in
    /// the vocabulary this building already has: the same man, the same rounds, the same mild procedure —
    /// arriving at the end of its patience sooner.</summary>
    public static int StartingRung(int heat) =>
        Math.Clamp(heat / HeatPerRung, 0, PatrolBeat.EscortsAWatchAllows);

    /// <summary>#715 · Past this, the panel at their gate wants a face with the paper.</summary>
    public const int TheGateWantsAFaceAt = 3;

    /// <summary>#715 · <b>DOES THE GATE ASK HARDER?</b> A cold gate never asks for the pass at all — the card
    /// is the whole transaction. A hot one asks on the first press, and the paper on its own is not enough.</summary>
    public static bool TheGateWantsAFace(int heat) => heat >= TheGateWantsAFaceAt;

    /// <summary>#715 · Past this, their net stops having anything to say to your ship's name.</summary>
    public const int TheNetStopsAnsweringAt = 5;

    /// <summary>#715/#760 · <b>HAS THE SEND STOPPED WORKING?</b> Above the rung, an outfit does not read the
    /// wallet that arrives in that ship's name at all. It is the strongest thing in the file and the last one
    /// to arrive, because it takes a verb away.</summary>
    public static bool TheNetStopsAnswering(int heat) => heat >= TheNetStopsAnsweringAt;

    // ── WHAT IS SAID ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #715 · <b>THE WHOLE OF THE PRESENTATION, and it names nobody.</b>
    ///
    /// <para>No percentage, no gauge, no company on the screen — §13.8's inference horror, which the issue's
    /// own canon section makes a condition of this meter existing. What the captain gets is one flat line at
    /// this outfit's doors and silence at everybody else's, and the difference between those two is the
    /// entire teaching mechanism: the day a site you have never been to greets you this way, you have learned
    /// who owns it.</para>
    ///
    /// <para>It would have been one word cheaper to write <i>MERIDIAN WORKS COMPANY remembers you</i>, and
    /// that word would have handed over the one inference the issue says is the payoff.</para>
    /// </summary>
    public const string TheyRememberYouHere = "🌡 They remember you here.";

    /// <summary>#715 · Is there anything to say at this site? True only where the outfit that runs it has a
    /// memory of the captain; false at every other outfit's site, however hot the captain is elsewhere.</summary>
    public static bool TheyRememberYouAt(ContactLedger book, string bodyId) => HeatAtSite(book, bodyId) > 0;

    /// <summary>#715 · What the panel says when the card is good and it wants the face as well. The machine
    /// has not learned anything; it has been TOLD something, and the difference between those is the whole
    /// sentence.</summary>
    public const string TheGateWantsAFaceLine =
        "🔒 The panel takes the card and reads it, and then does not open. It asks for the other thing — the " +
        "pass with your face on it, the one nobody down here has ever been asked for at a shaft door. It is " +
        "not a machine that has got careful. It is a machine that has been told something about you.";

    /// <summary>#715/#760 · What comes back when their net has stopped answering that ship. Not a refusal —
    /// a refusal is an answer, and there is not one.</summary>
    public const string TheNetWillNotAnswerLine =
        "Nothing is refused, exactly. Nothing is answered either. The carrier is up, your ship's name goes " +
        "out into it the way it always has, and the far end simply has nothing to say to that name any more.";

    /// <summary>Every sentence this meter can put on a screen, for the audit that reads them all
    /// (<c>EveryTextReadsTests</c>). Three, and none of them is a number.</summary>
    public static IEnumerable<string> EveryLine()
    {
        yield return TheyRememberYouHere;
        yield return TheGateWantsAFaceLine;
        yield return TheNetWillNotAnswerLine;
    }
}
