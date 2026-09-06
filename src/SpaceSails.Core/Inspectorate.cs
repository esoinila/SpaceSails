using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1149 slice 2 · <b>THE SAFETY INSPECTOR'S CARD — ONE MORE ID IN THE FLETCH WALLET.</b>
///
/// <para>Owner, ruling on #608: <i>"emergency air stations are built as part of standard safety rules …
/// built and monitored to exist by INSPECTORS. A lore-plot point: a safety inspector as a way in, one more
/// ID in our Fletch wallet."</i> Slice 1 built the world that makes the card mean something — a refuge on
/// every airless floor, holding by default, with an unsigned inspection tag hanging on its valve. This is
/// the man the tag is about, arriving as a piece of laminate.</para>
///
/// <h3>What it is, and the one thing that is new about it</h3>
///
/// <para><b>It is a <see cref="PatrolBeat.Badge"/>, unchanged, with an ISSUER THAT IS NO SITE.</b> Every
/// other pass in this game is a building vouching for a person, and #804's whole ladder is built on reading
/// the site code off it out loud. This one is issued by an office that sits ABOVE the sites — the
/// INSPECTORATE, which the game has already been printing across the top of authority cards since #679
/// (<c>UndergroundComplex.CardOffices</c>, <i>INSPECTORATE · NO STANDING</i>) — so it is honoured at every
/// listed complex and belongs to none of them.</para>
///
/// <para>Nothing else about the wallet moves. The satchel stores it (<see cref="Satchel.Kind.Badge"/> →
/// the wallet compartment), the fan fans it (<see cref="WalletChoice.Fan"/>), the man on the rota reads it
/// (<see cref="PatrolBeat.TheGuardReads"/>), the captain's own book files it, and the vault round-trips it,
/// all without a line of new plumbing — which is the whole reason the issuer is a badge id and not a fifth
/// <see cref="Satchel.Kind"/>.</para>
///
/// <h3>Where it is honoured, and it is three different questions</h3>
///
/// <list type="number">
/// <item><b>The refuge floors, ALWAYS, on every site.</b> An inspector may look at any refuge — that is
/// what the word means and what the tag on every valve in the game says he does. So a man on a round who
/// stops a captain on a floor that carries a refuge (<see cref="UndergroundComplex.RefugeOnThePlan"/>, which
/// on this ground is every airless floor) reads the card and walks on, whatever the roster says.</item>
/// <item><b>The gates — the ID CHECK band (#715) and the SEALED row (#590) — for ONE EXCURSION</b>, and
/// only after the card has been read and honoured at the front. An inspection is a thing that is HAPPENING;
/// it starts when somebody on the rota accepts that it is, and it is over when the captain goes home. That
/// state lives on the excursion beside #602's pad (<c>SurfaceExcursion.InspectionRunning</c>) and never in
/// the vault, for the pad's exact reason: the card is durable and the inspection is an afternoon.</item>
/// <item><b>Nowhere else, and that is the bet.</b> See below.</item>
/// </list>
///
/// <h3>The roster, which is what makes a bought card a gamble</h3>
///
/// <para><b>Nobody inspects unannounced.</b> The process is strict, and a site knows which watch its
/// inspector is due on. Presenting the card off the roster, away from a refuge floor, is
/// <see cref="WalletChoice.Outcome.WrongSite"/>'s cousin: the man says the same flat sentence and then the
/// round calls it in — the escalation that has been on these floors since #804, with no new security kind
/// and nothing new said about it (§13.8).</para>
///
/// <para><b>And a FOUND card is always due at its own site.</b> Not because the card remembers where it was
/// found — it does not, and a pass that carried its own provenance would be a second identity inside one
/// object — but because of what is true about the SITE it was found on: the failed refuge's site has an
/// inspector who came and never left (the tag's undated third entry), and it is still expecting him back.
/// <see cref="InspectionIsDue"/> asks the ground that question directly, so the law holds for any card the
/// captain brings there and needs no provenance at all.</para>
///
/// <h3>Laws</h3>
///
/// <para>It is <b>never consumed</b> — nothing anywhere spends it. It is a bet each time it is shown, not a
/// ticket. <b>The inspector is never named</b>, here or anywhere. And the covert-organisation paradox stays
/// on PAPER, where slice 1 put it: the tag says <i>No signature — none required</i> and no line in this file
/// or any other explains why.</para>
/// </summary>
public static class Inspectorate
{
    // ── WHO ISSUED IT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The issuer, in the place a badge id keeps a site code. It is deliberately not a body id and
    /// deliberately not parseable as one: <c>BodyNames.Designation</c> is never asked about it, because every
    /// seam that would have asked (<see cref="PatrolBeat.BadgeTitle"/>, <see cref="WalletChoice.Claims"/>)
    /// answers with <see cref="Plate"/> before it gets that far.
    ///
    /// <para>It can never collide with a real site: <c>FoundPass.MintedElsewhere</c> draws only from the
    /// grounds an ephemeris actually has, and no moon in this game or any generated one is called
    /// <c>inspectorate</c>.</para></summary>
    public const string IssuerId = "inspectorate";

