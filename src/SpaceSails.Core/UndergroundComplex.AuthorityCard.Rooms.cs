using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE CARD IN THE ROOM — what a refusal at a gate costs and who remembers it, which bands a site
/// has, the card a room holds, its title, and the line the find is written up in.
///
/// <para>Split out of <c>UndergroundComplex.AuthorityCard.cs</c> under #251 as a pure move: two runs of the
/// base file, no member renamed, re-scoped or re-ordered. The card offices (<c>TheOffices</c>, a
/// <c>static readonly</c>) stay in the opening file between them (#1163).</para>
/// </summary>
public static partial class UndergroundComplex
{
    // ── #715/#760 · WHAT A REFUSAL COSTS, AND WHO REMEMBERS IT ───────────────────────────────────────────
    //
    // Owner's ruling on #715: "the illegal heat should be targeted at the entity we crossed ... so not like
    // the Casinos that distribute cheaters lists in Vegas". #760 needs exactly one thing out of that meter —
    // that a standing REFUSED over the air costs what a card refused at a gate costs — and #715 is still
    // open, so there is no meter to bank it in.
    //
    // What is published here is therefore the CHARGE and not a total: who is owed, and how much. One
    // function, read by the gate and by the remote, so the day #715 lands there is one number to wire up and
    // no second spelling of it to go looking for. It is keyed to the OPERATOR and never to the moon, which is
    // the whole of the ruling: you burned somebody, and they remember, and nobody tells anybody else.

    /// <summary>#715 · Heat owed to one outfit.</summary>
    /// <param name="OperatorId">Who is owed it. Never a body id — a rock does not hold a grudge.</param>
    /// <param name="Points">How much. Zero is a real answer and the commonest one.</param>
    public readonly record struct HeatCharge(string OperatorId, int Points);

    /// <summary>#715/#760 · What one refused authority costs. Small, because being refused mostly costs you
    /// the refusal; the pressure comes from doing it again.</summary>
    public const int RefusedCardHeat = 1;

    /// <summary>#715/#760 · WHAT A REFUSAL AT THIS SITE'S GATE CHARGES, and to whom. The one function both
    /// the gate and the remote read.</summary>
    public static HeatCharge RefusedAtTheGate(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return new HeatCharge(SiteOperator.Of(bodyId).Id, RefusedCardHeat);
    }

    /// <summary>#715/#760 · Nothing owed to anybody: what an accepted standing costs, and what a send into a
    /// silence costs.</summary>
    public static HeatCharge NothingOwed => new(string.Empty, 0);

    /// <summary>Does this site have a shaft band that deep at all? Band 0 is the one the surface lift head
    /// serves; a band exists when its top floor is still inside the site's own depth.
    ///
    /// <para>#592: measured against <see cref="TrueDepthOf"/>, not the listed depth — so a Key found on the
    /// last floor the building admits to issues the card for the band it does not. That composition IS the
    /// way in: the panel never mentions the shaft, and a piece of paper somebody left in a room does.</para></summary>
    public static bool SiteHasBand(string bodyId, int band) =>
        band >= 0
        && (BandTop(band) >= DepthOf(bodyId)
            || (HasUnlistedBand(bodyId) && band == UnlistedBandOf(bodyId))
            // #677 · …and the halls, which are not a band of this building at all. The band BETWEEN them is
            // deliberately not here: nothing was dug in it, so nothing may ever authorise it or offer it.
            || (HasFoundBand(bodyId) && band == FoundBandOf(bodyId)));

    /// <summary>#590 · WHICH card a Key room holds: the one for the shaft band immediately below the floor
    /// you found it on. Not a roll — a fact about the building, and the most legible possible rule, because
    /// it means the card you need for the next shaft is always somewhere in the band you are standing in.
    ///
    /// <para>Returns null at the bottom band, where there is no shaft below to authorise. That Key is not
    /// wasted: the client turns it into a lead naming another moon, which is the same payoff Records and
    /// Dirt already give and keeps the deepest floor from handing out a card for a hole nobody dug.</para></summary>
    public static AuthorityCard? CardInRoom(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #677 · The next shaft that EXISTS, not the next band number. Under the band nobody listed there is
        // a band with nothing in it, and a card for a hole nobody dug is exactly the lie #613 was filed
        // about — a countersigned authority for a floor the building cannot open onto.
        return NextShaftBelow(bodyId, level) is { } next ? new AuthorityCard(bodyId, next) : null;
    }

