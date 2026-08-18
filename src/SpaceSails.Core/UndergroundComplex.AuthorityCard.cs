using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #590 · THE AUTHORITY CARD, WHICH NOW OPENS SOMETHING ────────────────────────────────────────────
    //
    // Owner: "could there be like a keycode etc that allows us access to the lab" — and, earlier the same
    // session, "Coordinates / instructions about places and sights, pin codes to doors etc."
    //
    // Haul.Key already existed and already said "Something down here will open for this." It opened nothing,
    // which is worse than not offering it at all (the #212 law: an affordance you can see and cannot use is
    // worse than none). This is that promise kept.
    //
    // THREE CALLS, each overrulable in one line:
    //
    // 1. IT AUTHORISES THE NEXT SHAFT BAND, and nothing else. #590 offered three candidate shapes and this
    //    is the load-bearing one: the car already serves a BAND and stops, and the way down is already "a
    //    different shaft, somewhere on this floor, which you have to find". A card turns that from a wall
    //    into a thing you EARN by working the band you are on. Depth stops being a number and becomes a
    //    reward.
    //
    // 2. THE SEALED SECTOR DOORS STAY SEALED. #590's option (2) is explicitly declined. Those doors exist to
    //    be walls with a world behind them, and LockedLine deliberately never teases; the moment one of them
    //    can open, every one of them becomes a puzzle and the illusion of scale turns into a lock hunt.
    //    A card never opens a SECTOR door, and TheAuthorityCardTests pins that.
    //
    // 3. NEVER A CODE THE PLAYER TYPES. You have the card or you do not. A keypad minigame would be out of
    //    register with everything around it, and the owner's own phrasing — "allows us access" — is about
    //    possession, not about a puzzle.
    //
    // Canon holds: a card may be countersigned by an office that denies existing. It never says what the
    // building was for.

    // ── #760 · STANDING IS WITH AN OPERATOR, NOT WITH A DOOR ────────────────────────────────────────────
    //
    // Owner, 2026-08-08: "same-company labs on different sites may accept the same cards for access … a card
    // that opens Company X's shaft on this moon should be honored at Company X's dig on the next one."
    //
    // The world was disagreeing with its own props. The estates stencilled on these cards have been
    // company-shaped since #679 and the gate was matching a BODY ID — so a captain who had worked two sites
    // of one outfit was a stranger at the second, and the countersignature that is supposed to mean
    // "somebody vouches for this person" meant "this person may open this hole".
    //
    // What a card carries now is a STANDING: an operator, and how far down that operator's org chart the
    // holder sits. Everything else about the card is untouched, and deliberately so — the face, the office,
    // the title, the site code, and the id in every existing save.

    /// <summary>#760 · How far down an operator's org chart the holder sits.</summary>
    public enum Reach
    {
        /// <summary>The operator's own. Honoured at every gate of theirs, everywhere.</summary>
        Prime,

        /// <summary>A vendor or contractor working to them. Honoured where the gate publishes that it takes
        /// vendors — the shafts do, the head office does not (<see cref="AcceptsVendors"/>).</summary>
        Vendor,
    }

    /// <summary>#760 · WHO VOUCHES FOR THE HOLDER, AND HOW FAR. Null on a card is not a missing standing: it
    /// is the ordinary one — the site's own operator, at prime reach — which is what every card in every save
    /// written before this issue is, and what every card the world mints today still is.</summary>
    /// <param name="OperatorId">The outfit's key (<see cref="SiteOperator.Operator"/>).</param>
    /// <param name="Reach">Prime or vendor.</param>
    public readonly record struct Standing(string OperatorId, Reach Reach);

    /// <summary>Which shaft band this card runs, and whose standing it is. The identity is the fact — a card
    /// is for one band, decided by the world rather than by the moment it is used.</summary>
    /// <param name="BodyId">The site it was issued at.</param>
    /// <param name="Band">The shaft band it runs.</param>
    /// <param name="Standing">#760 · Null for the ordinary card: the site's own operator, prime reach. An
    /// explicit standing is a card that says whose it is because the site it is being read at cannot
    /// say.</param>
    public readonly record struct AuthorityCard(string BodyId, int Band, Standing? Standing = null)
    {
        /// <summary>The stable string a save file and a carried-cards set hold.
        ///
        /// <para>#760 · An ordinary card's id is <b>byte for byte the one it has always been</b> —
        /// <c>body#band</c> — because an ordinary card is what the world mints and what every save holds. A
        /// card carrying an explicit standing spells it out after an <c>@</c>, which an older build's parser
        /// rejects rather than misreads (it wants an integer where the operator key starts), so a save read
        /// by a build that predates this issue drops a card it cannot place instead of inventing one.</para></summary>
        public string Id => Standing is { } s
            ? $"{BodyId}#{Band}@{s.OperatorId}/{(s.Reach == Reach.Vendor ? 'V' : 'P')}"
            : $"{BodyId}#{Band}";

        /// <summary>#760 · WHOSE STANDING THIS IS. The site's own operator unless the card says otherwise —
        /// one question, asked here, so nothing downstream has to know that "nothing written on it" and "the
        /// standing of the place that issued it" are the same card.</summary>
        public string OperatorId => Standing?.OperatorId ?? SiteOperator.Of(BodyId).Id;

        /// <summary>#760 · How far the holder sits down that operator's org chart. Prime unless the card says
        /// otherwise — which is every card this build mints, and every card in every older save.</summary>
        public Reach ReachOfIt => Standing?.Reach ?? Reach.Prime;

        /// <summary>Read one back off a save. Returns false on anything that is not a card we wrote.
        ///
        /// <para>#760 · BOTH FORMS. <c>body#band</c> — every card ever written before this issue — reads back
        /// as the card it has always been: the site's own operator, prime reach. <c>body#band@operator/P</c>
        /// or <c>/V</c> reads back the standing that was written down. Nothing else parses.</para></summary>
        public static bool TryParse(string? id, out AuthorityCard card)
        {
            card = default;
            if (id is null)
            {
                return false;
            }

            // #760 · The standing, if one was written down. Taken off first, so the band arithmetic below is
            // the arithmetic it has always been and the old form runs through code that never learned a new
            // shape.
            Standing? standing = null;
            int at = id.LastIndexOf('@');
            if (at >= 0)
            {
                string tail = id[(at + 1)..];
                int slash = tail.LastIndexOf('/');
                if (slash <= 0 || tail.Length - slash != 2)
                {
                    return false;
                }
                Reach reach;
                switch (tail[^1])
                {
                    case 'P':
                        reach = Reach.Prime;
                        break;
                    case 'V':
                        reach = Reach.Vendor;
                        break;
                    default:
                        return false;
                }
                standing = new Standing(tail[..slash], reach);
                id = id[..at];
            }

            int cut = id.LastIndexOf('#');
            if (cut <= 0 || !int.TryParse(id.AsSpan(cut + 1), System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int band) || band < 0)
            {
                return false;
            }
            card = new AuthorityCard(id[..cut], band, standing);
            return true;
        }
    }

    /// <summary>#760 · WHAT KIND OF GATE IS BEING ASKED. The one thing a gate publishes about itself that
    /// changes who it honours, and it is a property of the DOOR rather than of the building — which is what
    /// keeps it from becoming a second copy of "is this the head office".</summary>
    public enum GateKind
    {
        /// <summary>A shaft gate under a branch office. Takes vendors: the holes were dug by contractors and
        /// always have been.</summary>
        Shaft,

        /// <summary>The head office's own. Takes nobody's vendors — a contractor's paper is standing with
        /// whoever hired them, and nobody at this address hired them.
        ///
        /// <para>FLAGGED, said out loud rather than left to be discovered: <b>no gate in the shipped game is
        /// of this kind yet</b>. The head office's shaft gate is deliberately ABSENT (#411) and that absence
        /// is its rank difference. This is the rule written ahead of its door, so the day HQ grows one the
        /// answer is already there and already guarded — and it is the owner's to overrule in one line.</para></summary>
        HeadOffice,
    }

    /// <summary>#760 · Does this gate take a vendor's paper? Fable's default for v1, in one place: the shafts
    /// do, the head office does not.</summary>
    public static bool AcceptsVendors(GateKind kind) => kind != GateKind.HeadOffice;

    /// <summary>
    /// #760 · <b>THE ONE PREDICATE.</b> Does the card in this hand open that gate?
    ///
    /// <para>Three questions and no fourth: the gate's own band, the operator who runs the site the gate is
    /// in, and whether a vendor's reach is far enough for this kind of door. It replaces the body-id equality
    /// that used to stand in for all three, which is why ONE function now answers for the lift panel, the
    /// satchel's TRY, the wallet fan and the remote's send. Four callers reaching four conclusions about one
    /// question is the bug class this repo keeps a table of.</para>
    ///
    /// <para><b>The band still has to match.</b> Standing travels; a shaft number does not. A card runs one
    /// hole per site, and #679's sharpest refusal — <i>this card runs shaft 3 of this site</i> — is exactly
    /// what would be lost by turning an operator's paper into a skeleton key to their whole estate.</para>
    /// </summary>
    /// <param name="held">The card in the captain's hand.</param>
    /// <param name="gate">The gate: the site it is in, and the band it runs.</param>
    /// <param name="kind">What kind of door it is. A shaft unless somebody says otherwise.</param>
    public static bool Honours(AuthorityCard held, AuthorityCard gate, GateKind kind = GateKind.Shaft) =>
        held.Band == gate.Band
        && string.Equals(held.OperatorId, SiteOperator.Of(gate.BodyId).Id, StringComparison.Ordinal)
        && (held.ReachOfIt == Reach.Prime || AcceptsVendors(kind));

    /// <summary>#760 · Does anything in this wallet open that gate, and which? The pocket's version of
    /// <see cref="Honours"/>, written once so the panel and the remote can never disagree about a
    /// satchel.</summary>
    public static AuthorityCard? StandingFor(
        IReadOnlyList<Satchel.Item>? carried, AuthorityCard gate, GateKind kind = GateKind.Shaft)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (item.Kind == Satchel.Kind.Authority
                && AuthorityCard.TryParse(item.Id, out AuthorityCard held)
                && Honours(held, gate, kind))
            {
                return held;
            }
        }
        return null;
    }

    /// <summary>#760 · The same question asked of a set of card IDS rather than of a pocket — what the lift
    /// panel is handed. It is <see cref="StandingFor"/> with the wallet already unpacked, and it asks
    /// <see cref="Honours"/> exactly as that one does: two callers of one predicate, never two
    /// predicates.</summary>
    public static AuthorityCard? StandingAmong(
        IReadOnlyCollection<string>? heldCardIds, AuthorityCard gate, GateKind kind = GateKind.Shaft)
    {
        foreach (string id in heldCardIds ?? [])
        {
            if (AuthorityCard.TryParse(id, out AuthorityCard held) && Honours(held, gate, kind))
            {
                return held;
            }
        }
        return null;
    }

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

    /// <summary>#695 · ONE OFFICE, ONE FACE. The office that issued a card is the letterhead printed across
    /// the top of it AND the photograph laminated into it, and those are the same office because they are
    /// the same record — not because two pieces of arithmetic were written to agree.
    ///
    /// <para>Owner, wallet in hand: <i>"I have 3 ID cards but they all have the same gen AI image."</i> The
    /// title had rolled one of five offices since #679; the picture was a single constant. Pairing them by
    /// re-deriving the roll at the art seam would have been the house's most expensive bug class — two
    /// sources for one fact — waiting for somebody to touch one seed string and not the other.</para></summary>
    /// <param name="Letterhead">What the office stamps across the top of the card.</param>
    /// <param name="ArtUrl">The face laminated into it (#695). Degrades cleanly like every other art slot.</param>
    public readonly record struct CardOffice(string Letterhead, string ArtUrl);

    /// <summary>The five offices a card can be issued by, in the order the roll indexes them. Order is part
    /// of the save-compatible identity of a card: changing it re-issues every card in every wallet.</summary>
    private static readonly CardOffice[] TheOffices =
    [
        new("OFFICE OF WORKS · SUB-REGISTRY", "art/the-authority-card-works.jpg"),
        new("MINISTRY LIAISON · UNNUMBERED", "art/the-authority-card-liaison.jpg"),
        new("ESTATES · SPECIAL PROJECTS", "art/the-authority-card-estates.jpg"),
        new("PROCUREMENT · SCHEDULE C", "art/the-authority-card-procurement.jpg"),
        new("INSPECTORATE · NO STANDING", "art/the-authority-card-inspectorate.jpg"),
    ];

    /// <summary>Every office, for an audit that has to walk them all. Nothing in the game iterates this —
    /// a card gets exactly one, from <see cref="OfficeOf"/>.</summary>
    public static IReadOnlyList<CardOffice> CardOffices => TheOffices;

    /// <summary>WHICH office issued this card. The single roll — everything printed on the card, in words or
    /// in pixels, reads its answer rather than rolling again.</summary>
    public static CardOffice OfficeOf(AuthorityCard card) =>
        TheOffices[(int)(DiceRule.Seed($"hive:card:{card.BodyId}:{card.Band}") % (ulong)TheOffices.Length)];

    /// <summary>#695 · The face of THIS card. A pure function of the card's identity — no stored state, so a
    /// wallet loaded off a save shows the same five faces it showed when the cards were minted.</summary>
    public static string AuthorityCardArtUrl(AuthorityCard card) => OfficeOf(card).ArtUrl;

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

    // ── #678 · THE POCKET NEVER LIES ────────────────────────────────────────────────────────────────────
    //
    // Owner, after a live playtest: "we should have CI test that makes sure all picked items that sound
    // useful are put into the inventory ... If refused the item should stay where it was investigated last —
    // not disappear like they do now, or seem to."
    //
    // Two silent drops, one law. The pickup sentence and the pickup were composed in the client in the wrong
    // order — the line was printed, the room was marked emptied, and only then did Satchel.Add get a chance
    // to refuse — so a full pocket ate a find while announcing it, and a Key room whose card could not be
    // minted narrated a countersigned card into a hand that was empty. Both are this repo's third named bug
    // class: the sim doing one thing while the sentence reports another.
    //
    // The composition lives here now, pure, where a test can walk every haul against every pocket. The rule
    // it enforces, in one line:
    //
    //     A PICKUP LINE MAY ONLY BE PRINTED FOR SOMETHING THAT ACTUALLY WENT IN.
    //
    // And its other half: what the pocket cannot take is NOT consumed. The room keeps it, and searching
    // again offers it again — which is the enforcement side of #615 (leave must not destroy).

    /// <summary>#678 · What turning over one room actually yields: the thing that goes in the pocket (null if
    /// nothing does), the sentence that says so, and whether the room has been emptied at all.</summary>
    /// <param name="Take">The item to add, or null — nothing to add is not a failure, it is most rooms.</param>
    /// <param name="Line">The pocket line appended to the haul line. Empty where there is nothing to say.</param>
    /// <param name="RoomEmptied">False ONLY when the pocket refused the find. The caller must not mark the
    /// room searched — the find is still lying there.</param>
    public readonly record struct Pickup(Satchel.Item? Take, string Line, bool RoomEmptied);

    /// <summary>#678 · What goes in the pocket, said in the same breath as the decision to put it there.</summary>
    /// <param name="haul">What the room holds.</param>
    /// <param name="hereBodyId">The site being searched — used only to tell a card for THIS building from a
    /// card for another one, which is the one thing worth saying about an authority as it goes in.</param>
    /// <param name="minted">For a <see cref="Haul.Key"/>, the card the caller actually minted. Null means no
    /// card exists to hand over, and then the room says so rather than describing one.</param>
    /// <param name="findId">The durable id of this find — the seed tag the prose is rebuilt from.</param>
    /// <param name="carried">What is already in the pocket.</param>
    public static Pickup WhatGoesInThePocket(
        Haul haul, string hereBodyId, AuthorityCard? minted, string findId,
        IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(hereBodyId);
        ArgumentNullException.ThrowIfNull(findId);

        Satchel.Item? take = haul switch
        {
            Haul.Records => new Satchel.Item(Satchel.Kind.Paper, findId),
            Haul.Dirt => new Satchel.Item(Satchel.Kind.Dirt, findId),

            // #614 · What goes in the pocket is the RECORD of the thing on the pallet. You cannot lift it,
            // and a satchel claiming to hold a three-metre alloy band would be the same lie one size up.
            Haul.Relic => new Satchel.Item(Satchel.Kind.Relic, findId),
            Haul.Key when minted is { } card => new Satchel.Item(Satchel.Kind.Authority, card.Id),
            _ => null,
        };

        if (take is { } wanted && !Satchel.CanTake(carried, wanted))
        {
            return new Pickup(null, PocketFullLine, RoomEmptied: false);
        }

        string line = haul switch
        {
            Haul.Records => "  🎒 Into your pocket: operational paper.",
            Haul.Dirt => "  🎒 Into your pocket: a file on somebody.",

            // #677 · A record out of the halls is the SAME law as the pallet — what goes in the pocket is the
            // record of a thing that stays — said in the owner's own words, and it carries no leading indent
            // because the room it came out of has nothing of its own to say first (HaulLine returns empty
            // there, deliberately). Told apart by the find's own id, minted once by FindId.
            Haul.Relic when IsHallRecord(findId) => FoundRecordFindLine,
            Haul.Relic => "  🎒 Into your pocket: measurements, a photograph, a scraping. The thing itself " +
                "stays where it is.",
            Haul.Key when minted is { } c && !string.Equals(c.BodyId, hereBodyId, StringComparison.Ordinal)
                => "  🎒 Into your pocket: an authority card — and it is not for this building.",
            Haul.Key when minted is not null => "  🎒 Into your pocket: an authority card.",
            Haul.Equipment => "  💳 Crated and carried out — it sells, it does not fit a pocket.",

            // A stripped room, and a Key room that had no card left to give. Neither has anything to say
            // about a pocket, and saying nothing is the honest answer for both.
            _ => "",
        };

        return new Pickup(take, line, RoomEmptied: true);
    }

    /// <summary>#678 · What a full pocket says. It is the only refusal in the game that leaves the world
    /// unchanged, and it has to be unmistakable about that: the find is still there.</summary>
    public const string PocketFullLine =
        "  🎒 Your hands and pockets are full, so you put it back exactly where it was lying. It will still " +
        "be here when you have read, spent or left something behind.";

    /// <summary>What the gate says when the card works. Said once, at the moment the car goes deeper than
    /// this shaft was ever dug to.
    ///
    /// <para>#592: worded so it is true of BOTH shafts it can open. It used to say "where the plan said a
    /// shaft would be" — right about the listed building, and a lie about the band the plan denies having.
    /// A card that announces the secret is a card that has given it away.</para></summary>
    public static string CardAcceptedLine(AuthorityCard card) =>
        $"🎫 You find the other shaft. It is not marked and it is not beside the first one, and its gate " +
        $"reads the card without hesitating — {CardTitle(card)}, countersigned by an office that stopped " +
        "answering its own post decades ago and never once revoked a thing. The car below is colder than " +
        "the one above.";

    /// <summary>#760 · What the gate says when it honours a card that was issued somewhere else.
    ///
    /// <para>Its own sentence, because it is its own fact. "The gate reads it without hesitating" is what a
    /// card's own gate says; this is a gate deciding that an office two moons away is an office it answers
    /// to, and a captain let through on that basis has learned something real about the world — which is
    /// exactly the payoff #715 names for making entities legible.</para>
    ///
    /// <para>It names the SITE the card came from and not the outfit. The site code is printed on the card
    /// in the captain's hand (#679); the company is a thing they can look up in their own pocket, where the
    /// satchel groups the wallet under it. A gate that read a company name out loud would be doing the
    /// player's inference for them.</para></summary>
    public static string StandingHonouredLine(AuthorityCard held) =>
        $"🎫 The gate reads it, and it is not this building's card — it was issued for " +
        $"{BodyNames.Designation(held.BodyId)} SITE, and the gate opens anyway. Whoever the " +
        "countersignatures answer to, they answer for this hole too. Nobody down here was ever working for " +
        "the moon.";

    // ── #684 · THE PANEL READS YOUR WALLET WITHOUT BEING ASKED, AND THAT READ IS A SCENE ───────────────
    //
    // Owner's ruling, on whether the shaft gate should become a TRY target like the doors: it should not.
    // "The panel's unprompted wallet-read IS its character" — a machine that goes through your pockets for
    // you is the whole administrative horror of this place in one gesture, and putting a verb in front of it
    // would turn the building polite.
    //
    // What was wrong was never the interaction. It was that the read happened in SILENCE and then answered
    // out of a SECOND set of sentences. `SatchelTry.Target.ShaftGate` carries the sharpest refusal matrix in
    // the game — #679/#683 taught it to tell "another shaft of THIS site" from "somebody else's building",
    // each named — and it had no client caller at all, while the panel said a flat "every one of them
    // countersigned, current, and for another shaft" out of `WrongCardLine`. Two answers to one question,
    // and the better one was the one nobody could read. That is this repo's third named bug class wearing a
    // costume: the sim knowing a thing the sentence does not say.
    //
    // So `WrongCardLine` is GONE and the matrix is the source. This composes the read into the house card
    // idiom (#528) so the answer is TOLD rather than muttered — art, a title, and the matrix's own line
    // verbatim — and per #736's law the line the player acts on lives ON the card that is up, never only in
    // a pulse behind its backdrop.

    /// <summary>#684 · The panel's read of the wallet, and the card it is told on.</summary>
    /// <param name="Worked">Whether the gate opened. False is a refusal, and <paramref name="Line"/> names
    /// its reason either way (#603's law).</param>
    /// <param name="Line">The matrix's own sentence, verbatim. Nothing here rewrites it.</param>
    /// <param name="Presented">The card the gate actually read, or null when there was nothing in the wallet
    /// to read. It is what decides the face on the card (#695) — the office that issued THIS one.</param>
    /// <param name="Label">The card's title.</param>
    /// <param name="ArtUrl">The presented card's own face, or the nameless fallback when none was presented
    /// — which can never impersonate one of the five offices.</param>
    public readonly record struct GateRead(
        bool Worked, string Line, AuthorityCard? Presented, string Label, string ArtUrl);

    /// <summary>#684 · What the story card is called, at a refusal and at a reading alike. It names the thing
    /// the owner declined to put a verb in front of: the machine goes through your wallet, and you watch.</summary>
    public const string GateReadLabel = "🎫 THE PANEL READS YOUR WALLET";

    /// <summary>#684 · The gate below this car's band, reading what the captain happens to be carrying.
    ///
    /// <para>The judgement is <see cref="SatchelTry.ReadTheWallet"/>'s and only its — this asks the building
    /// which shaft the gate serves and then does as it is told.</para></summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="standingLevel">The floor the car is on; the gate is the one under this car's band.</param>
    /// <param name="carried">The satchel. Anything that is not an authority is not in the wallet.</param>
    /// <param name="heatAtThisOperator">#715 · What the outfit that runs this site remembers about this
    /// captain (<see cref="IllegalHeat.HeatAtSite"/>). Zero — a captain nobody has anything on — is the
    /// default and the old behaviour exactly, so every caller and every guard written before the meter
    /// existed still asks the question it always asked.</param>
    public static GateRead TheGateReads(
        string bodyId, int standingLevel, IReadOnlyList<Satchel.Item>? carried, int heatAtThisOperator = 0)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #677 · The next shaft that EXISTS. Under an unlisted band there is a band with nothing dug in it,
        // and a gate named for it would be a refusal about solid rock. Where the building has nothing below
        // at all there is no gate to read, and the band arithmetic is only a name for a card nobody holds.
        int band = NextShaftBelow(bodyId, standingLevel) ?? (BandOf(Math.Min(standingLevel, -1)) + 1);
        var gate = new AuthorityCard(bodyId, band);

        SatchelTry.WalletRead read =
            SatchelTry.ReadTheWallet(carried, SatchelTry.Target.ShaftGate, gate.Id);

        AuthorityCard? presented =
            read.Read is { } item && AuthorityCard.TryParse(item.Id, out AuthorityCard c) ? c : null;

        // ── #715 · AND THEN THE OTHER QUESTION, WHICH A COLD GATE NEVER ASKS ────────────────────────────
        //
        // Owner's ruling: the cost of heat is PRESSURE and never a lockout. This is the mildest shape that
        // has teeth — the card is still read, still correct, and still not enough on its own: an outfit that
        // remembers you wants the pass with your face on it as well, on the FIRST press, where a gate that
        // has never heard of you does not ask at any press.
        //
        // It is not a lock. The site's own pass opens it (#804 — the gig is having gone down), and so does
        // walking away for a few hours, because this meter cools in absence and in nothing else. And it is
        // owed to ONE outfit: the identical card at the identical band of a company that has heard nothing
        // opens on the first press, which is exactly the difference the issue asks the player to notice.
        if (read.Outcome.Worked && TheGateWantsAFaceHere(bodyId, carried, heatAtThisOperator))
        {
            return new(
                false, IllegalHeat.TheGateWantsAFaceLine, presented, GateReadLabel,
                presented is { } held ? AuthorityCardArtUrl(held) : AuthorityCardFallbackArtUrl);
        }

        return new(
            read.Outcome.Worked, read.Outcome.Line, presented, GateReadLabel,
            presented is { } shown ? AuthorityCardArtUrl(shown) : AuthorityCardFallbackArtUrl);
    }

    /// <summary>
    /// #715 · <b>DOES THIS SITE'S GATE WANT A FACE WITH THE PAPER?</b> The one predicate, read by the PANEL
    /// (<see cref="LiftPanel"/> — which button is drawn, and whether it opens) and by the READ
    /// (<see cref="TheGateReads"/> — what the card says when it does not). Two callers of one question, never
    /// two questions: a panel that refused while the card said the gate opened is this house's third named
    /// bug class, and it would land in the one system whose register is entirely procedure.
    ///
    /// <para><b>Never at the head office.</b> There is no gate there to ask anything, and that absence is the
    /// rank difference (#411) rather than an oversight — a parent undertaking that started checking passes
    /// would be a branch office with a bigger sign on it.</para>
    /// </summary>
    public static bool TheGateWantsAFaceHere(
        string bodyId, IReadOnlyList<Satchel.Item>? carried, int heatAtThisOperator)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return !IsHeadOffice(bodyId)
            && IllegalHeat.TheGateWantsAFace(heatAtThisOperator)
            && !PatrolBeat.BadgeHeld(bodyId, carried);
    }

    /// <summary>#684 · The same read, the other way it can end — told at the ARRIVAL rather than at the
    /// panel, because that is where the ride's beat has been said since #689 and a card raised on the frame
    /// the floor is rebuilt is a card raised at nobody.
    ///
    /// <para><see cref="CardAcceptedLine"/> stays the one sentence for this: it is about the car going deeper
    /// than the building admits to, which is a different question from the one the matrix answers, and it has
    /// been the panel's success beat since #592.</para></summary>
    public static GateRead TheGateAccepted(AuthorityCard card) => new(
        true, CardAcceptedLine(card), card, GateReadLabel, AuthorityCardArtUrl(card));
}