    /// <summary>#1149 · <b>THE AT-RANGE PLATE.</b> Canon, verbatim (Fable, 2026-09-06), and the whole of what
    /// the card says on its face. One word, because that is the entire claim: not a site, not a tier, not a
    /// department — an office, and the reason a man steps out of the way.</summary>
    public const string Plate = "INSPECTORATE";

    /// <summary>#1149 · <b>THE LOOK CARD.</b> Canon, verbatim. #614's law kept exactly: it says what the
    /// OBJECT is and not one word about what it opens.
    ///
    /// <para>The second sentence is the whole of the horror and it is a clerical observation. A photograph
    /// that has been replaced ONCE ALREADY is a card that has been somebody else's before it was yours, and
    /// the room it was found in does not say whose. Nothing explains it (§13.8, #649's
    /// comprehension-without-acceptance), and the inspector is not named.</para></summary>
    public const string LookCardLine =
        "Inspectorate credentials. The photograph has been replaced once already.";

    /// <summary>#1149 · <b>WHAT THE MAN ON THE ROTA SAYS.</b> Canon, verbatim, and it is said ONCE per
    /// excursion — at the read that opens the inspection, whichever way that read then goes.
    ///
    /// <para>It is flat, and it works in both directions, which is why it is the only sentence this feature
    /// authors for the challenge. Where an inspection IS due it is a shrug: nobody told him, nobody ever
    /// does, and he has four more corridors. Where one is NOT due it is the same shrug and then the
    /// beginning of a telephone call. The captain cannot tell the two apart from the sentence, which is
    /// exactly the position a bought card puts them in.</para>
    ///
    /// <para>Afterwards, inside the same excursion, the card reads like any pass that works
    /// (<see cref="PatrolBeat.SatisfiedLine"/>) — a man who has already been told there is an inspection on
    /// does not announce it to you a second time.</para></summary>
    public const string HonouredLine = "Inspection. Nobody told us. Nobody ever does.";

    /// <summary>The face laminated into it — the INSPECTORATE office's own, the one
    /// <c>UndergroundComplex.CardOffices</c> has been printing since #695. One office, one face (#695's law):
    /// a second painting for the same letterhead would be two answers to what this office's card looks
    /// like.</summary>
    public const string ArtUrl = "art/the-authority-card-inspectorate.jpg";

    /// <summary>What the look card is titled — the glyph a pass wears and the plate that is printed on it,
    /// composed rather than authored, so the card and the satchel row cannot come to two names for one
    /// object.</summary>
    public static string CardLabel => $"{PatrolBeat.BadgeGlyph} {Plate}";

    /// <summary>The card as a thing in a wallet. There is exactly ONE — no site scoping, no copies, no
    /// second id — so a captain who found one and then bought one is a captain carrying one card.</summary>
    public static Satchel.Item Card => PatrolBeat.Badge(IssuerId);

    /// <summary>Is this row the inspector's card? Asked of a paper rather than of a read, so every seam that
    /// meets one — the fan, the plate, the look card, the ladder — recognises it the same way.</summary>
    public static bool IsTheCard(Satchel.Item paper) =>
        paper.Kind == Satchel.Kind.Badge
        && string.Equals(PatrolBeat.SiteOfBadge(paper.Id), IssuerId, StringComparison.Ordinal);

    /// <summary>Is it in the wallet at all? The possession IS the state — no flag and no parallel ledger,
    /// the discipline <see cref="PatrolBeat.BadgeHeld"/> already keeps.</summary>
    public static bool Held(IReadOnlyList<Satchel.Item>? carried) => PatrolBeat.BadgeHeld(IssuerId, carried);

    // ── WHEN AN INSPECTION IS DUE ────────────────────────────────────────────────────────────────────────

    /// <summary>#1149 · <b>ONE WATCH IN THIS MANY HAS AN INSPECTION ON THE ROSTER.</b> Six, which is a sim
    /// DAY at <c>Interior.PatronRota.WatchSeconds</c> (four hours to the watch) — a site that expects its
    /// inspector about once a day is a site running a real programme rather than a lottery, and it is the
    /// number that makes a bought card a bet worth taking rather than one worth farming. FLAGGED for the
    /// owner's tuning, and the only rate in this file.</summary>
    public const int WatchesPerInspection = 6;

