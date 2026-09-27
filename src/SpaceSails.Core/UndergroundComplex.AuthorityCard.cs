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
    // 3. ~~NEVER A CODE THE PLAYER TYPES.~~ **OVERRULED BY THE OWNER, 2026-08-02 (#602), DELIBERATELY.**
    //    This call read: "You have the card or you do not. A keypad minigame would be out of register with
    //    everything around it." The reasoning behind it was sound and is worth keeping on the record — a lock
    //    you can ATTEMPT turns a wall into a puzzle, and a building full of attemptable walls is a lock hunt
    //    rather than a place. What it missed is the second half of the owner's own sentence: "getting that
    //    wrong would bring security to the site."
    //
    //    THE AFFORDANCE STATES ITS OWN COST, which is how this game has always resolved these. A vicious
    //    warning notice beside the pad (THREE WRONG ENTRIES CALL SECURITY. THE PAD REMEMBERS.) removes the
    //    puzzle entirely: the building has told you exactly what happens, so entering a guess is not
    //    problem-solving, it is gambling with a stated stake. Three tries is small enough to be uncrackable
    //    by construction — nobody enumerates a keypad with three attempts — so the code can only ever come
    //    from HAVING FOUND IT, which was the condition #602 set for allowing a pad at all. And the count is a
    //    ninety-second DECAY WINDOW, not a ledger (owner, same day): a building whose staff idly try the pad
    //    in passing cannot summon a patrol every third lifetime attempt, so it tolerates the curious and
    //    reacts to the persistent. The two rules hold each other up — the code being FINDABLE ONLY is what
    //    makes the reset harmless, and the reset is what makes the sticker fair rather than punitive. If
    //    anyone ever makes the code deducible, the reset becomes an exploit.
    //
    //    WHAT THE PAD DOES NOT TOUCH. It is the gated FLOORS on the lift panel and nothing else. Call 2 above
    //    stands unchanged — a SECTOR door has no reader, no pad and no way in — and so does the stop order's
    //    seal (see UndergroundComplex.Signs.HasNoReader). The pad lives beside the panel, on a row that is
    //    already drawn and already refusing; it never appears on a leaf somebody welded shut.
    //
    //    AND THE CARD IS NOT DEMOTED. A right code opens that band for THIS EXCURSION ONLY. The card remains
    //    the durable way in — it is in the wallet and it is still there next visit — which is the whole
    //    difference between the paper you earned and the paper you found in somebody's drawer.
    //
    //    Lives here rather than in a test comment because the ruling is about the CARD IDIOM's scope, and the
    //    machinery it authorises is UndergroundComplex.LiftCode.
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
}