    /// <summary>What is printed on the card. Institutional, expensive, and explains nothing — the register
    /// of an office that will not admit to being one.
    ///
    /// <para>#679 · AND IT SAYS WHICH SITE. Owner, holding three of them: <i>"a captain holding three cards
    /// from three moons sees three identical shapes and cannot plan a wallet."</i> He is right, and the fix
    /// is the least invented thing available: a pass has ALWAYS had the holder's place of work printed on
    /// it. So the site designation goes on the face, in the office's own register — caps, like everything
    /// else that office stamps — as the last field of the title.</para>
    ///
    /// <para>This is a deliberate softening of §13.10's <i>"never which moon"</i>, made by the owner in #679
    /// and recorded there: the line that must not be crossed is a NAV FIX. A site code sorts a wallet; a
    /// bearing and a distance would hand the captain the search the whole Hive is arranged around. It still
    /// never says what the building was for (§13.8), which is the canon that actually matters.</para></summary>
    public static string CardTitle(AuthorityCard card) =>
        $"🎫 SHAFT {card.Band + 1} · {OfficeOf(card).Letterhead} · " +
        $"{BodyNames.Designation(card.BodyId)} SITE";

    /// <summary>The Key haul, said out loud. It names the shaft it runs, because a card whose purpose is a
    /// mystery is a keypad by another route.
    ///
    /// <para>#678 · IT DESCRIBES THE CARD THE CALLER ACTUALLY MINTED, and there are three of those: the one
    /// for the shaft under this building, #613's card for ANOTHER site, and — the case that broke it — no
    /// card at all. It used to ask <see cref="CardInRoom"/> itself and narrate a countersigned authority in
    /// the captain's hand whenever the answer was null, which was a sentence about an object the sim had not
    /// handed over. That is the third named bug class, in the residual path of the fix made for it.</para></summary>
    /// <param name="minted">The card that went into the pocket, or null if none did.</param>
    public static string KeyLine(string bodyId, int level, AuthorityCard? minted)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (minted is not { } card)
        {
            // Nothing was minted, so nothing is described. The room still pays what an ordinary room pays —
            // a look at what somebody did on their way out — and it never once claims you are holding a card.
            string[] empty =
            [
                "🪪 A lanyard on the floor and the holder still clipped to it, and the window in the holder " +
                "is empty. Whoever ran the shafts off this floor left with the one thing in this room worth " +
                "taking, and the counterfoil book agrees with them: signed out, never signed back in.",

                "🪪 A drawer of counterfoils, and every stub in it is torn along the same crooked line. The " +
                "cards themselves went out of this building in somebody's breast pocket. What is left is the " +
                "half the office kept, which opens nothing and was never meant to.",

                "🪪 A punch, an inking pad gone hard, and a rack of blanks that were never made out to " +
                "anybody. This is where the authorities were issued. It is not where they ended up.",
            ];
            ulong seed = DiceRule.Seed($"hive:nokey:{bodyId}:{level}");
            return empty[(int)(seed % (ulong)empty.Length)];
        }

        if (!string.Equals(card.BodyId, bodyId, StringComparison.Ordinal))
        {
            // #613's wallet, and #679's site code on the face of it: a card that crossed a world in somebody
            // else's pocket and is still good at gates you have not found yet.
            return $"🎫 An authority card, countersigned twice and still active: {CardTitle(card)} — and " +
                "that is a building which is not this one. Whoever carried it worked somewhere else, and " +
                "came here, and did not leave.";
        }

        return $"🎫 An authority card, countersigned twice and still active: {CardTitle(card)}. This " +
            "building never got the news that its owners stopped paying, and neither did its gates. The " +
            "second shaft is somewhere on these floors, and this runs it.";
    }
}
