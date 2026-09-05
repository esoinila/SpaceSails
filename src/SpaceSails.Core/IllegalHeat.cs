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
    /// #715 · <b>THE WHOLE LIST OF WAYS TO BURN SOMEBODY.</b> Six, and every one of them is a moment where an
    /// outfit's own machinery or an outfit's own man found out about the captain on the outfit's own ground.
    ///
    /// <para><b>What is deliberately NOT here.</b> Being caught in a cabinet with a dogged door is not a
    /// crossing: nobody read anything, nobody refused anything, and the memory of that belongs to a different
    /// holder entirely (the counter's book, #770). Nor is a lock blown or a room emptied — those are the LAW's
    /// business and the ship's heat is where they land.</para>
    ///
    /// <para><b>#618 · …and a shot is BOTH, split exactly where the split is honest.</b> Firing a gun is the
    /// law's business and always was. A gun going off inside thirty-four deck units of a man on THEIR rota,
    /// on THEIR floor, is additionally something that outfit finds out — not by inference and not by a report,
    /// but because somebody they pay heard it and walked over. That is the same shape as every other row here
    /// (a person was there, and a line went into a book), and it is why the shot only ever costs anything
    /// where somebody was standing to hear it: on an empty floor, or out past the range, no register covers
    /// it and nothing is owed.</para>
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
        /// pass back on the way. The dearest thing a captain can do TO A FLOOR, because it is the only one
        /// where the outfit stops treating you as a problem for this floor and starts treating you as a
        /// problem.</summary>
        TheKickOut,

        /// <summary>#618 · A gun went off on a floor they have somebody on, close enough for him to hear it,
        /// and he left his round to go and look. Owner's own trigger, 2026-08-05: <i>"they come if we make a
        /// big noise like start to use the special ammo to open a locked door."</i>
        ///
        /// <para>Dearer than the escort and cheaper than the ejection, and it sits there on the reasoning
        /// this table is built on: an escort is a man writing your face down, an ejection is a man deciding
        /// you are a problem, and a bang on a working floor is between the two — nobody has your name, and
        /// nobody is going to forget the evening either.</para></summary>
        ShotOnTheirFloor,

        /// <summary>
        /// #525 · <b>HE SET HIS OWN REACTOR TO RUN AWAY WITH ITSELF WHILE CLAMPED TO THEIR COLLAR, AND LET
        /// IT.</b> Appended, like every member before it, because several switches name these arms by hand.
        ///
        /// <para><b>This is not a crossing of a floor and it is not on the same ladder as the five above
        /// it.</b> Those are an evening going badly: a card refused, a man walked to his car, a bang someone
        /// heard. Every one of them is written small on purpose, because the pressure in this meter is meant
        /// to come from doing things again. This one cannot be done again — there is one ship — and no
        /// number of refused cards adds up to a hull going off inside a harbour.</para>
        ///
        /// <para>So it is worth <see cref="Ceiling"/>: as hot as this outfit will ever get about one captain,
        /// which is not a big number chosen to feel big but <b>the meter's own top, quoted</b>. A typed 12
        /// beside it would be the mirrored constant this ground keeps a table of — retune the ceiling and a
        /// hand-typed weight would silently stop reaching it, or start overshooting a clamp that hides the
        /// bug. <see cref="Bank"/> clamps to the room that is left, so the answer at this outfit afterwards
        /// is exactly <see cref="Ceiling"/> whatever was on the book a moment before.</para>
        ///
        /// <para><b>And what it buys is already built.</b> <see cref="StartingRung"/> at the ceiling is
        /// <see cref="PatrolBeat.EscortsAWatchAllows"/> — the last rung — so their round begins every watch
        /// at the end of its patience. He is not carrying a new flag around their concourse; he is carrying
        /// their file.</para>
        /// </summary>
        SheWentAtTheirBerth,
    }

    /// <summary>What one crossing costs. Small numbers on purpose: the pressure in this meter comes from
    /// doing things again, not from any single evening — with the one exception that cannot be done again
    /// (<see cref="Crossing.SheWentAtTheirBerth"/>, which is worth the meter's own <see cref="Ceiling"/> and
    /// says why beside itself). FLAGGED for the owner's tuning.</summary>
    public static int WeightOf(Crossing why) => why switch
    {
        Crossing.RefusedCardAtAGate => UndergroundComplex.RefusedCardHeat,
        Crossing.RefusedSend => UndergroundComplex.RefusedCardHeat,
        Crossing.RefusedPress => UndergroundComplex.RefusedCardHeat,
        Crossing.TheEscort => 2,
        Crossing.ShotOnTheirFloor => 3,
        Crossing.TheKickOut => 4,
        Crossing.SheWentAtTheirBerth => Ceiling,
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

    // ── #535/#938 · AND THE ONE WAY TO TAKE SOMETHING OFF IT THAT IS NOT TIME ───────────────────────────
    //
    // The 2026-09-03 audit's row on this file, in four words: `IllegalHeat` has `Cool` and no scrub path.
    // Everything this meter could do was ADD, or wait. That was right while the only currency was hours —
    // owner's word for cooling is "get out", and a second way to walk it off would have been a second answer
    // to the question the whole meter asks.
    //
    // #535's key is the exception the fiction pays for, and the owner wrote the reason down himself: HEAT IS
    // NOT A MOOD, IT IS A PAPER TRAIL. Nobody is warier at their gate because they feel wronged; they are
    // warier because of what is filed. Delete the filings and the wariness loses its basis — which is why
    // this is the only thing in the game that lowers heat without time, a haven or a bribe, and why it costs
    // an object that cannot be bought.

    /// <summary>
    /// #535 · <b>ONE BAND OF THE METER</b>, and it is the meter's OWN step rather than a number somebody
    /// typed beside it: <see cref="HeatPerRung"/> is what a band means everywhere else in this file — it is
    /// the width <see cref="StartingRung"/> divides by to decide how far up the round's patience a captain
    /// starts. So "drops by one whole band" and "starts one rung lower at their gate" are the same sentence,
    /// and they cannot come apart the day the rung is retuned.
    /// </summary>
    public static int ABand => HeatPerRung;

    /// <summary>
    /// #535 · <b>SCRUB A BAND OFF ONE OUTFIT'S BOOK — an EDIT, not an hour.</b>
    ///
    /// <para>Takes <see cref="ABand"/> points off what this outfit remembers, or everything it remembers if
    /// that is less. An outfit with nothing on the book is untouched and answers zero: there is no such thing
    /// as negative heat, and a key burned over a clean file has burned for nothing (owner: <i>"a key burned
    /// at heat 1 removes almost nothing and nobody notices"</i>).</para>
    ///
    /// <para><b>The stamp does not move</b>, and that is the whole difference between this and
    /// <see cref="Cool"/>. Cooling advances the clock because it IS the clock; a scrub reaches into the file
    /// and takes pages out of it, so the hours the captain has or has not spent away from these people are
    /// exactly what they were a moment ago. Banking through <see cref="ContactLedger.ApplyHeat"/> at the
    /// row's own existing stamp is what says so.</para>
    ///
    /// <para><paramref name="why"/> is required and unused by the arithmetic. It is here because the audit
    /// row asked for a path that is <i>visible in the ledger as an edit, not as time passing</i>: a caller
    /// that cannot say why it is deleting somebody's file is a caller that should not be deleting it.</para>
    /// </summary>
    /// <returns>HOW MUCH WAS ERASED — the number the owner's own note says has to be carried forward,
    /// because <i>an absence is only evidence if somebody wrote down how big it was</i>. Zero when there was
    /// nothing on the book.</returns>
    public static int Scrub(ContactLedger book, string operatorId, string why)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(operatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(why);

        string id = LedgerId(operatorId);
        ContactHistory standing = book.For(id);
        int erased = Math.Min(ABand, Math.Max(0, standing.HeatOwed));
        if (erased <= 0)
        {
            return 0;
        }

        book.ApplyHeat(id, NameOf(operatorId), -erased, standing.HeatStampSimTime);
        return erased;
    }

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