    /// <summary>
    /// #1149 · <b>IS THIS SITE EXPECTING AN INSPECTOR THIS WATCH?</b>
    ///
    /// <para>Two clauses, and the first one is the story. A site whose one refuge FAILED
    /// (<see cref="UndergroundComplex.FailedRefugeFloorOf"/>) is a site with an open inspection on it — the
    /// tag's third entry has no date on it, somebody replaced a seal and never signed for it, and the
    /// building has been waiting for the rest of that visit ever since. So the answer there is always yes,
    /// which is what makes the card the captain FINDS in that refuge good at the ground he found it on
    /// without the card having to remember anything.</para>
    ///
    /// <para>Otherwise the roster: seeded off the site and the WATCH, so it is a fact about a shift rather
    /// than a coin flipped at the moment the palm goes out. A captain who is refused can go and do something
    /// else for four hours and try again, which is the same shape as #715's cooling heat and is the only
    /// honest way for a schedule to behave.</para>
    /// </summary>
    /// <param name="watch">The frozen watch — <c>Interior.PatronRota.WatchIndex</c>, taken once when the
    /// floor was drawn (<c>SurfaceExcursion.CanteenWatch</c>) and never a live clock. A roster that turned
    /// over while a man was walking towards you would be the sim and the sentence describing two different
    /// afternoons, which is this house's third named bug class.</param>
    public static bool InspectionIsDue(string bodyId, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UndergroundComplex.FailedRefugeFloorOf(bodyId) is not null
               || DiceRule.Roll(DiceRule.Seed($"inspectorate:roster:{bodyId}", watch), WatchesPerInspection)
                   .Face == 1;
    }

    /// <summary>
    /// #1149 · <b>WILL THIS CARD BE HONOURED, STANDING HERE, THIS WATCH?</b> The one predicate — read by the
    /// ladder (<see cref="WalletChoice.WhatHappens"/>) and by nothing else, so the sentence the captain is
    /// told and the state the excursion keeps are one answer to one question.
    ///
    /// <para>A refuge floor is honoured unconditionally: an inspector may look at any refuge, on any site, on
    /// any watch, and a building that argued about it would be a building arguing about its own safety
    /// regulations. Everywhere else the roster decides.</para>
    /// </summary>
    public static bool HonouredAt(string bodyId, int level, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UndergroundComplex.RefugeOnThePlan(bodyId, level) || InspectionIsDue(bodyId, watch);
    }

    // ── WHAT THE FENCE WANTS FOR ONE ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1149 · <b>HOW MANY TIERS OF PASS THIS GAME HAS</b>, which is what the top of the ladder is worth.
    ///
    /// <para>Read off <see cref="CanteenTable.OverqualifiedBand"/> and never typed: that is the band a hand
    /// who has spent six weeks in the cage has never held, and <c>CardTitle</c> prints a band one-based, so
    /// band 3 is the card that reads SHAFT 4. Four tiers of paper, from the day-labour chit to the deepest
    /// clearance in the building — and the inspector's card is not on that ladder at all, it is above it, so
    /// it is priced at the top of it.</para></summary>
    public static int TiersOfPass => CanteenTable.OverqualifiedBand + 1;

    /// <summary>
    /// #1149 · <b>WHAT THE FENCE WANTS FOR ONE, AND NOT A NUMBER ANYBODY TYPED.</b> The derivation, in the
    /// order it is composed:
    ///
    /// <list type="number">
    /// <item><b>What one document costs across this desk</b> — <see cref="IntelMarket.BasePrice"/>, the base
    /// of a route tip, which is the only statement this game has ever made about what a piece of paper is
    /// worth on the dark web before anything about the paper.</item>
    /// <item><b>Times the top of the pass ladder</b> — <see cref="TiersOfPass"/>. A pass is worth how deep it
    /// reaches; this one reaches every band of every listed complex, so it is priced as the highest tier
    /// there is.</item>
    /// <item><b>Plus the fence's own markup</b> — <see cref="CompromisingChip.FencePrice"/>, which is the one
    /// function in this game that says what a fence adds to a thing's worth
    /// (<see cref="IntelMarket.SellValueFraction"/> of it, through the market's own arithmetic). Reprice the
    /// intel market and this reprices with it.</item>
    /// </list>
    ///
    /// <para>Nothing here is a constant of its own, which is the discipline the chip's own price keeps and
    /// the reason it is worth keeping: a typed price is a number that stops agreeing with the economy the
    /// first afternoon anybody tunes the economy.</para></summary>
    public static int FencePrice => CompromisingChip.FencePrice(IntelMarket.BasePrice * TiersOfPass);

    /// <summary>Every authored sentence this feature owns, for the audit that reads them all. Three, exactly
    /// as the canon pass authored them — the plate, the look card, and the one thing the man says.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Plate;
        yield return LookCardLine;
        yield return HonouredLine;
    }
}
