using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// #251 · Split from IllegalHeat.cs, moved verbatim: THE BOOK — reading, banking and cooling an outfit's
// heat. Every const of the heat stays in IllegalHeat.cs.
public static partial class IllegalHeat
{
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
}
